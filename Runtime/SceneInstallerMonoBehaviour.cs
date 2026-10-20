using UnityEngine;

namespace RPGFramework.DI
{
    public sealed class SceneInstallerMonoBehaviour : MonoBehaviour
    {
        [SerializeField]
        private SceneInstallerBase m_SceneInstaller;

        public SceneInstallerBase SceneInstaller => m_SceneInstaller;
    }
}