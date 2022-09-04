namespace FontScanner;

using System.Diagnostics;

[DebuggerDisplay("{Column}, {Row}")]
public class FontBitmapCell
{
    public int Column { get; set; }
    public int Row { get; set; }
}
