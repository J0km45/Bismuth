using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 스크롤뷰에 들어갈 스크립트
public class SynergyScrollViewUI : MonoBehaviour
{
    [SerializeField] private Transform _synergyContent; // 시너지 스크롤뷰 속 Content
    [SerializeField] private SynergyUI _synergyPrefab;
    [SerializeField] private SynergyManager _synergyManager;
    [SerializeField] private SynergySO _synergySO;

    private List<SynergyUI> _synergys = new List<SynergyUI>(); // 존재하는 시너지 프리팹 리스트

    private void OnEnable()
    {
        _synergyManager.OnSynergyChanged += Refresh;
    }

    private void OnDisable()
    {
        _synergyManager.OnSynergyChanged -= Refresh;
    }

    // 시너지 목록 바뀔때 Refresh 호출
    public void Refresh(Dictionary<int, List<int>> dict)
    {
        ClearSynergys(); // 변화가 있을때마다 리스트 비우고 다시 채움

        List<KeyValuePair<int, List<int>>> sortedList = dict
            .OrderByDescending(pair => IsActivated(pair.Key, pair.Value.Count))   // 활성화 먼저
            .ThenByDescending(pair => GetProgress(pair.Key, pair.Value.Count))    // 최대카운트에 가까운 순
            .ThenByDescending(pair => pair.Value.Count)                           // 현재 카운트 큰 순
            .ToList();

        // 받아온 딕셔너리에서 시너지프리팹 하나씩 생성
        foreach (KeyValuePair<int, List<int>> pair in sortedList)
        {
            int synergyId = pair.Key;
            int count = pair.Value.Count;

            SynergyUI synergy = Instantiate(_synergyPrefab, _synergyContent);
            synergy.SetData(synergyId, count, _synergySO);
            _synergys.Add(synergy);
        }
    }

    // 시너지 활성화 여부 체크
    private bool IsActivated(int synergyId, int count)
    {
        SynergyData data = GetSynergyData(synergyId);

        return count >= data.Levels[0].ActiveCount;
    }

    // 현재 시너지 수가 최대 시너지 수에 얼마나 가까운지 체크
    private float GetProgress(int synergyId, int count)
    {
        SynergyData data = GetSynergyData(synergyId);
        int maxCount = data.Levels[data.Levels.Count - 1].ActiveCount;

        return (float)count / maxCount;
    }

    private SynergyData GetSynergyData(int synergyId)
    {
        foreach (SynergyData row in _synergySO.Rows)
        {
            if (row.ID == synergyId) return row;
        }

        return null;
    }

    // _synergys 리스트에 있는 모든 프리팹 삭제 후 리스트 비워줌
    private void ClearSynergys()
    {
        for (int i = 0; i < _synergys.Count; i++)
        {
            if (_synergys[i] != null)
                Destroy(_synergys[i].gameObject);
        }

        _synergys.Clear();
        DebugTool.Log("Synergys cleared", DebugType.Synergy, this);
    }
}
