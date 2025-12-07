using XandArt.Architecture.Events;
using XandArt.Architecture.IOC;
using XandArt.TheGrowth.StoryLine;

namespace XandArt.TheGrowth
{
    public class StoryLineManager : IShared
    {
        [Inject]
        private GameManager _gameManager;
        
        public void Init()
        {
            //EventBus.Global.Subscribe<GameLoadedEvent>(OnGameLoaded);
            EventBus.Global.Subscribe<CraftEvent>(OnEvent);
            EventBus.Global.Subscribe<EnterRoomEvent>(OnEvent);
        }

        public void Dispose()
        {
            
        }

        private void OnGameLoaded(GameLoadedEvent ev)
        {
             //ev.GameState.CurrentStoryStep
        }

        private void OnEvent<TEvent>(TEvent ev)
        {
            foreach (var component in _gameManager.CurrentGameState.CurrentStoryStep.components)
            {
                if (component is not TriggerComponent triggerComponent) continue;
                
                foreach (var trigger in triggerComponent.Triggers)
                    if (trigger is Trigger<TEvent> craftTrigger)
                        if (craftTrigger.CheckTriggerConditions(Game.BaseContext, ev))
                            triggerComponent.Execute(Game.BaseContext);
            }
        }
    }
}