using System;

namespace RPGFramework.DI
{
    /// <summary>
    /// Thrown when no binding exists for a contract in a container or any of its fallbacks.
    /// </summary>
    /// <remarks>
    /// Distinct from every other resolution failure, so a caller can tell a missing binding from a fault inside a
    /// constructor, property setter or injected method. <c>[InjectOptional]</c> never suppresses it: an optional
    /// member or parameter is skipped only when its own contract is bound nowhere, checked before anything resolves.
    /// </remarks>
    public sealed class DIBindingNotFoundException : Exception
    {
        /// <summary>The contract that could not be resolved.</summary>
        public Type ContractType { get; }

        internal DIBindingNotFoundException(Type contractType, string message) : base(message)
        {
            ContractType = contractType;
        }
    }
}