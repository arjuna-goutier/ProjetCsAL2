using System;
using System.Collections.Generic;
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

        private IEnumerable<CellGroup> lines {
            get {
                return from cell in cells
                       group cell by cell.X into line
                       select new CellGroup(line);
            }
        }
        private IEnumerable<CellGroup> columns {
            get
            {
                return from cell in cells
                       group cell by cell.Y
                       into column
                       select new CellGroup(column);
                /*for (var i = 0; i < Content[0].Length; ++i)
                    yield return new CellGroup(Content.Select(line => line.ElementAt(i)).ToArray());*/
            }
        }
        private IEnumerable<CellGroup> areas {
            get {
                var areaLength = (int) Math.Sqrt(Content.Length);
                var regions= new Matrix<List<Case>>(areaLength,areaLength,() => new List<Case>(Content.Length));
                for(var y = 0 ; y < Content.Count() ; ++y)
	                for(var x = 0 ; x < Content[y].Count() ; ++x)
                        regions.Values[x / areaLength, y / areaLength].Add(Content[y][x]);
                return regions.Select(cells => new CellGroup(cells));
            }
        }
        private IEnumerable<CellGroup> allGroups {
            get {
                return lines.Concat(columns).Concat(areas);
            }
        }
        public bool Resolved {
            get {
                return cells.All((c) => c.Resolved);
            }
        }
        public bool IsValid {
            get {
                return allGroups.All((group) => group.IsValid);
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
            foreach (var groupe in allGroups) {
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
            foreach (var groupe in allGroups)
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

        private IEnumerable<IEnumerable<CellGroup>> AreaRows
        {
            get
            {
                return from area in areas
                       group area by area.First().X / AreaLineCount into areaLine
                       select areaLine;
            }
        }

        private IEnumerable<IEnumerable<CellGroup>> AreaColumns
        {
            get
            {
                return from area in areas
                       group area by area.First().X / AreaLineCount into areaColumn
                       select areaColumn;
            }
        }

        private IEnumerable<CellGroup> AreaAtRow(int i)
        {
            return  from area in areas
                    where area.First().X/AreaLineCount == i
                    select area;
        }
        private IEnumerable<CellGroup> AreaAtColumn(int i)
        {
            return from area in areas
                   where area.First().X / AreaLineCount == i
                   select area;
        }

        public void TrouverJumeaux() {
                //if a value is only in a line or column
            foreach (var value in PossibleValues)
            {
                Console.WriteLine("value : {0}", value);
                foreach (var area in areas)
                {
                    Console.WriteLine("area {0}",area);
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
                        Console.WriteLine("Found line {0}",lineWithValue.First().First().X);
                        foreach (var cell in lines
                                                .ElementAt(lineWithValue
                                                                .First()
                                                                .First()
                                                                .X)
                                                .Where(c => !area.Contains(c)))
                        {
                            cell.Remove(value);
                        }
                    }
                    if (columnWithValue.Count() == 1)
                    {
                        Console.WriteLine("Found colulms {0}",  columnWithValue.First().First().Y);
                        foreach (var cell in columns
                            .ElementAt(columnWithValue
                                            .First()
                                            .First()
                                            .Y)
                            .Where(c => !area.Contains(c)))
                        {
                            cell.Remove(value);
                        }
                    }
                }
            }
            Console.WriteLine("end of function");
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
                foreach (var areaRow in AreaRows)
                {
                    var h = from area in areaRow
                            where area.Lines.Count(l => ! l.Any(c => c.Hypotheses.Contains(c.Value))) == 1
                            select area;
                    
                    if (h.Count() == AreaLineCount - 1)
                    {
                    }
                }

                foreach (var areaColumn in AreaColumns)
                {

                }
            }
        }
    }
    public class InvalidGrilleException : Exception {
        public InvalidGrilleException(string message)
            : base(message) { }
    }
}
