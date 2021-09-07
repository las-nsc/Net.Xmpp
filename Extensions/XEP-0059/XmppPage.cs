﻿
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Represents a result set as defined in XEP-0059
    /// </summary>
    public class XmppPage
    {
        /// <summary>
        /// The id of the first item in this page
        /// </summary>
        public string First { get; protected set; }

        /// <summary>
        /// The id of the last item in this page
        /// </summary>
        public string Last { get; protected set; }

        /// <summary>
        /// The total number of items in the result set.
        /// </summary>
        public int TotalCount { get; protected set; }

        /// <summary>
        /// Create a page from a set node
        /// </summary>
        /// <param name="xml">A set xml node</param>
        internal XmppPage(XmlElement xml)
        {
            xml.ThrowIfNull(nameof(xml));

            try
            {
                First = xml["first"].InnerText;
            }
            catch
            {
            }

            try
            {
                Last = xml["last"].InnerText;
            }
            catch
            {
            }

            try
            {
                TotalCount = int.Parse(xml["count"].InnerText);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Generate a request to fetch the page that follows this page
        /// </summary>
        public XmppPageRequest NextPageRequest(int fetchCount)
            => new(fetchCount) { After = Last };

        /// <summary>
        /// Generate a request to fetch the page that precedes this page
        /// </summary>
        public XmppPageRequest PreviousPageRequest(int fetchCount)
            => new(fetchCount) { Before = First };
    }

    /// <summary>
    /// Represents a single page of results in a result set, as specified by XEP-0059.
    /// </summary>
    public class XmppPage<T> : XmppPage, IEnumerable<T>
    {
        /// <summary>
        /// The items in this page
        /// </summary>
        public IList<T> Items { get; }

        /// <summary>
        /// The total number of items in this page
        /// </summary>
        public int PageCount => Items.Count;

        /// <summary>
        /// Enumerator for items in this page
        /// </summary>
        public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

        /// <summary>
        /// Enumerator for items in this page
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();

        /// <summary>
        /// Create an XmppPage from the an xml node containing a set and a function to pick items from the node
        /// </summary>
        /// <param name="xml">An xml node that contains a set node</param>
        /// <param name="itemSelector">Function to select items from the xml node</param>
        internal XmppPage(XmlElement xml, Func<XmlElement, IList<T>> itemSelector)
            : base(xml["set"])
        {
            itemSelector.ThrowIfNull(nameof(itemSelector));

            Items = itemSelector(xml);
        }

        /// <summary>
        /// Create an XmppPage from a set xml node and a list of items
        /// </summary>
        /// <param name="xml">A set xml node</param>
        /// <param name="items">The items for this page</param>
        internal XmppPage(XmlElement xml, IList<T> items)
            : base(xml)
        {
            Items = items;
        }

        /// <summary>
        /// Generate a request to fetch the page that follows this page
        /// </summary>
        public XmppPageRequest NextPageRequest() => NextPageRequest(PageCount);

        /// <summary>
        /// Generate a request to fetch the page that precedes this page
        /// </summary>
        public XmppPageRequest PreviousPageRequest() => PreviousPageRequest(PageCount);
    }
}