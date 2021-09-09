using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Threading.Tasks;

using Net.Xmpp.Extensions;
using Net.Xmpp.Extensions.Dataforms;
using Net.Xmpp.Im;

namespace Net.Xmpp.Client
{
    /// <summary>
    /// Implements an XMPP client providing basic instant messaging (IM) and
    /// presence functionality as well as various XMPP extension functionality.
    /// </summary>
    /// <remarks>
    /// This provides most of the functionality exposed by the XmppIm class but
    /// simplifies some of the more complicated aspects such as privacy lists and
    /// roster management. It also implements various XMPP protocol extensions.
    /// </remarks>
    public class XmppClient : IDisposable
    {
        /// <summary>
        /// True if the instance has been disposed of.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Provides access to the 'Message Archiving' XMPP extension functionality.
        /// </summary>
        private readonly MessageArchiving messageArchiving;

        /// <summary>
        /// Provices access to the 'Message Archive management' XMPP extension functionality.
        /// </summary>
        private readonly MessageArchiveManagement messageArchiveManagement;

        /// <summary>
        /// Provides access to the 'Software Version' XMPP extension functionality.
        /// </summary>
        private readonly SoftwareVersion version;

        /// <summary>
        /// Provides access to the 'Service Discovery' XMPP extension functionality.
        /// </summary>
        private readonly ServiceDiscovery sdisco;

        /// <summary>
        /// Provides access to the 'Entity Capabilities' XMPP extension functionality.
        /// </summary>
        private readonly EntityCapabilities ecapa;

        /// <summary>
        /// Provides access to the 'Ping' XMPP extension functionality.
        /// </summary>
        private readonly Ping ping;

        /// <summary>
        /// Provides access to the 'Custom Iq Extension' functionality
        /// </summary>
        private readonly CustomIqExtension cusiqextension;

        /// <summary>
        /// Provides access to the 'Attention' XMPP extension functionality.
        /// </summary>
        private readonly Attention attention;

        /// <summary>
        /// Provides access to the 'Entity Time' XMPP extension functionality.
        /// </summary>
        private readonly EntityTime time;

        /// <summary>
        /// Provides access to the 'Blocking Command' XMPP extension functionality.
        /// </summary>
        private readonly BlockingCommand block;

        /// <summary>
        /// Provides access to the 'Personal Eventing Protocol' extension.
        /// </summary>
        private readonly Pep pep;

        /// <summary>
        /// Provides access to the 'User Tune' XMPP extension functionality.
        /// </summary>
        private readonly UserTune userTune;

        /// <summary>
        /// Provides access to the "Multi-User Chat" XMPP extension functionality.
        /// </summary>
        private readonly MultiUserChat groupChat;

#if WINDOWSPLATFORM
        /// <summary>
        /// Provides access to the 'User Avatar' XMPP extension functionality.
        /// </summary>
        private UserAvatar userAvatar;
#endif

        /// <summary>
        /// Provides access to the 'User Mood' XMPP extension functionality.
        /// </summary>
        private readonly UserMood userMood;

        /// <summary>
        /// Provides access to the 'Data Forms' XMPP extension functionality.
        /// </summary>
        private readonly DataForms dataForms;

        /// <summary>
        /// Provides access to the 'Feature Negotiation' XMPP extension.
        /// </summary>
        private readonly FeatureNegotiation featureNegotiation;

        /// <summary>
        /// Provides access to the 'Stream Initiation' XMPP extension.
        /// </summary>
        private readonly StreamInitiation streamInitiation;

        /// <summary>
        /// Provides access to the 'SI File Transfer' XMPP extension.
        /// </summary>
        private readonly SIFileTransfer siFileTransfer;

        /// <summary>
        /// Provides access to the 'In-Band Bytestreams' XMPP extension.
        /// </summary>
        private readonly InBandBytestreams inBandBytestreams;

        /// <summary>
        /// Provides access to the 'User Activity' XMPP extension.
        /// </summary>
        private readonly UserActivity userActivity;

        /// <summary>
        /// Provides access to the 'Socks5 Bytestreams' XMPP extension.
        /// </summary>
        private readonly Socks5Bytestreams socks5Bytestreams;

        /// <summary>
        /// Provides access to the 'Server IP Check' XMPP extension.
        /// </summary>
        private readonly ServerIpCheck serverIpCheck;

        /// <summary>
        /// Provides access to the 'In-Band Registration' XMPP extension.
        /// </summary>
        private readonly InBandRegistration inBandRegistration;

        /// <summary>
        /// Provides access to the 'Chat State Nofitications' XMPP extension.
        /// </summary>
        private readonly ChatStateNotifications chatStateNotifications;

        /// <summary>
        /// Provides access to the 'Bits of Binary' XMPP extension.
        /// </summary>
        private readonly BitsOfBinary bitsOfBinary;

        /// <summary>
        /// Provides vcard Based Avatar functionality
        /// </summary>
        private readonly VCardAvatars vcardAvatars;

        /// <summary>
        /// Provides vcard functionality
        /// </summary>
        private readonly VCards vcard;

        /// <summary>
        /// Provides the Message Carbons extension
        /// </summary>
        private readonly MessageCarbons messageCarbons;

        /// <summary>
        /// Provides the Jabber Search extension
        /// </summary>
        private readonly JabberSearch search;

        /// <summary>
        /// Provides the HTTP File Upload extension
        /// </summary>
        private readonly HTTPFileUpload httpUpload;

        /// <summary>
        /// The hostname of the XMPP server to connect to.
        /// </summary>
        public string Hostname
        {
            get => Im.Hostname;
            set => Im.Hostname = value;
        }

        /// <summary>
        /// The port number of the XMPP service of the server.
        /// </summary>
        public int Port
        {
            get => Im.Port;
            set => Im.Port = value;
        }

        /// <summary>
        /// The username with which to authenticate. In XMPP jargon this is known
        /// as the 'node' part of the JID.
        /// </summary>
        public string Username
        {
            get => Im.Username;
            set => Im.Username = value;
        }

        /// <summary>
        /// The password with which to authenticate.
        /// </summary>
        public string Password
        {
            get => Im.Password;
            set => Im.Password = value;
        }

        /// <summary>
        /// If true the session will be TLS/SSL-encrypted if the server supports it.
        /// </summary>
        public bool Tls
        {
            get => Im.Tls;
            set => Im.Tls = value;
        }

        /// <summary>
        /// A delegate used for verifying the remote Secure Sockets Layer (SSL)
        /// certificate which is used for authentication.
        /// </summary>
        public RemoteCertificateValidationCallback? Validate
        {
            get => Im.Validate;
            set => Im.Validate = value;
        }

        /// <summary>
        /// Determines whether the session with the server is TLS/SSL encrypted.
        /// </summary>
        public bool IsEncrypted => Im.IsEncrypted;

        /// <summary>
        /// The address of the Xmpp entity.
        /// </summary>
        public Jid? Jid => Im.Jid;

        /// <summary>
        /// Determines whether the instance is connected to the XMPP server.
        /// </summary>
        public bool Connected => Im?.Connected == true;

        /// <summary>
        /// The event that is raised when a connection state changed.
        /// </summary>
        public event EventHandler<ConnectEventArgs>? OnConnect
        {
            add => Im.OnConnect += value;
            remove => Im.OnConnect -= value;
        }

        /// <summary>
        /// Determines whether the instance has been authenticated.
        /// </summary>
        public bool Authenticated => Im.Authenticated;

        /// <summary>
        /// The default IQ Set Time out in Milliseconds. -1 means no timeout
        /// </summary>
        public int DefaultTimeOut
        {
            get => Im.DefaultTimeOut;
            set => Im.DefaultTimeOut = value;
        }

        /// <summary>
        /// If true prints XML stanzas
        /// </summary>
        public bool DebugStanzas
        {
            get => Im.DebugStanzas;
            set => Im.DebugStanzas = value;
        }

        /// <summary>
        /// Contains settings for configuring file-transfer options.
        /// </summary>
        public FileTransferSettings? FileTransferSettings { get; private set; }

        /// <summary>
        /// The underlying XmppIm instance.
        /// </summary>
        public XmppIm Im { get; private set; }

        /// <summary>
        /// A callback method to invoke when a request for a subscription is received
        /// from another XMPP user.
        /// </summary>
        /// <include file='Examples.xml' path='S22/Xmpp/Client/XmppClient[@name="SubscriptionRequest"]/*'/>
        public SubscriptionRequest? SubscriptionRequest
        {
            get => Im.SubscriptionRequest;
            set => Im.SubscriptionRequest = value;
        }

        /// <summary>
        /// A callback method to invoke when a request for voice is received
        /// from another XMPP user.
        /// </summary>
        public RegistrationCallback? VoiceRequestedInGroupChat
        {
            get => groupChat?.VoiceRequested;
            set { if (groupChat is not null) groupChat.VoiceRequested = value; }
        }

        /// <summary>
        /// The event that is raised when a status notification has been received.
        /// </summary>
        public event EventHandler<StatusEventArgs>? StatusChanged
        {
            add => Im.Status += value;
            remove => Im.Status -= value;
        }

        /// <summary>
        /// The event that is raised when a mood notification has been received.
        /// </summary>
        public event EventHandler<MoodChangedEventArgs>? MoodChanged
        {
            add => userMood.MoodChanged += value;
            remove => userMood.MoodChanged -= value;
        }

        /// <summary>
        /// The event that is raised when an activity notification has been received.
        /// </summary>
        public event EventHandler<ActivityChangedEventArgs>? ActivityChanged
        {
            add => userActivity.ActivityChanged += value;
            remove => userActivity.ActivityChanged -= value;
        }

#if WINDOWSPLATFORM
        /// <summary>
        /// The event that is raised when a contact has updated his or her avatar.
        /// </summary>
        public event EventHandler<AvatarChangedEventArgs>? AvatarChanged {
            add => userAvatar.AvatarChanged += value;
            remove => userAvatar.AvatarChanged -= value;
        }
#endif

        /// <summary>
        /// The event that is raised when a contact has published tune information.
        /// </summary>
        public event EventHandler<TuneEventArgs>? Tune
        {
            add => userTune.Tune += value;
            remove => userTune.Tune -= value;
        }

        /// <summary>
        /// The event that is raised when a chat message is received.
        /// </summary>
        public event EventHandler<MessageEventArgs>? Message
        {
            add => Im.Message += value;
            remove => Im.Message -= value;
        }

        /// <summary>
        /// The event that is raised when an error message is received.
        /// </summary>
        public event EventHandler<MessageEventArgs>? ErrorMessage
        {
            add => Im.ErrorMessage += value;
            remove => Im.ErrorMessage -= value;
        }

        /// <summary>
        /// The event that is raised when the subject is changed in a group chat.
        /// </summary>
        public event EventHandler<MessageEventArgs>? GroupChatSubjectChanged
        {
            add => groupChat.SubjectChanged += value;
            remove => groupChat.SubjectChanged -= value;
        }

        /// <summary>
        /// Edit Subject of a group chat.
        /// </summary>
        public void EditRoomSubject(Jid room, string subject)
        {
            AssertValid();
            groupChat.EditRoomSubject(room, subject);
        }

        /// <summary>
        /// The event that is raised when a participant's presence is changed in a group chat.
        /// </summary>
        public event EventHandler<GroupPresenceEventArgs>? GroupPresenceChanged
        {
            add => groupChat.PrescenceChanged += value;
            remove => groupChat.PrescenceChanged -= value;
        }

        /// <summary>
        /// The event that is raised when an invite to a group chat is received.
        /// </summary>
        public event EventHandler<GroupInviteEventArgs>? GroupInviteReceived
        {
            add => groupChat.InviteReceived += value;
            remove => groupChat.InviteReceived -= value;
        }

        /// <summary>
        /// The event that is raised when an invite to a group chat is declined.
        /// </summary>
        public event EventHandler<GroupInviteDeclinedEventArgs>? GroupInviteDeclined
        {
            add => groupChat.InviteWasDeclined += value;
            remove => groupChat.InviteWasDeclined -= value;
        }

        /// <summary>
        /// The event that is raised when the server responds with an error in relation to a group chat.
        /// </summary>
        public event EventHandler<GroupErrorEventArgs>? GroupMucError
        {
            add => groupChat.MucErrorResponse += value;
            remove => groupChat.MucErrorResponse -= value;
        }

        /// <summary>
        /// The event that is raised periodically for every file-transfer operation to
        /// inform subscribers of the progress of the operation.
        /// </summary>
        public event EventHandler<FileTransferProgressEventArgs>? FileTransferProgress
        {
            add => siFileTransfer.FileTransferProgress += value;
            remove => siFileTransfer.FileTransferProgress -= value;
        }

        /// <summary>
        /// The event that is raised when an on-going file-transfer has been aborted
        /// prematurely, either due to cancellation or error.
        /// </summary>
        public event EventHandler<FileTransferAbortedEventArgs>? FileTransferAborted
        {
            add => siFileTransfer.FileTransferAborted += value;
            remove => siFileTransfer.FileTransferAborted -= value;
        }

        /// <summary>
        /// The event that is raised when the chat-state of an XMPP entity has
        /// changed.
        /// </summary>
        public event EventHandler<ChatStateChangedEventArgs>? ChatStateChanged
        {
            add => chatStateNotifications.ChatStateChanged += value;
            remove => chatStateNotifications.ChatStateChanged -= value;
        }

        /// <summary>
        /// The event that is raised when the roster of the user has been updated,
        /// i.e. a contact has been added, removed or updated.
        /// </summary>
        public event EventHandler<RosterUpdatedEventArgs>? RosterUpdated
        {
            add => Im.RosterUpdated += value;
            remove => Im.RosterUpdated -= value;
        }

        /// <summary>
        /// The event that is raised when a user or resource has unsubscribed from
        /// receiving presence notifications of the JID associated with this instance.
        /// </summary>
        public event EventHandler<UnsubscribedEventArgs>? Unsubscribed
        {
            add => Im.Unsubscribed += value;
            remove => Im.Unsubscribed -= value;
        }

        /// <summary>
        /// The event that is raised when a subscription request made by the JID
        /// associated with this instance has been approved.
        /// </summary>
        public event EventHandler<SubscriptionApprovedEventArgs>? SubscriptionApproved
        {
            add => Im.SubscriptionApproved += value;
            remove => Im.SubscriptionApproved -= value;
        }

        /// <summary>
        /// The event that is raised when a subscription request made by the JID
        /// associated with this instance has been refused.
        /// </summary>
        public event EventHandler<SubscriptionRefusedEventArgs>? SubscriptionRefused
        {
            add => Im.SubscriptionRefused += value;
            remove => Im.SubscriptionRefused -= value;
        }

        /// <summary>
        /// The event that is raised when an unrecoverable error condition occurs.
        /// </summary>
        public event EventHandler<Im.ErrorEventArgs> Error
        {
            add => Im.Error += value;
            remove => Im.Error -= value;
        }

        /// <summary>
        /// Initializes a new instance of the XmppClient class.
        /// </summary>
        /// <param name="hostname">The hostname of the XMPP server to connect to.</param>
        /// <param name="username">The username with which to authenticate. In XMPP jargon
        /// this is known as the 'node' part of the JID.</param>
        /// <param name="password">The password with which to authenticate.</param>
        /// <param name="port">The port number of the XMPP service of the server.</param>
        /// <param name="tls">If true the session will be TLS/SSL-encrypted if the server
        /// supports TLS/SSL-encryption.</param>
        /// <param name="validate">A delegate used for verifying the remote Secure Sockets
        /// Layer (SSL) certificate which is used for authentication. Can be null if not
        /// needed.</param>
        /// <param name="serveradress">Adress if hostname is diferrent from resolution name</param>
        /// <exception cref="ArgumentNullException">The hostname parameter or the
        /// username parameter or the password parameter is null.</exception>
        /// <exception cref="ArgumentException">The hostname parameter or the username
        /// parameter is the empty string.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The value of the port parameter
        /// is not a valid port number.</exception>
        /// <remarks>Use this constructor if you wish to connect to an XMPP server using
        /// an existing set of user credentials.</remarks>
        public XmppClient(string hostname, string username, string password, int port = 5222, bool tls = true,
            RemoteCertificateValidationCallback? validate = null, string serveradress = "", string? resource = null, int defaultTimeoutMs = -1)
            : this(new XmppIm(hostname, username, password, port, tls, validate, serveradress, resource, defaultTimeoutMs))
        {
        }

        /// <summary>
        /// Authenticates with the XMPP server using the specified username and
        /// password.
        /// </summary>
        /// <param name="username">The username to authenticate with.</param>
        /// <param name="password">The password to authenticate with.</param>
        /// <exception cref="ArgumentNullException">The username parameter or the
        /// password parameter is null.</exception>
        /// <exception cref="System.Security.Authentication.AuthenticationException">
        /// An authentication error occured while trying to establish a secure connection,
        /// or the provided credentials were rejected by the server, or the server requires
        /// TLS/SSL and the Tls property has been set to false.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network. If the InnerException is of type SocketExcption, use the
        /// ErrorCode property to obtain the specific socket error code.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="XmppException">An XMPP error occurred while negotiating the
        /// XML stream with the server, or resource binding failed, or the initialization
        /// of an XMPP extension failed.</exception>
        public void Authenticate(string username, string password)
        {
            Im.Autenticate(username, password);
        }

        /// <summary>
        /// Authenticates with the XMPP server using the specified username and
        /// password, but not GetRosters and Presence to Server, this must be done explicit by the client.
        /// </summary>
        /// <param name="username">The username to authenticate with.</param>
        /// <param name="password">The password to authenticate with.</param>
        /// <exception cref="ArgumentNullException">The username parameter or the
        /// password parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network. If the InnerException is of type SocketExcption, use the
        /// ErrorCode property to obtain the specific socket error code.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="XmppException">An XMPP error occurred while negotiating the
        /// XML stream with the server, or resource binding failed, or the initialization
        /// of an XMPP extension failed.</exception>
        public void SimpleAutenticate(string username, string password)
        {
            Im.SimpleAutenticate(username, password);
        }

        /// <summary>
        /// Send my Presence Signal to Server.
        /// </summary>
        public void SetPresence()
        {
            // Send initial presence.
            Im.SetPresence();
        }

        /// <summary>
        /// Recconection in case of lost connection.
        /// </summary>
        public void Reconnect()
        {
            Im.Reconnect();
        }

        /// <summary>
        /// Enable this connection to receive carbon messages.
        /// </summary>
        public void EnableCarbons()
        {
            messageCarbons.EnableCarbons(true);
        }

        /// <summary>
        /// Sends a chat message with the specified content to the specified JID.
        /// </summary>
        /// <param name="to">The JID of the intended recipient.</param>
        /// <param name="body">The content of the message.</param>
        /// <param name="subject">The subject of the message.</param>
        /// <param name="thread">The conversation thread the message belongs to.</param>
        /// <param name="type">The type of the message. Can be one of the values from
        /// the MessagType enumeration.</param>
        /// <param name="language">The language of the XML character data of
        /// the stanza.</param>
        /// <exception cref="ArgumentNullException">The to parameter or the body parameter
        /// is null.</exception>
        /// <exception cref="ArgumentException">The body parameter is the empty
        /// string.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppClient instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        /// <include file='Examples.xml' path='S22/Xmpp/Client/XmppClient[@name="SendMessage-1"]/*'/>
        public void SendMessage(Jid to, string body, string? subject = null,
            string? thread = null, MessageType type = MessageType.Normal,
            CultureInfo? language = null)
        {
            AssertValid();
            to.ThrowIfNull(nameof(to));
            body.ThrowIfNullOrEmpty("body");
            Im.SendMessage(to, body, subject, thread, type, language);
        }

        /// <summary>
        /// Sends a chat message with the specified content to the specified JID.
        /// </summary>
        /// <param name="to">The JID of the intended recipient.</param>
        /// <param name="bodies">A dictionary of message bodies. The dictionary
        /// keys denote the languages of the message bodies and must be valid
        /// ISO 2 letter language codes.</param>
        /// <param name="subjects">A dictionary of message subjects. The dictionary
        /// keys denote the languages of the message subjects and must be valid
        /// ISO 2 letter language codes.</param>
        /// <param name="thread">The conversation thread the message belongs to.</param>
        /// <param name="type">The type of the message. Can be one of the values from
        /// the MessagType enumeration.</param>
        /// <param name="language">The language of the XML character data of
        /// the stanza.</param>
        /// <exception cref="ArgumentNullException">The to parameter or the bodies
        /// parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppClient instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        /// <remarks>
        /// An XMPP chat-message may contain multiple subjects and bodies in different
        /// languages. Use this method in order to send a message that contains copies of the
        /// message content in several distinct languages.
        /// </remarks>
        /// <include file='Examples.xml' path='S22/Xmpp/Client/XmppClient[@name="SendMessage-2"]/*'/>
        public void SendMessage(Jid to, IDictionary<string, string> bodies,
            IDictionary<string, string>? subjects = null, string? thread = null,
            MessageType type = MessageType.Normal, CultureInfo? language = null)
        {
            AssertValid();
            to.ThrowIfNull(nameof(to));
            bodies.ThrowIfNull(nameof(bodies));
            Im.SendMessage(to, bodies, subjects, thread, type, language);
        }

        /// <summary>
        /// Sends the specified chat message.
        /// </summary>
        /// <param name="message">The chat message to send.</param>
        /// <exception cref="ArgumentNullException">The message parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppClient instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        public void SendMessage(Message message)
        {
            AssertValid();
            message.ThrowIfNull(nameof(message));
            Im.SendMessage(message);
        }

        /// <summary>
        /// Sets the availability status.
        /// </summary>
        /// <param name="availability">The availability state. Can be one of the
        /// values from the Availability enumeration, however not
        /// Availability.Offline.</param>
        /// <param name="message">An optional message providing a detailed
        /// description of the availability state.</param>
        /// <param name="priority">Provides a hint for stanza routing.</param>
        /// <param name="language">The language of the description of the
        /// availability state.</param>
        /// <exception cref="ArgumentException">The availability parameter has a
        /// value of Availability.Offline.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppClient instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        public void SetStatus(Availability availability, string? message = null,
            sbyte priority = 0, CultureInfo? language = null)
        {
            AssertValid();
            Im.SetStatus(availability, message, 0, language);
        }

        /// <summary>
        /// Sets the availability status.
        /// </summary>
        /// <param name="availability">The availability state. Can be one of the
        /// values from the Availability enumeration, however not
        /// Availability.Offline.</param>
        /// <param name="messages">A dictionary of messages providing detailed
        /// descriptions of the availability state. The dictionary keys denote
        /// the languages of the messages and must be valid ISO 2 letter language
        /// codes.</param>
        /// <param name="priority">Provides a hint for stanza routing.</param>
        /// <exception cref="ArgumentException">The availability parameter has a
        /// value of Availability.Offline.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppClient instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        public void SetStatus(Availability availability,
            Dictionary<string, string> messages, sbyte priority = 0)
        {
            AssertValid();
            Im.SetStatus(availability, messages, priority);
        }

        /// <summary>
        /// Sets the availability status.
        /// </summary>
        /// <param name="status">An instance of the Status class.</param>
        /// <exception cref="ArgumentNullException">The status parameter is null.</exception>
        /// <exception cref="ArgumentException">The Availability property of the status
        /// parameter has a value of Availability.Offline.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppClient instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        public void SetStatus(Status status)
        {
            AssertValid();
            status.ThrowIfNull(nameof(status));
            Im.SetStatus(status);
        }

        /// <summary>
        /// Retrieves the user's roster (contact list).
        /// </summary>
        /// <returns>The user's roster.</returns>
        /// <remarks>In XMPP jargon, the user's contact list is called a
        /// 'roster'.</remarks>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppClient instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        /// <include file='Examples.xml' path='S22/Xmpp/Client/XmppClient[@name="GetRoster"]/*'/>
        public Roster GetRoster()
        {
            AssertValid();
            return Im.GetRoster();
        }

        /// <summary>
        /// Adds the contact with the specified JID to the user's roster.
        /// </summary>
        /// <param name="jid">The JID of the contact to add to the user's roster.</param>
        /// <param name="name">The nickname with which to associate the contact.</param>
        /// <param name="groups">An array of groups or categories the new contact
        /// will be added to.</param>
        /// <remarks>This method creates a new item on the user's roster and requests
        /// a subscription from the contact with the specified JID.</remarks>
        /// <exception cref="ArgumentNullException">The jid parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppClient instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public void AddContact(Jid jid, string? name = null, params string[] groups)
        {
            AssertValid();
            jid.ThrowIfNull(nameof(jid));
            // Create a roster item for the new contact.
            Im.AddToRoster(new RosterItem(jid, name, groups));
            // Request a subscription from the contact.
            Im.RequestSubscription(jid);
        }

        /// <summary>
        /// Removes the item with the specified JID from the user's roster.
        /// </summary>
        /// <param name="jid">The JID of the roster item to remove.</param>
        /// <exception cref="ArgumentNullException">The jid parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppClient instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public void RemoveContact(Jid jid)
        {
            AssertValid();
            jid.ThrowIfNull(nameof(jid));
            // This removes the contact from the user's roster AND also cancels any
            // subscriptions.
            Im.RemoveFromRoster(jid);
        }

        /// <summary>
        /// Removes the specified item from the user's roster.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <exception cref="ArgumentNullException">The item parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppClient instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public void RemoveContact(RosterItem item)
        {
            AssertValid();
            item.ThrowIfNull(nameof(item));
            Im.RemoveFromRoster(item);
        }

#if WINDOWSPLATFORM
        /// <summary>
        /// Publishes the image located at the specified path as the user's avatar.
        /// </summary>
        /// <param name="filePath">The path to the image to publish as the user's
        /// avatar.</param>
        /// <exception cref="ArgumentNullException">The filePath parameter is
        /// null.</exception>
        /// <exception cref="ArgumentException">filePath is a zero-length string,
        /// contains only white space, or contains one or more invalid
        /// characters.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name,
        /// or both exceed the system-defined maximum length. For example, on
        /// Windows-based platforms, paths must be less than 248 characters, and
        /// file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is
        /// invalid, (for example, it is on an unmapped drive).</exception>
        /// <exception cref="UnauthorizedAccessException">The path specified is
        /// a directory, or the caller does not have the required
        /// permission.</exception>
        /// <exception cref="FileNotFoundException">The file specified in
        /// filePath was not found.</exception>
        /// <exception cref="NotSupportedException">filePath is in an invalid
        /// format, or the server does not support the 'Personal Eventing
        /// Protocol' extension.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppClient instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        /// <remarks>
        /// The following file types are supported:
        ///  BMP, GIF, JPEG, PNG and TIFF.
        /// </remarks>
        public void SetAvatar(string filePath) {
            AssertValid();
            filePath.ThrowIfNull(nameof(filePath));
            userAvatar.Publish(filePath);
        }
#endif

        /// <summary>
        /// Publishes the image located at the specified path as the user's avatar using vcard based Avatars
        /// </summary>
        /// <param name="filePath">The path to the image to publish as the user's avatar.</param>
        public void SetvCardAvatar(string filePath)
        {
            AssertValid();
            filePath.ThrowIfNull(nameof(filePath));

            try
            {
                using Stream s = File.OpenRead(filePath);
                vcardAvatars.SetAvatar(s);
            }
            catch (IOException copyError)
            {
                System.Diagnostics.Debug.WriteLine(copyError.Message);
                //Fix??? Should throw a network exception
            }
        }

        /// <summary>
        /// Get the vcard based Avatar of user with Jid
        /// </summary>
        /// <param name="jid">The string jid of the user</param>
        /// <param name="filepath">The filepath where the avatar will be stored</param>
        /// <param name="callback">The action that will be executed after the file has been downloaded</param>
        public void GetvCardAvatar(string jid, string filepath, Action<string, Jid> callback)
        {
            AssertValid();
            vcardAvatars.RequestAvatar(new Jid(jid), filepath, callback);
        }

        /// <summary>
        /// Requests a Custom Iq from the XMPP entinty Jid
        /// </summary>
        /// <param name="jid">The XMPP entity to request the custom IQ</param>
        /// <param name="str">The payload string to provide to the Request</param>
        /// <param name="callback">The callback method to call after the Request Result has being received. Included the serialised dat
        /// of the answer to the request</param>
        public void RequestCustomIq(Jid jid, string str, Action? callback = null)
        {
            AssertValid();
            if (callback == null) cusiqextension.RequestCustomIq(jid, str);
            else
                cusiqextension.RequestCustomIqAsync(jid, str, callback);
        }

        /// <summary>
        /// Sets the user's mood to the specified mood value.
        /// </summary>
        /// <param name="mood">A value from the Mood enumeration to set the user's
        /// mood to.</param>
        /// <param name="description">A natural-language description of, or reason
        /// for, the mood.</param>
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppClient instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        public void SetMood(Mood mood, string? description = null)
        {
            AssertValid();
            userMood?.SetMood(mood, description);
        }

        /// <summary>
        /// Sets the user's activity to the specified activity value(s).
        /// </summary>
        /// <param name="activity">A value from the GeneralActivity enumeration to
        /// set the user's general activity to.</param>
        /// <param name="specific">A value from the SpecificActivity enumeration
        /// best describing the user's activity in more detail.</param>
        /// <param name="description">A natural-language description of, or reason
        /// for, the activity.</param>
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppClient instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        /// <include file='Examples.xml' path='S22/Xmpp/Client/XmppClient[@name="SetActivity"]/*'/>
        public void SetActivity(GeneralActivity activity, SpecificActivity specific =
            SpecificActivity.Other, string? description = null)
        {
            AssertValid();
            userActivity.SetActivity(activity, specific, description);
        }

        /// <summary>
        /// Publishes the specified music information to contacts on the user's
        /// roster.
        /// </summary>
        /// <param name="title">The title of the song or piece.</param>
        /// <param name="artist">The artist or performer of the song or piece.</param>
        /// <param name="track">A unique identifier for the tune; e.g., the track number
        /// within a collection or the specific URI for the object (e.g., a
        /// stream or audio file).</param>
        /// <param name="length">The duration of the song or piece in seconds.</param>
        /// <param name="rating">The user's rating of the song or piece, from 1
        /// (lowest) to 10 (highest).</param>
        /// <param name="source">The collection (e.g., album) or other source
        /// (e.g., a band website that hosts streams or audio files).</param>
        /// <param name="uri">A URI or URL pointing to information about the song,
        /// collection, or artist</param>
        /// <exception cref="NotSupportedException">The server does not support the
        /// 'Personal Eventing Protocol' extension.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppClient instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        /// <remarks>Publishing no information (i.e. calling Publish without any parameters
        /// is considered a "stop command" to disable publishing).</remarks>
        public void SetTune(string? title = null, string? artist = null, string? track = null,
            int length = 0, int rating = 0, string? source = null, string? uri = null)
        {
            AssertValid();
            userTune.Publish(title, artist, track, length, rating, source, uri);
        }

        /// <summary>
        /// Publishes the specified music information to contacts on the user's
        /// roster.
        /// </summary>
        /// <param name="tune">The tune information to publish.</param>
        /// <exception cref="ArgumentNullException">The tune parameter is
        /// null.</exception>
        /// <exception cref="NotSupportedException">The server does not support the
        /// 'Personal Eventing Protocol' extension.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppClient instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        /// <include file='Examples.xml' path='S22/Xmpp/Client/XmppClient[@name="SetTune"]/*'/>
        public void SetTune(TuneInformation tune)
        {
            AssertValid();
            userTune.Publish(tune);
        }

        /// <summary>
        /// A callback method to invoke when a request for a file-transfer is received
        /// from another XMPP user.
        /// </summary>
        public FileTransferRequest? FileTransferRequest
        {
            get => siFileTransfer.TransferRequest;
            set => siFileTransfer.TransferRequest = value;
        }

        /// <summary>
        /// A callback method to invoke when a Custom Iq Request is received
        /// from another XMPP user.
        /// </summary>
        public CustomIqRequestDelegate? CustomIqDelegate
        {
            get => Im.CustomIqDelegate;
            set => Im.CustomIqDelegate = value;
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
        /// <returns>Sid of the file transfer</returns>
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
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppClient instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        public string InitiateFileTransfer(Jid to, string path,
            string? description = null, Action<bool, FileTransfer>? cb = null)
        {
            AssertValid();
            return siFileTransfer.InitiateFileTransfer(to, path, description, cb);
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
        /// <returns>The Sid of the file transfer</returns>
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
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppClient instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        public string InitiateFileTransfer(Jid to, Stream stream, string name, long size,
            string? description = null, Action<bool, FileTransfer>? cb = null)
        {
            AssertValid();
            return siFileTransfer.InitiateFileTransfer(to, stream, name, size, description, cb);
        }

        /// <summary>
        /// Cancels the specified file-transfer.
        /// </summary>
        /// <param name="transfer">The file-transfer to cancel.</param>
        /// <exception cref="ArgumentNullException">The transfer parameter is
        /// null.</exception>
        /// <exception cref="ArgumentException">The specified transfer instance does
        /// not represent an active data-transfer operation.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppClient instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        public void CancelFileTransfer(FileTransfer transfer)
        {
            AssertValid();
            transfer.ThrowIfNull(nameof(transfer));
            siFileTransfer.CancelFileTransfer(transfer);
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
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppClient instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        public void CancelFileTransfer(string sid, Jid from, Jid to)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("XmppClient CancelFileTransfer, sid {0}, from {1}, to {2}", sid, from.ToString(), to.ToString());
#endif

            AssertValid();
            sid.ThrowIfNullOrEmpty("sid");
            from.ThrowIfNull(nameof(from));
            to.ThrowIfNull(nameof(to));

            siFileTransfer.CancelFileTransfer(sid, from, to);
        }

        /// <summary>
        /// Initiates in-band registration with the XMPP server in order to register
        /// a new XMPP account.
        /// </summary>
        /// <param name="callback">A callback method invoked to let the user
        /// enter any information required by the server in order to complete the
        /// registration.</param>
        /// <exception cref="ArgumentNullException">The callback parameter is
        /// null.</exception>
        /// <exception cref="NotSupportedException">The XMPP server with does not
        /// support the 'In-Band Registration' XMPP extension.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        /// <remarks>
        /// See the "Howto: Register an account" guide for a walkthrough on how to
        /// register an XMPP account through the in-band registration process.
        /// </remarks>
        public void Register(RegistrationCallback callback)
        {
            callback.ThrowIfNull(nameof(callback));
            inBandRegistration.Register(callback);
        }

        /// <summary>
        /// Retrieves the current time of the XMPP client with the specified JID.
        /// </summary>
        /// <param name="jid">The JID of the user to retrieve the current time
        /// for.</param>
        /// <returns>The current time of the XMPP client with the specified JID.</returns>
        /// <exception cref="ArgumentNullException">The jid parameter is null.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is
        /// not connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to or
        /// reading from the network.</exception>
        /// <exception cref="NotSupportedException">The XMPP client of the
        /// user with the specified JID does not support the retrieval of the
        /// current time.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object
        /// has been disposed.</exception>
        /// <exception cref="XmppErrorException">The server or the XMPP client of
        /// the user with the specified JID returned an XMPP error code. Use the
        /// Error property of the XmppErrorException to obtain the specific error
        /// condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public DateTime GetTime(Jid jid)
        {
            AssertValid();
            return time.GetTime(jid);
        }

        /// <summary>
        /// Retrieves the software version of the XMPP client with the specified JID.
        /// </summary>
        /// <param name="jid">The JID of the user to retrieve version information
        /// for.</param>
        /// <returns>An initialized instance of the VersionInformation class providing
        /// the name and version of the XMPP client used by the user with the specified
        /// JID.</returns>
        /// <exception cref="ArgumentNullException">The jid parameter is null.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host, or the XmppCleint instance has not authenticated
        /// with the XMPP server.</exception>
        /// <exception cref="IOException">There was a failure while writing to or
        /// reading from the network.</exception>
        /// <exception cref="NotSupportedException">The XMPP client of the
        /// user with the specified JID does not support the retrieval of version
        /// information.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object
        /// has been disposed.</exception>
        /// <exception cref="XmppErrorException">The server or the XMPP client of
        /// the user with the specified JID returned an XMPP error code. Use the
        /// Error property of the XmppErrorException to obtain the specific error
        /// condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public VersionInformation GetVersion(Jid jid)
        {
            AssertValid();
            return version.GetVersion(jid);
        }

        /// <summary>
        /// Returns an enumerable collection of XMPP features supported by the XMPP
        /// client with the specified JID.
        /// </summary>
        /// <param name="jid">The JID of the XMPP client to retrieve a collection of
        /// supported features for.</param>
        /// <returns>An enumerable collection of XMPP extensions supported by the
        /// XMPP client with the specified JID.</returns>
        /// <exception cref="ArgumentNullException">The jid parameter is null.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is
        /// not connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to or
        /// reading from the network.</exception>
        /// <exception cref="NotSupportedException">The XMPP client of the
        /// user with the specified JID does not support the retrieval of feature
        /// information.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object
        /// has been disposed.</exception>
        /// <exception cref="XmppErrorException">The server or the XMPP client of
        /// the user with the specified JID returned an XMPP error code. Use the
        /// Error property of the XmppErrorException to obtain the specific error
        /// condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        /// <include file='Examples.xml' path='S22/Xmpp/Client/XmppClient[@name="GetFeatures"]/*'/>
        public IEnumerable<Extension> GetFeatures(Jid jid)
        {
            AssertValid();
            return ecapa.GetExtensions(jid);
        }

        /// <summary>
        /// Queries the XMPP entity with the specified JID for identity information.
        /// </summary>
        /// <param name="jid">The JID of the XMPP entity to query.</param>
        /// <returns>An enumerable collection of identities of the XMPP entity
        /// with the specified JID.</returns>
        /// <exception cref="ArgumentNullException">The jid parameter
        /// is null.</exception>
        /// <exception cref="NotSupportedException">The query could not be
        /// performed or the response was invalid.</exception>
        public IEnumerable<Identity> GetIdentities(Jid jid)
        {
            AssertValid();
            return sdisco.GetIdentities(jid);
        }

        /// <summary>
        /// Queries the XMPP entity with the specified JID for item information.
        /// </summary>
        /// <param name="jid">The JID of the XMPP entity to query.</param>
        /// <returns>An enumerable collection of items of the XMPP entity with
        /// the specified JID.</returns>
        /// <exception cref="ArgumentNullException">The jid parameter is
        /// null.</exception>
        /// <exception cref="NotSupportedException">The query could not be
        /// performed or the response was invalid.</exception>
        public IEnumerable<XmppItem> GetItems(Jid jid)
        {
            AssertValid();
            return sdisco.GetItems(jid);
        }

        /// <summary>
        /// Buzzes the user with the specified JID in order to get his or her attention.
        /// </summary>
        /// <param name="jid">The JID of the user to buzz.</param>
        /// <param name="message">An optional message to send along with the buzz
        /// notification.</param>
        /// <exception cref="ArgumentNullException">The jid parameter is null.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is
        /// not connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to or
        /// reading from the network.</exception>
        /// <exception cref="NotSupportedException">The XMPP client of the
        /// user with the specified JID does not support buzzing.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object
        /// has been disposed.</exception>
        /// <exception cref="XmppErrorException">The server or the XMPP client of
        /// the user with the specified JID returned an XMPP error code. Use the
        /// Error property of the XmppErrorException to obtain the specific error
        /// condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public void Buzz(Jid jid, string? message = null)
        {
            AssertValid();
            attention.GetAttention(jid, message);
        }

        /// <summary>
        /// Pings the user with the specified JID.
        /// </summary>
        /// <param name="jid">The JID of the user to ping.</param>
        /// <returns>The time it took to ping the user with the specified
        /// JID.</returns>
        /// <exception cref="ArgumentNullException">The jid parameter is null.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is
        /// not connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to or
        /// reading from the network.</exception>
        /// <exception cref="NotSupportedException">The XMPP client of the
        /// user with the specified JID does not support the 'Ping' XMPP protocol
        /// extension.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object
        /// has been disposed.</exception>
        /// <exception cref="XmppErrorException">The server or the XMPP client of
        /// the user with the specified JID returned an XMPP error code. Use the
        /// Error property of the XmppErrorException to obtain the specific error
        /// condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public TimeSpan Ping(Jid jid)
        {
            AssertValid();
            return ping.PingEntity(jid);
        }

        /// <summary>
        /// Blocks all communication to and from the XMPP entity with the specified JID.
        /// </summary>
        /// <param name="jid">The JID of the XMPP entity to block.</param>
        /// <exception cref="ArgumentNullException">The jid parameter is
        /// null.</exception>
        /// <exception cref="NotSupportedException">The server does not support the
        /// 'Blocking Command' extension and does not support privacy-list management.
        /// </exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is
        /// not connected to a remote host.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object
        /// has been disposed.</exception>
        public void Block(Jid jid)
        {
            AssertValid();
            jid.ThrowIfNull(nameof(jid));
            // If our server supports the 'Blocking Command' extension, we can just
            // use that.
            if (block?.Supported == true)
            {
                block.Block(jid);
            }
            else
            {
                // Privacy list blocking. If our server doesn't support privacy lists, we're
                // out of luck.
                PrivacyList? privacyList = null;
                var name = Im.GetDefaultPrivacyList();
                if (name != null)
                    privacyList = Im.GetPrivacyList(name);
                // If no default list has been set, look for a 'blocklist' list.
                foreach (var list in Im.GetPrivacyLists())
                {
                    if (list.Name == "blocklist")
                        privacyList = list;
                }
                // If 'blocklist' doesn't exist, create it and set it as default.
                privacyList ??= new PrivacyList("blocklist");
                privacyList.Add(new JidPrivacyRule(jid, false, 0), true);
                // Save the privacy list and activate it.
                Im.EditPrivacyList(privacyList);
                Im.SetDefaultPrivacyList(privacyList.Name);
                Im.SetActivePrivacyList(privacyList.Name);
            }
        }

        /// <summary>
        /// Fetch message history from the server.
        /// <para/>
        /// The 'start' and 'end' attributes MAY be specified to indicate a date range.
        /// <para/>
        /// If the 'with' attribute is omitted then collections with any JID are returned.
        /// <para/>
        /// If only 'start' is specified then all collections on or after that date should be returned.
        /// <para/>
        /// If only 'end' is specified then all collections prior to that date should be returned.
        /// </summary>
        /// <param name="pageRequest">Paging options</param>
        /// <param name="start">Optional start date range to query</param>
        /// <param name="end">Optional enddate range to query</param>
        /// <param name="with">Optional JID to filter archive results by</param>
        public XmppPage<ArchivedChatId> GetArchivedChatIds(XmppPageRequest pageRequest, DateTimeOffset? start = null, DateTimeOffset? end = null, Jid? with = null)
        {
            return messageArchiving.GetArchivedChatIds(pageRequest, start, end, with);
        }

        /// <summary>
        /// Fetch a page of archived messages from a chat
        /// </summary>
        /// <param name="pageRequest">Paging options</param>
        /// <param name="with">The id of the entity that the chat was with</param>
        /// <param name="start">The start time of the chat</param>
        public ArchivedChatPage GetArchivedChat(XmppPageRequest pageRequest, Jid with, DateTimeOffset start)
        {
            return messageArchiving.GetArchivedChat(pageRequest, with, start);
        }

        /// <summary>
        /// Fetch a page of archived messages
        /// </summary>
        /// <param name="pageRequest">Paging options</param>
        /// <param name="with">Optional filter to only return messages if they match the supplied JID</param>
        /// <param name="start">Optional filter to only return messages whose timestamp is equal to or later than the given timestamp.</param>
        /// <param name="end">Optional filter to only return messages whose timestamp is equal to or earlier than the timestamp given in the 'end' field.</param>
        public Task<XmppPage<Message>> GetArchivedMessages(XmppPageRequest pageRequest, Jid? with = null, DateTimeOffset? start = null, DateTimeOffset? end = null)
        {
            return messageArchiveManagement.GetArchivedMessages(pageRequest, with, null, start, end);
        }

        /// <summary>
        /// Fetch a page of archived messages from a multi-user chat room
        /// </summary>
        /// <param name="pageRequest">Paging options</param>
        /// <param name="roomId">The JID of the room</param>
        /// <param name="start">Optional filter to only return messages whose timestamp is equal to or later than the given timestamp.</param>
        /// <param name="end">Optional filter to only return messages whose timestamp is equal to or earlier than the timestamp given in the 'end' field.</param>
        public Task<XmppPage<Message>> GetArchivedMucMessages(XmppPageRequest pageRequest, Jid roomId, DateTimeOffset? start = null, DateTimeOffset? end = null)
        {
            return messageArchiveManagement.GetArchivedMessages(pageRequest, roomId, roomId, start, end);
        }

        /// <summary>
        /// Fetch a page of archived messages from a chat
        /// </summary>
        /// <param name="pageRequest">Paging options</param>
        /// <param name="chatId">The id of the chat</param>
        public ArchivedChatPage GetArchivedChat(XmppPageRequest pageRequest, ArchivedChatId chatId)
        {
            return messageArchiving.GetArchivedChat(pageRequest, chatId);
        }

        /// <summary>
        /// Unblocks all communication to and from the XMPP entity with the specified
        /// JID.
        /// </summary>
        /// <param name="jid">The JID of the XMPP entity to unblock.</param>
        /// <exception cref="ArgumentNullException">The jid parameter is
        /// null.</exception>
        /// <exception cref="NotSupportedException">The server does not support the
        /// 'Blocking Command' extension and does not support privacy-list management.
        /// </exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is
        /// not connected to a remote host.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is
        /// not connected to a remote host.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object
        /// has been disposed.</exception>
        public void Unblock(Jid jid)
        {
            AssertValid();
            jid.ThrowIfNull(nameof(jid));
            // If our server supports the 'Blocking Command' extension, we can just
            // use that.
            if (block.Supported)
            {
                block.Unblock(jid);
            }
            else
            {
                // Privacy list blocking. If our server doesn't support privacy lists, we're
                // out of luck.
                PrivacyList? privacyList = null;
                var name = Im.GetDefaultPrivacyList();
                if (name != null)
                    privacyList = Im.GetPrivacyList(name);
                // If no default list has been set, look for a 'blocklist' list.
                foreach (var list in Im.GetPrivacyLists())
                {
                    if (list.Name == "blocklist")
                        privacyList = list;
                }
                // No blocklist found.
                if (privacyList == null)
                    return;
                ISet<JidPrivacyRule> set = new HashSet<JidPrivacyRule>();
                foreach (var jidRule in privacyList.OfType<JidPrivacyRule>())
                {
                    if (jidRule.Jid == jid && !jidRule.Allow)
                        set.Add(jidRule);
                }
                foreach (var rule in set)
                    privacyList.Remove(rule);
                // Save the privacy list and activate it.
                if (privacyList.Count == 0)
                {
                    Im.SetDefaultPrivacyList();
                    Im.RemovePrivacyList(privacyList.Name);
                }
                else
                {
                    Im.EditPrivacyList(privacyList);
                    Im.SetDefaultPrivacyList(privacyList.Name);
                }
            }
        }

        /// <summary>
        /// Returns an enumerable collection of blocked contacts.
        /// </summary>
        /// <returns>An enumerable collection of JIDs which are on the client's
        /// blocklist.</returns>
        /// <exception cref="NotSupportedException">The server does not support the
        /// 'Blocking Command' extension and does not support privacy-list management.
        /// </exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is
        /// not connected to a remote host.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object
        /// has been disposed.</exception>
        public IEnumerable<Jid> GetBlocklist()
        {
            AssertValid();
            if (block?.Supported == true)
                return block.GetBlocklist();
            PrivacyList? privacyList = null;
            var name = Im.GetDefaultPrivacyList();
            if (name != null)
                privacyList = Im.GetPrivacyList(name);
            foreach (var list in Im.GetPrivacyLists())
            {
                if (list.Name == "blocklist")
                    privacyList = list;
            }
            var items = new HashSet<Jid>();
            if (privacyList != null)
            {
                foreach (var rule in privacyList)
                {
                    if (rule is JidPrivacyRule privacy)
                        items.Add(privacy.Jid);
                }
            }
            return items;
        }

        /// <summary>
        /// Returns a list of active public chat room messages.
        /// </summary>
        /// <param name="chatService">JID of the chat service (depends on server)</param>
        /// <returns>List of Room JIDs</returns>
        public IEnumerable<RoomInfoBasic> DiscoverRooms(Jid chatService)
        {
            AssertValid();
            return groupChat.DiscoverRooms(chatService);
        }

        /// <summary>
        /// Returns a list of active public chat room messages.
        /// </summary>
        /// <param name="chatRoom">Room Identifier</param>
        /// <returns>Information about room</returns>
        public RoomInfoExtended GetRoomInfo(Jid chatRoom)
        {
            AssertValid();
            return groupChat.GetRoomInfo(chatRoom);
        }

        /// <summary>
        /// Joins or creates new room using the specified room.
        /// </summary>
        /// <param name="chatRoom">Chat room</param>
        /// <param name="nickname">Desired nickname</param>
        /// <param name="password">(Optional) Password</param>
        public void JoinRoom(Jid chatRoom, string nickname, string? password = null)
        {
            AssertValid();
            groupChat.JoinRoom(chatRoom, nickname, password);
        }

        /// <summary>
        /// Request form with options to create a group chat
        /// </summary>
        /// <param name="room">Chat room</param>
        public DataForm RequestRegistration(Jid room)
        {
            AssertValid();
            return groupChat.RequestRegistration(room);
        }

        /// <summary>
        /// Send registration form to create a chat room
        /// </summary>
        /// <param name="room">Chat room</param>
        /// <param name="form">Options of room</param>
        public bool SendRegistration(Jid room, DataForm form)
        {
            AssertValid();
            return groupChat.SendRegistration(room, form);
        }

        /// <summary>
        /// Request immediate room creation with default server options
        /// </summary>
        /// <param name="room">Chat room</param>
        public void RequestInstantRoom(Jid room)
        {
            AssertValid();
            groupChat.RequestInstantRoom(room);
        }

        /// <summary>
        /// Leaves the specified room.
        /// </summary>
        /// <param name="room">Chat room</param>
        /// <param name="nickname">Registered user nick in the room</param>
        public void LeaveRoom(Jid room, string nickname)
        {
            AssertValid();
            groupChat.LeaveRoom(room, nickname);
        }

        /// <summary>
        /// Destroy the specified room.
        /// </summary>
        /// <param name="room">Chat room</param>
        /// <param name="reason">(Optional) Reason to destroy room.</param>
        public bool DestroyRoom(Jid room, string? reason = null)
        {
            AssertValid();
            return groupChat.DestroyRoom(room, reason);
        }

        /// <summary>
        /// Ban User from a chat group.
        /// </summary>
        /// <param name="room">Chat room</param>
        /// <param name="user">User to be banned</param>
        /// <param name="reason">(Optional) Reason for the ban.</param>
        public bool BanUser(Jid room, Jid user, string? reason = null)
        {
            AssertValid();
            return groupChat.SetPrivilege(room, user, Affiliation.Outcast, reason);
        }

        /// <summary>
        /// Add a user as admin of a MUC Room
        /// </summary>
        /// <param name="room">Chat room</param>
        /// <param name="user">User with admin permission</param>
        /// <param name="nick">(Optional) Desired nickname</param>
        /// <param name="reason">(Optional) Reason</param>
        public bool AddAdminToRoom(Jid room, Jid user, string? nick = null, string? reason = null)
        {
            AssertValid();
            return groupChat.SetPrivilege(room, user, Affiliation.Admin, reason, nick);
        }

        /// <summary>
        /// Remove user from a MUC Room
        /// </summary>
        /// <param name="room">Chat room</param>
        /// <param name="user">User with admin permission</param>
        /// <param name="reason">(Optional) Reason</param>
        public bool RemoveUser(Jid room, Jid user, string? reason = null)
        {
            AssertValid();
            return groupChat.SetPrivilege(room, user, Affiliation.None, reason);
        }

        /// <summary>
        /// Add a user as Member from a MUC Room
        /// </summary>
        /// <param name="room">Chat room</param>
        /// <param name="user">User with admin permission</param>
        /// <param name="nick">(Optional) Desired nickname</param>
        /// <param name="reason">(Optional) Reason</param>
        public bool AddMemberToRoom(Jid room, Jid user, string? nick = null, string? reason = null)
        {
            AssertValid();
            groupChat.SetPrivilege(room, user, Affiliation.Member, reason, nick);
            return true;
        }

        /// <summary>
        /// Sends a request to get X previous messages.
        /// </summary>
        /// <param name="option">How long to look back</param>
        public void GetGroupChatLog(History option)
        {
            AssertValid();
            groupChat.GetMessageLog(option);
        }

        /// <summary>
        /// Requests a list of occupants within the specific room.
        /// </summary>
        public IEnumerable<Occupant> GetRoomAllOccupants(Jid chatRoom)
        {
            AssertValid();
            return groupChat.GetMembers(chatRoom);
        }

        /// <summary>
        /// Requests a list of non-members within the specified room.
        /// </summary>
        public IEnumerable<Occupant> GetRoomStrangers(Jid chatRoom)
        {
            AssertValid();
            return groupChat.GetMembers(chatRoom, Affiliation.None);
        }

        /// <summary>
        /// Requests a list of room members within the specified room.
        /// </summary>
        public IEnumerable<Occupant> GetRoomMembers(Jid chatRoom)
        {
            AssertValid();
            return groupChat.GetMembers(chatRoom, Affiliation.Member);
        }

        /// <summary>
        /// Requests a list of room admins within the specified room.
        /// </summary>
        public IEnumerable<Occupant> GetRoomAdmins(Jid chatRoom)
        {
            AssertValid();
            return groupChat.GetMembers(chatRoom, Affiliation.Admin);
        }

        /// <summary>
        /// Requests a list of room owners within the specified room.
        /// </summary>
        public IEnumerable<Occupant> GetRoomOwners(Jid chatRoom)
        {
            AssertValid();
            return groupChat.GetMembers(chatRoom, Affiliation.Owner);
        }

        /// <summary>
        /// Requests a list of people banned within the specified room.
        /// </summary>
        public IEnumerable<Occupant> GetRoomBanList(Jid chatRoom)
        {
            AssertValid();
            return groupChat.GetMembers(chatRoom, Affiliation.Outcast);
        }

        /// <summary>
        /// Requests a list of visitors within the specified room.
        /// </summary>
        public IEnumerable<Occupant> GetRoomVisitors(Jid chatRoom)
        {
            AssertValid();
            return groupChat.GetMembers(chatRoom, Role.Visitor);
        }

        /// <summary>
        /// Requests a list of occupants with a voice privileges within the specified room.
        /// </summary>
        public IEnumerable<Occupant> GetRoomVoiceList(Jid chatRoom)
        {
            AssertValid();
            return groupChat.GetMembers(chatRoom, Role.Participant);
        }

        /// <summary>
        /// Requests a list of moderators within the specified room.
        /// </summary>
        public IEnumerable<Occupant> GetRoomModerators(Jid chatRoom)
        {
            AssertValid();
            return groupChat.GetMembers(chatRoom, Role.Moderator);
        }

        /// <summary>
        /// Allows moderators to kick an occupant from the room.
        /// </summary>
        /// <param name="chatRoom">chat room</param>
        /// <param name="nickname">user to kick</param>
        /// <param name="reason">reason for kick</param>
        public void KickGroupOccupant(Jid chatRoom, string nickname, string? reason = null)
        {
            groupChat.KickOccupant(chatRoom, nickname, reason);
        }

        /// <summary>
        /// Allows a user to modify the configuration of a specified room.
        /// Only "Room Owners" may edit room config.
        /// </summary>
        /// <param name="room">JID of the room.</param>
        /// <param name="callback">Room Configuration callback.</param>
        public void ModifyRoomConfig(Jid room, RegistrationCallback callback)
        {
            groupChat.ModifyRoomConfig(room, callback);
        }

        /// <summary>
        /// Asks the chat service to invite the specified user to the chat room you specify.
        /// </summary>
        /// <param name="to">user you intend to invite to chat room.</param>
        /// <param name="chatRoom">Jid of the chat room.</param>
        /// <param name="message">message you want to send to the user.</param>
        /// <param name="password">Password if any.</param>
        public void SendInvite(Jid to, Jid chatRoom, string message, string? password = null)
        {
            groupChat.SendInvite(to, chatRoom, message, password);
        }

        /// <summary>
        /// Responds to a group chat invitation with a decline message.
        /// </summary>
        /// <param name="invite">Original group chat invitation.</param>
        /// <param name="reason">Reason for declining.</param>
        public void DeclineInvite(Invite invite, string reason)
        {
            groupChat.DeclineInvite(invite, reason);
        }

        /// <summary>
        /// Allows visitors to request membership to a room.
        /// </summary>
        public void RequestVoice(Jid chatRoom)
        {
            groupChat.RequestPrivilige(chatRoom, Role.Participant);
        }

        /// <summary>
        /// Submit a serach forms
        /// </summary>
        /// <param name="form">DataForm with search criterias.</param>
        /// <returns>Search result based on DataForm request</returns>
        public DataForm Search(DataForm form)
        {
            AssertValid();
            return this.search.Search(form);
        }

        /// <summary>
        /// Request the Search Form
        /// </summary>
        /// <returns>DataForm for avaible fields search.</returns>
        public DataForm RequestSearchForm()
        {
            AssertValid();
            return this.search.RequestSearchForm();
        }

        /// <summary>
        /// Request the Search Form
        /// </summary>
        /// <returns>DataForm for avaible fields search</returns>
        public void RequestSlot(string fileName, long size, string contentType, Action<Slot> upload, Action<string> error)
        {
            AssertValid();
            httpUpload.RequestSlot(fileName, size, contentType, upload, error);
        }

        /// <summary>
        /// Closes the connection with the XMPP server. This automatically disposes
        /// of the object.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        public void Close()
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);
            Dispose();
        }

        /// <summary>
        /// Releases all resources used by the current instance of the XmppClient class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the current instance of the XmppClient
        /// class, optionally disposing of managed resource.
        /// </summary>
        /// <param name="disposing">true to dispose of managed resources, otherwise
        /// false.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                // Indicate that the instance has been disposed.
                disposed = true;
                // Get rid of managed resources.
                if (disposing)
                {
                    Im?.Close();
                    Im = null!;
                }
                // Get rid of unmanaged resources.
            }
        }

        /// <summary>
        /// Asserts the instance has not been disposed of and is connected to the
        /// XMPP server.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppClient instance is not
        /// connected to a remote host.</exception>
        private void AssertValid()
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (!Connected)
                throw new InvalidOperationException("Not connected to XMPP server.");
            if (!Authenticated)
                throw new InvalidOperationException("Not authenticated with XMPP server.");
        }

        /// <summary>
        /// Initializes the various XMPP extension modules.
        /// </summary>

        public XmppClient(XmppIm xmppIm)
        {
            Im = xmppIm;
            version = Im.LoadExtension<SoftwareVersion>();
            sdisco = Im.LoadExtension<ServiceDiscovery>();
            ecapa = Im.LoadExtension<EntityCapabilities>();
            ping = Im.LoadExtension<Ping>();
            attention = Im.LoadExtension<Attention>();
            time = Im.LoadExtension<EntityTime>();
            block = Im.LoadExtension<BlockingCommand>();
            pep = Im.LoadExtension<Pep>();
            userTune = Im.LoadExtension<UserTune>();
#if WINDOWSPLATFORM
            userAvatar = Im.LoadExtension<UserAvatar>();
#endif
            userMood = Im.LoadExtension<UserMood>();
            dataForms = Im.LoadExtension<DataForms>();
            featureNegotiation = Im.LoadExtension<FeatureNegotiation>();
            streamInitiation = Im.LoadExtension<StreamInitiation>();
            siFileTransfer = Im.LoadExtension<SIFileTransfer>();
            inBandBytestreams = Im.LoadExtension<InBandBytestreams>();
            userActivity = Im.LoadExtension<UserActivity>();
            socks5Bytestreams = Im.LoadExtension<Socks5Bytestreams>();
            FileTransferSettings = new FileTransferSettings(socks5Bytestreams, siFileTransfer);
            serverIpCheck = Im.LoadExtension<ServerIpCheck>();
            messageCarbons = Im.LoadExtension<MessageCarbons>();
            inBandRegistration = Im.LoadExtension<InBandRegistration>();
            chatStateNotifications = Im.LoadExtension<ChatStateNotifications>();
            bitsOfBinary = Im.LoadExtension<BitsOfBinary>();
            vcardAvatars = Im.LoadExtension<VCardAvatars>();
            vcard = Im.LoadExtension<VCards>();
            cusiqextension = Im.LoadExtension<CustomIqExtension>();
            groupChat = Im.LoadExtension<MultiUserChat>();
            search = Im.LoadExtension<JabberSearch>();
            messageArchiving = Im.LoadExtension<MessageArchiving>();
            messageArchiveManagement = Im.LoadExtension<MessageArchiveManagement>();
            httpUpload = Im.LoadExtension<HTTPFileUpload>();
        }
    }
}