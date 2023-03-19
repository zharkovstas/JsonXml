using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml;

namespace JsonXml.Tests;

public class XmlNodeConverterTests
{
    [Test]
    public void Write_GivenNull_WorksLikeJsonNet()
    {
        var expected = Newtonsoft.Json.JsonConvert.SerializeXmlNode((XmlNode?)null);

        var actual = Write(null);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase("<root/>")]
    [TestCase(@"<?xml version=""1.0"" encoding=""UTF-8""?><root/>")]
    [TestCase("<root></root>")]
    [TestCase("<root>Text</root>")]
    [TestCase("<root>123</root>")]
    [TestCase("<root>true</root>")]
    [TestCase("<root>false</root>")]
    [TestCase("<root><!-- Comment --></root>")]
    [TestCase(@"<root attribute=""value""/>")]
    [TestCase(@"<root first=""1"" second=""2""/>")]
    [TestCase("<root><child/></root>")]
    [TestCase("<root><first/><second/></root>")]
    [TestCase("<root><element/><element/></root>")]
    [TestCase("<root><element/><!-- Comment --><element/></root>")]
    [TestCase(@"<root attribute=""value"">Text</root>")]
    [TestCase("<root>Text<child/></root>")]
    [TestCase("<root>First<child/>Second</root>")]
    [TestCase("<root><child/>Text</root>")]
    [TestCase("<root><child/>Text<child/></root>")]
    [TestCase(@"<root><child>First</child><child attribute=""value"">Second</child></root>")]
    [TestCase("<root>&amp;&lt;&gt;&quot;'</root>")]
    [TestCase("<root><?pi ?></root>")]
    [TestCase(@"<!DOCTYPE root SYSTEM ""some.ent""><root></root>")]
    public void Write_WorksLikeJsonNet(string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);

        var expected = Newtonsoft.Json.JsonConvert.SerializeXmlNode(document);

        var actual = Write(document);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Read_GivenNull_ReturnsNull()
    {
        var actual = Read("null");

        Assert.That(actual, Is.Null);
    }

    [TestCase("{}")]
    [TestCase(@"{""root"":null}")]
    [TestCase(@"{""?xml"":{""@version"":""1.0"",""@encoding"":""UTF-8""},""root"":null}")]
    [TestCase(@"{""?xml"":{""@version"":""1.0"",""@encoding"":""UTF-8""},""root"":null}")]
    [TestCase(@"{""root"":""""}")]
    [TestCase(@"{""root"":""Text""}")]
    [TestCase(@"{""root"":{}}")]
    [TestCase(@"{""root"":true}")]
    [TestCase(@"{""root"":false}")]
    [TestCase(@"{""root"":123}")]
    [TestCase(@"{""root"":{}}")]
    [TestCase(@"{/* Comment */""root"":null}")]
    [TestCase(@"{""root"":null/* Comment */}")]
    [TestCase(@"{""root"":{/* Comment */}}")]
    [TestCase(@"{""root"":{""child"":null}}")]
    [TestCase(@"{""root"":{""@attribute"":null}}")]
    [TestCase(@"{""root"":{""@attribute"":""""}}")]
    [TestCase(@"{""root"":{""@attribute"":""Text""}}")]
    [TestCase(@"{""root"":{""@attribute"":true}}")]
    [TestCase(@"{""root"":{""@attribute"":false}}")]
    [TestCase(@"{""root"":{""@first"":1,""@second"":2}}")]
    [TestCase(@"{""root"":{""first"":null,""second"":null}}")]
    [TestCase(@"{""root"":{""element"":[null,null]}}")]
    [TestCase(@"{""root"":{""element"":[null,null]/* Comment */}}")]
    public void Read_WorksLikeJsonNet(string json)
    {
        var expected = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(json);

        var actual = Read(json);

        Assert.That(actual, Is.Not.Null);
        Assert.That(actual!.OuterXml, Is.EqualTo(expected!.OuterXml));
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