using System;
using NUnit.Framework;
using RPGFramework.DI.Tests.Types;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGFramework.DI.Tests
{
    /// <summary>
    /// A scene's container falling back to a global one that binds <see cref="IA" />, then binding it again without
    /// saying it overrides the global one.
    /// </summary>
    internal sealed class WhenAContractAFallbackBindsIsBoundAgain
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
        public void ThenBindingItThrows()
        {
            Assert.Throws<ArgumentException>(() => m_Scene.BindSingleton<IA, OtherA>());

            AssertTheGlobalOnesIsResolved();
        }

        [Test]
        public void ThenForcingItThrows()
        {
            Assert.Throws<ArgumentException>(() => m_Scene.ForceBindSingleton<IA, OtherA>());

            AssertTheGlobalOnesIsResolved();
        }

        [Test]
        public void ThenBindingAPrefabOfItThrows()
        {
            m_Prefab = new GameObject("Prefab", typeof(ComponentA));

            Assert.Throws<ArgumentException>(() => m_Scene.BindPrefab<IA, ComponentA>(m_Prefab.GetComponent<ComponentA>()));
        }

        [Test]
        public void ThenBindingAClassImplementingItBindsNoneOfItsContracts()
        {
            Assert.Throws<ArgumentException>(() => m_Scene.BindInterfacesAndConcreteToSelfSingleton<OtherA>());

            AssertTheGlobalOnesIsResolved();
            Assert.Throws<DIBindingNotFoundException>(() => ((IDIResolver)m_Scene).Resolve<OtherA>());
        }

        private void AssertTheGlobalOnesIsResolved()
        {
            Assert.AreSame(((IDIResolver)m_Global).Resolve<IA>(), ((IDIResolver)m_Scene).Resolve<IA>());
        }
    }
}
