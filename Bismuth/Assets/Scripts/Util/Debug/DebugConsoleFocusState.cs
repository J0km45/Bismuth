// ------------------------------------------------------------------------------
// 현재 포커스된 오브젝트와 컴포넌트 정보를 보관하고 갱신하는 상태 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
using System;

/// <summary>
/// 현재 포커스된 오브젝트와 컴포넌트 상태를 관리하는 클래스이다.
/// </summary>
public sealed class DebugConsoleFocusState
{
    /// <summary>
    /// focused 오브젝트 id 값을 외부에 노출한다. 내부 필드나 계산 결과를 읽기 쉽게 제공하기 위한 속성이다.
    /// </summary>
    public int FocusedGameObjectId { get; set; }
    /// <summary>
    /// focused 컴포넌트 id 값을 외부에 노출한다. 내부 필드나 계산 결과를 읽기 쉽게 제공하기 위한 속성이다.
    /// </summary>
    public int FocusedComponentId { get; set; }
    /// <summary>
    /// focused 오브젝트 이름 값을 외부에 노출한다. 내부 필드나 계산 결과를 읽기 쉽게 제공하기 위한 속성이다.
    /// </summary>
    public string FocusedObjectName { get; set; } = string.Empty;
    /// <summary>
    /// focused 컴포넌트 이름 값을 외부에 노출한다. 내부 필드나 계산 결과를 읽기 쉽게 제공하기 위한 속성이다.
    /// </summary>
    public string FocusedComponentName { get; set; } = string.Empty;

    /// <summary>
    /// has 오브젝트 포커스 여부를 계산해 반환한다. UI 표시나 분기 조건에서 바로 사용할 수 있다.
    /// </summary>
    public bool HasGameObjectFocus => FocusedGameObjectId != 0;
    /// <summary>
    /// has 컴포넌트 포커스 여부를 계산해 반환한다. UI 표시나 분기 조건에서 바로 사용할 수 있다.
    /// </summary>
    public bool HasComponentFocus => FocusedComponentId != 0;

    /// <summary>
    /// 오브젝트를 현재 포커스 대상으로 설정한다. 관련 선택 상태도 함께 갱신한다.
    /// </summary>
    public void FocusGameObject(int gameObjectId, string objectName)
    {
        FocusedGameObjectId = gameObjectId;
        FocusedComponentId = 0;
        FocusedObjectName = objectName ?? string.Empty;
        FocusedComponentName = string.Empty;
    }

    /// <summary>
    /// 컴포넌트를 현재 포커스 대상으로 설정한다. 관련 선택 상태도 함께 갱신한다.
    /// </summary>
    public void FocusComponent(int gameObjectId, int componentId, string objectName, string componentName)
    {
        FocusedGameObjectId = gameObjectId;
        FocusedComponentId = componentId;
        FocusedObjectName = objectName ?? string.Empty;
        FocusedComponentName = componentName ?? string.Empty;
    }

    /// <summary>
    /// 관련 작업를 비우거나 초기화한다. 이전 상태를 제거하고 다음 작업을 준비하는 데 사용한다.
    /// </summary>
    public void Clear()
    {
        FocusedGameObjectId = 0;
        FocusedComponentId = 0;
        FocusedObjectName = string.Empty;
        FocusedComponentName = string.Empty;
    }

    /// <summary>
    /// focused 오브젝트 상위 여부를 판정한다. 조건 분기에 사용할 수 있도록 bool 값을 반환한다.
    /// </summary>
    public bool IsFocusedObjectParent(int gameObjectId)
    {
        return FocusedGameObjectId == gameObjectId && FocusedComponentId != 0;
    }

    /// <summary>
    /// 오브젝트 focused 여부를 판정한다. 조건 분기에 사용할 수 있도록 bool 값을 반환한다.
    /// </summary>
    public bool IsObjectFocused(int gameObjectId)
    {
        return FocusedGameObjectId == gameObjectId && FocusedComponentId == 0;
    }

    /// <summary>
    /// 컴포넌트 focused 여부를 판정한다. 조건 분기에 사용할 수 있도록 bool 값을 반환한다.
    /// </summary>
    public bool IsComponentFocused(int componentId)
    {
        return FocusedComponentId == componentId;
    }

    /// <summary>
    /// label 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public string GetLabel()
    {
        if (FocusedComponentId != 0)
            return $"Focus : {FocusedObjectName}/{FocusedComponentName}";

        if (FocusedGameObjectId != 0)
            return $"Focus : {FocusedObjectName} (All Components)";

        return "Focus : All";
    }

    /// <summary>
    /// 하단 label 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public string GetFooterLabel(int segmentMaxLength)
    {
        if (FocusedComponentId != 0)
        {
            string objectName = TrimSegment(FocusedObjectName, segmentMaxLength);
            string componentName = TrimSegment(FocusedComponentName, segmentMaxLength);

            if (string.Equals(FocusedObjectName, FocusedComponentName, StringComparison.Ordinal))
                return $"Focus : {objectName}";

            return $"Focus : {objectName} / {componentName}";
        }

        if (FocusedGameObjectId != 0)
            return $"Focus : {TrimSegment(FocusedObjectName, segmentMaxLength)}";

        return "Focus : All";
    }

    /// <summary>
    /// suffix 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public string GetSuffix()
    {
        if (FocusedComponentId != 0)
            return $"({FocusedObjectName}/{FocusedComponentName})";

        if (FocusedGameObjectId != 0)
            return $"({FocusedObjectName})";

        return string.Empty;
    }

    /// <summary>
    /// trim 구간 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private static string TrimSegment(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (maxLength <= 0 || value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength) + "...";
    }
}
