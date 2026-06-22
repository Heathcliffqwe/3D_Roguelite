using System.Collections.Generic;

public class TreeNode
{
    public int Id { get; set; }
    public List<int> NeighbourIds { get; set; }
    public Modifier Reward { get; set; }
    public bool IsRoot { get; set; }
    
}