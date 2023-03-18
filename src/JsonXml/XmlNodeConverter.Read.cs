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

            return ReadRootObject(ref reader);
        }

        private XmlDocument ReadRootObject(ref Utf8JsonReader reader)
        {
            var document = new XmlDocument();
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        var element = document.CreateElement(reader.GetString());
                        document.AppendChild(element);
                        ReadPropertyValue(ref reader, document, element);
                        break;
                    case JsonTokenType.EndObject:
                        return document;
                }
            }
            return document;
        }

        private void ReadObject(ref Utf8JsonReader reader, XmlDocument document, XmlNode currentNode)
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
                            currentNode.Attributes.Append(attribute);
                            ReadPropertyValue(ref reader, document, attribute);
                        }
                        else
                        {
                            var element = document.CreateElement(propertyName);
                            currentNode.AppendChild(element);
                            ReadPropertyValue(ref reader, document, element);
                        }
                        break;
                    case JsonTokenType.EndObject:
                        return;
                    default:
                        break;
                }
            }
        }

        private void ReadPropertyValue(
            ref Utf8JsonReader reader,
            XmlDocument document,
            XmlNode currentNode)
        {
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Null:
                        return;
                    case JsonTokenType.String:
                        currentNode.AppendChild(document.CreateTextNode(reader.GetString()));
                        return;
                    case JsonTokenType.StartObject:
                        ReadObject(ref reader, document, currentNode);
                        return;
                    default:
                        break;
                }
            }
        }
    }
}
