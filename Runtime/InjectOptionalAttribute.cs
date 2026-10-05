using System;

namespace RPGFramework.DI
{
    // Where [Inject] goes, and on a parameter of a constructor or an injected method, which takes its declared default,
    // or null, when its contract is bound nowhere.
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
    public sealed class InjectOptionalAttribute : Attribute
    {
    }
}