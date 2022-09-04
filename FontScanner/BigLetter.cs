namespace FontScanner;

using System.Diagnostics;
using System.Drawing;

[DebuggerDisplay("{Text}")]
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
}
