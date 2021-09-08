using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Xml;

using Net.Xmpp.Extensions;

namespace Net.Xmpp.Im
{
    /// <summary>
    /// Represents a Message stanza as defined in XMPP:IM.
    /// </summary>
    public class Message : Core.Message
    {
        /// <summary>
        /// The type of the message stanza.
        /// </summary>
        private MessageType type;

        /// <summary>
        /// The type of the message stanza.
        /// </summary>
        public MessageType Type
        {
            get => type;

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
        public DateTimeOffset Timestamp { get; protected set; }

        /// <summary>
        /// A forwarded message that is contained within this message, if there is one present.
        /// </summary>
        public Message? ForwardedMessage { get; protected set; }

        /// <summary>
        /// A forwarded message that is contained within this message, if there is one present.
        /// </summary>
        public bool CarbonMessage { get; protected set; }

        /// <summary>
        /// The conversation thread this message belongs to.
        /// </summary>
        public string? Thread
        {
            get => element["thread"]?.InnerText;

            set
            {
                var e = element["thread"];
                if (e != null)
                {
                    if (value is null)
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
        public string? Subject
        {
            get
            {
                var bare = GetBare("subject");
                if (bare != null)
                    return bare.InnerText;
                string k = AlternateSubjects.Keys.FirstOrDefault();
                return k != null ? AlternateSubjects[k] : null;
            }

            set
            {
                var bare = GetBare("subject");
                if (bare != null)
                {
                    if (value is null)
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
        public string? Body
        {
            get
            {
                var bare = GetBare("body");
                if (bare != null)
                    return bare.InnerText;
                string k = AlternateBodies.Keys.FirstOrDefault();
                return k != null ? AlternateBodies[k] : null;
            }

            set
            {
                var bare = GetBare("body");
                if (bare != null)
                {
                    if (value is null)
                        element.RemoveChild(bare);
                    else
                        bare.InnerText = value;
                }
                else
                {
                    if (value != null)
                        element.Child(Xml.Element("body").Text(value));
                }
            }
        }

        /// <summary>
        /// A dictionary of alternate forms of the message subjects. The keys of the
        /// dictionary denote ISO 2 language codes.
        /// </summary>
        public IDictionary<string, string> AlternateSubjects { get; }

        /// <summary>
        /// A dictionary of alternate forms of the message bodies. The keys of the
        /// dictionary denote ISO 2 language codes.
        /// </summary>
        public IDictionary<string, string> AlternateBodies { get; }

        /// <summary>
        /// Initializes a new instance of the Message class.
        /// </summary>
        /// <param name="to">The JID of the intended recipient.</param>
        /// <param name="body">The content of the message.</param>
        /// <param name="subject">The subject of the message.</param>
        /// <param name="thread">The conversation thread this message belongs to.</param>
        /// <param name="type">The type of the message. Can be one of the values from
        /// the MessagType enumeration.</param>
        /// <param name="language">The language of the XML character data of
        /// the stanza.</param>
        /// <exception cref="ArgumentNullException">The to parameter is null.</exception>
        /// <exception cref="ArgumentException">The body parameter is the empty string.</exception>
        public Message(Jid to, string? body = null, string? subject = null, string? thread = null,
            MessageType type = MessageType.Normal, CultureInfo? language = null)
            : base(to, null, null, null, language)
        {
            to.ThrowIfNull(nameof(to));
            AlternateSubjects = new XmlDictionary(element, "subject", "xml:lang");
            AlternateBodies = new XmlDictionary(element, "body", "xml:lang");
            Type = type;
            Body = SecurityElement.Escape(body);
            Subject = SecurityElement.Escape(subject);
            Thread = thread;
            Timestamp = DelayedDelivery.GetDelayedTimestampOrNow(element);
        }

        /// <summary>
        /// Initializes a new instance of the Message class.
        /// </summary>
        /// <param name="to">The JID of the intended recipient.</param>
        /// <param name="bodies">A dictionary of message bodies. The dictionary
        /// keys denote the languages of the message bodies and must be valid
        /// ISO 2 letter language codes.</param>
        /// <param name="subjects">A dictionary of message subjects. The dictionary
        /// keys denote the languages of the message subjects and must be valid
        /// ISO 2 letter language codes.</param>
        /// <param name="thread">The conversation thread this message belongs to.</param>
        /// <param name="type">The type of the message. Can be one of the values from
        /// the MessagType enumeration.</param>
        /// <param name="language">The language of the XML character data of
        /// the stanza.</param>
        /// <exception cref="ArgumentNullException">The to parametr or the bodies
        /// parameter is null.</exception>
        public Message(Jid to, IDictionary<string, string> bodies,
            IDictionary<string, string>? subjects = null, string? thread = null,
            MessageType type = MessageType.Normal, CultureInfo? language = null)
            : base(to, null, null, null, language)
        {
            to.ThrowIfNull(nameof(to));
            bodies.ThrowIfNull(nameof(bodies));
            AlternateSubjects = new XmlDictionary(element, "subject", "xml:lang");
            AlternateBodies = new XmlDictionary(element, "body", "xml:lang");
            Type = type;
            foreach (var pair in bodies)
                AlternateBodies.Add(pair.Key, pair.Value);
            if (subjects != null)
            {
                foreach (var pair in subjects)
                    AlternateSubjects.Add(pair.Key, pair.Value);
            }
            Thread = thread;
            Timestamp = DelayedDelivery.GetDelayedTimestampOrNow(element);
        }

        /// <summary>
        /// Initializes a new instance of the Message class from the specified
        /// instance.
        /// </summary>
        /// <param name="message">An instance of the Core.Message class to
        /// initialize this instance with.</param>
        /// <exception cref="ArgumentNullException">The message parameter is null.</exception>
        /// <exception cref="ArgumentException">The 'type' attribute of
        /// the specified message stanza is invalid.</exception>
        internal Message(Core.Message message)
            : this(message.Data, DelayedDelivery.GetDelayedTimestampOrNow(message.Data))
        {
        }

        /// <summary>
        /// Initializes a new instance of the Message class from the specified
        /// instance.
        /// </summary>
        /// <param name="messageNode">A message xml node</param>
        /// <param name="timestamp">The timestamp to use for the message</param>
        /// <exception cref="ArgumentNullException">The message parameter is null.</exception>
        /// <exception cref="ArgumentException">The 'type' attribute of
        /// the specified message stanza is invalid.</exception>
        internal Message(XmlElement messageNode, DateTimeOffset timestamp)
        {
            messageNode.ThrowIfNull(nameof(messageNode));
            type = ParseType(messageNode.GetAttribute("type"));
            element = messageNode;
            AlternateSubjects = new XmlDictionary(element, "subject", "xml:lang");
            AlternateBodies = new XmlDictionary(element, "body", "xml:lang");
            Timestamp = timestamp;

            if (element["sent"] is { } carbonNode && carbonNode.NamespaceURI == "urn:xmpp:carbons:2")
            {
                CarbonMessage = true;
                if (carbonNode["forwarded"] is { } forwardedMessageNode && forwardedMessageNode.NamespaceURI == "urn:xmpp:forward:0")
                {
                    var forwardedTimestamp = DelayedDelivery.GetDelayedTimestampOrNow(forwardedMessageNode);
                    ForwardedMessage = new Message(forwardedMessageNode["message"], forwardedTimestamp);
                }
            }
            else
            {
                if (element["forwarded"] is { } forwardedMessageNode && forwardedMessageNode.NamespaceURI == "urn:xmpp:forward:0")
                {
                    var forwardedTimestamp = DelayedDelivery.GetDelayedTimestampOrNow(forwardedMessageNode);
                    ForwardedMessage = new Message(forwardedMessageNode["message"], forwardedTimestamp);
                }
            }
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
            return !(value?.Length > 0)
                ? MessageType.Normal
                : (MessageType)Enum.Parse(typeof(MessageType),
                value.Capitalize());
        }

        /// <summary>
        /// Attempts to retrieve the bare element (i.e. without an xml:lang
        /// attribute) with the specified tag name.
        /// </summary>
        /// <param name="tag">The tag name of the element to retrieve.</param>
        /// <returns>The located element or null if no such element exists.</returns>
        private XmlElement? GetBare(string tag)
        {
            foreach (XmlElement e in element.GetElementsByTagName(tag))
            {
                string k = e.GetAttribute("xml:lang");
                if (!(k?.Length > 0))
                    return e;
            }
            return null;
        }
    }
}