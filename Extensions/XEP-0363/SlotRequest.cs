using System.Xml;

namespace Net.Xmpp.Extensions
{
    internal class SlotRequest
    {
        private const string xmlns = "urn:xmpp:http:upload:0";

        /// <summary>
        /// File name
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long Size { get; }

        /// <summary>
        /// MIME Filet type
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// Create a Slot Request
        /// </summary>
        public SlotRequest(string file, long size, string contentType = null)
        {
            FileName = file;
            Size = size;
            ContentType = string.IsNullOrWhiteSpace(contentType) ? System.Net.Mime.MediaTypeNames.Application.Octet : contentType;
        }

        /// <summary>
        /// Create an XML element that represents this request
        /// </summary>
        public XmlElement ToXmlElement()
        {
            var requestNode = Xml.Element("request", xmlns);
            var fileAttr = requestNode.Attr("filename", FileName);
            var sizeAttr = requestNode.Attr("size", Size.ToString());
            var contentTypeAttr = requestNode.Attr("content-type", ContentType);

            return requestNode;
        }
    }
}
