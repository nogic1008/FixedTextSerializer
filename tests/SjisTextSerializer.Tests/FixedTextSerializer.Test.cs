using System;
using System.Text;
using FluentAssertions;
using SjisTextSerializer.Serialization;
using Xunit;

namespace SjisTextSerializer.Tests
{
    public class FixedTextSerializerTest
    {
        private readonly static Encoding _sjisEncoding;
        static FixedTextSerializerTest()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _sjisEncoding = Encoding.GetEncoding("Shift_JIS");
        }

        private Action Serialize<T>(T value) where T : notnull
            => () => _ = FixedTextSerializer.Serialize(value);

        [Fact]
        public void Serialize_Throws_InvalidOperationException()
        {
            object obj = new();
            Serialize(obj).Should().ThrowExactly<InvalidOperationException>();
        }

        [Theory]
        [InlineData(12345678, "aaaaaaaaaa", "bbbbbbbbbb", "12345678aaaaaaaaaabbbbbbbbbb")]
        [InlineData(1, "a", "b", "1       a         b         ")]
        [InlineData(12345678, "あああああ", "いいいいい", "12345678あああああいいいいい")]
        public void Serialize_Returns_ShiftJis_Encoded_String(int id, string name, string? remarks, string expected)
        {
            var obj = new TestClass
            {
                Id = id,
                Name = name,
                Remarks = remarks,
            };

            byte[] result = FixedTextSerializer.Serialize(obj);
            string actual = new(_sjisEncoding.GetChars(result));

            actual.Should().Be(expected);
        }

        [Fact]
        public void Serialize_Returns_ShiftJis_Encoded_String_Nested()
        {
            var obj = new TestNestedClass
            {
                Id = 12345678,
                Parent = new()
                {
                    Id = 12345678,
                    Name = "あああああ",
                }
            };

            byte[] result = FixedTextSerializer.Serialize(obj);
            string actual = new(_sjisEncoding.GetChars(result));

            actual.Should().Be("1234567812345678あああああ          ");
        }

#nullable disable warnings
        [FixedText]
        public class TestClass
        {
            [Length(8)]
            public int Id { get; set; }
            [Length(10)]
            public string Name { get; set; }
            public int Ignored { get; set; }
            [Length(10)]
            public string? Remarks { get; set; }
        }

        [FixedText]
        public class TestNestedClass
        {
            [Length(8)]
            public int Id { get; set; }
            public TestClass Parent { get; set; }
        }
    }
}
