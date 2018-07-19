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
using System.Text;
using System.Xml;

namespace OpenNETCF.Web.Configuration
{
    public class SessionConfiguration
    {
        public const bool DefaultAllowSessionState = true;
        public const int DefaultMaxSessions = 10;
        public const int DefaultTimeout = 20;

        internal SessionConfiguration()
        {
            AllowSessionState = DefaultAllowSessionState;
            MaxSessions = DefaultMaxSessions;
            Timeout = DefaultTimeout;
        }

        internal SessionConfiguration(XmlNode node) : this()
        {
            ParseXml(node);
        }

        internal static SessionConfiguration FromXml(XmlNode sessionNode)
        {
            return new SessionConfiguration(sessionNode);
        }

        private void ParseXml(XmlNode sessionNode)
        {
            XmlAttribute attrib = sessionNode.Attributes["allowSessionState"];
            if (attrib == null)
            {
                this.AllowSessionState = DefaultAllowSessionState;
            }
            else
            {
                try
                {
                    this.AllowSessionState = bool.Parse(attrib.Value);
                }
                catch
                {
                    this.AllowSessionState = DefaultAllowSessionState;
                }
            }

            attrib = sessionNode.Attributes["max"];
            if (attrib == null)
            {
                this.MaxSessions = DefaultMaxSessions;
            }
            else
            {
                try
                {
                    this.MaxSessions = int.Parse(attrib.Value);
                }
                catch
                {
                    this.MaxSessions = DefaultMaxSessions;
                }
            }

            attrib = sessionNode.Attributes["timeout"];
            if (attrib == null)
            {
                this.Timeout = DefaultTimeout;
            }
            else
            {
                try
                {
                    this.Timeout = (int)TimeSpan.Parse(attrib.Value).TotalMinutes;
                }
                catch
                {
                    this.Timeout = DefaultTimeout;
                }
            }

            if (this.Timeout < 1)
            {
                this.Timeout = DefaultTimeout;
            }
        }

        /// <summary>
        /// Timeout in minutes
        /// </summary>
        public int Timeout { get; private set; }

        /// <summary>
        /// Specifies the maximum number of concurrent sessions.
        /// </summary>
        public int MaxSessions { get; private set; }

        /// <summary>
        /// Specifies whether session state persistence for the Padarn server is enabled. 
        /// </summary>
        public bool AllowSessionState { get; private set; }
    }
}
