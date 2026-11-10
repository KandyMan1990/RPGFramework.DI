using System;
using NUnit.Framework;
using RPGFramework.DI.Tests.Types;

namespace RPGFramework.DI.Tests
{
    internal sealed class WhenSomethingAsksForTheResolver
    {
        private IDIContainer m_Global;
        private IDIContainer m_Scene;

        [SetUp]
        public void SetUp()
        {
            m_Global = new DIContainer();
            m_Scene  = new DIContainer();

            m_Scene.SetFallback(m_Global);
        }

        [Test]
        public void ThenAContainerGivesItself()
        {
            Assert.AreSame(m_Scene, ((IDIResolver)m_Scene).Resolve<IDIResolver>());
        }

        [Test]
        public void ThenAGlobalSingletonGetsTheGlobalContainer()
        {
            m_Global.BindSingleton<IResolverHolder, ResolverHolder>();

            ResolverHolder holder = (ResolverHolder)((IDIResolver)m_Scene).Resolve<IResolverHolder>();

            Assert.AreSame(m_Global, holder.Resolver);
        }

        [Test]
        public void ThenASceneSingletonGetsTheSceneContainer()
        {
            m_Scene.BindSingleton<IResolverHolder, ResolverHolder>();

            ResolverHolder holder = (ResolverHolder)((IDIResolver)m_Scene).Resolve<IResolverHolder>();

            Assert.AreSame(m_Scene, holder.Resolver);
        }

        [Test]
        public void ThenAGlobalTransientGetsTheContainerThatAsked()
        {
            m_Global.BindTransient<IResolverHolder, ResolverHolder>();

            ResolverHolder holder = (ResolverHolder)((IDIResolver)m_Scene).Resolve<IResolverHolder>();

            Assert.AreSame(m_Scene, holder.Resolver);
        }

        [Test]
        public void ThenBindingTheResolverThrows()
        {
            Assert.Throws<ArgumentException>(() => m_Global.BindSingletonFromInstance((IDIResolver)m_Scene));
        }
    }
}
