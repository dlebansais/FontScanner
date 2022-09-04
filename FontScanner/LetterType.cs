namespace FontScanner;

using System.Diagnostics;

[DebuggerDisplay("{FontSize}{!IsBlue && !IsItalic && !IsBold ? \" Normal\" : \"\",nq}{IsBlue ? \" Blue\" : \"\",nq}{IsItalic ? \" Italic\" : \"\",nq}{IsBold ? \" Bold\" : \"\",nq}")]
public record LetterType
{
    public const int MinFontSize = 8;

    public static readonly LetterType Normal = new(false, false);
    public static readonly LetterType Italic = new(true, false);
    public static readonly LetterType Bold = new(false, true);
    public static readonly LetterType ItalicBold = new(true, true);

    private LetterType(bool isItalic, bool isBold)
        : this(fontSize:0, isBlue: false, isItalic, isBold)
    {
    }

    public LetterType(double fontSize, bool isBlue, bool isItalic, bool isBold)
    {
        FontSize = fontSize;
        IsBlue = isBlue;
        IsItalic = isItalic;
        IsBold = isBold;
    }

    public double FontSize { get; }
    public bool IsBlue { get; }
    public bool IsItalic { get; }
    public bool IsBold { get; }

    public string BlueTag { get { return IsBlue ? "§" : string.Empty; } }
    public string ItalicTag { get { return IsItalic ? "*" : string.Empty; } }
    public string BoldTag { get { return IsBold ? "#" : string.Empty; } }

    public static bool IsSameType(LetterType l1, LetterType l2)
    {
        return l1.IsItalic == l2.IsItalic && l1.IsBold == l2.IsBold;
    }

    public static bool IsEqual(LetterType l1, LetterType l2)
    {
        Debug.Assert(l1.FontSize > 0);
        Debug.Assert(l2.FontSize > 0);

        return IsSameType(l1, l2) && l1.FontSize == l2.FontSize;
    }

    public static LetterType WithSizeAndColor(LetterType l, double fontSize, bool isBlue)
    {
        Debug.Assert(l.FontSize == 0);
        Debug.Assert(fontSize >= MinFontSize);
        Debug.Assert(!l.IsBlue);

        return new(fontSize, isBlue, l.IsItalic, l.IsBold);
    }
}
