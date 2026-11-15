using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace RPGFramework.DI
{
    [NoAutoStaticsCleanup]
    public sealed class NullDIContainer : IDIContainer
    {
        private static readonly IReadOnlyDictionary<Type, Func<IDIContainer, object>>                 m_NoBindings       = new Dictionary<Type, Func<IDIContainer, object>>();
        private static readonly IReadOnlyDictionary<Type, Func<Transform, ResolutionContext, object>> m_NoPrefabBindings = new Dictionary<Type, Func<Transform, ResolutionContext, object>>();

        void IDisposable.Dispose()
        {
        }

        void IDIContainer.BindTransient<TInterface, TConcrete>()
        {
        }

        INonLazyBinding IDIContainer.BindSingleton<TInterface, TConcrete>()
        {
            return NonLazyBinding.None;
        }

        void IDIContainer.BindSingletonFromInstance<TInterface>(TInterface instance)
        {
        }

        void IDIContainer.BindTransientIfNotRegistered<TInterface, TConcrete>()
        {
        }

        INonLazyBinding IDIContainer.BindSingletonIfNotRegistered<TInterface, TConcrete>()
        {
            return NonLazyBinding.None;
        }

        void IDIContainer.BindSingletonFromInstanceIfNotRegistered<TInterface>(TInterface instance)
        {
        }

        void IDIContainer.ForceBindTransient<TInterface, TConcrete>()
        {
        }

        INonLazyBinding IDIContainer.ForceBindSingleton<TInterface, TConcrete>()
        {
            return NonLazyBinding.None;
        }

        void IDIContainer.ForceBindSingletonFromInstance<TInterface>(TInterface instance)
        {
        }

        INonLazyBinding IDIContainer.BindInterfacesToSelfSingleton<TConcrete>()
        {
            return NonLazyBinding.None;
        }

        INonLazyBinding IDIContainer.BindInterfacesAndConcreteToSelfSingleton<TConcrete>()
        {
            return NonLazyBinding.None;
        }

        INonLazyBinding IDIContainer.BindInterfacesToSelfSingletonIfNotRegistered<TConcrete>()
        {
            return NonLazyBinding.None;
        }

        INonLazyBinding IDIContainer.BindInterfacesAndConcreteToSelfSingletonIfNotRegistered<TConcrete>()
        {
            return NonLazyBinding.None;
        }

        INonLazyBinding IDIContainer.ForceBindInterfacesToSelfSingleton<TConcrete>()
        {
            return NonLazyBinding.None;
        }

        INonLazyBinding IDIContainer.ForceBindInterfacesAndConcreteToSelfSingleton<TConcrete>()
        {
            return NonLazyBinding.None;
        }

        void IDIContainer.BindPrefab<TInterface, TConcrete>(TConcrete prefab)
        {
        }

        void IDIContainer.BindPrefabIfNotRegistered<TInterface, TConcrete>(TConcrete prefab)
        {
        }

        void IDIContainer.ForceBindPrefab<TInterface, TConcrete>(TConcrete prefab)
        {
        }

        IDIContainer IDIContainer.Fallback => null;

        bool IDIContainer.Unbind<TInterface>()
        {
            return false;
        }

        bool IDIContainer.Unbind<TInterface>(TInterface instance)
        {
            return false;
        }

        bool IDIContainer.UnbindInterfacesToSelf<TConcrete>()
        {
            return false;
        }

        void IDIContainer.SetFallback(IDIContainer fallback)
        {
        }

        void IDIContainer.BuildNonLazy()
        {
        }

        IReadOnlyDictionary<Type, Func<IDIContainer, object>> IDIContainer.Bindings => m_NoBindings;

        IReadOnlyDictionary<Type, Func<Transform, ResolutionContext, object>> IDIContainer.PrefabBindings => m_NoPrefabBindings;
    }
}
