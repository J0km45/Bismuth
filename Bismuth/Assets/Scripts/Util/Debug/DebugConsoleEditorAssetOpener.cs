// ------------------------------------------------------------------------------
// 에디터에서 스크립트 에셋을 찾고 원하는 줄로 여는 보조 기능을 담당하는 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;

/// <summary>
/// 에디터 환경에서 스크립트 에셋을 찾고 열어주는 보조 정적 클래스이다.
/// </summary>
public static class DebugConsoleEditorAssetOpener
{
    /// <summary>
    /// find script 처리를 시도한다. 성공 여부를 bool로 반환하고 실패 시 안전하게 빠져나간다.
    /// </summary>
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

    /// <summary>
    /// script를 연다. 외부 에셋이나 패널, 스크립트 위치로 이동시키는 데 사용한다.
    /// </summary>
    public static void OpenScript(MonoScript script, int lineNumber, int columnNumber)
    {
        if (script == null)
            return;

        AssetDatabase.OpenAsset(script, Math.Max(1, lineNumber), Math.Max(1, columnNumber));
    }
}
#endif
