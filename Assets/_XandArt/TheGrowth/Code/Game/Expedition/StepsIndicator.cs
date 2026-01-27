using TMPro;
using UnityEngine;
using XandArt.Architecture.Events;
using XandArt.Architecture.IOC;
using XandArt.TheGrowth.StoryLine;

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
            _manager.OnStepUpdate += UpdateSteps;
            EventBus.Global.Subscribe<ExpeditionStartEvent>(OnEvent);
        }
        
        private void OnDestroy()
        {
            EventBus.Global.Unsubscribe<ExpeditionStartEvent>(OnEvent);
        }
        
        private void UpdateSteps()
        {
            label.SetText(_manager.Steps.ToString());
        }

        private void OnEvent(ExpeditionStartEvent @event)
        {
            UpdateSteps();
        }
    }
}