using System.Collections.Generic;
using System.Linq;

public class SkillTree
{
    public List<TreeNode> Nodes { get; set; }
    public HashSet<int> AllocatedIds { get; set; }
    public int AvailablePoints { get; set; }

    public bool TryAllocate(int nodeId, StatSet statset)
    {
        var node =  Nodes.Find(n => n.Id == nodeId);
        if (node == null)
            return false;
        if (AvailablePoints > 0 && !AllocatedIds.Contains(nodeId) && (node.IsRoot || node.NeighbourIds.Any(id => AllocatedIds.Contains(id))))
        {
            statset.Add(node.Reward);
            AvailablePoints--;
            AllocatedIds.Add(nodeId);
            return true;
        }
        return false;
    }
}
