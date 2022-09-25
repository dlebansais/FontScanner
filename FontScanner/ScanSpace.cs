﻿namespace FontScanner;

using FontLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class ScanSpace
{
    public ScanSpace(Font font, List<ScanSpaceItem> itemList, ScanSpaceSearch search, bool isSingleOnly)
    {
        if (!IsItemListValid(font, itemList, isSingleOnly))
            throw new ArgumentException("Invalid list", nameof(itemList));

        Font = font;
        ItemList = itemList;
        Search = search;
    }

    public Font Font { get; }
    public List<ScanSpaceItem> ItemList { get; }
    public ScanSpaceSearch Search { get; }

    private bool IsItemListValid(Font font, List<ScanSpaceItem> itemList, bool isSingleOnly)
    {
        if (itemList.Count == 0)
            return false;

        foreach (ScanSpaceItem Item in itemList)
            if (!Item.IsValid)
                return false;

        string DebugText = itemList[0].DebugText;

        int CharacterCount = CellLoader.AllCharacters.Count + CellLoader.AllSuperscripts.Count;

        int TypeFlagBits = typeof(TypeFlags).GetEnumValues().Length - 1;
        int TypeFlagCount = (1 << TypeFlagBits) - 1;

        int IsSingleCount = isSingleOnly ? 1 : 2;

        List<double> FontSizeList = new();
        foreach (LetterType LetterType in font.SupportedLetterTypes)
        {
            double FontSize = LetterType.FontSize;
            if (!FontSizeList.Contains(FontSize))
                FontSizeList.Add(FontSize);
        }
        int FontSizeCount = FontSizeList.Count;

        bool[,,,] IsTaken = new bool[CharacterCount, TypeFlagCount, IsSingleCount, FontSizeCount];
        int TakenTotal = 0;

        foreach (ScanSpaceItem Item in itemList)
        {
            for (int i = 0; i < CharacterCount; i++)
            {
                for (int j = 0; j < TypeFlagCount; j++)
                {
                    for (int k = 0; k < IsSingleCount; k++)
                    {
                        for (int l = 0; l < FontSizeCount; l++)
                        {
                            TypeFlags TypeFlags = (TypeFlags)j;
                            bool IsSingle = (k == 0);
                            double FontSize = FontSizeList[l];

                            bool IsWithinSpace;
                            if (i < CellLoader.AllCharacters.Count)
                                IsWithinSpace = Item.IsWithinSpace(CellLoader.AllCharacters[i], TypeFlags, IsSingle, FontSize);
                            else
                                IsWithinSpace = Item.IsWithinSpace(CellLoader.AllSuperscripts[i - CellLoader.AllCharacters.Count], TypeFlags, IsSingle, FontSize);

                            if (IsWithinSpace)
                            {
                                if (IsTaken[i, j, k, l])
                                    return false;

                                IsTaken[i, j, k, l] = true;
                                TakenTotal++;
                            }
                        }
                    }
                }
            }
        }

        if (TakenTotal != CharacterCount * TypeFlagCount * IsSingleCount * FontSizeCount)
            return false;

        return true;
    }
}
