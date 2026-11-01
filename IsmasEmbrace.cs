using Modding;
using System.Collections.Generic;
using UnityEngine;

namespace IsmasEmbrace {
    public class IsmasEmbrace: Mod, IGlobalSettings<GlobalSettings> {
        new public string GetName() => "Isma's Embrace";
        public override string GetVersion() => "1.0.1.0";

        public static GlobalSettings globalSettings { get; set; } = new();
        public void OnLoadGlobal(GlobalSettings s) => globalSettings = s;
        public GlobalSettings OnSaveGlobal() => globalSettings;

        internal static IsmasEmbrace instance;

        public IsmasEmbrace(): base(null) {
            instance = this;
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects) {
            RandoInterop.Hook();
        }
    }
}
