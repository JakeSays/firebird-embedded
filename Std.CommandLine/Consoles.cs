using System;
using System.IO;
using Std.CommandLine.Utility;
using SysCon = System.Console;

namespace Std.CommandLine;

public static class DefaultConsoles
{
    public static SystemConsole StdOut => new (SysCon.Out, false);
    public static SystemConsole StdErr => new (SysCon.Error, true);
}

public class SystemConsole
{
    private readonly TextWriter _output;
    private readonly bool _stdError;

    public SystemConsole(TextWriter output, bool stdError)
    {
        _output = output;
        _stdError = stdError;
    }

    private bool IsRedirected => _stdError
        ? SysCon.IsErrorRedirected
        : SysCon.IsOutputRedirected;

    private SystemConsole WriteColor(ConsoleColor? color, string text)
    {
        if (text == null!)
        {
            return this;
        }

        if (IsRedirected)
        {
            _output.Write(text);
            return this;
        }

        var original = SysCon.ForegroundColor;

        if (color.HasValue)
        {
            SysCon.ForegroundColor = color.Value;
        }
        else
        {
            SysCon.ResetColor();
        }

        _output.Write(text);
        SysCon.ForegroundColor = original;

        return this;
    }


    private SystemConsole WriteColorLine(ConsoleColor? color, string? text)
    {
        if (text.IsNullOrEmpty())
        {
            _output.WriteLine();
            return this;
        }

        if (IsRedirected)
        {
            _output.WriteLine(text);
            return this;
        }

        var original = SysCon.ForegroundColor;

        if (color.HasValue)
        {
            SysCon.ForegroundColor = color.Value;
        }
        else
        {
            SysCon.ResetColor();
        }

        _output.WriteLine(text);
        SysCon.ForegroundColor = original;

        return this;
    }

    public void ResetColor() => SysCon.ResetColor();

    public SystemConsole Write(ConsoleColor color, string text) => WriteColor(color, text);
    public SystemConsole WriteLine(ConsoleColor color, string text) => WriteColor(color, text);

    public SystemConsole Red(string text) => WriteColor(ConsoleColor.Red, text);
    public SystemConsole RedLine(string text) => WriteColorLine(ConsoleColor.Red, text);

    public SystemConsole DarkRed(string text) => WriteColor(ConsoleColor.DarkRed, text);
    public SystemConsole DarkRedLine(string text) => WriteColorLine(ConsoleColor.DarkRed, text);

    public SystemConsole Green(string text) => WriteColor(ConsoleColor.Green, text);
    public SystemConsole GreenLine(string text) => WriteColorLine(ConsoleColor.Green, text);

    public SystemConsole DarkGreen(string text) => WriteColor(ConsoleColor.DarkGreen, text);
    public SystemConsole DarkGreenLine(string text) => WriteColorLine(ConsoleColor.DarkGreen, text);

    public SystemConsole Normal(string text) => WriteColor(null, text);
    public SystemConsole NormalLine(string? text = null) => WriteColorLine(null, text);

    public SystemConsole Yellow(string text) => WriteColor(ConsoleColor.Yellow, text);
    public SystemConsole YellowLine(string text) => WriteColorLine(ConsoleColor.Yellow, text);

    public SystemConsole DarkYellow(string text) => WriteColor(ConsoleColor.DarkYellow, text);
    public SystemConsole DarkYellowLine(string text) => WriteColorLine(ConsoleColor.DarkYellow, text);

    public SystemConsole Blue(string text) => WriteColor(ConsoleColor.Blue, text);
    public SystemConsole BlueLine(string text) => WriteColorLine(ConsoleColor.Blue, text);

    public SystemConsole DarkBlue(string text) => WriteColor(ConsoleColor.DarkBlue, text);
    public SystemConsole DarkBlueLine(string text) => WriteColorLine(ConsoleColor.DarkBlue, text);

    public SystemConsole Grey(string text) => WriteColor(ConsoleColor.Gray, text);
    public SystemConsole GreyLine(string text) => WriteColorLine(ConsoleColor.Gray, text);

    public SystemConsole DarkGrey(string text) => WriteColor(ConsoleColor.DarkGray, text);
    public SystemConsole DarkGreyLine(string text) => WriteColorLine(ConsoleColor.DarkGray, text);

    public SystemConsole Cyan(string text) => WriteColor(ConsoleColor.Cyan, text);
    public SystemConsole CyanLine(string text) => WriteColorLine(ConsoleColor.Cyan, text);

    public SystemConsole DarkCyan(string text) => WriteColor(ConsoleColor.DarkCyan, text);
    public SystemConsole DarkCyanLine(string text) => WriteColorLine(ConsoleColor.DarkCyan, text);

    public SystemConsole Magenta(string text) => WriteColor(ConsoleColor.Magenta, text);
    public SystemConsole MagentaLine(string text) => WriteColorLine(ConsoleColor.Magenta, text);

    public SystemConsole DarkMagenta(string text) => WriteColor(ConsoleColor.DarkMagenta, text);
    public SystemConsole DarkMagentaLine(string text) => WriteColorLine(ConsoleColor.DarkMagenta, text);

    public SystemConsole Clear()
    {
        SysCon.Clear();
        return this;
    }
}
