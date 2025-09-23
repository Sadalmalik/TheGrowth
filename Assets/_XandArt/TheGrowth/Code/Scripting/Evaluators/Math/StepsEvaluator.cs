namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает колличество шагов
    /// </summary>
    public class StepsEvaluator : Evaluator<float>
    {
        public override float Evaluate(Context context)
        {
            var expeditionManager = context.GetRequired<GlobalData>().container.Get<ExpeditionManager>();

            return expeditionManager.Steps;
        }
    }
}