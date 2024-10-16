namespace FlowForge;

public class FlowNode
{
    public string Id { get; private set; }
    public string Label { get; set; } // Display label for the node.
    
    public FlowNode(string id, string? label = null)
    {
        Id = id;
        Label = label ?? id;
    }

    public override string ToString()
    {
        return Label;
    }
}
