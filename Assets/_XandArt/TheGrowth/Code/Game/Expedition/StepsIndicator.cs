using TMPro;
using UnityEngine;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public class StepsIndicator : WidgetBase
    {
        [SerializeField]
        private TMP_Text label;
        
        [Inject]
        private ExpeditionManager _manager;

        public override void Init()
        {
            Debug.Log($"[TEST] StepsIndicator.Init");
            _manager.OnStepUpdate += UpdateSteps;
        }
        
        private void UpdateSteps()
        {
            Debug.Log($"[TEST] StepsIndicator.UpdateSteps: {_manager.Steps}");
            label.SetText(_manager.Steps.ToString());
        }
    }
}