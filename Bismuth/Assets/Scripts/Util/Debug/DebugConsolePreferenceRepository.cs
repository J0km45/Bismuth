using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class DebugConsolePreferenceRepository
{
    private readonly string _prefix;

    public DebugConsolePreferenceRepository(string prefix)
    {
        _prefix = prefix;
    }

    public bool GetBool(string key, bool defaultValue)
    {
        return DebugConsolePreferenceStore.GetBool(BuildKey(key), defaultValue);
    }

    public void SetBool(string key, bool value)
    {
        DebugConsolePreferenceStore.SetBool(BuildKey(key), value);
    }

    public float GetFloat(string key, float defaultValue)
    {
        return DebugConsolePreferenceStore.GetFloat(BuildKey(key), defaultValue);
    }

    public void SetFloat(string key, float value)
    {
        DebugConsolePreferenceStore.SetFloat(BuildKey(key), value);
    }

    public Rect GetRect(string key, Rect defaultValue)
    {
        return DebugConsolePreferenceStore.GetRect(BuildKey(key), defaultValue);
    }

    public void SetRect(string key, Rect value)
    {
        DebugConsolePreferenceStore.SetRect(BuildKey(key), value);
    }

    public string GetString(string key, string defaultValue = "")
    {
        return DebugConsolePreferenceStore.GetString(BuildKey(key), defaultValue);
    }

    public void SetString(string key, string value)
    {
        DebugConsolePreferenceStore.SetString(BuildKey(key), value);
    }

    public void DeleteKey(string key)
    {
        DebugConsolePreferenceStore.DeleteKey(BuildKey(key));
    }

    public HashSet<string> GetStringSet(string key)
    {
        HashSet<string> result = new HashSet<string>();
        string raw = GetString(key, string.Empty);

        if (string.IsNullOrWhiteSpace(raw))
            return result;

        string[] parts = raw.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
            result.Add(parts[i]);

        return result;
    }

    public void SetStringSet(string key, IEnumerable<string> values)
    {
        if (values == null)
        {
            DeleteKey(key);
            return;
        }

        List<string> list = new List<string>();
        foreach (string value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                list.Add(value);
        }

        if (list.Count == 0)
        {
            DeleteKey(key);
            return;
        }

        SetString(key, string.Join("\n", list));
    }

    private string BuildKey(string key)
    {
        return string.IsNullOrWhiteSpace(_prefix) ? key : $"{_prefix}.{key}";
    }
}
