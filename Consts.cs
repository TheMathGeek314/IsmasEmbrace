using System.Collections.Generic;

namespace IsmasEmbrace {
    internal class Consts {
        internal const string AcidItem = "Isma's_Embrace";
        internal const string WaterItem = "Quirrel's_Embrace";
        internal static readonly Dictionary<string, string[]> ComplexAcidScenes = new() {
            { "Fungus1_Slug", [ "water_fog" ] },
            { "Fungus2_06", [ "hot_spring_layers_0032_Color-Balance-1-copy-5" ] },
            { "Fungus3_archive_02", [ "water_fog" ] },
            { "Waterways_07", [ "water_fog", "hot_spring_layers_0032_Color-Balance-1-copy-5"] }
        };
    }
}
