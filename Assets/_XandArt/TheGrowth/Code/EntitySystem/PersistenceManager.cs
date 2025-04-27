using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace XandArt.TheGrowth
{
    public class PersistenceManager
    {
        private static readonly JsonSerializerSettings SerializationSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.All,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
            // ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            Converters =
            {
                new RefConverter()
            }
        };

        private const string SaveFolder = "save";

        public PersistentState Load(string saveId)
        {
            var path = GetPathToSave(saveId);
            
            Debug.Log($"Load game from {path}");
            
            var json = File.ReadAllText(path);
            var world = JsonConvert.DeserializeObject<PersistentState>(json, SerializationSettings);
            world.OnPostLoad();
            return world;
        }
        
        public void Save(string saveId, PersistentState persistentState)
        {
            var path = GetPathToSave(saveId);
            
            Debug.Log($"Save game to {path}");
            
            persistentState.OnPreSave();
            var json = JsonConvert.SerializeObject(persistentState, SerializationSettings);
            File.WriteAllText(path, json);
        }

        private string GetPathToSave(string saveId)
        {
            var folder = Path.GetFullPath(Path.Combine(Application.persistentDataPath, SaveFolder));

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, saveId);
        }
    }
}