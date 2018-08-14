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
using System.Globalization;
using System.IO;
using System.Reflection;
using OpenNETCF.Web.Hosting;
using OpenNETCF.Web.Logging;

namespace OpenNETCF.Web
{
    /// <summary>
    /// This abstract class defines the base worker methods and enumerations used by PS managed code to process requests.
    /// </summary>
    public abstract class HttpWorkerRequest
    {
        #region Abstract methods

        /// <summary>
        /// Used by the runtime to notify the HttpWorkerRequest that request processing for the current request is complete.
        /// </summary>
        public abstract void EndOfRequest();

        /// <summary>
        /// Sends all pending response data to the client.
        /// </summary>
        /// <param name="finalFlush">true if this is the last time response data will be flushed; otherwise, false.</param>
        public abstract void FlushResponse(bool finalFlush);

        /// <summary>
        /// Read the entire request.
        /// </summary>
        internal abstract bool ReadRequest(HttpRequest request);

        /// <summary>
        /// Read the headers from the request
        /// </summary>
        protected abstract NameValueCollection ReadRequestHeaders();

        /// <summary>
        /// Process the HTTP request
        /// </summary>
        internal abstract void ExecuteRequest(out LogDataItem ldi);

        /// <summary>
        /// Translate exceptions into user-friendly error pages.
        /// </summary>
        internal abstract void HandleRequestException(Exception e, LogDataItem ldi);

        /// <summary>
        /// Process the HTTP request
        /// </summary>
        public abstract void ProcessRequest();

        /// <summary>
        /// Returns the local file path to the requested URI
        /// </summary>
        /// <returns>The local file path to the requested URI.</returns>
        public abstract string GetLocalPath();

        /// <summary>
        /// Returns the virtual path to the requested URI
        /// </summary>
        /// <returns>The path to the requested URI.</returns>
        public abstract string GetUriPath();

        /// <summary>
        /// Provides access to the specified member of the request header.
        /// </summary>
        /// <returns>The server IP address returned in the request header.</returns>
        public abstract string GetLocalAddress();

        /// <summary>
        /// Provides access to the specified member of the request header.
        /// </summary>
        /// <returns>The server port number returned in the request header.</returns>
        public abstract int GetLocalPort();

        /// <summary>
        /// Provides access to the HTTP version of the request (for example, "HTTP/1.1").
        /// </summary>
        /// <returns>The HTTP version returned in the request header.</returns>
        public abstract string GetHttpVersion();

        /// <summary>
        /// Returns the specified member of the request header.
        /// </summary>
        /// <returns>The HTTP verb returned in the request header.</returns>
        public abstract string GetHttpVerbName();

        /// <summary>
        /// Returns the query string specified in the request URL.
        /// </summary>
        /// <returns>The request query string.</returns>
        public abstract string GetQueryString();

        /// <summary>
        /// Provides access to the specified member of the request header.
        /// </summary>
        /// <returns>The client&apos;s IP address.</returns>
        public abstract string GetRemoteAddress();

        /// <summary>
        /// Adds the specified number of bytes from a byte array to the response.
        /// </summary>
        /// <param name="data">The byte array to send.</param>
        /// <param name="length">The number of bytes to send, starting at the first byte.</param>
        public abstract void SendResponseFromMemory(byte[] data, int length);

        /// <summary>
        /// Adds a standard HTTP header to the response.
        /// </summary>
        /// <param name="name">The header name. For example, Accept-Range</param>
        /// <param name="value">The value of the header.</param>
        public abstract void SendKnownResponseHeader(string name, string value);

        /// <summary>
        /// Returns a value indicating whether the client connection is still active.
        /// </summary>
        /// <returns>true if the client connection is still active; otherwise, false.</returns>
        public abstract bool IsClientConnected();

        #endregion

        #region Virtual methods

        /// <summary>
        /// Provides access to the response stream.
        /// </summary>
        /// <returns>The response stream.</returns>
        public virtual Stream ResponseStream
        {
            get { throw new HttpException("Response stream is not available."); }
        }

        internal virtual string Status
        {
            get { return string.Empty; }
            set { string s = value; } // TODO: Check this is correct
        }

        /// <summary>
        /// When overridden in a derived class, returns the name of the client computer.
        /// </summary>
        /// <returns>The name of the client computer.</returns>
        public virtual string GetRemoteName()
        {
            return GetRemoteAddress();
        }

        /// <summary>
        /// Returns the physical path to the currently executing server application.
        /// </summary>
        /// <returns>The physical path of the current application.</returns>
        internal virtual string GetAppPathTranslated()
        {
            return Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase));
        }

        /// <summary>
        /// Returns a single server variable from a dictionary of server variables associated with the request.
        /// </summary>
        /// <param name="name">The name of the requested server variable.</param>
        /// <returns>The requested server variable.</returns>
        public virtual string GetServerVariable(string name)
        {
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual string GetKnownRequestHeader(int index)
        {
            return null;
        }

        /// <summary>
        /// When overridden in a derived class, returns the name of the local server.
        /// </summary>
        /// <returns>The name of the local server.</returns>
        public virtual string GetServerName()
        {
            return GetLocalAddress();
        }

        /// <summary>
        /// Maps a virtual path to a physical path on the server.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        [Obsolete("Use HostingEnvironment.MapPath instead.", false)]
        public virtual string MapPath(string virtualPath)
        {
            return HostingEnvironment.MapPath(virtualPath);
        }

        internal virtual string GetLocalPortAsString()
        {
            return GetLocalPort().ToString(NumberFormatInfo.InvariantInfo);
        }

        /// <summary>
        /// Returns a value indicating whether the connection uses SSL.
        /// </summary>
        /// <returns>true if the connection is an SSL connection; otherwise, false. The default is false.</returns>
        public virtual bool IsSecure()
        {
            return false;
        }

        internal virtual byte[] GetQueryStringRawBytes()
        {
            return null;
        }

        /// <summary>
        /// Terminates the connection with the client.
        /// </summary>
        public virtual void CloseConnection()
        {
        }

        /// <summary>
        /// Adds a Content-Length HTTP header to the response.
        /// </summary>
        /// <param name="contentLength">The length of the response, in bytes.</param>
        public virtual void SendCalculatedContentLength(int contentLength)
        {
        }

        /// <summary>
        /// Returns a value indicating whether HTTP response headers have been sent to the client for the current request.
        /// </summary>
        /// <returns></returns>
        public virtual bool HeadersSent()
        {
            return false;
        }

        internal virtual void ClearHeaders()
        {
        }

        #endregion
    }
}
