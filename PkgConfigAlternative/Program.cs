using CommandLine;
using NLog;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

// PKG_CONFIG_PATH=V:\static-poppler\root\lib\pkgconfig

namespace PkgConfigAlternative
{
    internal class Program
    {
        private class Opt
        {
            [Option("version")]
            public bool Version { get; set; }

            [Option("help")]
            public bool Help { get; set; }

            [Option("modversion")]
            public bool ModVersion { get; set; }

            [Option("cflags")]
            public bool Cflags { get; set; }

            [Option("libs")]
            public bool Libs { get; set; }

            [Option("exists")]
            public bool Exists { get; set; }

            [Option("print-errors")]
            public bool PrintErrors { get; set; }

            [Option("short-errors")]
            public bool ShortErrors { get; set; }

            [Option("variable")]
            public string? Variable { get; set; }

            [Option("libs-only-l")]
            public bool LibsOnlyLowerL { get; set; }

            [Option("libs-only-L")]
            public bool LibsOnlyUpperL { get; set; }

            [Option("libs-only-other")]
            public bool LibsOnlyOther { get; set; }

            [Option("cflags-only-I")]
            public bool CflagsOnlyI { get; set; }

            [Option("cflags-only-other")]
            public bool CflagsOnlyOther { get; set; }

            [Option("static")]
            public bool Static { get; set; }

            [Value(0, MetaName = "packages")]
            public IEnumerable<string>? Packages { get; set; }
        }

        static int Main(string[] args)
        {
            var parser = new Parser(
                configuration =>
                {
                    configuration.AutoVersion = false;
                    configuration.AutoHelp = false;
                }
            );

            Console.Out.NewLine = "\n";
            Console.Error.NewLine = "\n";

            var logger = LogManager.GetLogger("CommandLine");
            logger.Info(string.Join(" ", args));

            int exitCode = 0;
            parser.ParseArguments<Opt>(args)
                .WithParsed<Opt>(
                    o =>
                    {
                        if (o.Version)
                        {
                            Console.WriteLine(GetVersion());
                            return;
                        }
                        else if (o.Help)
                        {
                            Console.WriteLine("pkg-config");
                            return;
                        }

                        var packages = SplitRequires(string.Join(" ", o.Packages?.ToArray() ?? new string[0]));

                        try
                        {
                            if (o.ModVersion)
                            {
                                foreach (var package in packages)
                                {
                                    Console.WriteLine(LoadPkgConfig(package).ResolveKeyword("Version"));
                                }
                                return;
                            }

                            {
                                var tokens = new string[0]
                                        .Concat(
                                            Collect(
                                                LoadPkgConfig,
                                                packages,
                                                "Cflags",
                                                MakeCflagsFilter(o.Cflags, o.CflagsOnlyI, o.CflagsOnlyOther),
                                                o.Static
                                            )
                                        )
                                        .Concat(
                                            Collect(
                                                LoadPkgConfig,
                                                packages,
                                                "Libs",
                                                MakeLibsFilter(o.Libs, o.LibsOnlyLowerL, o.LibsOnlyUpperL, o.LibsOnlyOther),
                                                o.Static
                                            )
                                        )
                                        .ToArray();

                                if (tokens.Any())
                                {
                                    Console.WriteLine(string.Join(" ", tokens));
                                }
                            }

                            if (o.Exists)
                            {
                                Collect(LoadPkgConfig, packages, "Name", null, false);
                            }

                            if (o.Variable is string variable)
                            {
                                Console.WriteLine(
                                        string.Join(
                                            " ",
                                            packages
                                                .Select(pkg => LoadPkgConfig(pkg).ResolveVariable("${" + variable + "}"))
                                        )
                                    );
                            }
                        }
                        catch (Exception ex)
                        {
                            if (o.ShortErrors)
                            {
                                Console.Error.WriteLine($"Error: {ex.Message}");
                            }
                            else
                            {
                                Console.Error.WriteLine(ex);
                            }
                            exitCode = 1;
                        }
                    }
                )
                .WithNotParsed(
                    err =>
                    {
                        logger.Error(string.Join(" ", err.Select(it => it.Tag)));
                        Console.Error.WriteLine(string.Join(" ", err.Select(it => it.Tag)));
                        exitCode = 1;
                    }
                );

            return exitCode;
        }

        private static Func<string, bool> MakeLibsFilter(bool libs, bool libsOnlyLowerL, bool libsOnlyUpperL, bool libsOnlyOther)
        {
            return arg =>
            {
                var lowerL = arg.StartsWith("-l");
                var upperL = arg.StartsWith("-L");
                var other = !lowerL && !upperL;
                return libs || (libsOnlyLowerL ? lowerL : false) | (libsOnlyUpperL ? upperL : false) | (libsOnlyOther ? other : false);
            };
        }

        private static Func<string, bool> MakeCflagsFilter(bool cflags, bool cflagsOnlyI, bool cflagsOnlyOther)
        {
            return arg =>
            {
                var isI = arg.StartsWith("-I");
                var isOther = !isI;
                return cflags || (cflagsOnlyI ? isI : false) | (cflagsOnlyOther ? isOther : false);
            };
        }

        private static string[] Collect(
            Func<PackageSpecifier, PkgConfigFile> loadPkgConfig,
            IEnumerable<PackageSpecifier> packages,
            string keyword,
            Func<string, bool>? filter,
            bool staticOption
        )
        {
            var walked = new HashSet<string>();
            var tokens = new List<string?>();
            var targets = new Queue<PackageSpecifier>(packages);
            while (targets.Any())
            {
                var target = targets.Dequeue();
                var name = target.Name;
                if (!walked.Add(name))
                {
                    continue;
                }

                var pc = loadPkgConfig(target);
                tokens.AddRange(ApplyFilter(pc.ResolveKeyword(keyword), filter));

                foreach (var require in SplitRequires(pc.ResolveKeyword("Requires")))
                {
                    targets.Enqueue(require);
                }

                if (staticOption)
                {
                    foreach (var require in SplitRequires(pc.ResolveKeyword("Requires.private")))
                    {
                        targets.Enqueue(require);
                    }
                }
            }

            return tokens.OfType<string>().ToArray();
        }

        private record PackageSpecifier(
            string Name,
            VersionSpecifier? VersionSpecifier
        );

        private record VersionSpecifier(
            string Mark,
            string Version
        );

        private static IEnumerable<PackageSpecifier> SplitRequires(string? requires)
        {
            var parts = requires?
                .Split(new char[] { ',', ' ', '\t' })
                .Select(it => it.Trim())
                .Where(it => it.Length != 0)
                .ToArray() ?? new string[0];

            return SplitRequires(parts);
        }

        private static IEnumerable<PackageSpecifier> SplitRequires(string[] parts)
        {
            var list = new List<PackageSpecifier>();

            var marks = "=,<,>,<=,>=".Split(',');

            for (int x = 0, cx = parts.Length; x < cx;)
            {
                if (x + 2 < cx && marks.Contains(parts[x + 1]))
                {
                    list.Add(new PackageSpecifier(parts[x], new VersionSpecifier(parts[x + 1], parts[x + 2])));
                    x += 3;
                }
                else
                {
                    list.Add(new PackageSpecifier(parts[x], null));
                    x++;
                }
            }

            return list;
        }

        private static string[] ApplyFilter(string? body, Func<string, bool>? filter)
        {
            if (body == null || filter == null)
            {
                return new string[0];
            }

            return body.Split(' ', StringSplitOptions.None).Where(filter).ToArray();
        }

        private static string GetVersion() =>
            typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";

        private class PkgConfigFile
        {
            public PkgConfigFile(Dictionary<string, string> variables, Dictionary<string, string> keywords)
            {
                Variables = variables;
                Keywords = keywords;
            }

            public Dictionary<string, string> Variables { get; set; }
            public Dictionary<string, string> Keywords { get; set; }

            public string? ResolveKeyword(string key)
            {
                if (Keywords.TryGetValue(key, out string? body) && body != null)
                {
                    return ResolveVariable(body);
                }

                return null;
            }

            public string ResolveVariable(string body)
            {
                return Regex.Replace(
                    body,
                    "\\$\\{(?<name>[^\\}]+)\\}",
                    match =>
                    {
                        if (Variables.TryGetValue(match.Groups["name"].Value, out string? sub) && sub != null)
                        {
                            return ResolveVariable(sub);
                        }
                        else
                        {
                            return "";
                        }
                    }
                );
            }

            internal static PkgConfigFile From(string pcFile)
            {
                var variables = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                var keywords = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

                foreach (var line in File.ReadAllLines(pcFile))
                {
                    int mark = line.IndexOfAny(new char[] { '=', ':' });
                    if (mark != -1)
                    {
                        var isVariable = line[mark] == '=';
                        (isVariable ? variables : keywords)[line.Substring(0, mark).Trim()] = line.Substring(mark + 1).Trim();
                    }
                }

                return new PkgConfigFile(variables, keywords);
            }
        }

        private static PkgConfigFile LoadPkgConfig(PackageSpecifier package)
        {
            var PKG_CONFIG_PATH = Environment.GetEnvironmentVariable("PKG_CONFIG_PATH");
            if (string.IsNullOrEmpty(PKG_CONFIG_PATH))
            {
                throw new Exception("PKG_CONFIG_PATH is not set!");
            }

            var pcName = package.Name;

            var pcFile = PKG_CONFIG_PATH.Split(';')
                .Select(
                    pkgDir =>
                    {
                        var pcFile = Path.Combine(pkgDir, $"{pcName}.pc");
                        if (File.Exists(pcFile))
                        {
                            return pcFile;
                        }
                        else
                        {
                            return null;
                        }
                    }
                )
                .FirstOrDefault();

            if (pcFile == null)
            {
                throw new FileNotFoundException($"\"{pcName}\" not found from PKG_CONFIG_PATH!");
            }

            var pc = PkgConfigFile.From(pcFile);

            if (package.VersionSpecifier is VersionSpecifier versionSpecifier)
            {
                static bool TestVersion(string targetVersion, VersionSpecifier versionSpecifier)
                {
                    static string Normalizer(string text) => Regex.Replace(text, "\\d+", match => match.Value.PadLeft(10, '0'));

                    var triple = Normalizer(targetVersion).CompareTo(Normalizer(versionSpecifier.Version));

                    switch (versionSpecifier.Mark)
                    {
                        case "=": return triple == 0;
                        case "<": return triple < 0;
                        case ">": return triple > 0;
                        case "<=": return triple <= 0;
                        case ">=": return triple >= 0;
                        default: throw new ArgumentException(versionSpecifier.Mark);
                    }
                }

                var targetVersion = pc.ResolveKeyword("Version") ?? "0";

                if (!TestVersion(targetVersion, versionSpecifier))
                {
                    throw new FileNotFoundException($"{package.Name}@{targetVersion} does not meet condition: {versionSpecifier.Mark} {versionSpecifier.Version}");
                }
            }

            return pc;
        }
    }
}