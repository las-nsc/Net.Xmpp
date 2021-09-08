using System;
using System.Collections.Generic;
using System.Xml;

using Net.Xmpp.Core;
using Net.Xmpp.Im;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Implements Mechanism for providing Custom IQ Extensions
    /// </summary>
    internal class CustomIqExtension : XmppExtension, IInputFilter<Iq>
    {
        /// <summary>
        /// A reference to the 'Entity Capabilities' extension instance.
        /// </summary>
        private readonly EntityCapabilities ecapa;

        /// <summary>
        /// An enumerable collection of XMPP namespaces the extension implements.
        /// </summary>
        /// <remarks>This is used for compiling the list of supported extensions
        /// advertised by the 'Service Discovery' extension.</remarks>
        public override IEnumerable<string> Namespaces => new string[] { "urn:sharp.xmpp:customiq" };

        /// <summary>
        /// The named constant of the Extension enumeration that corresponds to this
        /// extension.
        /// </summary>
        public override Extension Xep => Extension.CustomIqExtension;

        /// <summary>
        /// Invoked when an IQ stanza is being received.
        /// If the Iq is correctly received a Result response is included
        /// with extension specific metadata included.
        /// If the Iq is not correctly received an error is returned
        /// Semantics of error on the response refer only to the XMPP level
        /// and not the application specific level
        /// </summary>
        /// <param name="stanza">The stanza which is being received.</param>
        /// <returns>true to intercept the stanza or false to pass the stanza
        /// on to the next handler.</returns>
        public bool Input(Iq stanza)
        {
            string? response = null;
            //if (stanza.Type != IqType.Get)
            //	return false;
            //get,set, result are supported
            var customIqStanza = stanza.Data["customiq"];
            if (customIqStanza == null || customIqStanza.NamespaceURI != "urn:sharp.xmpp:customiq")
                return false;
            //Result indicates that the request has been received.
            //It has not to do with the semantics of the message
            XmlElement query = stanza.Data["customiq"];

            XmlDocument targetDocument = new();

            CopyNodes(targetDocument, targetDocument, query.FirstChild);

            var xmlresponse = Xml.Element("customiq", "urn:sharp.xmpp:customiq");
            try
            {
                //call the callback for receiving a relevant stanza
                //and wait for answer in order provide it
                response = im.CustomIqDelegate?.Invoke(stanza.From, targetDocument.InnerXml);

                if (response?.Length > 0)
                {
                    xmlresponse.Text(response);
                }
                im.IqResult(stanza, xmlresponse);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Send back an error response" + e.StackTrace + e.ToString());
                // Send back an error response in case the callback method threw
                // an exception.
                im.IqError(stanza, ErrorType.Modify, ErrorCondition.BadRequest);
            }

            // We took care of this IQ request, so intercept it and don't pass it
            // on to other handlers.
            //Also send a void acknowledgement
            return true;
        }

        public void CopyNodes(XmlDocument targetDocument, XmlNode targetNode, XmlNode source)
        {
            XmlNode targetChildNode = targetDocument.CreateNode(source.NodeType, source.Name, "");
            if (!source.HasChildNodes)
                targetChildNode.InnerText = source.InnerText;
            targetNode.AppendChild(targetChildNode);
            foreach (XmlNode childNode in source.ChildNodes)
            {
                CopyNodes(targetDocument, targetChildNode, childNode);
            }
        }

        /// <summary>
        /// Requests the XMPP entity with the specified JID a GET command.
        /// When the Result is received and it not an error
        /// if fires the callback function
        /// </summary>
        /// <param name="jid">The JID of the XMPP entity to get.</param>
        /// <exception cref="ArgumentNullException">The jid parameter
        /// is null.</exception>
        /// <exception cref="NotSupportedException">The XMPP entity with
        /// the specified JID does not support the 'Ping' XMPP extension.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public void RequestCustomIqAsync(Jid jid, string request, Action callback)
        {
            jid.ThrowIfNull(nameof(jid));
            request.ThrowIfNull(nameof(request));

            //First check if the Jid entity supports the namespace
            if (!ecapa.Supports(jid, Extension.CustomIqExtension))
            {
                throw new NotSupportedException("The XMPP entity does not support the " +
                    "'CustomIqExtension' extension.");
            }
            var xml = Xml.Element("customiq", "urn:sharp.xmpp:customiq").Text(request);

            //The Request is Async
            im.IqRequestCallback(IqType.Get, jid, im.Jid, xml, null, (id, iq) =>
            {
                //For any reply we execute the callback
                if (iq.Type == IqType.Error)
                    throw Util.ExceptionFromError(iq, "Could not Send Object to XMPP entity.");
                if (iq.Type == IqType.Result)
                {
                    try
                    {
                        //An empty response means the message was received
                        callback?.Invoke();
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Not correctly formated response to RequestCustomIqAsync" + e.StackTrace + e.ToString());
                        throw Util.ExceptionFromError(iq, "Not correctly formated response to RequestCustomIqAsync, " + e.Message);
                    }
                }
            });
        }

        /// <summary>
        /// Requests the XMPP entity with the specified JID a GET command.
        /// When the Result is received and it not an error
        /// if fires the callback function
        /// </summary>
        /// <param name="jid">The JID of the XMPP entity to get.</param>
        /// <exception cref="ArgumentNullException">The jid parameter
        /// is null.</exception>
        /// <exception cref="NotSupportedException">The XMPP entity with
        /// the specified JID does not support the 'Ping' XMPP extension.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public void RequestCustomIq(Jid jid, string request)
        {
            jid.ThrowIfNull(nameof(jid));
            request.ThrowIfNull(nameof(request));

            //First check if the Jid entity supports the namespace
            if (!ecapa.Supports(jid, Extension.CustomIqExtension))
            {
                throw new NotSupportedException("The XMPP entity does not support the " +
                    "'CustomIqExtension' extension.");
            }
            var xml = Xml.Element("customiq", "urn:sharp.xmpp:customiq").Text(request);

            //The Request is Async
            im.IqRequest(IqType.Get, jid, im.Jid, xml);
        }

        /// <summary>
        /// Initializes a new instance of the CustomIq class.
        /// </summary>
        /// <param name="im">A reference to the XmppIm instance on whose behalf this
        /// instance is created.</param>
        public CustomIqExtension(XmppIm im, EntityCapabilities ecapa)
            : base(im)
        {
            this.ecapa = ecapa;
        }
    }
}