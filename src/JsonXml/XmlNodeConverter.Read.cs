using System;
using System.Xml;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonXml
{
    public partial class XmlNodeConverter : JsonConverter<XmlNode>
    {
        public override XmlNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        var propertyName = reader.GetString();
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
                                    document.CreateProcessingInstruction(propertyName.Substring(1), reader.GetString());
                                    break;
                                }
                                ReadPropertyValue(ref reader, document, document, propertyName);
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
            string propertyName = null;
            string version = null;
            string encoding = null;
            string standalone = null;
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
                        document.AppendChild(document.CreateXmlDeclaration(version, encoding, standalone));
                        return;
                }
            }
        }

        private static void ReadDocumentType(ref Utf8JsonReader reader, XmlDocument document)
        {
            string propertyName = null;
            string name = null;
            string publicId = null;
            string systemId = null;
            string internalSubset = null;
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
                        document.AppendChild(document.CreateDocumentType(name, publicId, systemId, internalSubset));
                        return;
                }
            }
        }

        private static void ReadPropertyValue(ref Utf8JsonReader reader, XmlDocument document, XmlNode currentNode, string propertyName)
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
                                ReadPrimitiveValue(ref reader, document, currentNode);
                                break;
                            default:
                                if (propertyName.StartsWith("@"))
                                {
                                    var attribute = document.CreateAttribute(propertyName.Substring(1));
                                    ReadPrimitiveValue(ref reader, document, attribute);
                                    currentNode.Attributes.Append(attribute);
                                    break;
                                }
                                if (propertyName.StartsWith("?"))
                                {
                                    currentNode.AppendChild(document.CreateProcessingInstruction(propertyName.Substring(1), reader.GetString()));
                                    break;
                                }
                                var elementWithSingleValue = document.CreateElement(propertyName);
                                ReadPrimitiveValue(ref reader, document, elementWithSingleValue);
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
                        var element = document.CreateElement(propertyName);
                        ReadObject(ref reader, document, element);
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

        private static void ReadObject(ref Utf8JsonReader reader, XmlDocument document, XmlNode currentNode)
        {
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        ReadPropertyValue(ref reader, document, currentNode, reader.GetString());
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



        private static void ReadPrimitiveValue(ref Utf8JsonReader reader, XmlDocument document, XmlNode currentNode)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return;
                case JsonTokenType.String:
                    currentNode.AppendChild(document.CreateTextNode(reader.GetString()));
                    return;
                case JsonTokenType.Number:
                    currentNode.AppendChild(document.CreateTextNode(reader.GetInt64().ToString()));
                    return;
                case JsonTokenType.True:
                    currentNode.AppendChild(document.CreateTextNode("true"));
                    return;
                case JsonTokenType.False:
                    currentNode.AppendChild(document.CreateTextNode("false"));
                    return;
                default:
                    return;
            }
        }
    }
}
