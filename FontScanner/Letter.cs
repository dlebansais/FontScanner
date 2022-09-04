namespace FontScanner;

using System.Diagnostics;

[DebuggerDisplay("{DisplayText,nq}")]
public record Letter
{
    public static readonly Letter EmptyNormal = new(LetterType.Normal);
    /*
    public static readonly Letter EmptyItalic = new(LetterType.Italic);
    public static readonly Letter EmptyBold = new(LetterType.Bold);
    */
    public static readonly Letter Unknown = new("█");
    public static readonly Letter Whitespace = new(" ");
    public static readonly Letter Ignore1 = new("█");
    public static readonly Letter Combo1 = new('!', '”', LetterType.Italic);
    public static readonly Letter Combo2 = new('t', 'h', LetterType.Normal);
    public static readonly Letter Combo3 = new('s', 't', LetterType.Normal);
    public static readonly Letter Combo4 = new('n', 'd', LetterType.Normal);
    public static readonly Letter SpecialJ = new(' ', 'j', LetterType.Normal);
    public static readonly Letter SpecialJItalic = new(' ', 'j', LetterType.Italic);
    public static readonly Letter SubscriptReserved = new(' ', '®', LetterType.Normal);

    private Letter(LetterType letterType)
    {
        Text = string.Empty;
        LetterType = letterType;
    }

    private Letter(string text)
    {
        Text = text;
        LetterType = LetterType.Normal;
    }

    private Letter(char character1, char character2, LetterType letterType)
    {
        Text = $"{character1}{character2}";
        LetterType = letterType;
    }

    public Letter(char character, LetterType letterType)
    {
        Text = character.ToString();
        LetterType = letterType;
    }

    public Letter(Letter template, LetterType letterType)
    {
        Text = template.Text;
        LetterType = letterType;
    }

    public string Text { get; }
    public LetterType LetterType { get; }

    public bool IsEmpty { get { return Text.Length == 0; } }
    public string DisplayText { get { return $"{LetterType.BlueTag}{LetterType.ItalicTag}{LetterType.BoldTag}{Text}"; } }
    public bool IsBlue { get { return LetterType.IsBlue; } }
    public string BlueTag { get { return LetterType.BlueTag; } }
    public bool IsItalic { get { return LetterType.IsItalic; } }
    public string ItalicTag { get { return LetterType.ItalicTag; } }
    public bool IsBold { get { return LetterType.IsBold; } }
    public string BoldTag { get { return LetterType.BoldTag; } }
}
