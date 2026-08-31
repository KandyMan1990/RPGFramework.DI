using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
        bool                                                                  Unbind<TInterface>();
        bool                                                                  Unbind<TInterface>(TInterface instance);
        bool                                                                  UnbindInterfacesToSelf<TConcrete>() where TConcrete : class;
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

        private static readonly Stack<Type> m_ConstructionStack = new Stack<Type>(8);
        private static readonly MethodInfo m_CreateTypedSetter = typeof(DIContainer).GetMethod(nameof(CreateTypedSetter), BindingFlags.NonPublic | BindingFlags.Static);

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
            for (IDIContainer current = fallback; current != null; current = current.GetFallback)
            {
                if (ReferenceEquals(current, this))
                {
                    throw new InvalidOperationException($"{nameof(DIContainer)}::{nameof(IDIContainer.SetFallback)} The requested fallback leads back to this container, which would make resolution loop forever");
                }
            }

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

        bool IDIContainer.Unbind<TInterface>()
        {
            bool unbound = UnbindContract(typeof(TInterface));

            return unbound;
        }

        bool IDIContainer.Unbind<TInterface>(TInterface instance)
        {
            bool unbound = UnbindContract(typeof(TInterface));

            // Ownership of the instance goes back to the caller, so it comes off the disposal list.
            if (instance is IDisposable disposable)
            {
                int index = IndexOfDisposable(disposable);

                if (index >= 0)
                {
                    m_Disposables.RemoveAt(index);
                }
            }

            return unbound;
        }

        bool IDIContainer.UnbindInterfacesToSelf<TConcrete>()
        {
            // Uses the same contract list the matching bind builds, so the two cannot drift apart. The
            // concrete type is always included: unbinding something that was never bound is a no-op, so one
            // method reverses both BindInterfacesToSelfSingleton and BindInterfacesAndConcreteToSelfSingleton.
            // Anything the container built stays on the disposal list — the container created it, so the
            // container still owns disposing it.
            List<Type> contracts = GetSelfBindableContracts(typeof(TConcrete), true);

            bool unbound = false;

            foreach (Type contract in contracts)
            {
                unbound |= UnbindContract(contract);
            }

            return unbound;
        }

        private bool UnbindContract(Type type)
        {
            bool unbound = m_Bindings.Remove(type) | m_PrefabBindings.Remove(type);

            return unbound;
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
            m_PrefabBindings.Clear();
            m_InjectCache.Clear();
            m_Fallback = null;
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

            throw new DIBindingNotFoundException(type, $"{nameof(DIContainer)}::{nameof(ResolveInternal)} No binding exists for type [{type}] in container or its fallbacks");
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

            throw new DIBindingNotFoundException(type, $"{nameof(DIContainer)}::{nameof(InstantiatePrefabInternal)} No binding exists for type [{type}] in container or its fallbacks");
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
                    m_Bindings.Remove(type);
                    m_PrefabBindings.Remove(type);
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// The lazy behind every singleton binding: builds the instance on first resolve and registers it for
        /// disposal, since the container created it.
        /// </summary>
        /// <remarks>
        /// One lazy can back several contracts, as it does for an interfaces-to-self bind. The factory still
        /// runs once, so the instance is registered for disposal once.
        /// </remarks>
        private ContextualLazy NewSingletonLazy(Type tConcrete)
        {
            return new ContextualLazy(context =>
                                      {
                                          object instance = CreateInstance(tConcrete, context);

                                          if (instance is IDisposable disposable)
                                          {
                                              m_Disposables.Add(disposable);
                                          }

                                          return instance;
                                      });
        }

        private INonLazyBinding BindType(Type tInterface, Type tConcrete, BindPolicy bindPolicy, bool singleton)
        {
            if (!HandleExistingBinding(tInterface, bindPolicy, nameof(BindType)))
            {
                return NonLazyBinding.None;
            }

            CacheConstructorAndParams(tConcrete);

            if (!singleton)
            {
                m_Bindings[tInterface] = context => CreateInstance(tConcrete, context);
                return NonLazyBinding.None;
            }

            ContextualLazy lazy = NewSingletonLazy(tConcrete);

            m_Bindings[tInterface] = context => lazy.GetValue(context);

            return new NonLazyBinding(() => lazy.GetValue(this));
        }

        private void BindInstance(Type tInterface, object instance, BindPolicy bindPolicy)
        {
            if (!HandleExistingBinding(tInterface, bindPolicy, nameof(BindInstance)))
            {
                return;
            }

            if (instance is IDisposable disposable && IndexOfDisposable(disposable) < 0)
            {
                m_Disposables.Add(disposable);
            }

            m_Bindings[tInterface] = context => instance;
        }

        /// <summary>
        /// Where an instance sits in the disposal list, or -1 if it is not tracked.
        /// </summary>
        /// <remarks>
        /// Reference-based, deliberately not <c>List.IndexOf</c> or <c>List.Contains</c>: those use
        /// <see cref="System.Collections.Generic.EqualityComparer{T}.Default"/>, so a type overriding
        /// <c>Equals</c> could match a different-but-equal object — leaving one instance undisposed when
        /// binding, or releasing the wrong one when unbinding.
        /// </remarks>
        private int IndexOfDisposable(IDisposable disposable)
        {
            for (int i = 0; i < m_Disposables.Count; i++)
            {
                if (ReferenceEquals(m_Disposables[i], disposable))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// The contracts an interfaces-to-self bind covers: every interface the type implements that is not a
        /// framework one, optionally plus the concrete type itself.
        /// </summary>
        private static List<Type> GetSelfBindableContracts(Type tConcrete, bool includeConcrete)
        {
            List<Type> contracts = new List<Type>();

            foreach (Type contract in tConcrete.GetInterfaces())
            {
                string nameSpace = contract.Namespace;

                if (nameSpace == "System" || nameSpace != null && nameSpace.StartsWith("System.", StringComparison.Ordinal))
                {
                    continue;
                }

                contracts.Add(contract);
            }

            if (includeConcrete)
            {
                contracts.Add(tConcrete);
            }

            return contracts;
        }

        private INonLazyBinding BindInterfacesToSelfSingletonInternal<TConcrete>(BindPolicy bindPolicy, bool includeConcrete)
        {
            Type tConcrete = typeof(TConcrete);

            List<Type> typesToBind = GetSelfBindableContracts(tConcrete, includeConcrete);

            if (typesToBind.Count == 0)
            {
                throw new InvalidOperationException($"{nameof(DIContainer)}::{nameof(BindInterfacesToSelfSingletonInternal)} Type [{tConcrete}] implements no bindable interfaces, so this call would bind nothing. Use {nameof(IDIContainer.BindInterfacesAndConcreteToSelfSingleton)} to bind the concrete type itself");
            }

            if (bindPolicy == BindPolicy.ErrorIfExists)
            {
                foreach (Type typeToBind in typesToBind)
                {
                    HandleExistingBinding(typeToBind, bindPolicy, nameof(BindInterfacesToSelfSingletonInternal));
                }
            }

            CacheConstructorAndParams(tConcrete);

            ContextualLazy lazy = NewSingletonLazy(tConcrete);

            bool bound = false;

            foreach (Type typeToBind in typesToBind)
            {
                if (!HandleExistingBinding(typeToBind, bindPolicy, nameof(BindInterfacesToSelfSingletonInternal)))
                {
                    continue;
                }

                m_Bindings[typeToBind] = lazy.GetValue;

                bound = true;
            }

            return bound ? new NonLazyBinding(() => lazy.GetValue(this)) : NonLazyBinding.None;
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
                throw new InvalidOperationException($"{nameof(DIContainer)}::{nameof(FindBestConstructor)} Type [{concreteType}] has no usable constructors.");
            }

            if (constructors.Length == 1)
            {
                return constructors[0];
            }

            ConstructorInfo best           = constructors[0];
            int             bestParamCount = best.GetParameters().Length;
            bool            ambiguous      = false;

            for (int i = 1; i < constructors.Length; i++)
            {
                int count = constructors[i].GetParameters().Length;
                if (count > bestParamCount)
                {
                    best           = constructors[i];
                    bestParamCount = count;
                    ambiguous      = false;
                }
                else if (count == bestParamCount)
                {
                    ambiguous = true;
                }
            }

            if (ambiguous)
            {
                throw BuildAmbiguousConstructorException(concreteType, constructors, bestParamCount);
            }

            return best;
        }

        private static Exception BuildAmbiguousConstructorException(Type concreteType, ConstructorInfo[] constructors, int paramCount)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(nameof(DIContainer)).Append("::").Append(nameof(FindBestConstructor));
            builder.Append(" Type [").Append(concreteType).Append("] has more than one constructor taking ").Append(paramCount);
            builder.Append(" parameters, so which one to inject is undecidable. Give the type one widest constructor, or mark the ones the container must not use with [Obsolete]. Candidates:");

            for (int i = 0; i < constructors.Length; i++)
            {
                Type[] parameterTypes = GetParameterTypes(constructors[i]);

                if (parameterTypes.Length != paramCount)
                {
                    continue;
                }

                builder.Append("\n    ").Append(concreteType.Name).Append('(');

                for (int p = 0; p < parameterTypes.Length; p++)
                {
                    if (p > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(parameterTypes[p].Name);
                }

                builder.Append(')');
            }

            InvalidOperationException exception = new InvalidOperationException(builder.ToString());

            return exception;
        }

        private static Type[] GetParameterTypes(MethodBase method)
        {
            ParameterInfo[] parameters     = method.GetParameters();
            Type[]          parameterTypes = new Type[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                parameterTypes[i] = parameters[i].ParameterType;
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
                parameterTypes                         = GetParameterTypes(constructorInfo);
                m_ConstructorParamsCache[concreteType] = parameterTypes;
            }

            EnsureInjectInfo(concreteType);
        }

        private InjectInfo EnsureInjectInfo(Type type)
        {
            if (m_InjectCache.TryGetValue(type, out InjectInfo injectInfo))
            {
                return injectInfo;
            }

            injectInfo          = BuildInjectInfo(type);
            m_InjectCache[type] = injectInfo;

            return injectInfo;
        }

        private static Action<object, object> CreateSetter(PropertyInfo property)
        {
            MethodInfo setter = property.GetSetMethod(true);

            if (setter == null || setter.IsStatic)
            {
                return null;
            }

            Type targetType = property.DeclaringType;
            Type valueType  = property.PropertyType;

            if (targetType == null || targetType.IsValueType || valueType.IsValueType)
            {
                return null;
            }

            MethodInfo factory = m_CreateTypedSetter.MakeGenericMethod(targetType, valueType);

            Action<object, object> boxed = (Action<object, object>)factory.Invoke(null, new object[] { setter });

            return boxed;
        }

        private static Action<object, object> CreateTypedSetter<TTarget, TValue>(MethodInfo setter)
            where TTarget : class
            where TValue : class
        {
            Action<TTarget, TValue> typed = (Action<TTarget, TValue>)Delegate.CreateDelegate(typeof(Action<TTarget, TValue>), setter);

            return (target, value) => typed((TTarget)target, (TValue)value);
        }

        private static bool IsInjectable(MemberInfo member, out bool optional)
        {
            if (member.IsDefined(typeof(InjectAttribute), true))
            {
                optional = false;

                return true;
            }

            optional = member.IsDefined(typeof(InjectOptionalAttribute), true);

            return optional;
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

            List<InjectMember>  members = new List<InjectMember>();
            HashSet<MethodInfo> seen    = new HashSet<MethodInfo>();

            // Walked a level at a time with DeclaredOnly, because GetFields/GetProperties/GetMethods return
            // inherited public and protected members but never private members of base types — so [Inject] on
            // a private base-class field was silently skipped despite InjectAttribute declaring
            // Inherited = true.
            for (Type type = concreteType; type != null && type != typeof(object); type = type.BaseType)
            {
                const BindingFlags declared = flags | BindingFlags.DeclaredOnly;

                foreach (FieldInfo field in type.GetFields(declared))
                {
                    // Fields cannot be overridden, so every one found is distinct storage and needs no
                    // de-duplication.
                    if (IsInjectable(field, out bool fieldOptional))
                    {
                        members.Add(new InjectMember(field, fieldOptional, new[] { field.FieldType }, null));
                    }
                }

                foreach (PropertyInfo property in type.GetProperties(declared))
                {
                    if (!property.CanWrite || !IsInjectable(property, out bool propertyOptional))
                    {
                        continue;
                    }

                    if (!seen.Add(property.GetSetMethod(true).GetBaseDefinition()))
                    {
                        continue;
                    }

                    members.Add(new InjectMember(property, propertyOptional, new[] { property.PropertyType }, CreateSetter(property)));
                }

                foreach (MethodInfo method in type.GetMethods(declared))
                {
                    if (method.IsStatic || !IsInjectable(method, out bool methodOptional))
                    {
                        continue;
                    }

                    // An override and the method it overrides appear at two levels; GetBaseDefinition maps both
                    // to the same declaration so it is injected once, by the most derived version.
                    if (!seen.Add(method.GetBaseDefinition()))
                    {
                        continue;
                    }

                    members.Add(new InjectMember(method, methodOptional, GetParameterTypes(method), null));
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

            InjectInfo injectInfo = EnsureInjectInfo(instance.GetType());

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
                            // No delegate path: a field has no setter method to bind one to.
                            field.SetValue(instance, ResolveInternal(entry.Dependencies[0], context));
                            break;
                        case PropertyInfo property:
                            object value = ResolveInternal(entry.Dependencies[0], context);

                            if (entry.Setter != null)
                            {
                                entry.Setter(instance, value);
                            }
                            else
                            {
                                property.SetValue(instance, value);
                            }

                            break;
                        case MethodInfo method:
                            object[] args = new object[entry.Dependencies.Length];

                            for (int i = 0; i < args.Length; i++)
                            {
                                args[i] = ResolveInternal(entry.Dependencies[i], context);
                            }

                            method.Invoke(instance, args);
                            break;
                    }
                }
                catch (DIBindingNotFoundException) when (entry.Optional)
                {
                    // Only a missing binding makes an optional member optional. Every ResolveInternal call
                    // above runs before its reflective call, so a missing dependency always arrives here
                    // unwrapped; anything else — a throwing property setter, a fault in an injected method
                    // body, a dependency constructor failing — is a real error and must not be swallowed.
                }
            }
        }
    }

    internal sealed class NonLazyBinding : INonLazyBinding
    {
        internal static readonly INonLazyBinding None = new NonLazyBinding(null);

        private readonly Func<object> m_Invoker;

        internal NonLazyBinding(Func<object> invoker)
        {
            m_Invoker = invoker;
        }

        void INonLazyBinding.AsNonLazy()
        {
            _ = m_Invoker?.Invoke();
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