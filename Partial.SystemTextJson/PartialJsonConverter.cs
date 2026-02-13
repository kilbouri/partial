using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Partial.Core;

namespace Partial.SystemTextJson;

/// <summary>
/// A <see cref="JsonConverter" /> that can allows <see cref="Partial{TSelf}" /> instances to
/// track which properties were given during deserialization.
/// <br />
/// <see cref="Partial{TSelf}.IsDefined" /> and <see cref="Partial{TSelf}.IsUndefined" />
/// will not work properly if the object is deserialized using System.Text.Json's default
/// converter.
/// </summary>
public class PartialJsonConverter : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.BaseType is not null
            && typeToConvert.BaseType.IsGenericType
            && typeToConvert.BaseType.GetGenericTypeDefinition() == typeof(Partial<>)
            && typeToConvert.BaseType.GetGenericArguments().FirstOrDefault() == typeToConvert;
    }

    /// <inheritdoc />
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
#if DEBUG
        // Check typeToConvert extends Partial<TSelf> where TSelf : Partial<TSelf>
        Debug.Assert(typeToConvert.BaseType is not null);
        Debug.Assert(typeToConvert.BaseType.IsGenericType);
        Debug.Assert(typeToConvert.BaseType.GetGenericTypeDefinition() == typeof(Partial<>));
        Debug.Assert(typeToConvert.BaseType.GetGenericArguments().Length == 1);
        Debug.Assert(typeToConvert.BaseType.GetGenericArguments().First() == typeToConvert);
#endif

        return Activator.CreateInstance(typeof(PartialJsonConverterImpl<>).MakeGenericType(typeToConvert)) as JsonConverter;
    }

    /// <summary>
    /// Implementation of <see cref="JsonConverter{T}" /> for <typeparamref name="TPartial" />.
    /// </summary>
    /// <typeparam name="TPartial">The type of partial model. Must extend <see cref="Partial{TSelf}" />.</typeparam>
    private sealed class PartialJsonConverterImpl<TPartial> : JsonConverter<TPartial> where TPartial : Partial<TPartial>
    {
        /// <inheritdoc />
        public override bool HandleNull => false;

        /// <inheritdoc />
        public override TPartial? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var jsonDocument = JsonDocument.ParseValue(ref reader);
            var jsonRoot = jsonDocument.RootElement;

            // Catch common failure ahead of time
            if (jsonRoot.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException($"Expected {JsonValueKind.Object}, got {jsonRoot.ValueKind}");
            }

            var model = Activator.CreateInstance<TPartial>();

#if DEBUG
            Debug.Assert(model.GetType() == typeToConvert);
#endif

            // MS docs are confusing as to whether we should be enumerating the JsonElement here or iterating based on the model's properties instead:
            // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/use-dom#how-to-search-a-jsondocument-and-jsonelement-for-sub-elements
            // > "Use the built-in enumerators (...) rather than doing your own indexing or loops"
            // but then
            // > "Don't do a sequential search on the whole JsonDocument (...). Instead, search (...) based on the known structure of the JSON data."
            //
            // While we should benchmark this if we become concerned about its performance, for the time being I can squash this from O(nm) down to
            // O(n + m) by building a dictionary of the incoming object first.
            var jsonPropertyMap = GetObjectMap(jsonRoot, options.PropertyNameCaseInsensitive);
            foreach (var modelProperty in typeToConvert.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty))
            {
                var propertyName = GetCustomJsonName(modelProperty) ?? modelProperty.Name;
                var lookupName = options.PropertyNameCaseInsensitive ? propertyName.ToUpperInvariant() : propertyName;

                if (jsonPropertyMap.TryGetValue(lookupName, out var jsonProperty))
                {
                    try
                    {
                        var value = jsonProperty.Value.Deserialize(modelProperty.PropertyType, options);
                        model.DefineProperty(modelProperty, value);
                    }
                    catch (JsonException e)
                    {
                        throw new JsonException(
                            e.Message,
                            jsonProperty.Name + e.Path?[1..],
                            e.LineNumber,
                            e.BytePositionInLine
                        );
                    }
                }
            }

            return model;
        }

        public override void Write(Utf8JsonWriter writer, TPartial value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            foreach (var definedProperty in value.EnumerateDefinedProperties())
            {
                var jsonName = GetCustomJsonName(definedProperty)
                    ?? options.PropertyNamingPolicy?.ConvertName(definedProperty.Name)
                    ?? definedProperty.Name;

                var jsonValue = definedProperty.GetValue(value);

                writer.WritePropertyName(jsonName);
                JsonSerializer.Serialize(writer, jsonValue, definedProperty.PropertyType, options);
            }

            writer.WriteEndObject();
        }

        private static string? GetCustomJsonName(PropertyInfo property)
            => (Attribute.GetCustomAttribute(property, typeof(JsonPropertyNameAttribute)) as JsonPropertyNameAttribute)?.Name;

        private static ImmutableDictionary<string, JsonProperty> GetObjectMap(JsonElement root, bool caseInsensitive)
        {
            return root
                    .EnumerateObject()
                    .ToImmutableDictionary(property => caseInsensitive ? property.Name.ToUpperInvariant() : property.Name);
        }
    }
}
