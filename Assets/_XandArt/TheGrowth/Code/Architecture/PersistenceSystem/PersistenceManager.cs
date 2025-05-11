using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using XandArt.Architecture.IOC;

namespace XandArt.Architecture
{
    public class PersistenceManager : IShared
    {
        private static readonly JsonSerializerSettings SerializationSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.All,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
            // ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            Converters =
            {
                new AssetRefConverter(),
                new RefConverter(),
                new VectorJsonConverter()
            }
        };

        private const string SaveFolder = "save";
        private string SaveFolderPath;
        
        public void Init()
        {
            SaveFolderPath = Path.GetFullPath(Path.Combine(Application.persistentDataPath, SaveFolder));
            
            if (!Directory.Exists(SaveFolderPath))
                Directory.CreateDirectory(SaveFolderPath);
        }

        public void Dispose()
        {
            
        }
        
        public T Load<T>(string saveId) where T : PersistentState
        {
            var path = GetPathToSave(saveId);
            
            Debug.Log($"Load game from {path}");
            
            var json = File.ReadAllText(path);
            var world = JsonConvert.DeserializeObject<T>(json, SerializationSettings);
            world.OnPostLoad();
            return world;
        }
        
        public void Save<T>(string saveId, T persistentState) where T : PersistentState
        {
            var path = GetPathToSave(saveId);
            
            Debug.Log($"Save game to {path}");
            
            persistentState.OnPreSave();
            var json = JsonConvert.SerializeObject(persistentState, SerializationSettings);
            File.WriteAllText(path, json);
        }

        private string GetPathToSave(string saveId)
        {
            return Path.Combine(SaveFolderPath, $"{saveId}.json");
        }
    }
}