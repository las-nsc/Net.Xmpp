﻿using System;

namespace Net.Xmpp.Core
{
    /// <summary>
    /// Provides data for the Presence event.
    /// </summary>
    public class PresenceEventArgs : EventArgs
    {
        /// <summary>
        /// The Presence stanza.
        /// </summary>
        public Presence Stanza { get; }

        /// <summary>
        /// Initializes a new instance of the PresenceEventArgs class.
        /// </summary>
        /// <param name="stanza">The Presence stanza on whose behalf the event is
        /// raised.</param>
        /// <exception cref="ArgumentNullException">The stanza parameter is
        /// null.</exception>
        public PresenceEventArgs(Presence stanza)
        {
            stanza.ThrowIfNull(nameof(stanza));
            Stanza = stanza;
        }
    }
}