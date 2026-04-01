using System.Collections;
using UnityEngine;

public class HitEffectSpawner : MonoBehaviour
{
    [SerializeField] private string sortingLayerName = "VFX";
    [SerializeField, Min(0.05f)] private float fallbackLifetime = 0.5f;
    [SerializeField] private bool followLog = false;

    private Animator[] animators;
    private ParticleSystem[] particleSystems;
    private SpriteRenderer[] spriteRenderers;
    private TrailRenderer[] trailRenderers;

    private Coroutine lifeRoutine;
    private HitEffectPoolMember poolMember;

    private Vector3 baseLocalScale;
    private bool baseScaleCached;

    private Transform followTarget;
    private Vector3 followOffset;
    private bool freezeOnTargetLost;
    private bool isFollowingTarget;

    private void Awake()
    {
        animators = GetComponentsInChildren<Animator>(true);
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        trailRenderers = GetComponentsInChildren<TrailRenderer>(true);
        poolMember = GetComponent<HitEffectPoolMember>();

        CacheBaseScale();
    }

    private void OnEnable()
    {
        ApplyVfxLayer();
        RestartVisuals();

        if (lifeRoutine != null)
            StopCoroutine(lifeRoutine);

        lifeRoutine = StartCoroutine(WaitForEffectEnd());
    }

    private void LateUpdate()
    {
        UpdateFollowTarget();
    }

    private void OnDisable()
    {
        if (lifeRoutine != null)
        {
            StopCoroutine(lifeRoutine);
            lifeRoutine = null;
        }

        ClearFollowTarget();
    }

    public void ConfigureSpawn(float uniformScaleMultiplier)
    {
        CacheBaseScale();

        float clampedScale = Mathf.Max(0.01f, uniformScaleMultiplier);
        transform.localScale = baseLocalScale * clampedScale;
    }

    public void ConfigureFollowTarget(Transform target, bool freezeOnTargetLost = true)
    {
        ConfigureFollowTarget(target, Vector3.zero, freezeOnTargetLost);
    }

    public void ConfigureFollowTarget(Transform target, Vector3 worldOffset, bool freezeOnTargetLost = true)
    {
        followTarget = target;
        followOffset = worldOffset;
        this.freezeOnTargetLost = freezeOnTargetLost;
        isFollowingTarget = target != null;

        if (!isFollowingTarget)
            return;

        transform.position = followTarget.position + followOffset;

        if (followLog)
        {
            DebugTool.Log(
                $"히트 이펙트 추적 시작 | target={followTarget.name}",
                DebugType.Unit,
                this
            );
        }
    }

    public void ClearFollowTarget()
    {
        followTarget = null;
        followOffset = Vector3.zero;
        freezeOnTargetLost = false;
        isFollowingTarget = false;
    }

    private void UpdateFollowTarget()
    {
        if (!isFollowingTarget)
            return;

        if (followTarget != null && followTarget.gameObject.activeInHierarchy)
        {
            transform.position = followTarget.position + followOffset;
            return;
        }

        if (followLog)
        {
            DebugTool.Log(
                "히트 이펙트 추적 종료 | 마지막 위치에 고정",
                DebugType.Unit,
                this
            );
        }

        if (!freezeOnTargetLost)
        {
            ReleaseOrDestroy();
            return;
        }

        followTarget = null;
        followOffset = Vector3.zero;
        freezeOnTargetLost = false;
        isFollowingTarget = false;
    }

    private void ApplyVfxLayer()
    {
        int vfxLayer = LayerMask.NameToLayer("VFX");
        if (vfxLayer >= 0)
            gameObject.layer = vfxLayer;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
                continue;

            spriteRenderers[i].sortingLayerName = sortingLayerName;
        }
    }

    private void RestartVisuals()
    {
        for (int i = 0; i < trailRenderers.Length; i++)
        {
            if (trailRenderers[i] != null)
                trailRenderers[i].Clear();
        }

        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] == null)
                continue;

            animators[i].Rebind();
            animators[i].Update(0f);
        }

        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] == null)
                continue;

            particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystems[i].Play(true);
        }
    }

    private IEnumerator WaitForEffectEnd()
    {
        // Animator state 정보가 첫 프레임에 안정화되도록 1프레임 대기
        yield return null;

        bool hasAnimator = animators != null && animators.Length > 0;
        bool hasParticle = particleSystems != null && particleSystems.Length > 0;

        if (!hasAnimator && !hasParticle)
        {
            yield return new WaitForSeconds(fallbackLifetime);
            ReleaseOrDestroy();
            yield break;
        }

        while (true)
        {
            bool animatorFinished = !hasAnimator || AreAnimatorsFinished();
            bool particleFinished = !hasParticle || AreParticlesFinished();

            if (animatorFinished && particleFinished)
                break;

            yield return null;
        }

        ReleaseOrDestroy();
    }

    private bool AreAnimatorsFinished()
    {
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null || !animator.isActiveAndEnabled)
                continue;

            if (animator.IsInTransition(0))
                return false;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            if (state.normalizedTime < 1f)
                return false;
        }

        return true;
    }

    private bool AreParticlesFinished()
    {
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem ps = particleSystems[i];
            if (ps == null)
                continue;

            if (ps.IsAlive(true))
                return false;
        }

        return true;
    }

    private void ReleaseOrDestroy()
    {
        if (poolMember != null)
        {
            poolMember.ReleaseToPool();
            return;
        }

        Destroy(gameObject);
    }

    private void CacheBaseScale()
    {
        if (baseScaleCached)
            return;

        baseLocalScale = transform.localScale;
        baseScaleCached = true;
    }
}
