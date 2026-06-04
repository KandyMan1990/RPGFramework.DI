using System.Threading.Tasks;
using RPGFramework.DI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiExample
{
    public interface ICore
    {
        Task LoadModuleAsync<T>(string sceneName) where T : IModule;
    }

    public class Core : ICore
    {
        private readonly IDIContainer m_GlobalContainer;

        private IDIContainer m_SceneContainer;
        private IDIResolver m_SceneResolver;
        private IModule m_CurrentModule;

        private Core()
        {
            m_GlobalContainer = new DIContainer();
            m_SceneContainer = new NullDIContainer();
            m_CurrentModule = new NullModule();

            Application.quitting += OnApplicationQuit;
        }

        public static Core Create(GlobalInstallerBase globalInstaller)
        {
            Core core = new Core();

            InstallCoreBindings(core, core.m_GlobalContainer);

            globalInstaller.InstallBindings(core.m_GlobalContainer);

            return core;
        }

        public async Task LoadModuleAsync<T>(string sceneName) where T : IModule
        {
            await m_CurrentModule.OnExitAsync();

            await SceneManager.LoadSceneAsync(sceneName);

            m_SceneContainer.Dispose();

            DIContainer sceneContainer = new DIContainer();

            m_SceneContainer = sceneContainer;
            m_SceneResolver = sceneContainer;

            SceneInstallerMonoBehaviour sceneInstallerMonoBehaviour = Object.FindAnyObjectByType<SceneInstallerMonoBehaviour>();
            SceneInstallerBase sceneInstaller = sceneInstallerMonoBehaviour.SceneInstaller;
            sceneInstaller.InstallBindings(m_SceneContainer);

            m_GlobalContainer.ForceBindSingletonFromInstance<IDIResolver>(m_SceneResolver);

            m_SceneContainer.SetFallback(m_GlobalContainer);

            m_CurrentModule = m_SceneResolver.Resolve<T>();

            await m_CurrentModule.OnEnterAsync();
        }

        private static void InstallCoreBindings(ICore core, IDIContainer container)
        {
            container.BindSingletonFromInstance<ICore>(core);
        }

        private void OnApplicationQuit()
        {
            m_SceneContainer.Dispose();
            m_GlobalContainer.Dispose();
        }
    }
}