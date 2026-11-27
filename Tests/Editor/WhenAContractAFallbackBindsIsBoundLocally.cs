using System;
using NUnit.Framework;
using RPGFramework.DI.Tests.Types;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGFramework.DI.Tests
{
    /// <summary>
    /// A scene's container falling back to a global one that binds <see cref="IA" />, then overriding it locally, each
    /// of the six ways.
    /// </summary>
    internal sealed class WhenAContractAFallbackBindsIsBoundLocally
    {
        private IDIContainer m_Global;
        private IDIContainer m_Scene;
        private GameObject   m_Prefab;
        private GameObject   m_Instance;

        [SetUp]
        public void SetUp()
        {
            m_Global   = new DIContainer();
            m_Scene    = new DIContainer();
            m_Prefab   = null;
            m_Instance = null;

            m_Global.BindSingleton<IA, A>();
            m_Scene.SetFallback(m_Global);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Instance != null)
            {
                Object.DestroyImmediate(m_Instance);
            }

            if (m_Prefab != null)
            {
                Object.DestroyImmediate(m_Prefab);
            }
        }

        [Test]
        public void ThenATransientOverridesIt()
        {
            m_Scene.BindTransientLocally<IA, OtherA>();

            AssertTheSceneResolvesItsOwn();
        }

        [Test]
        public void ThenASingletonOverridesIt()
        {
            m_Scene.BindSingletonLocally<IA, OtherA>();

            AssertTheSceneResolvesItsOwn();
        }

        [Test]
        public void ThenAnInstanceOverridesIt()
        {
            OtherA instance = new OtherA();

            m_Scene.BindSingletonFromInstanceLocally<IA>(instance);

            Assert.AreSame(instance, ((IDIResolver)m_Scene).Resolve<IA>());
        }

        [Test]
        public void ThenItsInterfacesToSelfOverrideIt()
        {
            m_Scene.BindInterfacesToSelfSingletonLocally<OtherA>();

            AssertTheSceneResolvesItsOwn();
        }

        [Test]
        public void ThenItsInterfacesAndConcreteOverrideIt()
        {
            m_Scene.BindInterfacesAndConcreteToSelfSingletonLocally<OtherA>();

            AssertTheSceneResolvesItsOwn();
            Assert.AreSame(((IDIResolver)m_Scene).Resolve<IA>(), ((IDIResolver)m_Scene).Resolve<OtherA>());
        }

        [Test]
        public void ThenAPrefabOverridesIt()
        {
            m_Prefab = new GameObject("Prefab", typeof(ComponentA));

            m_Scene.BindPrefabLocally<IA, ComponentA>(m_Prefab.GetComponent<ComponentA>());

            ComponentA instance = (ComponentA)((IDIResolver)m_Scene).InstantiatePrefab<IA>();

            m_Instance = instance.gameObject;

            Assert.IsNotNull(instance);
        }

        [Test]
        public void ThenTheFallbackKeepsItsOwn()
        {
            m_Scene.BindSingletonLocally<IA, OtherA>();

            Assert.IsInstanceOf<A>(((IDIResolver)m_Global).Resolve<IA>());
        }

        [Test]
        public void ThenUnbindingItLeavesTheFallbacks()
        {
            m_Scene.BindSingletonLocally<IA, OtherA>();

            Assert.IsTrue(m_Scene.Unbind<IA>());
            Assert.AreSame(((IDIResolver)m_Global).Resolve<IA>(), ((IDIResolver)m_Scene).Resolve<IA>());
        }

        [Test]
        public void ThenBindingItLocallyAgainThrows()
        {
            m_Scene.BindSingletonLocally<IA, OtherA>();

            Assert.Throws<ArgumentException>(() => m_Scene.BindSingletonLocally<IA, OtherA>());
        }

        private void AssertTheSceneResolvesItsOwn()
        {
            Assert.IsInstanceOf<OtherA>(((IDIResolver)m_Scene).Resolve<IA>());
            Assert.IsInstanceOf<A>(((IDIResolver)m_Global).Resolve<IA>());
        }
    }
}
