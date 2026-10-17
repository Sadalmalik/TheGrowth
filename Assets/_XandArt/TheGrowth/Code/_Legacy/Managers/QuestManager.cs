using System.Linq;
using XandArt.Architecture;
using XandArt.Architecture.IOC;
using XandArt.TheGrowth.Quests;

namespace XandArt.TheGrowth
{
    public class QuestManager : SharedObject, ITickable
    {
        [Inject]
        private GameManager _gameManager;

        public void Tick()
        {
            if (_gameManager == null) return;
            if (_gameManager.CurrentGameState == null) return;

            var gameState = _gameManager.CurrentGameState;
            var quests = gameState.Entities
                .OfType<CompositeEntity>()
                .Where(entity => entity.Model.GetComponent<QuestInfo>() != null)
                .ToList();

            foreach (var quest in quests)
            {
                bool completed = true;
                foreach (var component in quest.Components)
                {
                    if (component is QuestRequirementComponent requirement)
                    {
                        if (!requirement.IsSatisfied())
                            completed = false;
                    }
                }

                if (completed)
                {
                    quest.Model.GetComponent<QuestInfo>().OnComplete.ExecuteAll(Game.BaseContext);
                    gameState.Destroy(quest);
                }
            }
        }
    }
}