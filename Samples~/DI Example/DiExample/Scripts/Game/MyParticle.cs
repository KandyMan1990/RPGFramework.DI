using RPGFramework.DI;
using UnityEngine;

namespace DiExample.Game
{
    public interface IParticle
    {
        void SetPosition(Vector3 position);
    }

    public class MyParticle : MonoBehaviour, IParticle
    {
        [Inject]
        public void Construct(IScoreManager scoreManager)
        {
            Debug.Log($"ScoreManager: {scoreManager.Score}");
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }
    }
}