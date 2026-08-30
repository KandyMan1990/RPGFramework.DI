using System;

namespace RPGFramework.DI
{
    /// <summary>
    /// Thrown when no binding exists for a contract in a container or any of its fallbacks.
    /// </summary>
    /// <remarks>
    /// This is deliberately distinct from every other resolution failure. <c>[InjectOptional]</c> suppresses
    /// only this exception, so a missing binding stays optional while a genuine fault inside a constructor,
    /// property setter or injected method still surfaces.
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