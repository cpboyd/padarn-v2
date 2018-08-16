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
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using OpenNETCF.Web.Configuration;
using OpenNETCF.Web.Handlers;
using OpenNETCF.Web.Helpers;
using OpenNETCF.Web.Logging;
using OpenNETCF.Web.Server;
using OpenNETCF.WindowsCE;

namespace OpenNETCF.Web.Hosting
{
    /// <summary>
    /// Default handler for ASP.NET page requests for the Web Server
    /// </summary>
    public class AsyncWorkerRequest : HttpWorkerRequest
    {
        #region Fields

        private static Dictionary<Type, IHttpHandler> m_httpHandlerCache = new Dictionary<Type, IHttpHandler>();
        private static readonly char[] s_ColonOrNL = { ':', '\n' };
        private SocketWrapperBase m_client;
        internal NameValueCollection m_headers;
        private bool m_headersCleared;
        private bool m_headersSent;
        private HttpRawRequestContent m_httpRawRequestContent;
        private ILogProvider m_logProvider;
        private Stream m_output;
        private bool m_partialDownload = true;
        private MemoryStream m_response;
        private StringBuilder m_responseHeaders;
        private string m_serverHeader;

        #endregion // Fields

        /// <summary>
        /// Initializes an instance of <see cref="AsyncWorkerRequest"/>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="output"></param>
        /// <param name="logProvider"></param>
        internal AsyncWorkerRequest(SocketWrapperBase client, Stream output, ILogProvider logProvider)
        {
            m_logProvider = logProvider;
            m_logProvider.LogRuntimeInfo(ZoneFlags.WorkerRequest, "+AsyncWorkerRequest");

            this.m_client = client;
            m_logProvider.LogRuntimeInfo(ZoneFlags.WorkerRequest, string.Format("Creating network stream to {0}", m_client.RemoteEndPoint));
            m_output = output;

            if (m_output != null)
            {
                SetDefaultServerHeaderAndStatus();

                InitializeResponse();
            }
            else
            {
                m_logProvider.LogRuntimeInfo(ZoneFlags.WorkerRequest, "Network stream is null!");
            }
            m_logProvider.LogRuntimeInfo(ZoneFlags.WorkerRequest, "-AsyncWorkerRequest");
        }

        /// <summary>
        /// Initializes an instance of <see cref="AsyncWorkerRequest"/>
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="output"></param>
        /// <param name="logProvider"></param>
        internal AsyncWorkerRequest(Socket socket, Stream output, ILogProvider logProvider)
            : this(new HttpSocket(), output, logProvider)
        {
            ((HttpSocket)m_client).Create(socket);
        }

        /// <summary>
        /// Initializes an instance of <see cref="AsyncWorkerRequest"/>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logProvider"></param>
        internal AsyncWorkerRequest(SocketWrapperBase client, ILogProvider logProvider)
            : this(client, client.CreateNetworkStream(), logProvider)
        {
        }

        internal override string Status { get; set; }

        /// <summary>
        /// Provides access to the response stream.
        /// </summary>
        /// <returns>The response stream.</returns>
        internal override Stream ResponseStream
        {
            get { return m_response; }
        }

        /// <summary>
        /// Returns a value indicating whether the connection uses SSL.
        /// </summary>
        /// <returns>true if the connection is an SSL connection; otherwise, false.</returns>
        public override bool IsSecure()
        {
            return m_client is HttpsSocket;
        }

        /// <summary>
        /// Returns the local file path to the requested URI
        /// </summary>
        /// <returns>The local file path to the requested URI.</returns>
        public override string GetLocalPath()
        {
            string physicalPath = HostingEnvironment.MapPath(Path);

            string localFile = ((Path.EndsWith("/")) || (string.IsNullOrEmpty(System.IO.Path.GetExtension(physicalPath))))
                ? System.IO.Path.Combine(physicalPath, GetDefaultDocument(physicalPath))
                : physicalPath;

            return GetCasedFileNameFromCaselessName(localFile);
        }

        /// <summary>
        /// Returns the virtual path to the requested URI
        /// </summary>
        /// <returns>The path to the requested URI.</returns>
        public override string GetUriPath() { return Path; }

        /// <summary>
        /// Process the incoming HTTP request
        /// </summary>
        public override void ProcessRequest()
        {
            int et = Environment.TickCount;

            try
            {
                // ctacke - lock added to see if it addresses concurrency NativeException
                // but doesn't appear to fix anything
                lock (m_client)
                {
                    ProcessRequestInternal();
                }
            }
            finally
            {
#if DEBUG
                et = Environment.TickCount - et;
                if (et > 10)
                {
                    HttpRuntime.WriteTrace(string.Format("AsyncWorkerRequest::ProcessRequest took {0}ms", et));
                }
#endif
            }
        }

        /// <summary>
        /// Used by the runtime to notify the HttpWorkerRequest that request processing
        /// for the current request is complete.
        /// </summary>
        public override void EndOfRequest()
        {
            // Do the final flush to the server
            FlushResponse(true);
            CloseConnection();
        }

        /// <summary>
        /// Returns the local address of the web server
        /// </summary>
        /// <returns></returns>
        public override string GetLocalAddress()
        {
            return ((IPEndPoint)m_client.LocalEndPoint).Address.ToString();
        }

        /// <summary>
        /// Returns the local port of the web server
        /// </summary>
        /// <returns></returns>
        public override int GetLocalPort()
        {
            return ServerConfig.GetConfig().Port;
        }

        /// <summary>
        /// Return the HTTP version of the request
        /// </summary>
        /// <returns></returns>
        public override string GetHttpVersion()
        {
            return m_httpRawRequestContent.HttpVersion;
        }

        /// <summary>
        /// Returns the HTTP verb specified in the request
        /// </summary>
        /// <returns></returns>
        public override string GetHttpVerbName()
        {
            return m_httpRawRequestContent.HttpMethod;
        }

        /// <summary>
        /// Returns the remote address of the request
        /// </summary>
        /// <returns></returns>
        public override string GetRemoteAddress()
        {
            return ((IPEndPoint)m_client.RemoteEndPoint).Address.ToString();
        }

        /// <summary>
        /// Returns a value indicating whether HTTP response headers have been sent to the client for the current request.
        /// </summary>
        /// <returns>true if HTTP response headers have been sent to the client; otherwise, false.</returns>
        public override bool HeadersSent()
        {
            return m_headersSent;
        }

        internal override void ClearHeaders()
        {
            m_headers.Clear();
            m_headersCleared = true;
        }

        private void SendHeaders(HttpContext context)
        {
            if (m_headersSent)
            {
                return;
            }

            HttpResponse response = context.Response;

            // http://www.w3.org/Protocols/rfc2616/rfc2616-sec6.html
            m_responseHeaders.Insert(0, m_serverHeader);
            // status line
            m_responseHeaders.Insert(0, Status);

            // general header fields - see http://www.w3.org/Protocols/rfc2616/rfc2616-sec4.html#sec4.5
            m_responseHeaders.Append(response.Cache.GetHeaderString());

            // entity header fields - see http://www.w3.org/Protocols/rfc2616/rfc2616-sec7.html#sec7.1
            // Content-Encoding https://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.11
            if (!string.IsNullOrEmpty(response.ContentEncoding))
            {
                m_responseHeaders.AppendFormat("Content-Encoding: {0}\r\n", response.ContentEncoding);
            }

            // Content-Type https://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.17
            m_responseHeaders.AppendFormat("Content-Type: {0}\r\n", response.ContentType);

            // Cookies
            if (response.Cookies != null)
            {
                int count = response.Cookies.Count;
                for (int i = 0; i < count; i++)
                {
                    m_responseHeaders.AppendFormat(CultureInfo.CurrentCulture, "{0}\r\n", response.Cookies[i].GetSetCookieHeader(context));
                }
            }

            // append the content-length unless it was explicitly cleared
            if (!m_headersCleared && !m_responseHeaders.ToString().Contains("Content-Length:"))
            {
                int contentLength = 0;

                if ((!response.ForcedContentLength.HasValue) || (response.ForcedContentLength >= 0))
                {
                    if (response.ForcedContentLength.HasValue)
                    {
                        contentLength = (int)response.ForcedContentLength.Value;
                    }
                    else
                    {
                        contentLength += (int)m_response.Length;
                        //SendCalculatedContentLength(contentLength);
                    }

                    SendCalculatedContentLength(contentLength);
                    m_responseHeaders.Append("\r\n");
                }
            }

            // ensure the headers are terminated with \r\n\r\n
            int index = m_responseHeaders.Length - 4;

            while ((index < m_responseHeaders.Length) && (m_responseHeaders[index] != '\r')) index++;
            m_responseHeaders.Length = index;
            m_responseHeaders.Append("\r\n\r\n");

            byte[] buffer = Encoding.UTF8.GetBytes(m_responseHeaders.ToString());
            WriteToOutput(buffer);

            response.HeadersWritten = m_headersSent = true;
        }

        private void WriteToOutput(byte[] buffer)
        {
            WriteToOutput(buffer, 0, buffer.Length);
        }

        private void WriteToOutput(byte[] buffer, int offset, int count)
        {
            try
            {
                int retry = 3;
                while (retry-- > 0)
                {
                    if (m_output.CanWrite)
                        break;

                    Thread.Sleep(250);
                }
                if (retry < 0) throw new IOException("Unable to write to underlying stream.");

                m_output.Write(buffer, offset, count);
            }
            catch (IOException ioEx)
            {
                // this is seen occasionally - need to protect it
                // todo: what do we do when this occurs?  for now rethrow
                if (Marshal.GetHRForException(ioEx) == -2146232800)
                {
                    //An existing connection was forcibly closed by the remote host
                    //Unable to write data to the transport connection.
                    CloseConnection();
                    return;
                }
                throw;
            }
        }

        /// <summary>
        /// Flush the response stream to the client
        /// </summary>
        /// <param name="finalFlush"></param>
        public override void FlushResponse(bool finalFlush)
        {
            SendHeaders(HttpContext.Current);

            //if (Path.EndsWith(".aspx"))
            //{
            //    TranslateResponseASP();
            //}

            try
            {
                // coalesce output
                if (m_response.Length > 0)
                {
                    m_response.WriteTo(m_output);
                }
                m_response.SetLength(0);
                if (finalFlush)
                {
                    // TODO: determine why exactly this is here and if it needs to be deleted.
                    //       Having it causes a hang when serving some pages but it must be here for a reason, right?

                    //persistant connections http://www.w3.org/Protocols/rfc2616/rfc2616-sec8.html#sec8.1
                    //if (false) // if (KeepConnectionAlive)
                    //{
                    //    //Reset the headers sent since we are sendign new data
                    //    m_headersSent = false;

                    //    //get the content
                    //    HttpRawRequestContent content = GetPartialRawRequestContent(m_client);

                    //    if (content.Length > 0)
                    //    {
                    //        m_httpRawRequestContent = content;
                    //        SetDefaultServerHeaderAndStatus();
                    //        InitializeResponse();
                    //        ProcessRequest();
                    //    }
                    //    else
                    //    {
                    //        CloseConnection();
                    //    }
                    //}
                    //else
                    //{
                    CloseConnection();
                    //}
                }
            }
            catch (Exception e)
            {
                m_logProvider.LogPadarnError("AsyncWorkerRequest.FlushResponse: " + e.Message, null);
                CloseConnection();
            }
        }

        private void WriteResponse(IAsyncResult ar)
        {
            try
            {
                var finalFlush = (bool)ar.AsyncState;
                m_output.EndWrite(ar);
                m_output.Flush();
                m_response.SetLength(0);
                if (finalFlush)
                {
                    CloseConnection();
                }
            }
            catch
            {
            }
        }

        private void TranslateResponseASP()
        {
            const string regex = "<%=(.*?)%>";
            var input = new StringBuilder();

            input.Append(Encoding.UTF8.GetString(m_response.ToArray(), 0, (int)m_response.Length));

            string source = input.ToString();
            MatchCollection matches = Regex.Matches(source, regex, RegexOptions.IgnoreCase | RegexOptions.Multiline);

            if (matches.Count == 0) return;

            foreach (Match match in matches)
            {
                source = source.Replace(match.Value, ParseASP(match.Groups[1].Value));
            }

            m_response.SetLength(0);
            byte[] bytes = Encoding.UTF8.GetBytes(source);
            m_response.Write(bytes, 0, bytes.Length);
        }

        private string ParseASP(string asp)
        {
            asp = asp.Trim();

            switch (asp.ToLowerInvariant())
            {
                case "server.getlasterror()":
                    return LastError.Message;
            }

            return "PADARN:UNSUPPORTED";
        }

        /// <summary>
        /// Close the connection to the client.
        /// </summary>
        public override void CloseConnection()
        {
            // Dispose of the raw request content
            if (m_httpRawRequestContent != null)
            {
                m_httpRawRequestContent.Dispose();
            }

            if (m_client.Connected)
            {
                try
                {
                    m_client.Shutdown(SocketShutdown.Both);
                }
                catch { }
            }
        }

        /// <summary>
        /// Reads the HTTP headers from the request
        /// </summary>
        protected override NameValueCollection ReadRequestHeaders()
        {
            return m_headers = m_httpRawRequestContent.Headers;
        }

        /// <summary>
        /// Adds the specified number of bytes from a byte array to the response.
        /// </summary>
        /// <param name="data">The byte array to send.</param>
        /// <param name="length">The number of bytes to send, starting at the first byte.</param>
        public override void SendResponseFromMemory(byte[] data, int length)
        {
            SendResponseFromMemory(data, 0, length);
        }

        internal override void SendResponseFromMemory(byte[] data, int offset, int length)
        {
            if (length <= 0)
            {
                return;
            }

            m_response.Write(data, offset, length);
        }

        /// <summary>
        /// Adds the contents of the specified file to the response and specifies the starting position in the file and the number of bytes to send.
        /// </summary>
        /// <param name="filename">The name of the file to send.</param>
        /// <param name="offset">The starting position in the file.</param>
        /// <param name="length">The number of bytes to send.</param>
        public override void SendResponseFromFile(string filename, long offset, long length)
        {
            int et = Environment.TickCount;

            // TODO: allow this to be configurable
            const int maxBytesToRead = 0x40000;

            // Flush headers to HTTP output stream
            FlushResponse(false);

            long totalBytesRead = 0;
            using (FileStream fs = File.OpenRead(filename))
            {
                long fileSize = fs.Length;
                // If length is -1, transmit the remainder of the file:
                if (length == -1)
                {
                    length = fileSize - offset;
                }

                // check for a broken connection (i.e. a cancelled download)
                //if (!IsClientConnected())
                //{
                //    return;
                //}

                // Don't allocate a larger array than necessary:
                var bytesToRead = (int)Math.Min(maxBytesToRead, length);
                var content = new byte[bytesToRead];

                fs.Seek(offset, SeekOrigin.Begin);

                // packetize output to prevent OOMs on serving large files
                while (totalBytesRead < length)
                {
                    int bytesRead = fs.Read(content, 0, bytesToRead);
                    // Stop if we didn't read any bytes:
                    if (bytesRead < 1)
                        break;
                    totalBytesRead += bytesRead;

                    // Write directly to the HTTP output stream
                    WriteToOutput(content, 0, bytesRead);
                }
            }

            // if we sent more than 1MB, clean up
            if (totalBytesRead > 0x100000)
            {
                GC.Collect();
            }
#if DEBUG
            et = Environment.TickCount - et;
            if (et > 10)
            {
                HttpRuntime.WriteTrace(string.Format("AsyncWorkerRequest::SendResponseFromFile took {0}ms: {1}", et, filename));
            }
#endif
        }

        /// <summary>
        /// Calculates the length of the response and then writes to the response
        /// </summary>
        /// <param name="contentLength"></param>
        public override void SendCalculatedContentLength(int contentLength)
        {
            m_responseHeaders.AppendFormat(CultureInfo.CurrentCulture, "Content-Length: {0}\r\n", contentLength);
            //SendKnownResponseHeader("Content-Length", contentLength.ToString());
        }

        /// <summary>
        /// Sends a well-known HTTP header to the response
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void SendKnownResponseHeader(string name, string value)
        {
            m_responseHeaders.AppendFormat(CultureInfo.CurrentCulture, "{0}: {1}\r\n", name, value);
        }

        internal override byte[] GetQueryStringRawBytes()
        {
            return (m_httpRawRequestContent.RawQueryString == null)
                ? new byte[0]
                : Encoding.UTF8.GetBytes(m_httpRawRequestContent.RawQueryString);
        }

        /// <summary>
        /// Returns the query string from the request
        /// </summary>
        /// <returns></returns>
        public override string GetQueryString()
        {
            return m_httpRawRequestContent.RawQueryString;
        }

        /// <summary>
        /// Returns a value indicating whether the client connection is still active.
        /// </summary>
        /// <returns>true if the client connection is still active; otherwise, false.</returns>
        public override bool IsClientConnected()
        {
            return (m_client != null) && m_client.Connected;
        }

        #region Private Methods

        private static string m_versionString;
        private static Dictionary<string, Type> m_handlerTypeCache = new Dictionary<string, Type>();

        /// <summary>
        /// Determins if the connection should be kept alive
        /// </summary>
        private bool KeepConnectionAlive
        {
            get
            {
                return StringComparer.OrdinalIgnoreCase
                    .Equals(m_httpRawRequestContent.Headers["HTTP_CONNECTION"], "keep-alive");
            }
        }

        private Exception LastError { get; set; }

        private bool HasAuthorizationHeader
        {
            get { return !string.IsNullOrEmpty(m_headers["HTTP_AUTHORIZATION"]); }
        }

        internal string Path
        {
            get { return m_httpRawRequestContent.Path; }
            set { m_httpRawRequestContent.Path = value; }
        }

        internal HttpRawRequestContent HttpRawRequestContent
        {
            get { return m_httpRawRequestContent; }
        }

        /// <summary>
        /// initializes the resposen.  Called from ctor and before closing the connection to see if keep alive is available
        /// </summary>
        internal void InitializeResponse()
        {
            m_response = new MemoryStream();
            m_responseHeaders = new StringBuilder();
        }

        /// <summary>
        /// Sets the default headers.  Called from ctor and before closing the connection to see if keep alive is available
        /// </summary>
        private void SetDefaultServerHeaderAndStatus()
        {
            if (m_versionString == null)
            {
                m_versionString = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            }

            m_serverHeader = string.Format("Server: Padarn Web Server v{0}\r\n", m_versionString);
            Status = "HTTP/1.1 200 OK\r\n";
        }

        private IHttpHandler GetHandlerForFilename(string fileName, string mimeType, HttpMethodFlags method)
        {
            // Load the correct file handler
            IHttpHandler handler = GetCustomHandler(fileName, method);

            // TODO: ** check for custom HttpHandlers **
            if (handler == null && !fileName.StartsWith("about:"))
            {
                handler = new StaticFileHandler(fileName, mimeType);
            }

            return handler;
        }

        private Type CheckType(string typeName, ServerConfig config)
        {
            Type t;

            lock (m_handlerTypeCache)
            {
                if (m_handlerTypeCache.ContainsKey(typeName))
                {
                    t = m_handlerTypeCache[typeName];
                }
                else
                {
                    t = Type.GetType(typeName);

                    if (t == null)
                    {
                        t = config.GetType(typeName);

                        if (t == null)
                        {
                            throw new HttpException(HttpStatusCode.InternalServerError,
                                string.Format("Unable To load type '{0}'", typeName));
                        }
                    }

                    m_handlerTypeCache.Add(typeName, t);
                }
            }
            return t;
        }

        private IHttpHandler GetCustomHandler(string fileName, HttpMethodFlags method)
        {
            IHttpHandler handler = null;
            string subPath = null;

            ServerConfig config = ServerConfig.GetConfig();
            foreach (HttpHandler h in config.HttpHandlers.Where(h => (h.Verb & method) == method))
            {
                Match match = Regex.Match(fileName, h.Path, RegexOptions.IgnoreCase);
                if (!match.Success)
                {
                    continue;
                }

                subPath = fileName.Substring(match.Index + match.Length);
                Type t = CheckType(h.TypeName, config);
                try
                {
                    lock (m_httpHandlerCache)
                    {
                        if (m_httpHandlerCache.ContainsKey(t))
                        {
                            handler = m_httpHandlerCache[t];
                        }
                        else
                        {
                            handler = (IHttpHandler)Activator.CreateInstance(t);

                            if (handler.IsReusable)
                            {
                                m_httpHandlerCache.Add(t, handler);
                            }
                        }
                    }
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);

                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        Console.WriteLine(ex);
                    }

                    throw new HttpException(HttpStatusCode.InternalServerError,
                        string.Format("Unable to create '{0}' handler for '{1}' method: {2}",
                            t.Name,
                            method.GetVerbName(),
                            ex.ToString()));
                }
            }

            return (handler == null) ? null : new HttpHandlerResult(handler, subPath);
        }

        private bool FileNameIsInPath(string fileName, string path)
        {
            return (path == "*") || Regex.IsMatch(fileName, path, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Returns the physical path to the currently executing server application.
        /// </summary>
        /// <returns>The physical path of the current application.</returns>
        internal override string GetAppPathTranslated()
        {
            // TODO: cache these
            ServerConfig config = ServerConfig.GetConfig();

            string virtualRoot = (config.VirtualDirectories == null)
                ? string.Empty
                : config.VirtualDirectories.FindPhysicalDirectoryForVirtualUrlPath(Path);

            return string.IsNullOrEmpty(virtualRoot)
                ? System.IO.Path.GetFullPath(ServerConfig.GetConfig().DocumentRoot)
                : virtualRoot;
        }

        private void LogPageAccess(LogDataItem ldi)
        {
            if (m_logProvider == null)
                return;

            try
            {
                m_logProvider.LogPageAccess(ldi);
            }
            catch (Exception ex)
            {
                // swallow logging exceptions to prevent a bad logging plug-in from tearing us down
                // TODO: maybe log these errors somewhere?
                LogError("Exception trying to log page access: " + ex.Message, ldi);
            }
        }

        private HttpCachePolicy CheckGlobalCachePolicy(string fileName)
        {
            string fileExtension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();

            return (
                from profile in ServerConfig.GetConfig().Caching.Profiles
                where profile.Extension == fileExtension
                select new HttpCachePolicy(profile)
                ).FirstOrDefault();
        }

        internal override void HandleRequestException(Exception e, LogDataItem ldi)
        {
            LastError = e;

            // TODO: Convert this to use an ErrorFormatter
            LogError("Exception handling HTTP Request: " + e.Message, ldi);

            string exceptionPage = StringHelper.ConvertVerbatimLineEndings(Resources.ErrorPage);

            m_response.SetLength(0);
            Status = StringHelper.ConvertVerbatimLineEndings(Resources.HttpOK);

            if (e is HttpException)
            {
                var err = e as HttpException;

                // see if we have a custom error page:
                HttpStatusCode statusCode = err.StatusCode;
                exceptionPage = GetCustomErrorPage(statusCode);

                switch (statusCode)
                {
                    case HttpStatusCode.NotFound:
                        Status = StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusFileNotFound);
                        if (exceptionPage == null)
                        {
                            exceptionPage = string.Format(
                                StringHelper.ConvertVerbatimLineEndings(Resources.ContextualErrorTemplate),
                                Resources.FileNotFoundTitle, e.Message, Resources.FileNotFoundDesc);
                        }
                        break;
                    case HttpStatusCode.Unauthorized:
                        Status = StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusUnauthorized);
                        if (exceptionPage == null)
                        {
                            exceptionPage = string.Format(
                                StringHelper.ConvertVerbatimLineEndings(Resources.ContextualErrorTemplate),
                                Resources.UnauthorizedTitle, Resources.UnuthorizedMessage, Resources.UnauthorizedDesc);
                        }
                        break;
                    default:
                        if (e.InnerException == null)
                        {
                            // pass along the error
                            Status = GetStatusForErrorCode(statusCode);
                            if (exceptionPage == null)
                            {
                                exceptionPage = string.Format(Resources.ErrorPage, Path,
                                    string.Format(Resources.UnhandledExceptionDesc, Path),
                                    string.Format("{0}: {1}", e.GetType().FullName, e.Message),
                                    ParseStackTrace(e.StackTrace));
                            }
                            break;
                        }

                        if (e.InnerException.Message == Resources.Max_request_length_exceeded)
                        {
                            Status = StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusInternalServerError);
                            if (exceptionPage == null)
                            {
                                exceptionPage = string.Format(
                                    StringHelper.ConvertVerbatimLineEndings(Resources.ContextualErrorTemplate),
                                    Resources.MaxRequestLengthErrorTitle, e.Message,
                                    Resources.MaxRequestLengthErrorDesc);
                            }
                        }
                        else if (e.InnerException.Message == Resources.DiskError)
                        {
                            Status = StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusInternalServerError);
                            if (exceptionPage == null)
                            {
                                exceptionPage = string.Format(
                                    StringHelper.ConvertVerbatimLineEndings(Resources.ContextualErrorTemplate),
                                    Resources.DiskErrorTitle, e.Message, Resources.DiskErrorDesc);
                            }
                        }
                        else if (e.InnerException is OutOfMemoryException)
                        {
                            Status = StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusInternalServerError);
                            if (exceptionPage == null)
                            {
                                exceptionPage = string.Format(
                                    StringHelper.ConvertVerbatimLineEndings(Resources.ContextualErrorTemplate),
                                    Resources.OutOfMemoryErrorTitle, e.Message, Resources.OutOfMemoryErrorDesc);
                            }
                        }
                        break;
                }
            }
            else if (e is FileNotFoundException)
            {
                Status = StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusFileNotFound);
                exceptionPage = string.Format(StringHelper.ConvertVerbatimLineEndings(Resources.ContextualErrorTemplate),
                    Resources.CodeBehindNotFoundTitle, e.Message, Resources.CodeBehindNotFoundDesc);
            }
            else if (e is TypeLoadException)
            {
                Status = StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusInternalServerError);
                exceptionPage = string.Format(StringHelper.ConvertVerbatimLineEndings(Resources.ContextualErrorTemplate),
                    Resources.TypeLoadTitle, e.Message, Resources.TypeLoadDesc);
            }
            else // Unhandled Exception
            {
                Status = StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusInternalServerError);
                // see if we have a custom error page:
                exceptionPage = GetCustomErrorPage(HttpStatusCode.InternalServerError) ?? string.Format(
                    Resources.ErrorPage, Path, string.Format(Resources.UnhandledExceptionDesc, Path),
                    string.Format("{0}: {1}", e.GetType().FullName, e.Message), ParseStackTrace(e.StackTrace)
                    );
            }

            byte[] b = Encoding.UTF8.GetBytes(exceptionPage);

            m_responseHeaders.AppendFormat(CultureInfo.CurrentCulture, StringHelper.ConvertVerbatimLineEndings(Resources.HeaderFormat), Resources.HeaderContentType, "text/html");
            m_responseHeaders.AppendFormat(CultureInfo.CurrentCulture, StringHelper.ConvertVerbatimLineEndings(Resources.HeaderFormat), Resources.HeaderContentLength, b.Length);

            SendResponseFromMemory(b, b.Length);

            // let custom errors be translated
            TranslateResponseASP();

            FlushResponse(true);
        }

        /// <summary>
        /// Process the incoming HTTP request
        /// </summary>
        internal bool PreprocessRequest()
        {
            // Get the request binary contents
            try
            {
                if (m_client.Connected)
                {
                    m_httpRawRequestContent = GetPartialRawRequestContent(m_client);
                    if (m_client.Connected && m_httpRawRequestContent.Length == 0 && m_client.Available > 0)
                    {
                        // try again since we should not have a 0 length on the request
                        int retries = 5;
                        while (retries-- > 0 && m_client.Connected)
                        {
                            m_httpRawRequestContent = GetPartialRawRequestContent(m_client);
                            if (m_httpRawRequestContent.Length > 0)
                            {
                                break;
                            }
                            HttpRuntime.WriteTrace("! AsyncWorkerRequest::ProcessRequest timeout getting partial content");
                            Thread.Sleep(100);
                        }
                    }
                }
            }
            catch (SocketException e)
            {
                if (e.ErrorCode == 10054)
                {
                    // An existing connection was forcibly closed by the remote host
                    return false;
                }
            }

            // TODO; ctacke 2/4/10 - need to vet this isVirtualPath
            bool isVirtualPath = UrlPath.IsVirtualDirectory(m_httpRawRequestContent.Path);

            // if the raw content is null or we have no data just close the connection
            if (m_httpRawRequestContent == null
                || m_httpRawRequestContent.Length == 0
                || m_httpRawRequestContent.Path == null)
            {
                return false;
            }

            if ((!m_httpRawRequestContent.Path.EndsWith("/")) && (m_httpRawRequestContent.Path.LastIndexOf('.') == -1) && (isVirtualPath))
            {
                //first check to see if we have a forward slash
                //this is needed because if a url is hit for example http://site/virtualDir without the slash the header for subsequent
                //requests for images, css etc will return GET HTTP/1.1 /images/image.png instead of GET HTTP/1.1 /virtualDir/images/image.png
                //if a slash is added at the end of the url there is no issue
                //Even after implementing keep alive this is the only workaround i can see at this point
                //The following describes persistent connections http://www.w3.org/Protocols/rfc2616/rfc2616-sec8.html#sec8 and is implemented in
                //FlushResponse()
                HttpContext.Current.Response.Redirect(m_httpRawRequestContent.Path + "/");
                return false;
            }

            return true;
        }

        internal override bool ReadRequest(HttpRequest request)
        {
            try
            {
                // Get the content info (determines if it's a POST or GET
                if (!PreprocessRequest() || !GetContentInfo())
                {
                    return false;
                }

                // Get the headers
                request.Headers = ReadRequestHeaders();

                // set the URI
                request.Url = new Uri(UrlPath.Combine(
                    m_client.LocalEndPoint.ToString(), m_httpRawRequestContent.Path,
                    m_httpRawRequestContent.RawQueryString, IsSecure()));

                // Read the content data
                if (request.ContentLength > 0)
                {
                    request.RawPostContent = GetEntireRawContent();
                }
            }
            catch (IOException e)
            {
                // we had a stream issue - most likely we're out of space trying to write the temp file
                throw new HttpException(Resources.DiskError, e);
            }
            catch (OutOfMemoryException oom)
            {
                throw new HttpException(Resources.OutOfMemoryException, oom);
            }
            catch (ObjectDisposedException)
            {
                // if the underlying socket has been disposed
                Debug.WriteLine("Socket Disposed - aborting request");
                return false;
            }
            catch (Exception e)
            {
                throw new HttpException(Resources.HttpErrorParsingHeader, e);
            }

            return true;
        }

        internal override IHttpHandler MapRequestHandler(out LogDataItem ldi)
        {
            ldi = null;
            HttpMethodFlags method = HttpMethod.ParseFlag(HttpContext.Current.Request.HttpMethod);

            HttpRuntime.WriteTrace(string.Format("{0}: {1}", method, Path));

            // TODO: Refactor to check handlers for path & file together.
            // do we have a custom HttpHandler handler for the path?
            IHttpHandler customHandler = GetCustomHandler(Path, method) ??
                                         // check for virtual file
                                         GetVirtualFileHandler(Path);
            if (customHandler != null)
            {
                return customHandler;
            }

            string localFile, mime, defaultDoc = null;

            if (!Path.ToLowerInvariant().StartsWith("about:"))
            {
                localFile = GetLocalPath();
                mime = MimeMapping.GetMimeMapping(localFile);

                if (!File.Exists(localFile))
                {
                    string name = UrlPath.FixVirtualPathSlashes(System.IO.Path.Combine(Path, defaultDoc ?? string.Empty));
                    throw new HttpException(HttpStatusCode.NotFound, string.Format("The file '{0}' cannot be found.", name));
                }

                // validate the requested file is *beneath* the server root (no navigating above the root)
                if (!IsSubDirectoryOf(localFile, ServerConfig.GetConfig().DocumentRoot))
                {
                    throw new HttpException(HttpStatusCode.NotFound, "Not found");
                }
            }
            else
            {
                localFile = Path.Substring(1);
                mime = "text/html";
            }

            ldi = new LogDataItem(m_headers, localFile, m_client.RemoteEndPoint.ToString(),
                ServerConfig.GetConfig());

            IHttpHandler handler = GetHandlerForFilename(localFile, mime, method);

            LogPageAccess(ldi);

            HttpCachePolicy globalPolicy = CheckGlobalCachePolicy(localFile);
            if (globalPolicy != null) HttpContext.Current.Response.Cache = globalPolicy;

            // Now pass the request processing onto the relevant handler
            if (handler == null)
            {
                throw new HttpException(Resources.NoHttpHandler);
            }

            return handler;
        }

        private VirtualFileHandler GetVirtualFileHandler(string requestPath)
        {
            if (HostingEnvironment.VirtualPathProvider == null ||
                !HostingEnvironment.VirtualPathProvider.FileExists(requestPath))
            {
                return null;
            }

            VirtualFile vf = HostingEnvironment.VirtualPathProvider.GetFile(requestPath);
            if (vf == null)
            {
                throw new HttpException(HttpStatusCode.NotFound, Resources.HttpFileNotFound);
            }

            return new VirtualFileHandler(vf, MimeMapping.GetMimeMapping(requestPath));
        }

        internal void ProcessRequestInternal()
        {
            LogDataItem ldi = null;
            IHttpHandler handler = null;
            try
            {
                if (!ReadRequest(HttpContext.Current.Request))
                {
                    CloseConnection();
                    return;
                }

                handler = MapRequestHandler(out ldi);
                handler.ProcessRequest(HttpContext.Current);

                EndOfRequest();
            }
            catch (Exception e)
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    Console.WriteLine("Request exception: " + e.Message);
                }

                HandleRequestException(e, ldi);
                return;
            }
            finally
            {
                //Get rid of the uploaded content if available.  This will delete any temp files
                HttpRequest request = HttpContext.Current.Request;
                if (request.RawPostContent != null)
                {
                    request.RawPostContent.Dispose();
                    request.RawPostContent = null;
                }

                // If the HttpHandler implements IDisposable, let's try to dispose of it.
                var disposableHandler = handler as IDisposable;
                if (disposableHandler != null)
                {
                    disposableHandler.Dispose();
                }
            }
        }

        private bool IsSubDirectoryOf(string candidate, string other)
        {
            bool isChild = false;
            try
            {
                var candidateInfo = new DirectoryInfo(candidate);
                var otherInfo = new DirectoryInfo(other);

                while (candidateInfo.Parent != null)
                {
                    if (candidateInfo.Parent.FullName == otherInfo.FullName)
                    {
                        isChild = true;
                        break;
                    }

                    candidateInfo = candidateInfo.Parent;
                }
            }
            catch (Exception error)
            {
                string message = string.Format("Unable to check directories {0} and {1}: {2}", candidate, other, error);
                Trace.WriteLine(message);
            }

            return isChild;
        }

        public static string GetCasedFileNameFromCaselessName(string caselessName)
        {
            // skip on Windows (since the case is not relevant)
#if WindowsCE
            return caselessName;
#else
    // remove any relative pathing
            caselessName = System.IO.Path.GetFullPath(caselessName);

            string searchFile = System.IO.Path.GetFileName(caselessName);
            string searchPath = System.IO.Path.GetDirectoryName(caselessName);

            // crop any drive name
            string driveName;

            int index = searchPath.IndexOf(':');
            if (index > 0)
            {
                // the colon plus the first backslash
                driveName = caselessName.Substring(0, index + 2);
                searchPath = searchPath.Substring(index + 1);
            }
            else
            {
                // there is no drive (i.e. Linux)
                driveName = "/";
            }

            // get the directory list - being platform-agnostic (works on linux or windows)
            IEnumerable<string> directoryList = searchPath
                .Split(System.IO.Path.DirectorySeparatorChar)
                .Where(s => !string.IsNullOrEmpty(s));

            // put in the drive name if the OS supports it
            string buildPath = driveName;

            // build up a case-sensitive path
            foreach (string dir in directoryList)
            {
                string dir1 = dir;
                string casedDirectory = Directory.GetDirectories(buildPath)
                    .FirstOrDefault(d => StringComparer.InvariantCultureIgnoreCase.Equals(System.IO.Path.GetFileName(d), dir1));

                if (casedDirectory == null)
                {
                    return null;
                }

                buildPath = casedDirectory;
            }

            string casedFile = Directory.GetFiles(buildPath)
                .FirstOrDefault(f => StringComparer.InvariantCultureIgnoreCase.Equals(System.IO.Path.GetFileName(f), searchFile));

            return casedFile;
#endif
        }

        private string GetCustomErrorPage(HttpStatusCode errorCode)
        {
            var extensions = new[] { "htm", "html", "aspx" };

            string folder = ServerConfig.GetConfig().CustomErrorFolder;
            if ((folder == null) || !Directory.Exists(folder))
            {
                return null;
            }

            string path = System.IO.Path.Combine(folder, string.Format("{0}.", (int)errorCode));

            string returnPage = null;
            foreach (string checkPath in extensions.Select(ext => path + ext).Where(File.Exists))
            {
                try
                {
                    using (TextReader reader = File.OpenText(checkPath))
                    {
                        returnPage = reader.ReadToEnd();
                        break;
                    }
                }
                catch
                {
                    returnPage = null;
                }
            }

            return returnPage;
        }

        private string GetStatusForErrorCode(HttpStatusCode error)
        {
            switch (error)
            {
                case HttpStatusCode.BadRequest: // 400
                    return StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusBadRequest);
                case HttpStatusCode.Unauthorized: // 401
                    return StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusUnauthorized);
                case HttpStatusCode.PaymentRequired: // 402
                    return StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusPaymentRequired);
                case HttpStatusCode.Forbidden: // 403
                    return StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusForbidden);
                case HttpStatusCode.NotFound: // 404
                    return StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusFileNotFound);
                case HttpStatusCode.MethodNotAllowed: // 405
                    return StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusNotAllowed);
                case HttpStatusCode.NotAcceptable: // 406
                    return StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusNotAcceptable);
                case HttpStatusCode.ProxyAuthenticationRequired: // 407
                    return StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusProxyAuthRequired);
                case HttpStatusCode.RequestTimeout: // 408
                    return StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusRequestTimeout);
                case HttpStatusCode.Conflict: // 409
                    return StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusConflict);
                case HttpStatusCode.Gone: // 410
                    return StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusGone);
                case HttpStatusCode.LengthRequired: // 411
                    return StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusLengthRequired);
                case HttpStatusCode.PreconditionFailed: // 412
                    return StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusPreconditionFailed);
                case HttpStatusCode.RequestEntityTooLarge: // 413
                    return StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusRequestEntityTooLarge);
                case HttpStatusCode.RequestUriTooLong: // 414
                    return StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusRequestURITooLong);
                case HttpStatusCode.UnsupportedMediaType: // 415
                    return StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusUnsupportedMediaType);
                case HttpStatusCode.RequestedRangeNotSatisfiable: // 416
                    return StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusRequestedRangeNotSatisfiable);
                case HttpStatusCode.ExpectationFailed: // 417
                    return StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusExpectationFailed);

                case HttpStatusCode.InternalServerError: // 500
                    return StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusInternalServerError);
                default:
                    return string.Format(StringHelper.ConvertVerbatimLineEndings(Resources.HttpStatusGeneric), ((int)error).ToString());
            }
        }

        private string ParseStackTrace(string stackTrace)
        {
            var builder = new StringBuilder();

            IEnumerable<string> calls = stackTrace
                .Split('\n')
                .Select(call => call.Trim('\r'));
            foreach (string line in calls)
            {
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0}<br/>", line);

                if (line.EndsWith("Page_Load()"))
                {
                    break;
                }
            }

            return builder.ToString();
        }

        private void LogError(string errorInfo, LogDataItem ldi)
        {
            if (m_logProvider == null)
            {
                return;
            }

            try
            {
                m_logProvider.LogPadarnError(errorInfo, ldi);
            }
            catch
            {
                // swallow logging exceptions to prevent a bad logging plug-in from tearing us down
                // TODO: maybe log these errors somewhere?
            }
        }

        private static string GetDefaultDocument(string physicalPath)
        {
            string defaultDocument = "default.html";

            ServerConfig conf = ServerConfig.GetConfig();
            if (conf == null)
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    Console.WriteLine("No server config");
                }
            }
            else if (conf.DefaultDocuments == null)
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    Console.WriteLine("No default documents");
                }
            }
            else
            {
                foreach (string document in conf.DefaultDocuments)
                {
                    string localFile = GetCasedFileNameFromCaselessName(System.IO.Path.Combine(physicalPath, document));
                    if (localFile != null)
                    {
                        defaultDocument = document;
                        break;
                    }
                }
            }
            return defaultDocument;
        }

        /// <summary>
        /// Gets the remaing request content.  Primarly used for posted data
        /// </summary>
        /// <returns></returns>
        private HttpRawRequestContent GetEntireRawContent()
        {
            int length = HttpContext.Current.Request.ContentLength;
            HttpRuntime.WriteTrace(string.Format("Content-Length: {0}\nMax Request Length: {1}", length, HttpRuntimeConfig.GetConfig().MaxRequestLengthBytes));
            //check to max sure we have not exceeded the maxlength
            if (length > HttpRuntimeConfig.GetConfig().MaxRequestLengthBytes)
            {
                throw new HttpException(HttpStatusCode.InternalServerError, Resources.Max_request_length_exceeded);
            }
            //See if we only downloaded partial data
            if (m_partialDownload)
            {
                HttpRuntime.WriteTrace("Partial data download.");

                int totalLength = Int32.Parse(m_httpRawRequestContent.Headers["HTTP_CONTENT_LENGTH"]) + m_httpRawRequestContent.LengthOfHeaders;
                //Create a new raw content to download the data
                m_httpRawRequestContent = new HttpRawRequestContent(HttpRuntimeConfig.GetConfig().RequestLengthDiskThresholdBytes,
                    3024,
                    ((IPEndPoint)m_client.RemoteEndPoint).Address,
                    m_httpRawRequestContent);

                //Temp buffer
                byte[] buffer;

                //See if the browser wants a response code of 100 to continue sending posted data
                if (m_headers["HTTP_EXPECT"] != null && m_headers["HTTP_EXPECT"] == "100" && HttpContext.Current.Request.ContentType.StartsWith("multipart/form-data"))
                {
                    HttpRuntime.WriteTrace("Request contains multi-part form data");

                    //Tell the browser to continue sending data if required.  This is needed when uploading bigger files.
                    //HTTP/1.1 100 Continue
                    //TODO do we have to send the server headers for example?
                    /*
                       HTTP/1.1 100 Continue
                       Server: Microsoft-IIS/4.0
                       Date: Mon, 15 Apr 2002 00:49:27 GMT

                     */
                    buffer = Encoding.ASCII.GetBytes(Resources.HttpContinue);
                    WriteToOutput(buffer);
                    m_output.Flush();
                }

                // Loop until we get all the content downloaded
                int totalReceived = 0;
                buffer = new byte[10240];
                while (m_httpRawRequestContent.Length < totalLength)
                {
                    if (!m_client.Connected) break;

                    if (m_client.Available > 0)
                    {
                        int received = m_client.Receive(buffer);
                        m_httpRawRequestContent.AddBytes(buffer, 0, received);
                        totalReceived += received;
                        HttpRuntime.WriteTrace(string.Format("Bytes received: {0}", totalReceived));
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                m_httpRawRequestContent.DoneAddingBytes();
                m_partialDownload = false;
            }

            return m_httpRawRequestContent;
        }

        /// <summary>
        /// Retreives the entire Http request and stores it in a HttpRawRequestContent
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private HttpRawRequestContent GetPartialRawRequestContent(SocketWrapperBase client)
        {
            var rawContent = new HttpRawRequestContent(
                HttpRuntimeConfig.GetConfig().RequestLengthDiskThresholdBytes,
                3072/*Use 3k of memory then go to file if we go out of this threshold*/,
                ((IPEndPoint)client.RemoteEndPoint).Address);

            //TODO: I don't like this magic number, needs to get cleaned up
            // was fixed in 12259, but may have introduced other bugs (reverted in 13136)
            var buffer = new byte[10240];

            int received = client.Receive(buffer);

            if (received > 0)
            {
                rawContent.AddBytes(buffer, 0, received);
                rawContent.DoneAddingBytes();

                if (rawContent.HttpMethod == HttpMethod.Post && rawContent.Headers["HTTP_CONTENT_LENGTH"] == null)
                {
                    //it's a post but the content length has not been downloaded yet.
                    int retryCount = 5;
                    rawContent = DownloadUntilContentLengthHeader(rawContent, client, ref retryCount);
                }
                //Check for the content length header to see if there is more data to download
                m_partialDownload = rawContent.Headers["HTTP_CONTENT_LENGTH"] != null;
            }
            else
            {
                //set the rawcontent as done
                rawContent.DoneAddingBytes();
            }

            return rawContent;
        }

        private HttpRawRequestContent DownloadUntilContentLengthHeader(HttpRawRequestContent content, SocketWrapperBase client, ref int retryCount)
        {
            HttpRawRequestContent newContent = null;
            if (retryCount < 0)
            {
                newContent = new HttpRawRequestContent(HttpRuntimeConfig.GetConfig().RequestLengthDiskThresholdBytes,
                    3072/*Use 3k of memory then go to file if we go out of this threshold*/,
                    ((IPEndPoint)client.RemoteEndPoint).Address);
                newContent.DoneAddingBytes();
            }
            else
            {
                //Some browsers like taking their time to send the post data so just wait a bit
                Thread.Sleep(50);
                if (content.Headers["HTTP_CONTENT_LENGTH"] == null)
                {
                    newContent = new HttpRawRequestContent(HttpRuntimeConfig.GetConfig().RequestLengthDiskThresholdBytes,
                        3072/*Use 3k of memory then go to file if we go out of this threshold*/,
                        ((IPEndPoint)client.RemoteEndPoint).Address);
                    byte[] buffer = content.GetAsByteArray();
                    newContent.AddBytes(buffer, 0, buffer.Length);
                    content.Dispose();
                    if (client.Available > 0)
                    {
                        buffer = new byte[10240];
                        int received = client.Receive(buffer);
                        if (received != -1)
                        {
                            newContent.AddBytes(buffer, 0, received);
                            newContent.DoneAddingBytes();
                            if (newContent.Headers["HTTP_CONTENT_LENGTH"] == null)
                            {
                                --retryCount;
                                newContent = DownloadUntilContentLengthHeader(newContent, client, ref retryCount);
                            }
                        }
                    }
                    else
                    {
                        newContent.DoneAddingBytes();
                        --retryCount;
                        newContent = DownloadUntilContentLengthHeader(newContent, client, ref retryCount);
                    }
                }
            }

            return newContent;
        }

        private bool GetContentInfo()
        {
            string requestLine = m_httpRawRequestContent.ReadContentInfo();
            if (requestLine == null)
                throw new InvalidOperationException(string.Format(Resources.UnsupportedMethod, ""));
            return m_httpRawRequestContent.HttpMethod != null &&
                   m_httpRawRequestContent.HttpVersion != null &&
                   m_httpRawRequestContent.Path != null;

        }

        #endregion // Private Methods
    }
}
