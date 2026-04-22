using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public static class DebugConsoleFilterService
{
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

    public static bool ContainsIgnoreCase(string source, string target)
    {
        if (string.IsNullOrEmpty(target))
            return true;

        if (string.IsNullOrEmpty(source))
            return false;

        return source.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
