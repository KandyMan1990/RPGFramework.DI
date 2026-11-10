using NUnit.Framework;
using RPGFramework.DI.Tests.Types;

namespace RPGFramework.DI.Tests
{
    internal sealed class WhenAnOptionalMemberIsBoundButBroken
    {
        private IDIResolver m_Resolver;

        [SetUp]
        public void SetUp()
        {
            IDIContainer container = new DIContainer();

            container.BindSingleton<IA, A>();
            container.BindSingleton<IBoundButBroken, BoundButBroken>();

            m_Resolver = (IDIResolver)container;
        }

        [Test]
        public void ThenAnOptionalFieldThrowsNamingWhatIsMissing()
        {
            DIBindingNotFoundException refused = Assert.Throws<DIBindingNotFoundException>(() => m_Resolver.InjectInto(new OptionalBrokenField()));

            Assert.AreEqual(typeof(IMissing), refused.ContractType);
        }

        [Test]
        public void ThenAnOptionalPropertyThrowsNamingWhatIsMissing()
        {
            DIBindingNotFoundException refused = Assert.Throws<DIBindingNotFoundException>(() => m_Resolver.InjectInto(new OptionalBrokenProperty()));

            Assert.AreEqual(typeof(IMissing), refused.ContractType);
        }

        [Test]
        public void ThenAnOptionalMethodThrowsNamingWhatIsMissing()
        {
            DIBindingNotFoundException refused = Assert.Throws<DIBindingNotFoundException>(() => m_Resolver.InjectInto(new OptionalBrokenMethod()));

            Assert.AreEqual(typeof(IMissing), refused.ContractType);
        }

        [Test]
        public void ThenAnOptionalMethodWhoseOnlyMissingParameterIsOptionalStillRuns()
        {
            OptionalMethodWithOptionalParameter target = new OptionalMethodWithOptionalParameter();

            m_Resolver.InjectInto(target);

            Assert.IsTrue(target.Ran);
        }
    }
}
