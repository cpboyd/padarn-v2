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
using OpenNETCF.Web.Configuration;
using OpenNETCF.Web.Helpers;
using OpenNETCF.Web.Hosting;

namespace OpenNETCF.Web.Security
{
    /// <summary>
    /// Manages forms-authentication services for Web applications. This class cannot be inherited.
    /// </summary>
    public sealed class FormsAuthenticationModule : IHttpModule
    {
        private string m_loginPath;

        /// <summary>
        /// Initializes a new instance of the FormsAuthenticationModule class.
        /// </summary>
        public FormsAuthenticationModule()
        {
            // set the default
            LoginUrl = "login.aspx";
            FormsCookieName = ".ASPXAUTH";
            CookieDomain = string.Empty;
            DefaultUrl = "default.aspx";
            CookieMode = HttpCookieMode.UseCookies;
            EnableCrossAppRedirects = false;
            FormsCookiePath = "/";
            RequireSSL = false;
            SlidingExpiration = true;
            Timeout = new TimeSpan(0, 30, 0);
        }

        /// <summary>
        /// Gets the amount of time before an authentication ticket expires.
        /// </summary>
        public TimeSpan Timeout { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether sliding expiration is enabled.
        /// </summary>
        public bool SlidingExpiration { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the forms-authentication cookie requires SSL in order to be returned to the server.
        /// </summary>
        public bool RequireSSL { get; internal set; }

        /// <summary>
        /// Gets the path for the forms-authentication cookie.
        /// </summary>
        /// <value>The path of the cookie where the forms-authentication ticket information is stored. The default is "/".</value>
        public string FormsCookiePath { get; internal set; }

        /// <summary>
        /// Gets the URL that the FormsAuthenticationModule class will redirect to if no redirect URL is specified.
        /// </summary>
        public string DefaultUrl { get; internal set; }

        /// <summary>
        /// Gets the value of the domain of the forms-authentication cookie.
        /// </summary>
        public string CookieDomain { get; internal set; }

        /// <summary>
        /// Gets a value that indicates whether the application is configured for cookieless forms authentication.
        /// </summary>
        public HttpCookieMode CookieMode { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether authenticated users can be redirected to URLs in other Web applications.
        /// </summary>
        public bool EnableCrossAppRedirects { get; internal set; }

        /// <summary>
        /// Gets a value that indicates whether the application is configured to support cookieless forms authentication.
        /// </summary>
        public bool CookiesSupported
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the URL for the login page that the FormsAuthenticationModule class will redirect to.
        /// </summary>
        public string LoginUrl { get; internal set; }

        /// <summary>
        /// Gets the name of the cookie used to store the forms-authentication ticket.
        /// </summary>
        /// <value>The name of the cookie used to store the forms-authentication ticket. The default is ".ASPXAUTH".</value>
        public string FormsCookieName { get; internal set; }

        internal string LoginUrlServerPath
        {
            get
            {
                if (m_loginPath == null)
                {
                    m_loginPath = HostingEnvironment.MapPath(LoginUrl);
                }

                return m_loginPath;
            }
        }

        internal string ReturnUrl { get; set; }

        internal List<string> DenyUsers { get; set; }
        internal List<string> AllowsUsers { get; set; }

        #region IHttpModule Members

        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            context.AuthenticateRequest += OnAuthenticateRequest;
        }

        #endregion

        public void OnAuthenticateRequest(object sender, EventArgs e)
        {
            var context = (HttpContext)sender;
            string path = context.Request.Path;
            if (!ServerConfig.GetConfig().Authentication.IsForms ||
                !UrlPath.RequiresAuthentication(path))
            {
                return;
            }
            // is this a page that requires auth?
            string absolutePath = HostingEnvironment.MapPath(path);
            if (StringComparer.InvariantCultureIgnoreCase.Equals(absolutePath, LoginUrlServerPath))
            {
                return;
            }

            // are we already authenticated
            HttpCookie authCookie = context.Request.Cookies[FormsCookieName];
            if (authCookie != null &&
                // verify the cookie domain
                authCookie.Domain == CookieDomain &&
                // verify it hasn't expired
                authCookie.Expires > DateTime.Now)
            {
                // reset expiration
                if (SlidingExpiration)
                {
                    SetAuthCookie(authCookie["UID"], false);
                    //context.Response.Cookies[FormsCookieName]
                    authCookie.Expires = DateTime.Now.AddMinutes(30);
                }

                return;
            }

            // Redirect to login
            ReturnUrl = string.IsNullOrEmpty(path)
                ? DefaultUrl : path;
            string authUrl = string.Format("{0}?ReturnURL={1}", LoginUrl, path);
            context.Response.Redirect(authUrl);
            context.Response.Flush(true);
        }

        /// <summary>
        /// Returns the redirect URL for the original request that caused the redirect to the login page.
        /// </summary>
        /// <param name="userName">The name of the authenticated user.</param>
        /// <param name="createPersistentCookie">This parameter is ignored.</param>
        /// <returns>A string that contains the redirect URL.</returns>
        public string GetRedirectUrl(string userName, bool createPersistentCookie)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Redirects an authenticated user back to the originally requested URL or the default URL.
        /// </summary>
        /// <param name="userName">The authenticated user name.</param>
        /// <param name="createPersistentCookie"><b>true</b> to create a durable cookie (one that is saved across browser sessions); otherwise, <b>false</b>.</param>
        public void RedirectFromLoginPage(string userName, bool createPersistentCookie)
        {
            SetAuthCookie(userName, createPersistentCookie);
            HttpContext.Current.Response.Redirect(ReturnUrl);
        }

        public void SetAuthCookie(string userName, bool createPersistentCookie)
        {
            var authCookie = new HttpCookie(FormsCookieName)
            {
                Domain = CookieDomain,
                Expires = DateTime.Now.Add(Timeout)
            };
            authCookie.Values["UID"] = userName;

            if (createPersistentCookie)
            {
            }

            HttpContext.Current.Response.SetCookie(authCookie);
        }

        /// <summary>
        /// Redirects the browser to the login URL.
        /// </summary>
        public void RedirectToLoginPage()
        {
            HttpContext.Current.Response.Redirect(LoginUrl);
        }

        /// <summary>
        /// Removes the forms-authentication ticket from the browser.
        /// </summary>
        public void SignOut()
        {
            throw new NotSupportedException();
        }
    }
}
