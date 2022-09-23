namespace FontScanner;

using FontLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public static class ScanSpaceHelper
{
    #region Helper
    public static ScanSpace Get(Font font, LetterSkimmer skimmer, ScanInfo scanInfo)
    {
        if (skimmer.IsFirstLetter)
            return GetFirstLetterScanSpace(font);
        else
            return GetOtherLetterScanSpace(font, scanInfo);
    }
    #endregion

    #region First
    public static List<double> PreferredFirstLetterFontSizeList = new() { 109 };
    public const string PreferredFirstLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public static ScanSpace GetFirstLetterScanSpace(Font font)
    {
        if (FirstLetterScanSpace is null)
        {
            List<ScanSpaceItem> ItemList = new();

            FirstAddCommonLetters(ItemList, font, TypeFlags.Normal, isSingle: true);
            FirstAddCommonLetters(ItemList, font, TypeFlags.Italic, isSingle: true);
            FirstAddUncommonLetters(ItemList, font, TypeFlags.Normal, isSingle: true);
            FirstAddCommonLetters(ItemList, font, TypeFlags.Normal, isSingle: false);
            FirstAddUncommonLetters(ItemList, font, TypeFlags.Normal, isSingle: false);
            FirstAddCommonLetters(ItemList, font, TypeFlags.Italic, isSingle: false);
            FirstAddCommonLetters(ItemList, font, TypeFlags.Bold, isSingle: true);
            FirstAddCommonLetters(ItemList, font, TypeFlags.Blue, isSingle: true);
            FirstAddUncommonLetters(ItemList, font, TypeFlags.Italic, isSingle: true);
            FirstAddUncommonLetters(ItemList, font, TypeFlags.Bold, isSingle: true);
            FirstAddUncommonLetters(ItemList, font, TypeFlags.Blue, isSingle: true);
            FirstAddUncommonFonts(ItemList, font, TypeFlags.Normal, isSingle: true);
            FirstAddUncommonFonts(ItemList, font, TypeFlags.Italic, isSingle: true);
            FirstAddUncommonFonts(ItemList, font, TypeFlags.Bold, isSingle: true);
            FirstAddUncommonFonts(ItemList, font, TypeFlags.Blue, isSingle: true);
            FirstAddEverythingElse(ItemList, font, TypeFlags.Italic | TypeFlags.Bold, isSingle: true);
            FirstAddEverythingElse(ItemList, font, TypeFlags.Blue | TypeFlags.Italic, isSingle: true);
            FirstAddEverythingElse(ItemList, font, TypeFlags.Blue | TypeFlags.Bold, isSingle: true);
            FirstAddEverythingElse(ItemList, font, TypeFlags.Blue | TypeFlags.Italic | TypeFlags.Bold, isSingle: true);
            FirstAddCommonLetters(ItemList, font, TypeFlags.Bold, isSingle: false);
            FirstAddCommonLetters(ItemList, font, TypeFlags.Blue, isSingle: false);
            FirstAddUncommonLetters(ItemList, font, TypeFlags.Italic, isSingle: false);
            FirstAddUncommonLetters(ItemList, font, TypeFlags.Bold, isSingle: false);
            FirstAddUncommonLetters(ItemList, font, TypeFlags.Blue, isSingle: false);
            FirstAddUncommonFonts(ItemList, font, TypeFlags.Normal, isSingle: false);
            FirstAddUncommonFonts(ItemList, font, TypeFlags.Italic, isSingle: false);
            FirstAddUncommonFonts(ItemList, font, TypeFlags.Bold, isSingle: false);
            FirstAddUncommonFonts(ItemList, font, TypeFlags.Blue, isSingle: false);
            FirstAddEverythingElse(ItemList, font, TypeFlags.Italic | TypeFlags.Bold, isSingle: false);
            FirstAddEverythingElse(ItemList, font, TypeFlags.Blue | TypeFlags.Italic, isSingle: false);
            FirstAddEverythingElse(ItemList, font, TypeFlags.Blue | TypeFlags.Bold, isSingle: false);
            FirstAddEverythingElse(ItemList, font, TypeFlags.Blue | TypeFlags.Italic | TypeFlags.Bold, isSingle: false);

            FirstLetterScanSpace = new ScanSpace(font, ItemList);
        }

        return FirstLetterScanSpace;
    }

    private static void FirstAddCommonLetters(List<ScanSpaceItem> itemList, Font font, TypeFlags typeFlags, bool isSingle)
    {
        ScanSpaceItem NewItem = new(font);

        foreach (char c in PreferredFirstLetters)
            NewItem.CharacterList.Add(c);

        NewItem.TypeFlags = typeFlags;
        NewItem.IsSingle = isSingle;
        NewItem.FontSizeList.AddRange(PreferredFirstLetterFontSizeList);

        itemList.Add(NewItem);
    }

    private static void FirstAddUncommonLetters(List<ScanSpaceItem> itemList, Font font, TypeFlags typeFlags, bool isSingle)
    {
        ScanSpaceItem NewItem = new(font);

        foreach (char c in CellLoader.AllCharacters)
            if (PreferredFirstLetters.IndexOf(c) < 0)
                NewItem.CharacterList.Add(c);

        NewItem.SuperscriptList.AddRange(CellLoader.AllSuperscripts);
        NewItem.TypeFlags = typeFlags;
        NewItem.IsSingle = isSingle;
        NewItem.FontSizeList.AddRange(PreferredFirstLetterFontSizeList);

        itemList.Add(NewItem);
    }

    private static void FirstAddUncommonFonts(List<ScanSpaceItem> itemList, Font font, TypeFlags typeFlags, bool isSingle)
    {
        List<double> FontSizeList = new(font.FontSizeList);
        foreach (double FontSize in PreferredFirstLetterFontSizeList)
            FontSizeList.Remove(FontSize);

        ScanSpaceItem NewItem = new(font);
        NewItem.CharacterList.AddRange(CellLoader.AllCharacters);
        NewItem.SuperscriptList.AddRange(CellLoader.AllSuperscripts);
        NewItem.TypeFlags = typeFlags;
        NewItem.IsSingle = isSingle;
        NewItem.FontSizeList.AddRange(FontSizeList);

        itemList.Add(NewItem);
    }

    private static void FirstAddEverythingElse(List<ScanSpaceItem> itemList, Font font, TypeFlags typeFlags, bool isSingle)
    {
        ScanSpaceItem NewItem = new(font);
        NewItem.CharacterList.AddRange(CellLoader.AllCharacters);
        NewItem.SuperscriptList.AddRange(CellLoader.AllSuperscripts);
        NewItem.TypeFlags = typeFlags;
        NewItem.IsSingle = isSingle;
        NewItem.FontSizeList.AddRange(font.FontSizeList);

        itemList.Add(NewItem);
    }

    private static ScanSpace? FirstLetterScanSpace = null;
    #endregion

    #region Other
    public static List<double> PreferredOtherLetterFontSizeList = new() { 109 };
    public const string PreferredOtherLetters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static ScanSpace GetOtherLetterScanSpace(Font font, ScanInfo scanInfo)
    {
        if (OtherLetterScanSpace is null)
        {
            List<ScanSpaceItem> ItemList = new();
            OtherAddCommonLetters(ItemList, font, TypeFlags.Normal, isSingle: true);
            OtherAddCommonLetters(ItemList, font, TypeFlags.Italic, isSingle: true);
            OtherAddUncommonLetters(ItemList, font, TypeFlags.Normal, isSingle: true);
            OtherAddCommonLetters(ItemList, font, TypeFlags.Normal, isSingle: false);
            OtherAddUncommonLetters(ItemList, font, TypeFlags.Normal, isSingle: false);
            OtherAddCommonLetters(ItemList, font, TypeFlags.Italic, isSingle: false);
            OtherAddCommonLetters(ItemList, font, TypeFlags.Bold, isSingle: false);
            OtherAddCommonLetters(ItemList, font, TypeFlags.Blue, isSingle: false);
            OtherAddUncommonLetters(ItemList, font, TypeFlags.Italic, isSingle: false);
            OtherAddUncommonLetters(ItemList, font, TypeFlags.Bold, isSingle: false);
            OtherAddUncommonLetters(ItemList, font, TypeFlags.Blue, isSingle: false);
            OtherAddCommonLetters(ItemList, font, TypeFlags.Bold, isSingle: true);
            OtherAddCommonLetters(ItemList, font, TypeFlags.Blue, isSingle: true);
            OtherAddUncommonLetters(ItemList, font, TypeFlags.Italic, isSingle: true);
            OtherAddUncommonLetters(ItemList, font, TypeFlags.Bold, isSingle: true);
            OtherAddUncommonLetters(ItemList, font, TypeFlags.Blue, isSingle: true);
            OtherAddUncommonFonts(ItemList, font, TypeFlags.Normal, isSingle: true);
            OtherAddUncommonFonts(ItemList, font, TypeFlags.Italic, isSingle: true);
            OtherAddUncommonFonts(ItemList, font, TypeFlags.Bold, isSingle: true);
            OtherAddUncommonFonts(ItemList, font, TypeFlags.Blue, isSingle: true);
            OtherAddEverythingElse(ItemList, font, TypeFlags.Italic | TypeFlags.Bold, isSingle: true);
            OtherAddEverythingElse(ItemList, font, TypeFlags.Blue | TypeFlags.Italic, isSingle: true);
            OtherAddEverythingElse(ItemList, font, TypeFlags.Blue | TypeFlags.Bold, isSingle: true);
            OtherAddEverythingElse(ItemList, font, TypeFlags.Blue | TypeFlags.Italic | TypeFlags.Bold, isSingle: true);
            OtherAddUncommonFonts(ItemList, font, TypeFlags.Normal, isSingle: false);
            OtherAddUncommonFonts(ItemList, font, TypeFlags.Italic, isSingle: false);
            OtherAddUncommonFonts(ItemList, font, TypeFlags.Bold, isSingle: false);
            OtherAddUncommonFonts(ItemList, font, TypeFlags.Blue, isSingle: false);
            OtherAddEverythingElse(ItemList, font, TypeFlags.Italic | TypeFlags.Bold, isSingle: false);
            OtherAddEverythingElse(ItemList, font, TypeFlags.Blue | TypeFlags.Italic, isSingle: false);
            OtherAddEverythingElse(ItemList, font, TypeFlags.Blue | TypeFlags.Bold, isSingle: false);
            OtherAddEverythingElse(ItemList, font, TypeFlags.Blue | TypeFlags.Italic | TypeFlags.Bold, isSingle: false);

            OtherLetterScanSpace = new ScanSpace(font, ItemList);
        }

        Letter LastLetter = scanInfo.PreviousLetter;
        LetterType LastLetterType = scanInfo.LetterType;
        double LastFontSize = scanInfo.LetterType.FontSize;

        TypeFlags LastTypeFlags = TypeFlags.Normal;
        if (LastLetterType.IsBlue)
            LastTypeFlags |= TypeFlags.Blue;
        if (LastLetterType.IsItalic)
            LastTypeFlags |= TypeFlags.Italic;
        if (LastLetterType.IsBold)
            LastTypeFlags |= TypeFlags.Bold;

        if (LastLetterType.FontSize != 0)
        {
            MoveLastFontSizeToFirstPosition(OtherLetterScanSpace.ItemList, LastFontSize);
            MovePreviousTypeToFirstPosition(OtherLetterScanSpace.ItemList, LastTypeFlags, LastFontSize);
            MovePreviousLetterToFirstPosition(OtherLetterScanSpace.ItemList, LastTypeFlags, LastFontSize, LastLetter);
        }

        return OtherLetterScanSpace;
    }

    private static void OtherAddCommonLetters(List<ScanSpaceItem> itemList, Font font, TypeFlags typeFlags, bool isSingle)
    {
        ScanSpaceItem NewItem = new(font);

        foreach (char c in PreferredOtherLetters)
            NewItem.CharacterList.Add(c);

        NewItem.TypeFlags = typeFlags;
        NewItem.IsSingle = isSingle;
        NewItem.FontSizeList.AddRange(PreferredOtherLetterFontSizeList);

        itemList.Add(NewItem);
    }

    private static void OtherAddUncommonLetters(List<ScanSpaceItem> itemList, Font font, TypeFlags typeFlags, bool isSingle)
    {
        ScanSpaceItem NewItem = new(font);

        foreach (char c in CellLoader.AllCharacters)
            if (PreferredOtherLetters.IndexOf(c) < 0)
                NewItem.CharacterList.Add(c);

        NewItem.SuperscriptList.AddRange(CellLoader.AllSuperscripts);
        NewItem.TypeFlags = typeFlags;
        NewItem.IsSingle = isSingle;
        NewItem.FontSizeList.AddRange(PreferredOtherLetterFontSizeList);

        itemList.Add(NewItem);
    }

    private static void OtherAddUncommonFonts(List<ScanSpaceItem> itemList, Font font, TypeFlags typeFlags, bool isSingle)
    {
        List<double> FontSizeList = new(font.FontSizeList);
        foreach (double FontSize in PreferredOtherLetterFontSizeList)
            FontSizeList.Remove(FontSize);

        ScanSpaceItem NewItem = new(font);
        NewItem.CharacterList.AddRange(CellLoader.AllCharacters);
        NewItem.SuperscriptList.AddRange(CellLoader.AllSuperscripts);
        NewItem.TypeFlags = typeFlags;
        NewItem.IsSingle = isSingle;
        NewItem.FontSizeList.AddRange(FontSizeList);

        itemList.Add(NewItem);
    }

    private static void OtherAddEverythingElse(List<ScanSpaceItem> itemList, Font font, TypeFlags typeFlags, bool isSingle)
    {
        ScanSpaceItem NewItem = new(font);
        NewItem.CharacterList.AddRange(CellLoader.AllCharacters);
        NewItem.SuperscriptList.AddRange(CellLoader.AllSuperscripts);
        NewItem.TypeFlags = typeFlags;
        NewItem.IsSingle = isSingle;
        NewItem.FontSizeList.AddRange(font.FontSizeList);

        itemList.Add(NewItem);
    }

    private static void MoveLastFontSizeToFirstPosition(List<ScanSpaceItem> itemList, double lastFontSize)
    {
        foreach (ScanSpaceItem Item in itemList)
            MoveLastFontSizeToFirstPosition(Item, lastFontSize);
    }

    private static void MoveLastFontSizeToFirstPosition(ScanSpaceItem item, double lastFontSize)
    {
        List<double> FontSizeList = item.FontSizeList;

        if (FontSizeList.Count > 0 && FontSizeList[0] != lastFontSize && FontSizeList.Remove(lastFontSize))
            FontSizeList.Insert(0, lastFontSize);
    }

    private static void MovePreviousTypeToFirstPosition(List<ScanSpaceItem> itemList, TypeFlags typeFlags, double lastFontSize)
    {
        foreach (ScanSpaceItem Item in itemList)
            if (Item.FontSizeList[0] == lastFontSize && Item.TypeFlags == typeFlags && Item.CharacterList.Count == PreferredOtherLetters.Length)
            {
                itemList.Remove(Item);
                itemList.Insert(0, Item);
                break;
            }
    }

    private static void MovePreviousLetterToFirstPosition(List<ScanSpaceItem> itemList, TypeFlags typeFlags, double lastFontSize, Letter lastLetter)
    {
        if (lastLetter.Text.Length == 1)
        {
            char LastCharacter = lastLetter.Text[0];

            foreach (ScanSpaceItem Item in itemList)
                if (Item.FontSizeList[0] == lastFontSize && Item.TypeFlags == typeFlags && Item.CharacterList.Contains(LastCharacter))
                {
                    Item.CharacterList.Remove(LastCharacter);
                    Item.CharacterList.Insert(0, LastCharacter);
                    break;
                }
        }
    }

    private static ScanSpace? OtherLetterScanSpace = null;
    #endregion
}
