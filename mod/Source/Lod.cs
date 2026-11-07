using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using IsoRim;
using UnityEngine;
using Verse;

namespace RimZrism.Materials
{
    /// <summary>An aligned alternate OBJ and its nominal screen-height threshold .</summary>
    public sealed class LodLevel
    {
        public string model;
        public float belowScreenHeight;
    }

    /// <summary>
    /// Opt-in two/three-level LOD for immutable static building models .
    /// Independent of gameplay state models; matches an OBJ after the state model has been chosen.
    /// No automatic simplification, texture eviction, cross-fade, or dynamic-model support.
    /// </summary>
    public sealed class LodDef : Def
    {
        public string model;
        public List<LodLevel> levels;
        public float hysteresis = .15f;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors()) yield return error;
            if (string.IsNullOrWhiteSpace(model)) yield return "LOD0 model path is required";
            if (levels == null || levels.Count < 1 || levels.Count > 2)
            {
                yield return "Supply one or two alternate levels (two or three total LODs)";
                yield break;
            }
            float previous = 1f;
            foreach (LodLevel level in levels)
            {
                if (level == null || string.IsNullOrWhiteSpace(level.model))
                {
                    yield return "Alternate model path is required";
                    continue;
                }
                if (!float.IsFinite(level.belowScreenHeight) || level.belowScreenHeight <= 0f || level.belowScreenHeight >= previous)
                    yield return "Thresholds must be finite, descending and in (0,1)";
                previous = level.belowScreenHeight;
            }
            if (!float.IsFinite(hysteresis) || hysteresis < 0f || hysteresis > .3f)
                yield return "hysteresis must be finite and in [0,0.3]";
        }
    }

    internal sealed class LodChain
    {
        internal LodDef def;
        internal ModelHandle[] models;
        internal Vector3 min, max;

        internal int Choose(float height, int current)
        {
            if (!float.IsFinite(height) || height <= 0f) return 0;
            if (current < 0 || current >= models.Length)
            {
                int initial = 0;
                while (initial < models.Length - 1 && height < def.levels[initial].belowScreenHeight) initial++;
                return initial;
            }
            int next = current;
            while (next < models.Length - 1 && height < def.levels[next].belowScreenHeight * (1f - def.hysteresis)) next++;
            while (next > 0 && height > def.levels[next - 1].belowScreenHeight * (1f + def.hysteresis)) next--;
            return next;
        }
    }

    internal static class ModelLods
    {
        private static readonly Dictionary<ModelHandle, LodChain> chains = new Dictionary<ModelHandle, LodChain>();
        private static readonly Dictionary<string, ModelHandle> alternatives = new Dictionary<string, ModelHandle>(StringComparer.OrdinalIgnoreCase);
        [ThreadStatic] private static bool loading;
        internal static int ChainsLoaded;

        private static void Bounds(ModelHandle model, out Vector3 min, out Vector3 max) { min = model.Min; max = model.Max; }
        internal static void Initialize()
        {
            foreach (var def in DefDatabase<LodDef>.AllDefsListForReading)
            {
                try {
                    string path = Materials.AssetPath(def.modContentPack, def.model);
                    var model = Api.LoadModel(def.modContentPack, def.model);
                    if (model == null) throw new InvalidDataException("Missing LOD0");
                    if (!chains.ContainsKey(model)) Loaded(path, model);
                } catch (Exception e) { Log.Warning("[IRME] LOD rejected: " + e.Message); }
            }
            // Explicit construction avoids a compiler-generated static delegate field. The game
            // scans all static field types even when an unsupported host has disabled this addon.
            Api.AddStaticModelSelector(new Func<StaticModelContext, ModelHandle>(Select));
        }
        internal static int Corners(ModelHandle model) => Api.ReadStaticModel(model)?.Positions.Length ?? 0;

        internal static void Loaded(string path, ModelHandle model)
        {
            if (loading) return;
            var defs = DefDatabase<LodDef>.AllDefsListForReading.Where(d => Materials.Matches(d.modContentPack, d.model, path)).ToArray();
            if (defs.Length == 0) return;
            try
            {
                if (defs.Length != 1) throw new InvalidDataException("Multiple LodDefs target the same OBJ");
                LodDef def = defs[0];
                string[] errors = def.ConfigErrors().ToArray();
                if (errors.Length != 0) throw new InvalidDataException(string.Join("; ", errors));
                var chain = new LodChain { def = def, models = new ModelHandle[def.levels.Count + 1] };
                chain.models[0] = model;
                Bounds(model, out chain.min, out chain.max);
                var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path.GetFullPath(path) };
                loading = true;
                // Validate the base too: a rejected normal material must not silently become accepted at a distant LOD.
                ValidateModel(path, model);
                for (int i = 1; i < chain.models.Length; i++)
                {
                    string alternate = Materials.AssetPath(def.modContentPack, def.levels[i - 1].model);
                    if (!paths.Add(alternate)) throw new InvalidDataException("Repeated LOD path");
                    if (!alternatives.TryGetValue(alternate, out ModelHandle next))
                    {
                        next = Api.LoadModel(def.modContentPack, def.levels[i - 1].model);
                        if (next == null) throw new InvalidDataException("Cannot load " + alternate);
                        alternatives.Add(alternate, next);
                    }
                    ValidateModel(alternate, next);
                    if (Corners(next) > Corners(chain.models[i - 1]))
                        throw new InvalidDataException("Triangle count must not increase towards distant LODs");
                    Bounds(next, out Vector3 min, out Vector3 max);
                    float tolerance = Mathf.Max(.01f, (chain.max - chain.min).magnitude * .03f);
                    if ((min - chain.min).magnitude > tolerance || (max - chain.max).magnitude > tolerance)
                        throw new InvalidDataException("LOD bounds differ by more than 3%; export aligned models");
                    chain.models[i] = next;
                }
                chains.Add(model, chain);
                ChainsLoaded++;
            }
            catch (Exception e) { Log.Warning("[IRME] LOD chain rejected for " + path + "; original model retained. " + e.Message); }
            finally { loading = false; }
        }

        private static void ValidateModel(string path, ModelHandle model)
        {
            if (Api.ReadStaticModel(model) == null) throw new InvalidDataException("Only ordinary opaque static meshes are supported");
            if (DefDatabase<MaterialDef>.AllDefsListForReading.Any(d => Materials.Matches(d.modContentPack, d.model, path))
                && !Materials.HasBasis(model)) throw new InvalidDataException("Configured normal material failed validation on " + path);
        }

        private static ModelHandle Select(StaticModelContext context)
        {
            if (!chains.TryGetValue(context.Model, out LodChain chain)) return null;
            LodState state = context.Thing.Map.GetComponent<ModelLodMap>().Register(context.Thing, context.Model, chain);
            int index = 0;
            for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) for (int z = 0; z < 2; z++)
                state.corners[index++] = context.LocalToMap(new Vector3(x == 0 ? chain.min.x : chain.max.x,
                    y == 0 ? chain.min.y : chain.max.y, z == 0 ? chain.min.z : chain.max.z));
            state.group.Read(context.Thing.Map);
            if (state.group.visible)
            {
                bool fresh = state.generation != state.group.generation;
                state.level = state.Choose(fresh);
                state.generation = state.group.generation;
            }
            // Hidden prints can still be requested by the game. Keep an existing mesh level; an unseen first print
            // starts at the cheapest level without a projection. Visibility restoration is scheduled by the addon.
            else if (state.level < 0) state.level = chain.models.Length - 1;
            state.rendered = state.level;
            return chain.models[state.level];
        }
    }

    // Plain fields intentionally avoid pending host API types: original hosts must be able to scan this assembly
    // before Bootstrap rejects them. API calls only execute beyond the non-inlined compatibility gate.
    internal sealed class LodRow
    {
        internal int row, cursor, generation;
        internal bool visible, coarse, coarseSettled;
        internal Vector3 offset;
        internal readonly List<LodState> states = new List<LodState>();
        internal void Read(Map map)
        {
            var view = Api.ReadStaticModelView(map, row);
            bool lower = LodOptions.LowerFloorsUseLowest && view.Floor >= 0 && view.Floor < view.ViewedFloor;
            if (visible != view.Visible || coarse != lower || offset != view.Offset) { generation++; coarseSettled = false; }
            visible = view.Visible; coarse = lower; offset = view.Offset;
        }
    }

    internal sealed class LodState
    {
        internal Thing thing;
        internal ModelHandle original;
        internal LodChain chain;
        internal LodRow group;
        internal readonly Vector3[] corners = new Vector3[8]; // raw map space; never cache the mutable floor offset
        internal int level = -1, rendered = -1, generation = -1, projections;
        internal float height, nextCheck;
        internal int Choose(bool fresh)
        {
            if (group.coarse) return chain.models.Length - 1;
            height = ScreenHeight();
            return chain.Choose(height, fresh ? -1 : level);
        }
        internal float ScreenHeight()
        {
            projections++;
            float min = float.MaxValue, max = float.MinValue;
            foreach (Vector3 p in corners)
            {
                Vector2 projected = Api.WorldToViewport(p + group.offset);
                if (!float.IsFinite(projected.y)) return 1f;
                min = Mathf.Min(min, projected.y); max = Mathf.Max(max, projected.y);
            }
            return max - min;
        }
    }

    public sealed class ModelLodMap : MapComponent
    {
        private readonly Dictionary<(Thing, ModelHandle), LodState> byThing = new Dictionary<(Thing, ModelHandle), LodState>();
        private readonly List<LodState> states = new List<LodState>();
        private readonly Dictionary<int, LodRow> byRow = new Dictionary<int, LodRow>();
        private readonly List<LodRow> rows = new List<LodRow>();
        private readonly List<LodRow> visible = new List<LodRow>();
        private readonly HashSet<Section> dirtied = new HashSet<Section>();
        private int cursor, pruneCursor;
        private float nextPrune;
        internal int Switches, DirtyRequests, MaxSectionsPerFrame, Checks, RowQueries, HiddenRowsSkipped;

        public ModelLodMap(Map map) : base(map) { }
        internal LodState StateFor(Thing thing) => states.FirstOrDefault(s => s.thing == thing);
        private void Remove(LodState state)
        {
            byThing.Remove((state.thing, state.original));
            states.Remove(state); state.group.states.Remove(state);
            if (state.group.states.Count == 0)
            {
                byRow.Remove(state.group.row); rows.Remove(state.group);
            }
        }
        internal LodState Register(Thing thing, ModelHandle original, LodChain chain)
        {
            if (byThing.TryGetValue((thing, original), out LodState state) && state.group.row == thing.Position.z) return state;
            for (int i = states.Count - 1; i >= 0; i--) if (states[i].thing == thing) Remove(states[i]);
            if (!byRow.TryGetValue(thing.Position.z, out LodRow group))
            {
                group = new LodRow { row = thing.Position.z };
                byRow.Add(group.row, group); rows.Add(group);
            }
            state = new LodState { thing = thing, original = original, chain = chain, group = group };
            byThing.Add((thing, original), state); states.Add(state); group.states.Add(state); group.coarseSettled = false;
            return state;
        }
        private bool Gone(LodState state) => state.thing.Destroyed || !state.thing.Spawned || state.thing.Map != map;
        public override void MapComponentUpdate()
        {
            if (!Bootstrap.Enabled) return;
            UpdateEnabled();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void UpdateEnabled()
        {
            if (map != Find.CurrentMap || !Api.StaticModelExtensionsActive || states.Count == 0) return;
            float now = Time.realtimeSinceStartup;
            // Bounded, infrequent lifecycle maintenance also prunes objects destroyed on hidden floors.
            // This is not an LOD projection or a section rebuild.
            if (now >= nextPrune)
            {
                nextPrune = now + 1f;
                for (int i = 0; i < 128 && states.Count > 0; i++)
                {
                    if (pruneCursor >= states.Count) pruneCursor = 0;
                    var s = states[pruneCursor];
                    if (Gone(s)) Remove(s); else pruneCursor++;
                }
            }
            visible.Clear();
            foreach (var group in rows)
            {
                group.Read(map); RowQueries++;
                if (group.visible) { if (!group.coarse || !group.coarseSettled) visible.Add(group); }
                else HiddenRowsSkipped++;
            }
            if (visible.Count == 0) return;
            dirtied.Clear();
            int budget = Math.Min(128, states.Count);
            for (int i = 0; i < budget && visible.Count > 0; i++)
            {
                if (cursor >= visible.Count) cursor = 0;
                LodRow group = visible[cursor++];
                if (group.states.Count == 0) continue;
                if (group.cursor >= group.states.Count) group.cursor = 0;
                LodState state = group.states[group.cursor++];
                if (Gone(state)) { Remove(state); continue; }
                // A relocated static building is re-registered at its next normal print. Do not use stale corners.
                if (state.thing.Position.z != group.row) continue;
                bool fresh = state.generation != group.generation;
                if (!fresh && now < state.nextCheck) continue;
                // A lower floor already at its requested coarse level needs no per-building projections.
                if (group.coarse && state.level == state.chain.models.Length - 1)
                {
                    state.generation = group.generation; state.nextCheck = now + .25f; continue;
                }
                int next = state.Choose(fresh); Checks++;
                if (next == state.level)
                {
                    state.generation = group.generation; state.nextCheck = now + .25f; continue;
                }
                Section section = map.mapDrawer.SectionAt(state.thing.Position);
                if (section == null || (!dirtied.Contains(section) && dirtied.Count >= 2)) continue;
                state.level = next; state.generation = group.generation; state.nextCheck = now + .25f;
                Switches++;
                if (dirtied.Add(section))
                {
                    Api.InvalidateStaticModel(state.thing); DirtyRequests++;
                }
            }
            MaxSectionsPerFrame = Math.Max(MaxSectionsPerFrame, dirtied.Count);
            // Once a visible lower row has reached its fixed lowest LOD, skip the entire row until its view/policy
            // changes or a new object registers. Do not keep visiting each building every frame just to say 'no change'.
            foreach (var group in visible)
            {
                if (!group.coarse) continue;
                bool settled = true;
                foreach (var state in group.states)
                    if (state.level != state.chain.models.Length - 1 || state.generation != group.generation) { settled = false; break; }
                group.coarseSettled = settled;
            }
        }
    }
}
