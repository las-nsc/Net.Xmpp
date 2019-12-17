using Sharp.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace Sharp.Xmpp.Extensions.XEP_0384
{
    /// <summary>
    /// Represents a Message stanza as defined in xep-0384.
    /// </summary>
    public class EncryptedMessage : Core.Message
    {
        /// <summary>
        /// The type of the message stanza.
        /// </summary>
        private MessageType type;

        /// <summary>
        /// The time at which the message was originally sent.
        /// </summary>
        private DateTime timestamp = DateTime.Now;

        /// <summary>
        /// The type of the message stanza.
        /// </summary>
        public MessageType Type
        {
            get
            {
                return type;
            }

            set
            {
                type = value;
                var v = value.ToString().ToLowerInvariant();
                element.SetAttribute("type", v);
            }
        }

        /// <summary>
        /// The time at which the message was originally sent.
        /// </summary>
        public DateTime Timestamp
        {
            get
            {
                // Refer to XEP-0203.
                var delay = element["delay"];
                if (delay != null && delay.NamespaceURI == "urn:xmpp:delay")
                {
                    DateTime result;
                    if (DateTime.TryParse(delay.GetAttribute("stamp"), out result))
                        return result;
                }
                return timestamp;
            }
        }

        /// <summary>
        /// The conversation thread this message belongs to.
        /// </summary>
        public string Thread
        {
            get
            {
                if (element["thread"] != null)
                    return element["thread"].InnerText;
                return null;
            }

            set
            {
                var e = element["thread"];
                if (e != null)
                {
                    if (value == null)
                        element.RemoveChild(e);
                    else
                        e.InnerText = value;
                }
                else
                {
                    if (value != null)
                        element.Child(Xml.Element("thread").Text(value));
                }
            }
        }

        /// <summary>
        /// The subject of the message.
        /// </summary>
        public string Subject
        {
            get
            {
                XmlElement bare = GetBare("subject");
                if (bare != null)
                    return bare.InnerText;
                string k = AlternateSubjects.Keys.FirstOrDefault();
                return k != null ? AlternateSubjects[k] : null;
            }

            set
            {
                XmlElement bare = GetBare("subject");
                if (bare != null)
                {
                    if (value == null)
                        element.RemoveChild(bare);
                    else
                        bare.InnerText = value;
                }
                else
                {
                    if (value != null)
                        element.Child(Xml.Element("subject").Text(value));
                }
            }
        }

        /// <summary>
        /// The body of the message.
        /// </summary>
        public XmlElement Encrypted
        {
            get
            {
                XmlElement bare = GetBare("encrypted");
                return bare;
            }

            set
            {
                XmlElement bare = GetBare("encrypted");
                if (bare != null)
                {
                    if (value == null)
                        element.RemoveChild(bare);
                    else
                        element.Child(value);
                }
                else
                {
                    if (value != null)
                        element.Child(value);
                }
            }
        }

        /// <summary>
        /// The body of the message.
        /// </summary>
        public MessageHint Hint
        {
            get
            {
                XmlElement bare = GetBare("store");
                if (bare != null)
                    return MessageHint.Store;
                bare = GetBare("no-store");
                if (bare != null)
                    return MessageHint.NoStore;

                return MessageHint.None;
            }

            set
            {
                XmlElement bare;
                if (value == MessageHint.None)
                {
                    bare = GetBare("store");
                    if (bare != null)
                    {
                        element.RemoveChild(bare);
                    }
                    bare = GetBare("no-store");
                    if (bare != null)
                    {
                        element.RemoveChild(bare);
                    }
                }

                if (value == MessageHint.Store)
                {
                    bare = GetBare("no-store");
                    if (bare != null)
                    {
                        element.RemoveChild(bare);
                    }
                    bare = GetBare("store");
                    if (bare == null)
                    {
                        element.Child(Xml.Element("store", "urn:xmpp:hints"));
                    }
                }

                if (value == MessageHint.NoStore)
                {
                    bare = GetBare("store");
                    if (bare != null)
                    {
                        element.RemoveChild(bare);
                    }
                    bare = GetBare("no-store");
                    if (bare == null)
                    {
                        element.Child(Xml.Element("no-store", "urn:xmpp:hints"));
                    }
                }
            }
        }

        protected override string RootElementName
        {
            get
            {
                return "message";
            }
        }

        /// <summary>
        /// A dictionary of alternate forms of the message subjects. The keys of the
        /// dictionary denote ISO 2 language codes.
        /// </summary>
        public IDictionary<string, string> AlternateSubjects
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the Message class.
        /// </summary>
        /// <param name="to">The JID of the intended recipient.</param>
        /// <param name="encrypted">The content of the message.</param>
        /// <param name="subject">The subject of the message.</param>
        /// <param name="thread">The conversation thread this message belongs to.</param>
        /// <param name="type">The type of the message. Can be one of the values from
        /// the MessagType enumeration.</param>
        /// <param name="language">The language of the XML character data of
        /// the stanza.</param>
        /// <exception cref="ArgumentNullException">The to parameter is null.</exception>
        /// <exception cref="ArgumentException">The body parameter is the empty string.</exception>
        public EncryptedMessage(Jid to, XmlElement encrypted, string subject = null, string thread = null,
            MessageType type = MessageType.Normal, CultureInfo language = null, MessageHint messageHint = MessageHint.None)
            : base(to, null, null, null, language)
        {
            to.ThrowIfNull("to");
            encrypted.ThrowIfNull("encrypted");
            AlternateSubjects = new XmlDictionary(element, "subject", "xml:lang");
            Type = type;
            Encrypted = encrypted;
            Subject = subject;
            Thread = thread;
            Hint = messageHint;
        }

        internal EncryptedMessage(Core.Message message)
        {
            message.ThrowIfNull("message");
            type = ParseType(message.Data.GetAttribute("type"));
            element = message.Data;
            AlternateSubjects = new XmlDictionary(element, "subject", "xml:lang");
        }

        /// <summary>
        /// Parses the Message type from the specified string.
        /// </summary>
        /// <param name="value">The string to parse.</param>
        /// <returns>The MessageType value parsed from the string.</returns>
        /// <exception cref="ArgumentException">The specified value for the stanza
        /// type is invalid.</exception>
        private MessageType ParseType(string value)
        {
            // The 'type' attribute of message-stanzas is optional and if absent
            // a type of 'normal' is assumed.
            if (String.IsNullOrEmpty(value))
                return MessageType.Normal;
            return (MessageType)Enum.Parse(typeof(MessageType),
                value.Capitalize());
        }

        /// <summary>
        /// Attempts to retrieve the bare element (i.e. without an xml:lang
        /// attribute) with the specified tag name.
        /// </summary>
        /// <param name="tag">The tag name of the element to retrieve.</param>
        /// <returns>The located element or null if no such element exists.</returns>
        private XmlElement GetBare(string tag)
        {
            foreach (XmlElement e in element.GetElementsByTagName(tag))
            {
                string k = e.GetAttribute("xml:lang");
                if (String.IsNullOrEmpty(k))
                    return e;
            }
            return null;
        }
    }
}
