// ------------------------------------------------------------------------------
// 필터 상태와 레이아웃 상태를 저장소에 기록하거나 불러오는 저장 접근 계층 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상태 객체를 저장소와 연결하는 리포지토리 클래스이다.
/// </summary>
public sealed class DebugConsolePreferenceRepository
{
    // prefix 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private readonly string _prefix;

    /// <summary>
    /// 이 생성자는 debug console 환경설정 리포지토리 인스턴스를 만들 때 필요한 기본 상태를 준비한다.
    /// </summary>
    public DebugConsolePreferenceRepository(string prefix)
    {
        _prefix = prefix;
    }

    /// <summary>
    /// bool 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public bool GetBool(string key, bool defaultValue)
    {
        return DebugConsolePreferenceStore.GetBool(BuildKey(key), defaultValue);
    }

    /// <summary>
    /// set bool 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public void SetBool(string key, bool value)
    {
        DebugConsolePreferenceStore.SetBool(BuildKey(key), value);
    }

    /// <summary>
    /// float 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public float GetFloat(string key, float defaultValue)
    {
        return DebugConsolePreferenceStore.GetFloat(BuildKey(key), defaultValue);
    }

    /// <summary>
    /// set float 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public void SetFloat(string key, float value)
    {
        DebugConsolePreferenceStore.SetFloat(BuildKey(key), value);
    }

    /// <summary>
    /// 영역 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public Rect GetRect(string key, Rect defaultValue)
    {
        return DebugConsolePreferenceStore.GetRect(BuildKey(key), defaultValue);
    }

    /// <summary>
    /// set 영역 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public void SetRect(string key, Rect value)
    {
        DebugConsolePreferenceStore.SetRect(BuildKey(key), value);
    }

    /// <summary>
    /// string 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public string GetString(string key, string defaultValue = "")
    {
        return DebugConsolePreferenceStore.GetString(BuildKey(key), defaultValue);
    }

    /// <summary>
    /// set string 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public void SetString(string key, string value)
    {
        DebugConsolePreferenceStore.SetString(BuildKey(key), value);
    }

    /// <summary>
    /// delete 식별 키 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public void DeleteKey(string key)
    {
        DebugConsolePreferenceStore.DeleteKey(BuildKey(key));
    }

    /// <summary>
    /// string set 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
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

    /// <summary>
    /// set string set 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
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

    /// <summary>
    /// 식별 키 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
    /// </summary>
    private string BuildKey(string key)
    {
        return string.IsNullOrWhiteSpace(_prefix) ? key : $"{_prefix}.{key}";
    }
}
