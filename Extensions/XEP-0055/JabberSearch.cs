using Net.Xmpp.Core;
using Net.Xmpp.Extensions.Dataforms;
using Net.Xmpp.Im;
using System;
using System.Collections.Generic;

namespace Net.Xmpp.Extensions
{
    internal class JabberSearch : XmppExtension
    {

        public JabberSearch(XmppIm im): base(im)
        {

        }

        /// <summary>
        /// An enumerable collection of XMPP namespaces the extension implements.
        /// </summary>
        /// <remarks>This is used for compiling the list of supported extensions
        /// advertised by the 'Service Discovery' extension.</remarks>
        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] {
                     "jabber:iq:search"
                };
            }
        }

        /// <summary>
        /// The named constant of the Extension enumeration that corresponds to this
        /// extension.
        /// </summary>
        public override Extension Xep
        {
            get
            {
                return Extension.JabberSearch;
            }
        }

        /// <summary>
        /// Request the Search Form
        /// </summary>
        /// <returns>DataForm for avaible fields search</returns>
        public DataForm RequestSearchForm(string searchServer)
        {
            Iq iq = im.IqRequest(IqType.Get, new Jid(searchServer), im.Jid, Xml.Element("query", "jabber:iq:search"));
            if (iq.Type == IqType.Result)
            {
                var query = iq.Data["query"];
                if (query == null || query.NamespaceURI != "jabber:iq:search")
                    throw new XmppException("Erroneous server response.");

                return DataFormFactory.Create(query["x"]);
            }
            else
            {
                var error = iq.Data["error"];
                throw new Exception(error["text"].InnerText);
            }
        }

        /// <summary>
        /// Submit a serach forms
        /// </summary>
        /// <returns>Search result based on DataForm request</returns>
        public DataForm Search(string searchServer, DataForm search)
        {
  
            Iq iq = im.IqRequest(IqType.Set, new Jid(searchServer), im.Jid, Xml.Element("query", "jabber:iq:search").Child(search.ToXmlElement()));
            if (iq.Type == IqType.Result)
            {
                var query = iq.Data["query"];
                if (query == null || query.NamespaceURI != "jabber:iq:search")
                    throw new XmppException("Erroneous server response.");

                return DataFormFactory.Create(query["x"]);
            }
            else
            {
                var error = iq.Data["error"];
                throw new Exception(error["text"].InnerText);
            }
        }

    }
}
