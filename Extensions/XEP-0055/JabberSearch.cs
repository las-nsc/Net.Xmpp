using Net.Xmpp.Core;
using Net.Xmpp.Extensions.Dataforms;
using Net.Xmpp.Im;
using System;
using System.Collections.Generic;

namespace Net.Xmpp.Extensions
{
    internal class JabberSearch : XmppExtension
    {
        private const string xmlns = "jabber:iq:search";

        /// <summary>
        /// A reference to the 'Service Discovery' extension instance.
        /// </summary>
        private ServiceDiscovery sdisco;

        public JabberSearch(XmppIm im): base(im)
        {

        }

        /// <summary>
        /// Invoked after all extensions have been loaded.
        /// </summary>
        public override void Initialize()
        {
            sdisco = im.GetExtension<ServiceDiscovery>();
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
                     xmlns
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

        private Jid SearchDomain()
        {
            foreach (var item in sdisco.GetItems(im.Jid.Domain))
            {
                // Query each item for its identities and look for a 'directory' identity.
                foreach (var ident in sdisco.GetIdentities(item.Jid))
                {
                    if (ident.Category == "directory" && ident.Type == "user")
                    {
                        return item.Jid;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Request the Search Form
        /// </summary>
        /// <returns>DataForm for avaible fields search</returns>
        public DataForm RequestSearchForm()
        {
            Jid searchDomain = SearchDomain();

            if (searchDomain == null)
            {
                throw new Exception("Feauture not supported");
            }

            Iq iq = im.IqRequest(IqType.Get, searchDomain, im.Jid, Xml.Element("query", xmlns));
            if (iq.Type == IqType.Result)
            {
                var query = iq.Data["query"];
                if (query == null || query.NamespaceURI != xmlns)
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
        public DataForm Search(DataForm search)
        {
            Jid searchDomain = SearchDomain();

            if (searchDomain == null)
            {
                throw new Exception("Feauture not supported");
            }

            Iq iq = im.IqRequest(IqType.Set, searchDomain, im.Jid, Xml.Element("query", xmlns).Child(search.ToXmlElement()));
            if (iq.Type == IqType.Result)
            {
                var query = iq.Data["query"];
                if (query == null || query.NamespaceURI != xmlns)
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
