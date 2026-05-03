// ------------------------------------------------------------------------------
// 오브젝트와 컴포넌트를 필터링하기 위한 식별 키를 편하게 얻도록 감싼 유틸리티 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
using UnityEngine;

/// <summary>
/// 대상 식별 키를 외부 코드에서 쉽게 얻도록 감싼 정적 유틸 클래스이다.
/// </summary>
public static class DebugConsoleFilterKeyUtility
{
    /// <summary>
    /// 씬 식별 키 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public static string GetSceneKey(GameObject gameObject)
    {
        return DebugTargetKeyBuilder.BuildSceneKey(gameObject);
    }

    /// <summary>
    /// 계층 경로 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public static string GetHierarchyPath(Transform target)
    {
        return DebugTargetKeyBuilder.BuildHierarchyPath(target);
    }

    /// <summary>
    /// 오브젝트 식별 키 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public static string GetGameObjectKey(GameObject gameObject)
    {
        return DebugTargetKeyBuilder.BuildGameObjectKey(gameObject);
    }

    /// <summary>
    /// 컴포넌트 식별 키 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public static string GetComponentKey(Component component)
    {
        return DebugTargetKeyBuilder.BuildComponentKey(component);
    }
}
