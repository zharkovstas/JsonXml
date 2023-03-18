using System;
using System.Xml;
using System.Text.Json.Serialization;

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
