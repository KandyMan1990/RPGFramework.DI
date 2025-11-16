using UnityEngine;

namespace RPGFramework.DI
{
    public class SceneInstallerMonoBehaviour : MonoBehaviour
    {
        [SerializeField]
        private SceneInstallerBase m_SceneInstaller;

        public SceneInstallerBase SceneInstaller => m_SceneInstaller;
    }
}