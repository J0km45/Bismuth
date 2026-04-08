using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 특정 GameObject 하위의 모든 SpriteRenderer에 색상 틴트를 적용/해제하는 유틸 클래스.
/// 틴트 적용 시 원본 색상을 저장하고 틴트 색상을 곱하며, 해제 시 원본으로 복원한다.
/// </summary>
public class SpriteColorTint
{
    private readonly SpriteRenderer[] _renderers;
    private readonly Dictionary<SpriteRenderer, Color> _originalColors = new();
    private readonly string _ownerName;
    private bool _isTinted;

    public bool IsTinted => _isTinted;

    /// <summary>
    /// 대상 오브젝트 하위의 모든 SpriteRenderer를 수집하여 초기화한다.
    /// </summary>
    /// <param name="target">틴트를 적용할 루트 오브젝트</param>
    public SpriteColorTint(GameObject target)
    {
        if (target == null)
        {
            _renderers = System.Array.Empty<SpriteRenderer>();
            _ownerName = "None";
            DebugTool.Warnning("SpriteColorTint 초기화 실패 : target이 null입니다.", DebugType.Unit);
            return;
        }

        _ownerName = target.name;
        _renderers = target.GetComponentsInChildren<SpriteRenderer>(true);

        DebugTool.Log(
            $"SpriteColorTint 초기화 | owner={_ownerName}, rendererCount={_renderers.Length}",
            DebugType.Unit);
    }

    /// <summary>
    /// 틴트 색상을 적용한다. 원본 색상을 저장한 뒤 틴트를 곱한다.
    /// </summary>
    /// <param name="tintColor">적용할 틴트 색상</param>
    public void Apply(Color tintColor)
    {
        _originalColors.Clear();

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null)
                continue;

            _originalColors[_renderers[i]] = _renderers[i].color;
            _renderers[i].color *= tintColor;
        }

        _isTinted = true;

        DebugTool.Log(
            $"틴트 적용 | owner={_ownerName}, color={tintColor}",
            DebugType.Unit);
    }

    /// <summary>
    /// 틴트를 해제하고 저장해둔 원본 색상으로 복원한다.
    /// </summary>
    public void Remove()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null)
                continue;

            if (_originalColors.TryGetValue(_renderers[i], out Color original))
                _renderers[i].color = original;
        }

        _originalColors.Clear();
        _isTinted = false;

        DebugTool.Log(
            $"틴트 해제 | owner={_ownerName}",
            DebugType.Unit);
    }
}
