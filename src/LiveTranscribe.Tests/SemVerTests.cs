using LiveTranscribe.Core.Versioning;

namespace LiveTranscribe.Tests;

public class SemVerTests
{
    [Theory]
    [InlineData("1.2.3", 1, 2, 3, null)]
    [InlineData("v1.2.3", 1, 2, 3, null)]
    [InlineData("V10.0.1", 10, 0, 1, null)]
    [InlineData("2.0", 2, 0, 0, null)]
    [InlineData("3", 3, 0, 0, null)]
    [InlineData("1.2.3-beta.1", 1, 2, 3, "beta.1")]
    [InlineData("1.2.3-rc1+build.42", 1, 2, 3, "rc1")]
    public void TryParse_ValidInputs(string input, int major, int minor, int patch, string? pre)
    {
        Assert.True(SemVer.TryParse(input, out var v));
        Assert.Equal(major, v.Major);
        Assert.Equal(minor, v.Minor);
        Assert.Equal(patch, v.Patch);
        Assert.Equal(pre, v.PreRelease);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("1.2.3.4")]
    [InlineData("1.x.0")]
    public void TryParse_InvalidInputs(string? input)
    {
        Assert.False(SemVer.TryParse(input, out _));
    }

    [Fact]
    public void Newer_PatchMinorMajor()
    {
        Assert.True(Parse("1.0.1").IsNewerThan(Parse("1.0.0")));
        Assert.True(Parse("1.1.0").IsNewerThan(Parse("1.0.9")));
        Assert.True(Parse("2.0.0").IsNewerThan(Parse("1.9.9")));
    }

    [Fact]
    public void Release_IsNewerThan_PreRelease()
    {
        Assert.True(Parse("1.2.0").IsNewerThan(Parse("1.2.0-beta.1")));
        Assert.False(Parse("1.2.0-beta.1").IsNewerThan(Parse("1.2.0")));
    }

    [Fact]
    public void PreRelease_OrdinalOrdering()
    {
        Assert.True(Parse("1.2.0-beta.2").IsNewerThan(Parse("1.2.0-beta.1")));
        Assert.True(Parse("1.2.0-rc.1").IsNewerThan(Parse("1.2.0-beta.9")));
    }

    [Fact]
    public void Equal_Versions_AreNotNewer()
    {
        Assert.Equal(0, Parse("1.2.3").CompareTo(Parse("1.2.3")));
        Assert.False(Parse("1.2.3").IsNewerThan(Parse("1.2.3")));
    }

    private static SemVer Parse(string s)
    {
        Assert.True(SemVer.TryParse(s, out var v));
        return v;
    }
}
