using RPGFramework.DI;

namespace DiExample.FrontEnd
{
    public class FrontEndInstaller : SceneInstallerBase
    {
        public override void InstallBindings(IDIContainer container)
        {
            container.BindSingleton<IFrontEndModule, FrontEndModule>();
        }
    }
}