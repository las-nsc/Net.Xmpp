using System.Xml;

using Net.Xmpp.Core;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Implements MUC Mediated Invitation as described in XEP-0045.
    /// </summary>
    public class Invite : Message
    {
        private const string rootTag = "message",
            xTag = "x",
            inviteTag = "invite",
            reasonTag = "reason",
            passwordTag = "password",
            toAttribute = "to",
            fromAttribute = "from";

        /// <summary>
        /// Initialises a group chat invite.
        /// </summary>
        /// <param name="to">User you intend to invite to chat room.</param>
        /// /// <param name="from">User sending the invitation.</param>
        /// <param name="reason">Message included with the invitation.</param>
        /// <param name="room">Jid of the chat room.</param>
        /// <param name="password">(optional) Password.</param>
        public Invite(Jid to, Jid from, Jid room, string reason, string? password = null)
            : base(room, from, Xml.Element(xTag, MucNs.NsUser))
        {
            XElement.Child(Xml.Element(inviteTag).Child(Xml.Element(reasonTag)));
            SendTo = to;
            Reason = reason;
            Password = password;
        }

        internal Invite(Message message)
            : base(message.Data)
        {
        }

        /// <summary>
        /// JID of the user the invite is intended to be send to.
        /// </summary>
        public Jid? SendTo
        {
            get
            {
                var v = InviteElement?.GetAttribute(toAttribute);
                return v?.Length > 0 ? new Jid(v) : null;
            }

            set
            {
                if (value is null)
                    InviteElement?.RemoveAttribute(toAttribute);
                else
                    InviteElement?.SetAttribute(toAttribute, value.ToString());
            }
        }

        /// <summary>
        /// JID of the user the invite has been sent from.
        /// </summary>
        public Jid? ReceivedFrom
        {
            get
            {
                var v = InviteElement?.GetAttribute(fromAttribute);
                return v?.Length > 0 ? new Jid(v) : null;
            }

            private set
            {
                if (value is null)
                    InviteElement?.RemoveAttribute(fromAttribute);
                else
                    InviteElement?.SetAttribute(fromAttribute, value.ToString());
            }
        }

        /// <summary>
        /// Custom message that may be sent with the invitation.
        /// </summary>
        public string? Reason
        {
            get => ReasonElement?.InnerText;

            set
            {
                if (value?.Length > 0)
                    ReasonElement?.Text(value);
            }
        }

        /// <summary>
        /// (Optional) password of the chat room in the invitation.
        /// </summary>
        public string? Password
        {
            get => PasswordElement?.InnerText;

            set
            {
                var node = PasswordElement;

                if (node != null)
                    XElement.RemoveChild(node);

                if (value?.Length > 0)
                    XElement.Child(Xml.Element(passwordTag).Text(value));
            }
        }

        /// <summary>
        /// The tag name of the stanza's root element
        /// </summary>
        protected override string RootElementName => rootTag;

        private XmlElement XElement => element[xTag];

        private XmlElement? InviteElement => GetNode(xTag, inviteTag);

        private XmlElement? ReasonElement => GetNode(xTag, inviteTag, reasonTag);

        private XmlElement? PasswordElement => GetNode(xTag, passwordTag);

        internal static bool IsElement(Message message)
        {
            Invite temp = new(message);
            return temp?.XElement?.NamespaceURI == MucNs.NsUser && temp?.InviteElement != null;
        }
    }
}
