using System;
using System.Collections.Generic;
using System.Linq;
using FStats;
using FStats.StatControllers;
using FStats.Util;

namespace IsmasEmbrace {
    public static class FsInterop {
        public static void Hook() {
            API.OnGenerateFile += GenerateStats;
        }

        private static void GenerateStats(Action<StatController> generateStats) {
            if(!IsmasEmbrace.globalSettings.Enabled)
                return;
            generateStats(new EmbraceStats());
        }
    }

    public class EmbraceStats: StatController {
        public override void Initialize() {
            AcidDivingModule.OnItemObtained += AddEntry;
        }

        public override void Unload() {
            AcidDivingModule.OnItemObtained -= AddEntry;
        }

        public record KeyItem(string item, float timestamp);
        public List<KeyItem> KeyItems = [];
        public List<string> UsedKeys = [];

        public void AddEntry(string entry) {
            if(!UsedKeys.Contains(entry)) {
                KeyItem key = new(entry, FStatsMod.LS.Get<Common>().CountedTime);
                KeyItems.Add(key);
                UsedKeys.Add(entry);
            }
        }

        public override IEnumerable<DisplayInfo> GetDisplayInfos() {
            List<string> rows = KeyItems.OrderBy(x => x.timestamp).Select(x => $"{x.item}: {x.timestamp.PlaytimeHHMMSS()}").ToList();
            yield return new() {
                Title = "Isma's Embrace",
                MainStat = "",
                StatColumns = [string.Join("\n", rows)],
                Priority = BuiltinScreenPriorityValues.ExtensionStats
            };
        }
    }
}
