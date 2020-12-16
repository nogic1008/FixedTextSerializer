using System;

namespace SjisTextSerializer.Serialization
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FixedTextAttribute : Attribute
    {
    }
}
