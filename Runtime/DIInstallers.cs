using UnityEngine;

namespace RPGFramework.DI
{
    public abstract class DIInstallerBase : ScriptableObject
    {
        public abstract void InstallBindings(IDIContainer container);
    }
    
    public abstract class GlobalInstallerBase : DIInstallerBase { }
    public abstract class SceneInstallerBase : DIInstallerBase { }
}