using System;
using NUnit.Framework;
using RPGFramework.DI.Tests.Types;

namespace RPGFramework.DI.Tests
{
    internal sealed class WhenAPropertyIsInjected
    {
        private IDIResolver m_Resolver;

        [SetUp]
        public void SetUp()
        {
            IDIContainer container = new DIContainer();

            container.BindSingleton<IA, A>();

            m_Resolver = (IDIResolver)container;
        }

        [Test]
        public void ThenOneWithNoSetterThrowsNamingIt()
        {
            InvalidOperationException refused = Assert.Throws<InvalidOperationException>(() => m_Resolver.InjectInto(new GetOnlyTarget()));

            StringAssert.Contains($"{nameof(GetOnlyTarget)}.{nameof(GetOnlyTarget.Value)}", refused.Message);
        }

        [Test]
        public void ThenOneWithAPrivateSetterIsFilled()
        {
            PrivateSetterTarget target = new PrivateSetterTarget();

            m_Resolver.InjectInto(target);

            Assert.IsNotNull(target.Value);
        }
    }
}
