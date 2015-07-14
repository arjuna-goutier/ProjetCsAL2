using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ProjetCsWpf
{
    public class Case
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public char Value { 
            get {
                if (Resolved == false)
                    return default(char);
                return Hypotheses.First();
            }
            set
            {
                Debug.WriteLine("({0},{1}) = {2}", X, Y, Value);
                var hypotheses = Hypotheses;
                Hypotheses = new HashSet<char>() { value };
                Grille.PropagerCertitudes(value, X, Y);
            }
        }
        public ISet<char> Hypotheses { get; set; }
        public bool Resolved {get { return Hypotheses.Count() == 1; } }
        public Case(Grille grille, int x, int y, ISet<char> hypotheses) {
            X = x;
            Y = y;
            Grille = grille;
            Hypotheses = hypotheses;
        }
        public void Remove(char c) {
            if (Resolved)
                return;
            Debug.WriteLine("({0},{1}) : ~{2}", X,Y,c);
            Hypotheses.Remove(c);
            //si retirer ça nous permet de résoudre, on le dit a la grille
            if (Resolved)
            {
                Debug.WriteLine("({0},{1}) -> {2}",X,Y,Value);
                Grille.PropagerCertitudes(Value, X, Y);                
            }
        }

        public override string ToString() {
            return Resolved ? Value.ToString() : Hypotheses.Aggregate("", (s,v) => s + v);
        }
        public Grille Grille { get; set; }

        internal void Found(char p) {
            if (Resolved) return;
            Hypotheses = new HashSet<char>() { p };
            Grille.PropagerCertitudes(Value, X, Y);
        }
    }

    class CellValueComparator : IComparer<Case>, IEqualityComparer<Case>{
        public int Compare(Case c1, Case c2) {
            return c1.Value.CompareTo(c2.Value);
        }

        public bool Equals(Case c1, Case c2) {
            return c1.Value == c2.Value;
        }

        public int GetHashCode(Case c) {
            return c.Value.GetHashCode();
        }
    }
}
