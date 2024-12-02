namespace FlowForge;

using QuikGraph;

public class FlowEdge : Edge<FlowNode>
{
    public double MaxFlow { get; set; }
    
    public double CurrentFlow { get; set; }
    
    public double Residual
    {
        get => MaxFlow - CurrentFlow;
        //set => CurrentFlow = MaxFlow - value;
        //make the setter private
        private set => CurrentFlow = MaxFlow - value;
    }
    public bool IsBackwards { get; set; }
    
    public bool IsHighlighted { get; set; }

    public FlowEdge(FlowNode source, FlowNode target, double maxFlow) : base(source, target)
    {
        IsBackwards = false;
        MaxFlow = maxFlow;
        CurrentFlow = 0;
    }

    public override string ToString()
    {
        return $"{Source.Id} -> {Target.Id} [ResidualFlow: {Residual}, MaxFlow: {MaxFlow}]";
    }
}
