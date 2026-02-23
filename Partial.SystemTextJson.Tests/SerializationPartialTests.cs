using Partial.Core;
using Shouldly;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Partial.SystemTextJson.Tests;

/// <summary>
/// Class handles performing tests to make sure that deserialization and serialization of models acts as expected. JSON properties are loaded into a 
/// local model to check of the value has been tracked as "defined" correctly.
/// </summary>
[TestFixture]
public class SerializationPartialTests
{
    /// <summary>
    /// Generate a local serializer option set to allow for usage of the customer SystemTextJson converter.
    /// </summary>
    private JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true
    };

    public SerializationPartialTests()
    {
        SerializerOptions.Converters.Add(new PartialJsonConverter());
    }

    [Test, Category("deserialization")]
    public void PartialModelDeserializesAllProperties()
    {
        // Arrange
        var inboundJson = """
            {
                "name": "John Doe",
                "age": 15,
                "bankBalance": 125.25
            }
            """;

        // Act
        var model = JsonSerializer.Deserialize<TestUser>(inboundJson, SerializerOptions);

        // Assert
        model.ShouldNotBeNull();
        model.IsDefined(x => x.BankBalance).ShouldBeTrue();
        model.IsDefined(x => x.Age).ShouldBeTrue();
        model.IsDefined(x => x.Name).ShouldBeTrue();
    }

    [Test, Category("deserialization")]
    public void PartialModelDoesNotDefineMissingBankBalance()
    {
        // Arrange
        var inboundJson = """
            {
                "name": "John Doe",
                "age": 15
            }
            """;

        // Act
        var model = JsonSerializer.Deserialize<TestUser>(inboundJson, SerializerOptions);

        // Assert
        model.ShouldNotBeNull();
        model.IsDefined(x => x.BankBalance).ShouldBeFalse();
        model.IsDefined(x => x.Age).ShouldBeTrue();
        model.IsDefined(x => x.Name).ShouldBeTrue();
    }

    [Test, Category("serialization")]
    public void PartialModelSerializesWithDefinedProperties()
    {
        // Arrange
        var inboundJson = """
            {
                "name": "John Doe",
                "age": 15
            }
            """;

        // Act
        var model = JsonSerializer.Deserialize<TestUser>(inboundJson, SerializerOptions);
        var outboundJson = JsonSerializer.Serialize(model, SerializerOptions);

        using var document = JsonDocument.Parse(outboundJson);
        var propertyNames = document.RootElement.EnumerateObject().Select(x => x.Name).ToHashSet();

        // Assert - Deserialize
        model.ShouldNotBeNull();
        model.IsDefined(x => x.BankBalance).ShouldBeFalse();
        model.IsDefined(x => x.Age).ShouldBeTrue();
        model.IsDefined(x => x.Name).ShouldBeTrue();

        // Assert - Serialize
        propertyNames.ShouldNotBeEmpty();
        propertyNames.ShouldContain("name");
        propertyNames.ShouldContain("age");
        propertyNames.ShouldNotContain("bankBalance");
    }
}

/// <summary>
/// Local model used for testing serialization between string and symbol.
/// Uses the <see cref="JsonPropertyNameAttribute"/> to make property checking consistent between steps.
/// </summary>
file class TestUser : Partial<TestUser>
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("age")]
    public int Age { get; set; } = -1;

    [JsonPropertyName("bankBalance")]
    public double BankBalance { get; set; } = 0f;
}
