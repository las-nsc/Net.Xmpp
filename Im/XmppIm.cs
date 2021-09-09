using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Xml;

using Net.Xmpp.Core;
using Net.Xmpp.Extensions;

namespace Net.Xmpp.Im
{
    /// <summary>
    /// Implements the basic instant messaging (IM) and presence functionality.
    /// </summary>
    /// <remarks>For implementation details, refer to RFC 3921.</remarks>
    public class XmppIm : IDisposable
    {
        /// <summary>
        /// Provides access to the core facilities of XMPP.
        /// </summary>
        private XmppCore core;

        /// <summary>
        /// True if the instance has been disposed of.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// The loaded extensions.
        /// </summary>
        private readonly Dictionary<Type, XmppExtension> extensions = new();

        /// <summary>
        /// The hostname of the XMPP server to connect to.
        /// </summary>
        /// <exception cref="ArgumentNullException">The Hostname property is being
        /// set and the value is null.</exception>
        /// <exception cref="ArgumentException">The Hostname property is being set
        /// and the value is the empty string.</exception>
        public string Hostname
        {
            get => core.Hostname;
            set => core.Hostname = value;
        }

        /// <summary>
        /// The port number of the XMPP service of the server.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The Port property is being
        /// set and the value is not between 0 and 65536.</exception>
        public int Port
        {
            get => core.Port;
            set => core.Port = value;
        }

        /// <summary>
        /// The username with which to authenticate. In XMPP jargon this is known
        /// as the 'node' part of the JID.
        /// </summary>
        /// <exception cref="ArgumentNullException">The Username property is being
        /// set and the value is null.</exception>
        /// <exception cref="ArgumentException">The Username property is being set
        /// and the value is the empty string.</exception>
        public string Username
        {
            get => core.Username;
            set => core.Username = value;
        }

        /// <summary>
        /// The password with which to authenticate.
        /// </summary>
        /// <exception cref="ArgumentNullException">The Password property is being
        /// set and the value is null.</exception>
        public string Password
        {
            get => core.Password;
            set => core.Password = value;
        }

        /// <summary>
        /// If true the session will be TLS/SSL-encrypted if the server supports it.
        /// </summary>
        public bool Tls
        {
            get => core.Tls;
            set => core.Tls = value;
        }

        /// <summary>
        /// A delegate used for verifying the remote Secure Sockets Layer (SSL)
        /// certificate which is used for authentication.
        /// </summary>
        public RemoteCertificateValidationCallback? Validate
        {
            get => core.Validate;
            set => core.Validate = value;
        }

        /// <summary>
        /// Determines whether the session with the server is TLS/SSL encrypted.
        /// </summary>
        public bool IsEncrypted => core.IsEncrypted;

        /// <summary>
        /// The address of the Xmpp entity.
        /// </summary>
        public Jid Jid => core.Jid;

        /// <summary>
        /// The address of the Xmpp entity.
        /// </summary>
        public int DefaultTimeOut
        {
            get => core.MillisecondsDefaultTimeout;
            set => core.MillisecondsDefaultTimeout = value;
        }

        /// <summary>
        /// Print XML stanzas for debugging purposes
        /// </summary>
        public bool DebugStanzas
        {
            get => core.DebugStanzas;
            set => core.DebugStanzas = value;
        }

        /// <summary>
        /// Determines whether the instance is connected to the XMPP server.
        /// </summary>
        public bool Connected => core.Connected;

        /// <summary>
        /// Determines whether the instance has been authenticated.
        /// </summary>
        public bool Authenticated => core.Authenticated;

        /// <summary>
        /// A callback method to invoke when a request for a subscription is received
        /// from another XMPP user.
        /// </summary>
        public SubscriptionRequest? SubscriptionRequest { get; set; }

        /// <summary>
        /// A callback method to invoke when a Custom Iq Request is received
        /// from another XMPP user.
        /// </summary>
        public CustomIqRequestDelegate? CustomIqDelegate { get; set; }

        /// <summary>
        /// The event that is raised when a status notification from a contact has been
        /// received.
        /// </summary>
        public event EventHandler<StatusEventArgs>? Status;

        /// <summary>
        /// The event that is raised when a chat message is received.
        /// </summary>
        public event EventHandler<MessageEventArgs>? Message;

        /// <summary>
        /// The event that is raised when a connection state changed.
        /// </summary>
        public event EventHandler<ConnectEventArgs>? OnConnect;

        /// <summary>
        /// The event that is raised when an error message is received.
        /// </summary>
        public event EventHandler<MessageEventArgs>? ErrorMessage;

        /// <summary>
        /// The event that is raised when a subscription request made by the JID
        /// associated with this instance has been approved.
        /// </summary>
        public event EventHandler<SubscriptionApprovedEventArgs>? SubscriptionApproved;

        /// <summary>
        /// The event that is raised when a subscription request made by the JID
        /// associated with this instance has been refused.
        /// </summary>
        public event EventHandler<SubscriptionRefusedEventArgs>? SubscriptionRefused;

        /// <summary>
        /// The event that is raised when a user or resource has unsubscribed from
        /// receiving presence notifications of the JID associated with this instance.
        /// </summary>
        public event EventHandler<UnsubscribedEventArgs>? Unsubscribed;

        /// <summary>
        /// The event that is raised when the roster of the user has been updated,
        /// i.e. a contact has been added, removed or updated.
        /// </summary>
        public event EventHandler<RosterUpdatedEventArgs>? RosterUpdated;

        /// <summary>
        /// The event that is raised when an unrecoverable error condition occurs.
        /// </summary>
        public event EventHandler<ErrorEventArgs>? Error;

        /// <summary>
        /// Initializes a new instance of the XmppIm.
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
        /// <param name="serverAdress">Adress if hostname is diferrent from resolution name</param>
        /// <exception cref="ArgumentNullException">The hostname parameter or the
        /// username parameter or the password parameter is null.</exception>
        /// <exception cref="ArgumentException">The hostname parameter or the username
        /// parameter is the empty string.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The value of the port parameter
        /// is not a valid port number.</exception>
        public XmppIm(string hostname, string username, string password,
            int port = 5222, bool tls = true, RemoteCertificateValidationCallback? validate = null,
            string serverAdress = "", string? resource = null)
        {
            core = new XmppCore(hostname, username, password, port, tls, validate, serverAdress, resource);
            try
            {
                // If no username has been providd, don't establish a session.
                if (Username == null)
                    return;
                // Establish a session (Refer to RFC 3921, Section 3. Session Establishment).
                EstablishSession();
                // Send initial presence.
                SendPresence(new());
            }
            catch (SocketException e)
            {
                throw new IOException("Could not connect to the server", e);
            }
        }

        /// <summary>
        /// Authenticates with the XMPP server using the specified username and
        /// password.
        /// </summary>
        /// <param name="username">The username to authenticate with.</param>
        /// <param name="password">The password to authenticate with.</param>
        /// <exception cref="ArgumentNullException">The username parameter or the
        /// password parameter is null.</exception>
        /// <exception cref="AuthenticationException">An authentication error occured while
        /// trying to establish a secure connection, or the provided credentials were
        /// rejected by the server, or the server requires TLS/SSL and the Tls property has
        /// been set to false.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network. If the InnerException is of type SocketExcption, use the
        /// ErrorCode property to obtain the specific socket error code.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="XmppException">An XMPP error occurred while negotiating the
        /// XML stream with the server, or resource binding failed, or the initialization
        /// of an XMPP extension failed.</exception>
        public void Autenticate(string username, string password)
        {
            username.ThrowIfNull(nameof(username));
            password.ThrowIfNull(nameof(password));
            core.Authenticate(username, password);
            // Establish a session (Refer to RFC 3921, Section 3. Session Establishment).
            EstablishSession();
            // Retrieve user's roster as recommended (Refer to RFC 3921, Section 7.3).
            _ = GetRoster();
            // Send initial presence.
            SendPresence(new());
        }

        /// <summary>
        /// Authenticates with the XMPP server using the specified username and
        /// password, but not GetRosters and Presence to Server, this must be done explicit by the client.
        /// </summary>
        /// <param name="username">The username to authenticate with.</param>
        /// <param name="password">The password to authenticate with.</param>
        /// <exception cref="ArgumentNullException">The username parameter or the
        /// password parameter is null.</exception>
        /// <exception cref="AuthenticationException">An authentication error occured while
        /// trying to establish a secure connection, or the provided credentials were
        /// rejected by the server, or the server requires TLS/SSL and the Tls property has
        /// been set to false.</exception>
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
            username.ThrowIfNull(nameof(username));
            password.ThrowIfNull(nameof(password));
            core.Authenticate(username, password);
            // Establish a session (Refer to RFC 3921, Section 3. Session Establishment).
            EstablishSession();
        }

        /// <summary>
        /// Send my Presence Signal to Server.
        /// </summary>
        public void SetPresence()
        {
            // Send initial presence.
            SendPresence(new());
        }

        /// <summary>
        /// Reconnect XMPP in case of connection lost
        /// </summary>
        public void Reconnect()
        {
            core.Reconnect();
            // Establish a session (Refer to RFC 3921, Section 3. Session Establishment).
            EstablishSession();
            // Retrieve user's roster as recommended (Refer to RFC 3921, Section 7.3).
            _ = GetRoster();
            // Send initial presence.
            SendPresence(new());
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
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        public void SendMessage(Jid to, string body, string? subject = null,
            string? thread = null, MessageType type = MessageType.Normal,
            CultureInfo? language = null)
        {
            AssertValid();
            to.ThrowIfNull(nameof(to));
            body.ThrowIfNullOrEmpty("body");
            Message m = new(to, body, subject, thread, type, language);
            SendMessage(m);
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
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        public void SendMessage(Jid to, IDictionary<string, string> bodies,
            IDictionary<string, string>? subjects = null, string? thread = null,
            MessageType type = MessageType.Normal, CultureInfo? language = null)
        {
            AssertValid();
            to.ThrowIfNull(nameof(to));
            bodies.ThrowIfNull(nameof(bodies));
            Message m = new(to, bodies, subjects, thread, type, language);
            SendMessage(m);
        }

        /// <summary>
        /// Sends the specified chat message.
        /// </summary>
        /// <param name="message">The chat message to send.</param>
        /// <exception cref="ArgumentNullException">The message parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        public void SendMessage(Message message)
        {
            AssertValid();
            message.ThrowIfNull(nameof(message));
            // "Stamp" the sender's JID onto the message.
            message.From = Jid;
            // Invoke IOutput<Message> Plugins.
            foreach (var filter in extensions.Values.OfType<IOutputFilter<Message>>())
            {
                filter.Output(message);
            }
            core.SendMessage(message);
        }

        /// <summary>
        /// Sends a request to subscribe to the presence of the contact with the
        /// specified JID.
        /// </summary>
        /// <param name="jid">The JID of the contact to request a subscription
        /// from.</param>
        /// <exception cref="ArgumentNullException">The jid parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        public void RequestSubscription(Jid jid)
        {
            AssertValid();
            jid.ThrowIfNull(nameof(jid));
            Presence p = new(jid, null, PresenceType.Subscribe);
            SendPresence(p);
        }

        /// <summary>
        /// Unsubscribes from the presence of the contact with the specified JID.
        /// </summary>
        /// <param name="jid">The JID of the contact to unsubsribe from.</param>
        /// <exception cref="ArgumentNullException">The jid parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        public void Unsubscribe(Jid jid)
        {
            AssertValid();
            jid.ThrowIfNull(nameof(jid));
            Presence p = new(jid, null, PresenceType.Unsubscribe);
            SendPresence(p);
        }

        /// <summary>
        /// Approves a subscription request received from the contact with
        /// the specified JID.
        /// </summary>
        /// <param name="jid">The JID of the contact wishing to subscribe.</param>
        /// <exception cref="ArgumentNullException">The jid parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        public void ApproveSubscriptionRequest(Jid jid)
        {
            AssertValid();
            jid.ThrowIfNull(nameof(jid));
            Presence p = new(jid, null, PresenceType.Subscribed);
            SendPresence(p);
        }

        /// <summary>
        /// Refuses a subscription request received from the contact with
        /// the specified JID.
        /// </summary>
        /// <param name="jid">The JID of the contact wishing to subscribe.</param>
        /// <exception cref="ArgumentNullException">The jid parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        public void RefuseSubscriptionRequest(Jid jid)
        {
            AssertValid();
            jid.ThrowIfNull(nameof(jid));
            Presence p = new(jid, null, PresenceType.Unsubscribed);
            SendPresence(p);
        }

        /// <summary>
        /// Revokes the previously-approved subscription of the contact with
        /// the specified JID.
        /// </summary>
        /// <param name="jid">The JID of the contact whose subscription to
        /// revoke.</param>
        /// <exception cref="ArgumentNullException">The jid parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        public void RevokeSubscription(Jid jid)
        {
            AssertValid();
            jid.ThrowIfNull(nameof(jid));
            Presence p = new(jid, null, PresenceType.Unsubscribed);
            SendPresence(p);
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
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        public void SetStatus(Availability availability, string? message = null,
            sbyte priority = 0, CultureInfo? language = null)
        {
            AssertValid();
            if (availability == Availability.Offline)
                throw new ArgumentException("Invalid availability state.");
            List<XmlElement> elems = new();
            if (availability != Availability.Online)
            {
                var states = new Dictionary<Availability, string>() {
                        { Availability.Away, "away" },
                        { Availability.Dnd, "dnd" },
                        { Availability.Xa, "xa" },
                        { Availability.Chat, "chat" }
                    };
                elems.Add(Xml.Element("show").Text(states[availability]));
            }
            if (priority != 0)
                elems.Add(Xml.Element("priority").Text(priority.ToString()));
            if (message != null)
                elems.Add(Xml.Element("status").Text(message));
            Presence p = new(type: PresenceType.Available, language: language, data: elems.ToArray());
            SendPresence(p);
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
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        public void SetStatus(Availability availability,
            Dictionary<string, string> messages, sbyte priority = 0)
        {
            AssertValid();
            if (availability == Availability.Offline)
                throw new InvalidOperationException("Invalid availability state.");
            List<XmlElement> elems = new();
            if (availability != Availability.Online)
            {
                var states = new Dictionary<Availability, string>() {
                        { Availability.Away, "away" },
                        { Availability.Dnd, "dnd" },
                        { Availability.Xa, "xa" },
                        { Availability.Chat, "chat" }
                    };
                elems.Add(Xml.Element("show").Text(states[availability]));
            }
            if (priority != 0)
                elems.Add(Xml.Element("priority").Text(priority.ToString()));
            if (messages != null)
            {
                foreach (KeyValuePair<string, string> pair in messages)
                    elems.Add(Xml.Element("status").Attr("xml:lang", pair.Key).Text(pair.Value));
            }
            Presence p = new(type: PresenceType.Available, data: elems.ToArray());
            SendPresence(p);
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
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        public void SetStatus(Status status)
        {
            AssertValid();
            status.ThrowIfNull(nameof(status));
            SetStatus(status.Availability, status.Messages, status.Priority);
        }

        /// <summary>
        /// Retrieves the user's roster.
        /// </summary>
        /// <returns>The user's roster.</returns>
        /// <remarks>In XMPP jargon, the user's contact list is called a
        /// 'roster'.</remarks>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public Roster GetRoster()
        {
            AssertValid();
            Iq iq = IqRequest(IqType.Get, null, Jid,
                Xml.Element("query", "jabber:iq:roster"));
            if (iq.Type == IqType.Error)
                throw Util.ExceptionFromError(iq, "The roster could not be retrieved.");
            var query = iq.Data["query"];
            return query == null || query.NamespaceURI != "jabber:iq:roster"
                ? throw new XmppException("Erroneous server response.")
                : ParseRoster(iq.Data);
        }

        /// <summary>
        /// Adds the specified item to the user's roster.
        /// </summary>
        /// <param name="item">The item to add to the user's roster.</param>
        /// <remarks>In XMPP jargon, the user's contact list is called a
        /// 'roster'.</remarks>
        /// <exception cref="ArgumentNullException">The item parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public void AddToRoster(RosterItem item)
        {
            AssertValid();
            item.ThrowIfNull(nameof(item));
            var xml = Xml.Element("item").Attr("jid", item.Jid.ToString());
            if (item.Name?.Length > 0)
                xml.Attr("name", item.Name);
            foreach (string group in item.Groups)
                xml.Child(Xml.Element("group").Text(group));
            var query = Xml.Element("query", "jabber:iq:roster").Child(xml);
            Iq iq = IqRequest(IqType.Set, null, Jid, query);
            if (iq.Type == IqType.Error)
                throw Util.ExceptionFromError(iq, "The item could not be added to the roster.");
        }

        /// <summary>
        /// Removes the item with the specified JID from the user's roster.
        /// </summary>
        /// <param name="jid">The JID of the roster item to remove.</param>
        /// <exception cref="ArgumentNullException">The jid parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public void RemoveFromRoster(Jid jid)
        {
            AssertValid();
            jid.ThrowIfNull(nameof(jid));
            var query = Xml.Element("query", "jabber:iq:roster").Child(
                Xml.Element("item").Attr("jid", jid.ToString())
                .Attr("subscription", "remove"));
            Iq iq = IqRequest(IqType.Set, null, Jid, query);
            if (iq.Type == IqType.Error)
                throw Util.ExceptionFromError(iq, "The item could not be removed from the roster.");
        }

        /// <summary>
        /// Removes the specified item from the user's roster.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <exception cref="ArgumentNullException">The item parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public void RemoveFromRoster(RosterItem item)
        {
            AssertValid();
            item.ThrowIfNull(nameof(item));
            RemoveFromRoster(item.Jid);
        }

        /// <summary>
        /// Returns an enumerable collection of privacy lists stored on the user's
        /// server.
        /// </summary>
        /// <returns>An enumerable collection of all privacy lists stored on the
        /// user's server.</returns>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public IEnumerable<PrivacyList> GetPrivacyLists()
        {
            AssertValid();
            Iq iq = IqRequest(IqType.Get, null, Jid,
                Xml.Element("query", "jabber:iq:privacy"));
            if (iq.Type == IqType.Error)
                Util.ExceptionFromError(iq, "The privacy lists could not be retrieved.");
            var query = iq.Data["query"];
            if (query == null || query.NamespaceURI != "jabber:iq:privacy")
                throw new XmppException("Erroneous server response: " + iq);
            ISet<PrivacyList> lists = new HashSet<PrivacyList>();
            foreach (XmlElement list in query.GetElementsByTagName("list"))
            {
                string name = list.GetAttribute("name");
                if (name?.Length > 0)
                    lists.Add(GetPrivacyList(name));
            }
            return lists;
        }

        /// <summary>
        /// Retrieves the privacy list with the specified name from the server.
        /// </summary>
        /// <param name="name">The name of the privacy list to retrieve.</param>
        /// <returns>The privacy list retrieved from the server.</returns>
        /// <exception cref="ArgumentNullException">The name parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public PrivacyList GetPrivacyList(string name)
        {
            AssertValid();
            name.ThrowIfNull(nameof(name));
            var query = Xml.Element("query", "jabber:iq:privacy").
                Child(Xml.Element("list").Attr("name", name));
            Iq iq = IqRequest(IqType.Get, null, Jid, query);
            if (iq.Type == IqType.Error)
                throw Util.ExceptionFromError(iq, "The privacy list could not be retrieved.");
            query = iq.Data["query"];
            if (query == null || query.NamespaceURI != "jabber:iq:privacy" ||
                query["list"] == null)
            {
                throw new XmppException("Erroneous server response: " + iq);
            }
            PrivacyList list = new(name);
            var listElement = query["list"];
            // Parse the items on the list.
            foreach (XmlElement item in listElement.GetElementsByTagName("item"))
            {
                try
                {
                    PrivacyRule rule = ParsePrivacyItem(item);
                    list.Add(rule);
                }
                catch (Exception e)
                {
                    throw new XmppException("Erroneous privacy rule.", e);
                }
            }
            return list;
        }

        /// <summary>
        /// Removes the privacy list with the specified name.
        /// </summary>
        /// <param name="name">The name of the privacy list to remove.</param>
        /// <exception cref="ArgumentNullException">The name parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public void RemovePrivacyList(string name)
        {
            AssertValid();
            name.ThrowIfNull(nameof(name));
            var query = Xml.Element("query", "jabber:iq:privacy").Child(
                Xml.Element("list").Attr("name", name));
            Iq iq = IqRequest(IqType.Set, null, Jid, query);
            if (iq.Type == IqType.Error)
                throw Util.ExceptionFromError(iq, "The privacy list could not be removed.");
        }

        /// <summary>
        /// Creates or updates the privacy list with the name of the specified list
        /// on the user's server.
        /// </summary>
        /// <param name="list">An instance of the PrivacyList class to create a new
        /// privacy list from. If a list with the name of the provided list already
        /// exists on the user's server, it is overwritten.</param>
        /// <exception cref="ArgumentNullException">The list parameter is null.</exception>
        /// <exception cref="ArgumentException">The privacy list must contain one or
        /// more privacy rules.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public void EditPrivacyList(PrivacyList list)
        {
            AssertValid();
            list.ThrowIfNull(nameof(list));
            if (list.Count == 0)
            {
                throw new ArgumentException("The list must contain one or more privacy " +
                    "rules.");
            }
            var listElement = Xml.Element("list").Attr("name", list.Name);
            // Build the XML.
            foreach (PrivacyRule rule in list)
            {
                var item = Xml.Element("item")
                    .Attr("action", rule.Allow ? "allow" : "deny")
                    .Attr("order", rule.Order.ToString());
                if (rule.Granularity.HasFlag(PrivacyGranularity.Message))
                    item.Child(Xml.Element("message"));
                if (rule.Granularity.HasFlag(PrivacyGranularity.Iq))
                    item.Child(Xml.Element("iq"));
                if (rule.Granularity.HasFlag(PrivacyGranularity.PresenceIn))
                    item.Child(Xml.Element("presence-in"));
                if (rule.Granularity.HasFlag(PrivacyGranularity.PresenceOut))
                    item.Child(Xml.Element("presence-out"));
                if (rule is JidPrivacyRule jidRule)
                {
                    item.Attr("type", "jid");
                    item.Attr("value", jidRule.Jid.ToString());
                }
                else if (rule is GroupPrivacyRule groupRule)
                {
                    item.Attr("type", "group");
                    item.Attr("value", groupRule.Group);
                }
                else if (rule is SubscriptionPrivacyRule subRule)
                {
                    item.Attr("type", "subscription");
                    item.Attr("value", subRule.SubscriptionState.ToString()
                        .ToLowerInvariant());
                }
                listElement.Child(item);
            }
            Iq iq = IqRequest(IqType.Set, null, Jid,
                Xml.Element("query", "jabber:iq:privacy").Child(listElement));
            if (iq.Type == IqType.Error)
                throw Util.ExceptionFromError(iq, "The privacy list could not be edited.");
        }

        /// <summary>
        /// Returns the name of the currently active privacy list.
        /// </summary>
        /// <returns>The name of the currently active privacy list or null if no
        /// list is active.</returns>
        /// <remarks>The 'active' privacy list applies only to this connected
        /// resource or session, but not to the user as a whole.</remarks>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public string? GetActivePrivacyList()
        {
            AssertValid();
            Iq iq = IqRequest(IqType.Get, null, Jid,
                Xml.Element("query", "jabber:iq:privacy"));
            if (iq.Type == IqType.Error)
                throw Util.ExceptionFromError(iq, "The privacy list could not be retrieved.");
            var query = iq.Data["query"];
            if (query == null || query.NamespaceURI != "jabber:iq:privacy")
                throw new XmppException("Erroneous server response: " + iq);
            var active = query["active"];
            if (active == null)
                return null;
            string name = active.GetAttribute("name");
            return name?.Length > 0 ? name : null;
        }

        /// <summary>
        /// Activates the privacy list with the specified name.
        /// </summary>
        /// <param name="name">The name of the privacy list to activate. If this
        /// is null, any currently active list is deactivated.</param>
        /// <remarks>The 'active' privacy list applies only to this connected
        /// resource or session, but not to the user as a whole. Only one privacy list
        /// can be active at any time.</remarks>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public void SetActivePrivacyList(string? name = null)
        {
            AssertValid();
            var query = Xml.Element("query", "jabber:iq:privacy").Child(
                Xml.Element("active"));
            if (name != null)
                query["active"].Attr("name", name);
            Iq iq = IqRequest(IqType.Set, null, Jid, query);
            if (iq.Type == IqType.Error)
                throw Util.ExceptionFromError(iq, "The privacy list could not be activated.");
        }

        /// <summary>
        /// Returns the name of the default privacy list.
        /// </summary>
        /// <returns>The name of the default privacy list or null if no
        /// list has been set as default list.</returns>
        /// <remarks>The 'default' privacy list applies to the user as a whole, and
        /// is processed if there is no active list set for the current session or
        /// resource.</remarks>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public string? GetDefaultPrivacyList()
        {
            AssertValid();
            Iq iq = IqRequest(IqType.Get, null, Jid,
                Xml.Element("query", "jabber:iq:privacy"));
            if (iq.Type == IqType.Error)
                throw Util.ExceptionFromError(iq, "The privacy list could not be retrieved.");
            var query = iq.Data["query"];
            if (query == null || query.NamespaceURI != "jabber:iq:privacy")
                throw new XmppException("Erroneous server response: " + iq);
            var active = query["default"];
            if (active == null)
                return null;
            string name = active.GetAttribute("name");
            return name?.Length > 0 ? name : null;
        }

        /// <summary>
        /// Makes the privacy list with the specified name the default privacy list.
        /// </summary>
        /// <param name="name">The name of the privacy list make the default privacy
        /// list. If this is null, the current default list is declined.</param>
        /// <remarks>The 'default' privacy list applies to the user as a whole, and
        /// is processed if there is no active list set for the current session or
        /// resource.</remarks>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host, or the XmppIm instance has not authenticated with
        /// the XMPP server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public void SetDefaultPrivacyList(string? name = null)
        {
            AssertValid();
            var query = Xml.Element("query", "jabber:iq:privacy").Child(
                Xml.Element("default"));
            if (name != null)
                query["default"].Attr("name", name);
            Iq iq = IqRequest(IqType.Set, null, Jid, query);
            if (iq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(iq, "The privacy list could not be made " +
                    "the default.");
            }
        }

        /// <summary>
        /// Closes the connection with the XMPP server. This automatically disposes
        /// of the object.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        public void Close()
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);
            Dispose();
        }

        /// <summary>
        /// Releases all resources used by the current instance of the XmppIm class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the current instance of the XmppIm
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
                    core?.Close();
                    core = null!;
                }
                // Get rid of unmanaged resources.
            }
        }

        /// <summary>
        /// Loads the specified XMPP extension.
        /// </summary>
        /// <typeparam name="T">The type of the extension to load.</typeparam>
        /// <returns>An instance of the loaded extension.</returns>
        internal T LoadExtension<T>() where T : XmppExtension => (T)LoadExtension(typeof(T));

        internal XmppExtension LoadExtension(Type type)
        {
            if (extensions.TryGetValue(type, out var existing))
                return existing;

            var argData = type.GetConstructors()[0].GetParameters();

            var args = new List<object>();

            foreach (var arg in argData)
            {
                if (arg.ParameterType == GetType())
                {
                    args.Add(this);
                }
                else
                {
                    if (!extensions.TryGetValue(arg.ParameterType, out existing) && LoadExtension(arg.ParameterType) is { } loaded)
                        existing = loaded;
                    args.Add(existing);
                }
            }

            // Create instance of extension.
            var ext = (XmppExtension)Activator.CreateInstance(type, args.ToArray());
            // Add instance to list of loaded extensions.
            extensions.Add(type, ext);
            return ext;
        }

        /// <summary>
        /// Unloads the specified extension.
        /// </summary>
        /// <typeparam name="T">The type of the extension to unload.</typeparam>
        /// <returns>true if the extension was unloaded; Otherwise false. This
        /// method also returns false if the extension is not found in the
        /// original list of extensions.</returns>
        internal bool UnloadExtension<T>() where T : XmppExtension
        {
            return extensions.ContainsKey(typeof(T)) && extensions.Remove(typeof(T));
        }

        /// <summary>
        /// Retrieves the instance of the extension that implements the specified
        /// XML namespace.
        /// </summary>
        /// <param name="namespace">The XML namespace to look for.</param>
        /// <returns>The instance of the extension that implements the specified
        /// namespace, or null if no such extension exists.</returns>
        /// <exception cref="ArgumentNullException">The namespace parameter is
        /// null.</exception>
        internal XmppExtension? GetExtension(string @namespace)
        {
            @namespace.ThrowIfNull(nameof(@namespace));
            return extensions.Values.FirstOrDefault(ext => ext.Namespaces.Contains(@namespace));
        }

        /// <summary>
        /// Returns an enumerable collection of loaded extensions.
        /// </summary>
        internal IEnumerable<XmppExtension> Extensions => extensions.Values;

        /// <summary>
        /// Sends the specified presence stanza to the server.
        /// </summary>
        /// <param name="presence">The presence stanza to send to the server.</param>
        /// <exception cref="ArgumentNullException">The presence parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        internal void SendPresence(Presence presence)
        {
            presence.ThrowIfNull(nameof(presence));
            // Invoke IOutput<Presence> Plugins.
            foreach (var filter in extensions.Values.OfType<IOutputFilter<Presence>>())
            {
                filter.Output(presence);
            }
            core.SendPresence(presence);
        }

        /// <summary>
        /// Performs an IQ set/get request and blocks until the response IQ comes in.
        /// </summary>
        /// <param name="type">The type of the request. This must be either
        /// IqType.Set or IqType.Get.</param>
        /// <param name="to">The JID of the intended recipient for the stanza.</param>
        /// <param name="from">The JID of the sender.</param>
        /// <param name="data">he content of the stanza.</param>
        /// <param name="language">The language of the XML character data of
        /// the stanza.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait
        /// for the arrival of the IQ response or -1 to wait indefinitely.</param>
        /// <returns>The IQ response sent by the server.</returns>
        /// <exception cref="ArgumentException">The type parameter is not IqType.Set
        /// or IqType.Get.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The value of millisecondsTimeout
        /// is a negative number other than -1, which represents an indefinite
        /// timeout.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        /// <exception cref="TimeoutException">A timeout was specified and it
        /// expired.</exception>
        internal Iq IqRequest(IqType type, Jid? to = null, Jid? from = null,
            XmlElement? data = null, CultureInfo? language = null,
            int millisecondsTimeout = -1)
        {
            Iq iq = new(type, null, to, from, data, language);
            // Invoke IOutput<Iq> Plugins.
            foreach (var filter in extensions.Values.OfType<IOutputFilter<Iq>>())
            {
                filter.Output(iq);
            }
            return core.IqRequest(iq, millisecondsTimeout);
        }

        /// <summary>
        /// Performs an IQ set/get request asynchronously and optionally invokes a
        /// callback method when the IQ response comes in.
        /// </summary>
        /// <param name="type">The type of the request. This must be either
        /// IqType.Set or IqType.Get.</param>
        /// <param name="to">The JID of the intended recipient for the stanza.</param>
        /// <param name="from">The JID of the sender.</param>
        /// <param name="data">he content of the stanza.</param>
        /// <param name="language">The language of the XML character data of
        /// the stanza.</param>
        /// <param name="callback">A callback method which is invoked once the
        /// IQ response from the server comes in.</param>
        /// <returns>The ID value of the pending IQ stanza request.</returns>
        /// <exception cref="ArgumentException">The type parameter is not IqType.Set
        /// or IqType.Get.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        internal string IqRequestCallback(IqType type, Jid? to = null, Jid? from = null,
            XmlElement? data = null, CultureInfo? language = null,
            Action<string, Iq>? callback = null)
        {
            Iq iq = new(type, null, to, from, data, language);
            // Invoke IOutput<Iq> Plugins.
            foreach (var filter in extensions.Values.OfType<IOutputFilter<Iq>>())
            {
                filter.Output(iq);
            }
            return core.IqRequest(iq, callback);
        }

        /// <summary>
        /// Sends an IQ response for the IQ request with the specified id.
        /// </summary>
        /// <param name="type">The type of the response. This must be either
        /// IqType.Result or IqType.Error.</param>
        /// <param name="id">The id of the IQ request.</param>
        /// <param name="to">The JID of the intended recipient for the stanza.</param>
        /// <param name="from">The JID of the sender.</param>
        /// <param name="data">he content of the stanza.</param>
        /// <param name="language">The language of the XML character data of
        /// the stanza.</param>
        /// <exception cref="ArgumentException">The type parameter is not IqType.Result
        /// or IqType.Error.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        internal void IqResponse(IqType type, string id, Jid? to = null, Jid? from = null,
            XmlElement? data = null, CultureInfo? language = null)
        {
            AssertValid(false);
            Iq iq = new(type, id, to, from, data, language);
            // Invoke IOutput<Iq> Plugins.
            foreach (var filter in extensions.Values.OfType<IOutputFilter<Iq>>())
            {
                filter.Output(iq);
            }
            core.IqResponse(iq);
        }

        /// <summary>
        /// Sends an IQ response of type 'error' in response to the specified
        /// stanza.
        /// </summary>
        /// <param name="iq">The original stanza to reply to.</param>
        /// <param name="type">The type of the error. Can be one of the values
        /// from the ErrorType enumeration.</param>
        /// <param name="condition">The XMPP error condition. Can be one of the
        /// values from the ErrorCondition enumeration.</param>
        /// <param name="text">The text message to include in the error.</param>
        /// <param name="data">Additional XML elements to include as part of
        /// the error element of the response.</param>
        /// <exception cref="ArgumentNullException">The iq parameter is
        /// null.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        internal void IqError(Iq iq, ErrorType type, ErrorCondition condition,
            string? text = null, params XmlElement[] data)
        {
            AssertValid(false);
            iq.ThrowIfNull(nameof(iq));
            Iq response = new(IqType.Error, iq.Id, iq.From, Jid, new XmppError(type, condition, text, data).Data);
            core.IqResponse(response);
        }

        /// <summary>
        /// Sends an IQ response of type 'result' in response to the specified
        /// stanza.
        /// </summary>
        /// <param name="iq">The original stanza to reply to.</param>
        /// <param name="data">The first-level data element to include as
        /// part of the response.</param>
        /// <exception cref="ArgumentNullException">The iq parameter is
        /// null.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        internal void IqResult(Iq iq, XmlElement? data = null)
        {
            AssertValid(false);
            iq.ThrowIfNull(nameof(iq));
            Iq response = new(IqType.Result, iq.Id, iq.From, Jid, data);
            core.IqResponse(response);
        }

        /// <summary>
        /// Establishes a session with the XMPP server.
        /// </summary>
        /// <remarks>
        /// For details, refer to RFC 3921, Section 3. Session Establishment.
        /// </remarks>
        private void EstablishSession()
        {
            Iq ret = IqRequest(IqType.Set, Hostname, null,
                Xml.Element("session", "urn:ietf:params:xml:ns:xmpp-session"));
            if (ret.Type == IqType.Error)
                throw Util.ExceptionFromError(ret, "Session establishment failed for Hostname: " + Hostname);
        }

        /// <summary>
        /// Sets up the event handlers for the events exposed by the XmppCore instance.
        /// </summary>
        private void SetupEventHandlers()
        {
            core.Iq += (sender, e) => OnIq(e.Stanza);
            core.Presence += (sender, e) =>
                // FIXME: Raise Error event if constructor raises exception?
                OnPresence(new Presence(e.Stanza));
            core.Message += (sender, e) => OnMessage(new Message(e.Stanza));
            core.Error += (sender, e) => Error?.Invoke(sender, new(e.Exception));
            core.OnConnect += (sender, e) =>
            {
                ConnectionState state = ConnectionState.Disconnected;
                if (e.State == Core.ConnectionState.Connected)
                {
                    state = ConnectionState.Connected;
                }
                if (e.State == Core.ConnectionState.Lost)
                {
                    state = ConnectionState.Lost;
                }

                OnConnect?.Invoke(sender, new(state));
            };
        }

        /// <summary>
        /// Asserts the instance has not been disposed of and is connected to the
        /// XMPP server.
        /// </summary>
        /// <param name="authRequired">Set to true to assert the instance has been
        /// authenticated with the XMPP server.</param>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppIm instance is not
        /// connected to a remote host.</exception>
        private void AssertValid(bool authRequired = true)
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (!Connected)
                throw new InvalidOperationException("Not connected to XMPP server.");
            if (authRequired && !Authenticated)
                throw new InvalidOperationException("Not authenticated with XMPP server.");
        }

        /// <summary>
        /// Callback method when an IQ-request stanza has been received.
        /// </summary>
        /// <param name="iq">The received IQ stanza.</param>
        internal void OnIq(Iq iq)
        {
            // Invoke IInput<Iq> Plugins.
            foreach (var filter in extensions.Values.OfType<IInputFilter<Iq>>())
            {
                // Swallow IQ stanza?
                if (filter.Input(iq))
                    return;
            }
            var query = iq.Data["query"];
            if (query != null)
            {
                switch (query.NamespaceURI)
                {
                    case "jabber:iq:roster":
                        ProcessRosterIq(iq);
                        return;
                }
            }

            // If we're still here, send back an error response.
            IqError(iq, ErrorType.Cancel, ErrorCondition.FeatureNotImplemented);
        }

        /// <summary>
        /// Callback invoked when a presence stanza has been received.
        /// </summary>
        /// <param name="presence">The received presence stanza.</param>
        internal void OnPresence(Presence presence)
        {
            // Invoke IInput<Presence> Plugins.
            foreach (var filter in extensions.Values.OfType<IInputFilter<Presence>>())
            {
                // Swallow presence stanza?
                if (filter.Input(presence))
                    return;
            }
            switch (presence.Type)
            {
                case PresenceType.Available:
                case PresenceType.Unavailable:
                    ProcessStatusNotification(presence);
                    break;

                case PresenceType.Subscribe:
                    ProcessSubscriptionRequest(presence);
                    break;

                case PresenceType.Unsubscribe:
                    ProcessUnsubscribeRequest(presence);
                    break;

                case PresenceType.Subscribed:
                case PresenceType.Unsubscribed:
                    ProcessSubscriptionResult(presence);
                    break;
            }
        }

        /// <summary>
        /// Callback invoked when a message stanza has been received.
        /// </summary>
        /// <param name="message">The received message stanza.</param>
        internal void OnMessage(Message message)
        {
            // Invoke IInput<Message> Plugins.
            foreach (var filter in extensions.Values.OfType<IInputFilter<Message>>())
            {
                // Swallow message?
                if (filter.Input(message))
                    return;
            }

            if (message.From is null)
                return;

            if (message.Type == MessageType.Error)
            {
                ErrorMessage?.Invoke(this, new(message.From, message));
            }
            else if (message.Data["body"] != null)
            {
                Message?.Invoke(this, new(message.From, message));
            }
        }

        /// <summary>
        /// Processes presence stanzas containing availability and status
        /// information.
        /// </summary>
        /// <param name="presence">The presence stanza to process.</param>
        /// <exception cref="ArgumentException">The presence stanza contains
        /// invalid data.</exception>
        private void ProcessStatusNotification(Presence presence)
        {
            bool offline = presence.Type == PresenceType.Unavailable;
            var e = presence.Data["show"];
            var availability = offline ? Availability.Offline : Availability.Online;
            // If the optional 'show' element has been specified, parse the
            // availability status from it.
            if (!offline && e?.InnerText?.Length > 0)
            {
                string show = e.InnerText.Capitalize();
                availability = (Availability)Enum.Parse(typeof(Availability), show);
            }
            sbyte prio = 0;
            // Parse the optional 'priority' element.
            e = presence.Data["priority"];
            if (e?.InnerText?.Length > 0)
                prio = sbyte.Parse(e.InnerText);
            // Parse optional 'status' element(s).
            var langDef = presence.Data.GetAttribute("xml:lang");
            var dict = new Dictionary<string, string>();
            if (!(langDef?.Length > 0))
                langDef = core.Language?.Name;
            foreach (XmlNode node in presence.Data.GetElementsByTagName("status"))
            {
                if (node is not XmlElement element)
                    continue;
                var lang = element.GetAttribute("xml:lang");
                if (!(lang?.Length > 0))
                    lang = langDef;
                if (lang is not null)
                    dict.Add(lang, element.InnerText);
            }
            Status status = new(availability, dict, prio);
            // Raise Status event.
            if (presence.From is not null)
                Status?.Invoke(this, new(presence.From, status));
        }

        /// <summary>
        /// Processes a presence stanza containing a subscription request.
        /// It does not automatically accept or reject a subscription.
        /// Explicit invocation of ApproveSubscriptionRequest(presence.From) or
        /// RefuseSubscriptionRequest(presence.From) must take placed
        /// </summary>
        /// <param name="presence">The presence stanza to process.</param>
        private void ProcessSubscriptionRequest(Presence presence)
        {
            if (presence.From is not null)
                SubscriptionRequest?.Invoke(presence.From);
        }

        /// <summary>
        /// Processes a presence stanza containing an unsubscribe request.
        /// </summary>
        /// <param name="presence">The presence stanza to process.</param>
        private void ProcessUnsubscribeRequest(Presence presence)
        {
            if (presence.From is not null)
                Unsubscribed?.Invoke(this, new(presence.From));
        }

        /// <summary>
        /// Processes a presence stanza containing a response to a previously
        /// issues subscription requst.
        /// </summary>
        /// <param name="presence">The presence stanza to process.</param>
        private void ProcessSubscriptionResult(Presence presence)
        {
            if (presence.From is null)
                return;

            bool approved = presence.Type == PresenceType.Subscribed;
            if (approved)
            {
                SubscriptionApproved?.Invoke(this, new(presence.From));
            }
            else
            {
                SubscriptionRefused?.Invoke(this, new(presence.From));
            }
        }

        /// <summary>
        /// Parses a 'query' element containing zero or more roster items.
        /// </summary>
        /// <param name="query">The 'query' element containing the roster
        /// items to parse.</param>
        /// <returns>An initialized instance of the Roster class containing
        /// the parsed roster items.</returns>
        private Roster ParseRoster(XmlElement query)
        {
            Roster roster = new();
            var states = new Dictionary<string, SubscriptionState>() {
                { "none", SubscriptionState.None },
                { "to", SubscriptionState.To },
                { "from", SubscriptionState.From },
                { "both", SubscriptionState.Both }
            };
            foreach (XmlElement item in query.GetElementsByTagName("item"))
            {
                string jid = item.GetAttribute("jid");
                if (!(jid?.Length > 0))
                    continue;
                var name = item.GetAttribute("name");
                if (name?.Length == 0)
                    name = null;
                List<string> groups = new();
                foreach (XmlElement group in item.GetElementsByTagName("group"))
                    groups.Add(group.InnerText);
                string s = item.GetAttribute("subscription");
                SubscriptionState state = SubscriptionState.None;
                // Be lenient.
                if (states.ContainsKey(s))
                    state = states[s];
                s = item.GetAttribute("ask");
                roster.Add(new RosterItem(jid, name, state, s == "subscribe", groups));
            }
            return roster;
        }

        /// <summary>
        /// Processes an IQ stanza containing a roster management request.
        /// </summary>
        /// <param name="iq">The IQ stanza to process.</param>
        private void ProcessRosterIq(Iq iq)
        {
            var states = new Dictionary<string, SubscriptionState>() {
                { "none", SubscriptionState.None },
                { "to", SubscriptionState.To },
                { "from", SubscriptionState.From },
                { "both", SubscriptionState.Both }
            };
            // Ensure roster push is from a trusted source.
            bool trusted = iq.From is null || iq.From == Jid || iq.From == Jid.GetBareJid();
            var items = iq.Data["query"].GetElementsByTagName("item");
            // Push _should_ contain exactly 1 item.
            if (trusted && items.Count > 0 && items.Item(0) is XmlElement item)
            {
                string jid = item.GetAttribute("jid");
                if (jid?.Length > 0)
                {
                    var name = item.GetAttribute("name");
                    if (name?.Length == 0)
                        name = null;
                    List<string> groups = new();
                    foreach (XmlElement group in item.GetElementsByTagName("group"))
                        groups.Add(group.InnerText);
                    string s = item.GetAttribute("subscription");
                    SubscriptionState state = SubscriptionState.None;
                    if (states.ContainsKey(s))
                        state = states[s];
                    string ask = item.GetAttribute("ask");
                    RosterItem ri = new(jid, name, state, ask == "subscribe", groups);
                    RosterUpdated?.Invoke(this, new(ri, s == "remove"));
                }
                // Acknowledge IQ request.
                IqResult(iq);
            }
        }

        /// <summary>
        /// Parses the specified XML 'item' element containing an XMPP privacy rule.
        /// </summary>
        /// <param name="item">The XML element to parse.</param>
        /// <returns>An initialized instance of the PrivacyRule class.</returns>
        /// <exception cref="ArgumentNullException">The item parameter is null.</exception>
        /// <exception cref="ArgumentException">The specified item contains invalid
        /// or illegal data.</exception>
        /// <exception cref="FormatException">The value of the mandatory order
        /// attribute is malformed.</exception>
        /// <exception cref="OverflowException">The parsed value of the mandatory
        /// order attribute is greater than 32 bits.</exception>
        private PrivacyRule ParsePrivacyItem(XmlElement item)
        {
            item.ThrowIfNull(nameof(item));
            bool allow = item.GetAttribute("action") == "allow";
            uint order = uint.Parse(item.GetAttribute("order"));
            PrivacyGranularity granularity = 0;
            if (item["message"] != null)
                granularity |= PrivacyGranularity.Message;
            if (item["iq"] != null)
                granularity |= PrivacyGranularity.Iq;
            if (item["presence-in"] != null)
                granularity |= PrivacyGranularity.PresenceIn;
            if (item["presence-out"] != null)
                granularity |= PrivacyGranularity.PresenceOut;
            string type = item.GetAttribute("type");
            string value = item.GetAttribute("value");
            var states = new Dictionary<string, SubscriptionState>() {
                { "none", SubscriptionState.None },
                { "to", SubscriptionState.To },
                { "from", SubscriptionState.From },
                { "both", SubscriptionState.Both }
            };
            if (type?.Length > 0)
            {
                if (!(value?.Length > 0))
                    throw new ArgumentException("Missing value attribute.");
                switch (type)
                {
                    // value is a JID.
                    case "jid":
                        return new JidPrivacyRule(new Jid(value), allow, order, granularity);
                    // value is a groupname.
                    case "group":
                        return new GroupPrivacyRule(value, allow, order, granularity);
                    // value must be 'none', 'to', 'from' or 'both'.
                    case "subscription":
                        if (!states.ContainsKey(value))
                            throw new ArgumentException($"Invalid value for value attribute: {value}");
                        return new SubscriptionPrivacyRule(states[value], allow, order, granularity);

                    default:
                        throw new ArgumentException("The value of the type attribute " +
                            "is invalid: " + type);
                }
            }
            // If the element has no 'type' attribute, it's a generic privacy rule.
            return new PrivacyRule(allow, order, granularity);
        }
    }
}