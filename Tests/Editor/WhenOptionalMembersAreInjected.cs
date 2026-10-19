using NUnit.Framework;
using RPGFramework.DI.Tests.Types;

namespace RPGFramework.DI.Tests
{
    internal sealed class WhenOptionalMembersAreInjected
    {
        private OptionalTarget m_Target;

        [SetUp]
        public void SetUp()
        {
            IDIContainer container = new DIContainer();

            container.BindSingleton<IA, A>();

            m_Target = new OptionalTarget();

            ((IDIResolver)container).InjectInto(m_Target);
        }

        [Test]
        public void ThenNothingIsThrown()
        {
            IDIContainer container = new DIContainer();

            container.BindSingleton<IA, A>();

            Assert.DoesNotThrow(() => ((IDIResolver)container).InjectInto(new OptionalTarget()));
        }

        [Test]
        public void ThenAnOptionalFieldBoundNowhereIsLeftUnset()
        {
            Assert.IsNull(m_Target.MissingField);
        }

        [Test]
        public void ThenAnOptionalPropertyBoundNowhereIsLeftUnset()
        {
            Assert.IsNull(m_Target.MissingProperty);
        }

        [Test]
        public void ThenAnOptionalMethodMissingADependencyIsSkipped()
        {
            Assert.IsFalse(m_Target.MethodRan);
        }

        [Test]
        public void ThenOneWithItsDependenciesRuns()
        {
            Assert.IsTrue(m_Target.FoundMethodRan);
        }

        [Test]
        public void ThenARequiredMemberBesideThemIsSet()
        {
            Assert.IsNotNull(m_Target.Required);
        }
    }
}
