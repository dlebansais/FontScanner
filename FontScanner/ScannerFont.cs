namespace FontScanner;

using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;

[DebuggerDisplay("{Name,nq}")]
public class ScannerFont
{
    #region Init
    public ScannerFont(string fontName, Assembly fontAssembly)
    {
        Name = fontName;
        ProgressTable = FillProgressTable(fontAssembly);

        FontBitmap Bitmap = FillFontBitmap(fontAssembly);
        Dictionary<Letter, FontBitmapCell> CellTable = FillCellTable();
        CharacterTable = FillCharacterTable(Bitmap, CellTable);
    }

    private Dictionary<char, PixelArray> FillProgressTable(Assembly fontAssembly)
    {
        Dictionary<char, PixelArray> ProgressTable = new();
        string[] ResourceNames = fontAssembly.GetManifestResourceNames();

        foreach (string ResourceName in ResourceNames)
        {
            string[] Splitted = ResourceName.Split('.');
            if (Splitted.Length < 3)
                continue;

            if (Splitted[Splitted.Length - 3] != "PageResources")
                continue;

            string CharacterString = Splitted[Splitted.Length - 2];
            string Extension = Splitted[Splitted.Length - 1];

            if (Extension != "png")
                continue;

            if (!CharacterString.StartsWith("Page"))
                continue;

            CharacterString = CharacterString.Substring(4);

            char Character;
            if (CharacterString == "Slash")
                Character = '/';
            else if (CharacterString.Length == 1 && CharacterString[0] >= '0' && CharacterString[0] <= '9')
                Character = CharacterString[0];
            else
                continue;

            Stream PageBitmapStream = fontAssembly.GetManifestResourceStream(ResourceName);
            using Bitmap PageBitmap = new(PageBitmapStream);
            PixelArray Array = PixelArray.FromBitmap(PageBitmap);

            ProgressTable.Add(Character, Array);
        }

        return ProgressTable;
    }

    private FontBitmap FillFontBitmap(Assembly fontAssembly)
    {
        Dictionary<LetterType, Stream> StreamTable = new();
        string[] ResourceNames = fontAssembly.GetManifestResourceNames();

        foreach (string ResourceName in ResourceNames)
        {
            string[] Splitted = ResourceName.Split('.');
            if (Splitted.Length < 7)
                continue;

            if (Splitted[Splitted.Length - 6] != "FontResources")
                continue;

            string FontSizeString = Splitted[Splitted.Length - 4];
            string FontColorString = Splitted[Splitted.Length - 3];
            string LetterTypeString = Splitted[Splitted.Length - 2];
            string Extension = Splitted[Splitted.Length - 1];

            if (Extension != "png")
                continue;

            bool IsItalic;
            bool IsBold;
            double FontSize;

            if (LetterTypeString == "normal")
            {
                IsItalic = false;
                IsBold = false;
            }
            else if (LetterTypeString == "italic")
            {
                IsItalic = true;
                IsBold = false;
            }
            else if (LetterTypeString == "bold")
            {
                IsItalic = false;
                IsBold = true;
            }
            else if (LetterTypeString == "italic+bold")
            {
                IsItalic = true;
                IsBold = true;
            }
            else
                continue;

            bool IsBlue;

            if (FontColorString == "blue")
            {
                IsBlue = true;
            }
            else if (FontColorString == "black")
            {
                IsBlue = false;
            }
            else
                continue;

            while (FontSizeString.StartsWith("_") || FontSizeString.StartsWith("0"))
                FontSizeString = FontSizeString.Substring(1);
            FontSizeString = FontSizeString.Replace("x", ".");

            if (!double.TryParse(FontSizeString, NumberStyles.Float, CultureInfo.InvariantCulture, out FontSize) || FontSize < LetterType.MinFontSize)
                continue;

            //Debug.WriteLine(ResourceName);

            LetterType FontLetterType = new(FontSize, IsBlue, IsItalic, IsBold);
            Stream FontBitmapStream = fontAssembly.GetManifestResourceStream(ResourceName);

            StreamTable.Add(FontLetterType, FontBitmapStream);
        }

        return new FontBitmap(StreamTable);
    }

    private Dictionary<Letter, FontBitmapCell> FillCellTable()
    {
        Dictionary<Letter, FontBitmapCell> FontCellTable = new();

        FontCellTable.Add(Letter.SubscriptReserved, new FontBitmapCell() { Row = 4, Column = 16 });
        FillTestTable(FontCellTable, '£', new FontBitmapCell() { Row = 4, Column = 17 });
        FillTestTable(FontCellTable, '□', new FontBitmapCell() { Row = 4, Column = 18 });
        FillTestTable(FontCellTable, '₂', new FontBitmapCell() { Row = 5, Column = 0 });
        FillTestTable(FontCellTable, '©', new FontBitmapCell() { Row = 5, Column = 3 });
        FillTestTable(FontCellTable, '«', new FontBitmapCell() { Row = 5, Column = 5 });
        FontCellTable.Add(Letter.Combo3, new FontBitmapCell() { Row = 5, Column = 6 });
        FontCellTable.Add(Letter.Combo4, new FontBitmapCell() { Row = 5, Column = 7 });
        FillTestTable(FontCellTable, '®', new FontBitmapCell() { Row = 5, Column = 8 });
        FillTestTable(FontCellTable, 'ȕ', new FontBitmapCell() { Row = 5, Column = 16 });
        FillTestTable(FontCellTable, '•', new FontBitmapCell() { Row = 5, Column = 17 });
        FillTestTable(FontCellTable, 'Ī', new FontBitmapCell() { Row = 5, Column = 18 });
        FillTestTable(FontCellTable, '»', new FontBitmapCell() { Row = 6, Column = 1 });
        FillTestTable(FontCellTable, 'À', new FontBitmapCell() { Row = 6, Column = 6 });
        FillTestTable(FontCellTable, 'Â', new FontBitmapCell() { Row = 6, Column = 8 });
        FillTestTable(FontCellTable, 'Å', new FontBitmapCell() { Row = 6, Column = 11 });
        FillTestTable(FontCellTable, 'Ç', new FontBitmapCell() { Row = 6, Column = 13 });
        FillTestTable(FontCellTable, 'È', new FontBitmapCell() { Row = 6, Column = 14 });
        FillTestTable(FontCellTable, 'É', new FontBitmapCell() { Row = 6, Column = 15 });
        FillTestTable(FontCellTable, 'Ê', new FontBitmapCell() { Row = 6, Column = 16 });
        FillTestTable(FontCellTable, 'Ë', new FontBitmapCell() { Row = 6, Column = 17 });
        FillTestTable(FontCellTable, 'Ï', new FontBitmapCell() { Row = 7, Column = 1 });
        FillTestTable(FontCellTable, 'Ô', new FontBitmapCell() { Row = 7, Column = 6 });
        FillTestTable(FontCellTable, 'Ü', new FontBitmapCell() { Row = 7, Column = 14 });
        FillTestTable(FontCellTable, 'à', new FontBitmapCell() { Row = 7, Column = 18 });
        FillTestTable(FontCellTable, 'á', new FontBitmapCell() { Row = 7, Column = 19 });
        FillTestTable(FontCellTable, 'â', new FontBitmapCell() { Row = 8, Column = 0 });
        FillTestTable(FontCellTable, 'æ', new FontBitmapCell() { Row = 8, Column = 4 });
        FillTestTable(FontCellTable, 'ç', new FontBitmapCell() { Row = 8, Column = 5 });
        FillTestTable(FontCellTable, 'è', new FontBitmapCell() { Row = 8, Column = 6 });
        FillTestTable(FontCellTable, 'é', new FontBitmapCell() { Row = 8, Column = 7 });
        FillTestTable(FontCellTable, 'ê', new FontBitmapCell() { Row = 8, Column = 8 });
        FillTestTable(FontCellTable, 'ë', new FontBitmapCell() { Row = 8, Column = 9 });
        FillTestTable(FontCellTable, 'í', new FontBitmapCell() { Row = 8, Column = 11 });
        FillTestTable(FontCellTable, 'î', new FontBitmapCell() { Row = 8, Column = 12 });
        FillTestTable(FontCellTable, 'ï', new FontBitmapCell() { Row = 8, Column = 13 });
        FillTestTable(FontCellTable, 'ó', new FontBitmapCell() { Row = 8, Column = 17 });
        FillTestTable(FontCellTable, 'ô', new FontBitmapCell() { Row = 8, Column = 18 });
        FillTestTable(FontCellTable, 'ö', new FontBitmapCell() { Row = 9, Column = 0 });
        FillTestTable(FontCellTable, 'ù', new FontBitmapCell() { Row = 9, Column = 3 });
        FillTestTable(FontCellTable, 'û', new FontBitmapCell() { Row = 9, Column = 5 });
        FillTestTable(FontCellTable, 'ü', new FontBitmapCell() { Row = 9, Column = 6 });

        FillTestTable(FontCellTable, '‒', new FontBitmapCell() { Row = 11, Column = 0 }); // Short
        FillTestTable(FontCellTable, 'œ', new FontBitmapCell() { Row = 11, Column = 1 });
        FillTestTable(FontCellTable, '—', new FontBitmapCell() { Row = 11, Column = 2 }); // Long
        FillTestTable(FontCellTable, '…', new FontBitmapCell() { Row = 11, Column = 3 });
        FillTestTable(FontCellTable, '“', new FontBitmapCell() { Row = 11, Column = 4 });
        FillTestTable(FontCellTable, '”', new FontBitmapCell() { Row = 11, Column = 5 });
        FillTestTable(FontCellTable, 'ŵ', new FontBitmapCell() { Row = 11, Column = 6 });
        FontCellTable.Add(Letter.Ignore1, new FontBitmapCell() { Row = 11, Column = 7 });
        FillTestTable(FontCellTable, 'þ', new FontBitmapCell() { Row = 11, Column = 8 });
        FillTestTable(FontCellTable, '‘', new FontBitmapCell() { Row = 11, Column = 9 });
        FontCellTable.Add(Letter.Combo1, new FontBitmapCell() { Row = 11, Column = 10 });
        FontCellTable.Add(Letter.Combo2, new FontBitmapCell() { Row = 11, Column = 11 });
        FontCellTable.Add(Letter.SpecialJ, new FontBitmapCell() { Row = 11, Column = 12 });
        FontCellTable.Add(Letter.SpecialJItalic, new FontBitmapCell() { Row = 11, Column = 12 });
        FillTestTable(FontCellTable, 'ᾱ', new FontBitmapCell() { Row = 11, Column = 13 });
        FillTestTable(FontCellTable, 'ῑ', new FontBitmapCell() { Row = 11, Column = 14 });
        FillTestTable(FontCellTable, 'ῡ', new FontBitmapCell() { Row = 11, Column = 15 });
        FillTestTable(FontCellTable, 'ʿ', new FontBitmapCell() { Row = 11, Column = 16 });
        FillTestTable(FontCellTable, 'ḥ', new FontBitmapCell() { Row = 11, Column = 17 });
        FillTestTable(FontCellTable, 'ṣ', new FontBitmapCell() { Row = 11, Column = 18 });
        FillTestTable(FontCellTable, '’', new FontBitmapCell() { Row = 11, Column = 19 });

        return FontCellTable;
    }

    private void FillTestTable(Dictionary<Letter, FontBitmapCell> fontCellTable, char character, FontBitmapCell bitmapCell)
    {
        fontCellTable.Add(new Letter(character, LetterType.Normal), bitmapCell);
        fontCellTable.Add(new Letter(character, LetterType.Italic), bitmapCell);
        fontCellTable.Add(new Letter(character, LetterType.Bold), bitmapCell);
        fontCellTable.Add(new Letter(character, LetterType.ItalicBold), bitmapCell);
    }

    private Dictionary<Letter, PixelArray> FillCharacterTable(FontBitmap bitmap, Dictionary<Letter, FontBitmapCell> cellTable)
    {
        Dictionary<Letter, PixelArray> CharacterTable = new();
        bool[,] CellTaken = new bool[bitmap.Columns, bitmap.Rows];

        foreach (KeyValuePair<Letter, FontBitmapCell> Entry in cellTable)
        {
            Letter Letter = Entry.Key;
            FontBitmapCell Cell = Entry.Value;
            if (Cell.Column >= 0 && Cell.Column < bitmap.Columns && Cell.Row >= 0 && Cell.Row < bitmap.Rows)
            {
                CellTaken[Cell.Column, Cell.Row] = true;

                AddLetter(bitmap, Cell.Column, Cell.Row, CharacterTable, Letter);
            }
        }

        int BaseIndex = '!';

        for (int Column = 0; Column < bitmap.Columns; Column++)
            for (int Row = 0; Row < bitmap.Rows; Row++)
            {
                char Character = char.ConvertFromUtf32(BaseIndex + (Row * bitmap.Columns) + Column)[0];

                if (!CellTaken[Column, Row])
                    AddLetter(bitmap, Column, Row, CharacterTable, Character);
            }

        Debug.Assert(new List<Letter>(CharacterTable.Keys).TrueForAll((Letter l) => l.LetterType.FontSize >= LetterType.MinFontSize));
        return CharacterTable;
    }

    private void AddLetter(FontBitmap bitmap, int column, int row, Dictionary<Letter, PixelArray> characterTable, Letter letter)
    {
        foreach (LetterType Key in bitmap.SupportedLetterTypes)
            if (LetterType.IsSameType(Key, letter.LetterType))
            {
                PixelArray CellArray = bitmap.GetPixelArray(column, row, Key);
                CellArray = CellArray.Clipped();

                if (CellArray != PixelArray.Empty)
                    AddLetter(characterTable, letter, Key.FontSize, Key.IsBlue, CellArray);
            }
    }

    private void AddLetter(Dictionary<Letter, PixelArray> characterTable, Letter letter, double fontSize, bool isBlue, PixelArray cellArray)
    {
        LetterType NewLetterType = LetterType.WithSizeAndColor(letter.LetterType, fontSize, isBlue);
        Letter NewLetter = new(letter, NewLetterType);

        characterTable.Add(NewLetter, cellArray);
    }

    private void AddLetter(FontBitmap bitmap, int column, int row, Dictionary<Letter, PixelArray> characterTable, char character)
    {
        foreach (LetterType Key in bitmap.SupportedLetterTypes)
        {
            Letter Letter = new Letter(character, Key);
            if (!characterTable.ContainsKey(Letter))
            {
                PixelArray CellArray = bitmap.GetPixelArray(column, row, Key);
                CellArray = CellArray.Clipped();

                if (CellArray != PixelArray.Empty)
                    characterTable.Add(Letter, CellArray);
            }
        }
    }
    #endregion

    #region Properties
    public string Name { get; }
    public Dictionary<char, PixelArray> ProgressTable { get; }
    public Dictionary<Letter, PixelArray> CharacterTable { get; }
    #endregion

    #region Client Interface
    public static List<Letter> GetPreferredOrder(Dictionary<Letter, PixelArray> table)
    {
        List<Letter> Result = new(table.Keys);
        Result.Sort(SortByLikelyness);

        return Result;
    }

    private static int SortByLikelyness(Letter l1, Letter l2)
    {
        long Flags1 = LetterFlags(l1);
        long Flags2 = LetterFlags(l2);

        if (Flags1 > Flags2)
            return -1;
        else if (Flags1 < Flags2)
            return 1;

        if (l1.Text.Length > l2.Text.Length)
            return -1;
        else if (l1.Text.Length < l2.Text.Length)
            return 1;

        if (l1.LetterType.FontSize > l2.LetterType.FontSize)
            return -1;
        else if (l1.LetterType.FontSize < l2.LetterType.FontSize)
            return 1;

        for (int i = 0; i < l1.Text.Length; i++)
        {
            char c1 = l1.Text[i];
            char c2 = l2.Text[i];

            int Order = (int)c1 - (int)c2;

            if (Order != 0)
                return Order;
        }

        return 0;
    }

    private static long LetterFlags(Letter l)
    {
        const long FlagItalic = 0x20;
        const long FlagNotBold = 0x10;
        const long FlagNotUpper = 0x08;
        const long FlagLetter = 0x04;
        const long FlagThreeDots = 0x02;
        const long FlagNotUpperI = 0x01;

        long Flags = 0;

        if (l.IsItalic)
            Flags |= FlagItalic;
        if (!l.IsBold)
            Flags |= FlagNotBold;

        char c = l.Text[0];

        if (!char.IsUpper(c))
            Flags |= FlagNotUpper;
        if (char.IsLetter(c))
            Flags |= FlagLetter;
        if (c == '…')
            Flags |= FlagThreeDots;
        if (c != 'I')
            Flags |= FlagNotUpperI;

        return Flags;
    }
    #endregion
}
