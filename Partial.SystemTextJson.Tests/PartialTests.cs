using Partial.Core;
using Shouldly;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Partial.SystemTextJson.Tests;

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

[TestFixture]
public class PartialTests
{
    /// <summary>
    /// Generate a local serializer option set to allow for usage of the customer SystemTextJson converter.
    /// </summary>
    private JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true
    };

    public PartialTests()
    {
        SerializerOptions.Converters.Add(new PartialJsonConverter());
    }

    [Test]
    public void PartialModelSerializesAllProperties()
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

    [Test]
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

    [Test]
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

        // Assert
        model.ShouldNotBeNull();
        model.IsDefined(x => x.BankBalance).ShouldBeFalse();
        model.IsDefined(x => x.Age).ShouldBeTrue();
        model.IsDefined(x => x.Name).ShouldBeTrue();

        var outboundJson = JsonSerializer.Serialize(model, SerializerOptions);

        using var document = JsonDocument.Parse(outboundJson);
        var propertyNames = document.RootElement.EnumerateObject().Select(x => x.Name).ToHashSet();
        propertyNames.ShouldNotBeEmpty();
        propertyNames.ShouldContain("name");
        propertyNames.ShouldContain("age");
        propertyNames.ShouldNotContain("bankBalance");
    }
}
