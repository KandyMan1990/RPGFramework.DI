using System;
using System.Reflection;

namespace RPGFramework.DI
{
    public sealed class InjectInfo
    {
        public FieldInfo[]    Fields     { get; }
        public PropertyInfo[] Properties { get; }
        public MethodInfo[]   Methods    { get; }

        public InjectInfo(FieldInfo[] fields, PropertyInfo[] properties, MethodInfo[] methods)
        {
            Fields     = fields;
            Properties = properties;
            Methods    = methods;
        }

        public static readonly InjectInfo Empty = new InjectInfo(Array.Empty<FieldInfo>(),
                                                                 Array.Empty<PropertyInfo>(),
                                                                 Array.Empty<MethodInfo>());
    }
}