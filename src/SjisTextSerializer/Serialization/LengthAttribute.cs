using System;

namespace SjisTextSerializer.Serialization
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class LengthAttribute : Attribute
    {
        public int Length { get; }

        public LengthAttribute(int length) => Length = length;
    }
}
