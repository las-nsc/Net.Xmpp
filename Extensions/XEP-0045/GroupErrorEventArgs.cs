using System;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Represents a group presence error event in a group chat. Ref XEP-0045
    /// </summary>
    public class GroupErrorEventArgs : EventArgs
    {
        /// <summary>The full error object.</summary>
        internal MucError Data { get; }

        /// <summary>The error condition.</summary>
        public ErrorCondition ErrorCondition => Data.ErrorCondition;

        /// <summary>The type of error.</summary>
        public ErrorType ErrorType => Data.ErrorType;

        /// <summary>Jid of the chat room.</summary>
        public Jid? By => Data.By;

        /// <summary>Your group chat jid.</summary>
        public Jid? From => Data.From;

        /// <summary>Your public jid.</summary>
        public Jid? To => Data.To;

        /// <summary>
        /// Constructs a GroupErrorEventArgs.
        /// </summary>
        /// <param name="data">Group Chat Error.</param>
        internal GroupErrorEventArgs(MucError data)
        {
            data.ThrowIfNull(nameof(data));
            Data = data;
        }
    }
}
