namespace FlowForge;

using QuikGraph;

public class FlowEdge : Edge<FlowNode>
{
    public double MaxFlow { get; set; }
    public double CurrentFlow { get; set; }

    public FlowEdge(FlowNode source, FlowNode target, double maxFlow)
        : base(source, target)
    {
        MaxFlow = maxFlow;
        CurrentFlow = 0; // Initially, the current flow is set to zero.
    }

    public override string ToString()
    {
        return $"{Source.Id} -> {Target.Id} [CurrentFlow: {CurrentFlow}, MaxFlow: {MaxFlow}]";
    }
}
