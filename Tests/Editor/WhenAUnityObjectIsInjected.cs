using NUnit.Framework;
using RPGFramework.DI.Tests.Types;
using UnityEngine;

namespace RPGFramework.DI.Tests
{
    internal sealed class WhenAUnityObjectIsInjected
    {
        private GameObject        m_Holder;
        private InjectedComponent m_Component;
        private InjectedAsset     m_Asset;

        [SetUp]
        public void SetUp()
        {
            IDIContainer container = new DIContainer();

            container.BindSingleton<IA, A>();

            m_Holder    = new GameObject();
            m_Component = m_Holder.AddComponent<InjectedComponent>();
            m_Asset     = ScriptableObject.CreateInstance<InjectedAsset>();

            ((IDIResolver)container).InjectInto(m_Component);
            ((IDIResolver)container).InjectInto(m_Asset);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Holder);
            Object.DestroyImmediate(m_Asset);
        }

        [Test]
        public void ThenAComponentsOwnMemberIsFilled()
        {
            Assert.IsNotNull(m_Component.A);
        }

        [Test]
        public void ThenAPrivateMemberOfItsOwnBaseClassIsFilled()
        {
            Assert.IsNotNull(m_Component.BaseA);
        }

        [Test]
        public void ThenAnAssetsMemberIsFilled()
        {
            Assert.IsNotNull(m_Asset.A);
        }
    }
}
