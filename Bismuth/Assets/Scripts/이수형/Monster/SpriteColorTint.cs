using System.Collections.Generic;
using UnityEngine;


public class SpriteColorTint
{
    private readonly SpriteRenderer[] _renderers;
    private readonly Dictionary<SpriteRenderer, Color> _originalColors = new();
    private readonly string _ownerName;
    private bool _isTinted;

    public bool IsTinted => _isTinted;


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
