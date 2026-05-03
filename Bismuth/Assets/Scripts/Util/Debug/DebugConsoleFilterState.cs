// ------------------------------------------------------------------------------
// 런타임과 에디터 디버그 콘솔에서 공통으로 쓰는 필터 상태 데이터를 보관하는 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
using System;

[Serializable]
/// <summary>
/// 필터 옵션의 현재 값을 한 곳에 모아 저장하는 상태 클래스이다.
/// </summary>
public sealed class DebugConsoleFilterState
{
    // 자동 스크롤 값을 저장한다. 새 로그가 들어왔을 때 마지막 항목으로 자동 이동할지 결정한다.
    public bool AutoScroll = true;
    // 숨김 transform 값을 저장한다. Transform 컴포넌트를 목록에서 숨길지 결정한다.
    public bool HideTransform = true;
    // 묶기 이전 on 선택 값을 저장한다. 다른 대상을 선택했을 때 이전에 펼친 항목을 접을지 결정한다.
    public bool CollapsePreviousOnSelection = true;
    // show 타입 필터 패널 값을 저장한다. 타입 필터 패널 표시 여부를 저장한다.
    public bool ShowTypeFilterPanel;
    // 전체 활성화 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public bool GlobalEnabled = true;
    // 미러 to 유니티 console 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public bool MirrorToUnityConsole;
}
