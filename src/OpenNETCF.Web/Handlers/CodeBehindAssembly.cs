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
using System.IO;
using System.Linq;
using System.Reflection;

namespace OpenNETCF.Web
{
    /// <summary>
    /// Helper class for loading a code-behind assembly into the Web Server
    /// </summary>
    public sealed class CodeBehindAssembly
    {
        private Assembly asm;

        private CodeBehindAssembly(Assembly asm)
        {
            this.asm = asm;
        }

        /// <summary>
        /// Loads a code-behind assembly from the specified path.
        /// </summary>
        /// <param name="assemblyPath">The path to load the assembly from.</param>
        /// <returns>An instance of CodeBehindAssembly</returns>
        public static CodeBehindAssembly LoadFrom(string assemblyPath)
        {
            try
            {
                return new CodeBehindAssembly(Assembly.LoadFrom(assemblyPath));
            }
            catch (IOException ioe)
            {
                if (ioe.Message.EndsWith("was not found."))
                {
                    throw new FileNotFoundException(ioe.Message);
                }
            }

            return null;
        }

        /// <summary>
        /// Returns an array of Types with a particular base type.
        /// </summary>
        /// <param name="baseType">The base type used to filter the array</param>
        /// <returns>An array of types of type baseType</returns>
        public Type[] GetTypesFromBaseType(Type baseType)
        {
            try
            {
                Type[] types = asm.GetTypes();

                return (types == null) ? null
                    : types.Where(t => t != null && t.IsSubclassOf(baseType)).ToArray();
            }
            catch (TypeLoadException)
            {
                return null;
            }
        }
    }
}
