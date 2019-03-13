namespace CustomizationEditor
{
    public static class StaticExtensions    
    {
        /// <summary>
        ///     Accepts a string extension consisting of XML escaping it using HTML entity reference
        /// </summary>
        /// <returns>
        ///     Encoded XML string
        /// </returns>
        /// <param name="s">The current string object</param>
        public static string EscapeXml(this string s)
        {
            string toxml = s;
            if (!string.IsNullOrEmpty(toxml))
            {
                // replace literal values with entities
                toxml = toxml.Replace("&", "&amp;");
                // toxml = toxml.Replace("'", "&apos;");
                // toxml = toxml.Replace("\"", "&quot;");
                toxml = toxml.Replace(">", "&gt;");
                toxml = toxml.Replace("<", "&lt;");
            }
            return toxml;
        }

        /// <summary>
        ///     Accepts a string extension consisting of HTML entity reference escaped XML converting it to standard XML syntax
        /// </summary>
        /// <returns>
        ///     XML string
        /// </returns>
        /// <param name="s">The current string object</param>
        public static string UnescapeXml(this string s)
        {
            string unxml = s;
            if (!string.IsNullOrEmpty(unxml))
            {
                // replace entities with literal values
                unxml = unxml.Replace("&apos;", "'");
                unxml = unxml.Replace("&quot;", "\"");
                unxml = unxml.Replace("&gt;", ">");
                unxml = unxml.Replace("&lt;", "<");
                unxml = unxml.Replace("&amp;", "&");
            }
            return unxml;
        }

    }
}
