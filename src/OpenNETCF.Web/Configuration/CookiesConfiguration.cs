#region License
// Copyright ©2017 Tacke Consulting (dba OpenNETCF)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software 
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, 
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or 
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR 
// ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#endregion

using System;
using System.Xml;

namespace OpenNETCF.Web.Configuration
{
    /// <summary>
    /// Configures properties for cookies used by a Web application. 
    /// </summary>
    public sealed class CookiesConfiguration
    {
        internal CookiesConfiguration() { }

        internal CookiesConfiguration(XmlNode node)
        {
            XmlAttribute domainAttrib = node.Attributes["Domain"];
            if (domainAttrib == null)
                throw new XmlException("<cookies> tag must have a 'Domain' attribute");

            Domain = domainAttrib.Value;

            if (node.Attributes["RequireSSL"] != null)
            {
                RequireSSL = Convert.ToBoolean(node.Attributes["RequireSSL"].Value);
            }

            if (node.Attributes["HttpOnlyCookies"] != null)
            {
                HttpOnlyCookies = Convert.ToBoolean(node.Attributes["HttpOnlyCookies"].Value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Secure Sockets Layer (SSL) communication is required. 
        /// </summary>
        public bool RequireSSL
        {
            get; internal set;
        }

        /// <summary>
        /// Gets or sets the cookie domain name. 
        /// </summary>
        public string Domain
        { 
            get; internal set; 
        }

        /// <summary>
        /// Gets or sets a value indicating whether the support for the browser's HttpOnly cookie is enabled. 
        /// </summary>
        public bool HttpOnlyCookies
        {
            get; internal set;
        }
    }
}
