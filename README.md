# RPGFramework.DI

A lightweight, Unity-friendly dependency injection container for plain C# classes, MonoBehaviours, ScriptableObjects
and prefabs. It injects through constructors, fields, properties and methods, and any of them can be optional. It is
general purpose: nothing in it is specific to the RPG Framework.

Requires Unity 6000.6 or newer, and depends on nothing else.

---

## Features

- **Transient and singleton bindings**, from a type or from an instance you already have.
- **Interfaces to self**: bind every interface a class implements but .NET's own, optionally the class itself too.
- **Conditional and forced bindings**: bind only if nothing is bound yet, or replace what is.
- **Prefabs**: bind a prefab to an interface and instantiate it with its dependencies injected.
- **Optional injection** on fields, properties and methods, and on constructor and method parameters.
- **Fallback containers**: a scene's container falls back to a global one, so a scene sees both.
- **Non-lazy singletons**, built together once a container is ready rather than when first resolved.
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

`NullDIContainer` is an `IDIContainer` that binds nothing, so a container falling back to it finds nothing there: it
holds a place before a real container exists. It is not a resolver, and `AsNonLazy` on one of its binds does nothing.

A class can take `IDIResolver` as it would any dependency, as every `DIContainer` provides itself: a singleton gets the
container it is bound in, and a transient the container the resolve started from. So binding `IDIResolver` yourself
throws as already bound, and a global singleton's resolver stays the global container whichever scene is loaded.

### Binding

```csharp
container.BindTransient<IEnemy, Enemy>();                                 // a new instance every resolve
container.BindSingleton<IPlayer, Player>();                               // one instance, made when first resolved
container.BindSingletonFromInstance<IScoreManager>(scoreManager);         // an instance you already have
container.BindInterfacesToSelfSingleton<AudioService>();                  // every interface it implements but .NET's own
container.BindInterfacesAndConcreteToSelfSingleton<AudioService>();       // ...and AudioService itself
container.BindSingleton<ISaveService, SaveService>().AsNonLazy();         // made by BuildNonLazy, not when first resolved
```

`AsNonLazy` only marks a singleton; `container.BuildNonLazy()` builds those marked, in order. Call it once every
installer has run and the fallback is set, so a singleton can depend on what is bound after it or in the fallback. In
the editor, resolving from a container that still has one to build logs a warning.

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

A type with no binding throws `DIBindingNotFoundException`, whose `ContractType` names it and whose message says what
needed it: the classes being built, outermost first, or the `[Inject]` member being filled. A circular dependency
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
  found too. A property needs a setter, though a private one will do: in the editor, marking one without throws.
- `[InjectOptional]` makes a missing binding acceptable. On a field, property or method, the member is left alone, and a
  method is skipped if a dependency that is not itself optional is missing. On a constructor's or an injected method's
  parameter, the parameter takes the default it declares, or null.
- Only a missing binding is forgiven. A dependency that is bound but fails while being built still throws, as any real
  fault should.

### Prefabs

```csharp
container.BindPrefab<IWeapon, Sword>(swordPrefab);

IWeapon sword = resolver.InstantiatePrefab<IWeapon>(parentTransform);
```

`InstantiatePrefab` instantiates the bound prefab under the parent and injects the bound component. For a prefab
nothing is bound to, `InstantiatePrefabAndInject(prefab, parent)` does the same with the component you pass. **Only that
component is injected**, not the prefab's other components. Injection follows the instantiation, so on an active prefab
`Awake` and `OnEnable` run before it: use what was injected from `Start` or the `[Inject]` method.

### Fallback containers

```csharp
sceneContainer.SetFallback(globalContainer);
```

A container that cannot resolve a type asks its fallback, and so on along the chain, so a scene's container sees its own
bindings first and the global ones behind them. A fallback that leads back to the container throws.

A singleton is built from the container it is bound in, and that container's fallbacks, whichever container it was
resolved through. A global singleton therefore never sees a scene's bindings: it cannot hold on to objects that go when
the scene does, and what it is built with does not depend on which scene asked for it first. A transient is built from
the container the resolve started from.

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
not tracked: whoever resolved one owns it. Resolving from a disposed container throws `ObjectDisposedException`, so
something still holding a scene's container once the scene has gone is told that, not that a binding is missing.

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
using System.Threading.Tasks;
using RPGFramework.DI;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Boot : MonoBehaviour
{
    [SerializeField] private GlobalInstallerBase m_GlobalInstaller;

    public static IDIContainer Global { get; private set; }

    private void Start()
    {
        // Start cannot be awaited, so the work runs as a task, and anything it throws is logged rather than lost.
        BootAsync().ContinueWith(task => Debug.LogException(task.Exception), TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task BootAsync()
    {
        DIContainer container = new DIContainer();
        Global = container;

        m_GlobalInstaller.InstallBindings(container);
        Global.BuildNonLazy();
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
scene.BuildNonLazy();
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
  which binds the game's services, then Core's own where the game bound none, and awaits the installer's `Bootstrap`
  before the first module, so a manifest or a settings file fails there rather than at a first lookup.
- **Each module gets a scene container of its own**: Core loads the module's scene, makes a container from the scene's
  `SceneInstallerMonoBehaviour` with the global container as its fallback, and resolves the module from it. The game's
  scene installer binds the game's types, and each module then binds its own into the same container from a component
  in its scene, so a module is built fresh every time it is entered, and what it built goes with it.
- **A module's `IDIResolver` is its own scene's container**, as a module is bound there, so what it resolves later finds
  the scene's bindings as well as the global ones.
- **Both are disposed when the game quits.**

---

## Tests

The tests are in `Tests/Editor`, on Unity's Test Framework. They compile only in a project that lists the package as
testable: add it to `testables` in `Packages/manifest.json`,

```json
"testables": [
  "com.rpgframework.di"
]
```

then open **Window › General › Test Runner** and run the **EditMode** tests.

---

## Sample

**DI Example**: a global installer shared between scenes, scene installers whose bindings stay in their scene, and a
prefab spawned through the resolver with its dependencies injected. Add its three scenes to the build's scene list,
open EntryPoint and press Play.
