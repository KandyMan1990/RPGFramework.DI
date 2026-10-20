using RPGFramework.DI;

namespace DiExample
{
    public sealed class DiGlobalInstaller : GlobalInstallerBase
    {
        public override void InstallBindings(IDIContainer container)
        {
            container.BindSingleton<IScoreManager, ScoreManager>();
        }
    }
}