using UnityEngine;

namespace RPGFramework.DI.Tests.Types
{
    internal abstract class InjectedComponentBase : MonoBehaviour
    {
        [Inject]
        private IA m_BaseA;

        internal IA BaseA => m_BaseA;
    }

    internal sealed class InjectedComponent : InjectedComponentBase
    {
        [Inject]
        public IA A;
    }

    internal sealed class InjectedAsset : ScriptableObject
    {
        [Inject]
        public IA A;
    }
}
