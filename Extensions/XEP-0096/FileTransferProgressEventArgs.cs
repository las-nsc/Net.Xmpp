using System;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Provides data for the 'FileTransferProgress' event.
    /// </summary>
    [Serializable]
    public class FileTransferProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Contains information about the file-transfer on whose behalf the
        /// event has been raised.
        /// </summary>
        public FileTransfer Transfer { get; }

        /// <summary>
        /// Initializes a new instance of the FileTransferProgressEventArgs class.
        /// </summary>
        /// <param name="transfer">A FileTransfer object representing the file-transfer
        /// operation for which the event is being raised.</param>
        /// <exception cref="ArgumentNullException">The transfer parameter is
        /// null.</exception>
        internal FileTransferProgressEventArgs(FileTransfer transfer)
        {
            transfer.ThrowIfNull(nameof(transfer));
            Transfer = transfer;
        }
    }
}