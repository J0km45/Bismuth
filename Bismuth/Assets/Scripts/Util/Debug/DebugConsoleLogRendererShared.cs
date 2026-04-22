// ------------------------------------------------------------------------------
// 런타임과 에디터에서 공통으로 쓰는 로그 패널 렌더링 보조 코드를 모아둔 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로그 패널 렌더링에 필요한 데이터와 콜백을 전달하는 컨텍스트 클래스이다.
/// </summary>
public sealed class DebugConsoleLogRenderContextShared
{
    // 패널 너비 값을 저장한다. 패널의 현재 너비 값을 저장한다. 레이아웃 계산과 렌더링 폭 결정에 사용한다.
    public float PanelWidth;
    // 스크롤 값을 저장한다. 현재 스크롤 위치를 저장한다. 다시 그릴 때 사용자가 보고 있던 위치를 유지하는 데 사용한다.
    public Vector2 Scroll;
    // 자동 스크롤 값을 저장한다. 새 로그가 들어왔을 때 마지막 항목으로 자동 이동할지 결정한다.
    public bool AutoScroll;
    // last 로그 content 높이 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public float LastLogContentHeight;
    // last 로그 viewport 높이 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public float LastLogViewportHeight;
    // last 최대 로그 스크롤 y 값을 저장한다. 로그 패널의 스크롤 위치를 저장한다.
    public float LastMaxLogScrollY;
    // selected 로그 index 값을 저장한다. 현재 선택된 로그 항목의 인덱스를 저장한다.
    public int SelectedLogIndex;
    // 엔트리 목록 값을 저장한다. 현재 처리하거나 렌더링할 로그 엔트리 목록을 저장한다.
    public IReadOnlyList<DebugEntry> Entries;

    // 제목 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle TitleStyle;
    // box 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle BoxStyle;
    // 리치 label 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle RichLabelStyle;

    // get 포커스 suffix 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public Func<string> GetFocusSuffix;
    // should 표시 엔트리 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public Func<DebugEntry, bool> ShouldDisplayEntry;
    // 포커스 엔트리 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public Action<DebugEntry> FocusEntry;
    // open 엔트리 script 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public Action<DebugEntry> OpenEntryScript;
}

/// <summary>
/// 공통 로그 패널 UI를 그리는 정적 렌더러 클래스이다.
/// </summary>
public static class DebugConsoleLogRendererShared
{
    /// <summary>
    /// 관련 작업 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
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

    /// <summary>
    /// 로그 엔트리 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
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

    /// <summary>
    /// near bottom 여부를 판정한다. 조건 분기에 사용할 수 있도록 bool 값을 반환한다.
    /// </summary>
    private static bool IsNearBottom(float currentScrollY, float maxScrollY)
    {
        if (maxScrollY <= 0f)
            return true;

        float remaining = maxScrollY - currentScrollY;
        return remaining <= Mathf.Max(maxScrollY * 0.05f, 32f);
    }
}
