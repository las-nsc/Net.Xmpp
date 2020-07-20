using System;

namespace Net.Xmpp.Im
{
    /// <summary>
    /// Possible Connection events raised.
    /// </summary>
    /// <remarks>For implementation details, refer to RFC 3921.</remarks>
    public enum ConnectionState { Connected, Disconnected, Lost }

    public class ConnectEventArgs : EventArgs
    {
        public ConnectionState State
        {
            get;
            private set;
        }

        public ConnectEventArgs(ConnectionState state)
        {
            State = state;
        }
    }
}
