using System;
using System.Collections.Generic;

using Net.Xmpp.Core;
using Net.Xmpp.Extensions.Dataforms;
using Net.Xmpp.Im;

namespace Net.Xmpp.Extensions
{
    internal class JabberSearch : XmppExtension
    {
        private const string xmlns = "jabber:iq:search";

        /// <summary>
        /// A reference to the 'Service Discovery' extension instance.
        /// </summary>
        private readonly ServiceDiscovery sdisco;

        public JabberSearch(XmppIm im, ServiceDiscovery sdisco) : base(im)
        {
            this.sdisco = sdisco;
        }

        /// <summary>
        /// An enumerable collection of XMPP namespaces the extension implements.
        /// </summary>
        /// <remarks>This is used for compiling the list of supported extensions
        /// advertised by the 'Service Discovery' extension.</remarks>
        public override IEnumerable<string> Namespaces
            => new string[] { xmlns };

        /// <summary>
        /// The named constant of the Extension enumeration that corresponds to this
        /// extension.
        /// </summary>
        public override Extension Xep => Extension.JabberSearch;

        private Jid? SearchDomain()
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
            var searchDomain = SearchDomain();

            if (searchDomain is null)
            {
                throw new Exception("Feauture not supported");
            }

            Iq iq = im.IqRequest(IqType.Get, searchDomain, im.Jid, Xml.Element("query", xmlns));
            if (iq.Type == IqType.Result)
            {
                var query = iq.Data["query"];
                return query == null || query.NamespaceURI != xmlns
                    ? throw new XmppException("Erroneous server response.")
                    : DataFormFactory.Create(query["x"]);
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
            var searchDomain = SearchDomain();

            if (searchDomain is null)
            {
                throw new Exception("Feauture not supported");
            }

            Iq iq = im.IqRequest(IqType.Set, searchDomain, im.Jid, Xml.Element("query", xmlns).Child(search.ToXmlElement()));
            if (iq.Type == IqType.Result)
            {
                var query = iq.Data["query"];
                return query == null || query.NamespaceURI != xmlns
                    ? throw new XmppException("Erroneous server response.")
                    : DataFormFactory.Create(query["x"]);
            }
            else
            {
                var error = iq.Data["error"];
                throw new Exception(error["text"].InnerText);
            }
        }
    }
}
