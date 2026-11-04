using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using FlowForge.Algorithms;
using FlowForge.Model;
using MsaglPoint = Microsoft.Msagl.Core.Geometry.Point;

namespace FlowForge.Export;

/// <summary>Generates TikZ (LaTeX) code for the flow graph, either as the final result or as a step-by-step Beamer animation.</summary>
public class TikzCodeGenerator
{
    private readonly FlowGraph _flowGraph;
    private readonly IReadOnlyDictionary<string, MsaglPoint> _nodePositions;
    private readonly FordFulkersonAlgorithm _fordFulkerson;

    /// <param name="flowGraph">The graph showing the final flow.</param>
    /// <param name="nodePositions">Node positions in the units of the graph viewer; nodes without a position are placed in a row.</param>
    /// <param name="fordFulkerson">The algorithm that produced the flow; provides source, sink and the recorded steps.</param>
    public TikzCodeGenerator(FlowGraph flowGraph, IReadOnlyDictionary<string, MsaglPoint> nodePositions, FordFulkersonAlgorithm fordFulkerson)
    {
        _flowGraph = flowGraph;
        _nodePositions = nodePositions;
        _fordFulkerson = fordFulkerson;
    }

    /// <param name="includeAnimations">Emit one overlay (Beamer <c>\visible</c> block) per recorded algorithm step instead of only the final flow.</param>
    /// <param name="isStandalone">Wrap the picture in a complete Beamer document instead of emitting only the <c>tikzpicture</c>.</param>
    /// <exception cref="InvalidOperationException">An animation was requested but the algorithm has not been run.</exception>
    public string GenerateTikzCode(bool includeAnimations, bool isStandalone)
    {
        var tikzBuilder = new StringBuilder();

        if (isStandalone)
        {
            BuildTikzHeader(tikzBuilder, includeAnimations);
        }

        BuildTikzSettings(tikzBuilder);
        BuildTikzNodes(tikzBuilder);

        if (includeAnimations)
        {
            BuildTikzEdgesForAnimation(tikzBuilder);
        }
        else
        {
            BuildTikzEdges(tikzBuilder);
        }

        BuildTikzFooter(tikzBuilder, isStandalone);

        return tikzBuilder.ToString();
    }

    private static void BuildTikzHeader(StringBuilder tikzBuilder, bool includeAnimations)
    {
        tikzBuilder.AppendLine("\\documentclass{beamer}");
        tikzBuilder.AppendLine("\\usepackage{tikz}");
        tikzBuilder.AppendLine("\\usetikzlibrary{arrows.meta,positioning}");
        tikzBuilder.AppendLine("\\usepackage{amsmath}");
        tikzBuilder.AppendLine("\\begin{document}");
        tikzBuilder.AppendLine(includeAnimations ? "\\begin{frame}{Graph Animation}" : "\\begin{frame}{Flow Network}");
    }

    private static void BuildTikzSettings(StringBuilder tikzBuilder)
    {
        // Global scaling and styles for smaller nodes and text
        tikzBuilder.AppendLine(
            "\\begin{tikzpicture}[scale=2,->,>=stealth,shorten >=1pt,auto,node distance=2cm,thick,");
        tikzBuilder.AppendLine("main node/.style={circle,draw,minimum size=5mm,inner sep=1pt,font=\\scriptsize}]");
    }

    private void BuildTikzNodes(StringBuilder tikzBuilder)
    {
        tikzBuilder.AppendLine("\\definecolor{sourceColor}{HTML}{00AA00}");
        tikzBuilder.AppendLine("\\definecolor{targetColor}{HTML}{AA0000}");

        int index = 0;
        foreach (var vertex in _flowGraph.Graph.Vertices)
        {
            // Viewer coordinates are scaled down to centimetres; both viewer and TikZ have the y axis pointing up.
            double x = index;
            double y = 0;
            if (_nodePositions.TryGetValue(vertex.Id, out var position))
            {
                x = position.X / 100.0;
                y = position.Y / 100.0;
            }

            index++;

            string style = "circle,draw";
            if (vertex.Id == _fordFulkerson.SourceId)
            {
                style = "circle,draw,fill=sourceColor";
            }
            else if (vertex.Id == _fordFulkerson.TargetId)
            {
                style = "circle,draw,fill=targetColor";
            }

            tikzBuilder.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"\\node[main node, {style}] ({vertex.Id}) at ({x:F2},{y:F2}) {{{vertex.Id}}};"));
        }
    }

    private void BuildTikzEdges(StringBuilder tikzBuilder)
    {
        foreach (var edge in _flowGraph.GetEdges())
        {
            AppendEdge(tikzBuilder, _flowGraph, edge, edge.IsBackwards ? "red" : null);
        }
    }

    private void BuildTikzEdgesForAnimation(StringBuilder tikzBuilder)
    {
        var states = _fordFulkerson.GraphStates;
        if (states.Count == 0)
        {
            throw new InvalidOperationException("No graph states available for animation. Run an algorithm first.");
        }

        for (int frame = 0; frame < states.Count; frame++)
        {
            var stateGraph = states[frame];

            tikzBuilder.AppendLine($"\\visible<{frame + 1}>{{");

            foreach (var edge in stateGraph.GetEdges())
            {
                AppendEdge(tikzBuilder, stateGraph, edge, edge.IsHighlighted ? "red" : "black");
            }

            tikzBuilder.AppendLine("}");
        }
    }

    private static void AppendEdge(StringBuilder tikzBuilder, FlowGraph graph, FlowEdge edge, string? color)
    {
        var options = new List<string> { "every node/.style={font=\\tiny}" };

        if (edge.IsBackwards)
        {
            options.Add("bend right");
            options.Add("dashed");
        }
        else if (graph.Graph.TryGetEdge(edge.Target, edge.Source, out var opposite) && !opposite.IsBackwards && !ReferenceEquals(opposite, edge))
        {
            // Input edges in both directions would be drawn on top of each other without a bend.
            options.Add("bend right");
        }

        if (color != null)
        {
            options.Add(color);
        }

        tikzBuilder.AppendLine(
            $"\\path[{string.Join(",", options)}] ({edge.Source.Id}) edge [pos=0.5,sloped] node {{\\texttt{{{edge.FlowLabel}}}}} ({edge.Target.Id});");
    }

    private static void BuildTikzFooter(StringBuilder tikzBuilder, bool isStandalone)
    {
        tikzBuilder.AppendLine("\\end{tikzpicture}");

        if (isStandalone)
        {
            tikzBuilder.AppendLine("\\end{frame}");
            tikzBuilder.AppendLine("\\end{document}");
        }
    }
}
