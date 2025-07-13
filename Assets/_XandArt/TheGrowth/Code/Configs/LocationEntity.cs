using Newtonsoft.Json;
using XandArt.Architecture;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public class LocationEntity : Entity
    {
        [JsonIgnore]
        public LocationModel Model => (LocationModel) _model;

        [JsonIgnore]
        [Inject]
        private LocationManager _locationManager;
        
        [JsonIgnore]
        [Inject]
        private MenuManager _menuManager;

        public void Load()
        {
            _locationManager.LoadLocation(this);
        }

        public void Unload()
        {
            _locationManager.UnloadLocation(this);
        }
    }
}