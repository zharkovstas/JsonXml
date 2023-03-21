using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml;
using Newtonsoft.Json;

namespace JsonXml.Tests;

public class XmlNodeConverterTests
{
    [Test]
    public void Write_GivenNull_WorksLikeJsonNet()
    {
        var expected = JsonConvert.SerializeXmlNode((XmlNode?)null);

        var actual = Write(null);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Write_WorksLikeJsonNet([ValueSource(nameof(GetXmls))] string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);

        var expected = JsonConvert.SerializeXmlNode(document);

        var actual = Write(document);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Write_ThenRead_WorksLikeJsonNet([ValueSource(nameof(GetXmls))] string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);

        var expected = JsonConvert.DeserializeXmlNode(JsonConvert.SerializeXmlNode(document));

        var actual = Read(Write(document));

        Assert.That(actual, Is.Not.Null);
        Assert.That(actual!.OuterXml, Is.EqualTo(expected!.OuterXml));
    }

    [Test]
    public void Read_GivenNull_ReturnsNull()
    {
        var actual = Read("null");

        Assert.That(actual, Is.Null);
    }

    [Test]
    public void Read_WorksLikeJsonNet([ValueSource(nameof(GetJsons))] string json)
    {
        var expected = JsonConvert.DeserializeXmlNode(json);

        var actual = Read(json);

        Assert.That(actual, Is.Not.Null);
        Assert.That(actual!.OuterXml, Is.EqualTo(expected!.OuterXml));
    }

    [Test]
    public void Read_ThenWrite_WorksLikeJsonNet([ValueSource(nameof(GetJsons))] string json)
    {
        var expected = JsonConvert.SerializeXmlNode(JsonConvert.DeserializeXmlNode(json));

        var actual = Write(Read(json));

        Assert.That(actual, Is.EqualTo(expected));
    }

    private static string[] GetXmls()
    {
        return new[] {
            "<root />",
            @"<?xml version=""1.0"" encoding=""UTF-8""?><root />",
            "<root></root>",
            "<root>Text</root>",
            "<root>123</root>",
            "<root>true</root>",
            "<root>false</root>",
            "<root><!-- Comment --></root>",
            @"<root attribute=""value"" />",
            @"<root xml:lang=""en-USa"" />",
            @"<root first=""1"" second=""2"" />",
            "<root><child /></root>",
            "<root><first /><second /></root>",
            "<root><element /><element /></root>",
            "<root><element /><!-- Comment --><element /></root>",
            @"<root attribute=""value"">Text</root>",
            "<root>Text<child /></root>",
            "<root>First<child />Second</root>",
            "<root><child />Text</root>",
            "<root><child />Text<child /></root>",
            @"<root><child>First</child><child attribute=""value"">Second</child></root>",
            @"<root>&amp;&lt;&gt;""'</root>",
            "<root><?pi ?></root>",
            @"<!DOCTYPE root SYSTEM ""some.ent""><root></root>",
            "<root><![CDATA[test]]></root>",
            @"<root xmlns:custom=""http://www.example.com/custom""><custom:child /></root>",
            @"<root xmlns:custom=""http://www.example.com/custom"" custom:attribute=""value"" />",
        };
    }

    private static string[] GetJsons()
    {
        return new[] {
            "{}",
            @"{""root"":null}",
            @"{""?xml"":{""@version"":""1.0"",""@encoding"":""UTF-8""},""root"":null}",
            @"{""root"":""""}",
            @"{""root"":""Text""}",
            @"{""root"":{}}",
            @"{""root"":true}",
            @"{""root"":false}",
            @"{""root"":123}",
            @"{""root"":{}}",
            @"{/* Comment */""root"":null}",
            @"{""root"":null/* Comment */}",
            @"{""root"":{/* Comment */}}",
            @"{""root"":{""child"":null}}",
            @"{""root"":{""#text"":""Text""}}",
            @"{""root"":{""@attribute"":null}}",
            @"{""root"":{""@attribute"":""""}}",
            @"{""root"":{""@attribute"":""Text""}}",
            @"{""root"":{""@attribute"":true}}",
            @"{""root"":{""@attribute"":false}}",
            @"{""root"":{""@first"":1,""@second"":2}}",
            @"{""root"":{""first"":null,""second"":null}}",
            @"{""root"":{""element"":[null,null]}}",
            @"{""root"":{""element"":[null,null]/* Comment */}}",
            @"{""root"":""&<>\""'""}",
            @"{""root"":{""?pi"":""""}}",
            @"{""!DOCTYPE"":{""@name"":""root"",""@system"":""some.ent""},""root"":""""}",
            @"{""root"":{""#cdata-section"":""Text""}}",
            @"{""root"":{""@xmlns:custom"":""http://www.example.com/custom"",""custom:child"":null}}",
            @"{""root"":{""@xmlns:custom"":""http://www.example.com/custom"",""@custom:attribute"":null}}",
        };
    }

    private static string Write(XmlNode? node)
    {
        using var stream = new MemoryStream();

        var writerOptions = new JsonWriterOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using var writer = new Utf8JsonWriter(stream, writerOptions);

        var converter = new XmlNodeConverter();

        var serializerOptions = new JsonSerializerOptions
        {
            Converters = { converter },
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        converter.Write(writer, node, serializerOptions);
        writer.Flush();

        stream.Position = 0;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static XmlNode? Read(string json)
    {
        var readerOptions = new JsonReaderOptions
        {
            CommentHandling = JsonCommentHandling.Allow
        };

        var converter = new XmlNodeConverter();

        var serializerOptions = new JsonSerializerOptions
        {
            Converters = { converter },
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        var reader = new Utf8JsonReader(
            new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(json)),
            readerOptions);

        reader.Read();

        return converter.Read(ref reader, typeof(XmlNode), serializerOptions);
    }
}