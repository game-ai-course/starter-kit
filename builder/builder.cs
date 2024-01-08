using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TextCopy;

public static class Builder
{
    [STAThread]
    private static void Main()
    {
        var root = Path.GetDirectoryName(FileHelper.PatchDirectoryName("lib"))!;
        var dirs = new[] {Path.Combine(root, "lib"), Path.Combine(root, "bot") };
        Console.WriteLine(string.Join(", ", dirs));

        var ignoredPatterns = new[]
        {
            @"bin\" + Path.DirectorySeparatorChar,
            @"obj\" + Path.DirectorySeparatorChar, 
        };
        var sources =
            dirs.SelectMany(
                dir =>
                    Directory
                        .EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
                        .Where(fn => !ignoredPatterns.Any(p => Regex.IsMatch(fn, p, RegexOptions.IgnoreCase)))
                        .Select(fn => fn.ToLower())
                        .OrderBy(fn => fn)
                        .Select(fn => new { name = fn.ToLower(), src = File.ReadAllLines(fn) })).ToList();
        var exceptions = sources.Where(file => file.name.EndsWith(".solution.cs"))
            .Select(file => file.name.Replace(".solution.cs", ".cs")).ToList();

        var usings = new HashSet<string>();
        var namespaces = new HashSet<string>();
        var sb = new StringBuilder();
        foreach (var file in sources)
        {
            sb.Append($"\n// {Path.GetFileName(file.name)}\n");
            if (exceptions.Contains(file.name))
            {
                Console.WriteLine($"skip {file.name}");
                continue;
            }

            Console.WriteLine($"use {file.name}");
            var inUsings = true;
            foreach (var line in file.src)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var trimmed = line.Trim();
                if (trimmed.StartsWith("//")) continue;
                if (inUsings && trimmed.StartsWith("global using "))
                {
                    usings.Add(trimmed);
                    continue;
                }

                if (inUsings && trimmed.StartsWith("using "))
                {
                    FailWithRedMessage(
                        $"{file.name}: Do not use 'using ...' syntax. Use global usings instead. {trimmed}");
                    continue;
                }

                if (inUsings && trimmed.StartsWith("namespace "))
                {
                    namespaces.Add(trimmed[9..].Trim(' ', '\t', ';', '{'));
                    if (namespaces.Count > 1)
                        FailWithRedMessage(
                            $"{file.name}: Use only one namespace in your solution. Now used: {string.Join(", ", namespaces)}");
                    continue;
                }

                inUsings = false;
                sb.Append(line + "\n");
            }

            sb.Append('\n');
        }

        sb.Insert(0, $"{string.Join('\n', usings)}\nnamespace {namespaces.Single()};\n");
        
        sb.Insert(0, $"// Git: Dirty={ThisAssembly.Git.IsDirty}  {ThisAssembly.Git.CommitDate} {ThisAssembly.Git.Branch} {ThisAssembly.Git.Sha}\n\n");
        sb.Insert(0, $"// {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
        var result = sb.ToString();
        Console.WriteLine($"Length: {result.Length}");
        Console.WriteLine();
        ClipboardService.SetText(result);
        Console.WriteLine("result was copied to the clipboard");
        File.WriteAllText("build.cs", result);
        Console.WriteLine("result saved to build.cs");
        TryToBuild();
    }

    private static void TryToBuild()
    {
        var p = Process.Start(@"dotnet.exe", "build bot.csproj");
        p.WaitForExit();
        if (p.ExitCode != 0)
            FailWithRedMessage("Build failed");
    }

    private static void FailWithRedMessage(string message)
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ForegroundColor = oldColor;
        //Console.ReadLine();
        Environment.Exit(255);
    }
}