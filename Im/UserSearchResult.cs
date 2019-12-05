using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharp.Xmpp.Im
{
    /// <summary>
    /// Represents the result of SearchUser.
    /// </summary>
    public class UserSearchResult
    {
        /// <summary>
        /// User Jid
        /// </summary>
        public Jid Jid { get; set; }

        /// <summary>
        /// User Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// User E-mail
        /// </summary>
        public string Email { get; set; }
    }
}