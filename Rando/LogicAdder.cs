using System.IO;
using System.Reflection;
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
            Assembly assembly = typeof(LogicAdder).Assembly;
            using Stream s = assembly.GetManifestResourceStream("IsmasEmbrace.Resources.logic.json");
            lmb.DeserializeFile(LogicFileType.Locations, fmt, s);

            using Stream t = assembly.GetManifestResourceStream("IsmasEmbrace.Resources.macros.json");
            lmb.DeserializeFile(LogicFileType.Macros, fmt, t);

            using Stream r = assembly.GetManifestResourceStream("IsmasEmbrace.Resources.logicSubstitutions.json");
            lmb.DeserializeFile(LogicFileType.LogicSubst, fmt, r);

            DefineTermsAndItems(lmb, fmt);
        }

        private static void DefineTermsAndItems(LogicManagerBuilder lmb, JsonLogicFormat fmt) {
            lmb.AddItem(new SingleItem(Consts.AcidItem, new TermValue(lmb.GetTerm("ACID"), 1)));
            lmb.AddItem(new SingleItem(Consts.WaterItem, new TermValue(lmb.GetTerm("SWIM"), 1)));
        }
    }
}
