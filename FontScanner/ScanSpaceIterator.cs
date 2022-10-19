namespace FontScanner;

using FontLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class ScanSpaceIterator
{
    public ScanSpaceIterator(ScanSpace scanSpace)
    {
        ScanSpace = scanSpace;
        ItemList = ScanSpace.ItemList;
        Reset();
    }

    public ScanSpace ScanSpace { get; }
    public List<ScanSpaceItem> ItemList { get; }
    public int ItemIndex { get; private set; }
    public int CharacterIndex { get; private set; }
    public int FontSizeIndex { get; private set; }
    public CharacterPreferenceNew HighestCharacterPreferenceReached { get; set; }
    public List<TypeFlags> HighestTypeFlagsMixReached { get; } = new();
    public FontPreference HighestFontPreferenceReached { get; set; }

    public bool IsEnumerationValid { get { return ItemIndex >= 0 && !IsCompleted; } }
    public bool IsCompleted { get { return ItemIndex >= ItemList.Count; } }

    public ScanSpaceItem CurrentItem
    {
        get
        {
            CheckThatEnumerationIsValid();

            return ItemList[ItemIndex];
        }
    }

    public Letter CurrentLetter
    {
        get
        {
            CheckThatEnumerationIsValid();

            ScanSpaceItem Item = ItemList[ItemIndex];

            Debug.Assert(FontSizeIndex >= 0 && FontSizeIndex < Item.FontSizeList.Count);
            double FontSize = Item.FontSizeList[FontSizeIndex];

            TypeFlags TypeFlags = Item.TypeFlags;
            bool IsBlue = TypeFlags.HasFlag(TypeFlags.Blue);
            bool IsItalic = TypeFlags.HasFlag(TypeFlags.Italic);
            bool IsBold = TypeFlags.HasFlag(TypeFlags.Bold);

            LetterType CurrentLetterType = new(FontSize, IsBlue, IsItalic, IsBold);

            Debug.Assert(CharacterIndex >= 0 && CharacterIndex < Item.CharacterList.Count + Item.SuperscriptList.Count + Item.SubscriptList.Count);

            Letter Result;

            if (CharacterIndex < Item.CharacterList.Count)
                Result = new(Item.CharacterList[CharacterIndex], CurrentLetterType);
            else if (CharacterIndex < Item.CharacterList.Count + Item.SuperscriptList.Count)
                Result = new(Item.SuperscriptList[CharacterIndex - Item.CharacterList.Count], CurrentLetterType);
            else
                Result = new(Item.SubscriptList[CharacterIndex - Item.CharacterList.Count - Item.SuperscriptList.Count], CurrentLetterType);

            return Result;
        }
    }

    public bool IsAtLastCharacterOfCurrentItem
    {
        get
        {
            if (ItemIndex < 0 || ItemIndex >= ItemList.Count)
                return false;

            ScanSpaceItem Item = ItemList[ItemIndex];
            if (CharacterIndex + 1 != Item.CharacterList.Count)
                return false;

            if (FontSizeIndex + 1 != Item.FontSizeList.Count)
                return false;

            return true;
        }
    }

    public bool IsAtLastCharacterOfLastItem
    {
        get
        {
            return ItemIndex + 1 == ItemList.Count && IsAtLastCharacterOfCurrentItem;
        }
    }

    private void CheckThatEnumerationIsValid()
    {
        if (!IsEnumerationValid)
            throw new InvalidOperationException("Enumeration has either not started or has already finished.");
    }

    public void Reset()
    {
        ItemIndex = -1;
        CharacterIndex = 0;
        FontSizeIndex = 0;
    }

    public bool MoveNext()
    {
        return MoveNextInternal(IteratorMoveOption.Default);
    }

    public bool MoveNext(IteratorMoveOption option)
    {
        return MoveNextInternal(option);
    }

    private bool MoveNextInternal(IteratorMoveOption option)
    {
        if (!Increment(option))
            return false;

        Debug.Assert(ItemIndex >= 0 && ItemIndex < ItemList.Count);
        ScanSpaceItem Item = ItemList[ItemIndex];

        Debug.Assert(CharacterIndex < Item.CharacterList.Count + Item.SuperscriptList.Count + Item.SubscriptList.Count);
        Debug.Assert(FontSizeIndex < Item.FontSizeList.Count);

        if (HighestCharacterPreferenceReached < Item.CharacterPreference)
            HighestCharacterPreferenceReached = Item.CharacterPreference;

        if (HighestFontPreferenceReached < Item.FontPreference)
            HighestFontPreferenceReached = Item.FontPreference;

        TypeFlags TypeFlags = Item.TypeFlags;

        if (!HighestTypeFlagsMixReached.Contains(TypeFlags))
            HighestTypeFlagsMixReached.Add(TypeFlags);

        return true;
    }

    private bool Increment(IteratorMoveOption option)
    {
        if (ItemIndex < 0)
            return IncrementFirstTime();
        else if (ItemIndex < ItemList.Count)
            return IncrementValidItem(option);
        else
        {
            Debug.Assert(IsCompleted);
            return false;
        }
    }

    private bool IncrementFirstTime()
    {
        Debug.Assert(ItemIndex < 0);
        ItemIndex = 0;
        CharacterIndex = 0;
        FontSizeIndex = 0;

        return true;
    }

    private bool IncrementValidItem(IteratorMoveOption option)
    {
        Debug.Assert(IsEnumerationValid);
        ScanSpaceItem Item = ItemList[ItemIndex];

        if ((Item.IsSingle && option == IteratorMoveOption.PartialOnly) || (!Item.IsSingle && option == IteratorMoveOption.SingleOnly))
            return IncrementDirectlyToNextItem();
        else
            return IncrementToNextCharacter();
    }

    private bool IncrementDirectlyToNextItem()
    {
        return IncrementToNextItem();
    }

    private bool IncrementToNextCharacter()
    {
        Debug.Assert(IsEnumerationValid);
        ScanSpaceItem Item = ItemList[ItemIndex];

        CharacterIndex++;

        if (CharacterIndex >= Item.CharacterList.Count + Item.SuperscriptList.Count + Item.SubscriptList.Count)
            return IncrementToNextFontSize();
        else
            return true;
    }

    private bool IncrementToNextFontSize()
    {
        Debug.Assert(IsEnumerationValid);
        ScanSpaceItem Item = ItemList[ItemIndex];

        CharacterIndex = 0;

        List<double> AllowedFontSizeList = ScanSpace.Search.AllowedFontSizeList;

        for(;;)
        {
            FontSizeIndex++;

            if (FontSizeIndex >= Item.FontSizeList.Count)
                return IncrementToNextItem();
            else
            {
                double FontSize = Item.FontSizeList[FontSizeIndex];

                if (AllowedFontSizeList.Contains(FontSize))
                    return true;
            }
        }
    }

    private bool IncrementToNextItem()
    {
        CharacterIndex = 0;
        FontSizeIndex = 0;
        ItemIndex++;

        if (ItemIndex < ItemList.Count)
            return true;
        else
            return false;
    }

    public bool IsNotCompatibleWithPrimary(ScanSpaceIterator mainIterator, out ScanSpaceItem incompatibleItem)
    {
        if (IsCompleted)
        {
            incompatibleItem = ItemList[ItemList.Count - 1];
            return true;
        }

        if (IsAtLastCharacterOfCurrentItem && ItemIndex + 1 < ItemList.Count)
        {
            ScanSpaceItem NextItem = ItemList[ItemIndex + 1];

            if (!mainIterator.IsItemCompatibleWith(NextItem))
            {
                incompatibleItem = NextItem;
                return true;
            }
        }

        incompatibleItem = null!;
        return false;
    }

    public bool IsItemCompatibleWith(ScanSpaceItem item)
    {
        if (item.CharacterPreference > HighestCharacterPreferenceReached)
            return false;

        if (item.FontPreference > HighestFontPreferenceReached)
            return false;

        if (!HighestTypeFlagsMixReached.Contains(item.TypeFlags))
            return false;

        return true;
    }
}
