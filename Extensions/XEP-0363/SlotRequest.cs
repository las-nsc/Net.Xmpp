using System;
using System.Xml;

namespace Net.Xmpp.Extensions
{
    internal class SlotRequest
    {
        private const string xmlns = "urn:xmpp:http:upload:0";

        /// <summary>
        /// File name
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long Size { get; private set; }

        /// <summary>
        /// MIME Filet type
        /// </summary>
        public string ContentType { get; private set; }

        /// <summary>
        /// Create a Slot Request
        /// </summary>
        public SlotRequest(string file, long size, string contentType = null)
        {
            FileName = file;
            Size = size;
            if (String.IsNullOrWhiteSpace(contentType))
            {
                ContentType = System.Net.Mime.MediaTypeNames.Application.Octet;
            }
            else
            {
                ContentType = contentType;
            }
        }

        /// <summary>
        /// Create an XML element that represents this request
        /// </summary>
        public XmlElement ToXmlElement()
        {
            var requestNode = Xml.Element("request", xmlns);
            var fileAttr = Xml.Attr(requestNode, "filename", FileName);
            var sizeAttr = Xml.Attr(requestNode, "size", Size.ToString());
            var contentTypeAttr = Xml.Attr(requestNode, "content-type", ContentType);

            return requestNode;
        }
    }
}
