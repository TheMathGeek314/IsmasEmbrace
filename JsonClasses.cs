using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace IsmasEmbrace {
    public class EmbraceData {
        public static List<EmbraceData> AllLocations = new();

        public string name;
        public string scene;
        public float x;
        public float y;

        public void translate() {
            AllLocations.Add(this);
        }
    }

    public class SwimRoom {
        public static Dictionary<string, SwimRoom> ByScene = new();

        public string scene;
        public string type;
        public string[] renderers;
        public CameraLockJson[] zones;
        public ColliderJson[] boxes;
        public AcidEdit[] acidEdits;
        public string[] parentObjs;

        public void translate() {
            ByScene[scene] = this;
        }
    }

    public class CameraLockJson {
        public string AreaToClone;
        public float PosX;
        public float PosY;
        public float ScaleX;
        public float ScaleY;
        public float MinX;
        public float MaxX;
        public float MinY;
        public float MaxY;
    }

    public class ColliderJson {
        public float X;
        public float Y;
        public float SizeX;
        public float SizeY;
    }

    public class AcidEdit {
        public string name;
        public float position;
        public float size;
    }

    internal class ParseJson {
        private readonly string _jsonFilePath;
        private readonly Stream _jsonStream;
        private bool isPath;

        public ParseJson(string jsonFilePath) {
            _jsonFilePath = jsonFilePath;
            isPath = true;
        }

        public ParseJson(Stream jsonStream) {
            _jsonStream = jsonStream;
            isPath = false;
        }

        public List<T> ParseFile<T>() {
            if(isPath) {
                using StreamReader reader = new(_jsonFilePath);
                var json = reader.ReadToEnd();
                List<T> values = JsonConvert.DeserializeObject<List<T>>(json);
                return values;
            }
            else {
                using StreamReader reader = new(_jsonStream);
                var json = reader.ReadToEnd();
                List<T> values = JsonConvert.DeserializeObject<List<T>>(json);
                return values;
            }
        }
    }
}
