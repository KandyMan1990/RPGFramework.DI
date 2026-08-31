using System;
using System.Reflection;

namespace RPGFramework.DI
{
    internal readonly struct InjectMember
    {
        public readonly MemberInfo             Member;
        public readonly bool                   Optional;
        public readonly Type[]                 Dependencies;
        public readonly Action<object, object> Setter;

        internal InjectMember(MemberInfo             member,
                              bool                   optional,
                              Type[]                 dependencies,
                              Action<object, object> setter)
        {
            Member       = member;
            Optional     = optional;
            Dependencies = dependencies;
            Setter       = setter;
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