using System;
using System.Text.Json.Serialization;
using System.Xml;

namespace JsonXml
{
    public partial class XmlNodeConverter : JsonConverter<XmlNode>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(XmlNode).IsAssignableFrom(typeToConvert);
        }
    }
}