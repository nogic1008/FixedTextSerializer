using System;
using System.Runtime.CompilerServices;

[assembly: CLSCompliant(true)]
#if DEBUG
[assembly: InternalsVisibleTo("FixedTextSerializer.Tests")]
#endif
