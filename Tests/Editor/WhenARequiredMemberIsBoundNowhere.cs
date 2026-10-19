using NUnit.Framework;
using RPGFramework.DI.Tests.Types;

namespace RPGFramework.DI.Tests
{
    internal sealed class WhenARequiredMemberIsBoundNowhere
    {
        [Test]
        public void ThenInjectingThrowsNamingIt()
        {
            IDIResolver resolver = new DIContainer();

            DIBindingNotFoundException refused = Assert.Throws<DIBindingNotFoundException>(() => resolver.InjectInto(new RequiredTarget()));

            StringAssert.Contains(nameof(IMissing), refused.Message);
        }
    }
}
