﻿using System;
using System.Xml;

namespace Net.Xmpp.Extensions.Dataforms
{
    /// <summary>
    /// Represents a field for gathering or providing a single line or word of
    /// text, which may be shown in an interface.
    /// </summary>
    /// <remarks>
    /// This corresponds to a Winforms TextBox control.
    /// </remarks>
    public class TextField : DataField
    {
        /// <summary>
        /// The value of the field.
        /// </summary>
        public string? Value
        {
            get => element["value"]?.InnerText;

            private set
            {
                if (element["value"] is { } node)
                {
                    if (value is null)
                        element.RemoveChild(node);
                    else
                        node.InnerText = value;
                }
                else
                {
                    if (value != null)
                        element.Child(Xml.Element("value").Text(value));
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the TextField class for use in a
        /// requesting dataform.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <param name="required">Determines whether the field is required or
        /// optional.</param>
        /// <param name="label">A human-readable name for the field.</param>
        /// <param name="description">A natural-language description of the field,
        /// intended for presentation in a user-agent.</param>
        /// <param name="value">The default value of the field.</param>
        /// <exception cref="ArgumentNullException">The name parameter is
        /// null.</exception>
        public TextField(string name, bool required = false, string? label = null,
            string? description = null, string? value = null)
            : base(DataFieldType.TextSingle, name, required, label, description)
        {
            name.ThrowIfNull(nameof(name));
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the TextField class for use in a
        /// submitting dataform.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <param name="value">The value of the field.</param>
        /// <exception cref="ArgumentNullException">The name parameter is
        /// null.</exception>
        public TextField(string name, string value)
            : this(name, false, null, null, value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the TextField class from the specified
        /// XML element.
        /// </summary>
        /// <param name="element">The XML 'field' element to initialize the instance
        /// with.</param>
        /// <exception cref="ArgumentNullException">The element parameter is
        /// null.</exception>
        /// <exception cref="ArgumentException">The specified XML element is not a
        /// valid data-field element, or the element is not a data-field of type
        /// 'text-single'.</exception>
        internal TextField(XmlElement element)
            : base(element)
        {
            AssertType(DataFieldType.TextSingle);
        }
    }
}