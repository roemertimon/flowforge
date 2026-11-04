using System.Globalization;
using QuikGraph;

namespace FlowForge.Model;

/// <summary>A directed edge with a capacity and the flow currently routed over it.</summary>
public class FlowEdge : Edge<FlowNode>
{
    /// <summary>The capacity of the edge. For residual (backwards) edges this is the flow that can be cancelled.</summary>
    public double MaxFlow { get; set; }
    
    public double CurrentFlow { get; set; }
    
    public double Residual => MaxFlow - CurrentFlow;

    /// <summary>True for edges that only exist in the residual network and are not part of the input graph.</summary>
    public bool IsBackwards { get; set; }
    
    public bool IsHighlighted { get; set; }

    /// <summary>The flow in the form <c>current/capacity</c>, independent of the user's culture.</summary>
    public string FlowLabel => string.Create(CultureInfo.InvariantCulture, $"{CurrentFlow}/{MaxFlow}");

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
