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

namespace OpenNETCF.Web.UI.Parsers
{
    internal class Aspx
    {
        internal const string Page = "Page";
        // Page attributes:
        internal const string CodeBehind = "CodeBehind";
        internal const string Inherits = "Inherits";
        internal const string AutoEventWireup = "AutoEventWireup";
    }

    internal class AspxInfo
    {
        public AspxInfo()
        {
            // Padarn defaults to true for backward compatibility
            // IIS, I believe, defaults to false
            AutoEventWireup = true;
        }

        public AspxInfo(string tag)
            : this()
        {
            Parse(tag);
        }

        public string CodeBehindTypeName { get; set; }
        public string CodeBehindAssemblyName { get; set; }
        public bool AutoEventWireup { get; set; }

        internal void Parse(string tag)
        {
            string[] tokens = tag.Split(' ');

            for (int i = 1; i < tokens.Length; i++)
            {
                int index = tokens[i].IndexOf('=');
                if (index < 0)
                {
                    continue;
                }

                // We have a name-value pair
                string name = tokens[i].Substring(0, index);
                string value = tokens[i].Substring(index + 2).Trim('"');


                switch (name)
                {
                    case Aspx.Inherits:
                        CodeBehindTypeName = value;
                        break;
                    case Aspx.CodeBehind:
                        CodeBehindAssemblyName = value;
                        break;
                    case Aspx.AutoEventWireup:
                        try
                        {
                            AutoEventWireup = bool.Parse(value);
                        }
                        catch
                        {
                            AutoEventWireup = true;
                        }
                        break;
                }
            }
        }
    }
}
