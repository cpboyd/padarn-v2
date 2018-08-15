//
// Copyright ©2018 Christopher Boyd
//

using System.IO;
using OpenNETCF.Web.Hosting;

namespace OpenNETCF.Web.Handlers
{
    /// <summary>
    /// Provides access to virtual files.
    /// </summary>
    internal class VirtualFileHandler : IHttpHandler
    {
        private readonly VirtualFile _file;
        private readonly string _mimeType;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="mimeType"></param>
        internal VirtualFileHandler(VirtualFile file, string mimeType)
        {
            _file = file;
            _mimeType = mimeType;
        }

        #region IHttpHandler Members

        public bool IsReusable
        {
            get { return false; }
        }

        /// <summary>
        /// Processes the incoming HTTP request
        /// </summary>
        /// <param name="context">The HttpContext for the request</param>
        public void ProcessRequest(HttpContext context)
        {
            HttpResponse response = context.Response;
            response.ContentType = _mimeType;
            WriteVirtualFile(response);
        }

        #endregion

        private void WriteVirtualFile(HttpResponse response)
        {
            using (Stream stream = _file.Open())
            {
                // Make sure all the bytes are written to the stream 
                // and move to the start
                stream.Flush();
                stream.Seek(0, SeekOrigin.Begin);

                long length = stream.Length;
                if (length > 0)
                {
                    var buffer = new byte[(int)length];
                    stream.Read(buffer, 0x0, (int)length);
                    response.BinaryWrite(buffer);
                    response.Flush();
                }
            }
        }
    }
}
