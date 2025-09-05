using UnityEngine;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public class ExpeditionManager : IShared
    {
        private EntityBoard _board;
        
        public void Init()
        {
            Debug.Log($"TEST - ExpeditionManager.Init");
        }

        public void Dispose()
        {
        }

        public void SetBoard(EntityBoard board)
        {
            _board = board;

            if (_board != null)
            {
                // Init board
                Debug.Log($"TEST: Add Board");
            }
            else
            {
                // Dispose board
                Debug.Log($"TEST: Remove Board");
            }
        }
    }
}