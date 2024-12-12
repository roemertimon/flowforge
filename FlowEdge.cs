namespace FlowForge;

using QuikGraph;

public class FlowEdge : Edge<FlowNode>
{
    public double MaxFlow { get; set; }
    public double Residual { get; set; }
    public bool IsBackwards { get; set; }

    public FlowEdge(FlowNode source, FlowNode target, double maxFlow)
        : base(source, target)
    {
        IsBackwards = false;
        MaxFlow = maxFlow;
        Residual = maxFlow;
    }

    public override string ToString()
    {
        return $"{Source.Id} -> {Target.Id} [ResidualFlow: {Residual}, MaxFlow: {MaxFlow}]";
    }
}
