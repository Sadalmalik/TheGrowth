using Cysharp.Threading.Tasks;
using DG.Tweening;
using TheGrowth.Architecture.MVVM;
using UnityEngine;
using UnityEngine.UI;

namespace TheGrowth.UI.Root
{
    /// <summary>Вьюха окна без типа VM: то, что нужно корневой вьюхе (анимации, фокус, доступность).</summary>
    public interface IWindowView
    {
        ViewBase View { get; }

        /// <summary>Элемент, выбираемый при получении фокуса (геймпад); null — выбирать нечего.</summary>
        Selectable FirstSelected { get; }

        /// <summary>
        /// Ввод окна: <paramref name="interactable"/> — элементы доступны мыши и навигации (только у верхнего окна);
        /// <paramref name="blocksRaycasts"/> — окно перекрывает клики (все открытые, кроме скрываемых).
        /// </summary>
        void SetInput(bool interactable, bool blocksRaycasts);

        /// <summary>Анимация показа; завершается и при прерывании (<see cref="StopAnimation"/>, следующая анимация).</summary>
        UniTask PlayShowAsync();

        /// <summary>Анимация скрытия от текущего состояния; завершается и при прерывании.</summary>
        UniTask PlayHideAsync();

        void StopAnimation();
    }

    /// <summary>
    /// Вьюха окна слоя: префаб на тип VM (<see cref="UIViewConfig"/>), экземпляры — из пула фабрики. Показ и скрытие
    /// по умолчанию — альфа <see cref="CanvasGroup"/> (DOTween, время без масштаба), длительность 0 — сразу. Вне Play
    /// (EditMode-тесты) анимации мгновенные.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class WindowView<T> : View<T>, IWindowView where T : WindowViewModel
    {
        [SerializeField]
        private CanvasGroup m_CanvasGroup;

        [SerializeField]
        [Tooltip("Selected when the window gets focus; null — the first interactable selectable.")]
        private Selectable m_FirstSelected;

        [SerializeField]
        [Min(0f)]
        private float m_ShowDuration = 0.2f;

        [SerializeField]
        [Min(0f)]
        private float m_HideDuration = 0.15f;

        private Tween m_Tween;
        private UniTaskCompletionSource m_TweenCompletion;

        public ViewBase View => this;

        public virtual Selectable FirstSelected
        {
            get
            {
                if (IsSelectable(m_FirstSelected))
                    return m_FirstSelected;

                foreach (var selectable in GetComponentsInChildren<Selectable>())
                {
                    if (IsSelectable(selectable))
                        return selectable;
                }

                return null;
            }
        }

        protected CanvasGroup CanvasGroup
        {
            get
            {
                if (m_CanvasGroup == null && !TryGetComponent(out m_CanvasGroup))
                    m_CanvasGroup = gameObject.AddComponent<CanvasGroup>();

                return m_CanvasGroup;
            }
        }

        public void SetInput(bool interactable, bool blocksRaycasts)
        {
            CanvasGroup.interactable = interactable;
            CanvasGroup.blocksRaycasts = blocksRaycasts;
        }

        /// <summary>Показ — всегда с прозрачного (экземпляр из префаба или из пула после скрытия).</summary>
        public UniTask PlayShowAsync()
        {
            CanvasGroup.alpha = 0f;
            return Fade(1f, m_ShowDuration);
        }

        public UniTask PlayHideAsync()
        {
            return Fade(0f, m_HideDuration);
        }

        public void StopAnimation()
        {
            var tween = m_Tween;
            var completion = m_TweenCompletion;
            m_Tween = null;
            m_TweenCompletion = null;
            if (tween != null && tween.IsActive())
                tween.Kill();

            completion?.TrySetResult();
        }

        protected override void OnDestroy()
        {
            StopAnimation();
            base.OnDestroy();
        }

        /// <summary>Доступный выбираемый элемент окна (активен и включён в иерархии).</summary>
        protected static bool IsSelectable(Selectable selectable)
        {
            return selectable != null && selectable.enabled && selectable.gameObject.activeInHierarchy && selectable.interactable;
        }

        /// <summary>Альфа до <paramref name="target"/>; время пропорционально оставшемуся пути (прерванный показ скрывается быстрее).</summary>
        private UniTask Fade(float target, float duration)
        {
            StopAnimation();
            var group = CanvasGroup;
            var time = duration * Mathf.Abs(target - group.alpha);
            if (time <= 0f || !Application.isPlaying)
            {
                group.alpha = target;
                return UniTask.CompletedTask;
            }

            var completion = new UniTaskCompletionSource();
            m_TweenCompletion = completion;
            m_Tween = DOTween.To(() => group.alpha, value => group.alpha = value, target, time)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetTarget(this)
                .OnKill(() => completion.TrySetResult());
            return completion.Task;
        }
    }
}
