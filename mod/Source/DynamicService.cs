using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using IsoRim;
using UnityEngine;
using Verse;

namespace RimZrism.Materials
{
    /// <summary>Optional, main-thread dynamic material/LOD service. No vehicle framework dependency.</summary>
    public static class DynamicService
    {
        public static int Version => 1;
        public static bool Available => Bootstrap.Enabled;
        private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<Material> live = new HashSet<Material>();
        private static readonly List<Retired> retired = new List<Retired>();
        private static readonly HashSet<string> warnings = new HashSet<string>();
        private static Game session;
        private sealed class Retired { internal UnityEngine.Object value; internal int frame; }
        public static int LiveMaterials => live.Count;
        public static int CachedTextures => textures.Count;
        public static int PendingDestruction => retired.Count;

        private static void Warn(string key, Exception e)
        {
            if (warnings.Count < 64 && warnings.Add(key))
                Log.Warning("[IRME] Dynamic enhancement unavailable; keep base 3D material. " + key + ": " + e.Message);
        }

        public static Material CreateMaterial(Material source, string normalPng, string aoPng,
            float normalStrength, bool flipGreen, bool flipV, bool continuousLight, float aoStrength)
        {
            if (!Available || !UnityData.IsInMainThread || source == null) return null;
            Collect();
            Material material = null;
            try
            {
                if (!float.IsFinite(normalStrength) || normalStrength < 0 || normalStrength > 4 ||
                    !float.IsFinite(aoStrength) || aoStrength < 0 || aoStrength > 1)
                    throw new ArgumentException("Normal strength must be 0..4 and AO strength 0..1");
                var shader = DefDatabase<ShaderDef>.GetNamedSilentFail("IRME_DynamicNormalMapped")?.Shader;
                if (shader == null) return null;
                material = new Material(source) { name = source.name + "_IRME", shader = shader };
                // Configure BEFORE loading files: during startup the host can legitimately be unready.
                if (!Api.ConfigureFigureMaterial(material, FigureMaterialKind.Textured))
                {
                    UnityEngine.Object.Destroy(material);
                    return null;
                }
                material.SetTexture("_NormalTex", LoadLinear(normalPng));
                material.SetTexture("_ReliefAOTex", string.IsNullOrEmpty(aoPng) ? Texture2D.whiteTexture : LoadLinear(aoPng));
                material.SetVector("_NormalParams", new Vector4(normalStrength, flipGreen ? -1 : 1, flipV ? 1 : 0, continuousLight ? 1 : 0));
                material.SetVector("_ReliefAOParams", new Vector4(aoStrength, 0, 0, 0));
                live.Add(material);
                return material;
            }
            catch (Exception e)
            {
                if (material != null)
                {
                    Api.ReleaseFigureMaterial(material);
                    Retire(material);
                }
                Warn(normalPng ?? "missing normal path", e);
                return null;
            }
        }

        private static Texture2D LoadLinear(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
                throw new ArgumentException("An absolute PNG path is required");
            path = Path.GetFullPath(path);
            if (!string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("PNG texture required");
            if (textures.TryGetValue(path, out var cached)) return cached;
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length < 24 || bytes[0] != 137 || bytes[1] != 80 || bytes[2] != 78 || bytes[3] != 71)
                throw new InvalidDataException("Invalid PNG signature");
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, true);
            try
            {
                if (!ImageConversion.LoadImage(texture, bytes, false)) throw new InvalidDataException("PNG decode failed");
                texture.name = "IRME_dynamic_" + Path.GetFileName(path);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Trilinear;
                texture.anisoLevel = 4;
                textures.Add(path, texture);
                return texture;
            }
            catch { UnityEngine.Object.Destroy(texture); throw; }
        }

        public static void ReleaseMaterial(Material material)
        {
            if (!UnityData.IsInMainThread || ReferenceEquals(material, null) || !live.Remove(material)) return;
            // Host retires its copies; this service separately owns the original.
            Api.ReleaseFigureMaterial(material);
            Retire(material);
        }

        private static void Retire(UnityEngine.Object value) => retired.Add(new Retired { value = value, frame = Time.frameCount });

        public static void Collect()
        {
            if (!UnityData.IsInMainThread) return;
            if (!ReferenceEquals(session, Current.Game))
            {
                foreach (var m in new List<Material>(live)) ReleaseMaterial(m);
                foreach (var t in textures.Values) Retire(t);
                textures.Clear(); warnings.Clear();
                session = Current.Game;
            }
            for (int i = retired.Count - 1; i >= 0; --i)
            {
                if (Time.frameCount - retired[i].frame < 3) continue;
                if (retired[i].value != null) UnityEngine.Object.Destroy(retired[i].value);
                retired.RemoveAt(i);
            }
        }

        public static int SelectLod(float projectedHeight, int current, bool initialized,
            int maxLod, float nearFraction, float farFraction, float hysteresis)
        {
            if (!Available) return 0;
            return SelectPolicy(projectedHeight, current, initialized, maxLod, nearFraction, farFraction, hysteresis);
        }

        internal static int SelectPolicy(float height, int current, bool initialized, int maxLod, float near, float far, float band)
        {
            if (!float.IsFinite(height) || height < 0 || maxLod < 0 || maxLod > 2 ||
                !float.IsFinite(near) || !float.IsFinite(far) || near <= far || far <= 0 ||
                !float.IsFinite(band) || band < 0 || band >= .5f) return 0;
            if (!initialized) return Math.Min(maxLod, height < far ? 2 : height < near ? 1 : 0);
            int n = Math.Clamp(current, 0, maxLod);
            while (n < maxLod && height < (n == 0 ? near : far) * (1 - band)) n++;
            while (n > 0 && height > (n == 1 ? near : far) * (1 + band)) n--;
            return n;
        }
    }

    public sealed class DynamicServiceLifetime : GameComponent
    {
        public DynamicServiceLifetime(Game game) { }
        public override void GameComponentUpdate() { if (Bootstrap.Enabled) DynamicService.Collect(); }
    }
}
