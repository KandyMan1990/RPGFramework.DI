using System;
using System.Reflection;

namespace RPGFramework.DI
{
    internal readonly struct InjectMember
    {
        public readonly MemberInfo Member;
        public readonly bool       Optional;

        internal InjectMember(MemberInfo member, bool optional)
        {
            Member   = member;
            Optional = optional;
        }
    }

    internal sealed class InjectInfo
    {
        internal readonly InjectMember[] Members;

        internal InjectInfo(InjectMember[] members)
        {
            Members = members;
        }

        internal static readonly InjectInfo Empty = new InjectInfo(Array.Empty<InjectMember>());
    }
}