namespace FontScanner;

using FontLoader;

public class ScanInfo
{
    public LetterType LetterType { get; set; } = LetterType.Normal;
    public int VerticalOffset { get; set; }
    public Letter PreviousLetter { get; set; } = Letter.EmptyNormal;
    public PixelArray PreviousMergeArray { get; set; } = PixelArray.Empty;
    public int LastLetterWidth { get; set; }
    public int LastInside { get; set; }
}
