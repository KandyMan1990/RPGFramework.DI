using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        void                                                                  BindTransient<TInterface, TConcrete>() where TConcrete : TInterface;
        INonLazyBinding                                                       BindSingleton<TInterface, TConcrete>() where TConcrete : TInterface;
        void                                                                  BindSingletonFromInstance<TInterface>(TInterface instance);
        INonLazyBinding                                                       BindInterfacesToSelfSingleton<TConcrete>() where TConcrete : class;
        INonLazyBinding                                                       BindInterfacesAndConcreteToSelfSingleton<TConcrete>() where TConcrete : class;
        void                                                                  BindPrefab<TInterface, TConcrete>(TConcrete prefab) where TConcrete : Component, TInterface;
        void                                                                  BindTransientIfNotRegistered<TInterface, TConcrete>() where TConcrete : TInterface;
        INonLazyBinding                                                       BindSingletonIfNotRegistered<TInterface, TConcrete>() where TConcrete : TInterface;
        void                                                                  BindSingletonFromInstanceIfNotRegistered<TInterface>(TInterface instance);
        INonLazyBinding                                                       BindInterfacesToSelfSingletonIfNotRegistered<TConcrete>() where TConcrete : class;
        INonLazyBinding                                                       BindInterfacesToAndConcreteSelfSingletonIfNotRegistered<TConcrete>() where TConcrete : class;
        void                                                                  BindPrefabIfNotRegistered<TInterface, TConcrete>(TConcrete prefab) where TConcrete : Component, TInterface;
        void                                                                  ForceBindTransient<TInterface, TConcrete>() where TConcrete : TInterface;
        INonLazyBinding                                                       ForceBindSingleton<TInterface, TConcrete>() where TConcrete : TInterface;
        void                                                                  ForceBindSingletonFromInstance<TInterface>(TInterface instance);
        INonLazyBinding                                                       ForceBindInterfacesToSelfSingleton<TConcrete>() where TConcrete : class;
        INonLazyBinding                                                       ForceBindInterfacesAndConcreteToSelfSingleton<TConcrete>() where TConcrete : class;
        void                                                                  ForceBindPrefab<TInterface, TConcrete>(TConcrete prefab) where TConcrete : Component, TInterface;
        IDIContainer                                                          GetFallback { get; }
        void                                                                  SetFallback(IDIContainer fallback);
        IReadOnlyDictionary<Type, Func<IDIContainer, object>>                 GetBindings       { get; }
        IReadOnlyDictionary<Type, Func<Transform, ResolutionContext, object>> GetPrefabBindings { get; }
    }

    public interface IDIResolver
    {
        T          Resolve<T>();
        object     Resolve(Type                            type);
        TInterface InstantiatePrefab<TInterface>(Transform parent = null);
        void       InjectInto(object                       instance);
        void       InjectInto(object                       instance, IDIContainer context);
        T          InstantiatePrefabAndInject<T>(T         prefab,   Transform    parent = null) where T : Component;
    }

    public interface INonLazyBinding
    {
        void AsNonLazy();
    }

    public class DIContainer : IDIContainer, IDIResolver
    {
        private readonly Dictionary<Type, Func<IDIContainer, object>>                 m_Bindings;
        private readonly Dictionary<Type, Func<Transform, ResolutionContext, object>> m_PrefabBindings;
        private readonly Dictionary<Type, ConstructorInfo>                            m_ConstructorCache;
        private readonly Dictionary<Type, Type[]>                                     m_ConstructorParamsCache;
        private readonly Dictionary<Type, InjectInfo>                                 m_InjectCache;
        private readonly List<IDisposable>                                            m_Disposables;

        private IDIContainer m_Fallback;

        [ThreadStatic]
        private static Stack<Type> m_ConstructionStack;

        public DIContainer()
        {
            m_Bindings               = new Dictionary<Type, Func<IDIContainer, object>>();
            m_PrefabBindings         = new Dictionary<Type, Func<Transform, ResolutionContext, object>>();
            m_ConstructorCache       = new Dictionary<Type, ConstructorInfo>();
            m_ConstructorParamsCache = new Dictionary<Type, Type[]>();
            m_InjectCache            = new Dictionary<Type, InjectInfo>();
            m_Disposables            = new List<IDisposable>();
        }

        IDIContainer IDIContainer.GetFallback => m_Fallback;

        void IDIContainer.SetFallback(IDIContainer fallback)
        {
            m_Fallback = fallback;
        }

        IReadOnlyDictionary<Type, Func<IDIContainer, object>> IDIContainer.GetBindings => m_Bindings;

        IReadOnlyDictionary<Type, Func<Transform, ResolutionContext, object>> IDIContainer.GetPrefabBindings => m_PrefabBindings;

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
            return BindInterfacesToSelfSingletonInternal<TConcrete>(BindPolicy.ErrorIfExists, false);
        }

        INonLazyBinding IDIContainer.BindInterfacesAndConcreteToSelfSingleton<TConcrete>()
        {
            return BindInterfacesToSelfSingletonInternal<TConcrete>(BindPolicy.ErrorIfExists, true);
        }

        INonLazyBinding IDIContainer.BindInterfacesToSelfSingletonIfNotRegistered<TConcrete>()
        {
            return BindInterfacesToSelfSingletonInternal<TConcrete>(BindPolicy.SkipIfExists, false);
        }

        INonLazyBinding IDIContainer.BindInterfacesToAndConcreteSelfSingletonIfNotRegistered<TConcrete>()
        {
            return BindInterfacesToSelfSingletonInternal<TConcrete>(BindPolicy.SkipIfExists, true);
        }

        INonLazyBinding IDIContainer.ForceBindInterfacesToSelfSingleton<TConcrete>()
        {
            return BindInterfacesToSelfSingletonInternal<TConcrete>(BindPolicy.Overwrite, false);
        }

        INonLazyBinding IDIContainer.ForceBindInterfacesAndConcreteToSelfSingleton<TConcrete>()
        {
            return BindInterfacesToSelfSingletonInternal<TConcrete>(BindPolicy.Overwrite, true);
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

        TInterface IDIResolver.InstantiatePrefab<TInterface>(Transform parent)
        {
            ResolutionContext context = new ResolutionContext(this, this);

            return (TInterface)InstantiatePrefabInternal(typeof(TInterface), context, parent);
        }

        T IDIResolver.InstantiatePrefabAndInject<T>(T prefab, Transform parent)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            T instance = Object.Instantiate(prefab, parent);

            InjectIntoInternal(instance, this);

            return instance;
        }

        T IDIResolver.Resolve<T>()
        {
            return (T)ResolveInternal(typeof(T), this);
        }

        object IDIResolver.Resolve(Type type)
        {
            return ResolveInternal(type, this);
        }

        void IDIResolver.InjectInto(object instance)
        {
            InjectIntoInternal(instance, this);
        }

        void IDIResolver.InjectInto(object instance, IDIContainer context)
        {
            InjectIntoInternal(instance, context);
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

            return new InvalidOperationException($"{nameof(DIContainer)}::{nameof(BuildCircularDependencyException)} Circular dependency detected:\n{path}");
        }

        private object ResolveInternal(Type type, IDIContainer context)
        {
            IDIContainer current = context;

            while (current != null)
            {
                if (current.GetBindings.TryGetValue(type, out Func<IDIContainer, object> creator))
                {
                    return creator(context);
                }

                current = current.GetFallback;
            }
            throw new InvalidOperationException($"{nameof(DIContainer)}::{nameof(ResolveInternal)} No binding exists for type [{type}] in container or its fallbacks");
        }

        private object InstantiatePrefabInternal(Type type, ResolutionContext context, Transform parent)
        {
            IDIContainer current = context.Container;

            while (current != null)
            {
                if (current.GetPrefabBindings.TryGetValue(type, out Func<Transform, ResolutionContext, object> prefabFactory))
                {
                    return prefabFactory(parent, context);
                }

                current = current.GetFallback;
            }
            throw new InvalidOperationException($"{nameof(DIContainer)}::{nameof(InstantiatePrefabInternal)} No binding exists for type [{type}] in container or its fallbacks");
        }

        private bool HandleExistingBinding(Type type, BindPolicy bindPolicy, string context)
        {
            if (!m_Bindings.ContainsKey(type) && !m_PrefabBindings.ContainsKey(type))
            {
                return true;
            }

            switch (bindPolicy)
            {
                case BindPolicy.ErrorIfExists:
                    throw new ArgumentException($"{nameof(DIContainer)}::{context} [{type}] has already been bound");
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
                m_Bindings[tInterface] = context => CreateInstance(tConcrete, context);
                return null;
            }

            ContextualLazy lazy = new ContextualLazy(context =>
                                                     {
                                                         object instance = CreateInstance(tConcrete, context);

                                                         if (instance is IDisposable disposable)
                                                         {
                                                             m_Disposables.Add(disposable);
                                                         }

                                                         return instance;
                                                     });

            m_Bindings[tInterface] = context => lazy.GetValue(context);

            return new NonLazyBinding(() => lazy.GetValue(this));
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

            m_Bindings[tInterface] = context => instance;
        }

        private INonLazyBinding BindInterfacesToSelfSingletonInternal<TConcrete>(BindPolicy bindPolicy, bool includeConcrete)
        {
            Type tConcrete = typeof(TConcrete);
            CacheConstructorAndParams(tConcrete);

            ContextualLazy lazy = new ContextualLazy(context =>
                                                     {
                                                         object instance = CreateInstance(tConcrete, context);

                                                         if (instance is IDisposable disposable)
                                                         {
                                                             m_Disposables.Add(disposable);
                                                         }

                                                         return instance;
                                                     });

            List<Type> typesToBind = new List<Type>(tConcrete.GetInterfaces());

            if (includeConcrete)
            {
                typesToBind.Add(tConcrete);
            }

            foreach (Type typeToBind in typesToBind)
            {
                if (typeToBind == typeof(IDisposable))
                {
                    continue;
                }

                if (!HandleExistingBinding(typeToBind, bindPolicy, nameof(BindInterfacesToSelfSingletonInternal)))
                {
                    continue;
                }

                m_Bindings[typeToBind] = ctx => lazy.GetValue(ctx);
            }

            return new NonLazyBinding(() => lazy.GetValue(this));
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

            m_PrefabBindings[interfaceType] = (parent, context) =>
                                              {
                                                  TConcrete instance = Object.Instantiate(prefab, parent);

                                                  context.Resolver.InjectInto(instance, context.Container);

                                                  return instance;
                                              };
        }

        private object CreateInstance(Type concreteType, IDIContainer context)
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
                    args[i] = ResolveInternal(parameters[i], context);
                }

                object instance = constructor.Invoke(args);

                InjectIntoInternal(instance, context);

                return instance;
            }
            finally
            {
                m_ConstructionStack.Pop();
            }
        }

        private static ConstructorInfo FindBestConstructor(Type concreteType)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            ConstructorInfo[] constructors = concreteType.GetConstructors(flags)
                                                         .Where(c => !c.IsDefined(typeof(ObsoleteAttribute), inherit: true))
                                                         .ToArray();

            if (constructors.Length == 0)
            {
                throw new InvalidOperationException($"{nameof(DIContainer)}::{nameof(FindBestConstructor)} Type [{concreteType}] has no usable public constructors");
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
                throw new InvalidOperationException($"{nameof(DIContainer)}::{nameof(BuildInjectInfo)} Type [{concreteType}] has [Inject] on a constructor.  Constructor injection is implicit and does not support [Inject]");
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

        private void InjectIntoInternal(object instance, IDIContainer context)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (instance is Object unityObj && unityObj == null)
            {
                return;
            }

            Type type = instance.GetType();

            CacheConstructorAndParams(type);

            InjectInfo injectInfo = m_InjectCache[type];
            if (ReferenceEquals(injectInfo, InjectInfo.Empty))
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
                            field.SetValue(instance, ResolveInternal(field.FieldType, context));
                            break;
                        case PropertyInfo property:
                            property.SetValue(instance, ResolveInternal(property.PropertyType, context));
                            break;
                        case MethodInfo method:
                            ParameterInfo[] parameters = method.GetParameters();
                            object[]        args       = new object[parameters.Length];

                            for (int i = 0; i < parameters.Length; i++)
                            {
                                args[i] = ResolveInternal(parameters[i].ParameterType, context);
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
        private readonly Func<object> m_Invoker;

        internal NonLazyBinding(Func<object> invoker)
        {
            m_Invoker = invoker;
        }

        void INonLazyBinding.AsNonLazy()
        {
            _ = m_Invoker();
        }
    }

    internal sealed class ContextualLazy
    {
        private object m_Value;
        private bool   m_Created;

        private readonly Func<IDIContainer, object> m_Factory;

        internal ContextualLazy(Func<IDIContainer, object> factory)
        {
            m_Factory = factory;
        }

        internal object GetValue(IDIContainer ctx)
        {
            if (!m_Created)
            {
                m_Value   = m_Factory(ctx);
                m_Created = true;
            }

            return m_Value;
        }
    }
}