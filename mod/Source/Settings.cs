using UnityEngine;
using Verse;

namespace RimZrism.Materials
{
    public sealed class LodOptions : ModSettings
    {
        public bool lowerFloorsUseLowest = true;
        internal static LodOptions Current;
        internal static bool LowerFloorsUseLowest => Current == null || Current.lowerFloorsUseLowest;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref lowerFloorsUseLowest, "lowerFloorsUseLowest", true);
        }
    }
    public sealed class MaterialLodMod : Mod
    {
        public MaterialLodMod(ModContentPack content) : base(content) => LodOptions.Current = GetSettings<LodOptions>();
        public override string SettingsCategory() => "IsoRim Material & LOD";
        public override void DoSettingsWindowContents(Rect rect)
        {
            var list = new Listing_Standard(); list.Begin(rect);
            list.CheckboxLabeled("可见下层使用最远 LOD / Lowest LOD on visible lower floors",
                ref LodOptions.Current.lowerFloorsUseLowest,
                "当前查看层仍按屏幕大小分级；关闭后下层也按屏幕大小分级。隐藏层始终暂停 LOD。\nThe viewed floor uses screen-size LOD. Disable to use it on lower floors too. Hidden floors always suspend LOD.");
            list.End();
        }
    }
}
