using FontLoader;
using System.Collections.Generic;

namespace FontScanner;

public static class CellLoader
{
    public static readonly List<char> AllCharacters = new()
    {
        '!', '\"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/', '0', '1', '2', '3', '4',
        '5', '6', '7', '8', '9', ':', ';', '<', '=', '>', '?', '@', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H',
        'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '[', '\\',
        ']', '^', '_', '`', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
        'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '{', '|', '}', '~', LetterHelper.NoBreakSpace, '¡', '¢', '£', '¤', '¥',
        '¦', '§', '¨', '©', 'ª', '«', '¬', LetterHelper.SoftHypen, '®', '¯', '°', '±', '²', '³', '´', 'µ', '¶', '·', LetterHelper.Cedilla, '¹',
        'º', '»', '¼', '½', '¾', '¿', 'À', 'Á', 'Â', 'Ã', 'Ä', 'Å', 'Æ', 'Ç', 'È', 'É', 'Ê', 'Ë', 'Ì', 'Í',
        'Î', 'Ï', 'Ð', 'Ñ', 'Ò', 'Ó', 'Ô', 'Õ', 'Ö', '×', 'Ø', 'Ù', 'Ú', 'Û', 'Ü', 'Ý', 'Þ', 'ß', 'à', 'á',
        'â', 'ã', 'ä', 'å', 'æ', 'ç', 'è', 'é', 'ê', 'ë', 'ì', 'í', 'î', 'ï', 'ð', 'ñ', 'ò', 'ó', 'ô', 'õ',
        'ö', '÷', 'ø', 'ù', 'ú', 'û', 'ü', 'ý', 'þ', 'ÿ', 'Ā', 'ā', 'Ă', 'ă', 'Ą', 'ą', 'Ć', 'ć', 'Ĉ', 'ĉ',
        'Ċ', 'ċ', 'Č', 'č', 'Ď', 'ď', 'Đ', 'đ', 'Ē', 'ē', 'Ĕ', 'ĕ', 'Ė', 'ė', 'Ę', 'ę', 'Ě', 'ě', 'Ĝ', 'ĝ',
        '‒', 'œ', '—', '…', '“', '”', 'ŵ', '‘', 'ᾱ', 'ῑ', 'ῡ', 'ʿ', 'ḥ', 'ṣ', '’', '•', '∞'
    };

    public static readonly List<string> AllSuperscripts = new()
    {
        "th", "st", "nd", "+®", "+0", "+1", "+2", "+3", "+4", "+5", "+6", "+7", "+8", "+9",
    };

    // TODO: subscript
    public static readonly List<string> AllSubscripts = new()
    {
        "-2", // As in 'CO2'
    };

    public static readonly string FirstLetters =
        "jy";

    public const int CharacterSequenceMaxLength = 5;

    public static Dictionary<Letter, FontBitmapCell> FillCellTable()
    {
        Dictionary<Letter, FontBitmapCell> FontCellTable = new();

        int Row = 0;
        int Column = 0;

        foreach (char Character in AllCharacters)
        {
            if (!LetterHelper.IsWhitespace(Character))
            {
                FontBitmapCell BitmapCell = new() { Row = Row, Column = Column };
                FontCellTable.Add(new Letter(Character, LetterType.Normal), BitmapCell);
                FontCellTable.Add(new Letter(Character, LetterType.Italic), BitmapCell);
                FontCellTable.Add(new Letter(Character, LetterType.Bold), BitmapCell);
                FontCellTable.Add(new Letter(Character, LetterType.ItalicBold), BitmapCell);
            }

            Column++;
            if (Column >= 20)
            {
                Column = 0;
                Row++;
            }
        }

        foreach (string Superscript in AllSuperscripts)
        {
            FontBitmapCell BitmapCell = new() { Row = Row, Column = Column };
            string Text = Superscript;
            bool IsSingleGlyph = Superscript.Length <= 1 || Superscript[0] == '+';
            FontCellTable.Add(new Letter(Text, LetterType.Normal, isWhitespace: false, IsSingleGlyph), BitmapCell);
            FontCellTable.Add(new Letter(Text, LetterType.Italic, isWhitespace: false, IsSingleGlyph), BitmapCell);
            FontCellTable.Add(new Letter(Text, LetterType.Bold, isWhitespace: false, IsSingleGlyph), BitmapCell);
            FontCellTable.Add(new Letter(Text, LetterType.ItalicBold, isWhitespace: false, IsSingleGlyph), BitmapCell);

            Column++;
            if (Column >= 20)
            {
                Column = 0;
                Row++;
            }
        }

        foreach (string Subscript in AllSubscripts)
        {
            FontBitmapCell BitmapCell = new() { Row = Row, Column = Column };
            string Text = Subscript;
            bool IsSingleGlyph = Subscript.Length <= 1 || Subscript[0] == '-';
            FontCellTable.Add(new Letter(Text, LetterType.Normal, isWhitespace: false, IsSingleGlyph), BitmapCell);
            FontCellTable.Add(new Letter(Text, LetterType.Italic, isWhitespace: false, IsSingleGlyph), BitmapCell);
            FontCellTable.Add(new Letter(Text, LetterType.Bold, isWhitespace: false, IsSingleGlyph), BitmapCell);
            FontCellTable.Add(new Letter(Text, LetterType.ItalicBold, isWhitespace: false, IsSingleGlyph), BitmapCell);

            Column++;
            if (Column >= 20)
            {
                Column = 0;
                Row++;
            }
        }

        return FontCellTable;
    }
}
