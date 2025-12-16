using System;

namespace RPGFramework.DI
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class InjectAttribute : Attribute
    {
    }
}