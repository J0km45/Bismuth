using System.Collections.Generic;

public interface ICombinationUI
{
    public void Init(CombineManager combineManager, int index);
    public void SetData(List<int> sourceIds, int resultId, bool canCombine);
}
