using UnityEngine;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    [SerializeField] private DamageSkin damageTextPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ShowDamageText(int damage, Vector3 worldPos)
    {
        DamageSkin text = Instantiate(damageTextPrefab, worldPos, Quaternion.identity);

        Vector3 randomOffset = new Vector3(
            Random.Range(-0.3f, 0.3f),
            Random.Range(0.0f, 0.25f),
            0f
        );

        text.Initialize(damage, worldPos + randomOffset);
    }
}