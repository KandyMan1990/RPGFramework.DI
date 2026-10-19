using NUnit.Framework;
using RPGFramework.DI.Tests.Types;

namespace RPGFramework.DI.Tests
{
    internal sealed class WhenAConstructorHasOptionalParameters
    {
        private IDIResolver m_Resolver;

        [SetUp]
        public void SetUp()
        {
            IDIContainer container = new DIContainer();

            container.BindSingleton<IA, A>();
            container.BindTransient<IOptionalArguments, OptionalArguments>();
            container.BindTransient<IBoundButBroken, BoundButBroken>();
            container.BindTransient<IOptionalBrokenArgument, OptionalBrokenArgument>();
            container.BindTransient<IRequiredArgument, RequiredArgument>();

            m_Resolver = (IDIResolver)container;
        }

        [Test]
        public void ThenItIsStillBuilt()
        {
            Assert.IsNotNull(m_Resolver.Resolve<IOptionalArguments>());
        }

        [Test]
        public void ThenOneBoundNowhereIsNull()
        {
            Assert.IsNull(((OptionalArguments)m_Resolver.Resolve<IOptionalArguments>()).Missing);
        }

        [Test]
        public void ThenOneTakesTheDefaultItDeclares()
        {
            Assert.AreEqual(OptionalArguments.DEFAULT_RETRIES, ((OptionalArguments)m_Resolver.Resolve<IOptionalArguments>()).Retries);
        }

        [Test]
        public void ThenOneThatIsBoundIsResolved()
        {
            Assert.AreSame(m_Resolver.Resolve<IA>(), ((OptionalArguments)m_Resolver.Resolve<IOptionalArguments>()).A);
        }

        [Test]
        public void ThenOneBoundButFailingFurtherDownStillThrows()
        {
            DIBindingNotFoundException refused = Assert.Throws<DIBindingNotFoundException>(() => m_Resolver.Resolve<IOptionalBrokenArgument>());

            StringAssert.Contains(nameof(IMissing), refused.Message);
        }

        [Test]
        public void ThenARequiredOneBoundNowhereStillThrows()
        {
            DIBindingNotFoundException refused = Assert.Throws<DIBindingNotFoundException>(() => m_Resolver.Resolve<IRequiredArgument>());

            StringAssert.Contains(nameof(IMissing), refused.Message);
        }
    }
}
