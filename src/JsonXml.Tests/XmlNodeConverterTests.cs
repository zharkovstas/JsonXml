using System.Text.Json;
using System.Xml;

namespace JsonXml.Tests;

public class XmlNodeConverterTests
{
    [Test]
    public void Write_GivenNull_WorksAsJsonNet()
    {
        var expected = Newtonsoft.Json.JsonConvert.SerializeXmlNode((XmlNode?)null);

        var actual = JsonSerializer.Serialize((XmlNode?)null, CreateOptions());

        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase("<root/>")]
    [TestCase(@"<?xml version=""1.0"" encoding=""UTF-8""?><root/>")]
    [TestCase("<root></root>")]
    [TestCase("<root>Text</root>")]
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
    public void Write_WorksAsJsonNet(string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);

        var expected = Newtonsoft.Json.JsonConvert.SerializeXmlNode(document);

        var actual = JsonSerializer.Serialize(document, CreateOptions());

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Read_GivenNull_ReturnsNull()
    {
        var actual = JsonSerializer.Deserialize<XmlNode>("null", CreateOptions());

        Assert.That(actual, Is.Null);
    }

    [TestCase("{}")]
    [TestCase(@"{""root"":null}")]
    [TestCase(@"{""root"":""""}")]
    [TestCase(@"{""root"":""Text""}")]
    [TestCase(@"{""root"":{}}")]
    [TestCase(@"{""root"":{""child"":null}}")]
    [TestCase(@"{""root"":{""@attribute"": ""value""}}")]
    [TestCase(@"{""root"":{""first"":null,""second"":null}}")]
    public void Read_WorksAsJsonNet(string json)
    {
        var expected = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(json);

        var actual = JsonSerializer.Deserialize<XmlNode>(json, CreateOptions());

        Assert.That(actual, Is.Not.Null);
        Assert.That(actual!.OuterXml, Is.EqualTo(expected!.OuterXml));
    }

    private static JsonSerializerOptions CreateOptions()
    {
        return new JsonSerializerOptions
        {
            Converters = { new XmlNodeConverter() },
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }
}