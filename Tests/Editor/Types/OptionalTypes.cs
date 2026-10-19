namespace RPGFramework.DI.Tests.Types
{
    internal interface IOptionalArguments
    {
    }

    internal sealed class OptionalArguments : IOptionalArguments
    {
        internal const int DEFAULT_RETRIES = 3;

        internal readonly IA       A;
        internal readonly IMissing Missing;
        internal readonly int      Retries;

        public OptionalArguments([InjectOptional] IA a, [InjectOptional] IMissing missing, [InjectOptional] int retries = DEFAULT_RETRIES)
        {
            A       = a;
            Missing = missing;
            Retries = retries;
        }
    }

    internal interface IBoundButBroken
    {
    }

    internal sealed class BoundButBroken : IBoundButBroken
    {
        public BoundButBroken(IMissing missing)
        {
        }
    }

    internal interface IOptionalBrokenArgument
    {
    }

    internal sealed class OptionalBrokenArgument : IOptionalBrokenArgument
    {
        public OptionalBrokenArgument([InjectOptional] IBoundButBroken broken)
        {
        }
    }

    internal interface IRequiredArgument
    {
    }

    internal sealed class RequiredArgument : IRequiredArgument
    {
        public RequiredArgument(IMissing missing)
        {
        }
    }

    internal sealed class MethodArguments
    {
        internal IA       A;
        internal IMissing Missing;
        internal bool     Ran;

        [Inject]
        public void Construct(IA a, [InjectOptional] IMissing missing)
        {
            A       = a;
            Missing = missing;
            Ran     = true;
        }
    }

    internal sealed class OptionalTarget
    {
        [InjectOptional]
        public IMissing MissingField;

        [Inject]
        public IA Required;

        internal bool MethodRan;
        internal bool FoundMethodRan;

        [InjectOptional]
        public IMissing MissingProperty { get; set; }

        [InjectOptional]
        public void Construct(IA a, IMissing missing)
        {
            MethodRan = true;
        }

        [InjectOptional]
        public void ConstructFound(IA a)
        {
            FoundMethodRan = true;
        }
    }

    internal sealed class RequiredTarget
    {
        [Inject]
        public IMissing Missing;
    }
}
