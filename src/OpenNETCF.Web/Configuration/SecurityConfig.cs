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

using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace OpenNETCF.Web.Configuration
{
    /// <summary>
    /// Security config for the server (enabled protocols)
    /// </summary>
    public sealed class SecurityConfig
    {
        internal SecurityConfig()
        {
            this.Tls10 = true;
            this.Tls11 = true;
            this.Tls12 = true;
            this.ResumeSession = true;
            CipherList = new List<short>();
        }

        internal SecurityConfig(XmlNode node)
            : this()
        {
            ParseXml(node);
        }

        /// <summary>
        /// Get or the set the session resume capabilities on the server
        /// </summary>
        public bool ResumeSession { get; internal set; }

        /// <summary>
        /// Get or set the activation of TLS 1.0
        /// </summary>
        public bool Tls10 { get; internal set; }

        /// <summary>
        /// Get or set the activation of TLS 1.1
        /// </summary>
        public bool Tls11 { get; internal set; }

        /// <summary>
        /// Get or set the activation of TLS 1.2
        /// </summary>
        public bool Tls12 { get; internal set; }

        /// <summary>
        /// A comma-separated list of numeric cipher suites to enable.  This is Eldos-specific.
        /// See https://www.eldos.com/documentation/sbb/documentation/ref_cl_clientsslsocket_mtd_getciphersuites.html
        /// </summary>
        public List<short> CipherList { get; internal set; }

        private void ParseXml(XmlNode node)
        {
            foreach (XmlNode subnode in node.ChildNodes)
            {
                bool enabled = false;

                if (subnode.Attributes.Count > 0 && subnode.Attributes["Enabled"] != null)
                {
                    enabled = bool.Parse(subnode.Attributes["Enabled"].Value);
                }

                switch (subnode.Name)
                {
                    case "TLS10":
                        this.Tls10 = enabled;
                        break;

                    case "TLS11":
                        this.Tls11 = enabled;
                        break;

                    case "TLS12":
                        this.Tls12 = enabled;
                        break;

                    case "ResumeSession":
                        this.ResumeSession = enabled;
                        break;

                    case "CipherList":
                        try
                        {
                            string list = subnode.InnerText;
                            if (string.IsNullOrEmpty(list))
                            {
                                break;
                            }

                            IEnumerable<string> items = list
                                .Split(',')
                                .Select(item => item.Trim())
                                .Where(item => item.Length > 0);

                            foreach (string item in items)
                            {
                                try
                                {
                                    short cipher = short.Parse(item);
                                    this.CipherList.Add(cipher);
                                }
                                catch
                                {
                                    // ignore non-numerics
                                }
                            }
                        }
                        catch
                        {
                            // just use the defaults
                        }
                        break;
                }
            }
        }
    }
}
