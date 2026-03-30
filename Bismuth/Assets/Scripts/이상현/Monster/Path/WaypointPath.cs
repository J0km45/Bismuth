using UnityEngine;

/// <summary>
/// 자식 Transform을 웨이포인트 경로로 사용하는 스크립트
/// 자식 순서대로 경로를 제공
/// </summary>
public class WaypointPath : MonoBehaviour
{
    public int WaypointCount => transform.childCount;

    public Transform GetWaypoint(int index) => transform.GetChild(index);

    private void OnDrawGizmos()
    {
        int waypointCount = transform.childCount;
        if (waypointCount < 2) return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < waypointCount; i++)
        {
            Transform current = transform.GetChild(i);
            Gizmos.DrawSphere(current.position, 0.1f);

            if (i < waypointCount - 1)
            {
                Transform next = transform.GetChild(i + 1);
                Gizmos.DrawLine(current.position, next.position);
            }
        }
    }
}