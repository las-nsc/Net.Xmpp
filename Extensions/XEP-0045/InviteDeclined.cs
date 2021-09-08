using System.Xml;

using Net.Xmpp.Core;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Implements MUC Mediated Invitation as described in XEP-0045.
    /// </summary>
    public class InviteDeclined : Message
    {
        private const string rootTag = "message",
            xTag = "x",
            inviteTag = "decline",
            reasonTag = "reason",
            toAttribute = "to",
            fromAttribute = "from";

        /// <summary>
        /// Initialises a group chat invite.
        /// </summary>
        /// <param name="invite">Invitation to a chat room.</param>
        /// <param name="reason">Message included with the invitation.</param>
        public InviteDeclined(Invite invite, string reason)
            : base(invite.From, invite.To, Xml.Element(xTag, MucNs.NsUser))
        {
            XElement.Child(Xml.Element(inviteTag).Child(Xml.Element(reasonTag)));
            SendTo = invite.ReceivedFrom;
            Reason = reason;
        }

        internal InviteDeclined(Message message)
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
        /// The tag name of the stanza's root element
        /// </summary>
        protected override string RootElementName => rootTag;

        private XmlElement XElement => element[xTag];

        private XmlElement? InviteElement => GetNode(xTag, inviteTag);

        private XmlElement? ReasonElement => GetNode(xTag, inviteTag, reasonTag);

        internal static bool IsElement(Message message)
        {
            InviteDeclined temp = new(message);
            return temp?.XElement?.NamespaceURI == MucNs.NsUser && temp?.InviteElement != null;
        }
    }
}
