using System.Collections;
using TMPro;
using UnityEngine;

public class DamageSkin : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;

    [Header("Motion")]
    [SerializeField] private float _duration = 0.8f;
    [SerializeField] private float _upDistance = 0.5f;
    [SerializeField] private float _downDistance = 0.3f;

    private Color _originColor;
    private Color _originColor_outline;

    private void Awake()
    {
        _text = GetComponentInChildren<TMP_Text>();
        _originColor = _text.color;
    }

    public void Initialize(int damage, Vector3 startPos)
    {
        transform.position = startPos;
        _text.text = damage.ToString();

        StopAllCoroutines();
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        float time = 0f;
        Vector3 start = transform.position;
        Vector3 peak = start + Vector3.up * _upDistance;
        Vector3 end = peak + Vector3.down * _downDistance;

        while (time < _duration)
        {
            time += Time.deltaTime;
            float t = time / _duration;

            if (t < 0.35f)
            {
                float moveT = t / 0.35f;
                transform.position = Vector3.Lerp(start, peak, moveT);
            }
            else
            {
                float moveT = (t - 0.35f) / 0.65f;
                transform.position = Vector3.Lerp(peak, end, moveT);
            }

            Color color = _originColor;
            Color color_outline = _originColor_outline;
            color.a = Mathf.Lerp(1f, 0f, t);
            _text.color = color;

            yield return null;
        }

        Destroy(gameObject);
    }
}