using System;
using System.Buffers;
using System.IO;
using System.Text;
using FluentAssertions;
using Xunit;

namespace FixedTextSerializer.Tests
{
    public class FixedTextWriterTest
    {
        private readonly static Encoding _sjisEncoding;
        static FixedTextWriterTest()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _sjisEncoding = Encoding.GetEncoding("Shift_JIS");
        }

        #region Constractor
        private Action Constractor(IBufferWriter<byte>? bufferWriter)
            => () => _ = new FixedTextWriter(bufferWriter!);
        private Action Constractor(Stream? stream)
            => () => _ = new FixedTextWriter(stream!);

        [Fact]
        public void Constractor_Throws_ArgumentNullException_When_BufferWriter_Is_Null()
            => Constractor((IBufferWriter<byte>?)null)
                .Should().ThrowExactly<ArgumentNullException>()
                .WithMessage("*bufferWriter*");

        [Fact]
        public void Constractor_Throws_ArgumentNullException_When_Stream_Is_Null()
            => Constractor((Stream?)null)
                .Should().ThrowExactly<ArgumentNullException>()
                .WithMessage("*stream*");

        [Fact]
        public void Constractor_Throws_ArgumentException_When_Stream_Is_ReadOnly()
            => Constractor(new MemoryStream(Array.Empty<byte>(), writable: false))
                .Should().ThrowExactly<ArgumentException>()
                .WithMessage("stream is not writable.*");
        #endregion

        [Theory]
        [InlineData("")]
        [InlineData("foo")]
        [InlineData("ソソソソソ")]
        public void WriteRawByte_WritesTo_Buffer(string source)
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            var options = new FixedTextOptions()
            {
                CodePage = _sjisEncoding.CodePage
            };
            var writer = new FixedTextWriter(buffer, options);

            // Act
            writer.WriteString(source);
            writer.Flush();

            // Assert
            byte[] writtenBytes = buffer.WrittenSpan.ToArray();
            string result = new(_sjisEncoding.GetChars(writtenBytes));
            result.Should().Be(source);
        }

        [Fact]
        public void WriteRawBytes_WritesTo_Stream()
        {
            // Arrange
            var stream = new MemoryStream();
            var writer = new FixedTextWriter(stream);
            byte[] source = new byte[] { 0x43, 0x41, 0x46, 0x45 }; // CAFE

            // Act
            writer.WriteRawBytes(source);
            writer.Flush();

            // Assert
            byte[] result = stream.ToArray();
            result.Should().BeEquivalentTo(source);
        }

        [Fact]
        public void WriteRawBytes_WritesTo_Buffer()
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new FixedTextWriter(buffer);
            byte[] source = new byte[] { 0x43, 0x41, 0x46, 0x45 }; // CAFE

            // Act
            writer.WriteRawBytes(source);
            writer.Flush();

            // Assert
            byte[] result = buffer.WrittenSpan.ToArray();
            result.Should().BeEquivalentTo(source);
        }

        [Theory]
        [InlineData("")]
        [InlineData("foo")]
        [InlineData("ソソソソソ")]
        public void WriteString_Writes_SameString(string source)
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            var options = new FixedTextOptions()
            {
                CodePage = _sjisEncoding.CodePage
            };
            var writer = new FixedTextWriter(buffer, options);

            // Act
            writer.WriteString(source);
            writer.Flush();

            // Assert
            byte[] writtenBytes = buffer.WrittenSpan.ToArray();
            string result = new(_sjisEncoding.GetChars(writtenBytes));
            result.Should().Be(source);
        }

        [Theory]
        [InlineData(1, "0")]
        [InlineData(10, "0000000000")]
        public void WriteZero_Writes_Specified_Zero(int count, string expected)
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            var options = new FixedTextOptions()
            {
                CodePage = _sjisEncoding.CodePage
            };
            var writer = new FixedTextWriter(buffer, options);

            // Act
            writer.WriteZero(count);
            writer.Flush();

            // Assert
            byte[] writtenBytes = buffer.WrittenSpan.ToArray();
            string result = new(_sjisEncoding.GetChars(writtenBytes));
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(1, " ")]
        [InlineData(10, "          ")]
        public void WriteWhitespace_Writes_Specified_Whitespace(int count, string expected)
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            var options = new FixedTextOptions()
            {
                CodePage = _sjisEncoding.CodePage
            };
            var writer = new FixedTextWriter(buffer, options);

            // Act
            writer.WriteWhitespace(count);
            writer.Flush();

            // Assert
            byte[] writtenBytes = buffer.WrittenSpan.ToArray();
            string result = new(_sjisEncoding.GetChars(writtenBytes));
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(NewLine.Cr, "\r")]
        [InlineData(NewLine.Lf, "\n")]
        [InlineData(NewLine.CrLf, "\r\n")]
        public void WriteNewLine_Writes_Specified_NewLine(NewLine newLine, string expected)
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            var options = new FixedTextOptions()
            {
                CodePage = _sjisEncoding.CodePage,
                NewLine = newLine
            };
            var writer = new FixedTextWriter(buffer, options);

            // Act
            writer.WriteNewLine();
            writer.Flush();

            // Assert
            byte[] writtenBytes = buffer.WrittenSpan.ToArray();
            string result = new(_sjisEncoding.GetChars(writtenBytes));
            result.Should().Be(expected ?? Environment.NewLine);
        }

        [Fact]
        public void WriteNewLine_Writes_Environment_NewLine()
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            var options = new FixedTextOptions()
            {
                CodePage = _sjisEncoding.CodePage,
                NewLine = NewLine.Auto
            };
            var writer = new FixedTextWriter(buffer, options);

            // Act
            writer.WriteNewLine();
            writer.Flush();

            // Assert
            byte[] writtenBytes = buffer.WrittenSpan.ToArray();
            string result = new(_sjisEncoding.GetChars(writtenBytes));
            result.Should().Be(Environment.NewLine);
        }
    }
}
