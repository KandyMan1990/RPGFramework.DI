using System;

namespace RPGFramework.DI.Tests.Types
{
    internal interface IWidest
    {
    }

    /// <summary>Says which constructor built it: the count of its parameters.</summary>
    internal sealed class Widest : IWidest
    {
        internal readonly int Used;

        public Widest()
        {
            Used = 0;
        }

        public Widest(IA a)
        {
            Used = 1;
        }

        public Widest(IA a, IB b)
        {
            Used = 2;
        }
    }

    internal interface ISkipsObsolete
    {
    }

    internal sealed class SkipsObsolete : ISkipsObsolete
    {
        internal readonly int Used;

        [Obsolete]
        public SkipsObsolete(IA a, IB b)
        {
            Used = 2;
        }

        public SkipsObsolete(IA a)
        {
            Used = 1;
        }
    }

    internal interface ITied
    {
    }

    internal sealed class Tied : ITied
    {
        public Tied(IA a)
        {
        }

        public Tied(IB b)
        {
        }
    }

    internal interface IAllObsolete
    {
    }

    internal sealed class AllObsolete : IAllObsolete
    {
        [Obsolete]
        public AllObsolete()
        {
        }
    }
}
