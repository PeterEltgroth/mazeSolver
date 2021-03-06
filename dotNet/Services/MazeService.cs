using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Net.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using dotNet.Models;
using Microsoft.Extensions.Logging;

namespace dotNet.Services {
    public class MazeService : IMazeService {
        private static Regex REGEX_INVALIDS = new Regex (@"![#AB\.\r\n]*");
        private static Regex REGEX_START = new Regex (@"[A]");
        private static Regex REGEX_END = new Regex (@"[B]");
        private static Regex REGEX_CR_NL = new Regex (@"[\r\n]");
        private static string CR_NL = "\r\n";
        private static char START = 'A';
        private static char END = 'B';
        private static char BLOCKED = '#';
        private static char PATH = '@';
        private enum DIRECTION { UP, LEFT, DOWN, RIGHT };

        private readonly ILogger _logger;

        public MazeService (ILogger<MazeService> logger) {
            _logger = logger;
        }

        /**
        Validate only accepted characters exist in the map.
        */
        public bool validateMap (string map) {
            var strMap = map.ToString ();
            bool valid = (REGEX_INVALIDS.Matches (strMap).Count == 0) &&
                (REGEX_START.Matches (strMap).Count == 1) &&
                (REGEX_END.Matches (strMap).Count == 1) &&
                (REGEX_CR_NL.Matches (strMap).Count > 0);

            _logger.LogInformation ("Submitted map appears valid.");

            return valid;
        }

        public Solution solve (string strMap) {
            char[][] map = split (strMap);
            long maxNodes = map.Length * map[0].Length;
            Node start, end;

            List<Node> openNodes = new List<Node> ();
            List<Node> closedNodes = new List<Node> ();

            start = getFirstNodeOfType (map, START);
            end = getFirstNodeOfType (map, END);
            _logger.LogInformation ($"Start node: {start.ToString()} :{Environment.NewLine}End node: {end.ToString()}");

            Node current = start;
            openNodes.Add (start);

            while (!current.Equals (end) &&
                    openNodes.Count > 0 &&
                    ((closedNodes.Count + openNodes.Count) < maxNodes)
                ) {

                current = getLowestScoreNode (openNodes, current);
                closedNodes.Add (current);
                openNodes.Remove (current);

                List<Node> adjacentNodes = getAdjacentNodes (end, current, map);
                foreach (Node node in adjacentNodes) {
                    if (node != null && !closedNodes.Contains (node)) {
                        addToOpenNodes(openNodes, node);
                    }
                }
            }

            if (current.Equals (end)) {
                closedNodes.Add (current);
                openNodes.Remove (current);
            }

            Solution solution = getSolution (closedNodes, start, end, map);

            return solution;
        }

        private Node getLowestScoreNode (List<Node> nodes, Node current) {
            nodes.Sort ((a, b) => a.f.CompareTo (b.f));
            Node lowest = nodes.First ();
            // Tie break lowest `f` scores be getting one where `current` is the `parent`
            var tiedNodes = nodes.FindAll (a => a.f == nodes.First ().f);
            if (tiedNodes.Count > 1) {
                _logger.LogDebug ($"{tiedNodes.Count} tied 'f' scores, choose the node who's parent is the current node");
                Node found = tiedNodes.Find (a => a.parent == current);
                if (found != null) {
                    _logger.LogDebug ($"FOUND node:   {found.ToString()}  (where parent == current)");
                    return found;
                }
                _logger.LogDebug ($"No tied nodes had current as parent, so fall through.");
            }

            _logger.LogDebug ($"LOWEST node:  {lowest.ToString()}");
            return lowest;
        }

        private List<Node> getAdjacentNodes (Node end, Node current, char[][] map) {
            var nodes = new List<Node> ();
            nodes.Add (getNode (map, end, current, DIRECTION.UP));
            nodes.Add (getNode (map, end, current, DIRECTION.LEFT));
            nodes.Add (getNode (map, end, current, DIRECTION.DOWN));
            nodes.Add (getNode (map, end, current, DIRECTION.RIGHT));

            return nodes;
        }

        private void addToOpenNodes (List<Node> nodes, Node node) {
            Node existing = nodes.Find (a => a.Equals (node));
            if (existing != null) {
                _logger.LogDebug ($"Found existing {existing.ToString()}");
                // If the existing `f` is higher than this new node, overwrite
                if (existing.f > node.f) {
                    _logger.LogDebug ($"UPDATE NODE from: {Environment.NewLine}{existing.ToString()}{Environment.NewLine}to: {Environment.NewLine}{node.ToString()}'");
                    existing.f = node.f;
                    existing.parent = node.parent;
                }
            } else {
                _logger.LogDebug ($"ADD NODE: {node.ToString()}");
                nodes.Add (node);
            }
        }
        private Node getNode (char[][] map, Node end, Node current, DIRECTION direction) {
            Node node = null;
            int maxX = map[0].Length;
            int maxY = map.Length;
            int nextX, nextY;

            switch (direction) {
                case DIRECTION.UP:
                    nextX = current.x;
                    nextY = current.y - 1;
                    break;
                case DIRECTION.LEFT:
                    nextX = current.x - 1;
                    nextY = current.y;
                    break;
                case DIRECTION.DOWN:
                    nextX = current.x;
                    nextY = current.y + 1;
                    break;
                case DIRECTION.RIGHT:
                    nextX = current.x + 1;
                    nextY = current.y;
                    break;
                default:
                    // Should only happen if a new value is appended to the `enum` but not the `switch`
                    throw new InvalidEnumArgumentException ("Invalid DIRECTION provided");
            }

            if (nextX >= 0 && nextX < maxX && nextY >= 0 && nextY < maxY) {
                char val;
                try {
                    val = map[nextY][nextX];
                    _logger.LogDebug ($"MOVED '{direction}' to '{nextX}, {nextY}' with value '{val}'");
                    if (current.parent != null) {
                        _logger.LogDebug ($"Parent is '{current.parent.ToIdString()}'");
                    }

                    // Don't create a node if BLOCKED, the START node, or it has the same coordinates as current's parent.
                    if (BLOCKED == val || START == val ||
                        (current.parent != null && current.parent.x == nextX && current.parent.y == nextY) 
                    ) {
                        _logger.LogDebug ($"BLOCKED or current node's parent!");
                        return node;
                    }

                    // Use Manhatten/city block distance
                    int h = Math.Abs (end.x - nextX) + Math.Abs (end.y - nextY);
                    node = new Node (val, nextX, nextY, h, current);
                    _logger.LogInformation ($"CREATED node: {node.ToString()}");
                } catch (IndexOutOfRangeException e) {
                    throw new IndexOutOfRangeException (
                        $"ERROR getting Node in the {direction} direction of current Node at position {current.ToIdString()}",
                        e
                    );
                }
            }

            return node;
        }

        private Solution getSolution (List<Node> nodes, Node start, Node end, char[][] map) {
            nodes.Reverse ();
            Node previous = null;
            int steps = 0;
    
            if (nodes.Contains(start) && nodes.Contains(end)){
                // Replace `.` with `@`
                foreach (Node node in nodes) {
                    // End node
                    if (node.Equals(end)) {
                        _logger.LogDebug ($"END node '{node.ToShortString()}'");
                        steps++;
                        previous = node;
                    } else if (node.Equals(start)) {
                        _logger.LogDebug ($"START node '{node.ToShortString()}'");
                        break;
                    } else if (node.Equals (previous.parent)) {
                        _logger.LogDebug ($"PATH node '{node.ToString()}, swithcing char to '@'");
                        map[node.y][node.x] = PATH;
                        steps++;
                        previous = node;
                    // Start node
                    }
                    _logger.LogDebug ($"IGNORE Node '{node.ToIdString()}', not start, end or on path");
                }
            }

            // Convert map back to String
            StringBuilder sb = new StringBuilder ();
            for (int i = 0; i < map.Length; i++) {
                sb.Append (new string(map[i]));
                if (i < map.Length - 1) {
                    sb.Append (Environment.NewLine);
                }
            }

            _logger.LogDebug ($"Solution: steps {steps}, map: {Environment.NewLine}{sb.ToString()}");

            return new Solution (steps, sb.ToString ());
        }

        private Node getFirstNodeOfType (char[][] map, char type) {
            // Top left is coordinate 0, 0 
            for (int y = 0; y < map.Length; y++) {
                for (int x = 0; x < map[y].Length; x++) {
                    if (map[y][x] == type) {
                        _logger.LogDebug ($"FOUND node '{x}, {y}' for type '{type}'");
                        return new Node (map[y][x], x, y);
                    };
                }
            }

            return null;
        }

        /** 
            Returns a char[y][x] array
         */
        private char[][] split (string map) {
            string[] split = map.Split (CR_NL);
            char[][] grid = split.Select (row => row.ToArray ()).ToArray ();

            return grid;
        }
    }
}