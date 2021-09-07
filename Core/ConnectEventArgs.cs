using System;

namespace Net.Xmpp.Core
{
    public enum ConnectionState { Connected, Disconnected, Lost }
    public class ConnectEventArgs : EventArgs
    {
        public ConnectionState State { get; }

        public ConnectEventArgs(ConnectionState state)
        {
            State = state;
        }
    }
}
