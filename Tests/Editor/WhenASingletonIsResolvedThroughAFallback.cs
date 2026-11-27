using NUnit.Framework;
using RPGFramework.DI.Tests.Types;

namespace RPGFramework.DI.Tests
{
    internal sealed class WhenASingletonIsResolvedThroughAFallback
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
        public void ThenItsDependenciesComeFromTheContainerItIsBoundIn()
        {
            m_Global.BindSingleton<IScoped, GlobalScoped>();
            m_Global.BindSingleton<IScopeConsumer, ScopeConsumer>();
            m_Scene.BindSingletonLocally<IScoped, SceneScoped>();

            ScopeConsumer built = (ScopeConsumer)((IDIResolver)m_Scene).Resolve<IScopeConsumer>();

            Assert.AreSame(((IDIResolver)m_Global).Resolve<IScoped>(), built.Scoped);
        }

        [Test]
        public void ThenAnInterfacesToSelfSingletonDoesTheSame()
        {
            m_Global.BindSingleton<IScoped, GlobalScoped>();
            m_Global.BindInterfacesToSelfSingleton<ScopeConsumer>();
            m_Scene.BindSingletonLocally<IScoped, SceneScoped>();

            ScopeConsumer built = (ScopeConsumer)((IDIResolver)m_Scene).Resolve<IScopeConsumer>();

            Assert.AreSame(((IDIResolver)m_Global).Resolve<IScoped>(), built.Scoped);
        }

        [Test]
        public void ThenItCannotSeeWhatOnlyTheSceneBinds()
        {
            m_Global.BindSingleton<IScopeConsumer, ScopeConsumer>();
            m_Scene.BindSingleton<IScoped, SceneScoped>();

            DIBindingNotFoundException refused = Assert.Throws<DIBindingNotFoundException>(() => ((IDIResolver)m_Scene).Resolve<IScopeConsumer>());

            Assert.AreEqual(typeof(IScoped), refused.ContractType);
        }

        [Test]
        public void ThenATransientBoundInTheFallbackIsStillBuiltFromTheScene()
        {
            m_Global.BindSingleton<IScoped, GlobalScoped>();
            m_Global.BindTransient<IScopeConsumer, ScopeConsumer>();
            m_Scene.BindSingletonLocally<IScoped, SceneScoped>();

            ScopeConsumer built = (ScopeConsumer)((IDIResolver)m_Scene).Resolve<IScopeConsumer>();

            Assert.AreSame(((IDIResolver)m_Scene).Resolve<IScoped>(), built.Scoped);
        }
    }
}
