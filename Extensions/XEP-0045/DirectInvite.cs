using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Net.Xmpp.Core;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Implements MUC Direct Invitation as described in XEP-0045.
    /// </summary>
    public class DirectInvite : Message
    {
        private const string reasonTag = "reason",
            passwordTag = "password";

        protected override string RootElementName
        {
            get { return "message"; }
        }

        /// <summary>
        /// Initialises a group chat invite.
        /// </summary>
        /// <param name="to">User you intend to invite to chat room.</param>
        /// /// <param name="from">User sending the invitation.</param>
        /// <param name="reason">Message included with the invitation.</param>
        /// <param name="room">Jid of the chat room.</param>
        /// <param name="password">(optional) Password.</param>
        public DirectInvite(Jid to, Jid from, Jid room, string reason, string password = null)
            : base(to, from, Xml.Element("x", "jabber:x:conference"))
        {
            Room = room;
            Reason = reason;
            Password = password;
        }

        internal DirectInvite(Core.Message message)
            : base(message.Data)
        {
        }

        /// <summary>
        /// JID of the user the invite is intended to be send to.
        /// </summary>
        public Jid Room
        {
            get
            {
                string v = Data["x"].GetAttribute("jid");

                return String.IsNullOrEmpty(v) ? null : new Jid(v);
            }

            set
            {
                if (value == null)
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
            get
            {
                return Data["x"].GetAttribute(reasonTag);
            }

            set
            {
                if (value == null)
                    Data["x"].RemoveAttribute(reasonTag);
                else
                    Data["x"].SetAttribute(reasonTag, value.ToString());
            }
        }

        /// <summary>
        /// (Optional) password of the chat room in the invitation.
        /// </summary>
        public string Password
        {
            get
            {
                return Data["x"].GetAttribute(passwordTag);
            }

            set
            {
                if (value == null)
                    Data["x"].RemoveAttribute(passwordTag);
                else
                    Data["x"].SetAttribute(passwordTag, value.ToString());
            }
        }

        internal static bool IsElement(Core.Message message)
        {
            DirectInvite temp = new DirectInvite(message);
            return temp?.Data["x"]?.NamespaceURI == "jabber:x:conference";
        }

    }
}
