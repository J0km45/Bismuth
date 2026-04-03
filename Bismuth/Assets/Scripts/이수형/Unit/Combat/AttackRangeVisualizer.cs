using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AttackRangeVisualizer : MonoBehaviour
{
    [SerializeField] private CircleCollider2D sensor;
    [SerializeField] private int segments = 60;
    [SerializeField] private float lineWidth = 0.05f;

    private LineRenderer lr;

    public void Init(CircleCollider2D circle, LineRenderer lineRenderer)
    {
        sensor = circle;
        lr = lineRenderer;

        SetupLineRenderer();
        DrawCircle();
        lr.enabled = false; 
    }

    private void Awake()
    {
        if (lr == null)
            lr = GetComponent<LineRenderer>();

        if (sensor == null)
            sensor = GetComponent<CircleCollider2D>();
    }

    private void SetupLineRenderer()
    {
        if (lr == null) return;

        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = segments;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
    }

    public void Show()
    {
        DrawCircle();
        if (lr != null) lr.enabled = true;
    }

    public void Hide()
    {
        if (lr != null) lr.enabled = false;
    }

    public void Refresh()
    {
        DrawCircle();
    }

    private void DrawCircle()
    {
        if (sensor == null || lr == null) return;

        float radius = sensor.radius;

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, y, 0f));
        }
    }
}