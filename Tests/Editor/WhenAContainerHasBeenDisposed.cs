using System;
using NUnit.Framework;
using RPGFramework.DI.Tests.Types;

namespace RPGFramework.DI.Tests
{
    internal sealed class WhenAContainerHasBeenDisposed
    {
        private IDIContainer m_Container;
        private IDIResolver  m_Resolver;

        [SetUp]
        public void SetUp()
        {
            DIContainer container = new DIContainer();

            m_Container = container;
            m_Resolver  = container;

            m_Container.BindSingleton<IA, A>();
            m_Container.Dispose();
        }

        [Test]
        public void ThenResolvingSaysSo()
        {
            Assert.Throws<ObjectDisposedException>(() => m_Resolver.Resolve<IA>());
        }

        [Test]
        public void ThenInjectingARequiredMemberSaysSo()
        {
            Assert.Throws<ObjectDisposedException>(() => m_Resolver.InjectInto(new RequiredTarget()));
        }

        [Test]
        public void ThenInstantiatingABoundPrefabSaysSo()
        {
            Assert.Throws<ObjectDisposedException>(() => m_Resolver.InstantiatePrefab<IA>());
        }

        [Test]
        public void ThenDisposingAgainDoesNothing()
        {
            Assert.DoesNotThrow(() => m_Container.Dispose());
        }
    }
}
