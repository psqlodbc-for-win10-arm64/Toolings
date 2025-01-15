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
            services.AddSingleton<Arm64XCLHelper>();
            services.AddSingleton<Arm64XLIBHelper>();
            services.AddSingleton<Arm64XLINKHelper>();
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
            string GetByEnvOr(string env, string defaultExe)
            {
                var envExe = Environment.GetEnvironmentVariable(env);
                return string.IsNullOrWhiteSpace(envExe) ? defaultExe : envExe;
            }

            services.AddSingleton(
                sp => new CLExe(
                    GetByEnvOr(
                        "CL_EXE",
                        @"H:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\cl.exe"
                    )
                )
            );
            services.AddSingleton(
                sp => new LinkExe(
                    GetByEnvOr(
                        "LINK_EXE",
                        @"H:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\link.exe"
                    )
                )
            );
            services.AddSingleton(
                sp => new LibExe(
                    GetByEnvOr(
                        "LIB_EXE",
                        @"H:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.42.34433\bin\Hostx64\arm64\lib.exe"
                    )
                )
            );

            return services;
        }
    }
}
