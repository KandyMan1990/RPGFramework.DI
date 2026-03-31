using System;

namespace RPGFramework.DI
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
    public class InjectOptionalAttribute : Attribute
    {
    }
}