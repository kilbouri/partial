using Partial.Core;
using Shouldly;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Partial.SystemTextJson.Tests;

/// <summary>
/// Class responsible for testing that the deserialization of properties is tracked as Defined when case insensitivity is enabled.
/// </summary>
[TestFixture]
public class CaseInsensitiveDeserializationPartialTests
{
    /// <summary>
    /// Generate a local serializer option set to allow for usage of the customer SystemTextJson converter.
    /// </summary>
    private JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true
    };

    public CaseInsensitiveDeserializationPartialTests()
    {
        SerializerOptions.Converters.Add(new PartialJsonConverter());
    }

    [Test, Category("deserialization")]
    public void PartialModelDeserializesCorrectProperties()
    {
        // Arrange
        var inboundJson = """
            {
                "Name": "John Doe",
                "Age": 15,
                "BankBalance": 125.25
            }
            """;

        // Act
        var model = JsonSerializer.Deserialize<TestUser>(inboundJson, SerializerOptions);

        // Assert
        model.ShouldNotBeNull();
        model.IsDefined(x => x.Name).ShouldBeTrue();
        model.IsDefined(x => x.Age).ShouldBeTrue();
        model.IsDefined(x => x.BankBalance).ShouldBeTrue();
    }

    [Test, Category("deserialization")]
    public void PartialModelAllCapsDeserializesCorrectProperties()
    {
        // Arrange
        var inboundJson = """
            {
                "NAME": "John Doe",
                "AGE": 15,
                "BANKBALANCE": 125.25
            }
            """;

        // Act
        var model = JsonSerializer.Deserialize<TestUser>(inboundJson, SerializerOptions);

        // Assert
        model.ShouldNotBeNull();
        model.IsDefined(x => x.Name).ShouldBeTrue();
        model.IsDefined(x => x.Age).ShouldBeTrue();
        model.IsDefined(x => x.BankBalance).ShouldBeTrue();
    }

    [Test, Category("deserialization")]
    public void PartialModelAllLowerDeserializesCorrectProperties()
    {
        // Arrange
        var inboundJson = """
            {
                "name": "John Doe",
                "age": 15,
                "bankbalance": 125.25
            }
            """;

        // Act
        var model = JsonSerializer.Deserialize<TestUser>(inboundJson, SerializerOptions);

        // Assert
        model.ShouldNotBeNull();
        model.IsDefined(x => x.Name).ShouldBeTrue();
        model.IsDefined(x => x.Age).ShouldBeTrue();
        model.IsDefined(x => x.BankBalance).ShouldBeTrue();
    }


    [Test, Category("deserialization")]
    public void PartialModelWithMissingPropertiesDeserializesCorrectProperties()
    {
        // Arrange
        var inboundJson = """
            {
                "Age": 15,
                "BankBalance": 125.25
            }
            """;

        // Act
        var model = JsonSerializer.Deserialize<TestUser>(inboundJson, SerializerOptions);

        // Assert
        model.ShouldNotBeNull();
        model.IsDefined(x => x.Name).ShouldBeFalse();
        model.IsDefined(x => x.Age).ShouldBeTrue();
        model.IsDefined(x => x.BankBalance).ShouldBeTrue();
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