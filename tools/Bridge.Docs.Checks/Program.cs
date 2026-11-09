using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bridge.Core;

string root = Path.GetFullPath(args[0]);
var files = Directory.GetFiles(Path.Combine(root, "docs"), "*.md", SearchOption.AllDirectories)
    .Concat(new[] { Path.Combine(root, "README.md"), Path.Combine(root, "README.ru.md"),Path.Combine(root,"THIRD-PARTY-NOTICES.md"),Path.Combine(root,"src","Bridge.Transport.Mcp","README.md") })
    .Concat(Directory.GetFiles(Path.Combine(root,"Packages"),"README.md",SearchOption.AllDirectories));
int jsonCount = 0, links = 0, pages = 0;
foreach (var file in files)
{
    pages++; string text = File.ReadAllText(file);
    foreach (Match match in Regex.Matches(text, @"(?ms)^```json\s*\r?\n(.*?)^```\s*$"))
    {
        string json = match.Groups[1].Value;
        var value = BridgeValue.Parse(json);
        if (value.Kind == BridgeValueKind.Object)
        {
            if (value.TryGetProperty("bridgeVersion", out _)) BridgeDocument.Parse(json);
            else if (value.TryGetProperty("queryVersion", out _)) BridgeQuerySpec.FromJson(json);
        }
        jsonCount++;
    }
    foreach (Match match in Regex.Matches(text, @"\[[^\]]+\]\(([^)]+)\)"))
    {
        string target = match.Groups[1].Value.Trim('<', '>').Split('#')[0];
        if (target.Length == 0 || Uri.TryCreate(target, UriKind.Absolute, out _)) continue;
        string path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, Uri.UnescapeDataString(target)));
        if (!File.Exists(path) && !Directory.Exists(path)) throw new Exception($"Broken local link in {file}: {target}");
        links++;
    }
}
Console.WriteLine($"DOCS_PASS: {pages} pages, {links} local links, {jsonCount} JSON examples");
