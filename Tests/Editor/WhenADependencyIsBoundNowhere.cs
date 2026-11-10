using NUnit.Framework;
using RPGFramework.DI.Tests.Types;

namespace RPGFramework.DI.Tests
{
    internal sealed class WhenADependencyIsBoundNowhere
    {
        private DIBindingNotFoundException m_Refused;

        [SetUp]
        public void SetUp()
        {
            IDIContainer container = new DIContainer();

            container.BindTransient<IOptionalBrokenArgument, OptionalBrokenArgument>();
            container.BindTransient<IBoundButBroken, BoundButBroken>();

            m_Refused = Assert.Throws<DIBindingNotFoundException>(() => ((IDIResolver)container).Resolve<IOptionalBrokenArgument>());
        }

        [Test]
        public void ThenTheContractNamedIsTheOneMissing()
        {
            Assert.AreEqual(typeof(IMissing), m_Refused.ContractType);
        }

        [Test]
        public void ThenTheErrorNamesWhatNeededIt()
        {
            StringAssert.Contains($"needed by {nameof(BoundButBroken)}", m_Refused.Message);
        }

        [Test]
        public void ThenTheErrorGivesTheChainThatLedThere()
        {
            StringAssert.Contains($"{nameof(OptionalBrokenArgument)} -> {nameof(BoundButBroken)} -> {nameof(IMissing)}", m_Refused.Message);
        }
    }
}
