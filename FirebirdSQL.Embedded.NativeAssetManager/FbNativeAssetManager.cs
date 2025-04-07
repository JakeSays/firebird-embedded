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
        var basePath = GetBasePath(true);
        var versionedPath = Path.Combine(basePath, version.ToString());
        if (!Directory.Exists(versionedPath))
        {
            basePath = GetBasePath(false);
            versionedPath = Path.Combine(basePath, version.ToString());
            if (!Directory.Exists(versionedPath))
            {
                return null;
            }
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

    private static string GetBasePath(bool usePublished)
    {
        const string publishedDirectory = "firebird";

        if (usePublished)
        {
            return Path.Combine(_hostProcessDirectory, publishedDirectory);
        }

        var basePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? GetWindowsPath()
            : GetLinuxPath();

        return basePath;

        static string GetLinuxPath()
        {
            var rid = $"linux-{(Environment.Is64BitProcess ? "x64" : "x32")}";
            return Path.Combine(_hostProcessDirectory, rid);
        }

        static string GetWindowsPath()
        {
            var rid = $"win-{(Environment.Is64BitProcess ? "x64" : "x32")}";
            return Path.Combine(_hostProcessDirectory, rid);
        }
    }

    [DllImport ("libc")]
    private static extern int readlink(string path, byte[] buffer, int buflen);

    private static string? readlink(string path)
    {
        var buf = new byte[512];
        var ret = readlink(path, buf, buf.Length);
        if (ret == -1)
        {
            return null;
        }
        var cbuf = new char[512];
        var chars = Encoding.Default.GetChars(buf, 0, ret, cbuf, 0);
        return new string(cbuf, 0, chars);
    }

    [DllImport("kernel32.dll", SetLastError=true)]
    [PreserveSig]
    private static extern uint GetModuleFileNameW
    (
        [In]
        IntPtr hModule,

        [Out]
        StringBuilder lpFilename,

        [In]
        [MarshalAs(UnmanagedType.U4)]
        uint nSize
    );
}
