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
            ReadRootObject(ref reader, document);
            return document;
        }

        private static void ReadRootObject(ref Utf8JsonReader reader, XmlDocument document)
        {
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        ReadPropertyValue(ref reader, document, document, reader.GetString());
                        break;
                    case JsonTokenType.EndObject:
                        return;
                }
            }
        }

        private static void ReadPropertyValue(ref Utf8JsonReader reader, XmlDocument document, XmlNode currentNode, string propertyName)
        {
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Null:
                    case JsonTokenType.String:
                    case JsonTokenType.Number:
                    case JsonTokenType.True:
                    case JsonTokenType.False:
                        var elementWithSingleValue = document.CreateElement(propertyName);
                        ReadPrimitiveValue(ref reader, document, elementWithSingleValue);
                        currentNode.AppendChild(elementWithSingleValue);
                        return;
                    case JsonTokenType.StartObject:
                        var element = document.CreateElement(propertyName);
                        ReadObject(ref reader, document, element);
                        currentNode.AppendChild(element);
                        return;
                    case JsonTokenType.StartArray:
                        ReadArrayPropertyValue(ref reader, document, currentNode, propertyName);
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
                        var propertyName = reader.GetString();
                        if (propertyName.StartsWith("@"))
                        {
                            var attribute = document.CreateAttribute(propertyName.Substring(1));
                            reader.Read();
                            ReadPrimitiveValue(ref reader, document, attribute);
                            currentNode.Attributes.Append(attribute);
                        }
                        else
                        {
                            ReadPropertyValue(ref reader, document, currentNode, propertyName);
                        }
                        break;
                    case JsonTokenType.EndObject:
                        return;
                    default:
                        break;
                }
            }
        }

        private static void ReadArrayPropertyValue(ref Utf8JsonReader reader, XmlDocument document, XmlNode currentNode, string propertyName)
        {
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Null:
                    case JsonTokenType.String:
                    case JsonTokenType.Number:
                    case JsonTokenType.True:
                    case JsonTokenType.False:
                        var elementWithSingleValue = document.CreateElement(propertyName);
                        ReadPrimitiveValue(ref reader, document, elementWithSingleValue);
                        currentNode.AppendChild(elementWithSingleValue);
                        break;
                    case JsonTokenType.StartObject:
                        var element = document.CreateElement(propertyName);
                        ReadObject(ref reader, document, element);
                        currentNode.AppendChild(element);
                        break;
                    case JsonTokenType.EndArray:
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
