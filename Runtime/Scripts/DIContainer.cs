using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace RPGFramework.DI
{
    internal enum BindPolicy
    {
        ErrorIfExists,
        SkipIfExists,
        Overwrite
    }

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
        INonLazyBinding BindInterfacesToSelfSingleton<TConcrete>() where TConcrete : class;
        INonLazyBinding BindInterfacesToSelfSingletonIfNotRegistered<TConcrete>() where TConcrete : class;
        INonLazyBinding ForceBindInterfacesToSelfSingleton<TConcrete>() where TConcrete : class;
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

        [ThreadStatic]
        private static Stack<Type> m_ResolutionStack;

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

        bool IDIContainerNode.TryGetBinding(Type type, out Func<object> creator) => m_Bindings.TryGetValue(type, out creator);

        void IDIContainer.BindTransient<TInterface, TConcrete>()
        {
            BindType(typeof(TInterface), typeof(TConcrete), BindPolicy.ErrorIfExists, false);
        }

        INonLazyBinding IDIContainer.BindSingleton<TInterface, TConcrete>()
        {
            return BindType(typeof(TInterface), typeof(TConcrete), BindPolicy.ErrorIfExists, true);
        }

        void IDIContainer.BindSingletonFromInstance<TInterface>(TInterface instance)
        {
            BindInstance(typeof(TInterface), instance, BindPolicy.ErrorIfExists);
        }

        void IDIContainer.BindTransientIfNotRegistered<TInterface, TConcrete>()
        {
            BindType(typeof(TInterface), typeof(TConcrete), BindPolicy.SkipIfExists, false);
        }

        INonLazyBinding IDIContainer.BindSingletonIfNotRegistered<TInterface, TConcrete>()
        {
            return BindType(typeof(TInterface), typeof(TConcrete), BindPolicy.SkipIfExists, true);
        }

        void IDIContainer.BindSingletonFromInstanceIfNotRegistered<TInterface>(TInterface instance)
        {
            BindInstance(typeof(TInterface), instance, BindPolicy.SkipIfExists);
        }

        void IDIContainer.ForceBindTransient<TInterface, TConcrete>()
        {
            BindType(typeof(TInterface), typeof(TConcrete), BindPolicy.Overwrite, false);
        }

        INonLazyBinding IDIContainer.ForceBindSingleton<TInterface, TConcrete>()
        {
            return BindType(typeof(TInterface), typeof(TConcrete), BindPolicy.Overwrite, true);
        }

        void IDIContainer.ForceBindSingletonFromInstance<TInterface>(TInterface instance)
        {
            BindInstance(typeof(TInterface), instance, BindPolicy.Overwrite);
        }

        INonLazyBinding IDIContainer.BindInterfacesToSelfSingleton<TConcrete>()
        {
            return BindInterfacesToSelfSingletonInternal<TConcrete>(BindPolicy.ErrorIfExists);
        }

        INonLazyBinding IDIContainer.BindInterfacesToSelfSingletonIfNotRegistered<TConcrete>()
        {
            return BindInterfacesToSelfSingletonInternal<TConcrete>(BindPolicy.SkipIfExists);
        }

        INonLazyBinding IDIContainer.ForceBindInterfacesToSelfSingleton<TConcrete>()
        {
            return BindInterfacesToSelfSingletonInternal<TConcrete>(BindPolicy.Overwrite);
        }

        T IDIResolver.Resolve<T>()
        {
            return (T)m_DiResolver.Resolve(typeof(T));
        }

        object IDIResolver.Resolve(Type type)
        {
            m_ResolutionStack ??= new Stack<Type>(8);

            if (m_ResolutionStack.Contains(type))
            {
                throw BuildCircularDependencyException(type);
            }

            m_ResolutionStack.Push(type);

            try
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
            finally
            {
                m_ResolutionStack.Pop();
            }
        }

        private static Exception BuildCircularDependencyException(Type repeating)
        {
            var chain = m_ResolutionStack.Reverse().Append(repeating);

            string path = string.Join(" -> ", chain.Select(t => t.Name));

            return new InvalidOperationException($"{nameof(IDIContainer)}::{nameof(BuildCircularDependencyException)} Circular dependency detected:\n{path}");
        }

        private bool HandleExistingBinding(Type type, BindPolicy bindPolicy, string context)
        {
            if (!m_Bindings.ContainsKey(type))
            {
                return true;
            }

            switch (bindPolicy)
            {
                case BindPolicy.ErrorIfExists:
                    throw new ArgumentException($"{nameof(IDIContainer)}::{context} [{type}] has already been bound");
                case BindPolicy.SkipIfExists:
                    return false;
                case BindPolicy.Overwrite:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private INonLazyBinding BindType(Type tInterface, Type tConcrete, BindPolicy bindPolicy, bool singleton)
        {
            if (!HandleExistingBinding(tInterface, bindPolicy, nameof(BindType)))
            {
                return null;
            }

            CacheConstructorAndParams(tConcrete);

            if (!singleton)
            {
                m_Bindings[tInterface] = () => CreateInstance(tConcrete);
                return null;
            }

            Lazy<object> lazy = new Lazy<object>(() => CreateInstance(tConcrete), LazyThreadSafetyMode.None);
            m_Bindings[tInterface] = () => lazy.Value;

            return new NonLazyBinding(lazy);
        }

        private void BindInstance(Type tInterface, object instance, BindPolicy bindPolicy)
        {
            if (!HandleExistingBinding(tInterface, bindPolicy, nameof(BindInstance)))
            {
                return;
            }

            m_Bindings[tInterface] = () => instance;
        }

        private INonLazyBinding BindInterfacesToSelfSingletonInternal<TConcrete>(BindPolicy bindPolicy)
        {
            Type concrete = typeof(TConcrete);
            CacheConstructorAndParams(concrete);

            Lazy<object> lazy = new Lazy<object>(() => CreateInstance(concrete), LazyThreadSafetyMode.None);

            foreach (Type iface in concrete.GetInterfaces())
            {
                if (!HandleExistingBinding(iface, bindPolicy, nameof(BindInterfacesToSelfSingletonInternal)))
                {
                    continue;
                }

                m_Bindings[iface] = () => lazy.Value;
            }

            return new NonLazyBinding(lazy);
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