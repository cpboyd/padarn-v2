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
using System.Text;
using OpenNETCF.Security.Principal;
using OpenNETCF.Web.Configuration;
using OpenNETCF.Web.Security.Cryptography;

namespace OpenNETCF.Web.Security
{
    /// <summary>
    /// 
    /// </summary>
    internal class DigestAuthentication : Authentication
    {
        private readonly int nonceLifetime = 60;
        private string m_user;

        /// <summary>
        /// 
        /// </summary>
        public DigestAuthentication() : base("Digest") { }

        internal override string User
        {
            get { return m_user; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="authentication"></param>
        /// <returns></returns>
        public override bool AcceptCredentials(HttpContext context, string authentication)
        {
            bool auth = false;

            ServerConfig config = ServerConfig.GetConfig();
            var digest = new DigestAuthInfo(context.Request.HttpMethod);

            string[] elements = authentication.Split(',');

            foreach (string element in elements)
            {
                int splitIndex = element.IndexOf('=');
                string K = element.Substring(0, splitIndex).Trim(new char[] { ' ', '\"' });
                string V = element.Substring(splitIndex + 1).Trim(new char[] { ' ', '\"' });
                digest.AddElement(K, V);
            }

            m_user = digest["username"];

            if (config.Authentication.AuthenticationCallback == null)
            {
                auth = CheckConfigUserList(digest);
            }
            else
            {
                auth = CheckUserWithServerCallback(digest);
            }

            // set the user info
            var id = new GenericIdentity(User, this.AuthenticationMethod.ToLowerInvariant()) { IsAuthenticated = auth };
            var principal = new GenericPrincipal(id);
            context.User = principal;

            return auth;
        }

        private bool CheckUserWithServerCallback(DigestAuthInfo digest)
        {
            try
            {
                return ServerConfig.GetConfig().Authentication.AuthenticationCallback(digest);
            }
            catch
            {
                return false;
            }
        }

        private bool CheckConfigUserList(DigestAuthInfo digest)
        {
            AuthenticationConfiguration authConfig = ServerConfig.GetConfig().Authentication;
            User user = authConfig.Users.Find(digest.UserName);

            if (user == null) return false;

            string password = user.Password;
            string hash = digest.GetHashCode(password);

            return digest["response"].Equals(hash);

            //if (authConfig.Users.Find(User) == null)
            //{
            //    // Username does not exist configuration
            //    return false;
            //}

            //string realm = authConfig.Realm;

            //// Calculate the digest hashes (taken from RFC2617)

            //// A1 = unq(username-value) ":" unq(realm-value) ":" passwd
            //string A1 = String.Format("{0}:{1}:{2}", User, realm, password);
            //// H(A1) = MD5(A1)
            //string HA1 = MD5Hash(A1);

            //// A2 = method ":" digest-uri
            //string A2 = String.Format("{0}:{1}", method, digest["uri"]);
            //// H(A2) = MD5(A2)
            //string HA2 = MD5Hash(A2);

            //// KD(secret, data) = H(concat(secret, ":", data))
            //// if qop == auth:
            //// request-digest  = <"> < KD ( H(A1),     unq(nonce-value)
            ////                              ":" nc-value
            ////                              ":" unq(cnonce-value)
            ////                              ":" unq(qop-value)
            ////                              ":" H(A2)
            ////                            ) <">
            //// if qop is not present,
            //// request-digest  = 
            ////           <"> < KD ( H(A1), unq(nonce-value) ":" H(A2) ) > <">
            //string unhashedDigest;
            //if (digest["qop"].Equals("auth"))
            //{
            //    unhashedDigest = String.Format("{0}:{1}:{2}:{3}:{4}:{5}",
            //        HA1,
            //        digest["nonce"],
            //        digest["nc"],
            //        digest["cnonce"],
            //        digest["qop"],
            //        HA2);
            //}
            //else
            //{
            //    unhashedDigest = String.Format("{0}:{1}:{2}",
            //        HA1, digest["nonce"], HA2);
            //}

            //string hashedDigest = MD5Hash(unhashedDigest);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void OnEndRequest(object sender, EventArgs e)
        {
            var context = (HttpContext)sender;
            if (!AuthenticationRequired)
                return;

            string realm = ServerConfig.GetConfig().Authentication.Realm;
            string nonce = GetCurrentNonce();

            var challenge = new StringBuilder("Digest realm=\"");
            challenge.Append(realm);
            challenge.Append("\"");
            challenge.Append(", nonce=\"");
            challenge.Append(nonce);
            challenge.Append("\"");
            challenge.Append(", opaque=\"0000000000000000\"");
            challenge.Append(", stale=");
            challenge.Append("false");
            challenge.Append(", algorithm=MD5");
            challenge.Append(", qop=\"auth\"");

            context.Response.AppendHeader("WWW-Authenticate", challenge.ToString());
        }

        private string GetCurrentNonce()
        {
            DateTime nonceTime = DateTime.Now.AddSeconds(nonceLifetime);
            byte[] expireBytes = Encoding.ASCII.GetBytes(nonceTime.ToString("G"));
            string nonce = Convert.ToBase64String(expireBytes);
            nonce = nonce.TrimEnd('=');

            return nonce;
        }
    }
}
