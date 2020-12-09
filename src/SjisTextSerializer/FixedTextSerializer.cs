using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SjisTextSerializer.Serialization;

namespace SjisTextSerializer
{
    public static class FixedTextSerializer
    {
        private readonly static Encoding SjisEncoding;
        static FixedTextSerializer()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            SjisEncoding = Encoding.GetEncoding("Shift_JIS");
        }

        public static byte[] Serialize<T>(T value) where T: notnull
        {
            var type = value.GetType();
            if (type.GetCustomAttribute<FixedTextAttribute>() is null)
                throw new InvalidOperationException();

            var bytes = new List<byte>();
            foreach (var property in type.GetProperties())
            {
                if (property.PropertyType.GetCustomAttribute<FixedTextAttribute>() is not null)
                {
                    bytes.AddRange(Serialize(property.GetValue(value)));
                    continue;
                }

                var len = property.GetCustomAttribute<LengthAttribute>();
                if (len is null)
                    continue;

                byte[] propertyBytes = SjisEncoding.GetBytes(property.GetValue(value)?.ToString() ?? "");
                if (len.Length < propertyBytes.Length)
                    throw new FormatException();

                bytes.AddRange(propertyBytes);
                if (len.Length > propertyBytes.Length)
                    bytes.AddRange(Enumerable.Repeat((byte)' ', len.Length - propertyBytes.Length));
            }
            return bytes.ToArray();
        }
    }
}
