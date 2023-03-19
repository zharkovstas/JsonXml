using System.Linq;
using System.Xml;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonXml
{
    public partial class XmlNodeConverter : JsonConverter<XmlNode>
    {
        public override void Write(Utf8JsonWriter writer, XmlNode value, JsonSerializerOptions options)
        {
            WriteRecursively(writer, value, writePropertyName: true);
        }

        private static void WriteRecursively(Utf8JsonWriter writer, XmlNode value, bool writePropertyName)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            switch (value.NodeType)
            {
                case XmlNodeType.Document:
                    writer.WriteStartObject();

                    for (var i = 0; i < value.ChildNodes.Count; i++)
                    {
                        WriteRecursively(writer, value.ChildNodes[i], writePropertyName: true);
                    }

                    writer.WriteEndObject();
                    break;
                case XmlNodeType.Element:
                    if (writePropertyName)
                    {
                        writer.WritePropertyName(value.LocalName);
                    }

                    if (value.Attributes.Count == 0)
                    {
                        if (!value.HasChildNodes)
                        {
                            var element = (XmlElement)value;

                            if (element.IsEmpty)
                            {
                                writer.WriteNullValue();
                            }
                            else
                            {
                                writer.WriteStringValue(string.Empty);
                            }

                            break;
                        }

                        if (value.ChildNodes.Count == 1 && value.ChildNodes[0].NodeType == XmlNodeType.Text)
                        {
                            writer.WriteStringValue(value.ChildNodes[0].Value);
                            break;
                        }
                    }

                    writer.WriteStartObject();

                    for (var i = 0; i < value.Attributes.Count; i++)
                    {
                        WriteRecursively(writer, value.Attributes[i], writePropertyName: true);
                    }

                    var nodesGroupedByName = value.ChildNodes
                        .Cast<XmlNode>()
                        .GroupBy(x => x.LocalName)
                        .Select(x => (x.Key, Nodes: x.ToArray()));

                    foreach (var sameNameNodes in nodesGroupedByName)
                    {
                        if (sameNameNodes.Nodes.Length == 1)
                        {
                            WriteRecursively(writer, sameNameNodes.Nodes[0], writePropertyName: true);
                        }
                        else
                        {
                            writer.WritePropertyName(sameNameNodes.Key);
                            writer.WriteStartArray();

                            foreach (var node in sameNameNodes.Nodes)
                            {
                                WriteRecursively(writer, node, writePropertyName: false);
                            }

                            writer.WriteEndArray();
                        }
                    }

                    writer.WriteEndObject();
                    break;
                case XmlNodeType.Attribute:
                    writer.WriteString($"@{value.LocalName}", value.Value);
                    break;
                case XmlNodeType.Text:
                    if (writePropertyName)
                    {
                        writer.WritePropertyName("#text");
                    }
                    writer.WriteStringValue(value.Value);
                    break;
                case XmlNodeType.XmlDeclaration:
                    var declaration = (XmlDeclaration)value;
                    writer.WritePropertyName("?xml");
                    writer.WriteStartObject();

                    var declarationProperties = new (string Name, string Value)[]
                    {
                        ("@version", declaration.Version),
                        ("@encoding", declaration.Encoding),
                        ("@standalone", declaration.Standalone),
                    }
                    .Where(x => !string.IsNullOrEmpty(x.Value));

                    foreach (var property in declarationProperties)
                    {
                        writer.WriteString(property.Name, property.Value);
                    }

                    writer.WriteEndObject();
                    break;
                case XmlNodeType.Comment:
                    writer.WriteCommentValue(value.Value);
                    break;
                case XmlNodeType.ProcessingInstruction:
                    writer.WriteString($"?{value.LocalName}", value.Value);
                    break;
                case XmlNodeType.DocumentType:
                    var documentType = (XmlDocumentType)value;
                    writer.WritePropertyName($"!DOCTYPE");
                    writer.WriteStartObject();

                    var documentTypeProperties = new (string Name, string Value)[]
                    {
                        ("@name", documentType.Name),
                        ("@public", documentType.PublicId),
                        ("@system", documentType.SystemId),
                        ("@internalSubset", documentType.InternalSubset),
                    }
                    .Where(x => !string.IsNullOrEmpty(x.Value));

                    foreach (var property in documentTypeProperties)
                    {
                        writer.WriteString(property.Name, property.Value);
                    }

                    writer.WriteEndObject();
                    break;
                default:
                    break;
            }
        }
    }
}
