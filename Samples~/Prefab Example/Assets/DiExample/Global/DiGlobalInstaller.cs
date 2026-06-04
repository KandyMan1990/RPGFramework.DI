using RPGFramework.DI;

namespace DiExample
{
    public class DiGlobalInstaller : GlobalInstallerBase
    {
        public override void InstallBindings(IDIContainer container)
        {
            container.BindSingleton<IScoreManager, ScoreManager>();
        }
    }
}