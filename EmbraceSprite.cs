using System;
using Newtonsoft.Json;
using UnityEngine;
using ItemChanger;
using ItemChanger.Internal;

namespace IsmasEmbrace {
    [Serializable]
    public class EmbraceSprite: ISprite {
        private static SpriteManager EmbeddedSpriteManager = new(typeof(EmbraceSprite).Assembly, "IsmasEmbrace.Resources.");

        public string key;
        public EmbraceSprite(string key) {
            this.key = key;
        }

        [JsonIgnore]
        public Sprite Value => EmbeddedSpriteManager.GetSprite(key);
        public ISprite Clone() => (ISprite)MemberwiseClone();
    }
}
