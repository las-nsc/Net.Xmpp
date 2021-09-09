using System;
using System.Xml;

using Net.Xmpp.Core;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Implements the message history request object as described in XEP-0045.
    /// </summary>
    public class History : Presence
    {
        private const string rootTag = "presence",
            xTag = "x",
            historyTag = "history",
            maxCharsAttribute = "maxchars",
            maxStanzasAttribute = "maxstanzas",
            secondsAttribute = "seconds",
            sinceAttribute = "since";

        /// <summary>
        /// Initialises a MUC message history request object.
        /// </summary>
        /// <param name="to"></param>
        /// <param name="from"></param>
        /// <param name="maxChars"></param>
        public History(Jid to, Jid from, int maxChars = 0)
            : base(to, from, data: Xml.Element(xTag, MucNs.NsMain))
        {
            XElement.Child(Xml.Element(historyTag));
            MaxChars = maxChars;
        }

        /// <summary>
        /// Initialises a MUC message history request object.
        /// </summary>
        /// <param name="to"></param>
        /// <param name="from"></param>
        /// <param name="since"></param>
        public History(Jid to, Jid from, DateTime since)
            : base(to, from, data: Xml.Element(xTag, MucNs.NsMain))
        {
            XElement.Child(Xml.Element(historyTag));
            Since = since;
        }

        /// <summary>
        /// Limit the total number of characters in the history to "X" 
        /// (where the character count is the characters of the complete XML stanzas,
        /// not only their XML character data).
        /// </summary>
        public int? MaxChars
        {
            get => GetValueAsInteger(maxCharsAttribute);
            set => ReplaceValue(maxCharsAttribute, SafeNumber(value));
        }

        /// <summary>
        /// Limit the total number of messages in the history to "X".
        /// </summary>
        public int? MaxStanzas
        {
            get => GetValueAsInteger(maxStanzasAttribute);
            set => ReplaceValue(maxStanzasAttribute, SafeNumber(value));
        }

        /// <summary>
        /// Send only the messages received in the last "X" seconds.
        /// </summary>
        public int? Seconds
        {
            get => GetValueAsInteger(secondsAttribute);
            set => ReplaceValue(secondsAttribute, SafeNumber(value));
        }

        /// <summary>
        /// Send only the messages received since the UTC datetime specified.
        /// </summary>
        public DateTime? Since
        {
            get => GetValueAsDateTime(sinceAttribute);
            set
            {
                string? safeValue = null;

                if (value.HasValue)
                    safeValue = value.Value
                        .ToUniversalTime()
                        .ToString("yyyy-MM-ddTHH:mm:ssZ");

                ReplaceValue(sinceAttribute, safeValue);
            }
        }

        /// <summary>
        /// The tag name of the stanza's root element
        /// </summary>
        protected override string RootElementName => rootTag;

        private XmlElement XElement => element[xTag];

        private XmlElement? HistoryElement => GetNode(xTag, historyTag);

        /// <summary>
        /// Prevents the user from entering numbers less than zero.
        /// </summary>
        /// <param name="number">user input.</param>
        /// <returns>null or any number equal to or greater than zero.</returns>
        private string? SafeNumber(int? number)
        {
            string? result = null;

            if (number != null)
            {
                int? safeNumber = number < 0 ? 0 : number;
                result = safeNumber.ToString();
            }

            return result;
        }

        private int? GetValueAsInteger(string attributeName)
        {
            var v = HistoryElement?.GetAttribute(attributeName);

            int? result = null;
            if (v?.Length > 0)
                result = int.Parse(v);

            return result;
        }

        private DateTime? GetValueAsDateTime(string attributeName)
        {
            var v = HistoryElement?.GetAttribute(attributeName);

            DateTime? result = null;
            if (v?.Length > 0)
                result = DateTime.Parse(v);

            return result;
        }

        private void ReplaceValue(string attributeName, string? value)
        {
            const string zero = "0";

            HistoryElement?.RemoveAllAttributes();
            if (value is null)
                HistoryElement?.SetAttribute(maxCharsAttribute, zero);
            else
                HistoryElement?.SetAttribute(attributeName, value);
        }
    }
}
