#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;

public static class DebugConsoleEditorAssetOpener
{
    public static bool TryFindScript(string callerFilePath, out MonoScript script)
    {
        script = null;

        string fileName = Path.GetFileNameWithoutExtension(callerFilePath);
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        string[] guids = AssetDatabase.FindAssets($"{fileName} t:MonoScript");
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!string.Equals(Path.GetFileNameWithoutExtension(assetPath), fileName, StringComparison.Ordinal))
                continue;

            MonoScript found = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
            if (found == null)
                continue;

            script = found;
            return true;
        }

        return false;
    }

    public static void OpenScript(MonoScript script, int lineNumber, int columnNumber)
    {
        if (script == null)
            return;

        AssetDatabase.OpenAsset(script, Math.Max(1, lineNumber), Math.Max(1, columnNumber));
    }
}
#endif
