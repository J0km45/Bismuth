using System.Globalization;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class DebugConsolePreferenceStore
{
    public static bool GetBool(string key, bool defaultValue)
    {
#if UNITY_EDITOR
        return EditorPrefs.GetBool(key, defaultValue);
#else
        return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
#endif
    }

    public static void SetBool(string key, bool value)
    {
#if UNITY_EDITOR
        EditorPrefs.SetBool(key, value);
#else
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
#endif
    }

    public static int GetInt(string key, int defaultValue)
    {
#if UNITY_EDITOR
        return EditorPrefs.GetInt(key, defaultValue);
#else
        return PlayerPrefs.GetInt(key, defaultValue);
#endif
    }

    public static void SetInt(string key, int value)
    {
#if UNITY_EDITOR
        EditorPrefs.SetInt(key, value);
#else
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
#endif
    }

    public static float GetFloat(string key, float defaultValue)
    {
#if UNITY_EDITOR
        return EditorPrefs.GetFloat(key, defaultValue);
#else
        return PlayerPrefs.GetFloat(key, defaultValue);
#endif
    }

    public static void SetFloat(string key, float value)
    {
#if UNITY_EDITOR
        EditorPrefs.SetFloat(key, value);
#else
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
#endif
    }

    public static string GetString(string key, string defaultValue)
    {
#if UNITY_EDITOR
        return EditorPrefs.GetString(key, defaultValue);
#else
        return PlayerPrefs.GetString(key, defaultValue);
#endif
    }

    public static void SetString(string key, string value)
    {
#if UNITY_EDITOR
        EditorPrefs.SetString(key, value ?? string.Empty);
#else
        PlayerPrefs.SetString(key, value ?? string.Empty);
        PlayerPrefs.Save();
#endif
    }

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

    public static void SetRect(string key, Rect value)
    {
        string raw = string.Join("|",
            value.x.ToString(CultureInfo.InvariantCulture),
            value.y.ToString(CultureInfo.InvariantCulture),
            value.width.ToString(CultureInfo.InvariantCulture),
            value.height.ToString(CultureInfo.InvariantCulture));

        SetString(key, raw);
    }

    private static bool TryParseFloat(string raw, out float value)
    {
        return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
