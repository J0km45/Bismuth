using System.Collections.Generic;
using UnityEngine;

public class CombinationScrollViewUI : MonoBehaviour
{
    [SerializeField] private Transform _combinationContent; // 조합 스크롤뷰 속 Content
    [SerializeField] private GameObject _pairPrefab; // 2개 조합 프리팹
    [SerializeField] private GameObject _trioPrefab; // 3개조합 프리팹
    [SerializeField] private CombineManager _combineManager;

    private List<GameObject> _combinations = new List<GameObject>();

    private void OnEnable()
    {
        _combineManager.OnCombineListChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        _combineManager.OnCombineListChanged -= Refresh;
    }

    public void Refresh()
    {
        ClearCombinations();

        for (int i = 0; i < _combineManager.CombineList.Count; i++)
        {
            int[] recipe = _combineManager.CombineList[i];
            InitCombination(recipe, i);
        }
    }

    private void InitCombination(int[] recipe, int index)
    {
        int length = recipe.Length;
        int sourceCount = length - 2;

        GameObject prefab = sourceCount >= 3 ? _trioPrefab : _pairPrefab;
        GameObject combination = Instantiate(prefab, _combinationContent);

        if (combination.TryGetComponent(out ICombinationUI combi))
        {
            List<int> sourceIds = new List<int>();

            for (int i = 0; i < sourceCount; i++)
            {
                sourceIds.Add(recipe[i]);
            }

            int resultId = recipe[length - 2];
            bool canCombine = recipe[length - 1] == 1; // 0이면 조합 불가, 1이면 조합 가능

            combi.Init(_combineManager, index);
            combi.SetData(sourceIds, resultId, canCombine);
        }

        _combinations.Add(combination);
    }

    // _combinations 리스트에 있는 모든 프리팹 삭제 후 리스트 비워줌
    private void ClearCombinations()
    {
        for (int i = 0; i < _combinations.Count; i++)
        {
            if (_combinations[i] != null)
            {
                Destroy(_combinations[i]);
            }
        }

        _combinations.Clear();
    }
}
