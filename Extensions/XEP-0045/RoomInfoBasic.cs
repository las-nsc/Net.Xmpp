namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// The most basic form of a chat room
    /// </summary>
    public class RoomInfoBasic
    {
        /// <summary>
        /// Basic room info
        /// </summary>
        /// <param name="jid">Room identifier</param>
        /// <param name="name">Room name</param>
        public RoomInfoBasic(Jid jid, string? name = null)
        {
            jid.ThrowIfNull(nameof(jid));
            Jid = jid;

            Name = string.IsNullOrWhiteSpace(name) ? jid.Node : name;
        }

        /// <summary>
        /// The JID of the room.
        /// </summary>
        public Jid Jid { get; protected set; }

        /// <summary>
        /// The name of the room.
        /// </summary>
        public string Name { get; protected set; }
    }
}
