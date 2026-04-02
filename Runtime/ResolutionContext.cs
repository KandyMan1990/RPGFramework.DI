namespace RPGFramework.DI
{
    public readonly struct ResolutionContext
    {
        public readonly IDIContainer Container;
        public readonly IDIResolver  Resolver;

        public ResolutionContext(IDIContainer container, IDIResolver resolver)
        {
            Container = container;
            Resolver  = resolver;
        }
    }
}