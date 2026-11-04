using TheGrowth.Core.Entities;
using UnityEditor;

namespace TheGrowth.EditorTools.Entities
{
    /// <summary>
    /// Пресеты создания конфигов сущностей в меню Create: <c>[Game]/Entity/Card</c>, <c>Modifier</c>, <c>Mechanic</c> — вид и
    /// обязательные компоненты вида (<see cref="EntityModelEditing.CreatePreset"/>). ID заполняется из имени ассета при
    /// сборке реестра.
    /// </summary>
    public static class EntityModelPresets
    {
        private const string MenuRoot = "Assets/Create/[Game]/Entity/";

        [MenuItem(MenuRoot + "Card")]
        public static void CreateCard()
        {
            Create(EntityKind.Card, "Card");
        }

        [MenuItem(MenuRoot + "Modifier")]
        public static void CreateModifier()
        {
            Create(EntityKind.Modifier, "Modifier");
        }

        [MenuItem(MenuRoot + "Mechanic")]
        public static void CreateMechanic()
        {
            Create(EntityKind.Mechanic, "Mechanic");
        }

        private static void Create(EntityKind kind, string name)
        {
            ProjectWindowUtil.CreateAsset(EntityModelEditing.CreatePreset(kind), name + ".asset");
        }
    }
}
