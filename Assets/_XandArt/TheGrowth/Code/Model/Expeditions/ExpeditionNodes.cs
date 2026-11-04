using System;
using TheGrowth.Core.Scripting;
using UnityEngine;

namespace TheGrowth.Model.Expeditions
{
    /// <summary>
    /// Узлы хода экспедиции (<see cref="ExpeditionSystem"/>). Вне экспедиции команды ничего не делают, эвалуаторы возвращают 0 — с
    /// предупреждением в лог; <c>end turn</c> и <c>end expedition</c> вне действия игрока — тоже.
    /// </summary>
    internal static class ExpeditionNodeUtility
    {
        public const string NoExpedition = "there is no active expedition";

        public static void Warn(Command command, string node, string reason)
        {
            Debug.LogWarning($"[Expedition] {command.Location}: {node}: {reason}, the command does nothing");
        }

        /// <summary>Значение эвалуатора экспедиции; экспедиции нет — 0, предупреждение в лог.</summary>
        public static int Evaluate(ScriptContext context, string node, Func<ExpeditionSystem, int> value)
        {
            var system = context.Get<ExpeditionSystem>();
            if (system.IsActive)
                return value(system);

            Debug.LogWarning($"[Expedition] {context.Execution?.Name ?? "no script"}: {node}: {NoExpedition}, 0 is used");
            return 0;
        }
    }

    /// <summary>
    /// Запросить конец хода: обрабатывается после действия игрока (<c>OnStep</c> верхних карт клеток поля, затем номер хода;
    /// счётчик ходов меняют только скрипты).
    /// Повторный в том же действии — один конец хода.
    /// </summary>
    [Script("end_turn", "end turn")]
    public sealed class EndTurnCommand : InstantCommand
    {
        protected override void Run(ScriptContext context)
        {
            if (!context.Get<ExpeditionSystem>().TryRequestEndTurn(out var reason))
                ExpeditionNodeUtility.Warn(this, "end turn", reason);
        }
    }

    /// <summary>
    /// Закончить экспедицию победой или поражением: текущий скрипт дорабатывает, остаток хода пропускается, затем итоги.
    /// Повторный в том же действии — первый итог остаётся, предупреждение.
    /// </summary>
    [Script("end_expedition", "end expedition with [Result]")]
    public sealed class EndExpeditionCommand : InstantCommand
    {
        public ExpeditionResult Result;

        protected override void Run(ScriptContext context)
        {
            if (!context.Get<ExpeditionSystem>().TryRequestEnd(Result, out var reason))
                ExpeditionNodeUtility.Warn(this, $"end expedition with {Result.ToString().ToLowerInvariant()}", reason);
        }
    }

    /// <summary>Задать счётчик ходов экспедиции.</summary>
    [Script("set_turns_left", "set turns left to [Turns]")]
    public sealed class SetTurnsLeftCommand : InstantCommand
    {
        public Evaluator<int> Turns;

        protected override void Run(ScriptContext context)
        {
            var system = context.Get<ExpeditionSystem>();
            if (system.IsActive)
                system.SetTurnsLeft(Turns.Evaluate(context));
            else
                ExpeditionNodeUtility.Warn(this, "set turns left", ExpeditionNodeUtility.NoExpedition);
        }
    }

    /// <summary>Прибавить к счётчику ходов экспедиции; вычитание — отрицательным числом. Переполнение — ошибка исполнения.</summary>
    [Script("add_turns_left", "add [Amount] to turns left")]
    public sealed class AddTurnsLeftCommand : InstantCommand
    {
        public Evaluator<int> Amount;

        protected override void Run(ScriptContext context)
        {
            var system = context.Get<ExpeditionSystem>();
            if (system.IsActive)
                system.AddTurnsLeft(Amount.Evaluate(context));
            else
                ExpeditionNodeUtility.Warn(this, "add to turns left", ExpeditionNodeUtility.NoExpedition);
        }
    }

    /// <summary>Счётчик ходов экспедиции; экспедиции нет — 0.</summary>
    [Script("turns_left", "turns left")]
    public sealed class TurnsLeftEvaluator : Evaluator<int>
    {
        public override int Evaluate(ScriptContext context)
        {
            return ExpeditionNodeUtility.Evaluate(context, "turns left", system => system.TurnsLeft);
        }
    }

    /// <summary>Номер текущего хода экспедиции (первый — 1); экспедиции нет — 0.</summary>
    [Script("turn_number", "turn number")]
    public sealed class TurnNumberEvaluator : Evaluator<int>
    {
        public override int Evaluate(ScriptContext context)
        {
            return ExpeditionNodeUtility.Evaluate(context, "turn number", system => system.TurnNumber);
        }
    }
}
