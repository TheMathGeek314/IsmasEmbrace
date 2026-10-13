using System.IO;
using RandomizerCore;
using RandomizerCore.Json;
using RandomizerCore.Logic;
using RandomizerCore.LogicItems;
using RandomizerMod.RC;
using RandomizerMod.Settings;

namespace IsmasEmbrace {
    public static class LogicAdder {
        public static void Hook() {
            RCData.RuntimeLogicOverride.Subscribe(50f, ApplyLogic);
        }

        private static void ApplyLogic(GenerationSettings gs, LogicManagerBuilder lmb) {
            if(!IsmasEmbrace.globalSettings.Enabled)
                return;
            JsonLogicFormat fmt = new();
            using Stream s = typeof(LogicAdder).Assembly.GetManifestResourceStream("IsmasEmbrace.Resources.logic.json");
            lmb.DeserializeFile(LogicFileType.Locations, fmt, s);

            using Stream t = typeof(LogicAdder).Assembly.GetManifestResourceStream("IsmasEmbrace.Resources.macros.json");
            lmb.DeserializeFile(LogicFileType.Macros, fmt, t);

            DefineTermsAndItems(lmb, fmt);
        }

        private static void DefineTermsAndItems(LogicManagerBuilder lmb, JsonLogicFormat fmt) {
            lmb.AddItem(new SingleItem(Consts.AcidItem, new TermValue(lmb.GetTerm("ACID"), 1)));
            lmb.AddItem(new SingleItem(Consts.WaterItem, new TermValue(lmb.GetTerm("SWIM"), 1)));
        }
    }
}
