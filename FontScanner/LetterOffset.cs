namespace FontScanner;

using System.Diagnostics;

[DebuggerDisplay("{Offset}, {LetterWidth}, {WhitespaceWidth}")]
public struct LetterOffset
{
    public int Offset { get; set; }
    public int LetterWidth { get; set; }
    public int WhitespaceWidth { get; set; }
}
