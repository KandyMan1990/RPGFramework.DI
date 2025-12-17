using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGFramework.DI
{
    internal enum BindPolicy
    {
        ErrorIfExists,
        SkipIfExists,
        Overwrite
    }

    public interface IDIContainer : IDisposable
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
        void            BindPrefab<TInterface, TConcrete>(TConcrete                prefab) where TConcrete : Component, TInterface;
        void            BindPrefabIfNotRegistered<TInterface, TConcrete>(TConcrete prefab) where TConcrete : Component, TInterface;
        void            ForceBindPrefab<TInterface, TConcrete>(TConcrete           prefab) where TConcrete : Component, TInterface;
        TInterface      ResolvePrefab<TInterface>(Transform                        parent                   = null);
        T               InstantiateAndInject<T>(T                                  prefab, Transform parent = null) where T : Component;
    }

    public interface IDIResolver
    {
        T      Resolve<T>();
        object Resolve(Type      type);
        void   InjectInto(object instance);
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
        private readonly Dictionary<Type, Func<object>>            m_Bindings;
        private readonly Dictionary<Type, ConstructorInfo>         m_ConstructorCache;
        private readonly Dictionary<Type, Type[]>                  m_ConstructorParamsCache;
        private readonly Dictionary<Type, InjectInfo>              m_InjectCache;
        private readonly Dictionary<Type, Func<Transform, object>> m_PrefabBindings;
        private readonly List<IDisposable>                         m_Disposables;
        private readonly IDIResolver                               m_DiResolver;

        private IDIContainerNode m_Fallback;

        [ThreadStatic]
        private static Stack<Type> m_ConstructionStack;

        public DIContainer()
        {
            m_Bindings               = new Dictionary<Type, Func<object>>();
            m_ConstructorCache       = new Dictionary<Type, ConstructorInfo>();
            m_ConstructorParamsCache = new Dictionary<Type, Type[]>();
            m_InjectCache            = new Dictionary<Type, InjectInfo>();
            m_PrefabBindings         = new Dictionary<Type, Func<Transform, object>>();
            m_Disposables            = new List<IDisposable>();
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

        void IDIContainer.BindPrefab<TInterface, TConcrete>(TConcrete prefab)
        {
            BindPrefabInternal<TInterface, TConcrete>(prefab, BindPolicy.ErrorIfExists);
        }

        void IDIContainer.BindPrefabIfNotRegistered<TInterface, TConcrete>(TConcrete prefab)
        {
            BindPrefabInternal<TInterface, TConcrete>(prefab, BindPolicy.SkipIfExists);
        }

        void IDIContainer.ForceBindPrefab<TInterface, TConcrete>(TConcrete prefab)
        {
            BindPrefabInternal<TInterface, TConcrete>(prefab, BindPolicy.Overwrite);
        }

        TInterface IDIContainer.ResolvePrefab<TInterface>(Transform parent)
        {
            Type type = typeof(TInterface);

            if (!m_PrefabBindings.TryGetValue(type, out Func<Transform, object> factory))
            {
                throw new InvalidOperationException($"{nameof(IDIContainer)}::{nameof(IDIContainer.ResolvePrefab)} No prefab binding exists for [{type}]");
            }

            return (TInterface)factory(parent);
        }

        T IDIContainer.InstantiateAndInject<T>(T prefab, Transform parent)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            T instance = Object.Instantiate(prefab, parent);

            m_DiResolver.InjectInto(instance);

            return instance;
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

        void IDIResolver.InjectInto(object instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            Type type = instance.GetType();

            CacheConstructorAndParams(type);

            InjectInfo injectInfo = m_InjectCache[type];
            if (!ReferenceEquals(injectInfo, InjectInfo.Empty))
            {
                InjectInto(instance, injectInfo);
            }
        }

        void IDisposable.Dispose()
        {
            for (int i = m_Disposables.Count - 1; i >= 0; i--)
            {
                try
                {
                    m_Disposables[i].Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            m_Disposables.Clear();
            m_Bindings.Clear();
            m_ConstructorCache.Clear();
            m_ConstructorParamsCache.Clear();
        }

        private static Exception BuildCircularDependencyException(Type repeating)
        {
            IEnumerable<Type> chain = m_ConstructionStack.Reverse().Append(repeating);

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

            Lazy<object> lazy = new Lazy<object>(() =>
                                                 {
                                                     object instance = CreateInstance(tConcrete);

                                                     if (instance is IDisposable disposable)
                                                     {
                                                         m_Disposables.Add(disposable);
                                                     }

                                                     return instance;

                                                 },
                                                 LazyThreadSafetyMode.None);
            m_Bindings[tInterface] = () => lazy.Value;

            return new NonLazyBinding(lazy);
        }

        private void BindInstance(Type tInterface, object instance, BindPolicy bindPolicy)
        {
            if (!HandleExistingBinding(tInterface, bindPolicy, nameof(BindInstance)))
            {
                return;
            }

            if (instance is IDisposable disposable)
            {
                m_Disposables.Add(disposable);
            }

            m_Bindings[tInterface] = () => instance;
        }

        private INonLazyBinding BindInterfacesToSelfSingletonInternal<TConcrete>(BindPolicy bindPolicy)
        {
            Type tConcrete = typeof(TConcrete);
            CacheConstructorAndParams(tConcrete);

            Lazy<object> lazy = new Lazy<object>(() =>
                                                 {
                                                     object instance = CreateInstance(tConcrete);

                                                     if (instance is IDisposable disposable)
                                                     {
                                                         m_Disposables.Add(disposable);
                                                     }

                                                     return instance;

                                                 },
                                                 LazyThreadSafetyMode.None);

            foreach (Type iface in tConcrete.GetInterfaces())
            {
                if (iface == typeof(IDisposable))
                {
                    continue;
                }

                if (!HandleExistingBinding(iface, bindPolicy, nameof(BindInterfacesToSelfSingletonInternal)))
                {
                    continue;
                }

                m_Bindings[iface] = () => lazy.Value;
            }

            return new NonLazyBinding(lazy);
        }

        private void BindPrefabInternal<TInterface, TConcrete>(TConcrete prefab, BindPolicy bindPolicy) where TConcrete : Component, TInterface
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            Type interfaceType = typeof(TInterface);
            Type concreteType  = typeof(TConcrete);

            if (!HandleExistingBinding(interfaceType, bindPolicy, nameof(BindPrefabInternal)))
            {
                return;
            }

            CacheConstructorAndParams(concreteType);

            m_PrefabBindings[interfaceType] = parent =>
                                              {
                                                  TConcrete instance = Object.Instantiate(prefab, parent);
                                                  m_DiResolver.InjectInto(instance);

                                                  return instance;
                                              };
        }

        private object CreateInstance(Type concreteType)
        {
            m_ConstructionStack ??= new Stack<Type>(8);

            if (m_ConstructionStack.Contains(concreteType))
            {
                throw BuildCircularDependencyException(concreteType);
            }

            m_ConstructionStack.Push(concreteType);

            try
            {
                ConstructorInfo constructor = m_ConstructorCache[concreteType];
                Type[]          parameters  = m_ConstructorParamsCache[concreteType];
                object[]        args        = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    args[i] = m_DiResolver.Resolve(parameters[i]);
                }

                object instance = constructor.Invoke(args);

                InjectInfo injectInfo = m_InjectCache[concreteType];
                if (!ReferenceEquals(injectInfo, InjectInfo.Empty))
                {
                    InjectInto(instance, injectInfo);
                }

                return instance;
            }
            finally
            {
                m_ConstructionStack.Pop();
            }
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

            if (!m_InjectCache.ContainsKey(concreteType))
            {
                m_InjectCache[concreteType] = BuildInjectInfo(concreteType);
            }
        }

        private static InjectInfo BuildInjectInfo(Type concreteType)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            ConstructorInfo[] badConstructors = concreteType.GetConstructors(flags)
                                                            .Where(c => c.IsDefined(typeof(InjectAttribute), inherit: true))
                                                            .ToArray();

            if (badConstructors.Length > 0)
            {
                throw new InvalidOperationException($"{nameof(IDIContainer)}::{nameof(BuildInjectInfo)} Type [{concreteType}] has [Inject] on a constructor.  Constructor injection is implicit and does not support [Inject]");
            }

            List<InjectMember> members = new List<InjectMember>();

            foreach (FieldInfo field in concreteType.GetFields(flags))
            {
                if (field.IsDefined(typeof(InjectAttribute), true))
                {
                    members.Add(new InjectMember(field, false));
                }
                else if (field.IsDefined(typeof(InjectOptionalAttribute), true))
                {
                    members.Add(new InjectMember(field, true));
                }
            }

            foreach (PropertyInfo property in concreteType.GetProperties(flags))
            {
                if (!property.CanWrite)
                {
                    continue;
                }

                if (property.IsDefined(typeof(InjectAttribute), true))
                {
                    members.Add(new InjectMember(property, false));
                }
                else if (property.IsDefined(typeof(InjectOptionalAttribute), true))
                {
                    members.Add(new InjectMember(property, true));
                }
            }

            foreach (MethodInfo method in concreteType.GetMethods(flags))
            {
                if (method.IsStatic)
                {
                    continue;
                }

                if (method.IsDefined(typeof(InjectAttribute), true))
                {
                    members.Add(new InjectMember(method, false));
                }
                else if (method.IsDefined(typeof(InjectOptionalAttribute), true))
                {
                    members.Add(new InjectMember(method, true));
                }
            }

            return members.Count == 0 ? InjectInfo.Empty : new InjectInfo(members.ToArray());
        }

        private void InjectInto(object instance, InjectInfo injectInfo)
        {
            if (instance is Object unityObj && unityObj == null)
            {
                return;
            }

            foreach (InjectMember entry in injectInfo.Members)
            {
                try
                {
                    switch (entry.Member)
                    {
                        case FieldInfo field:
                            object fieldValue = m_DiResolver.Resolve(field.FieldType);
                            field.SetValue(instance, fieldValue);
                            break;
                        case PropertyInfo property:
                            MethodInfo setter = property.SetMethod;

                            if (setter == null || setter.IsStatic)
                            {
                                break;
                            }

                            object propertyValue = m_DiResolver.Resolve(property.PropertyType);
                            property.SetValue(instance, propertyValue);
                            break;
                        case MethodInfo method:
                            ParameterInfo[] parameters = method.GetParameters();
                            object[]        args       = new object[parameters.Length];

                            for (int i = 0; i < parameters.Length; i++)
                            {
                                ParameterInfo parameter = parameters[i];

                                bool optional = parameter.IsDefined(typeof(InjectOptionalAttribute), true);

                                try
                                {
                                    args[i] = m_DiResolver.Resolve(parameters[i].ParameterType);
                                }
                                catch when (optional)
                                {
                                    args[i] = null;
                                }
                            }

                            method.Invoke(instance, args);
                            break;
                    }
                }
                catch when (entry.Optional)
                {
                }
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