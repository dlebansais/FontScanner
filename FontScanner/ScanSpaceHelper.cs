namespace FontScanner;

using FontLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public static class ScanSpaceHelper
{
    #region Helper
    public static ScanSpace GetPrimary(Font font, LetterSkimmer skimmer)
    {
        if (skimmer.IsFirstLetter)
            return GetFirstLetterScanSpace(font);
        else
            return GetOtherLetterScanSpace(font);
    }

    public static ScanSpace GetSecondary(Font font)
    {
        return GetNextLetterScanSpace(font);
    }
    #endregion

    #region First
    public static ScanSpaceSearch FirstSearch { get; } = new(new List<char>());
    public static ScanSpaceSearch OtherSearch { get; } = new(new List<char>() { ',', '.', ';' });
    public static ScanSpaceSearch NextSearch { get; } = new(new List<char>() { ',', '.', ';' });

    public static ScanSpace GetFirstLetterScanSpace(Font font)
    {
        if (FirstLetterScanSpace is null)
        {
            List<ScanSpaceItem> ItemList = new();
            ScanSpaceSearch Search = FirstSearch;

            AddCommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: true, isPreferredFont: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: true, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: true, isPreferredFont: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: false, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: false, isPreferredFont: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: false, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: false, isPreferredFont: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: true, isPreferredFont: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: true, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: true, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: true, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: true, isPreferredFont: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: true, isPreferredFont: false);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: true, isPreferredFont: false);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: true, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: true, isPreferredFont: false);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: false, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: false, isPreferredFont: false);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: false, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: false, isPreferredFont: false);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: true, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: true, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: true, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: true, isPreferredFont: false);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: false, isPreferredFont: true);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Bold, isSingle: true);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Normal, isSingle: true);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Italic, isSingle: true);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Blue, isSingle: true);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Blue | TypeFlags.Bold, isSingle: true);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Italic | TypeFlags.Bold, isSingle: true);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Blue | TypeFlags.Italic, isSingle: true);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Blue | TypeFlags.Italic | TypeFlags.Bold, isSingle: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: false, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: false, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: false, isPreferredFont: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: false, isPreferredFont: false);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: false, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: false, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: false, isPreferredFont: false);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Normal, isSingle: false);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Italic, isSingle: false);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Bold, isSingle: false);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Blue, isSingle: false);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Blue | TypeFlags.Bold, isSingle: false);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Italic | TypeFlags.Bold, isSingle: false);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Blue | TypeFlags.Italic, isSingle: false);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Blue | TypeFlags.Italic | TypeFlags.Bold, isSingle: false);

            FirstLetterScanSpace = new ScanSpace(font, ItemList, Search, isSingleOnly: false);
        }

        return FirstLetterScanSpace;
    }

    public static ScanSpace GetOtherLetterScanSpace(Font font)
    {
        if (OtherLetterScanSpace is null)
        {
            List<ScanSpaceItem> ItemList = new();
            ScanSpaceSearch Search = OtherSearch;

            AddCommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: true, isPreferredFont: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: true, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: true, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: true, isPreferredFont: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: false, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: false, isPreferredFont: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: false, isPreferredFont: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: false, isPreferredFont: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: false, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: false, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: false, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: false, isPreferredFont: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: true, isPreferredFont: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: true, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: true, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: true, isPreferredFont: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: true, isPreferredFont: false);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: true, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: true, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: true, isPreferredFont: false);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: false, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: false, isPreferredFont: false);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: false, isPreferredFont: false);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: false, isPreferredFont: false);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: false, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: false, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: false, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: false, isPreferredFont: false);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: true, isPreferredFont: false);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: true, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: true, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: true, isPreferredFont: false);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Normal, isSingle: true);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Italic, isSingle: true);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Bold, isSingle: true);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Blue, isSingle: true);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Italic | TypeFlags.Bold, isSingle: true);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Blue | TypeFlags.Italic, isSingle: true);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Blue | TypeFlags.Bold, isSingle: true);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Blue | TypeFlags.Italic | TypeFlags.Bold, isSingle: true);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Normal, isSingle: false);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Italic, isSingle: false);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Bold, isSingle: false);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Blue, isSingle: false);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Italic | TypeFlags.Bold, isSingle: false);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Blue | TypeFlags.Italic, isSingle: false);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Blue | TypeFlags.Bold, isSingle: false);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Blue | TypeFlags.Italic | TypeFlags.Bold, isSingle: false);

            OtherLetterScanSpace = new ScanSpace(font, ItemList, Search, isSingleOnly: false);
        }

        return OtherLetterScanSpace;
    }

    public static ScanSpace GetNextLetterScanSpace(Font font)
    {
        if (NextLetterScanSpace is null)
        {
            List<ScanSpaceItem> ItemList = new();
            ScanSpaceSearch Search = NextSearch;

            AddCommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: true, isPreferredFont: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: true, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: true, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: true, isPreferredFont: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: true, isPreferredFont: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: true, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: true, isPreferredFont: true);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: true, isPreferredFont: true);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: true, isPreferredFont: false);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: true, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Normal, isSingle: true, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Italic, isSingle: true, isPreferredFont: false);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: true, isPreferredFont: false);
            AddCommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: true, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Bold, isSingle: true, isPreferredFont: false);
            AddUncommonLetters(ItemList, font, Search, TypeFlags.Blue, isSingle: true, isPreferredFont: false);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Normal, isSingle: true);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Italic, isSingle: true);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Bold, isSingle: true);
            AddUncommonFonts(ItemList, font, Search, TypeFlags.Blue, isSingle: true);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Italic | TypeFlags.Bold, isSingle: true);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Blue | TypeFlags.Italic, isSingle: true);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Blue | TypeFlags.Italic | TypeFlags.Bold, isSingle: true);
            AddEverythingElse(ItemList, font, Search, TypeFlags.Blue | TypeFlags.Bold, isSingle: true);

            NextLetterScanSpace = new ScanSpace(font, ItemList, Search, isSingleOnly: true);
        }

        return NextLetterScanSpace;
    }

    private static void AddCommonLetters(List<ScanSpaceItem> itemList, Font font, ScanSpaceSearch search, TypeFlags typeFlags, bool isSingle, bool isPreferredFont)
    {
        List<char> CharacterList = search.PreferredLetters;
        CharacterPreference CharacterPreference = CharacterPreference.Preferred;
        List<double> FontSizeList = isPreferredFont ? search.PreferredLetterFontSizeList : search.UsedLetterFontSizeList;
        FontPreference FontPreference = isPreferredFont ? FontPreference.Preferred : FontPreference.OtherUsed;

        ScanSpaceItem NewItem = new(font, typeFlags, isSingle, CharacterList, CharacterPreference, FontSizeList, FontPreference);

        itemList.Add(NewItem);
    }

    private static void AddUncommonLetters(List<ScanSpaceItem> itemList, Font font, ScanSpaceSearch search, TypeFlags typeFlags, bool isSingle, bool isPreferredFont)
    {
        List<char> CharacterList = new();
        foreach (char c in CellLoader.AllCharacters)
            if (!search.PreferredLetters.Contains(c) && !LetterHelper.IsWhitespace(c))
                CharacterList.Add(c);
        CharacterPreference CharacterPreference = CharacterPreference.AllOthers;

        List<double> FontSizeList = isPreferredFont ? search.PreferredLetterFontSizeList : search.UsedLetterFontSizeList;
        FontPreference FontPreference = isPreferredFont ? FontPreference.Preferred : FontPreference.OtherUsed;

        ScanSpaceItem NewItem = new(font, typeFlags, isSingle, CharacterList, CharacterPreference, FontSizeList, FontPreference);
        NewItem.AddSuperscripts(CellLoader.AllSuperscripts);
        NewItem.AddSubscripts(CellLoader.AllSubscripts);

        itemList.Add(NewItem);
    }

    private static void AddUncommonFonts(List<ScanSpaceItem> itemList, Font font, ScanSpaceSearch search, TypeFlags typeFlags, bool isSingle)
    {
        List<char> NonWhitespaceCharacterList = new();
        foreach (char c in CellLoader.AllCharacters)
            if (!LetterHelper.IsWhitespace(c))
                NonWhitespaceCharacterList.Add(c);

        List<double> FontSizeList = new(font.FontSizeList);
        foreach (double FontSize in search.PreferredLetterFontSizeList)
            FontSizeList.Remove(FontSize);
        foreach (double FontSize in search.UsedLetterFontSizeList)
            FontSizeList.Remove(FontSize);

        ScanSpaceItem NewItem = new(font, typeFlags, isSingle);
        NewItem.AddCharacters(NonWhitespaceCharacterList);
        NewItem.AddSuperscripts(CellLoader.AllSuperscripts);
        NewItem.AddSubscripts(CellLoader.AllSubscripts);
        NewItem.AddFontSizes(FontSizeList);

        itemList.Add(NewItem);
    }

    private static void AddEverythingElse(List<ScanSpaceItem> itemList, Font font, ScanSpaceSearch search, TypeFlags typeFlags, bool isSingle)
    {
        List<char> CharacterList = search.PreferredLetters;
        CharacterPreference CharacterPreference = CharacterPreference.Preferred;

        ScanSpaceItem NewItemPreferredChar = new(font, typeFlags, isSingle, CharacterList, CharacterPreference);
        NewItemPreferredChar.AddFontSizes(font.FontSizeList);

        itemList.Add(NewItemPreferredChar);

        List<char> NonWhitespaceCharacterList = new();
        foreach (char c in CellLoader.AllCharacters)
            if (!LetterHelper.IsWhitespace(c) && !CharacterList.Contains(c))
                NonWhitespaceCharacterList.Add(c);

        ScanSpaceItem NewItemOtherChar = new(font, typeFlags, isSingle);
        NewItemOtherChar.AddCharacters(NonWhitespaceCharacterList);
        NewItemOtherChar.AddSuperscripts(CellLoader.AllSuperscripts);
        NewItemOtherChar.AddSubscripts(CellLoader.AllSubscripts);
        NewItemOtherChar.AddFontSizes(font.FontSizeList);

        itemList.Add(NewItemOtherChar);
    }

    private static ScanSpace? FirstLetterScanSpace = null;
    private static ScanSpace? OtherLetterScanSpace = null;
    private static ScanSpace? NextLetterScanSpace = null;
    #endregion

    #region Optimization
    public static void InitializePreferredFonts(List<double> preferredFontSizeList)
    {
        FirstSearch.InitializePreferredFonts(preferredFontSizeList);
        OtherSearch.InitializePreferredFonts(preferredFontSizeList);
        NextSearch.InitializePreferredFonts(preferredFontSizeList);
    }

    public static void CollectUsedFonts(List<double> fontSizeList)
    {
        foreach (double FontSize in FirstSearch.PreferredLetterFontSizeList)
            AddUsedFonts(fontSizeList, FontSize);
        foreach (double FontSize in OtherSearch.PreferredLetterFontSizeList)
            AddUsedFonts(fontSizeList, FontSize);
        foreach (double FontSize in FirstSearch.UsedLetterFontSizeList)
            AddUsedFonts(fontSizeList, FontSize);
        foreach (double FontSize in OtherSearch.UsedLetterFontSizeList)
            AddUsedFonts(fontSizeList, FontSize);

        foreach (double FontSize in NextSearch.PreferredLetterFontSizeList)
            AddUsedFonts(fontSizeList, FontSize);
        foreach (double FontSize in NextSearch.UsedLetterFontSizeList)
            AddUsedFonts(fontSizeList, FontSize);
    }

    private static void AddUsedFonts(List<double> fontSizeList, double fontSize)
    {
        if (!fontSizeList.Contains(fontSize))
            fontSizeList.Add(fontSize);
    }

    public static void OptimizeFromLastScan(Font font, LetterSkimmer skimmer, ScanInfo scanInfo)
    {
        if (skimmer.IsFirstLetter)
            OptimizeFirstFromLastScan(font, scanInfo);
        else
            OptimizeOtherFromLastScan(font, scanInfo);
    }

    public static void OptimizeFirstFromLastScan(Font font, ScanInfo scanInfo)
    {
        ScanSpace FirstLetterScanSpace = GetFirstLetterScanSpace(font);
        ScanSpace OtherLetterScanSpace = GetOtherLetterScanSpace(font);
        ScanSpace NextLetterScanSpace = GetNextLetterScanSpace(font);

        Letter LastLetter = scanInfo.PreviousLetter;
        LetterType LastLetterType = LastLetter.LetterType;
        double LastFontSize = LastLetterType.FontSize;

        TypeFlags LastTypeFlags = TypeFlags.Normal;
        if (LastLetterType.IsBlue)
            LastTypeFlags |= TypeFlags.Blue;
        if (LastLetterType.IsItalic)
            LastTypeFlags |= TypeFlags.Italic;
        if (LastLetterType.IsBold)
            LastTypeFlags |= TypeFlags.Bold;

        Debug.Assert(LastFontSize != 0);

        MoveFontSizeToPreferred(FirstLetterScanSpace, LastFontSize);
        MoveFontSizeToPreferred(OtherLetterScanSpace, LastFontSize);
        MoveFontSizeToPreferred(NextLetterScanSpace, LastFontSize);
        MoveLastFontSizeToFirstPosition(FirstLetterScanSpace, LastFontSize);
        MoveLastFontSizeToFirstPosition(OtherLetterScanSpace, LastFontSize);
        MoveLastFontSizeToFirstPosition(NextLetterScanSpace, LastFontSize);
        MovePreviousTypeToFirstPosition(FirstLetterScanSpace, LastTypeFlags, LastFontSize);
        MovePreviousTypeToFirstPosition(OtherLetterScanSpace, LastTypeFlags, LastFontSize);
        MovePreviousTypeToFirstPosition(NextLetterScanSpace, LastTypeFlags, LastFontSize);

        //Debug.Assert(ScanSpace.IsItemListValid(font, FirstLetterScanSpace.ItemList, false));
        //Debug.Assert(ScanSpace.IsItemListValid(font, OtherLetterScanSpace.ItemList, false));
        //Debug.Assert(ScanSpace.IsItemListValid(font, NextLetterScanSpace.ItemList, true));
    }

    public static void OptimizeOtherFromLastScan(Font font, ScanInfo scanInfo)
    {
        ScanSpace OtherLetterScanSpace = GetOtherLetterScanSpace(font);
        ScanSpace NextLetterScanSpace = GetNextLetterScanSpace(font);

        Letter LastLetter = scanInfo.PreviousLetter;
        LetterType LastLetterType = LastLetter.LetterType;
        double LastFontSize = LastLetterType.FontSize;

        TypeFlags LastTypeFlags = TypeFlags.Normal;
        if (LastLetterType.IsBlue)
            LastTypeFlags |= TypeFlags.Blue;
        if (LastLetterType.IsItalic)
            LastTypeFlags |= TypeFlags.Italic;
        if (LastLetterType.IsBold)
            LastTypeFlags |= TypeFlags.Bold;

        Debug.Assert(LastFontSize != 0);

        MoveFontSizeToPreferred(OtherLetterScanSpace, LastFontSize);
        MoveLastFontSizeToFirstPosition(OtherLetterScanSpace, LastFontSize);
        MovePreviousTypeToFirstPosition(OtherLetterScanSpace, LastTypeFlags, LastFontSize);
        MovePreviousLetterToFirstPosition(OtherLetterScanSpace, LastTypeFlags, LastFontSize, LastLetter);

        MoveFontSizeToPreferred(NextLetterScanSpace, LastFontSize);
        MoveLastFontSizeToFirstPosition(NextLetterScanSpace, LastFontSize);
        MovePreviousTypeToFirstPosition(NextLetterScanSpace, LastTypeFlags, LastFontSize);
        MovePreviousLetterToFirstPosition(NextLetterScanSpace, LastTypeFlags, LastFontSize, LastLetter);

        Letter ExpectedNextLetter = scanInfo.ExpectedNextLetter;
        if (ExpectedNextLetter != Letter.EmptyNormal)
        {
            LetterType ExpectedNextLetterType = ExpectedNextLetter.LetterType;
            double ExpectedNextLetterFontSize = ExpectedNextLetterType.FontSize;

            if (ExpectedNextLetterFontSize != LastFontSize)
                MoveLastFontSizeToFirstPosition(NextLetterScanSpace, ExpectedNextLetterFontSize);

            TypeFlags ExpectedNextTypeFlags = TypeFlags.Normal;
            if (ExpectedNextLetterType.IsBlue)
                ExpectedNextTypeFlags |= TypeFlags.Blue;
            if (ExpectedNextLetterType.IsItalic)
                ExpectedNextTypeFlags |= TypeFlags.Italic;
            if (ExpectedNextLetterType.IsBold)
                ExpectedNextTypeFlags |= TypeFlags.Bold;

            if (ExpectedNextTypeFlags != LastTypeFlags)
                MovePreviousTypeToFirstPosition(NextLetterScanSpace, LastTypeFlags, LastFontSize);

            MovePreviousLetterToFirstPosition(NextLetterScanSpace, LastTypeFlags, LastFontSize, ExpectedNextLetter);
        }

        //Debug.Assert(ScanSpace.IsItemListValid(font, OtherLetterScanSpace.ItemList, false));
        //Debug.Assert(ScanSpace.IsItemListValid(font, NextLetterScanSpace.ItemList, true));
    }

    private static void MoveFontSizeToPreferred(ScanSpace scanSpace, double lastFontSize)
    {
        List<ScanSpaceItem> ItemList = scanSpace.ItemList;
        ScanSpaceSearch Search = scanSpace.Search;

        Debug.Assert(Search.PreferredLetterFontSizeList.Count == 1);

        if (Search.PreferredLetterFontSizeList[0] == lastFontSize)
            return;

        double PreviousPreferredFontSize = Search.PreferredLetterFontSizeList[0];
        Search.UsedLetterFontSizeList.Insert(0, PreviousPreferredFontSize);
        Search.PreferredLetterFontSizeList[0] = lastFontSize;

        if (Search.UsedLetterFontSizeList.Contains(lastFontSize))
        {
            Debug.Assert(Search.UsedLetterFontSizeList.Count > 1);
            Search.UsedLetterFontSizeList.Remove(lastFontSize);

            foreach (ScanSpaceItem Item in ItemList)
                Item.RefreshDebugText();
        }
        else
        {
            foreach (ScanSpaceItem Item in ItemList)
                if (Item.FontSizeList != Search.PreferredLetterFontSizeList && Item.FontSizeList != Search.UsedLetterFontSizeList)
                    Item.RemoveFontSize(lastFontSize);
                else
                    Item.RefreshDebugText();
        }
    }

    private static void MoveLastFontSizeToFirstPosition(ScanSpace scanSpace, double lastFontSize)
    {
        List<ScanSpaceItem> ItemList = scanSpace.ItemList;

        foreach (ScanSpaceItem Item in ItemList)
        {
            IReadOnlyList<double> FontSizeList = Item.FontSizeList;

            if (FontSizeList.Count > 0 && FontSizeList[0] != lastFontSize && Item.RemoveFontSize(lastFontSize))
                Item.InsertFontSize(lastFontSize);
        }
    }

    private static void MovePreviousTypeToFirstPosition(ScanSpace scanSpace, TypeFlags typeFlags, double lastFontSize)
    {
        List<ScanSpaceItem> ItemList = scanSpace.ItemList;
        List<ScanSpaceItem> MovedItemList = new();

        foreach (ScanSpaceItem Item in ItemList)
            if (Item.FontSizeList[0] == lastFontSize && Item.TypeFlags == typeFlags && Item.FontPreference <= FontPreference.OtherUsed)
                MovedItemList.Add(Item);

        MovedItemList.Sort(SortBySingleThenCharacterPreference);

        // Apply the sort inversed
        foreach (ScanSpaceItem Item in MovedItemList)
        {
            ItemList.Remove(Item);
            ItemList.Insert(0, Item);
        }
    }

    private static void MovePreviousLetterToFirstPosition(ScanSpace scanSpace, TypeFlags typeFlags, double lastFontSize, Letter lastLetter)
    {
        List<ScanSpaceItem> ItemList = scanSpace.ItemList;
        ScanSpaceSearch Search = scanSpace.Search;

        if (lastLetter.Text.Length == 1)
        {
            char LastCharacter = lastLetter.Text[0];

            foreach (ScanSpaceItem Item in ItemList)
                if (Item.FontSizeList[0] == lastFontSize && Item.TypeFlags == typeFlags && Item.CharacterList.Contains(LastCharacter) && Item.CharacterList.Count > 1)
                {
                    Item.RemoveCharacter(LastCharacter);
                    Search.PreferredLetters.Insert(0, LastCharacter);
                    break;
                }
        }
    }

    private static int SortBySingleThenCharacterPreference(ScanSpaceItem item1, ScanSpaceItem item2)
    {
        if (item1.IsSingle && !item2.IsSingle)
            return 1;
        else if (!item1.IsSingle && item2.IsSingle)
            return -1;
        else
            return item2.CharacterPreference - item1.CharacterPreference;
    }
    #endregion
}
