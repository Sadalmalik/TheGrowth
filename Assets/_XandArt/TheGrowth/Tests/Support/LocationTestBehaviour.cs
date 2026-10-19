using System;
using System.Collections.Generic;
using System.Threading;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Scripting;
using TheGrowth.Presentation.Locations;
using UnityEngine;

namespace TheGrowth.Tests.Support
{
    /// <summary>Поведение-проба: журнал init / dispose с состоянием токена, зависимости из трёх областей.</summary>
    public sealed class LocationTestBehaviour : MonoBehaviour, ILocationBehaviour
    {
#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private ILifetime m_Lifetime;

        [Inject]
        private ScriptRunner m_Runner;

        [Inject]
        private LocationRoot m_Root;
#pragma warning restore CS0649

        public string Id;
        public List<string> Journal;
        public bool ThrowOnInit;
        public bool ThrowOnDispose;

        public CancellationToken Token => m_Lifetime?.Token ?? CancellationToken.None;

        public ScriptRunner Runner => m_Runner;

        public LocationRoot Root => m_Root;

        public void OnLocationInit()
        {
            if (ThrowOnInit)
                throw new InvalidOperationException($"{Id}: init failed");

            Journal?.Add($"{Id}:init");
        }

        public void OnLocationDispose()
        {
            if (ThrowOnDispose)
                throw new InvalidOperationException($"{Id}: dispose failed");

            Journal?.Add($"{Id}:dispose:{(Token.IsCancellationRequested ? "cancelled" : "alive")}");
        }
    }
}
