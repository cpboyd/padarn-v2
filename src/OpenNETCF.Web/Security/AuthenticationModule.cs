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
using System.Diagnostics;
using System.Net;
using System.Text;
using OpenNETCF.Security.Principal;
using OpenNETCF.Web.Configuration;
using OpenNETCF.Web.Helpers;
using OpenNETCF.Web.Security.Cryptography;

namespace OpenNETCF.Web.Security
{
    internal class AuthenticationModule : IHttpModule
    {
        private const int NonceLifetime = 60;
        public const string Basic = "Basic";
        public const string Digest = "Digest";

        private readonly string authMethod;
        private string m_lastRequestFrom;
        private int m_requestCount;
        private string m_user;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authenticationMethod"></param>
        public AuthenticationModule(string authenticationMethod)
        {
            if (authenticationMethod != Basic && authenticationMethod != Digest)
            {
                throw new NotSupportedException(string.Format("Authorization type {0} is not supported.", authenticationMethod));
            }
            authMethod = authenticationMethod;
        }

        /// <summary>
        /// 
        /// </summary>
        public string AuthenticationMethod
        {
            get { return authMethod; }
        }

        protected bool AuthenticationRequired
        {
            get
            {
                return StringComparer.InvariantCultureIgnoreCase
                    .Equals(AuthenticationMethod, ServerConfig.GetConfig().Authentication.Mode);
            }
        }

        internal virtual string User
        {
            get { return m_user; }
        }

        #region IHttpModule Members

        public virtual void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            context.AuthenticateRequest += OnAuthenticateRequest;
        }

        #endregion

        public virtual bool AcceptCredentials(HttpContext context, string authentication)
        {
            IAuthenticationCallbackInfo info = GetAuthInfo(context, authentication);

            if (AuthenticationMethod == Basic && string.IsNullOrEmpty(info.UserName))
            {
                return false;
            }

            m_user = info.UserName;

            bool auth = (ServerConfig.GetConfig().Authentication.AuthenticationCallback == null)
                ? CheckConfigUserList(info)
                : CheckUserWithServerCallback(info);

            // set the user info
            var id = new GenericIdentity(User, AuthenticationMethod.ToLowerInvariant()) { IsAuthenticated = auth };
            var principal = new GenericPrincipal(id);
            context.User = principal;

            return auth;
        }

        private IAuthenticationCallbackInfo GetAuthInfo(HttpContext context, string authentication)
        {
            switch (AuthenticationMethod)
            {
                case Digest:
                    var info = new DigestAuthInfo(context.Request.HttpMethod);

                    string[] elements = authentication.Split(',');

                    foreach (string element in elements)
                    {
                        int splitIndex = element.IndexOf('=');
                        string K = element.Substring(0, splitIndex).Trim(new[] { ' ', '\"' });
                        string V = element.Substring(splitIndex + 1).Trim(new[] { ' ', '\"' });
                        info.AddElement(K, V);
                    }

                    return info;
                default:
                    byte[] userpass = Convert.FromBase64String(authentication);
                    string[] up = Encoding.UTF8.GetString(userpass, 0, userpass.Length).Split(':');

                    return new BasicAuthInfo
                    {
                        UserName = up[0],
                        Password = up[1],
                        Realm = ServerConfig.GetConfig().Authentication.Realm,
                        Uri = context.Request.Path,
                        Method = context.Request.HttpMethod
                    };
            }
        }

        private bool CheckUserWithServerCallback(IAuthenticationCallbackInfo info)
        {
            try
            {
                return ServerConfig.GetConfig().Authentication.AuthenticationCallback(info);
            }
            catch
            {
                return false;
            }
        }

        private bool CheckConfigUserList(IAuthenticationCallbackInfo info)
        {
            User user = ServerConfig.GetConfig().Authentication.Users.Find(info.UserName);
            return user != null && info.MatchCredentials(user.Password);
        }

        protected void DenyAccess(HttpContext context)
        {
            context.Response.SendStatus(401, "Access Denied", true);
            context.Response.Write("401 Access Denied.");
            context.Response.Flush(true);
        }

        private bool AuthenticateRequest(string authorizationHeader)
        {
            if (authorizationHeader == null)
                return false;
            int separator = authorizationHeader.IndexOf(' ');
            string mode = authorizationHeader.Substring(0, separator);
            if (!StringComparer.InvariantCultureIgnoreCase.Equals(mode, ServerConfig.GetConfig().Authentication.Mode))
            {
                return false;
            }

            string credentials = authorizationHeader.Substring(separator + 1);
            return AcceptCredentials(HttpContext.Current, credentials);
        }

        public virtual void OnAuthenticateRequest(object sender, EventArgs e)
        {
            var context = (HttpContext)sender;
            string path = context.Request.Path;
            if (!AuthenticationRequired || UrlPath.RequiresAuthentication(path))
                return;

            string authorizationHeader = context.Request.Headers["HTTP_AUTHORIZATION"];
            bool hasAuthorizationHeader = !string.IsNullOrEmpty(authorizationHeader);

            if (hasAuthorizationHeader && AuthenticateRequest(authorizationHeader))
            {
                return;
            }

            if (!hasAuthorizationHeader)
            {
                m_requestCount = 0;
            }
            else if (m_lastRequestFrom == HttpContext.Current.Request.UserHostAddress)
            {
                m_requestCount++;
                HttpRuntime.WriteTrace(string.Format("{0} requests from {1}", m_requestCount, m_lastRequestFrom));
                if (m_requestCount >= 3)
                {
                    m_requestCount = 0;
                    m_lastRequestFrom = string.Empty;
                    throw new HttpException(HttpStatusCode.Unauthorized, "Unauthorized");
                }

            }
            else
            {
                m_lastRequestFrom = HttpContext.Current.Request.UserHostAddress;
            }

            OnEndRequest(HttpContext.Current, EventArgs.Empty);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public virtual void OnEndRequest(object sender, EventArgs e)
        {
            var context = (HttpContext)sender;
            if (!AuthenticationRequired)
                return;

            string realm = ServerConfig.GetConfig().Authentication.Realm;
            StringBuilder challenge = new StringBuilder(AuthenticationMethod)
                .Append(" realm=\"")
                .Append(realm)
                .Append("\"");

            switch (AuthenticationMethod)
            {
                case Digest:
                    string nonce = GetCurrentNonce();
                    challenge
                        .Append(", nonce=\"")
                        .Append(nonce)
                        .Append("\"")
                        .Append(", opaque=\"0000000000000000\"")
                        .Append(", stale=")
                        .Append("false")
                        .Append(", algorithm=MD5")
                        .Append(", qop=\"auth\"");
                    break;
            }

            context.Response.AppendHeader("WWW-Authenticate", challenge.ToString());
            DenyAccess(context);
        }

        private string GetCurrentNonce()
        {
            DateTime nonceTime = DateTime.Now.AddSeconds(NonceLifetime);
            byte[] expireBytes = Encoding.ASCII.GetBytes(nonceTime.ToString("G"));
            string nonce = Convert.ToBase64String(expireBytes);
            nonce = nonce.TrimEnd('=');

            return nonce;
        }
    }
}
