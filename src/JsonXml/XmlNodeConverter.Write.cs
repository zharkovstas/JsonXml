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

            WriteNode(writer, value);
        }

        private static void WriteNode(Utf8JsonWriter writer, XmlNode? node)
        {
            if (node == null)
            {
                writer.WriteNullValue();
                return;
            }

            switch (node.NodeType)
            {
                case XmlNodeType.Document:
                    writer.WriteStartObject();

                    for (var i = 0; i < node.ChildNodes.Count; i++)
                    {
                        WritePropertyName(writer, node.ChildNodes[i]);
                        WriteNode(writer, node.ChildNodes[i]);
                    }

                    writer.WriteEndObject();
                    break;
                case XmlNodeType.Element:
                    if (node.Attributes!.Count == 0)
                    {
                        if (!node.HasChildNodes)
                        {
                            var element = (XmlElement)node;

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

                        if (node.ChildNodes.Count == 1)
                        {
                            var singleChildNode = node.ChildNodes[0]!;
                            if (singleChildNode.NodeType == XmlNodeType.Text)
                            {
                                writer.WriteStringValue(singleChildNode.Value);
                                break;
                            }
                        }
                    }

                    writer.WriteStartObject();

                    for (var i = 0; i < node.Attributes.Count; i++)
                    {
                        WritePropertyName(writer, node.Attributes[i]);
                        WriteNode(writer, node.Attributes[i]);
                    }

                    var nodesGroupedByName = node.ChildNodes
                        .Cast<XmlNode>()
                        .GroupBy(x => x.Name)
                        .Select(x => (x.Key, Nodes: x.ToArray()));

                    foreach (var sameNameNodes in nodesGroupedByName)
                    {
                        WritePropertyName(writer, sameNameNodes.Nodes[0]);
                        if (sameNameNodes.Nodes.Length == 1)
                        {
                            WriteNode(writer, sameNameNodes.Nodes[0]);
                        }
                        else
                        {
                            writer.WriteStartArray();

                            foreach (var sameNameNode in sameNameNodes.Nodes)
                            {
                                WriteNode(writer, sameNameNode);
                            }

                            writer.WriteEndArray();
                        }
                    }

                    writer.WriteEndObject();
                    break;
                case XmlNodeType.Attribute:
                    writer.WriteStringValue(node.Value);
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                case XmlNodeType.SignificantWhitespace:
                    writer.WriteStringValue(node.Value);
                    break;
                case XmlNodeType.XmlDeclaration:
                    var declaration = (XmlDeclaration)node;
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
                    if (node.Value != null)
                    {
                        writer.WriteCommentValue(node.Value);
                    }
                    break;
                case XmlNodeType.ProcessingInstruction:
                    writer.WriteStringValue(node.Value);
                    break;
                case XmlNodeType.DocumentType:
                    var documentType = (XmlDocumentType)node;
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
                default:
                    break;
            }
        }

        private static void WritePropertyName(Utf8JsonWriter writer, XmlNode? value)
        {
            if (value == null)
            {
                return;
            }

            switch (value.NodeType)
            {
                case XmlNodeType.Attribute:
                    writer.WritePropertyName($"@{value.Name}");
                    return;
                case XmlNodeType.XmlDeclaration:
                case XmlNodeType.ProcessingInstruction:
                    writer.WritePropertyName($"?{value.Name}");
                    return;
                case XmlNodeType.DocumentType:
                    writer.WritePropertyName("!DOCTYPE");
                    return;
                case XmlNodeType.Document:
                case XmlNodeType.Comment:
                    return;
                default:
                    writer.WritePropertyName(value.Name);
                    return;
            }
        }
    }
}