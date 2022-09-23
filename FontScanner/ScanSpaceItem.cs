namespace FontScanner;

using FontLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;

[DebuggerDisplay("{DebugText,nq}")]
public class ScanSpaceItem
{
    public ScanSpaceItem(Font font)
    {
        Font = font;
    }

    public Font Font { get; }
    public List<char> CharacterList { get; } = new();
    public List<string> SuperscriptList { get; } = new();
    public TypeFlags TypeFlags { get; set; }
    public bool IsSingle { get; set; }
    public List<double> FontSizeList { get; } = new();

    public bool IsValid { get { return (CharacterList.Count > 0 || SuperscriptList.Count > 0) && FontSizeList.Count > 0; } }

    public bool IsWithinSpace(char c, TypeFlags typeFlags, bool isSingle, double fontSize)
    {
        if (!CharacterList.Contains(c))
            return false;

        if (!IsWithinSpace(typeFlags, isSingle, fontSize))
            return false;

        return true;
    }

    public bool IsWithinSpace(string superscript, TypeFlags typeFlags, bool isSingle, double fontSize)
    {
        if (!SuperscriptList.Contains(superscript))
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

        if (!FontSizeList.Contains(fontSize))
            return false;

        return true;
    }

    public string DebugText
    {
        get
        {
            if (!IsValid)
                return "Invalid";

            string CharacterInterval = GetInterval(CharacterList, CellLoader.AllCharacters);
            string SuperscriptInterval = GetInterval(SuperscriptList, CellLoader.AllSuperscripts);

            string TextInterval;
            if (CharacterInterval.Length == 0)
            {
                Debug.Assert(SuperscriptInterval.Length > 0);
                TextInterval = SuperscriptInterval;
            }
            else if (SuperscriptInterval.Length == 0)
            {
                TextInterval = CharacterInterval;
            }
            else
            {
                TextInterval = $"{CharacterInterval} and {SuperscriptInterval}";
            }

            string TypeFlagsText = string.Empty;
            List<TypeFlags> TypeFlagList = new() { TypeFlags.Blue, TypeFlags.Italic, TypeFlags.Bold };
            foreach (TypeFlags EnumValue in TypeFlagList)
                if (TypeFlags.HasFlag(EnumValue))
                    TypeFlagsText = ConcatenateTypeFlag(TypeFlagsText, EnumValue);

            if (TypeFlagsText.Length == 0)
                TypeFlagsText = "Normal";

            string IsSingleText = IsSingle ? "Single" : "Partial";
            string FontSizeInterval = GetFontSizeInterval(Font, FontSizeList);

            string Result = $"{TextInterval}; {TypeFlagsText}; {IsSingleText}; {FontSizeInterval}";

            return Result;
        }
    }

    private static string GetInterval<T>(List<T> list, T[] all)
        where T: IEquatable<T>
    {
        string Result = string.Empty;
        int IndexAll = 0;
        int IndexList = 0;

        while (IndexAll < all.Length && IndexList < list.Count)
        {
            while (IndexAll < all.Length && !all[IndexAll].Equals(list[IndexList]))
                IndexAll++;

            if (IndexAll < all.Length)
            {
                int FirstIndex = IndexAll;

                while (IndexAll < all.Length && IndexList < list.Count && all[IndexAll].Equals(list[IndexList]))
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
        double FirstFontSize = fontSizeList[0];
        double LastFontSize = fontSizeList[0];

        for (int i = 1; i < fontSizeList.Count; i++)
        {
            double FontSize = fontSizeList[i];
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
}
