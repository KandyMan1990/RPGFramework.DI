using System;
using System.Collections.Generic;
using System.Reflection;

namespace RPGFramework.DI
{
    public class DIContainer
    {
        private readonly Dictionary<Type, Func<object>>    m_Bindings;
        private readonly Dictionary<Type, ConstructorInfo> m_ConstructorCache;
        private readonly Dictionary<Type, Type[]>          m_ConstructorParamsCache;

        private DIContainer m_Fallback;

        public DIContainer()
        {
            m_Bindings               = new Dictionary<Type, Func<object>>();
            m_ConstructorCache       = new Dictionary<Type, ConstructorInfo>();
            m_ConstructorParamsCache = new Dictionary<Type, Type[]>();
        }

        public void SetFallback(DIContainer fallback)
        {
            m_Fallback = fallback;
        }

        public void BindTransient<TInterface, TConcrete>()
        {
            m_Bindings[typeof(TInterface)] = () => CreateInstance(typeof(TConcrete));
        }

        public void BindSingleton<TInterface, TConcrete>()
        {
            Lazy<object> lazy = new Lazy<object>(() => CreateInstance(typeof(TConcrete)));
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
            DIContainer container = this;

            while (container != null)
            {
                if (container.m_Bindings.TryGetValue(type, out Func<object> creator))
                {
                    return creator();
                }

                container = container.m_Fallback;
            }

            throw new Exception($"No binding for type [{type}]");
        }

        private object CreateInstance(Type concreteType)
        {
            if (!m_ConstructorCache.TryGetValue(concreteType, out ConstructorInfo constructor))
            {
                constructor                      = FindBestConstructor(concreteType);
                m_ConstructorCache[concreteType] = constructor;

                ParameterInfo[] parameterInfos = constructor.GetParameters();
                Type[]          parameterTypes = new Type[parameterInfos.Length];

                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    parameterTypes[i] = parameterInfos[i].ParameterType;
                }

                m_ConstructorParamsCache[concreteType] = parameterTypes;
            }

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
    }
}