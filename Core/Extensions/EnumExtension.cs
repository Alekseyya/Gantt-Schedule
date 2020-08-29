using System;
using System.Linq;
using System.Reflection;

namespace Core.Extensions
{
    public static class EnumExtension
    {
        public static TAttribute GetAttributeOfType<TAttribute>(this Enum enumValue) where TAttribute : Attribute
        {
            return enumValue.GetType()
                .GetMember(enumValue.ToString())
                .First()
                .GetCustomAttribute<TAttribute>();
        }
    }
}
