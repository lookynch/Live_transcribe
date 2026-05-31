using System.Globalization;

namespace LiveTranscribe.Core.Versioning;

/// <summary>
/// Minimal semantic-version parse + compare used to decide whether a GitHub release
/// is newer than the installed version. Pre-release tags (e.g. 1.2.0-beta.1) sort
/// below their release counterpart, per SemVer 2.0.0.
/// </summary>
public sealed class SemVer : IComparable<SemVer>
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public string? PreRelease { get; }

    public bool IsPreRelease => !string.IsNullOrEmpty(PreRelease);

    public SemVer(int major, int minor, int patch, string? preRelease = null)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = string.IsNullOrEmpty(preRelease) ? null : preRelease;
    }

    public static bool TryParse(string? input, out SemVer version)
    {
        version = new SemVer(0, 0, 0);
        if (string.IsNullOrWhiteSpace(input)) return false;

        var s = input.Trim();
        if (s.StartsWith('v') || s.StartsWith('V')) s = s[1..];

        var plus = s.IndexOf('+'); // strip build metadata
        if (plus >= 0) s = s[..plus];

        string? pre = null;
        var dash = s.IndexOf('-');
        if (dash >= 0)
        {
            pre = s[(dash + 1)..];
            s = s[..dash];
        }

        var parts = s.Split('.');
        if (parts.Length is < 1 or > 3) return false;

        int Parse(int i) => i < parts.Length &&
            int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : -1;

        var major = Parse(0);
        var minor = parts.Length > 1 ? Parse(1) : 0;
        var patch = parts.Length > 2 ? Parse(2) : 0;
        if (major < 0 || minor < 0 || patch < 0) return false;

        version = new SemVer(major, minor, patch, pre);
        return true;
    }

    public int CompareTo(SemVer? other)
    {
        if (other is null) return 1;
        var c = Major.CompareTo(other.Major);
        if (c != 0) return c;
        c = Minor.CompareTo(other.Minor);
        if (c != 0) return c;
        c = Patch.CompareTo(other.Patch);
        if (c != 0) return c;

        // A version without pre-release is greater than one with.
        if (!IsPreRelease && other.IsPreRelease) return 1;
        if (IsPreRelease && !other.IsPreRelease) return -1;
        if (!IsPreRelease && !other.IsPreRelease) return 0;
        return string.CompareOrdinal(PreRelease, other.PreRelease);
    }

    public bool IsNewerThan(SemVer other) => CompareTo(other) > 0;

    public override string ToString() =>
        IsPreRelease ? $"{Major}.{Minor}.{Patch}-{PreRelease}" : $"{Major}.{Minor}.{Patch}";
}
