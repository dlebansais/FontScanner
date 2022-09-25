namespace FontScanner;

using FontLoader;
using System.Collections.Generic;
using System.Diagnostics;

public class ScanSpaceIterator
{
    public ScanSpaceIterator(ScanSpace scanSpace)
    {
        ScanSpace = scanSpace;
        ItemList = ScanSpace.ItemList;
        CurrentLetter = Letter.EmptyNormal;
        ItemCount = ItemList.Count;
        Reset();
    }

    public ScanSpace ScanSpace { get; }
    public List<ScanSpaceItem> ItemList { get; }
    public Letter CurrentLetter { get; private set; }
    public bool IsSingle { get; private set; }
    public int ItemIndex { get; private set; }
    public int ItemCount { get; }
    public CharacterPreferenceNew HighestCharacterPreferenceReached { get; set; }
    public List<TypeFlags> HighestTypeFlagsMixReached { get; } = new();
    public FontPreference HighestFontPreferenceReached { get; set; }

    public void Reset()
    {
        ItemIndex = 0;
        CharacterIndex = 0;
        FontSizeIndex = 0;
    }

    public bool MoveNext()
    {
        return MoveNextInternal(IteratorMoveOption.Default);
    }

    public bool MoveNextNoWhitespace()
    {
        do
        {
            if (!MoveNextInternal(IteratorMoveOption.Default))
                return false;
        }
        while (CurrentLetter.IsWhitespace);

        return true;
    }

    public bool MoveNextNoWhitespaceSingleOnly()
    {
        do
        {
            if (!MoveNextInternal(IteratorMoveOption.SingleOnly))
                return false;
        }
        while (CurrentLetter.IsWhitespace || !IsSingle);

        return true;
    }

    public bool MoveNextNoWhitespacePartialOnly()
    {
        do
        {
            if (!MoveNextInternal(IteratorMoveOption.PartialOnly))
                return false;
        }
        while (CurrentLetter.IsWhitespace || IsSingle);

        return true;
    }

    private bool MoveNextInternal(IteratorMoveOption option)
    {
        if (ItemIndex >= ItemCount)
            return false;

        ScanSpaceItem Item = ItemList[ItemIndex];
        Debug.Assert(CharacterIndex < Item.CharacterList.Count + Item.SuperscriptList.Count);
        Debug.Assert(FontSizeIndex < Item.FontSizeList.Count);

        double FontSize = Item.FontSizeList[FontSizeIndex];

        TypeFlags TypeFlags = Item.TypeFlags;
        bool IsBlue = TypeFlags.HasFlag(TypeFlags.Blue);
        bool IsItalic = TypeFlags.HasFlag(TypeFlags.Italic);
        bool IsBold = TypeFlags.HasFlag(TypeFlags.Bold);

        LetterType CurrentLetterType = new(FontSize, IsBlue, IsItalic, IsBold);

        if (CharacterIndex < Item.CharacterList.Count)
            CurrentLetter = new(Item.CharacterList[CharacterIndex], CurrentLetterType);
        else
            CurrentLetter = new(Item.SuperscriptList[CharacterIndex - Item.CharacterList.Count], CurrentLetterType);

        IsSingle = Item.IsSingle;

        if (HighestCharacterPreferenceReached < Item.CharacterPreference)
            HighestCharacterPreferenceReached = Item.CharacterPreference;

        if (HighestFontPreferenceReached < Item.FontPreference)
            HighestFontPreferenceReached = Item.FontPreference;

        if (!HighestTypeFlagsMixReached.Contains(TypeFlags))
            HighestTypeFlagsMixReached.Add(TypeFlags);

        Increment(option);
        return true;
    }

    private void Increment(IteratorMoveOption option)
    {
        ScanSpaceItem Item = ItemList[ItemIndex];

        if ((Item.IsSingle && option == IteratorMoveOption.PartialOnly) || (!Item.IsSingle && option == IteratorMoveOption.SingleOnly))
        {
            ItemIndex++;
            CharacterIndex = 0;
            FontSizeIndex = 0;
        }
        else
        {
            CharacterIndex++;
            if (CharacterIndex >= Item.CharacterList.Count + Item.SuperscriptList.Count)
            {
                CharacterIndex = 0;
                FontSizeIndex++;

                if (FontSizeIndex >= Item.FontSizeList.Count)
                {
                    FontSizeIndex = 0;
                    ItemIndex++;
                }
            }
        }
    }

    public bool IsCompatibleWith(ScanSpaceIterator mainIterator)
    {
        ScanSpaceItem SecondaryIteratorItem = ItemList[ItemIndex];

        if (SecondaryIteratorItem.CharacterPreference > mainIterator.HighestCharacterPreferenceReached)
            return false;

        if (SecondaryIteratorItem.FontPreference > mainIterator.HighestFontPreferenceReached)
            return false;

        if (!mainIterator.HighestTypeFlagsMixReached.Contains(SecondaryIteratorItem.TypeFlags))
            return false;

        return true;
    }

    private int CharacterIndex;
    private int FontSizeIndex;
}
