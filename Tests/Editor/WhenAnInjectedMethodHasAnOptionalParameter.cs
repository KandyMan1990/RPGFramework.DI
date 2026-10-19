using NUnit.Framework;
using RPGFramework.DI.Tests.Types;

namespace RPGFramework.DI.Tests
{
    internal sealed class WhenAnInjectedMethodHasAnOptionalParameter
    {
        private MethodArguments m_Target;

        [SetUp]
        public void SetUp()
        {
            IDIContainer container = new DIContainer();

            container.BindSingleton<IA, A>();

            m_Target = new MethodArguments();

            ((IDIResolver)container).InjectInto(m_Target);
        }

        [Test]
        public void ThenTheMethodRuns()
        {
            Assert.IsTrue(m_Target.Ran);
            Assert.IsNotNull(m_Target.A);
        }

        [Test]
        public void ThenOneBoundNowhereIsNull()
        {
            Assert.IsNull(m_Target.Missing);
        }
    }
}
