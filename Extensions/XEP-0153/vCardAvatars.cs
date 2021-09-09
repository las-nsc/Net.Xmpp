﻿using System;
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
    internal class VCardAvatars : XmppExtension, IInputFilter<Iq>
    {
        /// <summary>
        /// An enumerable collection of XMPP namespaces the extension implements.
        /// </summary>
        /// <remarks>This is used for compiling the list of supported extensions
        /// advertised by the 'Service Discovery' extension.</remarks>
        public override IEnumerable<string> Namespaces => new string[] {
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
        /// <summary>
        /// Set the Avatar based on the stream
        /// </summary>
        /// <param name="stream">Avatar stream</param>
        public void SetAvatar(Stream stream)
        {
            stream.ThrowIfNull(nameof(stream));

            string mimeType = "image/png";

            string hash = string.Empty, base64Data = string.Empty;
            MemoryStream ms = new();
            stream.CopyTo(ms);
            using (ms)
            {
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
        }

        /// <summary>
        /// Convert the Image to the appropriate format for XEP-0153
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private string Hash(byte[] data)
        {
            data.ThrowIfNull(nameof(data));
            using var sha1 = new SHA1Managed();
            return Convert.ToBase64String(sha1.ComputeHash(data));
        }

        /// <summary>
        /// Requests the avatar image with the specified hash from the node service
        /// running at the specified JID. It downloads it asynchronysly and executes
        /// a specified callback action when finished
        /// </summary>
        /// <param name="jid">The JID of the node service to request the avatar
        /// image from.</param>
        /// <param name="filepath">The full location of the file that the Avatar file we be written.</param>
        /// <param name="callback">A callback Action to be invoked after the end of the file write. </param>
        /// <exception cref="ArgumentNullException">The jid or the filepath parameter is null.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public void RequestAvatar(Jid jid, string filepath, Action<string, Jid> callback)
        {
            jid.ThrowIfNull(nameof(jid));
            filepath.ThrowIfNull(nameof(filepath));

            //Make the request
            var xml = Xml.Element("vCard", "vcard-temp");

            //The Request is Async
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
                                    callback?.Invoke(filepath, jid);
                                }
                            }
                            catch (Exception e)
                            {
                                System.Diagnostics.Debug.WriteLine("Error downloading and writing avatar file" + e.StackTrace + e.ToString());
                                //Exception is not contained here. Fix?
                            }
                        }
                    }
                }
            });
        }

        public void RequestAvatar(Jid jid, Action<byte[], Jid> callback)
        {
            jid.ThrowIfNull(nameof(jid));
            //filepath.ThrowIfNull(nameof(filePath));

            //Make the request
            var xml = Xml.Element("vCard", "vcard-temp");

            //The Request is Async
            im.IqRequestCallback(IqType.Get, jid, im.Jid, xml, null, (id, iq) =>
            {
                XmlElement query = iq.Data["vCard"];
                if (iq.Data["vCard"].NamespaceURI == "vcard-temp")
                {
                    XElement root = XElement.Parse(iq.Data.OuterXml);
                    XNamespace aw = "vcard-temp"; //SOS the correct namespace
                    IEnumerable<string> b64collection = from el in root.Descendants(aw + "BINVAL") select (string)el;
                    string? b64 = null;
                    if (b64collection != null)
                    {
                        b64 = b64collection.FirstOrDefault();

                        if (b64 != null)
                        {
                            try
                            {
                                byte[] data = Convert.FromBase64String(b64);
                                if (data != null)
                                {
                                    callback.Invoke(data, jid);
                                }
                            }
                            catch (Exception e)
                            {
                                System.Diagnostics.Debug.WriteLine("Error downloading and writing avatar file" + e.StackTrace + e.ToString());
                                //Exception is not contained here. Fix?
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Initializes a new instance of the vCard-Avatar class.
        /// </summary>
        /// <param name="im">A reference to the XmppIm instance on whose behalf this
        /// instance is created.</param>
        public VCardAvatars(XmppIm im)
            : base(im)
        {
        }
    }
}