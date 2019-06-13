using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace EFCore.Scaffolding.Handlebars.Extension.Utils
{
    public static class TypeExtends
    {
        public static bool IsNullableType(this Type type)
        {
            TypeInfo typeInfo = type.GetTypeInfo();
            if (typeInfo.IsValueType)
            {
                if (typeInfo.IsGenericType)
                {
                    return typeInfo.GetGenericTypeDefinition() == typeof(Nullable<>);
                }
                return false;
            }
            return true;
        }
    }
}

