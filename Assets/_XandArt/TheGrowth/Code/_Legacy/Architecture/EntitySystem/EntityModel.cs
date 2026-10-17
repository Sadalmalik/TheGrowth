using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace XandArt.Architecture
{
    public interface IEntityModelComponent
    {
        void OnEntityCreated(CompositeEntity card);
    }

    [CreateAssetMenu(
        fileName = "EntityModel",
        menuName = "[Game]/Entity Model",
        order = 0)]
    public class EntityModel : AbstractEntityModel
    {
        [ValidateInput(nameof(ElementsNotNull), "Components can't be null!")]
        public List<IEntityModelComponent> components = new List<IEntityModelComponent>();

        private bool ElementsNotNull(List<IEntityModelComponent> list)
        {
            return !list.Contains(null);
        }
        
        public TComponent GetComponent<TComponent>() where TComponent : IEntityModelComponent
        {
            return (TComponent) components?.FirstOrDefault(c => c.GetType() == typeof(TComponent));
        }

        public TComponent AddComponent<TComponent>() where TComponent : IEntityModelComponent, new()
        {
            var component = GetComponent<TComponent>();
            if (component == null)
            {
                component = new TComponent();
                components ??= new List<IEntityModelComponent>();
                components.Add(component);
            }
            return component;
        }

        public override Entity Create()
        {
            var entity = new CompositeEntity { _model = this };
            foreach (var component in components)
                component.OnEntityCreated(entity);
            return entity;
        }

        // [MenuItem("Assets/Create/[Game]/Entity Model 2", false, 0)]
        // public static void TestCreate()
        // {
        //     Debug.Log($"[TEST] Selection: {Selection.activeObject}, {Selection.activeContext}");
        //     // Selection.activeObject - папка в которой создавать объект
        // }
    }
}