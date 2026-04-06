using System.Collections.Generic;

public interface ICombinationUI
{
    public void Init(PlayerUIController playerUIController, int index);
    public void SetData(List<int> sourceIds, int resultId, bool canCombine, HashSet<int> set);
}
