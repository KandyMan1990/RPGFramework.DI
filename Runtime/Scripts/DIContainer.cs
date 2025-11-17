using System;
using System.Collections.Generic;
using System.Reflection;

namespace RPGFramework.DI
{
    public interface IDIContainer
    {
        IDIContainer GetFallback();
        void         SetFallback(IDIContainer fallback);
        bool         TryGetBinding(Type type, out Func<object> creator);
        void         BindTransient<TInterface, TConcrete>();
        void         BindSingleton<TInterface, TConcrete>();
        void         BindSingletonFromInstance<TInterface, TConcrete>(TConcrete instance) where TConcrete : TInterface;
        T            Resolve<T>();
        object       Resolve(Type type);
    }

    public class DIContainer : IDIContainer
    {
        private readonly Dictionary<Type, Func<object>>    m_Bindings;
        private readonly Dictionary<Type, ConstructorInfo> m_ConstructorCache;
        private readonly Dictionary<Type, Type[]>          m_ConstructorParamsCache;

        private IDIContainer m_Fallback;

        public DIContainer()
        {
            m_Bindings               = new Dictionary<Type, Func<object>>();
            m_ConstructorCache       = new Dictionary<Type, ConstructorInfo>();
            m_ConstructorParamsCache = new Dictionary<Type, Type[]>();
        }

        public void SetFallback(IDIContainer fallback)
        {
            m_Fallback = fallback;
        }

        public IDIContainer GetFallback() => m_Fallback;

        public void BindTransient<TInterface, TConcrete>()
        {
            Type concrete = typeof(TConcrete);
            CacheConstructorAndParams(concrete);

            m_Bindings[typeof(TInterface)] = () => CreateInstance(concrete);
        }

        public bool TryGetBinding(Type type, out Func<object> creator)
        {
            return m_Bindings.TryGetValue(type, out creator);
        }

        public void BindSingleton<TInterface, TConcrete>()
        {
            Type concrete = typeof(TConcrete);
            CacheConstructorAndParams(concrete);

            Lazy<object> lazy = new Lazy<object>(() => CreateInstance(concrete));
            m_Bindings[typeof(TInterface)] = () => lazy.Value;
        }

        public void BindSingletonFromInstance<TInterface, TConcrete>(TConcrete instance) where TConcrete : TInterface
        {
            m_Bindings[typeof(TInterface)] = () => instance;
        }

        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        public object Resolve(Type type)
        {
            IDIContainer container = this;

            while (container != null)
            {
                if (container.TryGetBinding(type, out Func<object> creator))
                {
                    return creator();
                }

                container = container.GetFallback();
            }

            throw new Exception($"No binding for type [{type}]");
        }

        private object CreateInstance(Type concreteType)
        {
            ConstructorInfo constructor = m_ConstructorCache[concreteType];

            Type[]   parameters = m_ConstructorParamsCache[concreteType];
            object[] args       = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                args[i] = Resolve(parameters[i]);
            }

            return constructor.Invoke(args);
        }

        private static ConstructorInfo FindBestConstructor(Type concreteType)
        {
            ConstructorInfo[] constructors = concreteType.GetConstructors();

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
            ConstructorInfo constructorInfo = FindBestConstructor(concreteType);
            m_ConstructorCache[concreteType] = constructorInfo;

            Type[] parameterTypes = GetConstructorParams(constructorInfo);
            m_ConstructorParamsCache[concreteType] = parameterTypes;
        }
    }
}