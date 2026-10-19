using NUnit.Framework;
using RPGFramework.DI.Tests.Types;

namespace RPGFramework.DI.Tests
{
    internal sealed class WhenAContainerFallsBack
    {
        [Test]
        public void ThenWhatItBindsIsBuiltFromItsFallbacksBindings()
        {
            IDIContainer global = new DIContainer();

            global.BindSingleton<IA, A>();
            global.BindTransient<IB, B>();

            IDIContainer scene = new DIContainer();

            scene.SetFallback(global);
            scene.BindTransient<IC, C>();

            C built = (C)((IDIResolver)scene).Resolve<IC>();

            Assert.IsNotNull(built.B);
            Assert.AreSame(((IDIResolver)global).Resolve<IA>(), built.A);
        }
    }
}
