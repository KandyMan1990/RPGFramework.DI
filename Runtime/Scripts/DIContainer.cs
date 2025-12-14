using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace RPGFramework.DI
{
    public interface IDIContainer
    {
        void            BindTransient<TInterface, TConcrete>() where TConcrete : TInterface;
        INonLazyBinding BindSingleton<TInterface, TConcrete>() where TConcrete : TInterface;
        void            BindSingletonFromInstance<TInterface>(TInterface instance);
        void            BindTransientIfNotRegistered<TInterface, TConcrete>() where TConcrete : TInterface;
        INonLazyBinding BindSingletonIfNotRegistered<TInterface, TConcrete>() where TConcrete : TInterface;
        void            BindSingletonFromInstanceIfNotRegistered<TInterface>(TInterface instance);
        void            ForceBindTransient<TInterface, TConcrete>() where TConcrete : TInterface;
        INonLazyBinding ForceBindSingleton<TInterface, TConcrete>() where TConcrete : TInterface;
        void            ForceBindSingletonFromInstance<TInterface>(TInterface instance);
        INonLazyBinding BindInterfacesToSelfSingleton<TConcrete>();
        INonLazyBinding BindInterfacesToSelfSingletonIfNotRegistered<TConcrete>();
        INonLazyBinding ForceBindInterfacesToSelfSingleton<TConcrete>();
    }

    public interface IDIResolver
    {
        T      Resolve<T>();
        object Resolve(Type type);
    }

    public interface IDIContainerNode
    {
        IDIContainerNode GetFallback();
        void             SetFallback(IDIContainerNode fallback);
        bool             TryGetBinding(Type           type, out Func<object> creator);
    }

    public interface INonLazyBinding
    {
        void AsNonLazy();
    }

    public class DIContainer : IDIContainer, IDIResolver, IDIContainerNode
    {
        private readonly Dictionary<Type, Func<object>>    m_Bindings;
        private readonly Dictionary<Type, ConstructorInfo> m_ConstructorCache;
        private readonly Dictionary<Type, Type[]>          m_ConstructorParamsCache;
        private readonly IDIResolver                       m_DiResolver;

        private IDIContainerNode m_Fallback;

        public DIContainer()
        {
            m_Bindings               = new Dictionary<Type, Func<object>>();
            m_ConstructorCache       = new Dictionary<Type, ConstructorInfo>();
            m_ConstructorParamsCache = new Dictionary<Type, Type[]>();
            m_DiResolver             = this;
        }

        IDIContainerNode IDIContainerNode.GetFallback() => m_Fallback;

        void IDIContainerNode.SetFallback(IDIContainerNode fallback)
        {
            m_Fallback = fallback;
        }

        bool IDIContainerNode.TryGetBinding(Type type, out Func<object> creator)
        {
            return m_Bindings.TryGetValue(type, out creator);
        }

        void IDIContainer.BindTransient<TInterface, TConcrete>()
        {
            if (m_Bindings.TryGetValue(typeof(TInterface), out Func<object> _))
            {
                Debug.LogException(new ArgumentException($"{nameof(IDIContainer)}::{nameof(IDIContainer.BindTransient)} [{typeof(TInterface)}] has already been bound"));
                return;
            }

            BindTransient<TInterface, TConcrete>();
        }

        INonLazyBinding IDIContainer.BindSingleton<TInterface, TConcrete>()
        {
            if (m_Bindings.TryGetValue(typeof(TInterface), out Func<object> _))
            {
                Debug.LogException(new ArgumentException($"{nameof(IDIContainer)}::{nameof(IDIContainer.BindSingleton)} [{typeof(TInterface)}] has already been bound"));
                return null;
            }

            return BindSingleton<TInterface, TConcrete>();
        }

        void IDIContainer.BindSingletonFromInstance<TInterface>(TInterface instance)
        {
            if (m_Bindings.TryGetValue(typeof(TInterface), out Func<object> _))
            {
                Debug.LogException(new ArgumentException($"{nameof(IDIContainer)}::{nameof(IDIContainer.BindSingletonFromInstance)} [{typeof(TInterface)}] has already been bound"));
                return;
            }

            BindSingletonFromInstance<TInterface>(instance);
        }

        void IDIContainer.BindTransientIfNotRegistered<TInterface, TConcrete>()
        {
            if (m_Bindings.TryGetValue(typeof(TInterface), out Func<object> _))
            {
                return;
            }

            BindTransient<TInterface, TConcrete>();
        }

        INonLazyBinding IDIContainer.BindSingletonIfNotRegistered<TInterface, TConcrete>()
        {
            if (m_Bindings.TryGetValue(typeof(TInterface), out Func<object> _))
            {
                return null;
            }

            return BindSingleton<TInterface, TConcrete>();
        }

        void IDIContainer.BindSingletonFromInstanceIfNotRegistered<TInterface>(TInterface instance)
        {
            if (m_Bindings.TryGetValue(typeof(TInterface), out Func<object> _))
            {
                return;
            }

            BindSingletonFromInstance<TInterface>(instance);
        }

        void IDIContainer.ForceBindTransient<TInterface, TConcrete>()
        {
            BindTransient<TInterface, TConcrete>();
        }

        INonLazyBinding IDIContainer.ForceBindSingleton<TInterface, TConcrete>()
        {
            return BindSingleton<TInterface, TConcrete>();
        }

        void IDIContainer.ForceBindSingletonFromInstance<TInterface>(TInterface instance)
        {
            BindSingletonFromInstance<TInterface>(instance);
        }

        INonLazyBinding IDIContainer.BindInterfacesToSelfSingleton<TConcrete>()
        {
            return BindInterfacesToSelfSingletonInternal<TConcrete>();
        }

        INonLazyBinding IDIContainer.BindInterfacesToSelfSingletonIfNotRegistered<TConcrete>()
        {
            return BindInterfacesToSelfSingletonIfNotRegisteredInternal<TConcrete>();
        }

        INonLazyBinding IDIContainer.ForceBindInterfacesToSelfSingleton<TConcrete>()
        {
            return ForceBindInterfacesToSelfSingletonInternal<TConcrete>();
        }

        T IDIResolver.Resolve<T>()
        {
            return (T)m_DiResolver.Resolve(typeof(T));
        }

        object IDIResolver.Resolve(Type type)
        {
            IDIContainerNode container = this;

            while (container != null)
            {
                if (container.TryGetBinding(type, out Func<object> creator))
                {
                    return creator();
                }

                container = container.GetFallback();
            }

            throw new InvalidOperationException($"{nameof(IDIResolver)}::{nameof(IDIResolver.Resolve)} No binding exists for type [{type}] in container or its fallbacks");
        }

        private void BindTransient<TInterface, TConcrete>()
        {
            Type concrete = typeof(TConcrete);
            CacheConstructorAndParams(concrete);

            m_Bindings[typeof(TInterface)] = () => CreateInstance(concrete);
        }

        private INonLazyBinding BindSingleton<TInterface, TConcrete>()
        {
            Type concrete = typeof(TConcrete);
            CacheConstructorAndParams(concrete);

            Lazy<object> lazy = new Lazy<object>(() => CreateInstance(concrete), LazyThreadSafetyMode.None);
            m_Bindings[typeof(TInterface)] = () => lazy.Value;

            return new NonLazyBinding(lazy);
        }

        private void BindSingletonFromInstance<TInterface>(TInterface instance)
        {
            m_Bindings[typeof(TInterface)] = () => instance;
        }

        private object CreateInstance(Type concreteType)
        {
            ConstructorInfo constructor = m_ConstructorCache[concreteType];

            Type[]   parameters = m_ConstructorParamsCache[concreteType];
            object[] args       = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                args[i] = m_DiResolver.Resolve(parameters[i]);
            }

            return constructor.Invoke(args);
        }

        private INonLazyBinding BindInterfacesToSelfSingletonInternal<TConcrete>()
        {
            Type concrete = typeof(TConcrete);
            CacheConstructorAndParams(concrete);

            Lazy<object> lazy = new Lazy<object>(() => CreateInstance(concrete), LazyThreadSafetyMode.None);

            foreach (Type iface in concrete.GetInterfaces())
            {
                if (m_Bindings.ContainsKey(iface))
                {
                    Debug.LogException(new ArgumentException($"{nameof(IDIContainer)}::{nameof(BindInterfacesToSelfSingletonInternal)} binding already exists for type [{iface}]"));
                    return null;
                }
                
                m_Bindings[iface] = () => lazy.Value;
            }

            return new NonLazyBinding(lazy);
        }
        
        private INonLazyBinding BindInterfacesToSelfSingletonIfNotRegisteredInternal<TConcrete>()
        {
            Type concrete = typeof(TConcrete);
            CacheConstructorAndParams(concrete);

            Lazy<object> lazy = new Lazy<object>(() => CreateInstance(concrete), LazyThreadSafetyMode.None);

            foreach (Type iface in concrete.GetInterfaces())
            {
                if (m_Bindings.ContainsKey(iface))
                {
                    continue;
                }
                
                m_Bindings[iface] = () => lazy.Value;
            }

            return new NonLazyBinding(lazy);
        }
        
        private INonLazyBinding ForceBindInterfacesToSelfSingletonInternal<TConcrete>()
        {
            Type concrete = typeof(TConcrete);
            CacheConstructorAndParams(concrete);

            Lazy<object> lazy = new Lazy<object>(() => CreateInstance(concrete), LazyThreadSafetyMode.None);

            foreach (Type iface in concrete.GetInterfaces())
            {
                if (m_Bindings.ContainsKey(iface))
                {
                    Debug.LogException(new ArgumentException($"{nameof(IDIContainer)}::{nameof(BindInterfacesToSelfSingletonInternal)} binding already exists for type [{iface}]"));
                    return null;
                }
                
                m_Bindings[iface] = () => lazy.Value;
            }

            return new NonLazyBinding(lazy);
        }

        private static ConstructorInfo FindBestConstructor(Type concreteType)
        {
            ConstructorInfo[] constructors = concreteType.GetConstructors()
                                                         .Where(c => !c.IsDefined(typeof(ObsoleteAttribute), inherit: true))
                                                         .ToArray();

            if (constructors.Length == 0)
            {
                throw new InvalidOperationException($"{nameof(IDIContainer)}::{nameof(FindBestConstructor)} Type [{concreteType}] has no usable public constructors");
            }

            if (constructors.Length == 1)
            {
                return constructors[0];
            }

            ConstructorInfo best           = constructors[0];
            int             bestParamCount = best.GetParameters().Length;

            for (int i = 1; i < constructors.Length; i++)
            {
                int count = constructors[i].GetParameters().Length;
                if (count > bestParamCount)
                {
                    best           = constructors[i];
                    bestParamCount = count;
                }
            }

            return best;
        }

        private static Type[] GetConstructorParams(ConstructorInfo constructor)
        {
            ParameterInfo[] parameterInfos = constructor.GetParameters();
            Type[]          parameterTypes = new Type[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                parameterTypes[i] = parameterInfos[i].ParameterType;
            }

            return parameterTypes;
        }

        private void CacheConstructorAndParams(Type concreteType)
        {
            if (!m_ConstructorCache.TryGetValue(concreteType, out ConstructorInfo constructorInfo))
            {
                constructorInfo                  = FindBestConstructor(concreteType);
                m_ConstructorCache[concreteType] = constructorInfo;
            }

            if (!m_ConstructorParamsCache.TryGetValue(concreteType, out Type[] parameterTypes))
            {
                parameterTypes                         = GetConstructorParams(constructorInfo);
                m_ConstructorParamsCache[concreteType] = parameterTypes;
            }
        }
    }

    internal sealed class NonLazyBinding : INonLazyBinding
    {
        private readonly Lazy<object> m_Lazy;

        internal NonLazyBinding(Lazy<object> lazy)
        {
            m_Lazy = lazy;
        }

        void INonLazyBinding.AsNonLazy()
        {
            _ = m_Lazy.Value;
        }
    }
}