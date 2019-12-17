using Sharp.Xmpp.Extensions.XEP_0384;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharp.Xmpp.Im
{
    public class EncryptedMessageEventArgs : EventArgs
    {
        /// <summary>
        /// The JID of the user or resource who sent the message.
        /// </summary>
        public Jid Jid
        {
            get;
            private set;
        }

        /// <summary>
        /// The received chat message.
        /// </summary>
        public EncryptedMessage Message
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the MessageEventArgs class.
        /// </summary>
        /// <exception cref="ArgumentNullException">The jid parameter or the message
        /// parameter is null.</exception>
        public EncryptedMessageEventArgs(Jid jid, EncryptedMessage message)
        {
            jid.ThrowIfNull("jid");
            message.ThrowIfNull("message");
            Jid = jid;
            Message = message;
        }
    }
}
