using System;
using System.Xml;

namespace Net.Xmpp.Extensions.Dataforms
{
    /// <summary>
    /// A factory for creating data-form instances.
    /// </summary>
    internal static class DataFormFactory
    {
        /// <summary>
        /// Creates a data-form instance of the proper type from the specified
        /// XML element.
        /// </summary>
        /// <param name="element">The XML element to create the data-form instance
        /// from.</param>
        /// <returns>An initialized instance of a data-form class of the respectiv
        /// type which derives from the DataForm base class.</returns>
        /// <exception cref="ArgumentNullException">The element parameter is
        /// null.</exception>
        /// <exception cref="ArgumentException">The specified XML element is not a
        /// valid data-form element.</exception>
        public static DataForm Create(XmlElement element)
        {
            element.ThrowIfNull(nameof(element));
            if (element.Name != "x" || element.NamespaceURI != "jabber:x:data")
                throw new ArgumentException("Invalid root element: " + element.Name);
            string s = element.GetAttribute("type");
            if (!(s?.Length > 0))
                throw new ArgumentException("Missing 'type' attribute.");
            try
            {
                DataFormType type = Util.ParseEnum<DataFormType>(s);
                return type switch
                {
                    DataFormType.Form => new RequestForm(element),
                    DataFormType.Submit => new SubmitForm(element),
                    DataFormType.Cancel => new CancelForm(element),
                    DataFormType.Result => new ResultForm(element),
                    _ => throw new ArgumentException("Invalid form type: " + type),
                };
            }
            catch (Exception e)
            {
                throw new XmlException("Invalid data-form element.", e);
            }
        }
    }
}