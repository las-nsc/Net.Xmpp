using System;
using System.Xml;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Implements a MUC error response which may be contained in either an IQ or a Presence.
    /// </summary>
    internal class MucError : Core.Stanza
    {
        private const string errorTag = "error",
            byAttribute = "by",
            typeAttribute = "type";

        public MucError(Core.Stanza stanza) : base(stanza.Data)
        {
        }

        /// <summary>
        /// Only filled in on a presence, may be null.
        /// </summary>
        public Jid? By
        {
            get
            {
                var v = ErrorNode?.Attributes?[byAttribute]?.Value;
                return v?.Length > 0 ? new Jid(v) : null;
            }
        }

        /// <summary>
        /// The type of error.
        /// </summary>
        public ErrorType ErrorType
        {
            get
            {
                // It's possible for the error tag to be either inside or outside the x tag.
                var errorTypeString = ErrorNode?.Attributes?[typeAttribute]?.Value;

                const bool ignoreCase = true;

                // It should always parse, otherwise the message doesn't meet the protocol.
                if (errorTypeString?.Length > 0 || !Enum.TryParse(errorTypeString, ignoreCase, out ErrorType error))
                    error = ErrorType.Cancel;

                return error;
            }
        }

        /// <summary>
        /// The reason for the error.
        /// </summary>
        public ErrorCondition ErrorCondition
        {
            get
            {
                const string allDashses = "-";

                // It's possible for the error tag to be either inside or outside the x tag.
                var nodeName = ErrorNode?.FirstChild?.Name?.Replace(allDashses, string.Empty);

                const bool ignoreCase = true;

                // It should always parse, otherwise the message doesn't meet the protocol.
                if (!(nodeName?.Length > 0) || !Enum.TryParse(nodeName, ignoreCase, out ErrorCondition reason))
                    reason = ErrorCondition.BadRequest;

                return reason;
            }
        }

        private XmlNode? ErrorNode =>
                // It's possible for the error tag to be either inside or outside the x tag.
                element.GetElementsByTagName(errorTag)?.Item(0);

        /// <summary>
        /// Determines whether the stanza contains an error.
        /// </summary>
        /// <param name="stanza">input stanza</param>
        /// <returns>true if contains an error</returns>
        public static bool IsError(Core.Stanza stanza)
        {
            // Not every response has a namespace in it
            return new MucError(stanza).ErrorNode != null;
        }
    }
}
