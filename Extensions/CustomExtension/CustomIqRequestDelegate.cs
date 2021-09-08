namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Invoked when a CustomIqRequest is made.
    /// </summary>
    /// <param name="from">The jid of the sender</param>
    /// <param name="str">The serialised data stream</param>
    /// <returns>The serialised anwser string</returns>
    public delegate string CustomIqRequestDelegate(Jid from, string str);
}