using NUnit.Framework;
using RPGFramework.DI.Tests.Types;

namespace RPGFramework.DI.Tests
{
    internal sealed class WhenASingletonIsResolved
    {
        [Test]
        public void ThenItIsTheSameInstanceEachTime()
        {
            IDIContainer container = new DIContainer();

            container.BindSingleton<IA, A>();

            IDIResolver resolver = (IDIResolver)container;

            Assert.AreSame(resolver.Resolve<IA>(), resolver.Resolve<IA>());
        }
    }
}
