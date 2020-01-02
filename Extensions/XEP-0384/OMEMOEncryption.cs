using Sharp.Xmpp.Core;
using Sharp.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharp.Xmpp.Extensions
{
    internal class OMEMOEncryption : XmppExtension, IInputFilter<Core.Message>
    {
        private static readonly string[] _namespaces = { "eu.siacs.conversations.axolotl" };

        public override IEnumerable<string> Namespaces
        {
            get { return _namespaces; }
        }

        public override Extension Xep
        {
            get { return Extension.OMEMOEncryption; }
        }

        public OMEMOEncryption(XmppIm im) :
            base(im)
        {
            
        }

        public bool Input(Core.Message stanza)
        {
            var encrypted = stanza.Data["encrypted"];
            if (encrypted == null || encrypted.NamespaceURI != "eu.siacs.conversations.axolotl")
                return false;
            EncryptedMessage msg = new EncryptedMessage(stanza);

            if (OnMessage != null)
            {
                OnMessage(this, new EncryptedMessageEventArgs(msg.From, msg));
            }
            return true;
        }

        public event EventHandler<EncryptedMessageEventArgs> OnMessage;
    }
}
