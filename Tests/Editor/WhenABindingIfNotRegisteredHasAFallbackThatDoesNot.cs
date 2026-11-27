using NUnit.Framework;
using RPGFramework.DI.Tests.Types;

namespace RPGFramework.DI.Tests
{
    /// <summary>A scene's container falling back to a global one that binds nothing, binding a default only if missing.</summary>
    internal sealed class WhenABindingIfNotRegisteredHasAFallbackThatDoesNot
    {
        private IDIContainer m_Scene;

        [SetUp]
        public void SetUp()
        {
            m_Scene = new DIContainer();

            m_Scene.SetFallback(new DIContainer());
            m_Scene.BindSingletonIfNotRegistered<IA, OtherA>();
        }

        [Test]
        public void ThenTheDefaultIsBound()
        {
            Assert.IsInstanceOf<OtherA>(((IDIResolver)m_Scene).Resolve<IA>());
        }
    }
}
