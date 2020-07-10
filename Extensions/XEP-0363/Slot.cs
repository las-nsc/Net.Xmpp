using System;
using System.Collections.Generic;
using System.Xml;

namespace Net.Xmpp.Extensions
{
    public class Slot
    {
        private const string xmlns = "urn:xmpp:http:upload:0";

        public string Get { get; set; }

        public string Put { get; set; }

        public Dictionary<string, string> HttpHeader { get; set; }

        public Slot(XmlElement element)
        {
            if (element.Name != "slot" || element.NamespaceURI != xmlns)
            {
                throw new ArgumentException("Invalid root element: " + element.Name);
            }
            HttpHeader = new Dictionary<string, string>();
            if (element["get"] != null)
            {
                Get = element["get"].Attributes["url"].Value;
            }
            if (element["put"] != null)
            {
                Put = element["put"].Attributes["url"].Value;
                foreach(XmlNode header in element["put"].ChildNodes)
                {
                    if (header.Name == "header")
                    {
                        HttpHeader.Add(header.Attributes["name"].Value, header.Value);
                    }
                }
            }
        }
    }
}
