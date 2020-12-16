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
        private readonly static Encoding _sjisEncoding;
        static FixedTextSerializer()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _sjisEncoding = Encoding.GetEncoding("Shift_JIS");
        }

        public static byte[] Serialize<T>(T value) where T : notnull
        {
            var type = value.GetType();
            if (type.GetCustomAttribute<FixedTextAttribute>() is null)
                throw new InvalidOperationException($"{type.FullName} does not have {nameof(FixedTextAttribute)}.");

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

                byte[] propertyBytes = _sjisEncoding.GetBytes(property.GetValue(value)?.ToString() ?? "");
                if (len.Length < propertyBytes.Length)
                    throw new FormatException($"Property \"{property.Name}\" should be up to {len.Length} byte length, but has {propertyBytes} byte length.");

                bytes.AddRange(propertyBytes);
                if (len.Length > propertyBytes.Length)
                    bytes.AddRange(Enumerable.Repeat((byte)' ', len.Length - propertyBytes.Length));
            }
            return bytes.ToArray();
        }
    }
}
