namespace FontScanner;

using System.Collections.Generic;

public class ScanSpaceSearch
{
    public ScanSpaceSearch(List<char> extraPreferredLetters)
    {
        PreferredLetters.AddRange(extraPreferredLetters);
    }

    public List<double> PreferredLetterFontSizeList = new() { 88 }; // 109
    public List<double> UsedLetterFontSizeList = new() { 136 }; // 211, 91, 154, 166
    public List<double> AllowedFontSizeList = new() { 88, 91, 136 };

    public List<char> PreferredLetters = new()
    {
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
    };

    public void InitializePreferredFonts(List<double> preferredFontSizeList)
    {
        if (preferredFontSizeList.Count == 0)
        {
            return;
        }
        else if (preferredFontSizeList.Count == 1)
        {
            if (PreferredLetterFontSizeList[0] != preferredFontSizeList[0])
            {
                UsedLetterFontSizeList.Clear();
                UsedLetterFontSizeList.Add(PreferredLetterFontSizeList[0]);
                PreferredLetterFontSizeList[0] = preferredFontSizeList[0];
            }
        }
        else
        {
            PreferredLetterFontSizeList[0] = preferredFontSizeList[0];
            UsedLetterFontSizeList.Clear();

            for (int i = 1; i < preferredFontSizeList.Count; i++)
                UsedLetterFontSizeList.Add(preferredFontSizeList[i]);
        }
    }

}
