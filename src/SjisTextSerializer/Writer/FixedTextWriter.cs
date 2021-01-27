using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SjisTextSerializer
{
    public class FixedTextWriter : IAsyncDisposable, IDisposable
    {
        private const int StackallocThreshold = 256;

        private const int DefaultGrowthSize = 4096;
        private const int InitialGrowthSize = 256;
        private IBufferWriter<byte>? _output;
        private Stream? _stream;
        private FixedTextOptions _options;

        private ArrayBufferWriter<byte>? _arrayBufferWriter;
        private Memory<byte> _memory;

        public FixedTextOptions Options => _options;

        /// <summary>
        /// Returns the amount of bytes written by the <see cref="FixedTextWriter"/> so far
        /// that have not yet been flushed to the output and committed.
        /// </summary>
        public int BytesPending { get; private set; }

        /// <summary>
        /// Returns the amount of bytes committed to the output by the <see cref="FixedTextWriter"/> so far.
        /// </summary>
        /// <remarks>
        /// In the case of IBufferwriter, this is how much the IBufferWriter has advanced.
        /// In the case of Stream, this is how much data has been written to the stream.
        /// </remarks>
        public long BytesCommitted { get; private set; }

        public FixedTextWriter(IBufferWriter<byte> bufferWriter, FixedTextOptions options = default)
        {
            _output = bufferWriter ?? throw new ArgumentNullException(nameof(bufferWriter));
            _options = options;
        }

        public FixedTextWriter(Stream stream, FixedTextOptions options = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite)
                throw new ArgumentException(nameof(stream) + " is not writable.", nameof(stream));

            _stream = stream;
            _options = options;
            _arrayBufferWriter = new();
        }

        #region Flush
        /// <summary>
        /// Commits the fixed text written so far which makes it visible to the output destination.
        /// </summary>
        /// <remarks>
        /// In the case of IBufferWriter, this advances the underlying <see cref="IBufferWriter{byte}" /> based on what has been written so far.
        /// In the case of Stream, this writes the data to the stream and flushes it.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        ///   The instance of <see cref="FixedTextWriter"/> has been disposed.
        /// </exception>
        public void Flush()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(FixedTextWriter));

            _memory = default;
            if (_stream is not null)
            {
                Debug.Assert(_arrayBufferWriter is not null);
                if (BytesPending != 0)
                {
                    _arrayBufferWriter.Advance(BytesPending);
                    BytesPending = 0;

                    _stream.Write(_arrayBufferWriter.WrittenSpan);

                    BytesCommitted += _arrayBufferWriter.WrittenCount;
                    _arrayBufferWriter.Clear();
                }
                _stream.Flush();
            }
            else
            {
                Debug.Assert(_output is not null);
                if (BytesPending != 0)
                {
                    _output.Advance(BytesPending);
                    BytesCommitted += BytesPending;
                    BytesPending = 0;
                }
            }
        }

        /// <summary>
        /// Asynchronously commits the fixed text written so far which makes it visible to the output destination.
        /// </summary>
        /// <remarks>
        /// In the case of IBufferWriter, this advances the underlying <see cref="IBufferWriter{byte}" /> based on what has been written so far.
        /// In the case of Stream, this writes the data to the stream and flushes it asynchronously, while monitoring cancellation requests.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        ///   The instance of <see cref="FixedTextWriter"/> has been disposed.
        /// </exception>
        public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(FixedTextWriter));

            _memory = default;
            if (_stream is not null)
            {
                Debug.Assert(_arrayBufferWriter is not null);
                if (BytesPending != 0)
                {
                    _arrayBufferWriter.Advance(BytesPending);
                    BytesPending = 0;

                    await _stream.WriteAsync(_arrayBufferWriter.WrittenMemory, cancellationToken).ConfigureAwait(false);

                    BytesCommitted += _arrayBufferWriter.WrittenCount;
                    _arrayBufferWriter.Clear();
                }
                await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Debug.Assert(_output is not null);
                if (BytesPending != 0)
                {
                    _output.Advance(BytesPending);
                    BytesCommitted += BytesPending;
                    BytesPending = 0;
                }
            }
        }
        #endregion

        #region Reset
        /// <summary>
        /// Resets the <see cref="FixedTextWriter"/> internal state so that it can be re-used.
        /// </summary>
        /// <remarks>
        /// The <see cref="FixedTextWriter"/> will continue to use the original writer options
        /// and the original output as the destination (either <see cref="IBufferWriter{byte}" /> or <see cref="Stream" />).
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        ///   The instance of <see cref="FixedTextWriter"/> has been disposed.
        /// </exception>
        public void Reset()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(FixedTextWriter));

            _arrayBufferWriter?.Clear();

            BytesPending = default;
            BytesCommitted = default;
            _memory = default;
        }

        /// <summary>
        /// Resets the <see cref="FixedTextWriter"/> internal state so that it can be re-used with the new instance of <see cref="Stream" />.
        /// </summary>
        /// <param name="stream">An instance of <see cref="Stream" /> used as a destination for writing JSON text into.</param>
        /// <remarks>
        /// The <see cref="FixedTextWriter"/> will continue to use the original writer options
        /// but now write to the passed in <see cref="Stream" /> as the new destination.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the instance of <see cref="Stream" /> that is passed in is null.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The instance of <see cref="FixedTextWriter"/> has been disposed.
        /// </exception>
        public void Reset(Stream stream)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(FixedTextWriter));

            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite)
                throw new ArgumentException(nameof(stream) + " is not writable.", nameof(stream));

            _stream = stream;
            if (_arrayBufferWriter is null)
                _arrayBufferWriter = new();
            else
                _arrayBufferWriter.Clear();
            _output = null;

            BytesPending = default;
            BytesCommitted = default;
            _memory = default;
        }

        /// <summary>
        /// Resets the <see cref="FixedTextWriter"/> internal state so that it can be re-used with the new instance of <see cref="IBufferWriter{Byte}" />.
        /// </summary>
        /// <param name="bufferWriter">An instance of <see cref="IBufferWriter{Byte}" /> used as a destination for writing JSON text into.</param>
        /// <remarks>
        /// The <see cref="FixedTextWriter"/> will continue to use the original writer options
        /// but now write to the passed in <see cref="IBufferWriter{Byte}" /> as the new destination.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the instance of <see cref="IBufferWriter{Byte}" /> that is passed in is null.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The instance of <see cref="FixedTextWriter"/> has been disposed.
        /// </exception>
        public void Reset(IBufferWriter<byte> bufferWriter)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(FixedTextWriter));

            _output = bufferWriter ?? throw new ArgumentNullException(nameof(bufferWriter));
            _stream = null;
            _arrayBufferWriter = null;

            BytesPending = default;
            BytesCommitted = default;
            _memory = default;
        }
        #endregion

        #region Dispose
        private bool IsDisposed => _stream is null && _output is null;

        /// <summary>
        /// Commits any left over fixed text that has not yet been flushed and releases all resources used by the current instance.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     In the case of IBufferWriter, this advances the underlying <see cref="IBufferWriter{Byte}" /> based on what has been written so far.
        ///     In the case of Stream, this writes the data to the stream and flushes it.
        ///   </para>
        ///   <para>
        ///     The <see cref="FixedTextWriter"/> instance cannot be re-used after disposing.
        ///   </para>
        /// </remarks>
        public void Dispose()
        {
            if (IsDisposed)
                return;

            Flush();

            BytesPending = default;
            BytesCommitted = default;
            _memory = default;
            _stream = null;
            _arrayBufferWriter = null;
            _output = null;
        }

        /// <summary>
        /// Asynchronously commits any left over fixed text that has not yet been flushed and releases all resources used by the current instance.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     In the case of IBufferWriter, this advances the underlying <see cref="IBufferWriter{Byte}" /> based on what has been written so far.
        ///     In the case of Stream, this writes the data to the stream and flushes it.
        ///   </para>
        ///   <para>
        ///     The <see cref="FixedTextWriter"/> instance cannot be re-used after disposing.
        ///   </para>
        /// </remarks>
        public async ValueTask DisposeAsync()
        {
            if (IsDisposed)
                return;

            await FlushAsync().ConfigureAwait(false);

            BytesPending = default;
            BytesCommitted = default;
            _memory = default;
            _stream = null;
            _arrayBufferWriter = null;
            _output = null;
        }
        #endregion

        public void WriteNewLine()
        {
            if (_memory.Length - BytesPending < 2)
                Grow(2);

            var output = _memory.Span;

            switch (_options.NewLine)
            {
                case NewLine.Lf:
                    output[BytesPending++] = (byte)'\n';
                    return;
                case NewLine.CrLf:
                    output[BytesPending++] = (byte)'\r';
                    output[BytesPending++] = (byte)'\n';
                    return;
                case NewLine.Cr:
                    output[BytesPending++] = (byte)'\r';
                    return;
                case NewLine.Auto:
                    foreach (byte c in Environment.NewLine)
                        output[BytesPending++] = c;
                    return;
            }
        }

        public void WriteWhitespace(int count = 1)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));
            Fill((byte)' ', count);
        }

        public void WriteZero(int count = 1)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));
            Fill((byte)'0', count);
        }

        public void WriteRawBytes(ReadOnlySpan<byte> encodedText)
        {
            if (_memory.Length - BytesPending < encodedText.Length)
                Grow(encodedText.Length);

            var output = _memory.Span[BytesPending..];
            encodedText.CopyTo(output);
            BytesPending += encodedText.Length;
        }

        public void WriteString(ReadOnlySpan<char> utf16String)
        {
            var encoding = Encoding.GetEncoding(_options.CodePage);
            int maxRequired = encoding.GetMaxByteCount(utf16String.Length);

            if (_memory.Length - BytesPending < maxRequired)
                Grow(maxRequired);

            var output = _memory.Span[BytesPending..];
            BytesPending += encoding.GetBytes(utf16String, output);
        }

        private void Grow(int requiredSize)
        {
            Debug.Assert(requiredSize > 0);

            if (_memory.Length == 0)
            {
                FirstCallToGetMemory(requiredSize);
                return;
            }

            int sizeHint = Math.Max(DefaultGrowthSize, requiredSize);

            Debug.Assert(BytesPending != 0);

            if (_stream is not null)
            {
                Debug.Assert(_arrayBufferWriter is not null);
                _memory = _arrayBufferWriter.GetMemory(BytesPending + sizeHint);
                Debug.Assert(_memory.Length >= sizeHint);
            }
            else
            {
                Debug.Assert(_output is not null);

                _output.Advance(BytesPending);
                BytesCommitted += BytesPending;
                BytesPending = 0;

                _memory = _output.GetMemory(sizeHint);

                if (_memory.Length < sizeHint)
                    throw new InvalidOperationException("need larger Span<byte>");
            }

            void FirstCallToGetMemory(int requiredSize)
            {
                Debug.Assert(_memory.Length == 0);
                Debug.Assert(BytesPending == 0);

                int sizeHint = Math.Max(InitialGrowthSize, requiredSize);

                if (_stream is not null)
                {
                    Debug.Assert(_arrayBufferWriter is not null);
                    _memory = _arrayBufferWriter.GetMemory(sizeHint);
                    Debug.Assert(_memory.Length >= sizeHint);
                }
                else
                {
                    Debug.Assert(_output != null);
                    _memory = _output.GetMemory(sizeHint);

                    if (_memory.Length < sizeHint)
                        throw new InvalidOperationException("need larger Span<byte>");
                }
            }
        }

        private void Fill(byte asciiChar, int count)
        {
            Debug.Assert(count >= 1);

            if (_memory.Length - BytesPending < count)
                Grow(count);

            var output = _memory.Span.Slice(BytesPending, count);
            output.Fill(asciiChar);
            BytesPending += count;
        }
    }
}
