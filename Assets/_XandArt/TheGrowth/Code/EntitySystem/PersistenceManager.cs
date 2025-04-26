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
            TypeNameHandling = TypeNameHandling.All
        };

        private const string SaveFolder = "save";

        public World Load(string saveId)
        {
            var file = Path.Combine(Application.persistentDataPath, SaveFolder, saveId);
            
            var json = File.ReadAllText(file);
            var world = JsonConvert.DeserializeObject<World>(json, SerializationSettings);
            world.OnPostLoad();
            return world;
        }
        
        public void Save(string saveId, World world)
        {
            var file = Path.Combine(Application.persistentDataPath, SaveFolder, saveId);
            
            world.OnPreSave();
            var json = JsonConvert.SerializeObject(world, SerializationSettings);
            File.WriteAllText(file, json);
        }
    }
}