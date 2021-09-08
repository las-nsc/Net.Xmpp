﻿using Net.Xmpp.Core;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Implements MUC Direct Invitation as described in XEP-0045.
    /// </summary>
    public class DirectInvite : Message
    {
        private const string reasonTag = "reason",
            passwordTag = "password";

        /// <summary>
        /// The tag name of the stanza's root element
        /// Allows the element tag name to be overridden.
        /// </summary>
        protected override string RootElementName => "message";

        /// <summary>
        /// Initialises a group chat invite.
        /// </summary>
        /// <param name="to">User you intend to invite to chat room.</param>
        /// /// <param name="from">User sending the invitation.</param>
        /// <param name="reason">Message included with the invitation.</param>
        /// <param name="room">Jid of the chat room.</param>
        /// <param name="password">(optional) Password.</param>
        public DirectInvite(Jid to, Jid from, Jid room, string reason, string? password = null)
            : base(to, from, Xml.Element("x", "jabber:x:conference"))
        {
            Room = room;
            Reason = reason;
            Password = password;
        }

        internal DirectInvite(Message message)
            : base(message.Data)
        {
        }

        /// <summary>
        /// JID of the user the invite is intended to be send to.
        /// </summary>
        public Jid? Room
        {
            get
            {
                string v = Data["x"].GetAttribute("jid");

                return v?.Length > 0 ? new Jid(v) : null;
            }

            set
            {
                if (value is null)
                    Data["x"].RemoveAttribute("jid");
                else
                    Data["x"].SetAttribute("jid", value.ToString());
            }
        }

        /// <summary>
        /// Custom message that may be sent with the invitation.
        /// </summary>
        public string Reason
        {
            get => Data["x"].GetAttribute(reasonTag);

            set
            {
                if (value is null)
                    Data["x"].RemoveAttribute(reasonTag);
                else
                    Data["x"].SetAttribute(reasonTag, value);
            }
        }

        /// <summary>
        /// (Optional) password of the chat room in the invitation.
        /// </summary>
        public string? Password
        {
            get => Data["x"].GetAttribute(passwordTag);

            set
            {
                if (value is null)
                    Data["x"].RemoveAttribute(passwordTag);
                else
                    Data["x"].SetAttribute(passwordTag, value);
            }
        }

        internal static bool IsElement(Message message)
        {
            DirectInvite temp = new(message);
            return temp?.Data["x"]?.NamespaceURI == "jabber:x:conference";
        }
    }
}
