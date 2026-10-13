using Modding;
using System.IO;
using ItemChanger;
using ItemChanger.Locations;
using ItemChanger.Tags;
using RandomizerMod.Logging;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;

namespace IsmasEmbrace {
    internal static class RandoInterop {
        public static void Hook() {
            RandoMenuPage.Hook();
            RequestModifier.Hook();
            LogicAdder.Hook();

            DefineLocations();
            DefineItems();

            RandoController.OnExportCompleted += EditModules;
            SettingsLog.AfterLogSettings += LogRandoSettings;

            if(ModHooks.GetMod("ConnectionSettingsRando") is Mod)
                CsrInterop.Hook();

            if(ModHooks.GetMod("CondensedSpoilerLogger") is Mod)
                CslInterop.Hook();

            if(ModHooks.GetMod("RandoSettingsManager") is Mod)
                RsmInterop.Hook();
        }

        private static void DefineLocations() {
            using Stream s = typeof(RandoInterop).Assembly.GetManifestResourceStream("IsmasEmbrace.Resources.data.json");
            foreach(EmbraceData data in new ParseJson(s).ParseFile<EmbraceData>()) {
                data.translate();
                CoordinateLocation eLoc = new() {
                    name = data.name,
                    sceneName = data.scene,
                    x = data.x,
                    y = data.y,
                    elevation = 0,
                    managed = false,
                    forceShiny = true,
                    flingType = FlingType.DirectDeposit
                };
                InteropTag tag = AddTag(eLoc);
                tag.Properties["PinSprite"] = new EmbraceSprite("Bubbles");
                tag.Properties["WorldMapLocation"] = (data.scene, data.x, data.y);
                Finder.DefineCustomLocation(eLoc);
            }
        }

        private static void DefineItems() {
            Finder.DefineCustomItem(new IsmasEmbraceItem());
            Finder.DefineCustomItem(new QuirrelsEmbraceItem());
        }

        private static void EditModules(RandoController controller) {
            if(IsmasEmbrace.globalSettings.Enabled) {
                AcidDivingModule adm = ItemChangerMod.Modules.GetOrAdd<AcidDivingModule>();
                adm.hasAcidDive = false;
                adm.hasWaterDive = false;
            }
        }

        private static void LogRandoSettings(LogArguments args, TextWriter w) {
            w.WriteLine("Logging Isma's Embrace settings:");
            w.WriteLine(JsonUtil.Serialize(IsmasEmbrace.globalSettings));
        }

        internal static InteropTag AddTag(TaggableObject obj) {
            InteropTag tag = obj.GetOrAddTag<InteropTag>();
            tag.Message = "RandoSupplementalMetadata";
            tag.Properties["ModSource"] = IsmasEmbrace.instance.GetName();
            return tag;
        }
    }
}
