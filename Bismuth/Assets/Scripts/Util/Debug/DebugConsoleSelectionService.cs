using System.Collections.Generic;
using UnityEngine;

public static class DebugConsoleSelectionService
{
    public static void FocusEntry(DebugConsoleFocusState focusState, DebugEntry entry)
    {
        if (focusState == null || entry == null)
            return;

        if (entry.Context is GameObject go)
        {
            focusState.FocusGameObject(go.GetInstanceID(), go.name);
            return;
        }

        if (entry.Context is Component component)
            focusState.FocusComponent(component.gameObject.GetInstanceID(), component.GetInstanceID(), component.gameObject.name, component.GetType().Name);
    }

    public static void ToggleGameObjectFocus(DebugConsoleFocusState focusState, GameObject go)
    {
        if (focusState == null || go == null)
            return;

        int id = go.GetInstanceID();
        if (focusState.IsObjectFocused(id))
        {
            focusState.Clear();
            return;
        }

        focusState.FocusGameObject(id, go.name);
    }

    public static void ToggleComponentFocus(DebugConsoleFocusState focusState, Component component)
    {
        if (focusState == null || component == null)
            return;

        int componentId = component.GetInstanceID();
        if (focusState.IsComponentFocused(componentId))
        {
            focusState.Clear();
            return;
        }

        focusState.FocusComponent(component.gameObject.GetInstanceID(), componentId, component.gameObject.name, component.GetType().Name);
    }

    public static void PrepareSelectionExpansion(Transform target, bool includeDetails, bool collapsePreviousOnSelection, HashSet<int> expandedComponents, HashSet<int> expandedChildren)
    {
        if (target == null)
            return;

        if (collapsePreviousOnSelection)
            PreserveExpansionWithinTopLevelRoot(target, expandedComponents, expandedChildren);

        ExpandSelectionPath(target, includeDetails, expandedComponents, expandedChildren);
    }

    public static void ExpandSelectionPath(Transform target, bool includeTargetDetails, HashSet<int> expandedComponents, HashSet<int> expandedChildren)
    {
        if (target == null)
            return;

        Transform current = target;

        if (includeTargetDetails)
            expandedComponents.Add(current.gameObject.GetInstanceID());

        while (current.parent != null)
        {
            Transform parent = current.parent;
            int parentId = parent.gameObject.GetInstanceID();
            expandedComponents.Add(parentId);
            expandedChildren.Add(parentId);
            current = parent;
        }
    }

    public static void PreserveExpansionWithinTopLevelRoot(Transform target, HashSet<int> expandedComponents, HashSet<int> expandedChildren)
    {
        Transform topLevelRoot = GetTopLevelRoot(target);
        if (topLevelRoot == null)
        {
            expandedComponents.Clear();
            expandedChildren.Clear();
            return;
        }

        HashSet<int> allowedIds = new HashSet<int>();
        CollectSubtreeIds(topLevelRoot, allowedIds);

        expandedComponents.RemoveWhere(id => !allowedIds.Contains(id));
        expandedChildren.RemoveWhere(id => !allowedIds.Contains(id));
    }

    public static Transform GetTopLevelRoot(Transform target)
    {
        if (target == null)
            return null;

        Transform current = target;
        while (current.parent != null)
            current = current.parent;

        return current;
    }

    public static void CollectSubtreeIds(Transform node, HashSet<int> ids)
    {
        if (node == null || ids == null)
            return;

        ids.Add(node.gameObject.GetInstanceID());
        for (int i = 0; i < node.childCount; i++)
            CollectSubtreeIds(node.GetChild(i), ids);
    }
}
