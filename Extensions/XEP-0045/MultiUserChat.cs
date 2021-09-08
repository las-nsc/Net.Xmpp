using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

using Net.Xmpp.Core;
using Net.Xmpp.Extensions.Dataforms;
using Net.Xmpp.Im;

namespace Net.Xmpp.Extensions
{
    internal class MultiUserChat : XmppExtension, IInputFilter<Im.Message>, IInputFilter<Im.Presence>
    {
        public MultiUserChat(XmppIm im) : base(im)
        {
        }

        /// <summary>
        /// An enumerable collection of XMPP namespaces the extension implements.
        /// </summary>
        /// <remarks>This is used for compiling the list of supported extensions
        /// advertised by the 'MultiUserChat' extension.</remarks>
        public override IEnumerable<string> Namespaces => new string[] {
                    MucNs.NsMain
                };

        /// <summary>
        /// The named constant of the Extension enumeration that corresponds to this
        /// extension.
        /// </summary>
        public override Extension Xep => Extension.MultiUserChat;

        public event EventHandler<Im.MessageEventArgs> SubjectChanged;

        public event EventHandler<GroupPresenceEventArgs>? PrescenceChanged;

        public event EventHandler<GroupInviteEventArgs>? InviteReceived;

        public event EventHandler<GroupInviteDeclinedEventArgs>? InviteWasDeclined;

        public event EventHandler<GroupErrorEventArgs>? MucErrorResponse;

        public RegistrationCallback VoiceRequested;

        public override void Initialize()
        {
            base.Initialize();
        }

        public bool Input(Im.Message stanza)
        {
            if (MucError.IsError(stanza))
            {
                // Unable to send a message... many reasons
                var error = new MucError(stanza);
                MucErrorResponse?.Invoke(this, new(error));
                return true;
            }

            if (Invite.IsElement(stanza))
            {
                // Incoming chat room invite
                var invite = new Invite(stanza);
                InviteReceived?.Invoke(this, new(invite));
                return true;
            }

            if (DirectInvite.IsElement(stanza))
            {
                // Incoming chat room invite
                var invite = new DirectInvite(stanza);
                InviteReceived?.Invoke(this, new(invite));
                return true;
            }

            if (InviteDeclined.IsElement(stanza))
            {
                // Chat room invite was declined
                var invite = new InviteDeclined(stanza);
                InviteWasDeclined?.Invoke(this, new(invite));
                return true;
            }

            if (stanza.Subject != null)
            {
                // Subject change
                SubjectChanged?.Invoke(this, new(stanza.From, stanza));
                return true;
            }

            // Things that could happen here:
            // Receive Registration Request
            // Receive Voice Request
            XmlElement xElement = stanza.Data["x"];
            if (xElement?.NamespaceURI == MucNs.NsXData)
            {
                switch (xElement.FirstChild.Value)
                {
                    default:
                        break;
                    case MucNs.NsRequest:
                        // Invoke Voice Request Submission callback/event.
                        // 8.6 Approving Voice Requests
                        if (VoiceRequested != null)
                        {
                            SubmitForm form = VoiceRequested.Invoke(new(xElement));
                            var message = new Core.Message(stanza.From, im.Jid, form.ToXmlElement());
                            SendMessage(message);
                            return true;
                        }
                        break;
                    case MucNs.NsRegister:
                        // Invoke Registration Request Submission callback/event.
                        // 9.9 Approving Registration Requests
                        // I'm unsure on how to implement this.
                        // return true;
                        break;
                }
            }

            // Any message with a body can be managed by the IM extension
            // Such as Group Chat Message & Group Chat History
            return false;
        }

        public bool Input(Im.Presence stanza)
        {
            if (MucError.IsError(stanza))
            {
                // Unable to join - No nickname specified / Duplicate nickname exists ... etc
                var error = new MucError(stanza);
                MucErrorResponse?.Invoke(this, new(error));
                return true;
            }

            // Things that could happen here:
            // Service Sends Notice of Membership
            // Service Passes Along Changed Presence
            // Service Updates Nick
            foreach (XmlElement xElement in stanza.Data.ChildNodes)
            {
                if (xElement?.NamespaceURI == MucNs.NsUser)
                {
                    Occupant? person = null;
                    foreach (XmlElement item in xElement.GetElementsByTagName("item"))
                    {
                        // There is only ever one item in a message here but, 
                        // I don't have a better way of getting the first element as an element, not a node.
                        var itemJid = item.GetAttribute("jid");
                        var itemAffiliation = item.GetAttribute("affiliation");
                        var itemRole = item.GetAttribute("role");
                        if (!string.IsNullOrWhiteSpace(itemJid) && !string.IsNullOrWhiteSpace(itemAffiliation) &&
                                !string.IsNullOrWhiteSpace(itemRole))
                        {
                            person = new Occupant(
                               item.GetAttribute("jid"),
                               item.GetAttribute("affiliation"),
                               item.GetAttribute("role"));
                        }
                    }

                    IList<MucStatusType> statusCodeList = new List<MucStatusType>();
                    foreach (XmlElement item in xElement.GetElementsByTagName("status"))
                    {
                        string codeAttribute = item.GetAttribute("code");
                        if (!string.IsNullOrWhiteSpace(codeAttribute))
                        {
                            var code = (MucStatusType)Enum.Parse(typeof(MucStatusType), codeAttribute);
                            statusCodeList.Add(code);
                        }
                    }

                    if (person != null)
                    {
                        PrescenceChanged?.Invoke(this, new(new(stanza.From.Domain, stanza.From.Node), person, statusCodeList));
                        return true;
                    }
                }
            }

            // Any message with an Availability status can be managed by the Presence extension
            return false;
        }

        /// <summary>
        /// Returns a list of active public chat room messages.
        /// </summary>
        /// <param name="chatService">JID of the chat service (depends on server)</param>
        /// <returns>List of Room JIDs</returns>
        public IEnumerable<RoomInfoBasic> DiscoverRooms(Jid chatService)
        {
            chatService.ThrowIfNull(nameof(chatService));
            return QueryRooms(chatService);
        }

        /// <summary>
        /// Returns a list of active public chat room messages.
        /// </summary>
        /// <param name="chatRoom">Existing room info</param>
        /// <returns>Information about room</returns>
        public RoomInfoExtended GetRoomInfo(Jid chatRoom)
        {
            chatRoom.ThrowIfNull(nameof(chatRoom));
            return QueryRoom(chatRoom);
        }

        /// <summary>
        /// Joins or creates new room using the specified room.
        /// </summary>
        /// <param name="jid">Chat room</param>
        /// <param name="nickname">Desired nickname</param>
        /// <param name="password">(Optional) Password</param>
        public void JoinRoom(Jid jid, string nickname, string? password = null)
        {
            XmlElement elem = Xml.Element("x", MucNs.NsMain);

            if (!string.IsNullOrEmpty(password))
                elem.Child(Xml.Element("password").Text(password));

            Jid joinRequest = new(jid.Domain, jid.Node, nickname);
            var msg = new Im.Presence(joinRequest, im.Jid, PresenceType.Available, null, null, elem);
            im.SendPresence(msg);
        }

        /// <summary>
        /// Leaves the specified room.
        /// </summary>
        public void LeaveRoom(Jid jid, string nickname)
        {
            Jid groupSpecificJid = new(jid.Domain, jid.Node, nickname);
            var msg = new Im.Presence(jid, groupSpecificJid, PresenceType.Unavailable);
            im.SendPresence(msg);
        }

        /// <summary>
        /// Requests previous chat room messages.
        /// </summary>
        public void GetMessageLog(History options)
        {
            im.SendPresence(new Im.Presence(options));
        }

        /// <summary>
        /// Requests a list of occupants within a specific room.
        /// </summary>
        public IEnumerable<Occupant> GetMembers(Jid room)
        {
            return QueryOccupants(room);
        }

        /// <summary>
        /// Requests a list of occupants with a specific affiliation.
        /// </summary>
        public IEnumerable<Occupant> GetMembers(Jid room, Affiliation affiliation)
        {
            return QueryOccupants(room, affiliation);
        }

        /// <summary>
        /// Requests a list of occupants with a specific role.
        /// </summary>
        public IEnumerable<Occupant> GetMembers(Jid room, Role role)
        {
            return QueryOccupants(room, role);
        }

        /// <summary>
        /// Set your nickname in the room.
        /// </summary>
        public void SetNickName(Jid room, string nickname)
        {
            room.ThrowIfNull(nameof(room));
            nickname.ThrowIfNullOrEmpty("nickname");

            Jid request = new(room.Domain, room.Node, nickname);
            var msg = new Core.Presence(request, im.Jid, null, null, null);

            im.SendPresence(new Im.Presence(msg));
        }

        /// <summary>
        /// Allows occupants to request privileges to a room.
        /// </summary>
        public void RequestPrivilige(Jid room, Role role)
        {
            if (role == Role.None)
                return;

            XmlElement formTypeValue = Xml.Element("value")
                .Text(MucNs.NsRequest);

            XmlElement formType = Xml.Element("field")
                .Attr("var", "FORM_TYPE")
                .Child(formTypeValue);

            XmlElement requestedRoleValue = Xml.Element("value")
                .Text(role.ToString().ToLowerInvariant());

            XmlElement requestedRole = Xml.Element("field")
                .Attr("var", MucNs.Role)
                .Attr("type", "list-single")
                .Attr("label", "Requested Role")
                .Child(requestedRoleValue);

            DataField[] fields =
            {
                new DataField(formType),
                new DataField(requestedRole)
            };

            SubmitForm form = new(fields);
            var message = new Core.Message(room, im.Jid, form.ToXmlElement());
            SendMessage(message);
        }

        public DataForm RequestRegistration(Jid room)
        {
            Iq iq = im.IqRequest(IqType.Get, room, im.Jid, Xml.Element("query", "jabber:iq:register"));
            if (iq.Type != IqType.Result)
                throw new NotSupportedException("Could not query features: " + iq);

            // Parse the result.
            var query = iq.Data["query"];
            return query == null || query.NamespaceURI != "jabber:iq:register"
                ? throw new NotSupportedException("Erroneous response: " + iq)
                : DataFormFactory.Create(query["x"]);
        }

        public bool SendRegistration(Jid room, DataForm form)
        {
            Iq iq = im.IqRequest(IqType.Set, room, im.Jid, Xml.Element("query", "jabber:iq:register").Child(form.ToXmlElement()));
            return iq.Type != IqType.Result ? throw new NotSupportedException("Could not query features: " + iq) : true;
        }

        public void RequestInstantRoom(Jid room)
        {
            Iq iq = im.IqRequest(IqType.Set, room, im.Jid, Xml.Element("query", MucNs.NsOwner).Child(Xml.Element("x", "jabber:x:data").Attr("type", "submit")));
            if (iq.Type != IqType.Result)
                throw new NotSupportedException("Could not query features: " + iq);
        }

        /// <summary>
        /// Allows owners and admins to grant privileges to an occupant.
        /// </summary>
        public bool SetPrivilege(Jid room, string nickname, Role privilege, string? reason = null)
        {
            return PostPrivilegeChange(room, nickname, privilege, reason);
        }

        /// <summary>
        /// Allows owners and admins to grant privileges to an occupant.
        /// </summary>
        public bool SetPrivilege(Jid room, Jid user, Affiliation privilege, string? reason = null, string? nickname = null)
        {
            return PostPrivilegeChange(room, user, privilege, reason, nickname);
        }

        public void ModifyRoomConfig(Jid room, RegistrationCallback callback)
        {
            RequestForm form = RequestRoomConfigForm(room);
            SubmitForm submit = callback.Invoke(form);
            SubmitRoomConfigForm(room, submit);
        }

        /// <summary>
        /// Allows moderators to kick an occupant from the room.
        /// </summary>
        /// <param name="room">chat room</param>
        /// <param name="nickname">user to kick</param>
        /// <param name="reason">reason for kick</param>
        public void KickOccupant(Jid room, string nickname, string reason)
        {
            XmlElement item = Xml.Element("item");
            item.Attr("nick", nickname);
            item.Attr("role", Role.None.ToString());

            if (!string.IsNullOrEmpty(reason))
                item.Child(Xml.Element("reason").Text(reason));

            XmlElement query = Xml.Element("query", MucNs.NsAdmin)
                .Child(item);

            Iq iq = im.IqRequest(IqType.Set, room, im.Jid, query);
            if (iq.Type != IqType.Result)
                throw new NotSupportedException("Could not query features: " + iq);
        }

        /// <summary>
        /// Asks the chat service to invite the specified user to the chat room you specify.
        /// </summary>
        /// <param name="to">user you intend to invite to chat room.</param>
        /// <param name="message">message you want to send to the user.</param>
        /// <param name="room">Jid of the chat room.</param>
        /// <param name="password">Password if any.</param>
        public void SendInvite(Jid to, Jid room, string message, string? password = null)
        {
            SendMessage(new DirectInvite(new Jid(to.Domain, to.Node), im.Jid, room, message, password));
        }

        /// <summary>
        /// Responds to a group chat invitation with a decline message.
        /// </summary>
        /// <param name="invite">Original group chat invitation.</param>
        /// <param name="reason">Reason for declining.</param>
        public void DeclineInvite(Invite invite, string reason)
        {
            SendMessage(new InviteDeclined(invite, reason));
        }

        /// <summary>
        /// Allows moderators (and above) to edit the room subject.
        /// </summary>
        public void EditRoomSubject(Jid room, string subject)
        {
            subject.ThrowIfNull(nameof(subject));
            Im.Message message = new(room, null, subject, null, MessageType.Groupchat);
            SendMessage(message);
        }

        /// <summary>
        /// Allows owners to destroy the room.
        /// </summary>
        public bool DestroyRoom(Jid room, string? reason = null)
        {
            room.ThrowIfNull(nameof(room));

            var item = Xml.Element("destroy")
                    .Attr("jid", room.ToString());

            if (!string.IsNullOrWhiteSpace(reason))
                item.Child(Xml.Element("reason").Text(reason));

            var queryElement = Xml.Element("query", MucNs.NsOwner)
                .Child(item);

            Iq iq = im.IqRequest(IqType.Set, room, im.Jid, queryElement);
            return iq.Type == IqType.Result;
        }

        private RequestForm RequestRoomConfigForm(Jid room)
        {
            Iq iq = im.IqRequest(IqType.Get, room, im.Jid, Xml.Element("query", MucNs.NsOwner));
            if (iq.Type != IqType.Result)
                throw new NotSupportedException("Could not query features: " + iq);

            // Parse the result.
            var query = iq.Data["query"];
            return query == null || query.NamespaceURI != MucNs.NsOwner
                ? throw new NotSupportedException("Erroneous response: " + iq)
                : DataFormFactory.Create(query["x"]) as RequestForm;
        }

        private void SubmitRoomConfigForm(Jid room, SubmitForm configForm)
        {
            // Construct the response element.
            var query = Xml.Element("query", MucNs.NsOwner);
            query.Child(configForm.ToXmlElement());

            Iq iq = im.IqRequest(IqType.Set, room, im.Jid, query);
            if (iq.Type == IqType.Error)
                throw Util.ExceptionFromError(iq, "The configuration changes could not be completed.");
        }

        private bool PostPrivilegeChange(Jid room, Jid user, Affiliation affiliation, string reason, string nickname)
        {
            room.ThrowIfNull(nameof(room));
            user.ThrowIfNull(nameof(user));

            var item = Xml.Element("item")
                    .Attr("affiliation", affiliation.ToString().ToLower())
                    .Attr("jid", user.ToString());

            if (!string.IsNullOrWhiteSpace(nickname))
            {
                item.Attr("nick", nickname);
            }
            if (!string.IsNullOrWhiteSpace(reason))
                item.Child(Xml.Element("reason").Text(reason));

            var queryElement = Xml.Element("query", MucNs.NsAdmin)
                .Child(item);

            Iq iq = im.IqRequest(IqType.Set, room, im.Jid, queryElement);
            return iq.Type == IqType.Result;
        }

        private bool PostPrivilegeChange(Jid room, string nickname, Role role, string reason)
        {
            room.ThrowIfNull(nameof(room));
            nickname.ThrowIfNull(nameof(nickname));

            var item = Xml.Element("item")
                    .Attr("role", role.ToString().ToLower())
                    .Attr("nick", nickname);

            if (!string.IsNullOrWhiteSpace(reason))
                item.Child(Xml.Element("reason").Text(reason));

            var queryElement = Xml.Element("query", MucNs.NsAdmin)
                .Child(item);

            Iq iq = im.IqRequest(IqType.Set, room, im.Jid, queryElement);
            return iq.Type == IqType.Result;
        }

        /// <summary>
        /// Queries for occupants in a room,
        /// This will fail if you do not have permissions.
        /// </summary>
        /// <param name="room">Chat room to query</param>
        /// <returns>An enumerable collection of items of the XMPP entity
        /// with the specified IRoom.</returns>
        /// <exception cref="ArgumentNullException">The IRoom jid parameter
        /// is null.</exception>
        /// <exception cref="NotSupportedException">The query could not be
        /// performed or the response was invalid.</exception>
        private IEnumerable<Occupant> QueryOccupants(Jid room)
        {
            room.ThrowIfNull(nameof(room));
            var items = QueryItems(room, Xml.Element("query", MucNs.NsRequestItems));
            return items.Select(x => new Occupant(x.Jid, x.Affiliation, x.Role));
        }

        /// <summary>
        /// Queries for occupants in a room,
        /// This will fail if you do not have permissions.
        /// </summary>
        /// <param name="room">Chat room to query</param>
        /// <param name="affiliation">Queried user affiliation</param>
        /// <returns>An enumerable collection of items of the XMPP entity
        /// with the specified IRoom.</returns>
        /// <exception cref="ArgumentNullException">The IRoom jid parameter
        /// is null.</exception>
        /// <exception cref="NotSupportedException">The query could not be
        /// performed or the response was invalid.</exception>
        private IEnumerable<Occupant> QueryOccupants(Jid room, Affiliation affiliation)
        {
            room.ThrowIfNull(nameof(room));
            var queryElement = Xml.Element("query", MucNs.NsAdmin)
                .Child(Xml.Element("item").Attr("affiliation", affiliation.ToString().ToLower()));

            var items = QueryItems(room, queryElement);
            return items.Select(x => new Occupant(x.Jid, x.Affiliation, x.Role));
        }

        /// <summary>
        /// Queries for occupants in a room,
        /// This will fail if you do not have permissions.
        /// </summary>
        /// <param name="room">Chat room to query</param>
        /// <param name="role">Queried user role</param>
        /// <returns>An enumerable collection of items of the XMPP entity
        /// with the specified IRoom.</returns>
        /// <exception cref="ArgumentNullException">The IRoom jid parameter
        /// is null.</exception>
        /// <exception cref="NotSupportedException">The query could not be
        /// performed or the response was invalid.</exception>
        private IEnumerable<Occupant> QueryOccupants(Jid room, Role role)
        {
            room.ThrowIfNull(nameof(room));
            var queryElement = Xml.Element("query", MucNs.NsAdmin)
                .Child(Xml.Element("item").Attr("role", role.ToString().ToLower()));

            var items = QueryItems(room, queryElement);
            return items.Select(x => new Occupant(x.Jid, x.Affiliation, x.Role));
        }

        /// <summary>
        /// Queries the XMPP entity with the specified JID for item information.
        /// </summary>
        /// <param name="jid">The JID of the XMPP entity to query.</param>
        /// <returns>An enumerable collection of items of the XMPP entity
        /// with the specified JID.</returns>
        /// <exception cref="ArgumentNullException">The jid parameter
        /// is null.</exception>
        /// <exception cref="NotSupportedException">The query could not be
        /// performed or the response was invalid.</exception>
        private IEnumerable<RoomInfoBasic> QueryRooms(Jid jid)
        {
            var items = QueryItems(jid, Xml.Element("query", MucNs.NsRequestItems));
            return items.Select(x => new RoomInfoBasic(x.Jid, x.Name));
        }

        /// <summary>
        /// Queries the XMPP entity with the specified JID for item information.
        /// </summary>
        /// <param name="jid">The JID of the XMPP entity to query.</param>
        /// <param name="query">Query object that will be sent 
        /// in the Iq request to the service.</param>
        /// <returns>An enumerable collection of items of the XMPP entity
        /// with the specified JID.</returns>
        /// <exception cref="ArgumentNullException">The jid parameter
        /// is null.</exception>
        /// <exception cref="NotSupportedException">The query could not be
        /// performed or the response was invalid.</exception>
        private IEnumerable<XmppItem> QueryItems(Jid jid, XmlElement query)
        {
            jid.ThrowIfNull(nameof(jid));
            query.ThrowIfNull(nameof(query));
            Iq iq = im.IqRequest(IqType.Get, jid, im.Jid, query);
            if (iq.Type != IqType.Result)
                throw new NotSupportedException("Could not query items: " + iq);
            // Parse the result.
            var response = iq.Data["query"];
            if (response == null)
                throw new NotSupportedException("Erroneous response: " + iq);
            ISet<XmppItem> items = new HashSet<XmppItem>();
            foreach (XmlElement e in response.GetElementsByTagName("item"))
            {
                string _jid = e.GetAttribute("jid"), node = e.GetAttribute("node"),
                    name = e.GetAttribute("name"), nick = e.GetAttribute("nick"),
                    affiliation = e.GetAttribute("affiliation"), role = e.GetAttribute("role");
                if (string.IsNullOrEmpty(_jid))
                    continue;
                try
                {
                    items.Add(new XmppItem(_jid, node, name, nick, role, affiliation));
                }
                catch (ArgumentException)
                {
                    // The JID is malformed, ignore the item.
                }
            }

            return items;
        }

        /// <summary>
        /// Queries the XMPP entity with the JID in the specified RoomInfo for item information.
        /// </summary>
        /// <param name="room">Holds the JID of the XMPP entity to query.</param>
        /// <returns>A more detailed description of the specified room.</returns>
        private RoomInfoExtended QueryRoom(Jid room)
        {
            room.ThrowIfNull(nameof(room));
            Iq iq = im.IqRequest(IqType.Get, room, im.Jid,
                Xml.Element("query", MucNs.NsRequestInfo));
            if (iq.Type != IqType.Result)
                throw new NotSupportedException("Could not query features: " + iq);
            // Parse the result.
            var query = iq.Data["query"];
            if (query == null || query.NamespaceURI != MucNs.NsRequestInfo)
                throw new NotSupportedException("Erroneous response: " + iq);

            Identity id = ParseIdentity(query);
            IEnumerable<DataField> features = ParseFields(query, "feature");
            IEnumerable<DataField> fields = ParseFields(query, "field");

            return new RoomInfoExtended(room, id.Name, features, fields);
        }

        /// <summary>
        /// Queries the XMPP entity with the specified JID for identity information.
        /// </summary>
        /// <param name="query">The query result</param>
        /// <returns>The first Identity returned.</returns>
        private Identity ParseIdentity(XmlElement query)
        {
            Identity? result = null;

            foreach (XmlElement e in query.GetElementsByTagName("identity"))
            {
                string cat = e.GetAttribute("category");
                string type = e.GetAttribute("type");
                string name = e.GetAttribute("name");

                if (!string.IsNullOrEmpty(cat) && !string.IsNullOrEmpty(type))
                {
                    result = new Identity(cat, type,
                        string.IsNullOrEmpty(name) ? null : name);
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Parses the Identity element and returns a list of the identity's features.
        /// </summary>
        /// <param name="query">The query result</param>
        /// <param name="tagName">The tag name of the objects</param>
        /// <returns>An enumerable collection of DataFields</returns>
        private IEnumerable<DataField> ParseFields(XmlElement query, string tagName)
        {
            ISet<DataField> fields = new HashSet<DataField>();

            foreach (XmlElement f in query.GetElementsByTagName(tagName))
            {
                fields.Add(new DataField(f));
            }

            return fields;
        }

        private void SendMessage(Core.Message message)
        {
            im.SendMessage(new Im.Message(message));
        }
    }
}
