namespace RPGFramework.DI.Tests.Types
{
    internal interface IA
    {
    }

    internal sealed class A : IA
    {
    }

    internal interface IB
    {
    }

    internal sealed class B : IB
    {
        internal readonly IA A;

        public B(IA a)
        {
            A = a;
        }
    }

    internal interface IC
    {
    }

    internal sealed class C : IC
    {
        internal readonly IA A;
        internal readonly IB B;

        public C(IA a, IB b)
        {
            A = a;
            B = b;
        }
    }

    internal interface IEmpty
    {
    }

    internal sealed class Empty : IEmpty
    {
    }

    /// <summary>Bound nowhere.</summary>
    internal interface IMissing
    {
    }

    internal interface ILoop1
    {
    }

    internal sealed class Loop1 : ILoop1
    {
        public Loop1(ILoop2 loop)
        {
        }
    }

    internal interface ILoop2
    {
    }

    internal sealed class Loop2 : ILoop2
    {
        public Loop2(ILoop1 loop)
        {
        }
    }
}
