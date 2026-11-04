using System;
using System.Collections.Generic;
using TheGrowth.Core.Content;
using TheGrowth.Core.Entities;
using UnityEngine;

namespace TheGrowth.Tests.EditMode.Entities
{
    /// <summary>Тестовый конфиг со ссылками на конфиги сущностей с <see cref="ModelKindAttribute"/>: поле, список, вложенный объект.</summary>
    public sealed class TestEntityLinks : ContentAsset
    {
        [ModelKind(EntityKind.Card)]
        public EntityModel Card;

        [ModelKind(EntityKind.Modifier)]
        public List<EntityModel> Modifiers = new List<EntityModel>();

        public TestEntityNested Nested = new TestEntityNested();

        [SerializeField]
        [ModelKind(EntityKind.Card, EntityKind.Mechanic)]
        private EntityModel m_Placed;

        /// <summary>Без атрибута: любой вид.</summary>
        public EntityModel Any;

        public EntityModel Placed
        {
            get => m_Placed;
            set => m_Placed = value;
        }
    }

    [Serializable]
    public sealed class TestEntityNested
    {
        [ModelKind(EntityKind.Mechanic)]
        public EntityModel Mechanic;
    }
}
