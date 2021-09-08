using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

using Net.Xmpp.Core;
using Net.Xmpp.Extensions.Dataforms;
using Net.Xmpp.Im;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Implements the 'SI File Transfer' extension as defined in XEP-0096.
    /// </summary>
    internal class SIFileTransfer : XmppExtension
    {
        /// <summary>
        /// A reference to the 'Stream Initiation' extension.
        /// </summary>
        private readonly StreamInitiation streamInitiation;

        /// <summary>
        /// A dictionary of negotiated sessions for file-transfers.
        /// </summary>
        private readonly ConcurrentDictionary<string, SISession> siSessions = new();

        /// <summary>
        /// A dictionary of file meta-data.
        /// </summary>
        private readonly ConcurrentDictionary<string, FileMetaData> metaData = new();

        /// <summary>
        /// There is no easy way to determine the mime-type of a file
        /// so we use this generic mime-type for stream initiation.
        /// </summary>
        private const string mimeType = "application/octet-stream";

        /// <summary>
        /// An array of data-stream methods that we support. As per specification,
        /// we must support SOCKS5 and In-Band Bytestreams.
        /// </summary>
        private static readonly Type[] supportedMethods = new Type[] {
            typeof(Socks5Bytestreams),
            typeof(InBandBytestreams)
        };

        /// <summary>
        /// An enumerable collection of XMPP namespaces the extension implements.
        /// </summary>
        /// <remarks>This is used for compiling the list of supported extensions
        /// advertised by the 'Service Discovery' extension.</remarks>
        public override IEnumerable<string> Namespaces => new string[] {
                    "http://jabber.org/protocol/si/profile/file-transfer"
                };

        /// <summary>
        /// The named constant of the Extension enumeration that corresponds to this
        /// extension.
        /// </summary>
        public override Extension Xep => Extension.SIFileTransfer;

        /// <summary>
        /// A callback method to invoke when a request for a file-transfer is received
        /// from another XMPP entity.
        /// </summary>
        public FileTransferRequest? TransferRequest { get; set; }

        /// <summary>
        /// Determines whether the in-band bytestreams method should be used, even if
        /// the preferred SOCKS5 method is available.
        /// </summary>
        public bool ForceInBandBytestreams { get; set; }

        /// <summary>
        /// The event that is raised periodically for every file-transfer to
        /// inform subscribers of the progress of the respective transfer operation.
        /// </summary>
        public event EventHandler<FileTransferProgressEventArgs>? FileTransferProgress;

        /// <summary>
        /// The event that is raised when an on-going file-transfer has been
        /// aborted prematurely, either due to cancellation or error.
        /// </summary>
        public event EventHandler<FileTransferAbortedEventArgs>? FileTransferAborted;

        /// <summary>
        /// Retrieves the SISession instance with the specified session id and
        /// with the specified sender and receiver.
        /// </summary>
        /// <param name="sid">The id of the session to retrieve.</param>
        /// <param name="from">The JID of the sender.</param>
        /// <param name="to">The JID of the receiver.</param>
        /// <returns>The SISession instance with the specified attributes, or null
        /// if no such SISession instance exists.</returns>
        /// <exception cref="ArgumentNullException">The sid parameter or the from
        /// parameter or the to parameter is null.</exception>
        public SISession? GetSession(string sid, Jid from, Jid to)
        {
            sid.ThrowIfNull(nameof(sid));
            from.ThrowIfNull(nameof(from));
            to.ThrowIfNull(nameof(to));
            return siSessions.TryGetValue(sid, out var value)
                ? value.From == from && value.To == to ? value
                : null : null;
        }

        /// <summary>
        /// Invalidates the session with the specified session id.
        /// </summary>
        /// <param name="sid">The id of the session to invalidate.</param>
        /// <exception cref="ArgumentNullException">The sid parameter
        /// is null.</exception>
        public void InvalidateSession(string sid)
        {
            sid.ThrowIfNull(nameof(sid));
            if (siSessions.TryRemove(sid, out var value))
            {
                value.Stream?.Dispose();
            }
        }

        /// <summary>
        /// Offers the specified file to the XMPP user with the specified JID and, if
        /// accepted by the user, transfers the file.
        /// </summary>
        /// <param name="to">The JID of the XMPP user to offer the file to.</param>
        /// <param name="path">The path of the file to transfer.</param>
        /// <param name="description">A description of the file so the receiver can
        /// better understand what is being sent.</param>
        /// <param name="cb">a callback method invoked once the other site has
        /// accepted or rejected the file-transfer request.</param>
        /// <returns>Sid of file transfer</returns>
        /// <exception cref="ArgumentNullException">The to parameter or the path
        /// parameter is null.</exception>
        /// <exception cref="ArgumentException">path is a zero-length string,
        /// contains only white space, or contains one or more invalid
        /// characters.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name,
        /// or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is
        /// invalid, (for example, it is on an unmapped drive).</exception>
        /// <exception cref="UnauthorizedAccessException">path specified a
        /// directory, or the caller does not have the required
        /// permission.</exception>
        /// <exception cref="FileNotFoundException">The file specified in path
        /// was not found.</exception>
        /// <exception cref="NotSupportedException">path is in an invalid
        /// format, or the XMPP entity with the specified JID does not support
        /// the 'SI File Transfer' XMPP extension.</exception>
        /// <exception cref="XmppErrorException">The server or the XMPP entity
        /// with the specified JID returned an XMPP error code. Use the Error
        /// property of the XmppErrorException to obtain the specific error
        /// condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or
        /// another unspecified XMPP error occurred.</exception>
        public string InitiateFileTransfer(Jid to, string path,
            string? description = null, Action<bool, FileTransfer>? cb = null)
        {
            to.ThrowIfNull(nameof(to));
            path.ThrowIfNull(nameof(path));
            FileInfo info = new(path);
            return InitiateFileTransfer(to, File.OpenRead(path), info.Name, info.Length,
                description, cb);
        }

        /// <summary>
        /// Offers the XMPP user with the specified JID the file with the specified
        /// name and, if accepted by the user, transfers the file using the supplied
        /// stream.
        /// </summary>
        /// <param name="to">The JID of the XMPP user to offer the file to.</param>
        /// <param name="stream">The stream to read the file-data from.</param>
        /// <param name="name">The name of the file, as offered to the XMPP user
        /// with the specified JID.</param>
        /// <param name="size">The number of bytes to transfer.</param>
        /// <param name="description">A description of the file so the receiver can
        /// better understand what is being sent.</param>
        /// <param name="cb">A callback method invoked once the other site has
        /// accepted or rejected the file-transfer request.</param>
        /// <returns>Sid of file transfer</returns>
        /// <exception cref="ArgumentNullException">The to parameter or the stream
        /// parameter or the name parameter is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The value of the size
        /// parameter is negative.</exception>
        /// <exception cref="NotSupportedException">The XMPP entity with the
        /// specified JID does not support the 'SI File Transfer' XMPP
        /// extension.</exception>
        /// <exception cref="XmppErrorException">The server or the XMPP entity
        /// with the specified JID returned an XMPP error code. Use the Error
        /// property of the XmppErrorException to obtain the specific error
        /// condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or
        /// another unspecified XMPP error occurred.</exception>
        public string InitiateFileTransfer(Jid to, Stream stream, string name, long size,
            string? description = null, Action<bool, FileTransfer>? cb = null)
        {
            to.ThrowIfNull(nameof(to));
            stream.ThrowIfNull(nameof(stream));
            name.ThrowIfNull(nameof(name));
            size.ThrowIfOutOfRange(0, long.MaxValue);
            //FIXME FIXME
            //if (!ecapa.Supports(to, Extension.SIFileTransfer)) {
            //    throw new NotSupportedException("The XMPP entity does not support the " +
            //        "'SI File Transfer' extension.");
            //}
            //FIXME FIXME
            // Perform stream-initiation asynchronously so that the caller is not
            // blocked until the other site has either accepted or rejected our offer.
            return InitiateStream(to, name, size, description,
                (result, iq) => OnInitiationResult(result, to, name, stream, size, description, cb));
        }

        /// <summary>
        /// Cancels the specified file-transfer.
        /// </summary>
        /// <param name="sid">Sid</param>
        /// <param name="from">From Jid</param>
        /// <param name="to">To Jid</param>
        /// <exception cref="ArgumentNullException">The transfer parameter is
        /// null.</exception>
        /// <exception cref="ArgumentException">The specified transfer instance does
        /// not represent an active data-transfer operation.</exception>
        public void CancelFileTransfer(string sid, Jid from, Jid to)
        {
            sid.ThrowIfNullOrEmpty("sid");
            from.ThrowIfNull(nameof(from));
            to.ThrowIfNull(nameof(to));
            var session = GetSession(sid, from, to);
            if (session == null)
            {
                throw new ArgumentException("The specified transfer instance does not represent an active data-transfer operation: " +
                    $"sid {sid}, from {from}, to {to}");
            }

            session.Extension.CancelTransfer(session);
        }

        /// <summary>
        /// Cancels the specified file-transfer.
        /// </summary>
        /// <param name="transfer">The file-transfer to cancel.</param>
        /// <exception cref="ArgumentNullException">The transfer parameter is
        /// null.</exception>
        /// <exception cref="ArgumentException">The specified transfer instance does
        /// not represent an active data-transfer operation.</exception>
        public void CancelFileTransfer(FileTransfer transfer)
        {
            transfer.ThrowIfNull(nameof(transfer));
            transfer.SessionId.ThrowIfNull(nameof(transfer.SessionId));
            var session = GetSession(transfer.SessionId!, transfer.From, transfer.To);
            if (session == null)
            {
                throw new ArgumentException("The specified transfer instance does not " +
                    "represent an active data-transfer operation.");
            }
            session.Extension.CancelTransfer(session);
        }

        /// <summary>
        /// Initializes a new instance of the SIFileTransfer class.
        /// </summary>
        /// <param name="im">A reference to the XmppIm instance on whose behalf this
        /// instance is created.</param>
        public SIFileTransfer(XmppIm im, StreamInitiation streamInitiation)
            : base(im)
        {
            this.streamInitiation = streamInitiation;

            // Register the 'file-transfer' profile.
            streamInitiation.RegisterProfile(
                "http://jabber.org/protocol/si/profile/file-transfer",
                OnStreamInitiationRequest
            );
        }

        /// <summary>
        /// Invoked whenever a 'Stream Initiation' request for file transfers
        /// is received.
        /// </summary>
        /// <param name="from">The JID of the XMPP entity that wishes to initiate
        /// the data-stream.</param>
        /// <param name="si">The 'si' element of the request.</param>
        /// <param name="result">The response to the SI request or an error element to include
        /// in the IQ response.</param>
        private void OnStreamInitiationRequest(Jid from, XmlElement si, Action<XmlElement> result)
        {
            // Sign up for the 'BytesTransferred' and 'TransferAborted' events of each
            // data-stream extension that we support.
            // Must be done after constructor so SiFileTransfer is registered as loaded
            foreach (var type in supportedMethods)
            {
                var ext = im.LoadExtension(type);
                if (ext is not IDataStream dataStream)
                    throw new XmppException("Invalid data-stream type: " + type);
                dataStream.BytesTransferred += OnBytesTransferred;
                dataStream.TransferAborted += OnTransferAborted;
            }

            try
            {
                string method = SelectStreamMethod(si["feature"]);
                // If the session-id is already in use, we cannot process the request.
                string sid = si.GetAttribute("id");
                if (!(sid?.Length > 0) || siSessions.ContainsKey(sid))
                {
                    result(new XmppError(ErrorType.Cancel, ErrorCondition.Conflict).Data);
                    return;
                }
                // Extract file information and hand to user.
                var file = si["file"];
                var desc = file["desc"]?.InnerText;
                var name = file.GetAttribute("name");
                int size = int.Parse(file.GetAttribute("size"));
                FileTransfer transfer = new(from, im.Jid, name, size, sid, desc);
                TransferRequest?.Invoke(transfer, (string savePath) =>
                {
                    try
                    {
                        // User has rejected the request.
                        if (savePath == null)
                        {
                            result(new XmppError(ErrorType.Cancel, ErrorCondition.NotAcceptable).Data);
                        }
                        else if (im.GetExtension(method) is IDataStream dataStream)
                        {
                            // Create an SI session instance.
                            SISession session = new(sid, File.OpenWrite(savePath),
                                size, true, from, im.Jid, dataStream);
                            siSessions.TryAdd(sid, session);
                            // Store the file's meta data.
                            metaData.TryAdd(sid, new FileMetaData(name, desc));
                            // Construct and return the negotiation result.
                            result(Xml.Element("si", "http://jabber.org/protocol/si").Child(
                                FeatureNegotiation.Create(new SubmitForm(
                                    new ListField("stream-method", method)))));
                        }
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception raised", e.ToString() + e.StackTrace);
                        result(new XmppError(ErrorType.Cancel, ErrorCondition.BadRequest).Data);
                    }
                });
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception raised", e.ToString() + e.StackTrace);
                result(new XmppError(ErrorType.Cancel, ErrorCondition.BadRequest).Data);
            }
        }

        /// <summary>
        /// Determines whether we support the specified extension namespace.
        /// </summary>
        /// <param name="namespace">The XML namespace to probe for.</param>
        /// <returns>true if our implementation supports the extension denoted by
        /// the specified namespace; Otherwise false.</returns>
        /// <exception cref="ArgumentNullException">The namespace parameter is
        /// null.</exception>
        private bool SupportsNamespace(string @namespace)
        {
            @namespace.ThrowIfNull(nameof(@namespace));
            foreach (var ext in im.Extensions)
            {
                foreach (string ns in ext.Namespaces)
                {
                    if (ns == @namespace)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Selects a stream-method from the list of advertised methods.
        /// </summary>
        /// <param name="feature">The 'feature' element containing the data-form
        /// with advertised stream-methods.</param>
        /// <returns>The selected stream-method.</returns>
        /// <exception cref="ArgumentException">None of the advertised methods is
        /// supported.</exception>
        private string SelectStreamMethod(XmlElement feature)
        {
            // See if we support any of the advertised stream-methods.
            DataForm form = FeatureNegotiation.Parse(feature);
            if (form.Fields["stream-method"] is not ListField field)
                throw new ArgumentNullException("stream-method");

            // Order of preference: Socks5, Ibb.
            string[] methods = new string[] {
                "http://jabber.org/protocol/bytestreams",
                "http://jabber.org/protocol/ibb"
            };
            for (int i = 0; i < methods.Length; i++)
            {
                if (ForceInBandBytestreams && methods[i] != "http://jabber.org/protocol/ibb")
                    continue;

                if (field.Values.Contains(methods[i]))
                    return methods[i];
            }
            throw new ArgumentException("No supported method advertised.");
        }

        /// <summary>
        /// Compiles and returns a list of namespaces representing the different
        /// stream-methods this implementation supports.
        /// </summary>
        /// <returns>An enumerable collection of namespaces representing the different
        /// stream-methods the implementation supports.</returns>
        private IEnumerable<string> GetStreamMethods()
        {
            ISet<string> set = new HashSet<string>();
            foreach (var type in supportedMethods)
            {
                // If forcing IBB, only advertise IBB to the other site.
                if (ForceInBandBytestreams && type != typeof(InBandBytestreams))
                    continue;
                if (im.LoadExtension(type) is { } ext)
                {
                    foreach (string ns in ext.Namespaces)
                        set.Add(ns);
                }
            }
            return set;
        }

        /// <summary>
        /// Initiates an XMPP data-stream using the SI file-transfer profile.
        /// </summary>
        /// <param name="to">The JID of the XMPP entity to initiate a data-stream
        /// with.</param>
        /// <param name="name">The name of the file to transfer accross the
        /// data-stream.</param>
        /// <param name="size">The size of the file to transfer accross the
        /// data-stream, in bytes.</param>
        /// <param name="description">A description of the file so the receiver can
        /// better understand what is being sent.</param>
        /// <param name="cb">A callback method to invoke once the result of the
        /// stream-initation operation has been received.</param>
        /// <returns>The stream initiation result, containing the session identifier
        /// as well as the negotiated stream-method.</returns>
        /// <exception cref="NotSupportedException">The XMPP entity with
        /// the specified JID does not support the 'Stream Initiation' XMPP
        /// extension.</exception>
        /// <exception cref="XmppErrorException">The server or the receiving XMPP
        /// entity returned an XMPP error code. Use the Error property of the
        /// XmppErrorException to obtain the specific error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        private string InitiateStream(Jid to, string name, long size,
            string? description = null, Action<InitiationResult?, Iq>? cb = null)
        {
            // Construct the 'file' element which is mandatory for the SI file-transfer
            // profile.
            var file = Xml.Element("file",
                "http://jabber.org/protocol/si/profile/file-transfer")
                .Attr("name", name)
                .Attr("size", size.ToString());
            if (description != null)
                file.Child(Xml.Element("desc").Text(description));
            // Collect namespaces of stream-methods that we can offer the other site.
            var methods = GetStreamMethods();
            // Try to initiate an XMPP data-stream.
            return streamInitiation.InitiateStream(to, mimeType,
                "http://jabber.org/protocol/si/profile/file-transfer", methods, file, cb);
        }

        /// <summary>
        /// Invoked once the result of a pending stream-initiation operation has been
        /// received.
        /// </summary>
        /// <param name="result">The result of the stream-initiation operation. If
        /// this parameter is null, stream-initiation failed.</param>
        /// <param name="to">The JID of the XMPP user to offer the file to.</param>
        /// <param name="name">The name of the file, as offered to the XMPP user
        /// with the specified JID.</param>
        /// <param name="stream">The stream to read the file-data from.</param>
        /// <param name="size">The number of bytes to transfer.</param>
        /// <param name="description">A description of the file so the receiver can
        /// better understand what is being sent.</param>
        /// <param name="cb">A callback method invoked once the other site has
        /// accepted or rejected the file-transfer request.</param>
        /// <remarks>This is called in the context of an arbitrary thread.</remarks>
        private void OnInitiationResult(InitiationResult? result, Jid to, string name,
            Stream stream, long size, string? description, Action<bool, FileTransfer>? cb)
        {
            try
            {
                if (result is null)
                    throw new ArgumentNullException(nameof(result));

                FileTransfer transfer = new(im.Jid, to, name, size, result.SessionId, description);
                // Get the instance of the data-stream extension that the other site has
                // selected.
                if (im.GetExtension(result.Method) is not IDataStream ext) return;
                // Register the session.
                SISession session = new(result.SessionId, stream, size, false, im.Jid, to, ext);
                siSessions.TryAdd(result.SessionId, session);
                // Store the file's meta data.
                metaData.TryAdd(result.SessionId, new FileMetaData(name, description));
                // Invoke user-provided callback.
                cb?.Invoke(true, transfer);
                // Perform the actual data-transfer.
                try
                {
                    ext.Transfer(session);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message + e.StackTrace + e.ToString());
                    // Nothing to do here.
                }
            }
            catch
            {
                FileTransfer transfer = new(im.Jid, to, name, size, null, description);
                // Something went wrong. Invoke user-provided callback to let them know
                // the file-transfer can't be performed.
                cb?.Invoke(false, transfer);
            }
        }

        /// <summary>
        /// Callback method invoked whenever bytes have been transferred.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments containing the SISession instance for
        /// which bytes have been transferred.</param>
        private void OnBytesTransferred(object sender, BytesTransferredEventArgs e)
        {
            // Get the Metadata of the file.
            if (metaData.TryGetValue(e.Session.Sid, out FileMetaData meta))
            {
                // Raise the 'FileTransferProgress' event.
                FileTransferProgress?.Invoke(this, new(new(e.Session, meta.Name, meta.Description)));
            }
        }

        /// <summary>
        /// Callback mthod invoked when an on-going transfer has been aborted.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments containing the SISession instance
        /// whose data-transfer has been aborted.</param>
        private void OnTransferAborted(object sender, TransferAbortedEventArgs e)
        {
            // Get the Metadata of the file.
            if (metaData.TryGetValue(e.Session.Sid, out FileMetaData meta))
            {
                // Raise the 'FileTransferAborted' event.
                FileTransferAborted?.Invoke(this, new(new(e.Session, meta.Name, meta.Description)));
            }
        }
    }
}