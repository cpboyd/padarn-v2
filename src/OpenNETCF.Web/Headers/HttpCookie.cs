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
using System.Collections.Specialized;
using System.Text;
using OpenNETCF.Web.Configuration;

namespace OpenNETCF.Web
{
    /// <summary>
    /// Provides a type-safe way to create and manipulate individual HTTP cookies.
    /// </summary>
    public sealed class HttpCookie
    {
        private string domain;
        private bool expirationSet;
        private DateTime expires;
        private bool httpOnly;
        private HttpValueCollection multiValue;
        private string name;
        private string path;
        private bool secure;
        private string stringValue;

        internal HttpCookie()
        {
            this.Changed = true;
            this.path = "/";
        }

        /// <summary>
        /// Creates and names a new cookie.
        /// </summary>
        /// <param name="name"></param>
        public HttpCookie(string name)
            : this()
        {
            this.SetDefaultsFromConfig();
            this.name = name;
        }

        /// <summary>
        /// Creates, names, and assigns a value to a new cookie.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public HttpCookie(string name, string value)
            : this(name)
        {
            this.stringValue = value;
        }

        internal bool Added { get; set; }

        internal bool Changed { get; set; }

        /// <summary>
        /// Gets or sets the domain to associate the cookie with.
        /// </summary>
        public string Domain
        {
            get { return this.domain; }
            set
            {
                this.domain = value;
                this.Changed = true;
            }
        }

        /// <summary>
        /// Gets or sets the expiration date and time for the cookie.
        /// </summary>
        public DateTime Expires
        {
            get
            {
                return this.expirationSet ? this.expires : DateTime.MinValue;
            }
            set
            {
                this.expires = value;
                this.expirationSet = true;
                this.Changed = true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a cookie has subkeys.
        /// </summary>
        public bool HasKeys
        {
            get { return this.Values.HasKeys(); }
        }

        /// <summary>
        /// Gets or sets a value that specifies whether a cookie is accessible by client-side script.
        /// </summary>
        public bool HttpOnly
        {
            get { return this.httpOnly; }
            set
            {
                this.httpOnly = value;
                this.Changed = true;
            }
        }

        /// <summary>
        /// Gets or sets the name of a cookie.
        /// </summary>
        public string Name
        {
            get { return this.name; }
            set
            {
                this.name = value;
                this.Changed = true;
            }
        }

        /// <summary>
        /// Gets or sets the virtual path to transmit with the current cookie.
        /// </summary>
        public string Path
        {
            get { return this.path; }
            set
            {
                this.path = value;
                this.Changed = true;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to transmit the cookie using Secure Sockets Layer (SSL)--that is, over HTTPS only.
        /// </summary>
        public bool Secure
        {
            get { return this.secure; }
            set
            {
                this.secure = value;
                this.Changed = true;
            }
        }

        /// <summary>
        /// Gets or sets an individual cookie value.
        /// </summary>
        public string Value
        {
            get
            {
                return this.multiValue != null ? this.multiValue.ToString(false) : this.stringValue;
            }
            set
            {
                if (this.multiValue != null)
                {
                    this.multiValue.Reset();
                    this.multiValue.Add(null, value);
                }
                else
                {
                    this.stringValue = value;
                }
                this.Changed = true;
            }
        }

        /// <summary>
        /// Gets a collection of key/value pairs that are contained within a single cookie object.
        /// </summary>
        public NameValueCollection Values
        {
            get
            {
                if (this.multiValue == null)
                {
                    this.multiValue = new HttpValueCollection();
                    if (this.stringValue != null)
                    {
                        if ((this.stringValue.IndexOf('&') >= 0) || (this.stringValue.IndexOf('=') >= 0))
                        {
                            this.multiValue.FillFromString(this.stringValue);
                        }
                        else
                        {
                            this.multiValue.Add(null, this.stringValue);
                        }
                        this.stringValue = null;
                    }
                }
                this.Changed = true;
                return this.multiValue;
            }
        }

        public string this[string key]
        {
            get { return this.Values[key]; }
            set
            {
                this.Values[key] = value;
                this.Changed = true;
            }
        }

        internal string GetSetCookieHeader(HttpContext context)
        {
            var builder = new StringBuilder("Set-Cookie: ");

            if (!string.IsNullOrEmpty(this.name))
            {
                builder.Append(this.name)
                    .Append('=');
            }
            string value = Value;
            if (value != null)
            {
                builder.Append(value);
            }
            if (!string.IsNullOrEmpty(this.domain))
            {
                builder.Append("; domain=")
                    .Append(this.domain);
            }
            if (this.expirationSet && (this.expires != DateTime.MinValue))
            {
                builder.Append("; expires=")
                    .Append(HttpUtility.FormatHttpCookieDateTime(this.expires));
            }
            if (!string.IsNullOrEmpty(this.path))
            {
                builder.Append("; path=")
                    .Append(this.path);
            }
            if (this.secure)
            {
                builder.Append("; secure");
            }
            if (this.httpOnly && this.SupportsHttpOnly(context))
            {
                builder.Append("; HttpOnly");
            }
            return builder.ToString();
        }

        private void SetDefaultsFromConfig()
        {
            CookiesConfiguration cookiesConfig = ServerConfig.GetConfig().Cookies;
            if (cookiesConfig != null)
            {
                this.secure = cookiesConfig.RequireSSL;
                this.httpOnly = cookiesConfig.HttpOnlyCookies;

                if (!string.IsNullOrEmpty(cookiesConfig.Domain))
                {
                    this.domain = cookiesConfig.Domain;
                }
            }
        }

        private bool SupportsHttpOnly(HttpContext context)
        {
            if ((context == null) || context.Request == null)
            {
                return false;
            }

            HttpBrowserCapabilities userAgent = context.Request.Browser;
            return (userAgent != null) && (userAgent.Type != "IE5" || userAgent.Platform != "MacPPC");
        }
    }
}
