using System;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Represents a request for a file-transfer.
    /// </summary>
    /// <param name="transfer">A FileTransfer object containing information about
    /// the file that is being offered.</param>
    /// <param name="result">The callback path where the file will be saved to or null to reject
    /// the file request.</param>
    public delegate void FileTransferRequest(FileTransfer transfer, Action<string> result);
}