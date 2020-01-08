using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharp.Xmpp.Core
{
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
