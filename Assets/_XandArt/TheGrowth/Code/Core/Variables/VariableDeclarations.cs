using System.Collections.Generic;
using TheGrowth.Core.Content;
using UnityEngine;

namespace TheGrowth.Core.Variables
{
    /// <summary>
    /// Конфиг объявлений переменных (геймдизайнерский): список переменных одной темы. Конфигов несколько по темам; все
    /// объявления из реестра контента собирает <see cref="VariableCatalog"/>, имена уникальны по всем конфигам и областям.
    /// </summary>
    [CreateAssetMenu(menuName = "[Game]/Variables", fileName = "Variables")]
    public sealed class VariableDeclarations : ContentAsset
    {
        [SerializeField]
        private List<VariableDeclaration> m_Variables = new List<VariableDeclaration>();

        public IReadOnlyList<VariableDeclaration> Variables => m_Variables;

        /// <summary>Только тесты.</summary>
        internal void SetVariables(IEnumerable<VariableDeclaration> variables)
        {
            m_Variables = new List<VariableDeclaration>(variables);
        }
    }
}
