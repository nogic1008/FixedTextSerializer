using System;
using System.Text;

namespace FixedTextSerializer
{
    public struct FixedTextOptions
    {
        private NewLine _newLine;
        /// <summary>
        /// Gets and sets the newline string style.
        /// </summary>
        public NewLine NewLine
        {
            readonly get => _newLine;
            set => _newLine = value <= NewLine.CrLf ? value
                : throw new ArgumentOutOfRangeException(nameof(value));
        }

        public bool InsertFinalNewLine { readonly get; set; }

        /// <summary>
        /// Gets and sets the code page identifier of the preferred encoding.
        /// Possible values are listed in <see href="https://docs.microsoft.com/dotnet/api/system.text.encoding#list-of-encodings"/>,
        /// or 0 (zero) to use the default encoding.
        /// </summary>
        public int CodePage { readonly get; set; }

        /// <summary>
        /// Sets code page name of the preferred encoding.
        /// Possible values are listed in <see href="https://docs.microsoft.com/dotnet/api/system.text.encoding#list-of-encodings"/>
        /// </summary>
        public string Encode
        {
            set => CodePage = Encoding.GetEncoding(value).CodePage;
        }
    }

    [Flags]
    public enum NewLine : byte
    {
        ///<summary>Use <see cref="Environment.NewLine"/></summary>
        Auto = 0,
        ///<summary>Use CR("\r", 0x0d)</summary>
        Cr = 1,
        ///<summary>Use LF("\n", 0x0a)</summary>
        Lf = 2,
        ///<summary>Use CRLF("\r\n", 0x0d + 0x0a)</summary>
        CrLf = Cr | Lf,
    }
}
