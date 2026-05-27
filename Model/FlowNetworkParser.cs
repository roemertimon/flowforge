using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace FlowForge.Model;

/// <summary>
/// Reads flow networks from the text format used by the application.
/// The first line holds the number of vertices n (at least 2); the vertices are numbered 1 to n, vertex 1 is the source
/// and vertex n is the sink. Every further line describes an edge as <c>from,to,capacity</c> or <c>from:to:capacity</c>
/// with whole numbers. Blank lines are ignored.
/// </summary>
public static class FlowNetworkParser
{
    public static FlowGraph ParseFile(string filePath) => Parse(File.ReadAllLines(filePath));

    /// <exception cref="FormatException">The text does not follow the format described above.</exception>
    public static FlowGraph Parse(IEnumerable<string> lines)
    {
        FlowGraph? graph = null;
        int vertexCount = 0;
        int edgeCount = 0;
        int lineNumber = 0;

        foreach (var rawLine in lines)
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (line.Length == 0) continue;

            if (graph == null)
            {
                if (!int.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out vertexCount) || vertexCount <= 1)
                {
                    throw new FormatException("The first line must specify an integer greater than 1 for the number of vertices.");
                }

                graph = new FlowGraph();
                for (int i = 1; i <= vertexCount; i++)
                {
                    graph.AddVertex(i.ToString(CultureInfo.InvariantCulture));
                }

                continue;
            }

            var parts = line.Split(new[] { ',', ':' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length != 3 ||
                !TryParseInteger(parts[0], out int from) ||
                !TryParseInteger(parts[1], out int to) ||
                !TryParseInteger(parts[2], out int capacity))
            {
                throw new FormatException($"Invalid edge format on line {lineNumber}: {line}");
            }

            if (from < 1 || from > vertexCount || to < 1 || to > vertexCount)
            {
                throw new FormatException($"Edge references invalid vertices on line {lineNumber}: {line}");
            }

            if (capacity < 0)
            {
                throw new FormatException($"The capacity must not be negative on line {lineNumber}: {line}");
            }

            graph.AddEdge(from.ToString(CultureInfo.InvariantCulture), to.ToString(CultureInfo.InvariantCulture), capacity);
            edgeCount++;
        }

        if (graph == null || edgeCount == 0)
        {
            throw new FormatException("The file must contain the number of vertices and at least one edge.");
        }

        return graph;
    }

    private static bool TryParseInteger(string text, out int value) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
}
