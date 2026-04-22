// ------------------------------------------------------------------------------
// 로그 선택, 포커스 계산, 선택 유지 같은 선택 관련 판단 로직을 담당하는 서비스 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 선택 상태와 포커스 결과를 계산하는 정적 서비스 클래스이다.
/// </summary>
public static class DebugConsoleSelectionService
{
    /// <summary>
    /// 엔트리를 현재 포커스 대상으로 설정한다. 관련 선택 상태도 함께 갱신한다.
    /// </summary>
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

    /// <summary>
    /// 오브젝트 포커스 상태를 켜고 끄는 토글 동작을 수행한다.
    /// </summary>
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

    /// <summary>
    /// 컴포넌트 포커스 상태를 켜고 끄는 토글 동작을 수행한다.
    /// </summary>
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

    /// <summary>
    /// prepare 선택 expansion 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public static void PrepareSelectionExpansion(Transform target, bool includeDetails, bool collapsePreviousOnSelection, HashSet<int> expandedComponents, HashSet<int> expandedChildren)
    {
        if (target == null)
            return;

        if (collapsePreviousOnSelection)
            PreserveExpansionWithinTopLevelRoot(target, expandedComponents, expandedChildren);

        ExpandSelectionPath(target, includeDetails, expandedComponents, expandedChildren);
    }

    /// <summary>
    /// expand 선택 경로 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
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

    /// <summary>
    /// preserve expansion within top 레벨 root 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
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

    /// <summary>
    /// top 레벨 root 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public static Transform GetTopLevelRoot(Transform target)
    {
        if (target == null)
            return null;

        Transform current = target;
        while (current.parent != null)
            current = current.parent;

        return current;
    }

    /// <summary>
    /// 조건에 맞는 subtree ids 항목을 모아 반환한다.
    /// </summary>
    public static void CollectSubtreeIds(Transform node, HashSet<int> ids)
    {
        if (node == null || ids == null)
            return;

        ids.Add(node.gameObject.GetInstanceID());
        for (int i = 0; i < node.childCount; i++)
            CollectSubtreeIds(node.GetChild(i), ids);
    }
}
