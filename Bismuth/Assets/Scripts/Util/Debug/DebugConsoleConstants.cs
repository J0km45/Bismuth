// ------------------------------------------------------------------------------
// 디버그 콘솔에서 공통으로 사용하는 크기, 길이 제한, 기본 레이아웃 값을 모아둔 상수 정의 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
using UnityEngine;

/// <summary>
/// 디버그 콘솔에서 공통으로 참조하는 상수 모음을 제공하는 정적 클래스이다.
/// </summary>
public static class DebugConsoleConstants
{
    // 최대 표시 이름 length 값을 저장한다. 표시용 이름 값을 저장한다.
    public const int MaxDisplayNameLength = 15;
    // 하단 포커스 구간 최대 length 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public const int FooterFocusSegmentMaxLength = 16;

    // 계층 행 높이 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public const float HierarchyRowHeight = 22f;
    // 계층 토글 크기 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public const float HierarchyToggleSize = 18f;
    // 계층 foldout 크기 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public const float HierarchyFoldoutSize = 18f;
    // 패널 splitter 너비 값을 저장한다. 인스턴스 식별자 값을 저장한다.
    public const float PanelSplitterWidth = 6f;
    // 최대 계층 indent 패널티 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public const float MaxHierarchyIndentPenalty = 24f;
    // 최소 계층 패널 너비 값을 저장한다. 패널의 현재 너비 값을 저장한다. 레이아웃 계산과 렌더링 폭 결정에 사용한다.
    public const float MinHierarchyPanelWidth = 220f;
    // 최소 로그 패널 너비 값을 저장한다. 패널의 현재 너비 값을 저장한다. 레이아웃 계산과 렌더링 폭 결정에 사용한다.
    public const float MinLogPanelWidth = 220f;
    // 계층 행 content right 예약 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public const float HierarchyRowContentRightReserve = 18f;

    // 기본 에디터 계층 패널 너비 값을 저장한다. 패널의 현재 너비 값을 저장한다. 레이아웃 계산과 렌더링 폭 결정에 사용한다.
    public const float DefaultEditorHierarchyPanelWidth = 420f;
    // 기본 런타임 계층 패널 너비 값을 저장한다. 패널의 현재 너비 값을 저장한다. 레이아웃 계산과 렌더링 폭 결정에 사용한다.
    public const float DefaultRuntimeHierarchyPanelWidth = 480f;
    // 기본 런타임 창 x 값을 저장한다. 시간 관련 값을 저장한다.
    public const float DefaultRuntimeWindowX = 20f;
    // 기본 런타임 창 y 값을 저장한다. 시간 관련 값을 저장한다.
    public const float DefaultRuntimeWindowY = 20f;
    // 기본 런타임 창 너비 값을 저장한다. 시간 관련 값을 저장한다.
    public const float DefaultRuntimeWindowWidth = 1450f;
    // 기본 런타임 창 높이 값을 저장한다. 시간 관련 값을 저장한다.
    public const float DefaultRuntimeWindowHeight = 850f;

    public static readonly Rect DefaultRuntimeWindowRect = new Rect(
        DefaultRuntimeWindowX,
        DefaultRuntimeWindowY,
        DefaultRuntimeWindowWidth,
        DefaultRuntimeWindowHeight);
}
