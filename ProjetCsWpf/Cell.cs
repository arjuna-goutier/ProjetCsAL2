using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                if(Resolved)
                    return;
                //Debug.WriteLine("({0},{1}) = {2}", X, Y, Value);
                var hypotheses = Hypotheses;
                Hypotheses = new ObservableCollection<char>() { value };
                Grille.PropagerCertitudes(value, X, Y);
            }
        }
        public ObservableCollection<char> Hypotheses { get; set; }
        public bool Resolved {get { return Hypotheses.Count() == 1; } }
        public Case(Grille grille, int x, int y, ObservableCollection<char> hypotheses) {
            X = x;
            Y = y;
            Grille = grille;
            Hypotheses = hypotheses;
        }

        public Case(Case cell,Grille grille)
        {
            X = cell.X;
            Y = cell.Y;
            Hypotheses = new ObservableCollection<char>(cell.Hypotheses);
            Grille = grille;
        }

        public void Remove(char c) {
            //Debug.WriteLine("({0},{1}) : ~{2}", X, Y, c);
            if (Resolved)
            {
                //Debug.WriteLine("known...");
                return;
            }
            Hypotheses.Remove(c);
            //si retirer ça nous permet de résoudre, on le dit a la grille
            if (Resolved)
            {
                //Debug.WriteLine("({0},{1}) -> {2}",X,Y,Value);
                Grille.PropagerCertitudes(Value, X, Y);                
            }
        }

        public override string ToString()
        {
            return string.Format("({0},{1}) : {2}", X, Y,
                Hypotheses.Select(h => h.ToString()).Aggregate((s, v) => s + ", " + v));
//            return Resolved ? Value.ToString() : Hypotheses.Aggregate("", (s,v) => s + v);
        }
        public Grille Grille { get; set; }

        /*internal void Found(char p) {
            if (Resolved) return;
            Hypotheses = new HashSet<char>() { p };
            Grille.PropagerCertitudes(Value, X, Y);
        }*/
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
