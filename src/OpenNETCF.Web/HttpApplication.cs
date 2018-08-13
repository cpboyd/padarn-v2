//
// Copyright ©2018 Christopher Boyd
//

using System;
using System.ComponentModel;

namespace OpenNETCF.Web
{
    /// <summary>
    /// Defines the methods, properties, and events that are common to all application objects in an ASP.NET application.
    /// </summary>
    public sealed class HttpApplication : DefaultHttpHandler, IComponent
    {
        /// <summary>
        /// Gets HTTP-specific information about the current request.
        /// </summary>
        public HttpContext Context
        {
            get { return HttpContext.Current; }
        }

        /// <summary>
        /// Gets the intrinsic request object for the current request.
        /// </summary>
        public HttpRequest Request
        {
            get { return Context.Request; }
        }

        /// <summary>
        /// Gets the intrinsic response object for the current request.
        /// </summary>
        public HttpResponse Response
        {
            get { return Context.Response; }
        }

        #region IComponent Members

        /// <summary>
        /// Disposes the HttpApplication instance.
        /// </summary>
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets or sets a site interface for an <see cref="T:System.ComponentModel.IComponent,"/> implementation.
        /// </summary>
        public ISite Site { get; set; }
        /// <summary>
        /// Occurs when the application is disposed.
        /// </summary>
        public event EventHandler Disposed;

        #endregion
    }
}
