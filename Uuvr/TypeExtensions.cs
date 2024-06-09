using System;
using System.Linq;
using System.Reflection;

namespace Uuvr;

public static class TypeExtensions
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public |
                                       BindingFlags.Static;

    private static MemberInfo? GetAnyMember(this Type type, string name)
    {
        return type.GetMember(name, Flags).FirstOrDefault() ??
               type.BaseType?.GetMember(name, Flags).FirstOrDefault() ??
               type.BaseType?.BaseType?.GetMember(name, Flags).FirstOrDefault();
    }

    public static T? GetValue<T>(this object obj, string name)
    {
        return obj.GetType().GetAnyMember(name) switch
        {
            FieldInfo field => (T)field.GetValue(obj),
            PropertyInfo property => (T)property.GetValue(obj, null),
            _ => default(T?)
        };
    }

    public static void SetValue(this object obj, string name, object value)
    {
        switch (obj.GetType().GetAnyMember(name))
        {
            case FieldInfo field:
                field.SetValue(obj, value);
                break;
            case PropertyInfo property:
                property.SetValue(obj, value, null);
                break;
        }
    }
}
