using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace FlowForge;

public class TikzCodeGenerator
{
    private readonly FlowGraph _flowGraph;
    private readonly Dictionary<string, Microsoft.Msagl.Core.Geometry.Point> _nodePositions;
    private IReadOnlyList<FlowGraph> _graphStates;

    public TikzCodeGenerator(FlowGraph flowGraph, Dictionary<string, Microsoft.Msagl.Core.Geometry.Point> nodePositions, IReadOnlyList<FlowGraph> graphStates)
    {
        this._flowGraph = flowGraph;
        this._nodePositions = nodePositions;
        this._graphStates = graphStates;
    }

    private string GenerateTikzCode_Curved(bool includeAnimations, bool isStandalone)
    {
        if (_flowGraph == null)
        {
            throw new InvalidOperationException("Flow graph is not initialized.");
        }

        var tikzBuilder = new System.Text.StringBuilder();

        if (isStandalone)
        {
            BuildTikzHeader(tikzBuilder);
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

        if (isStandalone)
        {
            BuildTikzFooter(tikzBuilder);
        }

        return tikzBuilder.ToString();
    }

    private static void BuildTikzFooter(StringBuilder tikzBuilder)
    {
        tikzBuilder.AppendLine("\\end{tikzpicture}");
        tikzBuilder.AppendLine("\\end{frame} ");
        tikzBuilder.AppendLine("\\end{document}");
    }

    private void BuildTikzEdges(StringBuilder tikzBuilder)
    {
        // Edges
        foreach (var edge in _flowGraph.GetEdges())
        {
            if (edge.IsBackwards)
            {
                string label = $"0/{edge.Residual}";
                // Reverse edge styling (curved, dashed, different color)
                tikzBuilder.AppendLine(
                    $"\\path[every node/.style={{font=\\tiny}},bend right,dashed,red] ({edge.Source.Id}) edge [pos=0.5,sloped] node {{\\texttt{{{label}}}}} ({edge.Target.Id});");
            }
            else
            {
                string label = $"{edge.MaxFlow - edge.Residual}/{edge.MaxFlow}";
                // Normal edge styling
                tikzBuilder.AppendLine(
                    $"\\path[every node/.style={{font=\\tiny}}] ({edge.Source.Id}) edge [pos=0.5,sloped] node {{\\texttt{{{label}}}}} ({edge.Target.Id});");
            }
        }
    }

    private void BuildTikzEdgesForAnimation(StringBuilder tikzBuilder)
    {
        if (_graphStates == null || !_graphStates.Any())
            throw new InvalidOperationException("No graph states available for animation.");

        for (int frame = 0; frame < _graphStates.Count; frame++)
        {
            var stateGraph = _graphStates[frame];
        
            // Begin the frame visibility block
            tikzBuilder.AppendLine($"\\only<{frame + 1}>{{");

            foreach (var edge in stateGraph.GetEdges())
            {
                string label = $"{edge.Residual}/{edge.MaxFlow}";

                if (edge.IsBackwards)
                {
                    // Reverse edge styling (curved, dashed, different color)
                    tikzBuilder.AppendLine(
                        $"\\path[every node/.style={{font=\\tiny}},bend right,dashed,red] ({edge.Source.Id}) edge [pos=0.5,sloped] node {{\\texttt{{{label}}}}} ({edge.Target.Id});");
                }
                else
                {
                    // Normal edge styling
                    tikzBuilder.AppendLine(
                        $"\\path[every node/.style={{font=\\tiny}}] ({edge.Source.Id}) edge [pos=0.5,sloped] node {{\\texttt{{{label}}}}} ({edge.Target.Id});");
                }
            }

            // End the frame visibility block
            tikzBuilder.AppendLine("}");
        }
    }

    private void BuildTikzNodes(StringBuilder tikzBuilder)
    {
        // Nodes
        foreach (var vertex in _flowGraph.Graph.Vertices)
        {
            if (_nodePositions.ContainsKey(vertex.Id))
            {
                var position = _nodePositions[vertex.Id];
                // Format x and y as floating-point values with two decimal places
                string formattedX =
                    (position.X / 100.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                string formattedY =
                    (position.Y / 100.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

                tikzBuilder.AppendLine(
                    $"\\node[main node] ({vertex.Id}) at ({formattedX},{formattedY}) {{{vertex.Id}}};");
            }
        }
    }

    private static void BuildTikzSettings(StringBuilder tikzBuilder)
    {
        // Global scaling and styles for smaller nodes and text
        tikzBuilder.AppendLine(
            "\\begin{tikzpicture}[scale=3,->,>=stealth,shorten >=1pt,auto,node distance=2cm,thick,");
        tikzBuilder.AppendLine("main node/.style={circle,draw,minimum size=5mm,inner sep=1pt,font=\\scriptsize}]");
    }

    private static void BuildTikzHeader(StringBuilder tikzBuilder)
    {
        tikzBuilder.AppendLine("\\documentclass[tikz,border=3mm]{beamer}");
        
        tikzBuilder.AppendLine("\\usepackage{tikz}");
        tikzBuilder.AppendLine("\\usetikzlibrary{arrows.meta,positioning}");
        
        tikzBuilder.AppendLine("\\usepackage{amsmath}");
        
        tikzBuilder.AppendLine("\\begin{document}");
        tikzBuilder.AppendLine("\\begin{frame}{Graph Animation}");
    }

    public void ExportGraphToLatex(bool? includeAnimation, bool? standalone)
    {
        bool includeAnimationChecked = includeAnimation ?? false;
        bool standaloneChecked = standalone ?? true;

        var saveFileDialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "TeX files (*.tex)|*.tex",
            Title = "Export Graph to LaTeX"
        };

        if (saveFileDialog.ShowDialog() != true) return;

        string filePath = saveFileDialog.FileName;

        try
        {
            string tikzCode = GenerateTikzCode_Curved(includeAnimationChecked, standaloneChecked);
            
            System.IO.File.WriteAllText(filePath, tikzCode);

            // Dynamic message based on options
            string message = "Graph successfully exported to LaTeX file:\n" + filePath + "\n\nOptions:\n";
            message += includeAnimationChecked ? "- Includes animation\n" : "- No animation\n";
            message += standaloneChecked ? "- Standalone document\n" : "- TikZ code only (no standalone)";

            MessageBox.Show(message, "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting graph: {ex.Message}", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string GenerateTikzCode_WithAnimation()
    {
        throw new NotImplementedException();
    }

    private string StripStandaloneWrapper(string tikzCode)
    {
        var lines = tikzCode.Split('\n').ToList();
        lines.RemoveAll(line => line.StartsWith("\\documentclass") || line.StartsWith("\\usepackage") || line.StartsWith("\\begin{document}") || line.StartsWith("\\end{document}"));
        return string.Join("\n", lines);
    }
    
    private string GenerateTikzCode()
    {
        if (_flowGraph == null)
            throw new InvalidOperationException("Flow graph is not initialized.");

        var tikzBuilder = new System.Text.StringBuilder();
        tikzBuilder.AppendLine("\\documentclass[tikz,border=3mm]{standalone}");
        tikzBuilder.AppendLine("\\usetikzlibrary{arrows.meta,positioning}");
        tikzBuilder.AppendLine("\\usepackage{amsmath}");
        tikzBuilder.AppendLine("\\begin{document}");

        // Global scaling and styles for smaller nodes and text
        tikzBuilder.AppendLine(
            "\\begin{tikzpicture}[scale=3,->,>=stealth,shorten >=1pt,auto,node distance=2cm,thick,");
        tikzBuilder.AppendLine("main node/.style={circle,draw,minimum size=1mm,inner sep=1pt,font=\\scriptsize}]");

        // Nodes
        foreach (var vertex in _flowGraph.Graph.Vertices)
        {
            if (_nodePositions.ContainsKey(vertex.Id))
            {
                var position = _nodePositions[vertex.Id];
                // Format x and y as floating-point values with two decimal places
                string formattedX =
                    (position.X / 100.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                string formattedY =
                    (position.Y / 100.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

                tikzBuilder.AppendLine(
                    $"\\node[main node] ({vertex.Id}) at ({formattedX},{formattedY}) {{{vertex.Id}}};");
            }
        }

        // Edges
        foreach (var edge in _flowGraph.GetEdges())
        {
            string label = $"{edge.Residual}/{edge.MaxFlow}";

            if (edge.IsBackwards)
            {
                // Reverse edge styling: offset slightly above forward edges
                tikzBuilder.AppendLine(
                    $"\\path[every node/.style={{font=\\tiny}},dashed,red] ({edge.Source.Id}) edge [pos=0.5,above] node {{\\texttt{{{label}}}}} ({edge.Target.Id});");
            }
            else
            {
                // Normal edge styling
                tikzBuilder.AppendLine(
                    $"\\path[every node/.style={{font=\\tiny}}] ({edge.Source.Id}) edge [pos=0.5,below] node {{\\texttt{{{label}}}}} ({edge.Target.Id});");
            }
        }

        tikzBuilder.AppendLine("\\end{tikzpicture}");
        tikzBuilder.AppendLine("\\end{document}");

        return tikzBuilder.ToString();
    }
}