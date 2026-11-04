using System;
using System.Globalization;
using TheGrowth.Core.Content;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Scripting;
using TheGrowth.Model.Graphs;
using TheGrowth.Model.Shapes;
using TheGrowth.Model.Slots;

namespace TheGrowth.Model.Cards
{
#region Shapes

    /// <summary>
    /// Узлы фигур (<see cref="Shape"/>): метры, углы и масштаб — числа с дробной частью (<c>1.5</c>), литералом. Неверное
    /// значение (отрицательный размер, масштаб не больше 0) — ошибка исполнения.
    /// </summary>
    internal static class ShapeNodeUtility
    {
        /// <summary>Значение параметра; не задан (короткий шаблон) — <paramref name="fallback"/>.</summary>
        public static float Get(Evaluator<float> value, ScriptContext context, float fallback)
        {
            return value?.Evaluate(context) ?? fallback;
        }

        /// <summary>Метры не меньше 0; иначе ошибка исполнения.</summary>
        public static float Meters(float value, string node, string what)
        {
            if (value < 0f)
                throw new InvalidOperationException($"{node}: the {what} is {Format(value)}m, it must be 0 or more");

            return value;
        }

        public static string Format(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Круг радиусом в метрах: <c>circle 2m</c>.</summary>
    [Script("circle", "circle [Radius]m")]
    public sealed class CircleNode : Evaluator<Shape>
    {
        public Evaluator<float> Radius;

        public override Shape Evaluate(ScriptContext context)
        {
            return new CircleShape(ShapeNodeUtility.Meters(Radius.Evaluate(context), "circle", "radius"));
        }
    }

    /// <summary>
    /// Квадрат: размер — половина стороны в метрах, поворот в градусах (необязательно), затем масштаб по осям X и Z графа
    /// (необязательно). Ромб на сетке с неквадратным шагом — <c>square 1.06m rotated 45 scaled 1.5 2</c>.
    /// </summary>
    [Script("square", "square [Size]m")]
    [Script("square", "square [Size]m rotated [Angle]")]
    [Script("square", "square [Size]m scaled [ScaleX] [ScaleZ]")]
    [Script("square", "square [Size]m rotated [Angle] scaled [ScaleX] [ScaleZ]")]
    public sealed class SquareNode : Evaluator<Shape>
    {
        public Evaluator<float> Size;
        public Evaluator<float> Angle;
        public Evaluator<float> ScaleX;
        public Evaluator<float> ScaleZ;

        public override Shape Evaluate(ScriptContext context)
        {
            var size = ShapeNodeUtility.Meters(Size.Evaluate(context), "square", "size");
            var angle = ShapeNodeUtility.Get(Angle, context, 0f);
            var scaleX = ShapeNodeUtility.Get(ScaleX, context, 1f);
            var scaleZ = ShapeNodeUtility.Get(ScaleZ, context, 1f);
            if (scaleX <= 0f || scaleZ <= 0f)
                throw new InvalidOperationException($"square: the scale is {ShapeNodeUtility.Format(scaleX)} {ShapeNodeUtility.Format(scaleZ)}, it must be above 0");

            return new SquareShape(size, angle, scaleX, scaleZ);
        }
    }

    /// <summary>Линия из центра под углом (градусы) на длину в метрах в обе стороны: <c>line 90 4m</c>.</summary>
    [Script("line", "line [Angle] [Length]m")]
    public sealed class LineNode : Evaluator<Shape>
    {
        public Evaluator<float> Angle;
        public Evaluator<float> Length;

        public override Shape Evaluate(ScriptContext context)
        {
            var angle = Angle.Evaluate(context);
            return new LineShape(angle, ShapeNodeUtility.Meters(Length.Evaluate(context), "line", "length"), true);
        }
    }

    /// <summary>Луч из центра под углом (градусы) на длину в метрах — в одну сторону: <c>ray 0 3m</c>.</summary>
    [Script("ray", "ray [Angle] [Length]m")]
    public sealed class RayNode : Evaluator<Shape>
    {
        public Evaluator<float> Angle;
        public Evaluator<float> Length;

        public override Shape Evaluate(ScriptContext context)
        {
            var angle = Angle.Evaluate(context);
            return new LineShape(angle, ShapeNodeUtility.Meters(Length.Evaluate(context), "ray", "length"), false);
        }
    }

    /// <summary>Точки A или B. Скелет совпадает с <c>or</c> наборов и условий — результат другого типа.</summary>
    [Script("shapes_or", "[A] or [B]")]
    public sealed class ShapesOr : Evaluator<Shape>
    {
        public Evaluator<Shape> A;
        public Evaluator<Shape> B;

        public override Shape Evaluate(ScriptContext context)
        {
            return Shape.Union(A.Evaluate(context), B.Evaluate(context));
        }
    }

    /// <summary>Точки, которые есть и в A, и в B.</summary>
    [Script("shapes_and", "[A] and [B]")]
    public sealed class ShapesAnd : Evaluator<Shape>
    {
        public Evaluator<Shape> A;
        public Evaluator<Shape> B;

        public override Shape Evaluate(ScriptContext context)
        {
            return Shape.Intersection(A.Evaluate(context), B.Evaluate(context));
        }
    }

    /// <summary>Точки A, которых нет в B (может убрать и центр).</summary>
    [Script("shapes_except", "[A] except [B]")]
    public sealed class ShapesExcept : Evaluator<Shape>
    {
        public Evaluator<Shape> A;
        public Evaluator<Shape> B;

        public override Shape Evaluate(ScriptContext context)
        {
            return Shape.Difference(A.Evaluate(context), B.Evaluate(context));
        }
    }

#endregion

#region Slot sets

    /// <summary>
    /// Клетки поля, центры которых в фигуре с центром в клетке карты (плоскость XZ графа), по порядку графа; клетка карты
    /// входит в фигуру-примитив. Ширина линий — ширина карты (<see cref="FieldConfig.CardSize"/>). Карта не на клетке поля —
    /// пусто.
    /// </summary>
    [Script("slots_in_shape", "slots in [Shape] around [Card]")]
    public sealed class SlotsInShapeEvaluator : Evaluator<SlotSet>
    {
        public Evaluator<Shape> Shape;
        public Evaluator<Entity> Card;

        public override SlotSet Evaluate(ScriptContext context)
        {
            var shape = Shape.Evaluate(context);
            var system = context.Get<SlotSystem>();
            var field = system.Expedition?.Field;
            var center = system.GetSlot(Card.Evaluate(context));
            if (field == null || center == null || shape == null)
                return SlotSet.Empty;

            var halfWidth = FieldConfig.GetCardSize(context.Get<IContentRegistry>()).x * 0.5f;
            return new SlotSet(field.GetCellsIn(shape, center, halfWidth));
        }
    }

#endregion
}
