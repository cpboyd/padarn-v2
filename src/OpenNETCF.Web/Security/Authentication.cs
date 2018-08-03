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
using OpenNETCF.Web.Configuration;

namespace OpenNETCF.Web.Security
{
    internal abstract class Authentication : IHttpModule
    {
        private readonly string authMethod;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authenticationMethod"></param>
        public Authentication(string authenticationMethod)
        {
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
            get { return (string.Compare(AuthenticationMethod, ServerConfig.GetConfig().Authentication.Mode, true) == 0); }
        }

        internal abstract string User { get; }

        public virtual void Dispose()
        {
        }

        public void Init(HttpContext context)
        {
        }

        protected void DenyAccess(HttpContext context)
        {
            context.Response.SendStatus(401, "Access Denied", true);
            context.Response.Write("401 Access Denied.");
        }

        protected string Authorization(HttpContext context, string authenticationMethod)
        {
            string requestedAuthMethod = context.Request.Headers["Authorization"];
            if ((requestedAuthMethod == null) || (authenticationMethod.Length == 0) ||
                !requestedAuthMethod.StartsWith(authenticationMethod, StringComparison.InvariantCultureIgnoreCase))
                return null; // Anonymous request 

            return requestedAuthMethod.Substring(authenticationMethod.Length + 1);
        }

        public virtual void OnAuthenticateRequest(object sender, EventArgs e)
        {
            if (!AuthenticationRequired)
                return;
            var context = (HttpContext)sender;

            string authData = Authorization(context, AuthenticationMethod);
            if (String.IsNullOrEmpty(authData) || !AcceptCredentials(context, authData))
            {
                DenyAccess(context);
                return;
            }
        }

        public abstract void OnEndRequest(object sender, EventArgs e);

        public abstract bool AcceptCredentials(HttpContext context, string authentication);
    }
}
