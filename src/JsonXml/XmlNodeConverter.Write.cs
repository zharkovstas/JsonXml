using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace JsonXml
{
    public partial class XmlNodeConverter : JsonConverter<XmlNode>
    {
        public override void Write(Utf8JsonWriter writer, XmlNode? value, JsonSerializerOptions options)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            WriteRecursively(writer, value, writePropertyName: true);
        }

        private static void WriteRecursively(Utf8JsonWriter writer, XmlNode? value, bool writePropertyName)
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
                        writer.WritePropertyName(value.Name);
                    }

                    if (value.Attributes!.Count == 0)
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

                        if (value.ChildNodes.Count == 1)
                        {
                            var singleChildNode = value.ChildNodes[0]!;
                            if (singleChildNode.NodeType == XmlNodeType.Text)
                            {
                                writer.WriteStringValue(singleChildNode.Value);
                                break;
                            }
                        }
                    }

                    writer.WriteStartObject();

                    for (var i = 0; i < value.Attributes.Count; i++)
                    {
                        WriteRecursively(writer, value.Attributes[i], writePropertyName: true);
                    }

                    var nodesGroupedByName = value.ChildNodes
                        .Cast<XmlNode>()
                        .GroupBy(x => x.Name)
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
                    writer.WriteString($"@{value.Name}", value.Value);
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

                    if (!string.IsNullOrEmpty(declaration.Version))
                    {
                        writer.WriteString("@version", declaration.Version!);
                    }

                    if (!string.IsNullOrEmpty(declaration.Encoding))
                    {
                        writer.WriteString("@encoding", declaration.Encoding!);
                    }

                    if (!string.IsNullOrEmpty(declaration.Standalone))
                    {
                        writer.WriteString("@standalone", declaration.Standalone!);
                    }

                    writer.WriteEndObject();
                    break;
                case XmlNodeType.Comment:
                    if (value.Value != null)
                    {
                        writer.WriteCommentValue(value.Value);
                    }
                    break;
                case XmlNodeType.ProcessingInstruction:
                    writer.WriteString($"?{value.Name}", value.Value);
                    break;
                case XmlNodeType.DocumentType:
                    var documentType = (XmlDocumentType)value;
                    writer.WritePropertyName("!DOCTYPE");
                    writer.WriteStartObject();

                    if (!string.IsNullOrEmpty(documentType.Name))
                    {
                        writer.WriteString("@name", documentType.Name!);
                    }

                    if (!string.IsNullOrEmpty(documentType.PublicId))
                    {
                        writer.WriteString("@public", documentType.PublicId!);
                    }

                    if (!string.IsNullOrEmpty(documentType.SystemId))
                    {
                        writer.WriteString("@system", documentType.SystemId!);
                    }

                    if (!string.IsNullOrEmpty(documentType.InternalSubset))
                    {
                        writer.WriteString("@internalSubset", documentType.InternalSubset!);
                    }

                    writer.WriteEndObject();
                    break;
                case XmlNodeType.CDATA:
                    writer.WriteString($"#cdata-section", value.Value);
                    break;
                case XmlNodeType.SignificantWhitespace:
                    writer.WriteString($"#significant-whitespace", value.Value);
                    break;
                default:
                    break;
            }
        }
    }
}