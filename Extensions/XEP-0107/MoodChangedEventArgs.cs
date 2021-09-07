using System;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Provides data for the MoodChanged event.
    /// </summary>
    [Serializable]
    public class MoodChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The JID of the XMPP entity that published the mood information.
        /// </summary>
        public Jid Jid { get; }

        /// <summary>
        /// The mood of the XMPP entity.
        /// </summary>
        public Mood Mood { get; }

        /// <summary>
        /// a natural-language description of, or reason for, the mood. This may be
        /// null.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Initializes a new instance of the MoodChangedEventArgs class.
        /// </summary>
        /// <param name="jid">The JID of the XMPP entity that published the
        /// mood information.</param>
        /// <param name="mood">One of the values from the Mood enumeration.</param>
        /// <param name="description">A natural-language description of, or
        /// reason for, the mood.</param>
        /// <exception cref="ArgumentNullException">The jid parameter is
        /// null.</exception>
        public MoodChangedEventArgs(Jid jid, Mood mood, string description = null)
        {
            jid.ThrowIfNull(nameof(jid));
            Jid = jid;
            Mood = mood;
            Description = description;
        }
    }
}