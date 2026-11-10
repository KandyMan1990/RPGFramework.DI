using System.Text.RegularExpressions;
using NUnit.Framework;
using RPGFramework.DI.Tests.Types;
using UnityEngine;
using UnityEngine.TestTools;

namespace RPGFramework.DI.Tests
{
    internal sealed class WhenASingletonIsNonLazy
    {
        private BuildCounter m_Counter;
        private IDIContainer m_Container;

        [SetUp]
        public void SetUp()
        {
            m_Counter   = new BuildCounter();
            m_Container = new DIContainer();

            m_Container.BindSingletonFromInstance(m_Counter);
            m_Container.BindSingleton<ICounted, Counted>().AsNonLazy();
        }

        [Test]
        public void ThenItIsNotBuiltAsItIsBound()
        {
            Assert.AreEqual(0, m_Counter.Built);
        }

        [Test]
        public void ThenItMayDependOnWhatIsBoundAfterIt()
        {
            m_Container.BindSingleton<IA, A>();

            m_Container.BuildNonLazy();

            Assert.AreEqual(1, m_Counter.Built);
        }

        [Test]
        public void ThenItMayDependOnAFallbackSetAfterIt()
        {
            IDIContainer fallback = new DIContainer();

            fallback.BindSingleton<IA, A>();
            m_Container.SetFallback(fallback);

            m_Container.BuildNonLazy();

            Assert.AreEqual(1, m_Counter.Built);
        }

        [Test]
        public void ThenBuildingTwiceBuildsItOnce()
        {
            m_Container.BindSingleton<IA, A>();

            m_Container.BuildNonLazy();
            m_Container.BuildNonLazy();

            Assert.AreEqual(1, m_Counter.Built);
        }

        [Test]
        public void ThenResolvingItOnceBuiltBuildsNothingMore()
        {
            m_Container.BindSingleton<IA, A>();
            m_Container.BuildNonLazy();

            ((IDIResolver)m_Container).Resolve<ICounted>();

            Assert.AreEqual(1, m_Counter.Built);
        }

        [Test]
        public void ThenAnInterfacesToSelfBindingIsBuiltToo()
        {
            BuildCounter counter   = new BuildCounter();
            IDIContainer container = new DIContainer();

            container.BindSingletonFromInstance(counter);
            container.BindSingleton<IA, A>();
            container.BindInterfacesToSelfSingleton<Counted>().AsNonLazy();

            container.BuildNonLazy();

            Assert.AreEqual(1, counter.Built);
        }

        [Test]
        public void ThenResolvingBeforeItIsBuiltWarns()
        {
            m_Container.BindSingleton<IA, A>();

            LogAssert.Expect(LogType.Warning, new Regex(nameof(IDIContainer.BuildNonLazy)));

            ((IDIResolver)m_Container).Resolve<IA>();
        }
    }
}
