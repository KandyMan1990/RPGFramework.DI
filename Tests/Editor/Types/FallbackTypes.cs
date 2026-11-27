using UnityEngine;

namespace RPGFramework.DI.Tests.Types
{
    /// <summary>A second <see cref="IA" />, a scene's own beside the one a global container binds.</summary>
    internal sealed class OtherA : IA
    {
    }

    /// <summary>An <see cref="IA" /> a prefab carries.</summary>
    internal sealed class ComponentA : MonoBehaviour, IA
    {
    }
}
