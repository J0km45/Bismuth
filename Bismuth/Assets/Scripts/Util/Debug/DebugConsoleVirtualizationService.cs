// ------------------------------------------------------------------------------
// 대량 로그를 그릴 때 필요한 스크롤 범위와 가상화 계산을 담당하는 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
using UnityEngine;

/// <summary>
/// 스크롤 범위와 가상화 계산을 담당하는 정적 서비스 클래스이다.
/// </summary>
public static class DebugConsoleVirtualizationService
{
    /// <summary>
    /// 최대 스크롤 y 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public static float GetMaxScrollY(float contentHeight, float viewportHeight)
    {
        return Mathf.Max(0f, contentHeight - viewportHeight);
    }

    /// <summary>
    /// 자동 스크롤를 수행해야 하는지 정책적으로 판단한다.
    /// </summary>
    public static bool ShouldAutoScroll(bool autoScroll)
    {
        return autoScroll;
    }
}
