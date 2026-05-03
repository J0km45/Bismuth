// ------------------------------------------------------------------------------
// 디버그 콘솔의 스크롤 위치와 창 레이아웃 정보를 저장하는 상태 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
using System;
using UnityEngine;

[Serializable]
/// <summary>
/// 디버그 콘솔 레이아웃과 스크롤 상태를 저장하는 클래스이다.
/// </summary>
public sealed class DebugConsoleLayoutState
{
    // 계층 스크롤 값을 저장한다. 계층 패널의 스크롤 위치를 저장한다.
    public Vector2 HierarchyScroll;
    // 로그 스크롤 값을 저장한다. 로그 패널의 스크롤 위치를 저장한다.
    public Vector2 LogScroll;
    // 타입 필터 스크롤 값을 저장한다. 타입 필터 패널의 스크롤 위치를 저장한다.
    public Vector2 TypeFilterScroll;
    // 창 영역 값을 저장한다. 런타임 창의 위치와 크기를 저장한다.
    public Rect WindowRect = DebugConsoleConstants.DefaultRuntimeWindowRect;
    // 계층 패널 너비 값을 저장한다. 패널의 현재 너비 값을 저장한다. 레이아웃 계산과 렌더링 폭 결정에 사용한다.
    public float HierarchyPanelWidth = DebugConsoleConstants.DefaultEditorHierarchyPanelWidth;
}
