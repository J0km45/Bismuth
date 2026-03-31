using UnityEngine;

public class HitEffectSpawner : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("VFX");
        GameObject childPrefab = transform.GetChild(0).gameObject;
        childPrefab.GetComponent<SpriteRenderer>().sortingLayerName = "VFX";
        Destroy(gameObject, 1f);
    }
}
