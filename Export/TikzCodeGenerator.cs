using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using FlowForge.Algorithms;
using FlowForge.Model;

namespace FlowForge.Export;

public class TikzCodeGenerator
{
    private readonly FlowGraph _flowGraph;
    private readonly Dictionary<string, Microsoft.Msagl.Core.Geometry.Point> _nodePositions;
    private readonly FordFulkersonAlgorithm _fordFulkerson;
    private readonly IReadOnlyList<FlowGraph> _graphStates;

    public TikzCodeGenerator(FlowGraph flowGraph, Dictionary<string, Microsoft.Msagl.Core.Geometry.Point> nodePositions, FordFulkersonAlgorithm fordFulkerson)
    {
        this._flowGraph = flowGraph;
        this._nodePositions = nodePositions;
        this._fordFulkerson = fordFulkerson;
        this._graphStates = _fordFulkerson.GraphStates;
    }

    private string GenerateTikzCode(bool includeAnimations, bool isStandalone)
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
        
        BuildTikzFooter(tikzBuilder, isStandalone);

        return tikzBuilder.ToString();
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
    
    private static void BuildTikzSettings(StringBuilder tikzBuilder)
    {
        // Global scaling and styles for smaller nodes and text
        tikzBuilder.AppendLine(
            "\\begin{tikzpicture}[scale=2,->,>=stealth,shorten >=1pt,auto,node distance=2cm,thick,");
        tikzBuilder.AppendLine("main node/.style={circle,draw,minimum size=5mm,inner sep=1pt,font=\\scriptsize}]");
    }
    
    private void BuildTikzNodes(StringBuilder tikzBuilder)
    {
        // Define custom colors for source and target nodes using hex codes
        tikzBuilder.AppendLine("\\definecolor{sourceColor}{HTML}{00AA00}"); // Strong green
        tikzBuilder.AppendLine("\\definecolor{targetColor}{HTML}{AA0000}"); // Strong red
        
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

                // Determine node color
                string colorStyle = "circle,draw";
                if (vertex.Id == _fordFulkerson.SourceId)
                {
                    colorStyle = "circle,draw,fill=sourceColor";
                }
                else if (vertex.Id == _fordFulkerson.TargetId)
                {
                    colorStyle = "circle,draw,fill=targetColor";
                }
                
                // Write the TikZ node with the appropriate style
                tikzBuilder.AppendLine($"\\node[main node, {colorStyle}] ({vertex.Id}) at ({formattedX},{formattedY}) {{{vertex.Id}}};");
            }
        }
    }
    
    private void BuildTikzEdges(StringBuilder tikzBuilder)
    {
        // Edges
        foreach (var edge in _flowGraph.GetEdges())
        {
            string label = $"{edge.CurrentFlow}/{edge.MaxFlow}";
            
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
    }

    private void BuildTikzEdgesForAnimation(StringBuilder tikzBuilder)
    {
        if (_graphStates == null || !_graphStates.Any())
            throw new InvalidOperationException("No graph states available for animation.");

        for (int frame = 0; frame < _graphStates.Count; frame++)
        {
            var stateGraph = _graphStates[frame];
        
            // Begin the frame visibility block
            tikzBuilder.AppendLine($"\\visible<{frame + 1}>{{");

            foreach (var edge in stateGraph.GetEdges())
            {
                string label = $"{edge.CurrentFlow}/{edge.MaxFlow}";
                string color = edge.IsHighlighted ? "red" : "black"; // Choose color based on state

                if (edge.IsBackwards)
                {
                    // Backward edge styling
                    tikzBuilder.AppendLine(
                        $"\\path[every node/.style={{font=\\tiny}},bend right,dashed,{color}] ({edge.Source.Id}) edge [pos=0.5,sloped] node {{\\texttt{{{label}}}}} ({edge.Target.Id});");
                }
                else
                {
                    // Normal edge styling
                    tikzBuilder.AppendLine(
                        $"\\path[every node/.style={{font=\\tiny}},{color}] ({edge.Source.Id}) edge [pos=0.5,sloped] node {{\\texttt{{{label}}}}} ({edge.Target.Id});");
                }
            }

            // End the frame visibility block
            tikzBuilder.AppendLine("}");
        }
    }
    
    private static void BuildTikzFooter(StringBuilder tikzBuilder, bool isStandalone)
    {
        tikzBuilder.AppendLine("\\end{tikzpicture}");
        
        if (isStandalone)
        {
            tikzBuilder.AppendLine("\\end{frame} ");
            tikzBuilder.AppendLine("\\end{document}");
        }
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
            string tikzCode = GenerateTikzCode(includeAnimationChecked, standaloneChecked);
            
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
}