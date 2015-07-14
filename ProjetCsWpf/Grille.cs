using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace ProjetCsWpf
{

    public class Grille
    {
        public DateTime CreationDate { get; private set; }
        public string Name { get; private set; }
        public Case[][] Content { get; set; }
        public char[] PossibleValues { get; set; }
        public int Taille { get { return PossibleValues.Count(); } }
        public int AreaLineCount { get { return (int) Math.Sqrt(Content.Length); } }
        private IEnumerable<Case> cells {
            get {
                return from line in Content
                       from cell in line
                       select cell;
            }
        }

        private IEnumerable<CellGroup> Lines {
            get {
                return from cell in cells
                       group cell by cell.Y into line
                       select new CellGroup(line);
            }
        }
        private IEnumerable<CellGroup> Columns {
            get
            {
                return from cell in cells
                       group cell by cell.X
                       into column
                       select new CellGroup(column);
                /*for (var i = 0; i < Content[0].Length; ++i)
                    yield return new CellGroup(Content.Select(line => line.ElementAt(i)).ToArray());*/
            }
        }
        private IEnumerable<CellGroup> Areas {
            get {
                return from cell in cells
                       group cell by new {
                           x = cell.X / AreaLineCount, 
                           y = cell.Y / AreaLineCount,
                       } into area
                       select new CellGroup(area);
            }
        }
        private IEnumerable<IEnumerable<CellGroup>> AreaLines
        {
            get
            {
                return from area in Areas
                       group area by area.First().Y / AreaLineCount into areaLine
                       select areaLine;
            }
        }

        private IEnumerable<IEnumerable<CellGroup>> AreaColumns
        {
            get
            {
                return from area in Areas
                       group area by area.First().X / AreaLineCount into areaColumn
                       select areaColumn;
            }
        }

        private IEnumerable<CellGroup> AllGroups {
            get {
                return Lines.Concat(Columns).Concat(Areas);
            }
        }
        public bool Resolved {
            get {
                return cells.All((c) => c.Resolved);
            }
        }
        public bool IsValid {
            get {
                return AllGroups.All((group) => group.IsValid);
            }
        }
        public IEnumerable<Case> Resolveds()
        {
            return (from cell in cells
                    where cell.Resolved
                    select cell).ToArray();
        }

        public Grille(string name, DateTime creationTime,char[] possibleValues,  char[][] grille) {
            if (grille.Length == 0)
                throw new InvalidGrilleException("The grid is empty");
            if (grille.Length != grille[0].Length)
                throw new InvalidGrilleException("All lines are not of the same size");
            if (grille.Any(line => line.Count() != grille[0].Length))
                throw new InvalidGrilleException("The grid is not a square");
            if ((int)Math.Sqrt(grille.Length) % 1 != 0)
                throw new InvalidGrilleException("Cannot form valid areas with this size");
            if (!grille.All(line => line.All(c => c == '.' || possibleValues.Contains(c))))
                throw new InvalidGrilleException("All values are not in possible values");
            
            PossibleValues = possibleValues;

            this.Content = grille.Select((line,y) => line.Select((value,x) => value == '.' ? new Case(this,x,y,possibleValues.ToSet()) :new Case(this,x,y,new HashSet<char>() {value})).ToArray()).ToArray();
            this.Name = name;
            this.CreationDate = creationTime;
        }

        public void Resolve() {
            while (!Resolved) {
                PropagerCertitudes();
                RetirerCandidatsUniques();
            }
        }
        
        // pour les valeures déja trouvées, on retires les valeures dans les lignes colones region corespondantes
        public void PropagerCertitudes() {
            //on retire les lignes qu'il manque
            foreach (var cell in Resolveds()) 
                foreach (var element in ConcernedCells(cell.X, cell.Y))
                    element.Remove(cell.Value);
        }

        // si une region n'a qu'une hypothese possible, on la retire
        public void RetirerCandidatsUniques() {
            foreach (var groupe in AllGroups) {
                foreach (var p in PossibleValues) {
                    var cellulePossible = (from cell in groupe
                                          where cell.Hypotheses.Contains(p) 
                                          select cell).ToArray();

                    if (cellulePossible.Count() == 1)
                        cellulePossible.First().Found(p);
                }
            }
        }

        public void UnSeulCandidat()
        {
            foreach (var groupe in AllGroups)
            {
                //only one cell possible for a value
                foreach (var value in PossibleValues)
                {
                    var canBe = from cell in groupe
                                where cell.Hypotheses.Contains(value)
                                select cell;
                    canBe = canBe.ToArray();
                    if (canBe.Count() == 1)
                    {
                        canBe.First().Value = value;
                    }
                }
            }
        }


        private IEnumerable<CellGroup> AreaAtRow(int i)
        {
            return  from area in Areas
                    where area.First().Y / AreaLineCount == i
                    select area;
        }
        private IEnumerable<CellGroup> AreaAtColumn(int i)
        {
            return from area in Areas
                   where area.First().X / AreaLineCount == i
                   select area;
        }

        public void TrouverJumeaux() {
                //if a value is only in a line or column
            foreach (var value in PossibleValues)
            {
                Debug.WriteLine("value : {0}", value);
                foreach (var area in Areas)
                {
                    Debug.WriteLine("area {0}",area);
                    var lineWithValue = (
                        from line in area.Lines
                        where line.Any(c => c.Hypotheses.Contains(value))
                        select line
                    ).ToArray();
                    var columnWithValue = (from column in area.Columns
                                            where column.Any(c => c.Hypotheses.Contains(value))
                                            select column).ToArray();
                    if (lineWithValue.Count() == 1 && columnWithValue.Count() == 1) //already resolved
                        continue;
                    if (lineWithValue.Count() == 1)
                    {
                        Debug.WriteLine("line has jumeaux : " + lineWithValue.First().GetString());
                        Debug.WriteLine("Found line {0}",lineWithValue.First().First().Y);
                        foreach (var cell in Lines
                                                .ElementAt(lineWithValue
                                                                .First()
                                                                .First()
                                                                .Y)
                                                .Where(c => !area.Contains(c)))
                        {
                            cell.Remove(value);
                        }
                    }
                    if (columnWithValue.Count() == 1)
                    {
                        Debug.WriteLine("column has jumeaux : " + columnWithValue.First().GetString());
                        Debug.WriteLine("Found colulms {0}", columnWithValue.First().First().X);
                        foreach (var cell in Columns
                            .ElementAt(columnWithValue
                                            .First()
                                            .First()
                                            .X)
                            .Where(c => !area.Contains(c)))
                        {
                            cell.Remove(value);
                        }
                    }
                }
            }
            Debug.WriteLine("end of function");
        }

        public IEnumerable<Case> ConcernedCells(int x, int y)
        {
            var areaLength = (int)Math.Sqrt((double)Content.Length);
            return (from cell in cells
                   where !cell.Resolved
                   && (   cell.X == x
                       || cell.Y == y
                       || (
                               cell.X / areaLength == x / areaLength
                            && cell.Y / areaLength == y / areaLength
                        )
                    )
                   select cell).ToArray();
        }

        public override string ToString()
        {
            return string.Format(@"{0}
{1}
{2}
{3}
",
                 Name,
                 CreationDate,
                 PossibleValues.Aggregate("", (s, b) => s + b + ","),
                 Content.Aggregate("", (s, b) => s + b.Aggregate("", (s1, b1) => s1 + b1 + " ") + Environment.NewLine)
            );
        }
    
        internal void PropagerCertitudes(char value,int x,int y)
        {
            foreach (var element in ConcernedCells(x, y))
                element.Remove(value);
        }

        public void TrouverInteraction()
        {
            foreach (var value in PossibleValues)
            {
                Debug.WriteLine("value : " + value);
                foreach (var areaRow in AreaLines)
                {
                    for (var i = 0; i < 3; ++i)
                    {
                        var areaTarget = areaRow.Where(area => area.Lines.ElementAt(i).All(c => !c.Hypotheses.Contains(value)));
                        if (areaTarget.Count() == AreaLineCount - 1)
                        {
                            var toEmpty = areaRow.Except(areaTarget).First();
                            Debug.WriteLine("lines : ");
                            foreach (var l in toEmpty.Lines)
                            {
                                Debug.WriteLine(l.GetString());
                            }
                            Debug.WriteLine("to empty(row) : " + toEmpty);
                            Debug.WriteLine("line : " + i);
                            Debug.WriteLine("except(row) : " + toEmpty.Lines.ElementAt(i).GetString());
                            foreach (var cell in toEmpty.Except(toEmpty.Lines.ElementAt(i)))
                            {
                                cell.Remove(value);
                            }
                        }
                    }
                }

                foreach (var areaColumn in AreaColumns)
                {
                    for (var i = 0; i < 3; ++i)
                    {
                        var areaTarget = areaColumn.Where(area => area.Columns.ElementAt(i).All(c => !c.Hypotheses.Contains(value)));
                        if (areaTarget.Count() == AreaLineCount - 1)
                        {

                            var toEmpty = areaColumn.Except(areaTarget).First();

                            Debug.WriteLine("to empty(column) : " + toEmpty);
                            Debug.WriteLine("except(column) : " + toEmpty.Columns.ElementAt(i).GetString());
                            Debug.WriteLine("column : " + i);

                            foreach (var cell in toEmpty.Except(toEmpty.Columns.ElementAt(i)))
                            {
                                cell.Remove(value);
                            }
                        }
                    }
                }
            }
        }

        public void GroupeIsoles()
        {
            
        }
    }
    public class InvalidGrilleException : Exception {
        public InvalidGrilleException(string message)
            : base(message) { }
    }
}
