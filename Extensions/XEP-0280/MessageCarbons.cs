using Net.Xmpp.Core;
using Net.Xmpp.Im;
using System;
using System.Collections.Generic;
using Message = Net.Xmpp.Im.Message;

namespace Net.Xmpp.Extensions
{
    internal class MessageCarbons : XmppExtension, IInputFilter<Message>
    {
        private static readonly string[] _namespaces = { "urn:xmpp:carbons:2" };
        private EntityCapabilities ecapa;

        public override IEnumerable<string> Namespaces => _namespaces;

        public override Extension Xep => Extension.MessageCarbons;

        public override void Initialize()
        {
            ecapa = im.GetExtension<EntityCapabilities>();
        }

        public void EnableCarbons(bool enable = true)
        {
            if (!ecapa.Supports(im.Jid.Domain, Extension.MessageCarbons))
            {
                throw new NotSupportedException("The XMPP server does not support " +
                    "the 'Message Carbons' extension.");
            }
            var iq = im.IqRequest(IqType.Set, null, im.Jid,
                Xml.Element(enable ? "enable" : "disable", _namespaces[0]));
            if (iq.Type == IqType.Error)
                throw Util.ExceptionFromError(iq, "Message Carbons could not " +
                    "be enabled.");
        }

        public bool Input(Message stanza)
        {
            if (stanza.CarbonMessage)
            {
                this.im.OnMessage(stanza.ForwardedMessage);

                return true;
            }

            return false;
        }

        public MessageCarbons(XmppIm im) :
            base(im)
        {
        }
    }
}