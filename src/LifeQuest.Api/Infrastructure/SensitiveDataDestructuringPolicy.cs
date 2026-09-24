using System.Collections.Concurrent;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace LifeQuest.Api.Infrastructure;

/// <summary>
/// Framework <c>LoggingBehavior</c> her MediatR isteğini ve yanıtını <c>{@Request}</c> ile olduğu gibi loglar;
/// bu şifre, token ve e-posta gibi verilerin log'a düşmesi demektir. Bu policy LifeQuest tiplerindeki hassas
/// alanları maskeler (loglarda PII maskeleme).
/// </summary>
internal sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private static readonly string[] SecretMarkers = ["password", "token", "secret"];
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]?> Cache = new();

    public bool TryDestructure(object value, ILogEventPropertyValueFactory factory, out LogEventPropertyValue result)
    {
        var properties = Cache.GetOrAdd(value.GetType(), SensitiveTypeProperties);
        if (properties is null)
        {
            result = null!;
            return false;
        }

        var structure = properties.Select(p =>
        {
            var propertyValue = p.GetValue(value);
            var masked = Mask(p.Name, propertyValue);
            return new LogEventProperty(p.Name, masked ?? factory.CreatePropertyValue(propertyValue, destructureObjects: true));
        });

        result = new StructureValue(structure, value.GetType().Name);
        return true;
    }

    private static PropertyInfo[]? SensitiveTypeProperties(Type type)
    {
        if (type.Namespace?.StartsWith("LifeQuest", StringComparison.Ordinal) != true)
            return null;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray();

        return properties.Any(p => IsSecret(p.Name) || IsEmail(p.Name)) ? properties : null;
    }

    private static ScalarValue? Mask(string name, object? value)
    {
        if (value is not string text)
            return null;

        if (IsSecret(name))
            return new ScalarValue("***");

        if (IsEmail(name))
        {
            var at = text.IndexOf('@');
            return new ScalarValue(at > 1 ? $"{text[0]}***{text[at..]}" : "***");
        }

        return null;
    }

    private static bool IsSecret(string name) => SecretMarkers.Any(m => name.Contains(m, StringComparison.OrdinalIgnoreCase));

    private static bool IsEmail(string name) => name.Equals("Email", StringComparison.OrdinalIgnoreCase);
}
