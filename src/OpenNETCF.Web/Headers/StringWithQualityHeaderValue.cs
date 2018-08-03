//
// Copyright ©2018 Christopher Boyd
//

using System;

namespace OpenNETCF.Web.Headers
{
    /// <summary>
    /// Represents a string header value with an optional quality.
    /// </summary>
    public class StringWithQualityHeaderValue : ICloneable
    {
        private readonly double _quality;
        private readonly string _value;

        public StringWithQualityHeaderValue(string value) : this(value, 1) { }

        public StringWithQualityHeaderValue(string value, double quality)
        {
            _value = value;
            _quality = quality;
        }

        public string Value
        {
            get { return _value; }
        }

        public double Quality
        {
            get { return _quality; }
        }

        public object Clone()
        {
            return new StringWithQualityHeaderValue(Value, Quality);
        }

        public override string ToString()
        {
            return Value + ";q=" + Quality;
        }

        public static StringWithQualityHeaderValue Parse(string input)
        {
            string[] array = input.Split(';');
            string value = array[0].Trim();
            for (int i = 1; i < array.Length; i++)
            {
                int index = array[i].IndexOf('=');
                if (index < 1)
                {
                    continue;
                }

                switch (array[i].Substring(0, index).Trim().ToLowerInvariant())
                {
                    case "q":
                        double quality = 1;
                        // Suppress any parsing errors and assume default quality.
                        try
                        {
                            quality = Double.Parse(array[i].Substring(index + 1));
                        }
                        catch { }
                        return new StringWithQualityHeaderValue(value, quality);
                }
            }

            return new StringWithQualityHeaderValue(value);
        }
    }
}
