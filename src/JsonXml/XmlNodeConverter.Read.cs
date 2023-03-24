using System;
using System.Collections.Generic;
using System.Xml;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonXml
{
    public partial class XmlNodeConverter : JsonConverter<XmlNode>
    {
        public override XmlNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new InvalidOperationException("Must be an object");
            }

            var document = new XmlDocument();

            var namespaceUrisByPrefix = new Dictionary<string, string>
            {
                ["xml"] = "http://www.w3.org/XML/1998/namespace",
                ["xmlns"] = "http://www.w3.org/2000/xmlns/"
            };

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        var propertyName = reader.GetString()!;
                        switch (propertyName)
                        {
                            case "?xml":
                                ReadXmlDeclaration(ref reader, document);
                                break;
                            case "!DOCTYPE":
                                ReadDocumentType(ref reader, document);
                                break;
                            default:
                                if (propertyName.StartsWith("?"))
                                {
                                    reader.Read();
                                    document.CreateProcessingInstruction(propertyName.Substring(1), reader.GetString() ?? string.Empty);
                                    break;
                                }
                                ReadPropertyValue(ref reader, document, document, propertyName, namespaceUrisByPrefix);
                                break;
                        }
                        break;
                    case JsonTokenType.Comment:
                        document.AppendChild(document.CreateComment(reader.GetComment()));
                        break;
                    default:
                        break;
                }
            }

            return document;
        }

        private static void ReadXmlDeclaration(ref Utf8JsonReader reader, XmlDocument document)
        {
            string? propertyName = null;
            string? version = null;
            string? encoding = null;
            string? standalone = null;
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        propertyName = reader.GetString();
                        break;
                    case JsonTokenType.String:
                        switch (propertyName)
                        {
                            case "@version":
                                version = reader.GetString();
                                break;
                            case "@encoding":
                                encoding = reader.GetString();
                                break;
                            case "@standalone":
                                standalone = reader.GetString();
                                break;
                            default:
                                break;
                        }
                        break;
                    case JsonTokenType.EndObject:
                        if (version != null)
                        {
                            document.AppendChild(document.CreateXmlDeclaration(version, encoding, standalone));
                        }
                        return;
                }
            }
        }

        private static void ReadDocumentType(ref Utf8JsonReader reader, XmlDocument document)
        {
            string? propertyName = null;
            string? name = null;
            string? publicId = null;
            string? systemId = null;
            string? internalSubset = null;
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        propertyName = reader.GetString();
                        break;
                    case JsonTokenType.String:
                        switch (propertyName)
                        {
                            case "@name":
                                name = reader.GetString();
                                break;
                            case "@public":
                                publicId = reader.GetString();
                                break;
                            case "@system":
                                systemId = reader.GetString();
                                break;
                            case "@internalSubset":
                                internalSubset = reader.GetString();
                                break;
                            default:
                                break;
                        }
                        break;
                    case JsonTokenType.EndObject:
                        if (name != null)
                        {
                            document.AppendChild(document.CreateDocumentType(name, publicId, systemId, internalSubset));
                        }
                        return;
                }
            }
        }

        private static void ReadPropertyValue(
            ref Utf8JsonReader reader,
            XmlDocument document,
            XmlNode currentNode,
            string propertyName,
            Dictionary<string, string> namespaceUrisByPrefix)
        {
            bool isInsideArray = false;
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Null:
                    case JsonTokenType.String:
                    case JsonTokenType.Number:
                    case JsonTokenType.True:
                    case JsonTokenType.False:
                        switch (propertyName)
                        {
                            case "#text":
                                var text = ReadPrimitiveValue(ref reader);
                                if (text != null)
                                {
                                    currentNode.AppendChild(document.CreateTextNode(text));
                                }
                                break;
                            case "#significant-whitespace":
                                var significantWhitespace = ReadPrimitiveValue(ref reader);
                                if (significantWhitespace != null)
                                {
                                    currentNode.AppendChild(document.CreateSignificantWhitespace(significantWhitespace));
                                }
                                break;
                            case "#cdata-section":
                                var cData = ReadPrimitiveValue(ref reader);
                                if (cData != null)
                                {
                                    currentNode.AppendChild(document.CreateCDataSection(cData));
                                }
                                break;
                            default:
                                if (propertyName.StartsWith("@"))
                                {
                                    var attributeName = propertyName.Substring(1);
                                    var attribute = CreateAttribute(propertyName.Substring(1), document, namespaceUrisByPrefix);
                                    var attributeValue = ReadPrimitiveValue(ref reader);
                                    if (attributeValue != null)
                                    {
                                        attribute.AppendChild(document.CreateTextNode(attributeValue));
                                        if (propertyName.StartsWith("@xmlns:"))
                                        {
                                            namespaceUrisByPrefix[propertyName.Substring("@xmlns:".Length)] = attributeValue;
                                        }
                                    }

                                    if (currentNode.Attributes != null)
                                    {
                                        currentNode.Attributes.Append(attribute);
                                    }

                                    break;
                                }
                                if (propertyName.StartsWith("?"))
                                {
                                    currentNode.AppendChild(document.CreateProcessingInstruction(propertyName.Substring(1), reader.GetString() ?? string.Empty));
                                    break;
                                }
                                var elementWithSingleValue = CreateElement(propertyName, document, namespaceUrisByPrefix);
                                var elementValue = ReadPrimitiveValue(ref reader);
                                if (elementValue != null)
                                {
                                    elementWithSingleValue.AppendChild(document.CreateTextNode(elementValue));
                                }
                                currentNode.AppendChild(elementWithSingleValue);
                                break;
                        }
                        if (!isInsideArray)
                        {
                            return;
                        }
                        break;
                    case JsonTokenType.Comment:
                        currentNode.AppendChild(document.CreateComment(reader.GetComment()));
                        break;
                    case JsonTokenType.StartObject:
                        var fakeElement = document.CreateElement("fake");
                        ReadObject(ref reader, document, fakeElement, namespaceUrisByPrefix);
                        var element = CreateElement(propertyName, document, namespaceUrisByPrefix);
                        while (fakeElement.ChildNodes.Count > 0)
                        {
                            var newChild = fakeElement.ChildNodes[0];
                            if (newChild == null)
                            {
                                break;
                            }
                            element.AppendChild(newChild);
                        }
                        while (fakeElement.Attributes.Count > 0)
                        {
                            element.Attributes.Append(fakeElement.Attributes[0]);
                        }
                        currentNode.AppendChild(element);
                        if (!isInsideArray)
                        {
                            return;
                        }
                        break;
                    case JsonTokenType.StartArray:
                        isInsideArray = true;
                        break;
                    case JsonTokenType.EndArray:
                        return;
                    default:
                        break;
                }
            }
        }

        private static void ReadObject(
            ref Utf8JsonReader reader,
            XmlDocument document,
            XmlNode currentNode,
            Dictionary<string, string> namespaceUrisByPrefix)
        {
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        ReadPropertyValue(ref reader, document, currentNode, reader.GetString()!, namespaceUrisByPrefix);
                        break;
                    case JsonTokenType.Comment:
                        currentNode.AppendChild(document.CreateComment(reader.GetComment()));
                        break;
                    case JsonTokenType.EndObject:
                        return;
                    default:
                        break;
                }
            }
        }

        private static string? ReadPrimitiveValue(ref Utf8JsonReader reader)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Null => null,
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => reader.TryGetInt64(out var l)
                        ? l.ToString()
                        : reader.GetDouble().ToString(),
                JsonTokenType.True => "true",
                JsonTokenType.False => "false",
                _ => null
            };
        }

        private static XmlAttribute CreateAttribute(
            string name,
            XmlDocument document,
            Dictionary<string, string> namespaceUrisByPrefix)
        {
            var colonIndex = name.IndexOf(':');

            if (colonIndex < 1 || colonIndex == name.Length - 1)
            {
                return document.CreateAttribute(name);
            }

            var prefix = name.Substring(0, colonIndex);
            var localName = name.Substring(colonIndex + 1);

            if (namespaceUrisByPrefix.TryGetValue(prefix, out var namespaceUri))
            {
                return document.CreateAttribute(prefix, localName, namespaceUri);
            }

            return document.CreateAttribute(localName);
        }

        private static XmlElement CreateElement(
            string name,
            XmlDocument document,
            Dictionary<string, string> namespaceUrisByPrefix)
        {
            var colonIndex = name.IndexOf(':');

            if (colonIndex < 1 || colonIndex == name.Length - 1)
            {
                return document.CreateElement(name);
            }

            var prefix = name.Substring(0, colonIndex);
            var localName = name.Substring(colonIndex + 1);

            if (namespaceUrisByPrefix.TryGetValue(prefix, out var namespaceUri))
            {
                return document.CreateElement(prefix, localName, namespaceUri);
            }

            return document.CreateElement(localName);
        }
    }
}
