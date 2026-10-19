using System;
using NUnit.Framework;
using RPGFramework.DI.Tests.Types;

namespace RPGFramework.DI.Tests
{
    internal sealed class WhenDependenciesAreCircular
    {
        [Test]
        public void ThenResolvingIsRefusedNamingTheLoop()
        {
            IDIContainer container = new DIContainer();

            container.BindTransient<ILoop1, Loop1>();
            container.BindTransient<ILoop2, Loop2>();

            InvalidOperationException refused = Assert.Throws<InvalidOperationException>(() => ((IDIResolver)container).Resolve<ILoop1>());

            StringAssert.Contains("Circular dependency", refused.Message);
        }
    }
}
