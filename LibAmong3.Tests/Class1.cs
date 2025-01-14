using CoffReader;
using LibAmong3.Helpers;
using NUnit.Framework;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace LibAmong3.Tests
{
    public class Class1
    {
        [Test]
        [Ignore("TDD")]
        public void MakeCoffHelperTest()
        {
            var buf = new MakeCoffHelper().Make(
                [
                    (0xAA64, ".obj", new byte[] { 0x12, 0x34, }),
                    (0xA641, ".obj", new byte[] { 0x56, 0x78, }),
                ]
            );
            File.WriteAllBytes("a.obj", buf);
        }

        [Test]
        [Ignore("TDD")]
        public void Test()
        {
            var file = File.ReadAllBytes(
                @"V:\psqlodbc-for-win10-arm64\openssl\apps\lib\libapps-lib-app_libctx.obj"
            );

            var coff = CoffParser.Parse(file, true);
        }

        [Test]
        public void RewriteVcxprojForArm64x()
        {
            var linkParser = new ParseLinkArgHelper();

            var slnDir = @"V:\psqlodbc-for-win10-arm64\postgres\build-17-1-arm64x-release";
            foreach (var xmlFile in Directory.GetFiles(slnDir, "*.vcxproj", SearchOption.AllDirectories))
            {
                try
                {
                    var anyChanges = false;
                    var xml = XDocument.Load(xmlFile);
                    var msbuild = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");

                    {
                        // To build ARM64X EXE/DLL/LIB, we need to add ProjectConfiguration for arm64ec.

                        var hits = xml
                            .Elements(msbuild + "Project")
                            .Elements(msbuild + "ItemGroup")
                            .Elements(msbuild + "ProjectConfiguration")
                            .ToArray()
                            ;
                        if (true
                            && hits.Any(it => it.Attribute("Include")?.Value == "release|arm64")
                            && hits.Any(it => it.Attribute("Include")?.Value == "debug|arm64")
                        )
                        {
                            if (hits.Count() == 2)
                            {
                                var ItemGroup = hits.First().Parent!;
                                ItemGroup.Add(
                                    new XElement(
                                        msbuild + "ProjectConfiguration"
                                        , new XAttribute("Include", "release|arm64ec")
                                        , new XElement(msbuild + "Configuration", "release")
                                        , new XElement(msbuild + "Platform", "arm64ec")
                                    )
                                );
                                ItemGroup.Add(
                                    new XElement(
                                        msbuild + "ProjectConfiguration"
                                        , new XAttribute("Include", "debug|arm64ec")
                                        , new XElement(msbuild + "Configuration", "debug")
                                        , new XElement(msbuild + "Platform", "arm64ec")
                                    )
                                );
                                anyChanges = true;
                            }
                        }
                        else
                        {
                            throw new Exception("ProjectConfiguration release|arm64 debug|arm64 not found.");
                        }
                    }

                    {
                        // To build ARM64 EXE/DLL/LIB, BuildAsX must be true.

                        var PropertyGroup = xml
                            .Elements(msbuild + "Project")
                            .Elements(msbuild + "PropertyGroup")
                            .FirstOrDefault(it => it.Attribute("Label")?.Value == "Globals")
                            ;
                        if (PropertyGroup != null)
                        {
                            var BuildAsX = PropertyGroup
                                .Elements(msbuild + "BuildAsX")
                                .ToArray();

                            if (BuildAsX.Any() && BuildAsX.All(it => it.Value == "true"))
                            {
                                // already done
                            }
                            else
                            {
                                if (BuildAsX.Any())
                                {
                                    foreach (var one in BuildAsX)
                                    {
                                        one.Remove();
                                    }
                                }

                                {
                                    PropertyGroup.Add(
                                        new XElement(
                                            msbuild + "BuildAsX"
                                            , new XText("true")
                                        )
                                    );
                                    anyChanges = true;
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("PropertyGroup Label=Globals not found.");
                        }
                    }

                    {
                        // To build ARM64X EXE/DLL/LIB, MachineARM64 must be changed to MachineARM64X.

                        var TargetMachine = xml
                            .Elements(msbuild + "Project")
                            .Elements(msbuild + "ItemDefinitionGroup")
                            .Elements(msbuild + "Link")
                            .Elements(msbuild + "TargetMachine")
                            .Where(it => it.Value == "MachineARM64")
                            ;
                        foreach (var one in TargetMachine)
                        {
                            one.Value = "MachineARM64X";
                            anyChanges = true;
                        }
                    }

                    {
                        // To build ARM64X EXE/DLL, corresponding `/defArm64Native:xxx` is required for each `/def:xxx`.

                        var AdditionalOptions = xml
                            .Elements(msbuild + "Project")
                            .Elements(msbuild + "ItemDefinitionGroup")
                            .Elements(msbuild + "Link")
                            .Elements(msbuild + "AdditionalOptions")
                            ;
                        foreach (var one in AdditionalOptions)
                        {
                            var optChanged = false;

                            var linkArgs = one.Value.Split(' ')
                                .Select(linkParser.Parse)
                                .ToList();
                            var defs = linkArgs
                                .Where(it => it.Def.Length != 0)
                                .ToArray();
                            if (true
                                && defs.Count() != 0
                                && !linkArgs.Any(it => it.DefArm64Native.Length != 0)
                            )
                            {
                                linkArgs.AddRange(
                                    defs
                                        .Select(it => linkParser.Parse($"/defArm64Native:{it.Def}"))
                                );
                                optChanged = true;
                            }

                            if (optChanged)
                            {
                                one.Value = string.Join(
                                    " ",
                                    linkArgs
                                        .OrderBy(it => it, MoveAdditionalOptionsToLast.Instance)
                                        .Select(it => it.Value)
                                );
                                anyChanges = true;
                            }
                        }
                    }

                    {
                        // To build ARM64X DLL/EXE/LIB, we need to separate the obj files place of ARM64 and ARM64EC.

                        var IntDir = xml
                            .Elements(msbuild + "Project")
                            .Elements(msbuild + "PropertyGroup")
                            .Where(it => (it.Attribute("Condition")?.Value ?? "") == "")
                            .Elements(msbuild + "IntDir")
                            ;
                        if (IntDir.Any())
                        {
                            foreach (var one in IntDir)
                            {
                                // <IntDir>886b0b2@@libpq@sha\</IntDir>
                                foreach (var conf in "release|arm64,debug|arm64,release|arm64ec,debug|arm64ec".Split(',').Reverse())
                                {
                                    var value = one.Value.TrimEnd('\\') + $"\\{conf.Replace("|", "_")}\\";

                                    one.Parent!.AddAfterSelf(
                                        new XElement(
                                            msbuild + "PropertyGroup"
                                            , new XAttribute("Condition", $" '$(Configuration)|$(Platform)'=='{conf}' ")
                                            , new XElement(
                                                msbuild + "IntDir"
                                                , new XText(value)
                                            )
                                        )
                                    );
                                }

                                one.Remove();

                                anyChanges = true;
                            }
                        }
                    }

                    //{
                    //    // <Object /> must not exist at LIB projects, in order to build EXE/DLL.
                    //    // Convert them to <ProjectReference Include="...">
                    //    var Objects = xml
                    //        .Elements(msbuild + "Project")
                    //        .Elements(msbuild + "ItemGroup")
                    //        .Elements(msbuild + "Object")
                    //        .Where(it => it.Attribute("Include")?.Value?.EndsWith(".obj") ?? false)
                    //        .ToArray()
                    //        ;
                    //    foreach (var one in Objects)
                    //    {
                    //        var objRelPath = one.Attribute("Include")!.Value;
                    //        var objFullPath = Path.GetFullPath(
                    //            Path.Combine(
                    //                Path.GetDirectoryName(xmlFile)!,
                    //                objRelPath
                    //            )
                    //        );
                    //        var dependentProjFile = Path.GetDirectoryName(objFullPath) + ".vcxproj";

                    //        if (File.Exists(dependentProjFile))
                    //        {
                    //            one.Name = msbuild + "Object__";

                    //            var newInclude = Path.GetDirectoryName(objRelPath)! + ".vcxproj";

                    //            var already = xml
                    //                .Elements(msbuild + "Project")
                    //                .Elements(msbuild + "ItemGroup")
                    //                .Elements(msbuild + "ProjectReference")
                    //                .Where(it => it.Attribute("Include")?.Value == newInclude);

                    //            if (already.Any())
                    //            {
                    //                // won't add
                    //            }
                    //            else
                    //            {
                    //                xml
                    //                    .Element(msbuild + "Project")!
                    //                    .Add(
                    //                        new XElement(
                    //                            msbuild + "ItemGroup",
                    //                            new XElement(
                    //                                msbuild + "ProjectReference"
                    //                                , new XAttribute("Include", newInclude)
                    //                                , new XElement(
                    //                                    msbuild + "LinkLibraryDependencies"
                    //                                    , new XText("false")
                    //                                )
                    //                            )
                    //                        )
                    //                    );

                    //                anyChanges = true;
                    //            }
                    //        }
                    //    }
                    //}


                    //{
                    //    // To build ARM64 LIB/DLL/EXE, both ARM64 and ARM64EC object files are needed.

                    //    var Objects = xml
                    //        .Elements(msbuild + "Project")
                    //        .Elements(msbuild + "ItemGroup")
                    //        .Where(it => string.IsNullOrWhiteSpace(it.Attribute("Condition")?.Value))
                    //        .Elements(msbuild + "Object")
                    //        .Where(it => !string.IsNullOrWhiteSpace(it.Attribute("Include")?.Value))
                    //        .ToArray()
                    //        ;
                    //    var subDirNames = "release_arm64,release_arm64ec".Split(',');
                    //    foreach (var one in Objects)
                    //    {
                    //        // <Object Include="..\..\src/common\349de3c@@libpgcommon_ryu@sta\d2s.c.obj" />
                    //        // <Object Include="..\..\..\src\bin\pgevent\release_arm64ec\src_bin_pgevent_pgmsgevent.rc_pgmsgevent.res" />
                    //        var include = one.Attribute("Include")!.Value!;
                    //        if (!subDirNames.Any(include.Contains))
                    //        {
                    //            foreach (var subDirName in subDirNames)
                    //            {
                    //                one.AddAfterSelf(
                    //                    new XElement(
                    //                        msbuild + "Object"
                    //                        , new XAttribute("Include", AttachIntDir(include, subDirName))
                    //                    )
                    //                );
                    //            }

                    //            one.Remove();
                    //            anyChanges = true;
                    //        }
                    //    }
                    //}

                    {
                        // For StaticLibrary vcxprojs, `<EmbedManifest>false</EmbedManifest>` must not exist.
                        //
                        // Otherwise it causes terrible build errors.
                        // ```
                        // 1>349de3c@@libpgcommon@sta\release_arm64\libpgcommon.a.intermediate.manifest : general error c1010070: Failed to load and parse the manifest.
                        // ``

                        var PropertyGroup = xml
                            .Elements(msbuild + "Project")
                            .Elements(msbuild + "PropertyGroup")
                            .FirstOrDefault(it => it.Attribute("Label")?.Value == "Configuration")
                            ;
                        if (PropertyGroup != null)
                        {
                            var ConfigurationType = PropertyGroup
                                .Elements(msbuild + "ConfigurationType");
                            if (ConfigurationType.Any(it => it.Value == "StaticLibrary"))
                            {
                                var EmbedManifest = xml
                                    .Elements(msbuild + "Project")
                                    .Elements(msbuild + "PropertyGroup")
                                    .Elements(msbuild + "EmbedManifest")
                                    .Where(it => it.Value == "false")
                                    .ToArray()
                                    ;
                                foreach (var one in EmbedManifest)
                                {
                                    one.Remove();
                                    anyChanges = true;
                                }
                            }
                        }
                    }

                    //{
                    //    var CLInclude = xml
                    //        .Elements(msbuild + "Project")
                    //        .Elements(msbuild + "ItemGroup")
                    //        .Elements(msbuild + "CLInclude")
                    //        .Where(it => it.Attribute("Include")?.Value?.EndsWith(".rc") ?? false)
                    //        .ToArray()
                    //        ;
                    //    foreach (var one in CLInclude)
                    //    {
                    //        // <CLInclude Include="dfd93ca@@libpgtypes@sha\win32ver.rc" />

                    //        one.AddAfterSelf(
                    //            new XElement(
                    //                msbuild + "ResourceCompile"
                    //                , new XAttribute("Include", one.Attribute("Include")!.Value)
                    //            )
                    //        );

                    //        one.Remove();
                    //        anyChanges = true;
                    //    }
                    //}

                    {
                        // To build ARM64X DLL/EXE, we need to prevent from including RC files twice.

                        var CustomBuild = xml
                            .Elements(msbuild + "Project")
                            .Elements(msbuild + "ItemGroup")
                            .Elements(msbuild + "CustomBuild")
                            .Where(it => true
                                && (it.Attribute("Include")?.Value?.EndsWith(".rc") ?? false)
                                && (it.Attribute("Condition")?.Value ?? "") == ""
                            )
                            .ToArray()
                            ;
                        foreach (var one in CustomBuild)
                        {
                            // <CustomBuild Include="..\..\..\..\..\src/port/win32ver.rc">
                            // <CustomBuild Include="..\..\..\..\..\src/port/win32ver.rc" Condition=" '$(Configuration)|$(Platform)'=='release|arm64' or '$(Configuration)|$(Platform)'=='debug|arm64' ">

                            one.SetAttributeValue(
                                "Condition",
                                " '$(Configuration)|$(Platform)'=='release|arm64' or '$(Configuration)|$(Platform)'=='debug|arm64' "
                            );

                            anyChanges = true;
                        }
                    }

                    if (anyChanges)
                    {
                        xml.Save(xmlFile);

                        Console.WriteLine($"OK {xmlFile}");
                    }
                    else
                    {
                        Console.WriteLine($"-- {xmlFile}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"NG {xmlFile} `{ex.Message}`");
                }
            }
        }

        private object AttachIntDir(string include, string subDirName)
        {
            // Include="..\..\src/common\349de3c@@libpgcommon_ryu@sta\d2s.c.obj"
            include = include.Replace("/", "\\");
            var parts = include.Split('\\').ToList();
            parts.Insert(parts.Count - 1, subDirName);
            return string.Join("\\", parts);
        }

        private class MoveAdditionalOptionsToLast : IComparer<LinkArg>
        {
            public static readonly MoveAdditionalOptionsToLast Instance = new MoveAdditionalOptionsToLast();

            private readonly string _mark = "%(AdditionalOptions)";

            public int Compare(LinkArg? x, LinkArg? y)
            {
                return Classify(x?.Value).CompareTo(Classify(y?.Value));
            }

            private int Classify(string? value)
            {
                return (value == _mark) ? 1 : 0;
            }
        }

        [Test]
        public void RewriteSlnForArm64x()
        {
            var dir = @"V:\psqlodbc-for-win10-arm64\postgres\build-17-1-arm64x-release";
            foreach (var slnFile in Directory.GetFiles(dir, "postgresql.sln", SearchOption.AllDirectories))
            {
                var body = File.ReadAllText(slnFile);

                body = body
                    //.Replace(".release|arm64.", ".release|arm64.")
                    //.Replace("= release|arm64", "= release|arm64")
                    .Replace("= release|x64", "= release|arm64")
                    ;

                File.WriteAllText(slnFile, body, new UTF8Encoding(true));
            }
        }
    }
}
