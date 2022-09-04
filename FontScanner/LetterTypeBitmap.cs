namespace FontScanner;

using System.Diagnostics;

[DebuggerDisplay("{CellSize}")]
public record LetterTypeBitmap
{
    public LetterTypeBitmap(int cellSize, int baseline, int stride, byte[] argbValues)
    {
        CellSize = cellSize;
        Baseline = baseline;
        Stride = stride;
        ArgbValues = argbValues;
    }

    public int CellSize { get; }
    public int Baseline { get; }
    public int Stride { get; }
    public byte[] ArgbValues { get; }
}
