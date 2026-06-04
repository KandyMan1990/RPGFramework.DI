using RPGFramework.DI;
using UnityEngine;

namespace DiExample.Game
{
    public class GameInstaller : SceneInstallerBase
    {
        [SerializeField]
        private MyParticle m_ParticlePrefab;
        
        public override void InstallBindings(IDIContainer container)
        {
            container.BindSingleton<IGameModule, GameModule>();
            
            container.BindPrefab<IParticle, MyParticle>(m_ParticlePrefab);
        }
    }
}