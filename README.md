# RPGFramework.DI

A lightweight, Unity-friendly dependency injection container designed for MonoBehaviours, ScriptableObjects, prefabs, and general C# classes. Supports constructor, field, property, and method injection, including optional injections. 

---

## Features

- **Transient & Singleton Bindings**  
  Bind classes or interfaces as transient or singleton.
- **Prefabs**  
  Bind Unity prefabs and automatically inject dependencies when instantiated.
- **Force / Skip / Conditional Bindings**  
  BindIfNotRegistered, ForceBind, and standard bindings with error checks.
- **Interfaces-to-Self**  
  Automatically bind all interfaces implemented by a concrete class.
- **Optional Injection**  
  Mark fields, properties, or method parameters as optional.
- **Fallback Containers**  
  Support hierarchical containers with fallback lookups.
- **Non-Lazy Singletons**  
  Optionally force singleton instances to be created immediately.

---

## Installation

Install via UPM using the latest release on GitHub. No external dependencies are required.

---

## Usage

### Creating a Container

```csharp
using RPGFramework.DI;

var container = new DIContainer();
```

### Binding Types
```csharp
container.BindTransient<IEnemy, Enemy>(); // New instance each resolve
container.BindSingleton<IPlayer, Player>(); // Singleton instance
container.BindSingletonFromInstance<IScoreManager>(scoreManagerInstance); // Pre-created instance
```

### Conditional / Force Bindings
```csharp
container.BindTransientIfNotRegistered<IService, Service>();
container.ForceBindSingleton<IConfig, Config>();
```

### Interfaces-to-Self
```csharp
container.BindInterfacesToSelfSingleton<MyComponent>();
```

### Prefab Bindings
```csharp
container.BindPrefab<IWeapon, Sword>(swordPrefab);
var swordInstance = container.ResolvePrefab<IWeapon>(parentTransform);
```

### Resolving Instances
```csharp
var player = container.Resolve<IPlayer>();
container.InjectInto(someExistingObject); // Inject dependencies into an existing instance
```

### Installers
```csharp
[CreateAssetMenu(menuName="Installers/GlobalInstaller")]
public class MyGlobalInstaller : GlobalInstallerBase
{
    public override void InstallBindings(IDIContainer container)
    {
        container.BindSingleton<IScoreManager, ScoreManager>();
    }
}
```

### Attributes
```csharp
[Inject] – Required dependency injection on fields, properties, or methods.

[InjectOptional] – Optional dependency; container will not throw if missing.
```

### Scene & Global Installers
SceneInstallerBase – For scene-specific bindings.

GlobalInstallerBase – For game-wide bindings.

SceneInstallerMonoBehaviour – Attach to a scene object to hold a SceneInstaller reference.

### Example
```csharp
public class PlayerController : MonoBehaviour
{
    [Inject] private IWeapon m_Weapon;

    private void Start()
    {
        m_Weapon.Fire();
    }
}
```

### Disposal
The container implements IDisposable. Singleton instances implementing IDisposable will be disposed when the container is disposed.
```csharp
container.Dispose();
```

### Notes
Always perform prefab instantiation and injection on the main Unity thread.

Circular dependencies will throw an exception.

Optional injection failures are silently ignored.
