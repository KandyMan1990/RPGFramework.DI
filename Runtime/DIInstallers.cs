using System.Threading.Tasks;
using UnityEngine;

namespace RPGFramework.DI
{
    public abstract class DIInstallerBase : ScriptableObject
    {
        public abstract void InstallBindings(IDIContainer container);
    }

    public abstract class GlobalInstallerBase : DIInstallerBase
    {
        public virtual Task Bootstrap(IDIResolver resolver)
        {
            return Task.CompletedTask;
        }
    }

    public abstract class SceneInstallerBase : DIInstallerBase { }
}