using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DebugConsoleHierarchyRenderContextShared
{
    public DebugConsoleManager Manager;
    public float PanelWidth;
    public Vector2 Scroll;
    public string HierarchySearch = string.Empty;

    public GUIStyle TitleStyle;
    public GUIStyle BoxStyle;
    public GUIStyle LinkButtonStyle;
    public GUIStyle FoldoutButtonStyle;
    public GUIStyle FooterLeftLabelStyle;
    public GUIStyle FooterRightLabelStyle;

    public float HierarchyRowHeight;
    public float HierarchyToggleSize;
    public float HierarchyFoldoutSize;
    public float HierarchyRowContentRightReserve;
    public float MaxHierarchyIndentPenalty;

    public HashSet<int> ExpandedComponents;
    public HashSet<int> ExpandedChildren;

    public Func<int> GetVisibleEntryCount;
    public Func<string> GetFocusLabel;
    public Func<string> GetFooterFocusLabel;
    public Func<GameObject, bool> ShouldShowGameObject;
    public Func<Component[], bool> HasVisibleComponents;
    public Func<GameObject, bool> HasVisibleChildren;
    public Func<GameObject, string, bool> HasMatchingComponent;
    public Func<Component, string, bool> ShouldShowComponent;
    public Func<int, bool> IsObjectFocused;
    public Func<int, bool> IsFocusedObjectParent;
    public Func<int, bool> IsComponentFocused;
    public Func<bool, bool, bool, GUIStyle> GetHierarchyRowStyle;
    public Func<bool, bool, bool, GUIStyle> GetObjectButtonStyle;
    public Func<bool, bool, GUIStyle> GetComponentButtonStyle;
    public Func<string, string> GetDisplayName;
    public Action<GameObject> ToggleGameObjectFocus;
    public Action<Component> ToggleComponentFocus;
    public Action<HashSet<int>, int> ToggleExpandedSet;
    public Action SaveState;
}

public static class DebugConsoleHierarchyRendererShared
{
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

    private static float GetHierarchyRowContentWidth(DebugConsoleHierarchyRenderContextShared ctx)
    {
        float width = ctx.PanelWidth;
        width -= ctx.BoxStyle.padding.left + ctx.BoxStyle.padding.right;
        width -= ctx.HierarchyRowContentRightReserve;
        return Mathf.Max(140f, width);
    }

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
