//
// Copyright ©2018 Christopher Boyd
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.Web.Headers
{
    /// <summary>
    /// Represents a string header value with an optional quality.
    /// </summary>
    public class StringWithQualityHeaderValue: ICloneable
    {
        private readonly string _value;
        private readonly double _quality;

        public string Value
        {
            get { return _value; }
        }

        public double Quality
        {
            get { return _quality; }
        }

        public StringWithQualityHeaderValue(string value) : this(value, 1) {}

        public StringWithQualityHeaderValue(string value, double quality)
        {
            _value = value;
            _quality = quality;
        }

        public override string ToString()
        {
            return Value + ";q=" + Quality;
        }

        public object Clone()
        {
            return new StringWithQualityHeaderValue(Value, Quality);
        }

        public static StringWithQualityHeaderValue Parse(string input)
        {
            string[] array = input.Split(';');
            string value = array[0].Trim();
            for (int i = 1; i < array.Length; i++)
            {
                string[] toParse = array[i].Split('=');
                switch (toParse[0].Trim().ToLower())
                {
                    case "q":
                        double quality = 1;
                        // Suppress any parsing errors and assume default quality.
                        try
                        {
                            quality = Double.Parse(toParse[1]);
                        }
                        catch { }
                        return new StringWithQualityHeaderValue(value, quality);
                }
            }

            return new StringWithQualityHeaderValue(value);
        }
    }
}
