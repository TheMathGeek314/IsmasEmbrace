using Modding;
using System.Collections.Generic;
using UnityEngine;

namespace IsmasEmbrace {
    public class IsmasEmbrace: Mod, IGlobalSettings<GlobalSettings> {
        new public string GetName() => "Isma's Embrace";
        public override string GetVersion() => "1.0.0.0";

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

// TODO
//  place locations
//  define logic
//  are they always 100% consistently named? Can I disregard object name and only account for possible parentage of dupe named items

// UP NEXT
//  Fungal is done, resume with Queen's Gardens
