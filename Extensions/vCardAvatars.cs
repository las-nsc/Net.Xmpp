using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;

using Net.Xmpp.Core;
using Net.Xmpp.Im;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Implements the 'vCard based Avatars' extension as defined in XEP-0153.
    /// </summary>
    internal class vCardAvatars : XmppExtension, IInputFilter<Iq>
    {
        /// <summary>
        /// A reference to the 'Entity Capabilities' extension instance.
        /// </summary>
        private readonly EntityCapabilities ecapa;

        /// <summary>
        /// A cache of images indexed by their respective SHA-1 hashes.
        /// </summary>
        private readonly IDictionary<string, string> cachedImages = new Dictionary<string, string>();

        /// <summary>
        /// An enumerable collection of XMPP namespaces the extension implements.
        /// </summary>
        /// <remarks>This is used for compiling the list of supported extensions
        /// advertised by the 'Service Discovery' extension.</remarks>
        public override IEnumerable<string> Namespaces => new string[]
        {
            "vcard-temp:x:update" ,
            "vcard-temp"
        };

        /// <summary>
        /// The named constant of the Extension enumeration that corresponds to this
        /// extension.
        /// </summary>
        public override Extension Xep => Extension.vCardsAvatars;

        /// <summary>
        /// Invoked when an IQ stanza is being received.
        /// </summary>
        /// <param name="stanza">The stanza which is being received.</param>
        /// <returns>true to intercept the stanza or false to pass the stanza
        /// on to the next handler.</returns>
        public bool Input(Iq stanza)
        {
            if (stanza.Type != IqType.Get)
                return false;
            var vcard = stanza.Data["vCard "];
            if (vcard == null || vcard.NamespaceURI != "vcard-temp")
                return false;
            im.IqResult(stanza);
            // We took care of this IQ request, so intercept it and don't pass it
            // on to other handlers.
            return true;
        }

        //http://www.xmpp.org/extensions/xep-0153.html
        public void SetAvatar(Stream stream)
        {
            stream.ThrowIfNull(nameof(stream));

            const string mimeType = "image/png";

            string hash = string.Empty, base64Data = string.Empty;
            MemoryStream ms = new();
            stream.CopyTo(ms);
            using (ms)
            {
                //                    await image.Save(Splat.CompressedBitmapFormat.Jpeg,1, ms);
                //					// Calculate the SHA-1 hash of the image data.
                byte[] data = ms.ToArray();
                hash = Hash(data);
                //					// Convert the binary data into a BASE64-string.
                base64Data = Convert.ToBase64String(data);
            }
            var xml = Xml.Element("vCard", "vcard-temp").Child(Xml.Element("Photo").Child(Xml.Element("Type").Text(mimeType)).Child(Xml.Element("BINVAL").Text(base64Data)));
            im.IqRequestCallback(IqType.Set, null, im.Jid, xml, null, (id, iq) =>
            {
                if (iq.Type == IqType.Result)
                {
                    // Result must contain a 'feature' element.
                    XmlElement data = Xml.Element("x", "vcard-temp:x:update").Child(Xml.Element("photo").Text(hash));
                    im.SendPresence(new(type: PresenceType.Available, data: data));
                }
            });

            //var result = im.IqRequest(IqType.Set, null, im.Jid, xml);
            //            

        }
        // Publish the image- and meta data.

        //pep.Publish("urn:xmpp:avatar:data", hash,
        //    Xml.Element("data", "urn:xmpp:avatar:data").Text(base64Data));
        //pep.Publish("urn:xmpp:avatar:metadata", hash,
        //    Xml.Element("metadata", "urn:xmpp:avatar:metadata").Child(
        //    Xml.Element("info")
        //        .Attr("bytes", size.ToString())
        //        .Attr("height", height.ToString())
        //        .Attr("width", width.ToString())
        //        .Attr("id", hash)
        //        .Attr("type", mimeType))

        private string Hash(byte[] data)
        {
            data.ThrowIfNull(nameof(data));
            using var sha1 = new SHA1Managed();
            return Convert.ToBase64String(sha1.ComputeHash(data));
        }

        /// <summary>
        /// Requests the avatar image with the specified hash from the node service
        /// running at the specified JID.
        /// </summary>
        /// <param name="jid">The JID of the node service to request the avatar
        /// image from.</param>
        /// <param name="hash">The hash of the avatar image to retrieve.</param>
        /// <returns>An Image instance representing the retrieved avatar image.</returns>
        /// <exception cref="ArgumentNullException">The jid parameter or the
        /// hash parameter is null.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public void RequestAvatar(Jid jid, string filepath, Action callback)
        {
            jid.ThrowIfNull(nameof(jid));
            //Make the request
            var xml = Xml.Element("vCard", "vcard-temp");

            //var result = im.IqRequest(IqType.Get, jid, im.Jid, xml);
            im.IqRequestCallback(IqType.Get, jid, im.Jid, xml, null, (id, iq) =>
            {
                XmlElement query = iq.Data["vCard"];
                if (iq.Data["vCard"].NamespaceURI == "vcard-temp")
                {
                    XElement root = XElement.Parse(iq.Data.OuterXml);
                    XNamespace aw = "vcard-temp"; //SOS the correct namespace
                    IEnumerable<string> b64collection = from el in root.Descendants(aw + "BINVAL")
                                                        select (string)el;
                    string? b64 = null;
                    if (b64collection != null)
                    {
                        b64 = b64collection.FirstOrDefault();

                        if (b64 != null)
                        {
                            try
                            {
                                byte[] data = Convert.FromBase64String(b64);
                                //string hashvalue = Hash(data);
                                if (data != null)
                                {
                                    string dir = Path.GetDirectoryName(filepath);
                                    if (!Directory.Exists(dir))
                                    {
                                        Directory.CreateDirectory(dir);
                                    }

                                    using (var file = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                                    {
                                        file.Write(data, 0, data.Length);
                                    }
                                    callback?.Invoke();
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Error downloading and writing avatar file" + e);
                            }
                        }
                    }
                }
            });
            //Check the result

        }

        /// <summary>
        /// Initializes a new instance of the vCard-Avatar class.
        /// </summary>
        /// <param name="im">A reference to the XmppIm instance on whose behalf this
        /// instance is created.</param>
        public vCardAvatars(XmppIm im, EntityCapabilities ecapa)
            : base(im)
        {
            this.ecapa = ecapa;
        }
    }
}
