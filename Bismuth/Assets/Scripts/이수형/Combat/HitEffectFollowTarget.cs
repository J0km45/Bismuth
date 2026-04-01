using UnityEngine;

public class HitEffectFollowTarget : MonoBehaviour
{
    [SerializeField] private Vector3 localOffset;

    private Transform target;
    private bool isFollowing;

    public void Initialize(Transform target, Vector3 localOffset = default)
    {
        this.target = target;
        this.localOffset = localOffset;
        isFollowing = target != null;

        if (isFollowing)
            transform.position = target.position + localOffset;
    }

    private void LateUpdate()
    {
        if (!isFollowing)
            return;

        if (target == null || !target.gameObject.activeInHierarchy)
        {
            StopFollowing();
            return;
        }

        transform.position = target.position + localOffset;
    }

    public void StopFollowing()
    {
        isFollowing = false;
        target = null;
    }
}