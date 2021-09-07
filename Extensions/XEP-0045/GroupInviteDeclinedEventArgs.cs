using System;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Represents a group invite event in a group chat. Ref XEP-0045
    /// </summary>
    public class GroupInviteDeclinedEventArgs : EventArgs
    {
        /// <summary>
        /// The full invite object.
        /// </summary>
        public InviteDeclined Data { get; }

        /// <summary>
        /// Person who sent the invitation.
        /// </summary>
        public Jid From => Data.ReceivedFrom;

        /// <summary>
        /// Chat room specified in the invitation.
        /// </summary>
        public Jid ChatRoom => Data.From;

        /// <summary>
        /// Message contained in the invitation.
        /// </summary>
        public string Reason => Data.Reason;

        /// <summary>
        /// Constructs a GroupInviteEventArgs.
        /// </summary>
        /// <param name="invite">Group Chat Invitation.</param>
        public GroupInviteDeclinedEventArgs(InviteDeclined invite)
        {
            invite.ThrowIfNull(nameof(invite));
            Data = invite;
        }
    }
}
