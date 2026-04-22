// ------------------------------------------------------------------------------
// 에디터와 런타임 환경 차이를 숨기고 문자열 기반 설정 저장을 담당하는 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
using System.Globalization;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 환경에 맞는 설정 저장소 접근을 제공하는 정적 클래스이다.
/// </summary>
public static class DebugConsolePreferenceStore
{
    /// <summary>
    /// bool 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public static bool GetBool(string key, bool defaultValue)
    {
#if UNITY_EDITOR
        return EditorPrefs.GetBool(key, defaultValue);
#else
        return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
#endif
    }

    /// <summary>
    /// set bool 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public static void SetBool(string key, bool value)
    {
#if UNITY_EDITOR
        EditorPrefs.SetBool(key, value);
#else
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
#endif
    }

    /// <summary>
    /// int 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public static int GetInt(string key, int defaultValue)
    {
#if UNITY_EDITOR
        return EditorPrefs.GetInt(key, defaultValue);
#else
        return PlayerPrefs.GetInt(key, defaultValue);
#endif
    }

    /// <summary>
    /// set int 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public static void SetInt(string key, int value)
    {
#if UNITY_EDITOR
        EditorPrefs.SetInt(key, value);
#else
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
#endif
    }

    /// <summary>
    /// float 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public static float GetFloat(string key, float defaultValue)
    {
#if UNITY_EDITOR
        return EditorPrefs.GetFloat(key, defaultValue);
#else
        return PlayerPrefs.GetFloat(key, defaultValue);
#endif
    }

    /// <summary>
    /// set float 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public static void SetFloat(string key, float value)
    {
#if UNITY_EDITOR
        EditorPrefs.SetFloat(key, value);
#else
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
#endif
    }

    /// <summary>
    /// string 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public static string GetString(string key, string defaultValue)
    {
#if UNITY_EDITOR
        return EditorPrefs.GetString(key, defaultValue);
#else
        return PlayerPrefs.GetString(key, defaultValue);
#endif
    }

    /// <summary>
    /// set string 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public static void SetString(string key, string value)
    {
#if UNITY_EDITOR
        EditorPrefs.SetString(key, value ?? string.Empty);
#else
        PlayerPrefs.SetString(key, value ?? string.Empty);
        PlayerPrefs.Save();
#endif
    }


    /// <summary>
    /// delete 식별 키 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public static void DeleteKey(string key)
    {
#if UNITY_EDITOR
        if (EditorPrefs.HasKey(key))
            EditorPrefs.DeleteKey(key);
#else
        if (PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
#endif
    }

    /// <summary>
    /// 영역 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public static Rect GetRect(string key, Rect defaultValue)
    {
        string raw = GetString(key, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;

        string[] parts = raw.Split('|');
        if (parts.Length != 4)
            return defaultValue;

        if (!TryParseFloat(parts[0], out float x) ||
            !TryParseFloat(parts[1], out float y) ||
            !TryParseFloat(parts[2], out float width) ||
            !TryParseFloat(parts[3], out float height))
        {
            return defaultValue;
        }

        return new Rect(x, y, width, height);
    }

    /// <summary>
    /// set 영역 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public static void SetRect(string key, Rect value)
    {
        string raw = string.Join("|",
            value.x.ToString(CultureInfo.InvariantCulture),
            value.y.ToString(CultureInfo.InvariantCulture),
            value.width.ToString(CultureInfo.InvariantCulture),
            value.height.ToString(CultureInfo.InvariantCulture));

        SetString(key, raw);
    }

    /// <summary>
    /// parse float 처리를 시도한다. 성공 여부를 bool로 반환하고 실패 시 안전하게 빠져나간다.
    /// </summary>
    private static bool TryParseFloat(string raw, out float value)
    {
        return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
