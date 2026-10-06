using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.DI
{
    public class NullDIContainer : IDIContainer
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
            return null;
        }

        void IDIContainer.BindSingletonFromInstance<TInterface>(TInterface instance)
        {
        }

        void IDIContainer.BindTransientIfNotRegistered<TInterface, TConcrete>()
        {
        }

        INonLazyBinding IDIContainer.BindSingletonIfNotRegistered<TInterface, TConcrete>()
        {
            return null;
        }

        void IDIContainer.BindSingletonFromInstanceIfNotRegistered<TInterface>(TInterface instance)
        {
        }

        void IDIContainer.ForceBindTransient<TInterface, TConcrete>()
        {
        }

        INonLazyBinding IDIContainer.ForceBindSingleton<TInterface, TConcrete>()
        {
            return null;
        }

        void IDIContainer.ForceBindSingletonFromInstance<TInterface>(TInterface instance)
        {
        }

        INonLazyBinding IDIContainer.BindInterfacesToSelfSingleton<TConcrete>()
        {
            return null;
        }

        INonLazyBinding IDIContainer.BindInterfacesAndConcreteToSelfSingleton<TConcrete>()
        {
            return null;
        }

        INonLazyBinding IDIContainer.BindInterfacesToSelfSingletonIfNotRegistered<TConcrete>()
        {
            return null;
        }

        INonLazyBinding IDIContainer.BindInterfacesAndConcreteToSelfSingletonIfNotRegistered<TConcrete>()
        {
            return null;
        }

        INonLazyBinding IDIContainer.ForceBindInterfacesToSelfSingleton<TConcrete>()
        {
            return null;
        }

        INonLazyBinding IDIContainer.ForceBindInterfacesAndConcreteToSelfSingleton<TConcrete>()
        {
            return null;
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

        IReadOnlyDictionary<Type, Func<IDIContainer, object>> IDIContainer.Bindings => m_NoBindings;

        IReadOnlyDictionary<Type, Func<Transform, ResolutionContext, object>> IDIContainer.PrefabBindings => m_NoPrefabBindings;
    }
}