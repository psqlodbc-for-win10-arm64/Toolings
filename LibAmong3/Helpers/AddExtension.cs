using LibAmong3.Helpers.CLTargetHelpers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public static class AddExtension
    {
        public static IServiceCollection AddLibAmong3(this IServiceCollection services)
        {
            services.AddSingleton<Arm64XARHelper>();
            services.AddSingleton<Arm64XCLHelper>();
            services.AddSingleton<Arm64XLIBHelper>();
            services.AddSingleton<Arm64XLINKHelper>();
            services.AddSingleton<CLCmdHelper>();
            services.AddSingleton<DecideCLTargetTypeHelper>();
            services.AddSingleton<MakeCoffHelper>();
            services.AddSingleton<NormHelper>();
            services.AddSingleton<ParseClArgHelper>();
            services.AddSingleton<ParseLibArgHelper>();
            services.AddSingleton<ParseLinkArgHelper>();
            services.AddSingleton<ParseWinArgsHelper>();
            services.AddSingleton<RunCLHelper>();
            services.AddSingleton<RunLIBHelper>();
            services.AddSingleton<RunLINKHelper>();
            services.AddSingleton<Func<TempFileHelper>>(TempFileHelperProvider.GetDefault());
            services.AddSingleton<WinCmdHelper>();

            return services;
        }

        public static IServiceCollection Add3Exes(this IServiceCollection services)
        {
            string? GetFirstAvailOrNull(params Func<string?>?[] generators)
            {
                return generators
                    .Select(gen => gen?.Invoke())
                    .FirstOrDefault(exe => exe != null && File.Exists(exe));
            }

            services.AddSingleton(
                sp => new CLExe(
                    GetFirstAvailOrNull(
                        () => Environment.GetEnvironmentVariable("CL_EXE"),
                        () => @"C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\cl.exe",
                        () => @"H:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\cl.exe"
                    ) ?? throw new Exception("CL.exe not found! Please set CL_EXE environment variable")
                )
            );
            services.AddSingleton(
                sp => new LinkExe(
                    GetFirstAvailOrNull(
                        () => Environment.GetEnvironmentVariable("LINK_EXE"),
                        () => @"C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\link.exe",
                        () => @"H:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\link.exe"
                    ) ?? throw new Exception("LINK.exe not found! Please set LINK_EXE environment variable")
                )
            );
            services.AddSingleton(
                sp => new LibExe(
                    GetFirstAvailOrNull(
                        () => Environment.GetEnvironmentVariable("LIB_EXE"),
                        () => @"C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\lib.exe",
                        () => @"H:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\lib.exe"
                    ) ?? throw new Exception("LIB.exe not found! Please set LIB_EXE environment variable")
                )
            );

            return services;
        }
    }
}
