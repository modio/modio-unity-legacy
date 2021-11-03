using System;

namespace ModIO.UserDataIOCallbacks
{
    // --- Initialization ---
    /// <summary>Delegate for SetActiveUser callback.</summary>
    public delegate void SetActiveUserCallback<T>(T userId, bool success);

    // --- File I/O ---
    /// <summary>Delegate for ReadFile callback.</summary>
    public delegate void ReadFileCallback(string relativePath, bool success, byte[] data);

    /// <summary>Delegate for ReadJSONFile callback.</summary>
    public delegate void ReadJSONFileCallback<T>(string relativePath, bool success, T jsonObject);

    /// <summary>Delegate for WriteFile callbacks.</summary>
    public delegate void WriteFileCallback(string relativePath, bool success);

    // --- File Management ---
    /// <summary>Delegate for DeleteFile callbacks.</summary>
    public delegate void DeleteFileCallback(string relativePath, bool success);

    /// <summary>Delegate for GetFileExists callback.</summary>
    public delegate void GetFileExistsCallback(string relativePath, bool doesExist);

    /// <summary>Delegate for GetFileSize callback.</summary>
    public delegate void GetFileSizeCallback(string relativePath, Int64 byteCount);

    /// <summary>Delegate for GetFileSizeAndHash callback.</summary>
    public delegate void GetFileSizeAndHashCallback(string relativePath, bool success,
                                                    Int64 byteCount, string md5Hash);

    /// <summary>Delegate for ClearActiveUserData callback.</summary>
    public delegate void ClearActiveUserDataCallback(bool success);
}
