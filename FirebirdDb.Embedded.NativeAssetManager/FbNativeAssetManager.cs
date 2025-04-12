using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
// ReSharper disable InconsistentNaming

namespace FirebirdSql.Embedded;

public static class FbNativeAssetManager
{
    public static string? NativeAssetPath(FirebirdVersion version)
    {
        var basePath = GetBasePath();
        if (basePath == null)
        {
            return null;
        }

        var versionedPath = Path.Combine(basePath, version.ToString());

        Console.WriteLine($"***** {version}: {versionedPath}");
        if (!Directory.Exists(versionedPath))
        {
            return null;
        }

        var fullPath = Path.Combine(versionedPath, MakeNativeBinaryName(version));

        return File.Exists(fullPath)
            ? fullPath
            : null;

        static string MakeNativeBinaryName(FirebirdVersion version)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "fbclient.dll";
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return $"plugins/libEngine{version.NativeBinarySuffix()}.so";
            }

            throw new PlatformNotSupportedException("Only Windows and Linux platforms are currently supported.");
        }
    }

    private static readonly string _hostProcessDirectory = MakeHostProcessDirectory();

    private static string MakeHostProcessDirectory()
    {
        var runningOnWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        if (runningOnWindows)
        {
            const uint bufferSize = 1_200;
            var buffer = new StringBuilder((int) bufferSize);

            GetModuleFileNameW(IntPtr.Zero, buffer, bufferSize);

            var processPath = Path.GetDirectoryName(buffer.ToString());
            return processPath!;
        }

        var linuxProcessPath = Path.GetDirectoryName(readlink("/proc/self/exe")!)!;
        return linuxProcessPath;
    }

    private static string? GetBasePath()
    {
        const string firebirdDirectory = "firebird";

        var intermediateDir = Path.Combine(_hostProcessDirectory, firebirdDirectory);
        //first see if the binaries are alongside the host process
        if (!Directory.Exists(intermediateDir))
        {
            //they're not so see if we're running as an expanded single-file deployment
            //if we're running as a non-expanded single-file deployment corlibLocation will be null
            var corlibLocation = typeof(string).Assembly.Location;
            if (corlibLocation == null!)
            {
                //no luck - binaries can't be located
                return null;
            }

            intermediateDir = Path.Combine(Path.GetDirectoryName(corlibLocation)!, firebirdDirectory);
        }

        var basePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? GetWindowsPath(intermediateDir)
            : GetLinuxPath(intermediateDir);

        return basePath;

        static string GetLinuxPath(string intermediateDir)
        {
            var rid = $"linux-{(Environment.Is64BitProcess ? "x64" : "x32")}";
            return Path.Combine(intermediateDir, rid);
        }

        static string GetWindowsPath(string intermediateDir)
        {
            var rid = $"win-{(Environment.Is64BitProcess ? "x64" : "x86")}";
            return Path.Combine(intermediateDir, rid);
        }
    }

    [DllImport ("libc")]
    private static extern int readlink(string path, byte[] buffer, int buflen);

    private static string? readlink(string path)
    {
        const int bufferSize = 1_024;
        var buf = new byte[bufferSize];
        var pathLength = readlink(path, buf, buf.Length);
        if (pathLength == -1)
        {
            return null;
        }
        var chars = new char[bufferSize];
        var charCount = Encoding.Default.GetChars(buf, 0, pathLength, chars, 0);
        return new string(chars, 0, charCount);
    }

    [DllImport("kernel32.dll", SetLastError=true)]
    [PreserveSig]
    private static extern uint GetModuleFileNameW
    (
        [In]
        IntPtr hModule,

        [Out, MarshalAs(UnmanagedType.LPWStr)]
        StringBuilder lpFilename,

        [In]
        [MarshalAs(UnmanagedType.U4)]
        uint nSize
    );
}
