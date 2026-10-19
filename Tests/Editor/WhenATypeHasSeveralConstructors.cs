using System;
using NUnit.Framework;
using RPGFramework.DI.Tests.Types;

namespace RPGFramework.DI.Tests
{
    internal sealed class WhenATypeHasSeveralConstructors
    {
        private IDIResolver m_Resolver;

        [SetUp]
        public void SetUp()
        {
            IDIContainer container = new DIContainer();

            container.BindSingleton<IA, A>();
            container.BindSingleton<IB, B>();
            container.BindTransient<IWidest, Widest>();
            container.BindTransient<ISkipsObsolete, SkipsObsolete>();

            m_Resolver = (IDIResolver)container;
        }

        [Test]
        public void ThenTheWidestIsChosen()
        {
            Assert.AreEqual(2, ((Widest)m_Resolver.Resolve<IWidest>()).Used);
        }

        [Test]
        public void ThenAnObsoleteOneIsPassedOver()
        {
            Assert.AreEqual(1, ((SkipsObsolete)m_Resolver.Resolve<ISkipsObsolete>()).Used);
        }

        [Test]
        public void ThenTwoWidestAreRefusedNamingBoth()
        {
            InvalidOperationException refused = Assert.Throws<InvalidOperationException>(() => ((IDIContainer)new DIContainer()).BindTransient<ITied, Tied>());

            StringAssert.Contains("Tied(IA)", refused.Message);
            StringAssert.Contains("Tied(IB)", refused.Message);
        }

        [Test]
        public void ThenOnlyObsoleteOnesAreRefused()
        {
            InvalidOperationException refused = Assert.Throws<InvalidOperationException>(() => ((IDIContainer)new DIContainer()).BindTransient<IAllObsolete, AllObsolete>());

            StringAssert.Contains("no usable constructors", refused.Message);
        }
    }
}
