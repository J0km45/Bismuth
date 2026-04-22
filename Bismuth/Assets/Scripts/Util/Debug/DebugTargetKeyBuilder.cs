// ------------------------------------------------------------------------------
// 오브젝트와 컴포넌트를 안정적으로 추적하기 위한 키와 경로 문자열을 만드는 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
using System.Text;
using UnityEngine;

/// <summary>
/// 씬, 계층 경로, 오브젝트 키, 컴포넌트 키를 만드는 정적 클래스이다.
/// </summary>
public static class DebugTargetKeyBuilder
{
    /// <summary>
    /// 씬 식별 키 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
    /// </summary>
    public static string BuildSceneKey(GameObject go)
    {
        if (go == null)
            return "[NullScene]";

        if (!go.scene.IsValid())
            return "[InvalidScene]";

        if (!string.IsNullOrWhiteSpace(go.scene.path))
            return go.scene.path;

        if (!string.IsNullOrWhiteSpace(go.scene.name))
            return go.scene.name;

        return "[UnnamedScene]";
    }

    /// <summary>
    /// 계층 경로 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
    /// </summary>
    public static string BuildHierarchyPath(Transform transform)
    {
        if (transform == null)
            return string.Empty;

        StringBuilder builder = new StringBuilder(BuildPathSegment(transform));
        Transform current = transform.parent;

        while (current != null)
        {
            builder.Insert(0, '/');
            builder.Insert(0, BuildPathSegment(current));
            current = current.parent;
        }

        return builder.ToString();
    }

    /// <summary>
    /// 오브젝트 식별 키 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
    /// </summary>
    public static string BuildGameObjectKey(GameObject go)
    {
        if (go == null)
            return string.Empty;

        return $"{BuildSceneKey(go)}|{BuildHierarchyPath(go.transform)}";
    }

    /// <summary>
    /// 컴포넌트 식별 키 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
    /// </summary>
    public static string BuildComponentKey(Component component)
    {
        if (component == null)
            return string.Empty;

        int sameTypeIndex = GetSameTypeIndex(component);
        return $"{BuildGameObjectKey(component.gameObject)}|{component.GetType().FullName}#{sameTypeIndex}";
    }

    /// <summary>
    /// 경로 구간 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
    /// </summary>
    private static string BuildPathSegment(Transform transform)
    {
        return $"{transform.name}[{transform.GetSiblingIndex()}]";
    }

    /// <summary>
    /// same 타입 index 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private static int GetSameTypeIndex(Component component)
    {
        Component[] components = component.GetComponents<Component>();
        int sameTypeIndex = 0;

        for (int i = 0; i < components.Length; i++)
        {
            Component current = components[i];
            if (current == null)
                continue;

            if (current.GetType() != component.GetType())
                continue;

            if (current == component)
                return sameTypeIndex;

            sameTypeIndex++;
        }

        return sameTypeIndex;
    }
}
