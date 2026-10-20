using System.Collections.Generic;

namespace TheGrowth.Core.Scripting
{
    public enum ControlStepKind
    {
        /// <summary>Перейти к следующей команде.</summary>
        Continue,

        /// <summary>Войти в блок команды; после блока — следующая команда.</summary>
        Enter,

        /// <summary>Переход на начало секции без возврата (<c>goto</c>).</summary>
        Jump,

        /// <summary>Вызов секции с возвратом (<c>call</c>).</summary>
        Call,

        /// <summary>Возврат из вызова; вне вызова — конец запуска.</summary>
        Return,
    }

    /// <summary>
    /// Шаг, который управляющая команда возвращает исполнителю.
    /// </summary>
    public readonly struct ControlStep
    {
        public ControlStepKind Kind { get; }

        public IReadOnlyList<Command> Block { get; }

        public SectionRef Target { get; }

        private ControlStep(ControlStepKind kind, IReadOnlyList<Command> block, SectionRef target)
        {
            Kind = kind;
            Block = block;
            Target = target;
        }

        public static ControlStep Continue => default;

        public static ControlStep Return => new ControlStep(ControlStepKind.Return, null, default);

        /// <summary>
        /// Блок должен быть одним из блоков команды (<see cref="ControlCommand.GetBlock"/>).
        /// Пустой блок или null — то же, что <see cref="Continue"/>.
        /// </summary>
        public static ControlStep Enter(IReadOnlyList<Command> block)
        {
            return new ControlStep(ControlStepKind.Enter, block, default);
        }

        public static ControlStep Jump(SectionRef target)
        {
            return new ControlStep(ControlStepKind.Jump, null, target);
        }

        public static ControlStep Call(SectionRef target)
        {
            return new ControlStep(ControlStepKind.Call, null, target);
        }
    }
}
