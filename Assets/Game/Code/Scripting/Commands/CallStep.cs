namespace XandArt.TheGrowth
{
    /// <summary>
    /// Команда вызывает OnStep у всех карт на поле
    /// </summary>
    public class CallStep : Command
    {
        public float delay;
        
        public override void Execute(Context context)
        {
            CardManager.Instance.CallStep(delay);
        }
    }
}