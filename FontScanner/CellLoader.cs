﻿using FontLoader;
using System.Collections.Generic;

namespace FontScanner;

public static class CellLoader
{
    public static readonly string AllCharacters =
        "!\"#$%&'()*+,-./01234" +
        "56789:;<=>?@ABCDEFGH" +
        "IJKLMNOPQRSTUVWXYZ[\\" +
        "]^_`abcdefghijklmnop" +
        "qrstuvwxyz{|}~ ¡¢£¤¥" +
        "¦§¨©ª«¬ ®¯°±²³´µ¶· ¹" +
        "º»¼½¾¿ÀÁÂÃÄÅÆÇÈÉÊËÌÍ" +
        "ÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßàá" +
        "âãäåæçèéêëìíîïðñòóôõ" +
        "ö÷øùúûüýþÿĀāĂăĄąĆćĈĉ" +
        "ĊċČčĎďĐđĒēĔĕĖėĘęĚěĜĝ" +
        "‒œ—…“”ŵ‘ᾱῑῡʿḥṣ’•";

    public static readonly string[] AllSupercripts =
    {
        "th", "st", "nd", "®",
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
            if (Character != ' ' && Character != ' ')
            {
                FontBitmapCell BitmapCell = new() { Row = Row, Column = Column };
                FontCellTable.Add(new Letter(Character, LetterType.Normal), BitmapCell);
                FontCellTable.Add(new Letter(Character, LetterType.Italic), BitmapCell);
                FontCellTable.Add(new Letter(Character, LetterType.Bold), BitmapCell);
                FontCellTable.Add(new Letter(Character, LetterType.ItalicBold), BitmapCell);

                if (FirstLetters.Contains($"{Character}"))
                {
                    string Text = $"*{Character}";
                    FontCellTable.Add(new Letter(Text, LetterType.Normal), BitmapCell);
                    FontCellTable.Add(new Letter(Text, LetterType.Italic), BitmapCell);
                    FontCellTable.Add(new Letter(Text, LetterType.Bold), BitmapCell);
                    FontCellTable.Add(new Letter(Text, LetterType.ItalicBold), BitmapCell);
                }
            }

            Column++;
            if (Column >= 20)
            {
                Column = 0;
                Row++;
            }
        }

        foreach (string Supercript in AllSupercripts)
        {
            FontBitmapCell BitmapCell = new() { Row = Row, Column = Column };
            string Text = $" {Supercript}";
            FontCellTable.Add(new Letter(Text, LetterType.Normal), BitmapCell);
            FontCellTable.Add(new Letter(Text, LetterType.Italic), BitmapCell);
            FontCellTable.Add(new Letter(Text, LetterType.Bold), BitmapCell);
            FontCellTable.Add(new Letter(Text, LetterType.ItalicBold), BitmapCell);

            Column++;
            if (Column >= 20)
            {
                Column = 0;
                Row++;
            }
        }

        foreach (string Supercript in AllSupercripts)
        {
            FontBitmapCell BitmapCell = new() { Row = Row, Column = Column };
            string Text = $"-{Supercript}";
            FontCellTable.Add(new Letter(Text, LetterType.Normal), BitmapCell);
            FontCellTable.Add(new Letter(Text, LetterType.Italic), BitmapCell);
            FontCellTable.Add(new Letter(Text, LetterType.Bold), BitmapCell);
            FontCellTable.Add(new Letter(Text, LetterType.ItalicBold), BitmapCell);

            Column++;
            if (Column >= 20)
            {
                Column = 0;
                Row++;
            }
        }

        return FontCellTable;
    }

/*
    public static Dictionary<Letter, FontBitmapCell> FillCellTable()
    {
        Dictionary<Letter, FontBitmapCell> FontCellTable = new();

        FontCellTable.Add(Letter.SubscriptReserved, new FontBitmapCell() { Row = 4, Column = 16 });
        FillTestTable(FontCellTable, '£', new FontBitmapCell() { Row = 4, Column = 17 });
        FillTestTable(FontCellTable, '□', new FontBitmapCell() { Row = 4, Column = 18 });
        FillTestTable(FontCellTable, '₂', new FontBitmapCell() { Row = 5, Column = 0 });
        FillTestTable(FontCellTable, 'ń', new FontBitmapCell() { Row = 5, Column = 2 });
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
        FillTestTable(FontCellTable, '×', new FontBitmapCell() { Row = 7, Column = 9 });
        FillTestTable(FontCellTable, 'Ü', new FontBitmapCell() { Row = 7, Column = 14 });
        FillTestTable(FontCellTable, 'à', new FontBitmapCell() { Row = 7, Column = 18 });
        FillTestTable(FontCellTable, 'á', new FontBitmapCell() { Row = 7, Column = 19 });
        FillTestTable(FontCellTable, 'â', new FontBitmapCell() { Row = 8, Column = 0 });
        FillTestTable(FontCellTable, 'ä', new FontBitmapCell() { Row = 8, Column = 2 });
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
        FillTestTable(FontCellTable, 'ć', new FontBitmapCell() { Row = 9, Column = 17 });

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

    private static void FillTestTable(Dictionary<Letter, FontBitmapCell> fontCellTable, char character, FontBitmapCell bitmapCell)
    {
        fontCellTable.Add(new Letter(character, LetterType.Normal), bitmapCell);
        fontCellTable.Add(new Letter(character, LetterType.Italic), bitmapCell);
        fontCellTable.Add(new Letter(character, LetterType.Bold), bitmapCell);
        fontCellTable.Add(new Letter(character, LetterType.ItalicBold), bitmapCell);
    }
*/
}