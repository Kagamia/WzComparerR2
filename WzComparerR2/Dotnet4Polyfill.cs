using System;

#if NET462
namespace System.Runtime.InteropServices
{
    internal static class RuntimeInformation
    {
        public static Architecture ProcessArchitecture
        {
            get
            {
                if (string.Equals(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"), "arm64", StringComparison.OrdinalIgnoreCase))
                {
                    return Architecture.Arm64;
                }
                return Environment.Is64BitProcess ? Architecture.X64 : Architecture.X86;
            }
        }
    }

    internal enum Architecture
    {
        X86 = 0,
        X64 = 1,
        Arm = 2,
        Arm64 = 3,
    }
}
#endif