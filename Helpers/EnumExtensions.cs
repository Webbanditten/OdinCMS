using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace KeplerCMS.Helpers;

public static class EnumExtensions
{
    // Note that we never need to expire these cache items, so we just use ConcurrentDictionary rather than MemoryCache
    private static readonly ConcurrentDictionary<string, string> DisplayNameCache = new ConcurrentDictionary<string, string>();

    public static string Description(this Enum value)
    {
        var key = $"{value.GetType().FullName}.{value}";

        var displayName = DisplayNameCache.GetOrAdd(key, x =>
        {
            var name = (DescriptionAttribute[])value
                .GetType()
                .GetTypeInfo()
                .GetField(value.ToString())
                ?.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return name != null && name.Length > 0 ? name[0].Description : value.ToString();
        });

        return displayName;
    }
}