using RPGFramework.DI;

namespace DiExample.Game
{
    public class GameInstaller : SceneInstallerBase
    {
        public override void InstallBindings(IDIContainer container)
        {
            container.BindSingleton<IGameModule, GameModule>();
        }
    }
}