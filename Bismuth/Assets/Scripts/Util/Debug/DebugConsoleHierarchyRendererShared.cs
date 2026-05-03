// ------------------------------------------------------------------------------
// 런타임과 에디터에서 공통으로 사용할 수 있는 계층 패널 렌더링 보조 코드를 모아둔 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 계층 패널을 그릴 때 필요한 데이터와 콜백을 전달하는 컨텍스트 클래스이다.
/// </summary>
public sealed class DebugConsoleHierarchyRenderContextShared
{
    // 매니저 값을 저장한다. 중앙 DebugConsoleManager 인스턴스 참조를 저장한다.
    public DebugConsoleManager Manager;
    // 패널 너비 값을 저장한다. 패널의 현재 너비 값을 저장한다. 레이아웃 계산과 렌더링 폭 결정에 사용한다.
    public float PanelWidth;
    // 스크롤 값을 저장한다. 현재 스크롤 위치를 저장한다. 다시 그릴 때 사용자가 보고 있던 위치를 유지하는 데 사용한다.
    public Vector2 Scroll;
    // 계층 검색 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    public string HierarchySearch = string.Empty;

    // 제목 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle TitleStyle;
    // box 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle BoxStyle;
    // link 버튼 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle LinkButtonStyle;
    // foldout 버튼 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle FoldoutButtonStyle;
    // 하단 left label 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle FooterLeftLabelStyle;
    // 하단 right label 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle FooterRightLabelStyle;

    // 계층 행 높이 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public float HierarchyRowHeight;
    // 계층 토글 크기 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public float HierarchyToggleSize;
    // 계층 foldout 크기 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public float HierarchyFoldoutSize;
    // 계층 행 content right 예약 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public float HierarchyRowContentRightReserve;
    // 최대 계층 indent 패널티 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public float MaxHierarchyIndentPenalty;

    // 펼침 컴포넌트 목록 값을 저장한다. 트리 항목의 펼침 상태를 저장한다.
    public HashSet<int> ExpandedComponents;
    // 펼침 하위 목록 값을 저장한다. 트리 항목의 펼침 상태를 저장한다.
    public HashSet<int> ExpandedChildren;

    // get 표시 엔트리 개수 값을 저장한다. 개수 또는 표시할 항목 수를 저장한다.
    public Func<int> GetVisibleEntryCount;
    // get 포커스 label 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public Func<string> GetFocusLabel;
    // get 하단 포커스 label 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public Func<string> GetFooterFocusLabel;
    // should show 오브젝트 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public Func<GameObject, bool> ShouldShowGameObject;
    // has 표시 컴포넌트 목록 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public Func<Component[], bool> HasVisibleComponents;
    // has 표시 하위 목록 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public Func<GameObject, bool> HasVisibleChildren;
    // has matching 컴포넌트 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public Func<GameObject, string, bool> HasMatchingComponent;
    // should show 컴포넌트 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public Func<Component, string, bool> ShouldShowComponent;
    // is 오브젝트 focused 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    public Func<int, bool> IsObjectFocused;
    // is focused 오브젝트 상위 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    public Func<int, bool> IsFocusedObjectParent;
    // is 컴포넌트 focused 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    public Func<int, bool> IsComponentFocused;
    // get 계층 행 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public Func<bool, bool, bool, GUIStyle> GetHierarchyRowStyle;
    // get 오브젝트 버튼 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public Func<bool, bool, bool, GUIStyle> GetObjectButtonStyle;
    // get 컴포넌트 버튼 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public Func<bool, bool, GUIStyle> GetComponentButtonStyle;
    // get 표시 이름 값을 저장한다. 표시용 이름 값을 저장한다.
    public Func<string, string> GetDisplayName;
    // 토글 오브젝트 포커스 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public Action<GameObject> ToggleGameObjectFocus;
    // 토글 컴포넌트 포커스 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public Action<Component> ToggleComponentFocus;
    // 토글 펼침 set 값을 저장한다. 트리 항목의 펼침 상태를 저장한다.
    public Action<HashSet<int>, int> ToggleExpandedSet;
    // save 상태 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public Action SaveState;
}

/// <summary>
/// DebugConsoleHierarchyRendererShared 관련 역할을 담당하는 class이다.
/// </summary>
public static class DebugConsoleHierarchyRendererShared
{
    /// <summary>
    /// 관련 작업 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    public static void Draw(DebugConsoleHierarchyRenderContextShared ctx)
    {
        GUILayout.BeginVertical(ctx.BoxStyle, GUILayout.Width(ctx.PanelWidth), GUILayout.ExpandHeight(true));
        GUILayout.BeginHorizontal();
        GUILayout.Label("Scene Objects / Components", ctx.TitleStyle, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        ctx.Scroll = GUILayout.BeginScrollView(ctx.Scroll);

        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            DrawGameObjectNode(ctx, roots[i], 0);

        GUILayout.EndScrollView();

        GUILayout.Space(4f);
        DrawFooter(ctx);
        GUILayout.EndVertical();
    }

    /// <summary>
    /// 하단 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private static void DrawFooter(DebugConsoleHierarchyRenderContextShared ctx)
    {
        string footerFocusFullText = ctx.GetFocusLabel();
        string footerFocusDisplayText = ctx.GetFooterFocusLabel();
        string footerCountText = $"Count : {ctx.GetVisibleEntryCount()}";
        float footerHorizontalPadding = 10f;
        float footerGap = 12f;
        float footerFocusRequiredWidth = ctx.FooterLeftLabelStyle.CalcSize(new GUIContent(footerFocusDisplayText)).x;
        float footerCountRequiredWidth = ctx.FooterRightLabelStyle.CalcSize(new GUIContent(footerCountText)).x;
        bool useTwoLineFooter = ctx.PanelWidth < footerFocusRequiredWidth + footerCountRequiredWidth + (footerHorizontalPadding * 2f) + footerGap;

        if (useTwoLineFooter)
        {
            GUILayout.BeginVertical(ctx.BoxStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(52f));

            GUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
            GUILayout.Space(footerHorizontalPadding);
            GUILayout.Label(new GUIContent(footerFocusDisplayText, footerFocusFullText), ctx.FooterLeftLabelStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(22f));
            GUILayout.Space(footerHorizontalPadding);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
            GUILayout.Space(footerHorizontalPadding);
            GUILayout.Label(new GUIContent(footerCountText, footerCountText), ctx.FooterLeftLabelStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(22f));
            GUILayout.Space(footerHorizontalPadding);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            return;
        }

        GUILayout.BeginHorizontal(ctx.BoxStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(30f));
        GUILayout.Space(footerHorizontalPadding);
        float footerCountWidth = Mathf.Ceil(footerCountRequiredWidth) + 4f;
        float footerLeftWidth = Mathf.Max(60f, ctx.PanelWidth - footerCountWidth - (footerHorizontalPadding * 2f) - footerGap);
        GUILayout.Label(new GUIContent(footerFocusDisplayText, footerFocusFullText), ctx.FooterLeftLabelStyle, GUILayout.Width(footerLeftWidth), GUILayout.MinHeight(22f));
        GUILayout.Space(footerGap);
        GUILayout.Label(new GUIContent(footerCountText, footerCountText), ctx.FooterRightLabelStyle, GUILayout.Width(footerCountWidth), GUILayout.MinHeight(22f));
        GUILayout.Space(footerHorizontalPadding);
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 오브젝트 node 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private static void DrawGameObjectNode(DebugConsoleHierarchyRenderContextShared ctx, GameObject go, int depth)
    {
        if (go == null || !ctx.ShouldShowGameObject(go))
            return;

        int id = go.GetInstanceID();
        bool objectEnabled = ctx.Manager.GetGameObjectEnabled(go);

        Component[] components = go.GetComponents<Component>();
        bool hasVisibleComponents = ctx.HasVisibleComponents(components);
        bool hasVisibleChildren = ctx.HasVisibleChildren(go);
        bool hasDetails = hasVisibleComponents || hasVisibleChildren;

        bool detailsExpanded = ctx.ExpandedComponents.Contains(id);
        bool childrenExpanded = ctx.ExpandedChildren.Contains(id);

        bool searchActive = !string.IsNullOrWhiteSpace(ctx.HierarchySearch);
        bool forceOpenDetails = searchActive && (ctx.HasMatchingComponent(go, ctx.HierarchySearch) || hasVisibleChildren);
        bool forceOpenChildren = searchActive && hasVisibleChildren;

        bool isObjectFocused = ctx.IsObjectFocused(id);
        bool isComponentParentFocused = ctx.IsFocusedObjectParent(id);
        bool showDetails = hasDetails && (detailsExpanded || forceOpenDetails);
        bool canShowChildControls = hasVisibleChildren && isObjectFocused;
        bool showChildren = hasVisibleChildren && (forceOpenChildren || (childrenExpanded && canShowChildControls));

        float objectLeadingSpace = depth * 18f;
        float rowContentWidth = GetHierarchyRowContentWidth(ctx);

        GUILayout.BeginVertical(ctx.GetHierarchyRowStyle(isObjectFocused, isComponentParentFocused, false), GUILayout.Width(rowContentWidth));
        GUILayout.BeginHorizontal(GUILayout.Width(rowContentWidth), GUILayout.Height(ctx.HierarchyRowHeight));
        GUILayout.Space(objectLeadingSpace);

        bool nextObjectEnabled = GUILayout.Toggle(objectEnabled, GUIContent.none, GUILayout.Width(ctx.HierarchyToggleSize), GUILayout.Height(ctx.HierarchyRowHeight));
        if (nextObjectEnabled != objectEnabled)
        {
            ctx.Manager.SetGameObjectEnabled(go, nextObjectEnabled);
            ctx.SaveState?.Invoke();
        }

        GUIStyle objectStyle = ctx.GetObjectButtonStyle(objectEnabled, isObjectFocused, isComponentParentFocused);
        GUIContent objectContent = new GUIContent(ctx.GetDisplayName(go.name), go.name);
        float objectButtonWidth = GetHierarchyTextButtonWidth(ctx, rowContentWidth, objectLeadingSpace, true, true);
        if (GUILayout.Button(objectContent, objectStyle, GUILayout.Width(objectButtonWidth), GUILayout.Height(ctx.HierarchyRowHeight)))
            ctx.ToggleGameObjectFocus(go);

        if (hasDetails)
        {
            string detailFoldoutLabel = showDetails ? "▾" : "▸";
            if (GUILayout.Button(detailFoldoutLabel, ctx.FoldoutButtonStyle, GUILayout.Width(ctx.HierarchyFoldoutSize), GUILayout.Height(ctx.HierarchyRowHeight)))
                ctx.ToggleExpandedSet(ctx.ExpandedComponents, id);
        }
        else
        {
            GUILayout.Space(ctx.HierarchyFoldoutSize);
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        if (showDetails && hasVisibleComponents)
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = objectEnabled;

            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (!ctx.ShouldShowComponent(component, go.name))
                    continue;

                bool isComponentFocused = ctx.IsComponentFocused(component.GetInstanceID());
                float componentLeadingSpace = (depth + 1) * 18f + ctx.HierarchyToggleSize + 8f;
                rowContentWidth = GetHierarchyRowContentWidth(ctx);

                GUILayout.BeginVertical(ctx.GetHierarchyRowStyle(false, false, isComponentFocused), GUILayout.Width(rowContentWidth));
                GUILayout.BeginHorizontal(GUILayout.Width(rowContentWidth), GUILayout.Height(ctx.HierarchyRowHeight));
                GUILayout.Space(componentLeadingSpace);

                bool componentEnabled = ctx.Manager.GetComponentEnabled(component);
                bool nextComponentEnabled = GUILayout.Toggle(componentEnabled, GUIContent.none, GUILayout.Width(ctx.HierarchyToggleSize), GUILayout.Height(ctx.HierarchyRowHeight));
                if (nextComponentEnabled != componentEnabled)
                {
                    ctx.Manager.SetComponentEnabled(component, nextComponentEnabled);
                    ctx.SaveState?.Invoke();
                }

                GUIStyle componentStyle = ctx.GetComponentButtonStyle(objectEnabled, isComponentFocused);
                GUIContent componentContent = new GUIContent(ctx.GetDisplayName(component.GetType().Name), component.GetType().Name);
                float componentButtonWidth = GetHierarchyTextButtonWidth(ctx, rowContentWidth, componentLeadingSpace, true, true);
                if (GUILayout.Button(componentContent, componentStyle, GUILayout.Width(componentButtonWidth), GUILayout.Height(ctx.HierarchyRowHeight)))
                    ctx.ToggleComponentFocus(component);

                GUILayout.Space(ctx.HierarchyFoldoutSize);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUI.enabled = previousEnabled;
        }

        if (!hasVisibleChildren)
            return;

        if (canShowChildControls)
        {
            float childLeadingSpace = (depth + 1) * 18f + ctx.HierarchyToggleSize + 8f + ctx.HierarchyToggleSize;

            GUILayout.BeginHorizontal(GUILayout.Width(rowContentWidth), GUILayout.Height(ctx.HierarchyRowHeight));
            GUILayout.Space((depth + 1) * 18f + ctx.HierarchyToggleSize + 8f);
            GUILayout.Space(ctx.HierarchyToggleSize);

            float childButtonWidth = GetHierarchyTextButtonWidth(ctx, rowContentWidth, childLeadingSpace, true, true);
            if (GUILayout.Button(new GUIContent("하위 오브젝트", "하위 오브젝트"), ctx.LinkButtonStyle, GUILayout.Width(childButtonWidth), GUILayout.Height(ctx.HierarchyRowHeight)))
                ctx.ToggleExpandedSet(ctx.ExpandedChildren, id);

            string childFoldoutLabel = showChildren ? "▾" : "▸";
            if (GUILayout.Button(childFoldoutLabel, ctx.FoldoutButtonStyle, GUILayout.Width(ctx.HierarchyFoldoutSize), GUILayout.Height(ctx.HierarchyRowHeight)))
                ctx.ToggleExpandedSet(ctx.ExpandedChildren, id);

            GUILayout.EndHorizontal();
        }

        if (!showChildren)
            return;

        for (int i = 0; i < go.transform.childCount; i++)
            DrawGameObjectNode(ctx, go.transform.GetChild(i).gameObject, depth + 1);
    }

    /// <summary>
    /// 계층 행 content 너비 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private static float GetHierarchyRowContentWidth(DebugConsoleHierarchyRenderContextShared ctx)
    {
        float width = ctx.PanelWidth;
        width -= ctx.BoxStyle.padding.left + ctx.BoxStyle.padding.right;
        width -= ctx.HierarchyRowContentRightReserve;
        return Mathf.Max(140f, width);
    }

    /// <summary>
    /// 계층 텍스트 버튼 너비 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private static float GetHierarchyTextButtonWidth(DebugConsoleHierarchyRenderContextShared ctx, float rowContentWidth, float leadingSpace, bool reserveToggle, bool reserveFoldout)
    {
        float width = rowContentWidth;
        width -= Mathf.Min(leadingSpace, ctx.MaxHierarchyIndentPenalty);

        if (reserveToggle)
            width -= ctx.HierarchyToggleSize + 4f;

        if (reserveFoldout)
            width -= ctx.HierarchyFoldoutSize + 4f;

        return Mathf.Max(92f, width);
    }
}
