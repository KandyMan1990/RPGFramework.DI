namespace RPGFramework.DI
{
    internal readonly struct ResolutionContext
    {
        internal readonly IDIContainer Container;
        internal readonly IDIResolver  Resolver;

        internal ResolutionContext(IDIContainer container, IDIResolver resolver)
        {
            Container = container;
            Resolver  = resolver;
        }
    }
}