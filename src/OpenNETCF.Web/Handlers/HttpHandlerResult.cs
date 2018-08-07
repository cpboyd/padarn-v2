//
// Copyright ©2018 Christopher Boyd
//                                 

namespace OpenNETCF.Web
{
    /// <summary>
    /// HttpHandler wrapper to pass along the subpath.
    /// </summary>
    public class HttpHandlerResult : IHttpHandler
    {
        /// <summary>
        /// The HttpHandler corresponding to a specific path.
        /// </summary>
        public IHttpHandler Handler;

        /// <summary>
        /// The subpath following the initial path match.
        /// </summary>
        public string SubPath;

        /// <summary>
        /// Simple constructor for HttpHandlerResult 
        /// </summary>
        /// <param name="handler">The HttpHandler corresponding to a specific path.</param>
        /// <param name="subPath">The subpath following the initial path match.</param>
        public HttpHandlerResult(IHttpHandler handler, string subPath)
        {
            Handler = handler;
            SubPath = subPath;
        }

        #region IHttpHandler Members

        /// <summary>
        /// Enables processing of HTTP Web requests by a custom HttpHandler that 
        /// implements the <see cref="IHttpHandler"/> interface.
        /// </summary>
        /// <param name="context">An <see cref="HttpContext"/> object that provides references to the 
        /// intrinsic server objects (for example, Request, Response, Session, and Server) 
        /// used to service HTTP requests.</param>
        public void ProcessRequest(HttpContext context)
        {
            context.Request.SubPath = SubPath;
            Handler.ProcessRequest(context);
        }

        /// <summary>
        /// Gets a value indicating whether another request can use the IHttpHandler instance.
        /// </summary>
        public bool IsReusable { get { return Handler.IsReusable; } }

        #endregion
    }
}
