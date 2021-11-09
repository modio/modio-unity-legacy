using System;
using System.Collections.Generic;

namespace ModIO.PlatformIOCallbacks
{
    // --- File I/O ---
    /// <summary>Delegate for ReadFile callback.</summary>
    public delegate void ReadFileCallback(string path, bool success, byte[] data);

    /// <summary>Delegate for ReadJSONFile callback.</summary>
    public delegate void ReadJSONFileCallback<T>(string path, bool success, T jsonObject);

    /// <summary>Delegate for WriteFile callbacks.</summary>
    public delegate void WriteFileCallback(string path, bool success);

    // --- File Management ---
    /// <summary>Delegate for DeleteFile callbacks.</summary>
    public delegate void DeleteFileCallback(string path, bool success);

    /// <summary>Delegate for MoveFile callbacks.</summary>
    public delegate void MoveFileCallback(string source, string destination, bool success);

    /// <summary>Delegate for GetFileExists callback.</summary>
    public delegate void GetFileExistsCallback(string path, bool doesExist);

    /// <summary>Delegate for GetFileSizeAndHash callback.</summary>
    public delegate void GetFileSizeAndHashCallback(string path, bool success, Int64 byteCount,
                                                    string md5Hash);

    /// <summary>Delegate for GetFiles callback.</summary>
    public delegate void GetFilesCallback(string path, bool success, IList<string> fileList);

    // --- Directory Management ---
    /// <summary>Delegate for CreateDirectory callback.</summary>
    public delegate void CreateDirectoryCallback(string path, bool success);

    /// <summary>Delegate for DeleteDirectory callback.</summary>
    public delegate void DeleteDirectoryCallback(string path, bool success);

    /// <summary>Delegate for MoveDirectory callback.</summary>
    public delegate void MoveDirectoryCallback(string source, string destination, bool success);

    /// <summary>Delegate for GetDirectoryExists callback.</summary>
    public delegate void GetDirectoryExistsCallback(string path, bool success);

    /// <summary>Delegate for GetDirectories callback.</summary>
    public delegate void GetDirectoriesCallback(string path, bool success,
                                                IList<string> directoryList);
}
