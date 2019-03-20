using System.Text;
using System.ComponentModel;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
namespace dotNet.Models {

    public class Node {
        public char value { get; set; }
        public int x { get; set; }
        public int y { get; set; }

        // f = g + h
        public long f;
        // distance to start
        public long g;
        //estimated distance to end
        public long h;

        public Node parent;

        public Node () { }
        public Node (char val, int x, int y) {
            this.value = val;
            this.x = x;
            this.y = y;
        }

        public Node (char val, int x, int y, int h, Node parent) : this (val, x, y) {
            if (parent == null) {
                throw new NullReferenceException($"Parent is NULL for Node {x}, {y}");
            }
            this.g = parent.g + 1;
            this.h = h;
            this.f = this.g + this.h;
            this.parent = parent;
        }

        public override bool Equals(object obj) {
            if (!(obj is Node)) return false;

            Node n = (Node)obj;
            return this.x == n.x & this.y == n.y;
        }

        public override int GetHashCode(){
            return Tuple.Create(x, y).GetHashCode();
        }

        public string ToIdString(){
            return $"coordinates ({x}, {y})";
        }

        public string ToShortString(){
            return $"coordinates ({x}, {y}), value: `{value}`, distances: ({f}, {g}, {h})";
        }

        public override string ToString(){
            StringBuilder sb = new StringBuilder();
            sb.Append($"coordinates ({x}, {y}), value: `{value}`, distances: ({f}, {g}, {h})");
            if (parent != null) {
                sb.Append($", parent: ({parent.x}, {parent.y})");
            }

            return sb.ToString();
        }

    }
}