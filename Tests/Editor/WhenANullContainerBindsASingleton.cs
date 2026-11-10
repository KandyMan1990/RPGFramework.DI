using NUnit.Framework;
using RPGFramework.DI.Tests.Types;

namespace RPGFramework.DI.Tests
{
    internal sealed class WhenANullContainerBindsASingleton
    {
        [Test]
        public void ThenAsNonLazyDoesNothing()
        {
            IDIContainer container = new NullDIContainer();

            Assert.DoesNotThrow(() => container.BindSingleton<IA, A>().AsNonLazy());
        }
    }
}
