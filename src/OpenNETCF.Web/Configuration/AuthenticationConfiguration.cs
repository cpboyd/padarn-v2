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
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using OpenNETCF.Web.Security;

namespace OpenNETCF.Web.Configuration
{
    /// <summary>
    /// Descriptions the web server's authentication configuration
    /// </summary>
    public sealed class AuthenticationConfiguration
    {
        private UserCollection m_users;

        /// <summary>
        /// Creates an instance of the AuthneticationConfiguration class. 
        /// </summary>
        public AuthenticationConfiguration()
        {
            m_users = new UserCollection();
        }

        internal AuthenticationConfiguration(XmlNode node)
            : this()
        {
            ParseXml(node);
        }

        /// <summary>
        /// Gets the authentication mode. Either Basic, Digest or Forms.
        /// </summary>
        public string Mode { get; internal set; }

        /// <summary>
        /// Gets the authentication realm for HTTP Authentication.
        /// </summary>
        public string Realm { get; internal set; }

        /// <summary>
        /// Gets a boolean value indicating whether authentication is enabled or not. 
        /// </summary>
        public bool Enabled { get; internal set; }

        /// <summary>
        /// Describes the users configuration for authentication.
        /// </summary>
        public UserCollection Users
        {
            get { return m_users; }
        }

        public AuthenticationCallbackHandler AuthenticationCallback { get; set; }

        private void ParseXml(XmlNode node)
        {
            Mode = node.Attributes["Mode"].Value;

            if (StringComparer.InvariantCultureIgnoreCase.Equals(Mode, "forms"))
            {
                FormsAuthentication.IsEnabled = true;
            }

            XmlAttribute attr = node.Attributes["Enabled"];
            Enabled = false;
            if (attr != null)
            {
                try
                {
                    Enabled = bool.Parse(attr.Value);
                }
                catch { }
            }

            attr = node.Attributes["Realm"];
            Realm = "Padarn";
            if (attr != null)
            {
                try
                {
                    Realm = attr.Value;
                }
                catch { }
            }

            if (!node.HasChildNodes)
            {
                return;
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                switch (child.Name.FirstCharToLower())  // for backward compatibility
                {
                    case "users":
                        foreach (XmlNode userNode in child.ChildNodes)
                        {
                            string name = userNode.AttributeOrEmpty("Name");
                            string password = userNode.AttributeOrEmpty("Password");

                            var user = new User { Name = name, Password = password };
                            if (userNode.HasChildNodes)
                            {
                                IEnumerable<Role> roles = userNode.ChildNodes.GetAttributeList("Name")
                                    .Select(roleName => new Role { Name = roleName });
                                user.Roles.AddRange(roles);
                            }

                            Users.Add(user);
                        }
                        break;
                    case "forms":
                        //loginUrl="login.aspx"
                        //name=".PADARNAUTH"
                        //domain="testdomain"
                        //defaultUrl="default.aspx"
                        //path="/"
                        //timeout="30"

                        //slidingExpiration="true"
                        //requireSSL="false"
                        //protection="None"
                        //enableCrossAppRedirects="false"
                        //cookieless="UseCookies"    

                        attr = child.Attributes["loginUrl"];
                        if (attr != null)
                        {
                            FormsAuthentication.LoginUrl = attr.Value;
                        }

                        attr = child.Attributes["loginUrl"];
                        if (attr != null)
                        {
                            FormsAuthentication.LoginUrl = attr.Value;
                        }
                        break;
                }
            }
        }
    }
}
