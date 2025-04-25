// <copyright file="XarEntryType.cs" company="Quamotion bv">
// Copyright (c) Quamotion bv. All rights reserved.
// </copyright>

namespace Std.FirebirdEmbedded.Tools.Build.Osx;

/// <summary>
/// Specifies the type of an entry in a Xar file.
/// </summary>
internal enum XarEntryType
{
    /// <summary>
    /// The entry is a file.
    /// </summary>
    File,

    /// <summary>
    /// The entry is a directory.
    /// </summary>
    Directory,
}
