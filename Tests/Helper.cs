namespace Dynamitey.Tests
{
    /// <summary>
    /// Common base for the test fixtures. It derived from NUnit's AssertionHelper,
    /// which supplied the Expect(actual, constraint) syntax; that type was obsolete
    /// in NUnit 3 and removed in NUnit 4. The call sites now use Assert.That, so
    /// this class carries nothing — it is kept because every fixture derives from
    /// it and it is the obvious place for shared setup to land.
    /// </summary>
    public class Helper
    {

    }
}
