using System;

namespace Net.Xmpp.Im
{
    /// <summary>
    /// Provides data for the Status event.
    /// </summary>
    public class StatusEventArgs : EventArgs
    {
        /// <summary>
        /// The JID of the user or resource whose status has changed.
        /// </summary>
        public Jid Jid { get; }

        /// <summary>
        /// The status of the user.
        /// </summary>
        public Status Status { get; }

        /// <summary>
        /// Initializes a new instance of the StatusEventArgs class.
        /// </summary>
        /// <exception cref="ArgumentNullException">The jid parameter or the status
        /// parameter is null.</exception>
        public StatusEventArgs(Jid jid, Status status)
        {
            jid.ThrowIfNull(nameof(jid));
            status.ThrowIfNull(nameof(status));
            Jid = jid;
            Status = status;
        }
    }
}