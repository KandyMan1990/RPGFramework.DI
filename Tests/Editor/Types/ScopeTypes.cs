namespace RPGFramework.DI.Tests.Types
{
    internal interface IScoped
    {
    }

    internal sealed class GlobalScoped : IScoped
    {
    }

    internal sealed class SceneScoped : IScoped
    {
    }

    internal interface IScopeConsumer
    {
    }

    internal sealed class ScopeConsumer : IScopeConsumer
    {
        internal readonly IScoped Scoped;

        public ScopeConsumer(IScoped scoped)
        {
            Scoped = scoped;
        }
    }

    internal interface IResolverHolder
    {
    }

    internal sealed class ResolverHolder : IResolverHolder
    {
        internal readonly IDIResolver Resolver;

        public ResolverHolder(IDIResolver resolver)
        {
            Resolver = resolver;
        }
    }
}
