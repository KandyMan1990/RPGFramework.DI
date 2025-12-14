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
        void BindTransient<TInterface, TConcrete>() where TConcrete : TInterface;
        void BindSingletonLazy<TInterface, TConcrete>() where TConcrete : TInterface;
        void BindSingletonNonLazy<TInterface, TConcrete>() where TConcrete : TInterface;
        void BindSingletonFromInstance<TInterface>(TInterface instance);
        void BindTransientIfNotRegistered<TInterface, TConcrete>() where TConcrete : TInterface;
        void BindSingletonIfNotRegisteredLazy<TInterface, TConcrete>() where TConcrete : TInterface;
        void BindSingletonIfNotRegisteredNonLazy<TInterface, TConcrete>() where TConcrete : TInterface;
        void BindSingletonFromInstanceIfNotRegistered<TInterface>(TInterface instance);
        void ForceBindTransient<TInterface, TConcrete>() where TConcrete : TInterface;
        void ForceBindSingletonLazy<TInterface, TConcrete>() where TConcrete : TInterface;
        void ForceBindSingletonNonLazy<TInterface, TConcrete>() where TConcrete : TInterface;
        void ForceBindSingletonFromInstance<TInterface>(TInterface instance);
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

        void IDIContainer.BindSingletonLazy<TInterface, TConcrete>()
        {
            if (m_Bindings.TryGetValue(typeof(TInterface), out Func<object> _))
            {
                Debug.LogException(new ArgumentException($"{nameof(IDIContainer)}::{nameof(IDIContainer.BindSingletonLazy)} [{typeof(TInterface)}] has already been bound"));
                return;
            }

            BindSingletonLazy<TInterface, TConcrete>();
        }

        void IDIContainer.BindSingletonNonLazy<TInterface, TConcrete>()
        {
            if (m_Bindings.TryGetValue(typeof(TInterface), out Func<object> _))
            {
                Debug.LogException(new ArgumentException($"{nameof(IDIContainer)}::{nameof(IDIContainer.BindSingletonNonLazy)} [{typeof(TInterface)}] has already been bound"));
                return;
            }

            BindSingletonNonLazy<TInterface, TConcrete>();
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

        void IDIContainer.BindSingletonIfNotRegisteredLazy<TInterface, TConcrete>()
        {
            if (m_Bindings.TryGetValue(typeof(TInterface), out Func<object> _))
            {
                return;
            }

            BindSingletonLazy<TInterface, TConcrete>();
        }

        void IDIContainer.BindSingletonIfNotRegisteredNonLazy<TInterface, TConcrete>()
        {
            if (m_Bindings.TryGetValue(typeof(TInterface), out Func<object> _))
            {
                return;
            }

            BindSingletonNonLazy<TInterface, TConcrete>();
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

        void IDIContainer.ForceBindSingletonLazy<TInterface, TConcrete>()
        {
            BindSingletonLazy<TInterface, TConcrete>();
        }

        void IDIContainer.ForceBindSingletonNonLazy<TInterface, TConcrete>()
        {
            BindSingletonNonLazy<TInterface, TConcrete>();
        }

        void IDIContainer.ForceBindSingletonFromInstance<TInterface>(TInterface instance)
        {
            BindSingletonFromInstance<TInterface>(instance);
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

        private void BindSingletonLazy<TInterface, TConcrete>()
        {
            Type concrete = typeof(TConcrete);
            CacheConstructorAndParams(concrete);

            Lazy<object> lazy = new Lazy<object>(() => CreateInstance(concrete), LazyThreadSafetyMode.None);
            m_Bindings[typeof(TInterface)] = () => lazy.Value;
        }

        private void BindSingletonNonLazy<TInterface, TConcrete>()
        {
            Type concrete = typeof(TConcrete);
            CacheConstructorAndParams(concrete);

            object obj = CreateInstance(concrete);
            m_Bindings[typeof(TInterface)] = () => obj;
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
}