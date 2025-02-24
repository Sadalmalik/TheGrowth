using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sadalmalik.TheGrowth
{
    public class Game
    {
        public World World;
        
        public void NewGame()
        {
            World = new World();
        }
    }
    public class World
    {
        public List<Entity> Entities { get; private set; } = new List<Entity>();
    }
    
    public class Entity
    {
        [JsonIgnore]
        public EntityView View;
    }
}