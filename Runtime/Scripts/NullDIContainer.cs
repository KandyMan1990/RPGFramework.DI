using System;
using UnityEngine;

namespace RPGFramework.DI
{
    public class NullDIContainer : IDIContainer
    {
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

        INonLazyBinding IDIContainer.BindInterfacesToSelfSingletonIfNotRegistered<TConcrete>()
        {
            return null;
        }

        INonLazyBinding IDIContainer.ForceBindInterfacesToSelfSingleton<TConcrete>()
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

        TInterface IDIContainer.ResolvePrefab<TInterface>(Transform parent)
        {
            return default;
        }

        T IDIContainer.InstantiateAndInject<T>(T prefab, Transform parent)
        {
            return null;
        }
    }
}