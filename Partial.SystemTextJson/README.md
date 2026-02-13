# Partial.SystemTextJson

`System.Text.Json` integration for Partial. Provides `PartialJsonConverter` which enables automatic property tracking during `System.Text.Json` deserialization.

## Overview

When deserializing JSON into a partial model using the standard `System.Text.Json` deserializer, the property tracking information is lost because the default converter doesn't notify the `Partial<TSelf>` base class about which properties were actually present in the Json.

`PartialJsonConverter` solves this by implementing a custom `JsonConverterFactory` that properly tracks which properties are deserialized, allowing `IsDefined()` and `IsUndefined()` to work correctly.

## Installation

```bash
dotnet add package Partial.SystemTextJson
```

This will automatically include `Partial.Core` as a dependency.

## Usage

### Basic Setup

Register the converter with your `JsonSerializerOptions`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using Partial.SystemTextJson;

var options = new JsonSerializerOptions
{
    Converters = { new PartialJsonConverter() }
};
```

### Deserialization

```csharp
var json = """
{
    "username": "alice",
    "email": "alice@example.com"
}
""";

var user = JsonSerializer.Deserialize<UserUpdate>(json, options);

// Properties from Json are now tracked
if (user.IsDefined(u => u.Username)) { /* true */ }
if (user.IsDefined(u => u.Email)) { /* true */ }
if (user.IsUndefined(u => u.Age)) { /* true */ }
```

### Serialization

Only properties that were defined are serialized back to Json:

```csharp
var user = JsonSerializer.Deserialize<UserUpdate>(json, options);
var serialized = JsonSerializer.Serialize(user, options);
// Result contains only the properties that were defined:

Console.WriteLine(serialized); // {"username": "alice", "email": "alice@example.com"}
```
