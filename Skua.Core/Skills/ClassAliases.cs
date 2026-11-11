using Skua.Core.Models;

namespace Skua.Core.Skills;

/// <summary>
/// Lets a renamed copy of a class (for example the HeroMart "Archivist of Temporis", which has the same skills as the
/// Calendar class "Archivist of Time") reuse the original's saved skill profile instead of needing its own.
/// Pairs come from built-in defaults plus ClassSkillAliases.txt in the options folder, one "Variant=Original" per line.
/// An alias is only used when the variant has no profile of its own, so profiles you made yourself always win.
/// </summary>
public static class ClassAliases
{
    private const string FileName = "ClassSkillAliases.txt";

    // Both pairs are stated on the AQW wiki ("This class has the same skills as ...").
    private static readonly string[] Defaults =
    {
        "Archivist of Temporis=Archivist of Time",
        "Chrono ShadowHunter=Chrono ShadowSlayer",
    };

    private static readonly object _lock = new();
    private static Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase);
    private static DateTime _loadedStamp = DateTime.MinValue;
    private static bool _loaded;

    private static string FilePath => Path.Combine(ClientFileSources.SkuaOptionsDIR, FileName);

    private static void EnsureLoaded()
    {
        lock (_lock)
        {
            DateTime stamp = DateTime.MinValue;
            try
            {
                if (File.Exists(FilePath))
                    stamp = File.GetLastWriteTimeUtc(FilePath);
            }
            catch
            {
            }

            if (_loaded && stamp == _loadedStamp)
                return;

            Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase);
            Parse(Defaults, map);
            try
            {
                if (!File.Exists(FilePath))
                {
                    Directory.CreateDirectory(ClientFileSources.SkuaOptionsDIR);
                    File.WriteAllLines(FilePath, new[] { "# Variant=Original  (the variant uses the original class's saved skill profile)" }.Concat(Defaults));
                    stamp = File.GetLastWriteTimeUtc(FilePath);
                }
                else
                {
                    Parse(File.ReadAllLines(FilePath), map);
                }
            }
            catch
            {
                // A missing or locked file just means we use the defaults.
            }

            _aliases = map;
            _loadedStamp = stamp;
            _loaded = true;
        }
    }

    private static void Parse(IEnumerable<string> lines, Dictionary<string, string> into)
    {
        foreach (string line in lines)
        {
            string t = line.Trim();
            int eq = t.IndexOf('=');
            if (t.Length == 0 || t.StartsWith('#') || eq <= 0)
                continue;
            string variant = t[..eq].Trim();
            string original = t[(eq + 1)..].Trim();
            if (variant.Length > 0 && original.Length > 0)
                into[variant] = original;
        }
    }

    /// <summary>The original class this one copies, or null if there is no alias.</summary>
    public static string? GetOriginal(string className)
    {
        if (string.IsNullOrWhiteSpace(className))
            return null;
        EnsureLoaded();
        lock (_lock)
            return _aliases.TryGetValue(className.Trim(), out string? original) ? original : null;
    }

    /// <summary>
    /// The class name whose profile should be used: the class itself if it has one, otherwise its aliased original if
    /// that has one, otherwise the class itself.
    /// </summary>
    public static string ResolveProfileName(string className, IEnumerable<string> classesWithProfiles)
    {
        HashSet<string> have = new(classesWithProfiles, StringComparer.OrdinalIgnoreCase);
        if (have.Contains(className))
            return className;
        string? original = GetOriginal(className);
        return original is not null && have.Contains(original) ? original : className;
    }
}
