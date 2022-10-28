namespace FontScanner;

using FontLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;

[DebuggerDisplay("{DebugText,nq}")]
public class ScanSpaceItem
{
    public ScanSpaceItem(Font font, TypeFlags typeFlags, bool isSingle)
    {
        Font = font;
        TypeFlags = typeFlags;
        IsSingle = isSingle;
        _CharacterList = new();
        CharacterList = _CharacterList.AsReadOnly();
        _FontSizeList = new();
        FontSizeList = _FontSizeList.AsReadOnly();
        SuperscriptList = new List<string>();
        SubscriptList = new List<string>();
        _DebugText = BuildDebugText();
    }

    public ScanSpaceItem(Font font, TypeFlags typeFlags, bool isSingle, List<char> characterList, CharacterPreference characterPreference)
    {
        foreach (char c in characterList)
            Debug.Assert(!LetterHelper.IsWhitespace(c));

        Font = font;
        TypeFlags = typeFlags;
        IsSingle = isSingle;
        _CharacterList = characterList;
        CharacterList = _CharacterList.AsReadOnly();
        CharacterPreference = characterPreference;
        _FontSizeList = new();
        FontSizeList = _FontSizeList.AsReadOnly();
        SuperscriptList = new List<string>();
        SubscriptList = new List<string>();
        _DebugText = BuildDebugText();
    }

    public ScanSpaceItem(Font font, TypeFlags typeFlags, bool isSingle, List<double> fontSizeList, FontPreference fontPreference)
    {
        Font = font;
        TypeFlags = typeFlags;
        IsSingle = isSingle;
        _CharacterList = new();
        CharacterList = _CharacterList.AsReadOnly();
        _FontSizeList = fontSizeList;
        FontSizeList = _FontSizeList.AsReadOnly();
        FontPreference = fontPreference;
        SuperscriptList = new List<string>();
        SubscriptList = new List<string>();
        _DebugText = BuildDebugText();
    }

    public ScanSpaceItem(Font font, TypeFlags typeFlags, bool isSingle, List<char> characterList, CharacterPreference characterPreference, List<double> fontSizeList, FontPreference fontPreference)
    {
        foreach (char c in characterList)
            Debug.Assert(!LetterHelper.IsWhitespace(c));

        Font = font;
        TypeFlags = typeFlags;
        IsSingle = isSingle;
        _CharacterList = characterList;
        CharacterList = _CharacterList.AsReadOnly();
        CharacterPreference = characterPreference;
        _FontSizeList = fontSizeList;
        FontSizeList = _FontSizeList.AsReadOnly();
        FontPreference = fontPreference;
        SuperscriptList = new List<string>();
        SubscriptList = new List<string>();
        _DebugText = BuildDebugText();
    }

    private ScanSpaceItem(ScanSpaceItem primaryItem)
    {
        Font = primaryItem.Font;
        TypeFlags = primaryItem.TypeFlags;
        IsSingle = true;
        _CharacterList = primaryItem._CharacterList;
        CharacterList = primaryItem._CharacterList;
        CharacterPreference = primaryItem.CharacterPreference;
        _FontSizeList = primaryItem._FontSizeList;
        FontSizeList = primaryItem.FontSizeList;
        FontPreference = primaryItem.FontPreference;
        SuperscriptList = primaryItem.SuperscriptList;
        SubscriptList = primaryItem.SubscriptList;
        _DebugText = BuildDebugText();
    }

    public static ScanSpaceItem AsSecondary(ScanSpaceItem primaryItem)
    {
        ScanSpaceItem NewItem = new ScanSpaceItem(primaryItem);
        return NewItem;
    }

    public Font Font { get; }
    public TypeFlags TypeFlags { get; }
    public bool IsSingle { get; }
    public IReadOnlyList<char> CharacterList { get; }
    private List<char> _CharacterList;
    public CharacterPreference CharacterPreference { get; }
    public List<string> SuperscriptList { get; } = new();
    public List<string> SubscriptList { get; } = new();
    public IReadOnlyList<double> FontSizeList { get; }
    private List<double> _FontSizeList;
    public FontPreference FontPreference { get; }

    public bool IsValid { get { return (CharacterList.Count > 0 || SuperscriptList.Count > 0 || SubscriptList.Count > 0) && FontSizeList.Count > 0; } }

    public bool IsWithinSpace(char c, TypeFlags typeFlags, bool isSingle, double fontSize)
    {
        if (!_CharacterList.Contains(c))
            return false;

        if (!IsWithinSpace(typeFlags, isSingle, fontSize))
            return false;

        return true;
    }

    public bool IsWithinSpace(string text, TypeFlags typeFlags, bool isSingle, double fontSize)
    {
        if (!SuperscriptList.Contains(text) && !SubscriptList.Contains(text))
            return false;

        if (!IsWithinSpace(typeFlags, isSingle, fontSize))
            return false;

        return true;
    }

    private bool IsWithinSpace(TypeFlags typeFlags, bool isSingle, double fontSize)
    {
        if (TypeFlags != typeFlags)
            return false;

        if (IsSingle != isSingle)
            return false;

        if (!_FontSizeList.Contains(fontSize))
            return false;

        return true;
    }

    public void AddCharacters(List<char> characterList)
    {
        _CharacterList.AddRange(characterList);
        _DebugText = BuildDebugText();
    }

    public void AddSuperscripts(List<string> superscriptList)
    {
        SuperscriptList.AddRange(superscriptList);
        _DebugText = BuildDebugText();
    }

    public void AddSubscripts(List<string> subscriptList)
    {
        SubscriptList.AddRange(subscriptList);
        _DebugText = BuildDebugText();
    }

    public bool RemoveCharacter(char character)
    {
        bool Result = _CharacterList.Remove(character);
        _DebugText = BuildDebugText();
        return Result;
    }

    public void AddFontSizes(List<double> fontSizeList)
    {
        _FontSizeList.AddRange(fontSizeList);
        _DebugText = BuildDebugText();
    }

    public bool RemoveFontSize(double fontSize)
    {
        bool Result = _FontSizeList.Remove(fontSize);
        _DebugText = BuildDebugText();
        return Result;
    }

    public void InsertFontSize(double fontSize)
    {
        _FontSizeList.Insert(0, fontSize);
        _DebugText = BuildDebugText();
    }

    public string DebugText
    {
        get
        {
            if (_DebugText is null)
                _DebugText = BuildDebugText();

            return _DebugText;
        }
    }

    private string? _DebugText;

    public void RefreshDebugText()
    {
        _DebugText = BuildDebugText();
    }

    private string BuildDebugText()
    {
        if (!IsValid)
            return "Invalid";

        string CharacterInterval = GetInterval(RemoveNotDisplayable(_CharacterList), RemoveNotDisplayable(CellLoader.AllCharacters), SortByOrderInAllCharacters);
        string SuperscriptInterval = GetInterval(SuperscriptList, CellLoader.AllSuperscripts, SortByOrderInAllSuperscripts);
        string SubscriptInterval = GetInterval(SubscriptList, CellLoader.AllSubscripts, SortByOrderInAllSubscripts);

        string TextInterval = string.Empty;

        if (CharacterInterval.Length > 0)
            TextInterval += CharacterInterval;

        if (SuperscriptInterval.Length > 0)
        {
            if (TextInterval.Length > 0)
                TextInterval += " and ";
            TextInterval += SuperscriptInterval;
        }

        if (SubscriptInterval.Length > 0)
        {
            if (TextInterval.Length > 0)
                TextInterval += " and ";
            TextInterval += SubscriptInterval;
        }

        string TypeFlagsText = string.Empty;
        List<TypeFlags> TypeFlagList = new() { TypeFlags.Blue, TypeFlags.Italic, TypeFlags.Bold };
        foreach (TypeFlags EnumValue in TypeFlagList)
            if (TypeFlags.HasFlag(EnumValue))
                TypeFlagsText = ConcatenateTypeFlag(TypeFlagsText, EnumValue);

        if (TypeFlagsText.Length == 0)
            TypeFlagsText = "Normal";

        string IsSingleText = IsSingle ? "Single" : "Partial";
        string FontSizeInterval = GetFontSizeInterval(Font, _FontSizeList);

        string Result = $"{TextInterval}; {TypeFlagsText}; {IsSingleText}; {FontSizeInterval}";

        return Result;
    }

    private static string GetInterval<T>(List<T> list, List<T> all, Comparison<T> comparison)
        where T: IEquatable<T>
    {
        string Result = string.Empty;
        int IndexAll = 0;
        int IndexList = 0;
        List<T> SortedList = new(list);
        SortedList.Sort(comparison);

        while (IndexAll < all.Count && IndexList < SortedList.Count)
        {
            while (IndexAll < all.Count && !all[IndexAll].Equals(SortedList[IndexList]))
                IndexAll++;

            if (IndexAll < all.Count)
            {
                int FirstIndex = IndexAll;

                while (IndexAll < all.Count && IndexList < SortedList.Count && all[IndexAll].Equals(SortedList[IndexList]))
                {
                    IndexAll++;
                    IndexList++;
                }

                int LastIndex = IndexAll - 1;

                if (Result.Length > 0)
                    Result += ", ";

                if (FirstIndex == LastIndex)
                    Result += $"{all[FirstIndex]}";
                else
                    Result += $"{all[FirstIndex]} to {all[LastIndex]}";
            }
        }

        return Result;
    }

    private static string ConcatenateTypeFlag(string s, TypeFlags typeFlag)
    {
        string Result = s;
        if (s.Length > 0)
            Result += "+";

        switch (typeFlag)
        {
            default:
                break;
            case TypeFlags.Italic:
                Result += "Italic";
                break;
            case TypeFlags.Bold:
                Result += "Bold";
                break;
            case TypeFlags.Blue:
                Result += "Blue";
                break;
        }

        return Result;
    }

    private static string GetFontSizeInterval(Font font, List<double> fontSizeList)
    {
        Debug.Assert(fontSizeList.Count > 0);

        string Result = string.Empty;
        List<double> AllFontSizes = font.FontSizeList;
        List<double> SortedFontSizeList = new(fontSizeList);
        SortedFontSizeList.Sort();

        double FirstFontSize = SortedFontSizeList[0];
        double LastFontSize = SortedFontSizeList[0];

        for (int i = 1; i < SortedFontSizeList.Count; i++)
        {
            double FontSize = SortedFontSizeList[i];
            if (AllFontSizes.IndexOf(FontSize) == AllFontSizes.IndexOf(LastFontSize) + 1)
                LastFontSize = FontSize;
            else
            {
                if (Result.Length > 0)
                    Result += ", ";

                if (FirstFontSize == LastFontSize)
                    Result += $"{FirstFontSize}";
                else
                    Result += $"{FirstFontSize} to {LastFontSize}";

                FirstFontSize = FontSize;
                LastFontSize = FontSize;
            }
        }

        if (Result.Length > 0)
            Result += ", ";

        if (FirstFontSize == LastFontSize)
            Result += $"{FirstFontSize}";
        else
            Result += $"{FirstFontSize} to {LastFontSize}";

        return Result;
    }

    private static List<char> RemoveNotDisplayable(List<char> list)
    {
        List<char> Result = new();

        foreach (char c in list)
            if (!LetterHelper.IsWhitespace(c))
                Result.Add(c);

        return Result;
    }

    private static int SortByOrderInAllCharacters(char c1, char c2)
    {
        return CellLoader.AllCharacters.IndexOf(c1) - CellLoader.AllCharacters.IndexOf(c2);
    }

    private static int SortByOrderInAllSuperscripts(string item1, string item2)
    {
        return CellLoader.AllSuperscripts.IndexOf(item1) - CellLoader.AllSuperscripts.IndexOf(item2);
    }

    private static int SortByOrderInAllSubscripts(string item1, string item2)
    {
        return CellLoader.AllSubscripts.IndexOf(item1) - CellLoader.AllSubscripts.IndexOf(item2);
    }
}
