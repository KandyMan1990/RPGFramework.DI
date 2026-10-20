using RPGFramework.DI;
using UnityEngine;

namespace DiExample.EntryPoint
{
    public sealed class EntryPoint : MonoBehaviour
    {
        [SerializeField]
        private GlobalInstallerBase m_GlobalInstaller;

        private void Start()
        {
            ICore core = Core.Create(m_GlobalInstaller);

            _ = core.LoadModuleAsync<IFrontEndModule>("FrontEnd");
        }
    }
}