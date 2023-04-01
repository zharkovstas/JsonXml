# JsonXml

A .NET library for serializing XML to JSON and deserializing it back, depending only on `System.Text.Json`.

This library is designed to be a drop-in replacement for `Newtonsoft.Json.Converters.XmlNodeConverter`.

## Usage

```csharp
using System.Text.Json;
using System.Xml;

using JsonXml;

var xml = @"<root><child>First</child><child attribute=""value"">Second</child></root>";

var document = new XmlDocument();
document.LoadXml(xml);

var options = new JsonSerializerOptions
{
    Converters = { new XmlNodeConverter() }
};

var json = JsonSerializer.Serialize(document, options);

Console.WriteLine(json);
// {"root":{"child":["First",{"@attribute":"value","#text":"Second"}]}}

Console.WriteLine(JsonSerializer.Deserialize<XmlDocument>(json, options)!.OuterXml == xml);
// True
```

## Known differences between JsonXml and Json.NET aka Newtonsoft.Json

* `JsonXml` does not make use of a custom `json` namespace for controlling array serialization
* `JsonXml` does not make use of special `$`-properties

## TODO

* Implement handling entity references (`Newtonsoft.Json` fails)
* Make a NuGet package
