using System;
using NUnit.Framework;
using RPGFramework.DI.Tests.Types;

namespace RPGFramework.DI.Tests
{
    /// <summary>A scene's container binding first and being given its fallback after.</summary>
    internal sealed class WhenAFallbackIsSetAfterBinding
    {
        private IDIContainer m_Global;
        private IDIContainer m_Scene;

        [SetUp]
        public void SetUp()
        {
            m_Global = new DIContainer();
            m_Scene  = new DIContainer();

            m_Global.BindSingleton<IA, A>();
        }

        [Test]
        public void ThenOneBindingWhatItBindsIsRefused()
        {
            m_Scene.BindSingleton<IA, OtherA>();

            Assert.Throws<InvalidOperationException>(() => m_Scene.SetFallback(m_Global));
        }

        [Test]
        public void ThenOneBindingNothingItBindsIsSet()
        {
            m_Scene.BindTransient<IB, B>();
            m_Scene.SetFallback(m_Global);

            B built = (B)((IDIResolver)m_Scene).Resolve<IB>();

            Assert.AreSame(((IDIResolver)m_Global).Resolve<IA>(), built.A);
        }
    }
}
