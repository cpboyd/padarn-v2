//
// Copyright ©2018 Christopher Boyd
//

using System;
using System.ComponentModel;
using System.Linq;
using OpenNETCF.Web.Logging;

namespace OpenNETCF.Web
{
    /// <summary>
    /// Defines the methods, properties, and events that are common to all application objects in an ASP.NET application.
    /// </summary>
    public class HttpApplication : DefaultHttpHandler, IComponent
    {
        private EventHandlerList _events;

        /// <summary>
        /// Occurs as the first event in the HTTP pipeline chain of execution when ASP.NET responds to a request.
        /// </summary>
        public event EventHandler BeginRequest
        {
            add { Events.AddHandler(Event.BeginRequest, value); }
            remove { Events.RemoveHandler(Event.BeginRequest, value); }
        }

        /// <summary>
        /// Occurs when a security module has established the identity of the user.
        /// </summary>
        public event EventHandler AuthenticateRequest
        {
            add { Events.AddHandler(Event.AuthenticateRequest, value); }
            remove { Events.RemoveHandler(Event.AuthenticateRequest, value); }
        }

        /// <summary>
        /// Occurs when a security module has established the identity of the user.
        /// </summary>
        public event EventHandler PostAuthenticateRequest
        {
            add { Events.AddHandler(Event.PostAuthenticateRequest, value); }
            remove { Events.RemoveHandler(Event.PostAuthenticateRequest, value); }
        }

        /// <summary>
        /// Occurs when a security module has verified user authorization.
        /// </summary>
        public event EventHandler AuthorizeRequest
        {
            add { Events.AddHandler(Event.AuthorizeRequest, value); }
            remove { Events.RemoveHandler(Event.AuthorizeRequest, value); }
        }

        /// <summary>
        /// Occurs when the user for the current request has been authorized.
        /// </summary>
        public event EventHandler PostAuthorizeRequest
        {
            add { Events.AddHandler(Event.PostAuthorizeRequest, value); }
            remove { Events.RemoveHandler(Event.PostAuthorizeRequest, value); }
        }

        /// <summary>
        /// Occurs just before ASP.NET starts executing an event handler (for example, a page or an XML Web service).
        /// </summary>
        public event EventHandler PreRequestHandlerExecute
        {
            add { Events.AddHandler(Event.PreRequestHandlerExecute, value); }
            remove { Events.RemoveHandler(Event.PreRequestHandlerExecute, value); }
        }

        /// <summary>
        /// Occurs when the ASP.NET event handler (for example, a page or an XML Web service) finishes execution.
        /// </summary>
        public event EventHandler PostRequestHandlerExecute
        {
            add { Events.AddHandler(Event.PostRequestHandlerExecute, value); }
            remove { Events.RemoveHandler(Event.PostRequestHandlerExecute, value); }
        }

        /// <summary>
        /// Occurs just before ASP.NET performs any logging for the current request.
        /// </summary>
        public event EventHandler LogRequest
        {
            add { Events.AddHandler(Event.LogRequest, value); }
            remove { Events.RemoveHandler(Event.LogRequest, value); }
        }

        /// <summary>
        /// Occurs when ASP.NET has completed processing all the event handlers for the <see cref="LogRequest"/> event.
        /// </summary>
        public event EventHandler PostLogRequest
        {
            add { Events.AddHandler(Event.PostLogRequest, value); }
            remove { Events.RemoveHandler(Event.PostLogRequest, value); }
        }

        /// <summary>
        /// Occurs as the last event in the HTTP pipeline chain of execution when ASP.NET responds to a request.
        /// </summary>
        public event EventHandler EndRequest
        {
            add { Events.AddHandler(Event.EndRequest, value); }
            remove { Events.RemoveHandler(Event.EndRequest, value); }
        }

        /// <summary>
        /// Occurs when an unhandled exception is thrown.
        /// </summary>
        public event EventHandler Error
        {
            add { Events.AddHandler(Event.Error, value); }
            remove { Events.RemoveHandler(Event.Error, value); }
        }

        /// <summary>
        /// Gets the list of event handler delegates that process all application events.
        /// </summary>
        protected EventHandlerList Events
        {
            get
            {
                if (_events == null)
                {
                    _events = new EventHandlerList();
                }
                return _events;
            }
        }

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
            if (_events != null)
            {
                try
                {
                    HandleEvent(Event.Disposed);
                }
                finally
                {
                    _events.Dispose();
                }
            }
        }

        /// <summary>
        /// Gets or sets a site interface for an <see cref="T:System.ComponentModel.IComponent,"/> implementation.
        /// </summary>
        public ISite Site { get; set; }

        /// <summary>
        /// Occurs when the application is disposed.
        /// </summary>
        public event EventHandler Disposed
        {
            add { Events.AddHandler(Event.Disposed, value); }
            remove { Events.RemoveHandler(Event.Disposed, value); }
        }

        #endregion

        /// <summary>
        /// Process the HTTP request
        /// </summary>
        /// <param name="context">The current context of the request</param>
        public override void ProcessRequest(HttpContext context)
        {
            int et = Environment.TickCount;

            HandleRequest(context);
#if DEBUG
            et = Environment.TickCount - et;
            if (et > 10)
            {
                HttpRuntime.WriteTrace(string.Format("HttpApplication::ProcessRequest took {0}ms", et));
            }
#endif
        }

        private void HandleRequest(HttpContext context)
        {
            LogDataItem ldi = null;
            var wr = context.WorkerRequest;
            HttpRequest request = context.Request;
            try
            {
                if (!wr.ReadRequest(request))
                {
                    wr.CloseConnection();
                    return;
                }

                HandleEvent(Event.BeginRequest);

                // Authentication events:
                HandleEvent(Event.AuthenticateRequest);
                HandleEvent(Event.PostAuthenticateRequest);
                HandleEvent(Event.AuthorizeRequest);
                HandleEvent(Event.PostAuthorizeRequest);

                // ResolveRequestCache
                // PostResolveRequestCache

                // MapRequestHandler
                // PostMapRequestHandler

                // AcquireRequestState
                // create the session now (we needed the headers for the cookies)
                //HttpContext.Current.InitializeSession();
                // PostAcquireRequestState

                HandleEvent(Event.PreRequestHandlerExecute);
                wr.ExecuteRequest(out ldi);
                HandleEvent(Event.PostRequestHandlerExecute);

                // ReleaseRequestState
                // PostReleaseRequestState

                // UpdateRequestCache
                // PostUpdateRequestCache

                HandleEvent(Event.LogRequest);
                HandleEvent(Event.PostLogRequest);

                HandleEvent(Event.EndRequest);
            }
            catch (Exception e)
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    Console.WriteLine("Request exception: " + e.Message);
                }

                wr.HandleRequestException(e, ldi);
                return;
            }
            finally
            {
                //Get rid of the uploaded content if available.  This will delete any temp files
                if (request.RawPostContent != null)
                {
                    request.RawPostContent.Dispose();
                    request.RawPostContent = null;
                }
            }
        }

        private void HandleEvent(object type)
        {
            var eventHandler = Events[type] as EventHandler;

            if (eventHandler != null)
            {
                Delegate[] handlers = eventHandler.GetInvocationList();

                foreach (EventHandler handler in handlers.OfType<EventHandler>())
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }

        private static class Event
        {
            internal static readonly object Disposed = new object();
            internal static readonly object Error = new object();
            internal static readonly object BeginRequest = new object();
            internal static readonly object AuthenticateRequest = new object();
            internal static readonly object PostAuthenticateRequest = new object();
            internal static readonly object AuthorizeRequest = new object();
            internal static readonly object PostAuthorizeRequest = new object();
            internal static readonly object PreRequestHandlerExecute = new object();
            internal static readonly object PostRequestHandlerExecute = new object();
            internal static readonly object LogRequest = new object();
            internal static readonly object PostLogRequest = new object();
            internal static readonly object EndRequest = new object();
        }
    }
}
