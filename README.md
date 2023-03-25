# JsonXml

A .NET library to convert between XML and JSON depending on nothing but `System.Text.Json`.

Is meant to be a drop-in replacement for `Newtonsoft.Json.Converters.XmlNodeConverter`.

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

* Fix handling multiple processing instructions
* Write a separate set of tests for cases when behavior differs from `Newtonsoft.Json`
* Fix handling multiple comments (`Newtonsoft.Json` behaves poorly)
* Implement handling entity references (`Newtonsoft.Json` fails)
* Make a NuGet package
