using System;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Represents a group invite event in a group chat. Ref XEP-0045
    /// </summary>
    public class GroupInviteEventArgs : EventArgs
    {
        /// <summary>
        /// Person who sent the invitation.
        /// </summary>
        public Jid? From { get; }

        /// <summary>
        /// Chat room specified in the invitation.
        /// </summary>
        public Jid? ChatRoom { get; }

        /// <summary>
        /// Message contained in the invitation.
        /// </summary>
        public string? Reason { get; }

        /// <summary>
        /// Password (if any).
        /// </summary>
        public string? Password { get; }

        /// <summary>
        /// Constructs a GroupInviteEventArgs.
        /// </summary>
        /// <param name="invite">Group Chat Invitation.</param>
        public GroupInviteEventArgs(Invite invite)
        {
            invite.ThrowIfNull(nameof(invite));
            From = invite.From;
            ChatRoom = invite.From;
            Reason = invite.Reason;
            Password = invite.Password;
        }

        /// <summary>
        /// Constructs a GroupInviteEventArgs.
        /// </summary>
        /// <param name="invite">Group Chat Invitation.</param>
        public GroupInviteEventArgs(DirectInvite invite)
        {
            invite.ThrowIfNull(nameof(invite));
            From = invite.From;
            ChatRoom = invite.Room;
            Reason = invite.Reason;
            Password = invite.Password;
        }
    }
}
