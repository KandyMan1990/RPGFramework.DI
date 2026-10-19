using NUnit.Framework;
using RPGFramework.DI.Tests.Types;

namespace RPGFramework.DI.Tests
{
    internal sealed class WhenATransientIsResolved
    {
        private IDIResolver m_Resolver;

        [SetUp]
        public void SetUp()
        {
            IDIContainer container = new DIContainer();

            container.BindSingleton<IA, A>();
            container.BindTransient<IB, B>();
            container.BindTransient<IEmpty, Empty>();

            m_Resolver = (IDIResolver)container;
        }

        [Test]
        public void ThenItIsANewInstanceEachTime()
        {
            Assert.AreNotSame(m_Resolver.Resolve<IB>(), m_Resolver.Resolve<IB>());
        }

        [Test]
        public void ThenItsConstructorGetsTheSingleton()
        {
            Assert.AreSame(m_Resolver.Resolve<IA>(), ((B)m_Resolver.Resolve<IB>()).A);
        }

        [Test]
        public void ThenOneWithNoParametersIsBuiltEachTimeToo()
        {
            IEmpty first = m_Resolver.Resolve<IEmpty>();

            Assert.IsNotNull(first);
            Assert.AreNotSame(first, m_Resolver.Resolve<IEmpty>());
        }
    }
}
