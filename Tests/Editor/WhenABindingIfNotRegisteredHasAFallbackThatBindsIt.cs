using NUnit.Framework;
using RPGFramework.DI.Tests.Types;
using UnityEngine;

namespace RPGFramework.DI.Tests
{
    /// <summary>
    /// A scene's container falling back to a global one that binds <see cref="IA" />, then binding its own default of
    /// it only if missing, each of the six ways.
    /// </summary>
    internal sealed class WhenABindingIfNotRegisteredHasAFallbackThatBindsIt
    {
        private IDIContainer m_Global;
        private IDIContainer m_Scene;
        private GameObject   m_Prefab;

        [SetUp]
        public void SetUp()
        {
            m_Global = new DIContainer();
            m_Scene  = new DIContainer();
            m_Prefab = null;

            m_Global.BindSingleton<IA, A>();
            m_Scene.SetFallback(m_Global);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Prefab != null)
            {
                Object.DestroyImmediate(m_Prefab);
            }
        }

        [Test]
        public void ThenATransientIsNotBound()
        {
            m_Scene.BindTransientIfNotRegistered<IA, OtherA>();

            AssertTheGlobalOnesIsResolved();
        }

        [Test]
        public void ThenASingletonIsNotBound()
        {
            m_Scene.BindSingletonIfNotRegistered<IA, OtherA>();

            AssertTheGlobalOnesIsResolved();
        }

        [Test]
        public void ThenAnInstanceIsNotBound()
        {
            m_Scene.BindSingletonFromInstanceIfNotRegistered<IA>(new OtherA());

            AssertTheGlobalOnesIsResolved();
        }

        [Test]
        public void ThenItsInterfacesToSelfAreNotBound()
        {
            m_Scene.BindInterfacesToSelfSingletonIfNotRegistered<OtherA>();

            AssertTheGlobalOnesIsResolved();
        }

        [Test]
        public void ThenItsInterfacesAreNotBoundButItsConcreteIs()
        {
            m_Scene.BindInterfacesAndConcreteToSelfSingletonIfNotRegistered<OtherA>();

            AssertTheGlobalOnesIsResolved();
            Assert.IsNotNull(((IDIResolver)m_Scene).Resolve<OtherA>());
        }

        [Test]
        public void ThenAPrefabIsNotBound()
        {
            m_Prefab = new GameObject("Prefab", typeof(ComponentA));

            m_Scene.BindPrefabIfNotRegistered<IA, ComponentA>(m_Prefab.GetComponent<ComponentA>());

            AssertTheGlobalOnesIsResolved();
        }

        private void AssertTheGlobalOnesIsResolved()
        {
            Assert.AreSame(((IDIResolver)m_Global).Resolve<IA>(), ((IDIResolver)m_Scene).Resolve<IA>());
        }
    }
}
