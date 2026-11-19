using System.Text.RegularExpressions;

static class MarkdownLinks
{
    public static void Check(string root, string[] files, List<string> errors)
    {
        var headings = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            foreach (var (line, number) in Prose(file))
            foreach (Match match in Regex.Matches(line, "\\]\\((?<url><[^>]+>|[^ )]+)\\)|(?:href|src)=[\"'](?<url>[^\"']+)[\"']"))
            {
                var url = match.Groups["url"].Value.Trim('<', '>');
                if (url.Length == 0 || url.StartsWith("//", StringComparison.Ordinal) || Regex.IsMatch(url, @"^[a-zA-Z][\w+.-]*:")) continue;
                var parts = url.Split('#', 2);
                var relative = Uri.UnescapeDataString(parts[0].Split('?', 2)[0]);
                var target = relative.Length == 0 ? file : Path.GetFullPath(relative, Path.GetDirectoryName(file)!);
                if (!ExistsWithExactCase(root, target))
                {
                    errors.Add($"{Path.GetRelativePath(root, file)}:{number}: missing path {url}.");
                    continue;
                }
                if (parts.Length != 2 || !target.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) continue;
                if (!headings.TryGetValue(target, out var anchors)) headings.Add(target, anchors = Anchors(target));
                if (!anchors.Contains(Uri.UnescapeDataString(parts[1])))
                    errors.Add($"{Path.GetRelativePath(root, file)}:{number}: missing anchor {url}.");
            }
        }
    }

    private static bool ExistsWithExactCase(string root, string target)
    {
        var relative = Path.GetRelativePath(root, target);
        if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)) return false;
        if (relative == ".") return true;
        var parent = root;
        foreach (var name in relative.Split(Path.DirectorySeparatorChar))
        {
            if (!Directory.Exists(parent) || !Directory.EnumerateFileSystemEntries(parent).Any(path => Path.GetFileName(path) == name)) return false;
            parent = Path.Combine(parent, name);
        }
        return true;
    }

    private static HashSet<string> Anchors(string file)
    {
        var anchors = new HashSet<string>(StringComparer.Ordinal);
        var duplicates = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (line, _) in Prose(file))
        {
            foreach (Match match in Regex.Matches(line, "(?:id|name)=[\"']([^\"']+)[\"']")) anchors.Add(match.Groups[1].Value);
            var heading = Regex.Match(line, @"^ {0,3}#{1,6} +(.+?)\s*#*\s*$");
            if (!heading.Success) continue;
            var text = Regex.Replace(heading.Groups[1].Value, @"\[([^]]+)\]\([^)]+\)", "$1");
            var slug = Regex.Replace(text.ToLowerInvariant(), @"[^\w\- ]", "").Replace(' ', '-');
            if (slug.Length == 0) continue;
            var count = duplicates.GetValueOrDefault(slug);
            duplicates[slug] = count + 1;
            anchors.Add(count == 0 ? slug : $"{slug}-{count}");
        }
        return anchors;
    }

    private static IEnumerable<(string Line, int Number)> Prose(string file)
    {
        var fence = "";
        var number = 0;
        foreach (var line in File.ReadLines(file))
        {
            number++;
            var marker = Regex.Match(line, @"^ {0,3}(`{3,}|~{3,})(.*)$");
            if (marker.Success)
            {
                var run = marker.Groups[1].Value;
                if (fence.Length == 0) fence = run;
                else if (fence[0] == run[0] && run.Length >= fence.Length && string.IsNullOrWhiteSpace(marker.Groups[2].Value)) fence = "";
            }
            else if (fence.Length == 0) yield return (line, number);
        }
    }
}
