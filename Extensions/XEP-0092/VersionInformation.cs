﻿using System;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Provides information about the software application associated with an
    /// XMPP entity.
    /// </summary>
    [Serializable]
    public class VersionInformation
    {
        /// <summary>
        /// The natural-language name of the software.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The specific version of the software.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// The operating system of the entity.
        /// </summary>
        public string Os { get; }

        /// <summary>
        /// Initializes a new instance of the VersionInformation class.
        /// </summary>
        /// <param name="name">The natural-language name of the software.</param>
        /// <param name="version">The specific version of the software.</param>
        /// <param name="os">The operating system of the entity.</param>
        /// <exception cref="ArgumentNullException">The name parameter or the version
        /// parameter is null.</exception>
        public VersionInformation(string name, string version, string os = null)
        {
            name.ThrowIfNull(nameof(name));
            version.ThrowIfNull(nameof(version));
            Name = name;
            Version = version;
            Os = os;
        }
    }
}