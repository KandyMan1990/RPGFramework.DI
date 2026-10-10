# RPGFramework.DI

A lightweight, Unity-friendly dependency injection container for plain C# classes, MonoBehaviours, ScriptableObjects
and prefabs. It injects through constructors, fields, properties and methods, and any of them can be optional. It is
general purpose: nothing in it is specific to the RPG Framework.

Requires Unity 6000.6 or newer, and depends on nothing else.

---

## Features

- **Transient and singleton bindings**, from a type or from an instance you already have.
- **Interfaces to self**: bind every interface a class implements, optionally the class itself too.
- **Conditional and forced bindings**: bind only if nothing is bound yet, or replace what is.
- **Prefabs**: bind a prefab to an interface and instantiate it with its dependencies injected.
- **Optional injection** on fields, properties and methods, and on constructor and method parameters.
- **Fallback containers**: a scene's container falls back to a global one, so a scene sees both.
- **Non-lazy singletons**, built as soon as they are bound rather than when first resolved.
- **Installers** as ScriptableObjects, with editor menus that write the script and make the asset.

---

## Installation

Install through the Package Manager from the repository's latest release tag.

---

## Usage

### Containers and resolvers

A container has two faces: `IDIContainer`, which installers bind into, and `IDIResolver`, which code resolves from.
`DIContainer` implements both, and every member is an explicit interface implementation, so hold it as the interface
you need:

```csharp
using RPGFramework.DI;

DIContainer  diContainer = new DIContainer();
IDIContainer container   = diContainer;
IDIResolver  resolver    = diContainer;
```

`NullDIContainer` binds and resolves nothing, for holding a place before a real container exists.

### Binding

```csharp
container.BindTransient<IEnemy, Enemy>();                                 // a new instance every resolve
container.BindSingleton<IPlayer, Player>();                               // one instance, made when first resolved
container.BindSingletonFromInstance<IScoreManager>(scoreManager);         // an instance you already have
container.BindInterfacesToSelfSingleton<AudioService>();                  // every interface AudioService implements
container.BindInterfacesAndConcreteToSelfSingleton<AudioService>();       // ...and AudioService itself
container.BindSingleton<ISaveService, SaveService>().AsNonLazy();         // made now, not when first resolved
```

Binding a type that is already bound throws. Two other forms of every binding change that:

```csharp
container.BindSingletonIfNotRegistered<IConfig, DefaultConfig>();         // skipped if IConfig is already bound
container.ForceBindSingleton<ILoginProvider, AppleLoginProvider>();       // replaces whatever ILoginProvider was bound to
```

The forced form suits platform overrides: bind a default everywhere, then force the platform's own over it.

`Unbind<T>()` removes a binding. `Unbind<T>(instance)` removes it and hands the instance back, so the container will no
longer dispose it. `UnbindInterfacesToSelf<T>()` undoes either interfaces-to-self binding.

### Resolving

```csharp
IPlayer player = resolver.Resolve<IPlayer>();
object  menu   = resolver.Resolve(menuType);

resolver.InjectInto(someExistingObject);                                  // fill an object's [Inject] members
```

A type with no binding throws `DIBindingNotFoundException`, whose `ContractType` names it. A circular dependency
throws, listing the chain that loops.

### Constructors

A bound class is built through the constructor with the most parameters, each resolved from the container; private and
internal constructors count. Two constructors sharing the most parameters make the choice ambiguous, which throws when
the class is bound. A constructor marked `[Obsolete]` is never chosen, which is a way to settle a tie.

### Fields, properties and methods

```csharp
public class PlayerController : MonoBehaviour
{
    [Inject] private IWeapon m_Weapon;

    [Inject]
    private void Construct(IInputRouter inputRouter, [InjectOptional] IHaptics haptics)
    {
        ...
    }
}
```

- `[Inject]` marks a field, property or method the container must fill. Private members and those of base classes are
  found too.
- `[InjectOptional]` makes a missing binding acceptable. On a field, property or method, the member is left alone, and a
  method is skipped if any of its dependencies is missing. On a constructor's or an injected method's parameter, the
  parameter takes the default it declares, or null.
- Only a missing binding is forgiven. A dependency that is bound but fails while being built still throws, as any real
  fault should.

### Prefabs

```csharp
container.BindPrefab<IWeapon, Sword>(swordPrefab);

IWeapon sword = resolver.InstantiatePrefab<IWeapon>(parentTransform);
```

`InstantiatePrefab` instantiates the bound prefab under the parent and injects the bound component. For a prefab
nothing is bound to, `InstantiatePrefabAndInject(prefab, parent)` does the same with the component you pass. **Only that
component is injected**, not the prefab's other components.

### Fallback containers

```csharp
sceneContainer.SetFallback(globalContainer);
```

A container that cannot resolve a type asks its fallback, and so on along the chain, so a scene's container sees its own
bindings first and the global ones behind them. A fallback that leads back to the container throws.

### Installers

Installers are ScriptableObjects that bind into a container:

```csharp
public class MyGlobalInstaller : GlobalInstallerBase
{
    public override void InstallBindings(IDIContainer container)
    {
        container.BindSingleton<IScoreManager, ScoreManager>();
    }

    // Optional: work to do once everything is bound, awaited by whoever builds the global container.
    public override Task Bootstrap(IDIResolver resolver)
    {
        return resolver.Resolve<ISaveService>().LoadSettingsAsync();
    }
}
```

- `GlobalInstallerBase` is for game-wide bindings, `SceneInstallerBase` for one scene's.
- `SceneInstallerMonoBehaviour` holds a scene's installer, for whatever builds that scene's container to find.
- **Assets > Create > RPG Framework > DI > Global Installer** (or **Scene Installer**) writes the installer's script
  from a template, then makes its asset once the script has compiled.

### Disposal

`IDIContainer` is `IDisposable`. Disposing a container disposes, latest first, every singleton it built and every
instance bound with `BindSingletonFromInstance` that implements `IDisposable`, then clears its bindings. Transients are
not tracked: whoever resolved one owns it.

```csharp
container.Dispose();
```

### Notes

- Bind, resolve and instantiate on Unity's main thread.
- Reflection is cached per container, and goes with it when it is disposed, so a container rebuilt for each scene
  reflects again over the types it binds.

---

## Using it on its own

**Build one global container for the game's life**, from a global installer, in a first scene that then loads the
next, so nothing is resolved before its bindings exist:

```csharp
using RPGFramework.DI;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Boot : MonoBehaviour
{
    [SerializeField] private GlobalInstallerBase m_GlobalInstaller;

    public static IDIContainer Global { get; private set; }

    private async void Start()
    {
        DIContainer container = new DIContainer();
        Global = container;

        m_GlobalInstaller.InstallBindings(container);
        await m_GlobalInstaller.Bootstrap(container);

        await SceneManager.LoadSceneAsync("Title");
    }
}
```

**Give each scene a container of its own** once it has loaded, from the installer its `SceneInstallerMonoBehaviour`
holds, falling back to the global one:

```csharp
DIContainer  container = new DIContainer();
IDIContainer scene     = container;

Object.FindAnyObjectByType<SceneInstallerMonoBehaviour>().SceneInstaller.InstallBindings(scene);
scene.SetFallback(Boot.Global);
```

Some suggestions for fitting it in:

- **Dispose a scene's container when the scene goes**, so what it built goes with it, and the global one when the game
  quits.
- **Resolve at the edges** — the code that starts a scene, or a component's `[Inject]` method — and pass what was
  resolved on, rather than handing the resolver around to be asked later.
- **Inject what Unity made**: a component already in the scene was built by Unity, not the container, so call
  `InjectInto` on it once the scene's container exists.
- **Bind a default, then force a platform's own** over it, or bind it only `IfNotRegistered` from a shared installer.
- **Make a dependency optional** with `[InjectOptional]` where a feature may simply not be there, so its absence is not
  an error.

---

## In the RPG Framework

- **Core builds both containers.** The game's entry point calls `CoreModuleFactory.Create` with its global installer,
  which binds Core's own services, then the game's, and awaits the installer's `Bootstrap` before the first module, so
  a manifest or a settings file fails there rather than at a first lookup.
- **Each module change builds a new scene container**: Core loads the module's scene, disposes the last scene's
  container with what it built, makes a new one from the scene's `SceneInstallerMonoBehaviour` with the global
  container as its fallback, and resolves the module from it. Each module is bound in its own scene's installer, so it
  is built fresh every time it is entered.
- **`IDIResolver` in the global container is the current scene's**, so something global that resolves later finds the
  scene's bindings as well as its own.
- **Both are disposed when the game quits.**

---

## Sample

**DI Example**: a global installer shared between scenes, scene installers whose bindings stay in their scene, and a
prefab spawned through the resolver with its dependencies injected. Add its three scenes to the build's scene list,
open EntryPoint and press Play.
