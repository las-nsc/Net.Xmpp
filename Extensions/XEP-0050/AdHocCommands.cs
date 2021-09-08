using System.Collections.Generic;
using System.Linq;
using System.Xml;

using Net.Xmpp.Core;
using Net.Xmpp.Im;

namespace Net.Xmpp.Extensions
{
    internal class AdHocCommands : XmppExtension
    {
        public AdHocCommands(XmppIm im)
            : base(im)
        {
        }

        public override IEnumerable<string> Namespaces => new string[0]; // todo:

        public override Extension Xep => Extension.AdHocCommands;

        public List<AdHocCommand> GetAdHocCommands()
        {
            var query = Xml.Element("query", "http://jabber.org/protocol/disco#items").Attr("node", "http://jabber.org/protocol/commands");
            var response = im.IqRequest(IqType.Get, im.Hostname, im.Jid, query);
            return response.Data["query"].GetElementsByTagName("item").Cast<XmlElement>().Select(e => new AdHocCommand(e)).ToList();
        }
    }
}
