using System.Linq;
using UnityEngine;
using XandArt.Architecture;
using XandArt.Architecture.IOC;
using XandArt.TheGrowth.Crafting;

namespace XandArt.TheGrowth
{
    [SelectionBase]
    public class HUBCraftScreen : WidgetBase
    {
        [SerializeField]
        private RectTransform _projectContainer;
        
        [SerializeField]
        private RectTransform _projectPlaceholder;
        
        [SerializeField]
        private RectTransform _craftsList;
        
        [SerializeField]
        private RectTransform _craftContainer;
        
        [SerializeField]
        private RectTransform _craftPlaceholder;

        [SerializeField]
        private UICraft _CraftSlectPrefab;
        
        [Inject]
        private GameManager m_GameManager;

        private CraftingContainer m_Crafting;
        
        public override void Init()
        {
            m_Crafting = (CraftingContainer) m_GameManager.CurrentGameState.GetAll<CraftingContainer>().FirstOrDefault();

            RefreshProjectCraft();
            RefreshCraftsList();
        }

        public void RefreshProjectCraft()
        {
            var project = m_Crafting.Project.Value;
            var hasProject = project != null && project.uiCraftPrefab;
            if (hasProject)
            {
                _projectContainer.Clear();
                Instantiate(project.uiCraftPrefab, _projectContainer);
            }
            _projectPlaceholder.gameObject.SetActive(!hasProject);
        }

        public void RefreshCraftsList()
        {
            _craftsList.Clear();

            foreach (var craft in m_Crafting.Crafts)
            {
                var uiCraft = Instantiate(_CraftSlectPrefab, _craftsList);
                uiCraft.Setup(this, craft.Value);
            }
        }

        public void SelectCraft(string craftAssetName)
        {
            var craft = m_Crafting.Crafts
                .Select(craftRef => craftRef.Value)
                .FirstOrDefault(craft => craft.name.Equals(craftAssetName));
            _craftContainer.Clear();
            var hasCraft = craft != null && craft.uiCraftPrefab;
            if (hasCraft)
            {
                _craftContainer.Clear();
                Instantiate(craft.uiCraftPrefab, _craftContainer);
            }
            _craftPlaceholder.gameObject.SetActive(!hasCraft);
        }
    }
}