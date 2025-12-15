using System.ComponentModel;
using System.Reflection;

namespace EzioHost.Shared.Extensions;

public static class EnumExtension
{
    public static string GetDescription(this Enum value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        var type = value.GetType();

        var name = Enum.GetName(type, value);
        if (name != null)
        {
            var field = type.GetField(name);
            if (field != null)
            {
                var attr = field.GetCustomAttribute<DescriptionAttribute>();
                if (attr != null) return attr.Description;
            }
        }

        return value.ToString();
    }

    public static IEnumerable<TEnum> GetEnumsLessThanOrEqual<TEnum>(this TEnum value) where TEnum : Enum
    {
        return Enum.GetValues(typeof(TEnum))
            .Cast<TEnum>()
            .Where(e => Comparer<TEnum>.Default.Compare(e, value) <= 0);
    }
}