using UnityEngine;

namespace XandArt.TheGrowth.Common
{
    public class GoToHubRoom : Command
    {
        public string RoomName;
        
        public override void Execute(Context context)
        {
            var hub = Object.FindObjectsByType<HUB>(sortMode: FindObjectsSortMode.None);
            
            hub[0].GoToRoom(RoomName);
        }
    }
}