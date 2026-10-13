using System;
using System.Reflection;
using MonoMod.RuntimeDetour;
using CondensedSpoilerLogger;
using ItemChanger;

namespace IsmasEmbrace {
    internal static class CslInterop {
        private static MethodInfo ApplyMerges = typeof(SpoilerReader).GetMethod("ApplyMerges", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo MergeItems = typeof(SpoilerReader).GetMethod("MergeItems", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void Hook() {
            new Hook(ApplyMerges, (Action<SpoilerReader> orig, SpoilerReader self) => {
                orig(self);
                MergeItems.Invoke(self, [ItemNames.Ismas_Tear, new string[] { Consts.AcidItem }]);
                MergeItems.Invoke(self, [ItemNames.Swim, new string[] { Consts.WaterItem }]);
            });
        }
    }
}
