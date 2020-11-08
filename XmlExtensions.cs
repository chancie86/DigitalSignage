using System;
using System.Globalization;
using System.Xml;

namespace Display
{
    /// <summary>
    /// Simple helpers for processing XML.
    /// </summary>
    internal static class XmlExtensions
    {
        /// <summary>
        /// Retrieves the value of an attribute, or return a default value if
        /// the attribute doesn't exist.
        /// </summary>
        /// <param name="node">XmlNode to examine</param>
        /// <param name="attributeName">Attribute to look for</param>
        /// <param name="defaultValue">Default value to return if attribute is not present</param>
        /// <exception cref="ArgumentNullException">Thrown if node or attributeName are null</exception>
        /// <returns>Value of attribute or defaultValue.</returns>
        public static string Attr(this XmlNode node, string attributeName, string defaultValue)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (attributeName == null)
            {
                throw new ArgumentNullException("attributeName");
            }

            if (node.Attributes == null)
            {
                return defaultValue;
            }

            var attr = node.Attributes[attributeName];
            return attr == null ? defaultValue : attr.Value;
        }

        /// <summary>
        /// Retrieves the value of a required attribute.
        /// This method throws a FormatException if the attribute isn't found.
        /// </summary>
        /// <param name="node">XmlNode to examine</param>
        /// <param name="attributeName">Attribute to extract</param>
        /// <exception cref="FormatException">Thrown if attribute is missing</exception>
        /// <returns>Value of attribute</returns>
        public static string Attr(this XmlNode node, string attributeName)
        {
            var value = Attr(node, attributeName, null);
            if (value == null)
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture,
                    "Missing attribute '{0}'", attributeName));
            }

            return value;
        }
    }
}
