using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class DebugConsoleLogRenderContextShared
{
    public float PanelWidth;
    public Vector2 Scroll;
    public bool AutoScroll;
    public float LastLogContentHeight;
    public float LastLogViewportHeight;
    public float LastMaxLogScrollY;
    public int SelectedLogIndex;
    public IReadOnlyList<DebugEntry> Entries;

    public GUIStyle TitleStyle;
    public GUIStyle BoxStyle;
    public GUIStyle RichLabelStyle;

    public Func<string> GetFocusSuffix;
    public Func<DebugEntry, bool> ShouldDisplayEntry;
    public Action<DebugEntry> FocusEntry;
    public Action<DebugEntry> OpenEntryScript;
}

public static class DebugConsoleLogRendererShared
{
    public static void Draw(DebugConsoleLogRenderContextShared ctx)
    {
        GUILayout.BeginVertical(ctx.BoxStyle, GUILayout.Width(ctx.PanelWidth), GUILayout.ExpandHeight(true));
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Logs {ctx.GetFocusSuffix()}", ctx.TitleStyle);
        GUILayout.EndHorizontal();

        float contentHeight = 0f;

        ctx.Scroll = GUILayout.BeginScrollView(ctx.Scroll, false, !ctx.AutoScroll, GUIStyle.none, ctx.AutoScroll ? GUIStyle.none : GUI.skin.verticalScrollbar);

        IReadOnlyList<DebugEntry> entries = ctx.Entries;
        float width = Mathf.Max(ctx.PanelWidth - (ctx.AutoScroll ? 20f : 32f), 300f);

        for (int i = 0; i < entries.Count; i++)
        {
            DebugEntry entry = entries[i];
            if (!ctx.ShouldDisplayEntry(entry))
                continue;

            float drawnHeight = DrawLogEntry(ctx, entry, i, width);
            contentHeight += drawnHeight + 4f;
            GUILayout.Space(4f);
        }

        GUILayout.EndScrollView();

        Rect scrollRect = GUILayoutUtility.GetLastRect();
        ctx.LastLogViewportHeight = scrollRect.height;
        ctx.LastLogContentHeight = contentHeight + 8f;
        ctx.LastMaxLogScrollY = Mathf.Max(0f, ctx.LastLogContentHeight - ctx.LastLogViewportHeight);

        if (Event.current.type == EventType.Repaint && ctx.AutoScroll)
            ctx.Scroll.y = ctx.LastMaxLogScrollY + 4f;

        GUILayout.EndVertical();
    }

    private static float DrawLogEntry(DebugConsoleLogRenderContextShared ctx, DebugEntry entry, int index, float width)
    {
        GUIContent content = new GUIContent(entry.RichText);
        float height = ctx.RichLabelStyle.CalcHeight(content, width);

        Rect rect = GUILayoutUtility.GetRect(10f, height + 12f, GUILayout.ExpandWidth(true));

        Color previousColor = GUI.color;
        if (index == ctx.SelectedLogIndex)
            GUI.color = new Color(0.75f, 0.85f, 1f, 1f);

        GUI.Box(rect, GUIContent.none);
        GUI.color = previousColor;

        Rect labelRect = new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, rect.height - 12f);
        GUI.Label(labelRect, content, ctx.RichLabelStyle);

        if (Event.current.type == EventType.MouseDown &&
            Event.current.button == 0 &&
            rect.Contains(Event.current.mousePosition))
        {
            ctx.SelectedLogIndex = index;
            ctx.FocusEntry(entry);

            if (Event.current.clickCount >= 2)
                ctx.OpenEntryScript(entry);

            Event.current.Use();
        }

        return rect.height;
    }

    private static bool IsNearBottom(float currentScrollY, float maxScrollY)
    {
        if (maxScrollY <= 0f)
            return true;

        float remaining = maxScrollY - currentScrollY;
        return remaining <= Mathf.Max(maxScrollY * 0.05f, 32f);
    }
}
