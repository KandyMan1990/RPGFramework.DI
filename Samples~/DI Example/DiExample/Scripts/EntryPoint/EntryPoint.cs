using RPGFramework.DI;
using UnityEngine;

namespace DiExample.EntryPoint
{
    public class EntryPoint : MonoBehaviour
    {
        [SerializeField]
        private GlobalInstallerBase m_GlobalInstaller;

        private void Start()
        {
            Core core = Core.Create(m_GlobalInstaller);

            _ = core.LoadModuleAsync<IFrontEndModule>("FrontEnd");
        }
    }
}