using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TextCopy;

// ReSharper disable once CheckNamespace
internal static class Builder
{
    [STAThread]
    private static void Main(string[] args)
    {
        var dirs = args.Length > 0 ? args : new[] {@"."};

        var ignoredPatterns = new[]
        {
            @"tests?\.cs",
            @"_tests?\.cs",
            @"_should.cs",
            @"_should.solution.cs",
            @"bin\\",
            @"obj\\",
            "Builder.cs",
            @".*/tests.*"
        };
        var sources =
            dirs.SelectMany(
                dir =>
                    Directory
                        .EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
                        .Where(fn => !ignoredPatterns.Any(p => Regex.IsMatch(fn, p, RegexOptions.IgnoreCase)))
                        .Select(fn => fn.ToLower())
                        .OrderBy(fn => fn)
                        .Select(fn => new {name = fn.ToLower(), src = File.ReadAllText(fn)})).ToList();
        var exceptions = sources.Where(file => file.name.EndsWith(".solution.cs"))
            .Select(file => file.name.Replace(".solution.cs", ".cs")).ToList();

        var usings = new HashSet<string>();
        var sb = new StringBuilder();
        foreach (var file in sources)
        {
            if (exceptions.Contains(file.name))
            {
                Console.WriteLine($"skip {file.name}");
                continue;
            }

            Console.WriteLine($"use {file.name}");
            var source = file.src;
            var pattern = @"using [A-Z0-9.]+;\r?\n";
            var usingLines = Regex.Matches(source, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase)
                .Select(m => m.Value).ToList();
            foreach (var usingLine in usingLines)
            {
                if (!usingLine.StartsWith("using System"))
                {
                    var oldColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(usingLine + " in " + file.name);
                    Console.WriteLine("You can't use third party libs in your solution! Press any key and fix it!");
                    Console.ForegroundColor = oldColor;
                    Console.ReadLine();
                    Environment.Exit(255);
                }

                usings.Add(usingLine);
            }

            var sourceWithNoUsings =
                Regex.Replace(source, pattern, "", RegexOptions.Multiline | RegexOptions.IgnoreCase)
                    .Trim();
            sb.AppendLine(sourceWithNoUsings);
            sb.AppendLine();
        }

        sb.Insert(0, string.Join("", usings) + "\r\n");
        var result = sb.ToString();
        Console.WriteLine($"Length: {result.Length}");
        if (result.Length > 90000)
            result = Compress(result);
        Console.WriteLine();
        ClipboardService.SetText(result);
        Console.WriteLine("result was copied to the clipboard");
        File.WriteAllText("build.cs", result);
        Console.WriteLine("result saved to build.cs");
    }

    private static string Compress(string result)
    {
        result = result.Replace("\r\n", "\n");
        result = Regex.Replace(result, @"\n[ \t]+", "\n");
        Console.WriteLine($"after spaces compression: {result.Length}");
        return result;
    }
}