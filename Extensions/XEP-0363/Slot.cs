using System;
using System.Collections.Generic;
using System.Xml;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Slot information for upload file
    /// </summary>
    public class Slot
    {
        private const string xmlns = "urn:xmpp:http:upload:0";

        /// <summary>
        /// URL for download the file
        /// </summary>
        public string Get { get; set; }

        /// <summary>
        /// URL for file upload
        /// </summary>
        public string Put { get; set; }

        /// <summary>
        /// HTTP Headers to use in upload
        /// </summary>
        public Dictionary<string, string> HttpHeader { get; set; }

        /// <summary>
        /// Create a Slot
        /// </summary>
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
