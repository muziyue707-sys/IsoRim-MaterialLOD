using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using IsoRim;
using UnityEngine;
using Verse;

[assembly: InternalsVisibleTo("IRME.Tests")]
namespace RimZrism.Materials
{
    public sealed class MaterialDef : Def
    {
        public string model, materialName, normalTexture;
        public float normalStrength = 1;
        public bool flipGreen, flipNormalV = true;
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var error in base.ConfigErrors()) yield return error;
            if (string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(materialName) || string.IsNullOrWhiteSpace(normalTexture))
                yield return "model, materialName and normalTexture required";
            if (!float.IsFinite(normalStrength) || normalStrength < 0 || normalStrength > 4) yield return "normalStrength must be 0..4";
        }
    }

    [StaticConstructorOnStartup]
    public static class Bootstrap
    {
        public static bool Enabled { get; private set; }
        static Bootstrap()
        {
            // The reviewed shader contract and this candidate's API must both match. Never silently force-enable an update.
            string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(Api).Assembly.Location)));
            if (Api.Version < 61 || hash != Compatibility.IsoRimSha256)
            {
                Log.Warning("[IRME] Unsupported IsoRim build: API=" + Api.Version + " SHA256=" + hash + ". Extension disabled.");
                return;
            }
            InitializeEnabled();
        }
        // Do not resolve pending API types until the host hash has passed.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void InitializeEnabled()
        {
            Materials.Initialize();
            ModelLods.Initialize();
            Enabled = true;
            Log.Message("[IRME] 0.3.1 API-only addon initialized; surfaces=" + Materials.LoadedModels + " LOD chains=" + ModelLods.ChainsLoaded);
        }
    }

    internal static class Materials
    {
        private sealed class Corner { internal Vector3 p, n; internal Vector2 uv; internal Vector4 t; }
        private static readonly HashSet<ModelHandle> registered = new HashSet<ModelHandle>();
        private static readonly Dictionary<MaterialDef, Texture2D> textures = new Dictionary<MaterialDef, Texture2D>();
        private static readonly Dictionary<MaterialDef, Dictionary<Material, Material>> cache = new Dictionary<MaterialDef, Dictionary<Material, Material>>();
        internal static int LoadedModels, MaterialRequests;
        internal static bool HasBasis(ModelHandle model) => registered.Contains(model);
        internal static string AssetPath(ModContentPack mod, string relative)
        {
            if (mod == null || string.IsNullOrWhiteSpace(relative)) throw new InvalidDataException("Missing mod/path");
            string root = Path.GetFullPath(mod.RootDir).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(Path.Combine(root, relative));
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Asset path leaves mod directory");
            return full;
        }
        internal static bool Matches(ModContentPack mod, string relative, string path)
        {
            try { return string.Equals(AssetPath(mod, relative), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }
        internal static void Initialize()
        {
            var grouped = new Dictionary<string, List<MaterialDef>>(StringComparer.OrdinalIgnoreCase);
            foreach (var def in DefDatabase<MaterialDef>.AllDefsListForReading)
            {
                try
                {
                    var errors = def.ConfigErrors().ToArray();
                    if (errors.Length > 0) throw new InvalidDataException(string.Join("; ", errors));
                    string path = AssetPath(def.modContentPack, def.model);
                    if (!grouped.TryGetValue(path, out var defs)) grouped.Add(path, defs = new List<MaterialDef>());
                    defs.Add(def);
                }
                catch (Exception e) { Log.Warning("[IRME] Invalid material " + def.defName + ": " + e.Message); }
            }
            foreach (var pair in grouped)
            {
                try { Load(pair.Key, pair.Value); }
                catch (Exception e) { Log.Warning("[IRME] Material rejected for " + pair.Key + "; original retained. " + e.Message); }
            }
        }
        private static Vector3 V3(BinaryReader r) => new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        private static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
        private static void Load(string path, List<MaterialDef> defs)
        {
            var first = defs[0];
            ModelHandle model = Api.LoadModel(first.modContentPack, first.model);
            StaticModelGeometry geometry = Api.ReadStaticModel(model);
            if (geometry == null) throw new InvalidDataException("Unsupported model");
            var slots = new Dictionary<int, MaterialDef>();
            foreach (var def in defs)
            {
                int slot = Array.IndexOf(geometry.MaterialNames, def.materialName);
                if (slot < 0 || string.IsNullOrEmpty(geometry.TexturePaths[slot]) || geometry.TexturePaths[slot].Contains("{stuff}"))
                    throw new InvalidDataException("Fixed map_Kd required for " + def.materialName);
                if (!slots.TryAdd(slot, def)) throw new InvalidDataException("Duplicate material slot");
            }
            Vector3[] normals; Vector4[] tangents;
            using (var r = new BinaryReader(File.OpenRead(path + ".irbasis")))
            {
                if (r.ReadUInt32() != 0x31424D49) throw new InvalidDataException("Expected IMB1 basis header");
                if (!r.ReadBytes(32).SequenceEqual(SHA256.HashData(File.ReadAllBytes(path)))) throw new InvalidDataException("OBJ/basis hash mismatch");
                int count = r.ReadInt32();
                if (count != geometry.Positions.Length || count % 3 != 0 || count > 65535) throw new InvalidDataException("Basis corner count mismatch");
                var raw = new Corner[count]; normals = new Vector3[count]; tangents = new Vector4[count];
                for (int i = 0; i < count; i++)
                {
                    var c = new Corner { p = V3(r), n = V3(r), uv = new Vector2(r.ReadSingle(), r.ReadSingle()), t = new Vector4(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle()) };
                    if (!Finite(c.p) || !Finite(c.n) || !float.IsFinite(c.uv.x) || !float.IsFinite(c.uv.y)) throw new InvalidDataException("Non-finite corner");
                    raw[i] = c;
                }
                if (r.BaseStream.Position != r.BaseStream.Length) throw new InvalidDataException("Trailing basis data");
                for (int i = 0; i < count; i += 3) for (int k = i; k < i + 3; k++)
                {
                    var matches = raw.Skip(i).Take(3).Where(c => (c.p - geometry.Positions[k]).sqrMagnitude < 1e-9f && (c.uv - geometry.UVs[k]).sqrMagnitude < 1e-9f).ToArray();
                    if (matches.Length != 1) throw new InvalidDataException("Corner position/UV mismatch: " + k);
                    normals[k] = matches[0].n; tangents[k] = matches[0].t;
                }
            }
            foreach (var def in defs) Normal(def);
            Api.RegisterStaticSurface(model, normals, tangents, context => slots.TryGetValue(context.Slot, out var def) ? MaterialFor(def, context.Original) : null);
            registered.Add(model); LoadedModels++;
        }
        private static Texture2D Normal(MaterialDef def)
        {
            if (textures.TryGetValue(def, out var texture)) return texture;
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, true);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(AssetPath(def.modContentPack, "Textures/" + def.normalTexture + ".png")), false)) throw new InvalidDataException("Invalid PNG");
                texture.name = "IRME_" + def.defName; texture.filterMode = FilterMode.Trilinear; texture.wrapMode = TextureWrapMode.Repeat; texture.anisoLevel = 4;
                textures.Add(def, texture); return texture;
            }
            catch { UnityEngine.Object.Destroy(texture); throw; }
        }
        private static Material MaterialFor(MaterialDef def, Material original)
        {
            MaterialRequests++;
            if (!cache.TryGetValue(def, out var variants)) cache.Add(def, variants = new Dictionary<Material, Material>());
            if (!variants.TryGetValue(original, out var material))
            {
                material = new Material(DefDatabase<ShaderDef>.GetNamed("IRME_NormalMapped").Shader);
                material.name = "IRME_" + def.defName; material.mainTexture = original.mainTexture; material.color = original.color; material.renderQueue = original.renderQueue;
                material.SetFloat("_LightMode", 1); material.SetTexture("_NormalTex", Normal(def));
                material.SetVector("_NormalParams", new Vector4(def.normalStrength, def.flipGreen ? -1 : 1, def.flipNormalV ? 1 : 0, 0));
                Api.ConfigureStaticMaterial(material, supportsFloorFade: true);
                variants.Add(original, material);
            }
            return material;
        }
    }
}
