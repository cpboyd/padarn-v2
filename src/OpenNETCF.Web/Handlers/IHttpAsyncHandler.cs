//
// Copyright ©2018 Christopher Boyd
//

using System;

namespace OpenNETCF.Web
{
    /// <summary>
    /// Defines the contract that ASP.NET implements to synchronously process HTTP Web 
    /// requests using custom HTTP handlers.
    /// </summary>
    public interface IHttpAsyncHandler: IHttpHandler
    {
        /// <summary>
        /// Initiates an asynchronous call to the HTTP handler.
        /// </summary>
        /// <param name="context">An <see cref="HttpContext"/> object that provides references to the 
        /// intrinsic server objects (for example, Request, Response, Session, and Server) 
        /// used to service HTTP requests.</param>
        /// <param name="cb">The <see cref="T:System.AsyncCallback"/> to call when the asynchronous method call is complete.
        /// If cb is null, the delegate is not called.</param>
        /// <param name="extraData">Any extra data needed to process the request.</param>
        /// <returns>An <see cref="T:System.IAsyncResult"/> that contains information about the status of the process.</returns>
        IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData);

        /// <summary>
        /// Provides an asynchronous process End method when the process ends.
        /// </summary>
        /// <remarks>You can use EndProcessRequest to raise any exceptions caught during the asynchronous process.</remarks>
        /// <param name="result">An <see cref="T:System.IAsyncResult"/> that contains information about the status of the process.</param>
        void EndProcessRequest(IAsyncResult result);
    }
}
