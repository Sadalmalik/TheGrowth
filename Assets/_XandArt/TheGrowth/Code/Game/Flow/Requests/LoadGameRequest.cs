using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheGrowth.Core.Saves;
using UnityEngine;

namespace TheGrowth.Game.Flow
{
    /// <summary>
    /// Загрузка слота из меню или локации: прочитать и проверить сейв (архив, описание, все секции) → выгрузить
    /// локацию → уничтожить сессию → создать сессию из сейва → войти в сохранённую локацию. Слот не читается (повреждён,
    /// удалён) — ошибка в лог, текущее состояние не меняется (общий обработчик переходов с возвратом в меню не
    /// задействуется). Ошибка после уничтожения сессии (секция новее кода, нет
    /// миграции, сцена локации) — как у любого перехода: возврат в главное меню.
    /// </summary>
    public sealed class LoadGameRequest : IFlowRequest
    {
        public LoadGameRequest(string slotId)
        {
            SlotId = slotId;
        }

        public string SlotId { get; }

        public bool IsAllowedIn(FlowState state)
        {
            return state is FlowState.MainMenu or FlowState.Hub or FlowState.Expedition;
        }

        public async UniTask ExecuteAsync(GameFlow flow, CancellationToken token)
        {
            SaveGame save;
            try
            {
                save = await flow.Saves.LoadAsync(SlotId, token);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // SaveException — ожидаемая ошибка данных, её сообщение содержит причину; остальное — со стеком.
                if (exception is not SaveException)
                    Debug.LogException(exception);

                Debug.LogError($"[Flow] Slot '{SlotId}' can't be loaded, the game continues: {exception.Message}");
                return;
            }

            flow.SetState(FlowState.Loading);
            await flow.UnloadLocationAsync();
            flow.DestroySession();
            flow.CreateSession(save);
            await flow.LoadLocationAsync(flow.GetSessionLocation(), token);
        }

        public override string ToString()
        {
            return $"{nameof(LoadGameRequest)}({SlotId})";
        }
    }
}
