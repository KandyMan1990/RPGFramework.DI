namespace RPGFramework.DI.Tests.Types
{
    internal sealed class BuildCounter
    {
        internal int Built;
    }

    internal interface ICounted
    {
    }

    internal sealed class Counted : ICounted
    {
        public Counted(BuildCounter counter, IA a)
        {
            counter.Built++;
        }
    }
}
