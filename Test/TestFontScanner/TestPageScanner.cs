namespace TestFontScanner;

using FontLoader;
using FontScanner;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Bitmap = System.Drawing.Bitmap;

[TestFixture]
public class TestPageScanner
{
    [OneTimeSetUp]
    public static void InitTestSession()
    {
        TestFont = new("Test", typeof(Dummy).Assembly);
    }

    static Font TestFont = null!;
    static int TestSideMargin = 0;

    private Page LoadPage(int pageIndex)
    {
        Assembly TestAssembly = typeof(TestPageScanner).Assembly;
        using Stream PageBitmapStream = TestAssembly.GetManifestResourceStream($"{typeof(TestPageScanner).Namespace}.TestResources.Page{pageIndex}.png");
        Bitmap Bitmap = new Bitmap(PageBitmapStream);
        PageImage PageImage = new(Bitmap);
        Page NewPage = new(PageImage, typeof(Dummy).Assembly, typeof(Dummy).Namespace, TestSideMargin, checkExcludedLetter: false);

        return NewPage;
    }

    [Test]
    public void BasicTest()
    {
        Page TestPage = LoadPage(0);
        bool IsScanComplete = PageScanner.Scan(TestFont, TestPage);
        Assert.IsTrue(IsScanComplete);
    }
}
