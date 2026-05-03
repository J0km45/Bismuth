// ------------------------------------------------------------------------------
// 현재 필터 상태와 선택 상태를 기준으로 로그나 대상을 표시할지 판정하는 서비스 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 필터 조건 충족 여부를 판정하는 정적 서비스 클래스이다.
/// </summary>
public static class DebugConsoleFilterService
{
    /// <summary>
    /// allowed 여부를 판정한다. 조건 분기에 사용할 수 있도록 bool 값을 반환한다.
    /// </summary>
    public static bool IsAllowed(DebugConsoleFilterState filterState, Func<DebugType, bool> isTypeEnabled, Func<int, bool> isGameObjectEnabled, Func<int, bool> isComponentEnabled, DebugType type, int gameObjectId, int componentId)
    {
        if (filterState != null && !filterState.GlobalEnabled)
            return false;

        if (isTypeEnabled != null && !isTypeEnabled(type))
            return false;

        if (gameObjectId != 0 && isGameObjectEnabled != null && !isGameObjectEnabled(gameObjectId))
            return false;

        if (componentId != 0 && isComponentEnabled != null && !isComponentEnabled(componentId))
            return false;

        return true;
    }

    /// <summary>
    /// allowed 여부를 판정한다. 조건 분기에 사용할 수 있도록 bool 값을 반환한다.
    /// </summary>
    public static bool IsAllowed(DebugConsoleFilterState filterState, Func<DebugType, bool> isTypeEnabled, Func<GameObject, bool> isGameObjectEnabled, Func<Component, bool> isComponentEnabled, DebugType type, Object context)
    {
        if (filterState != null && !filterState.GlobalEnabled)
            return false;

        if (isTypeEnabled != null && !isTypeEnabled(type))
            return false;

        if (context is GameObject go)
            return isGameObjectEnabled == null || isGameObjectEnabled(go);

        if (context is Component component)
        {
            bool gameObjectAllowed = isGameObjectEnabled == null || isGameObjectEnabled(component.gameObject);
            bool componentAllowed = isComponentEnabled == null || isComponentEnabled(component);
            return gameObjectAllowed && componentAllowed;
        }

        return true;
    }

    /// <summary>
    /// 표시 엔트리를 수행해야 하는지 정책적으로 판단한다.
    /// </summary>
    public static bool ShouldDisplayEntry(DebugEntry entry, Func<DebugEntry, bool> basePredicate, DebugConsoleFocusState focusState, string logSearch)
    {
        if (entry == null)
            return false;

        if (basePredicate != null && !basePredicate(entry))
            return false;

        if (focusState != null)
        {
            if (focusState.HasComponentFocus)
            {
                if (entry.ComponentId != focusState.FocusedComponentId)
                    return false;
            }
            else if (focusState.HasGameObjectFocus)
            {
                if (entry.GameObjectId != focusState.FocusedGameObjectId)
                    return false;
            }
        }

        if (string.IsNullOrWhiteSpace(logSearch))
            return true;

        string searchPool = $"{entry.Message} {entry.SourceName} {entry.MemberName} {entry.Type} {entry.Time}";
        return ContainsIgnoreCase(searchPool, logSearch);
    }

    /// <summary>
    /// 개수 표시 엔트리 목록 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public static int CountVisibleEntries(IReadOnlyList<DebugEntry> entries, Func<DebugEntry, bool> predicate)
    {
        if (entries == null || predicate == null)
            return 0;

        int count = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            if (predicate(entries[i]))
                count++;
        }

        return count;
    }

    /// <summary>
    /// contains ignore case 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public static bool ContainsIgnoreCase(string source, string target)
    {
        if (string.IsNullOrEmpty(target))
            return true;

        if (string.IsNullOrEmpty(source))
            return false;

        return source.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
