using System;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Represents meta-data of a file.
    /// </summary>
    [Serializable]
    internal class FileMetaData
    {
        /// <summary>
        /// The name of the file.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// A description of the file so the receiver can better understand what
        /// is being sent.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Initializes a new instance of the FileMetaData class.
        /// </summary>
        /// <param name="name">The name of the file.</param>
        /// <param name="description">A description of the file so the receiver
        /// can better understand what is being sent.</param>
        /// <exception cref="ArgumentNullException">The name parameter is
        /// null.</exception>
        public FileMetaData(string name, string description = null)
        {
            name.ThrowIfNull(nameof(name));
            Name = name;
            Description = description;
        }
    }
}