using Partial.Core;
using Shouldly;
using System.Text.Json;

namespace Partial.SystemTextJson.Tests;

file class TestUser : Partial<TestUser>
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; } = -1;
    public double BankBalance { get; set; } = 0f;
}

[TestFixture]
public class PartialTests
{
    [Test]
    public void PartialModelSerializesAllProperties()
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
        var model = JsonSerializer.Deserialize<TestUser>(inboundJson);

        // Assert
        model.ShouldNotBeNull();
        model.IsDefined(x => x.BankBalance).ShouldBeTrue();
        model.IsDefined(x => x.Age).ShouldBeTrue();
        model.IsDefined(x => x.Name).ShouldBeTrue();
    }
}
