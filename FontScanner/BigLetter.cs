namespace FontScanner;

using FontLoader;
using System.Diagnostics;
using System.Drawing;

[DebuggerDisplay("{LetterArray is null ? \"None\" : DisplayText,nq}")]
public record BigLetter
{
    public static BigLetter None = new();

    private BigLetter()
    {
        Text = '\0';
        LetterArray = null!;
    }

    public BigLetter(char text, PixelArray letterArray)
    {
        Text = text;
        LetterArray = letterArray;
    }

    public char Text { get; }
    public PixelArray LetterArray { get; }
    public Rectangle Location { get; set; }
    public string DisplayText { get { return LetterArray is null ? string.Empty : Text.ToString(); } }
}
