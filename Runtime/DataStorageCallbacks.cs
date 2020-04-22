using System;
using System.Collections.Generic;

namespace ModIO.DataStorageCallbacks
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

    /// <summary>Delegate for GetFileSize callback.</summary>
    public delegate void GetFileSizeCallback(string path, Int64 byteCount);

    /// <summary>Delegate for GetFileSizeAndHash callback.</summary>
    public delegate void GetFileSizeAndHashCallback(string path, bool success, Int64 byteCount, string md5Hash);

    // --- User Data Callbacks ---
    /// <summary>Delegate for SetActiveUser callback.</summary>
    public delegate void SetActiveUserCallback<T>(T userId, bool success);

    /// <summary>Delegate for ClearActiveUserData callback.</summary>
    public delegate void ClearActiveUserDataCallback(bool success);
}
