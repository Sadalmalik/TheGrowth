using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheGrowth.Architecture.IOC;
using UnityEngine;

namespace TheGrowth.Game.Scenes
{
    /// <summary>
    /// Загрузка и выгрузка сцен additive по имени (глобальная область). Пользуется только <c>GameFlow</c>.
    /// Интерфейс — для тестов без сцен: заглушка отдаёт корневые объекты, созданные в памяти.
    /// </summary>
    public interface ISceneService : ISharedInterface
    {
        /// <summary>Загрузить сцену additive (сцена должна быть в Build Settings); результат — её корневые объекты.</summary>
        /// <param name="activate">Сделать сцену активной: её освещение, в неё создаются новые объекты.</param>
        UniTask<IReadOnlyList<GameObject>> LoadAdditiveAsync(string sceneName, bool activate, CancellationToken token);

        /// <summary>Выгрузить сцену; не загружена — предупреждение.</summary>
        UniTask UnloadAsync(string sceneName);
    }
}
