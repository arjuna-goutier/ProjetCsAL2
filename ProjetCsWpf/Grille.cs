using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Configuration;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;

namespace ProjetCsWpf
{

    public class Grille:ViewModelBase
    {
        public DateTime CreationDate { get; private set; }
        public string Name { get; private set; }
        public Case[][] Content { get; set; }
        public char[] PossibleValues { get; set; }
        public int Taille { get { return PossibleValues.Count(); } }
        public int AreaLineCount { get { return (int) Math.Sqrt(Content.Length); } }
        private long solvingTime;
        public long SolvingTime
        {
            get
            {
                return solvingTime;
            }
            set
            {
                solvingTime = value;
                OnPropertyChanged("SolvingTime");
            }
        }

        public IEnumerable<Case> Cells {
            get {
                return from line in Content
                       from cell in line
                       select cell;
            }
        }

        public IEnumerable<CellGroup> Lines
        {
            get {
                return from cell in Cells
                       group cell by cell.Y into line
                       select new CellGroup(line);
            }
        }
        public IEnumerable<CellGroup> Columns
        {
            get
            {
                return from cell in Cells
                       group cell by cell.X
                       into column
                       select new CellGroup(column);
                /*for (var i = 0; i < Content[0].Length; ++i)
                    yield return new CellGroup(Content.Select(line => line.ElementAt(i)).ToArray());*/
            }
        }
        public IEnumerable<CellGroup> Areas
        {
            get {
                return from cell in Cells
                       group cell by new {
                           x = cell.X / AreaLineCount, 
                           y = cell.Y / AreaLineCount,
                       } into area
                       select new CellGroup(area);
            }
        }
        public IEnumerable<IEnumerable<CellGroup>> AreaLines
        {
            get
            {
                return from area in Areas
                       orderby area.First().Y
                       group area by area.First().Y / AreaLineCount into areaLine
                       select areaLine;
            }
        }

        public IEnumerable<IEnumerable<CellGroup>> AreaColumns
        {
            get
            {
                return from area in Areas
                       group area by area.First().X / AreaLineCount into areaColumn
                       select areaColumn;
            }
        }

        public IEnumerable<CellGroup> AllGroups
        {
            get {
                return Lines.Concat(Columns).Concat(Areas);
            }
        }
        public bool Resolved {
            get {
                return Cells.All((c) => c.Resolved);
            }
        }
        public bool IsValid {
            get {
                return AllGroups.All((group) => group.IsValid);
            }
        }
        public IEnumerable<Case> Resolveds
        {
            get
            {
                return (from cell in Cells
                        where cell.Resolved
                        select cell).ToArray();                
            }
        }

        public IEnumerable<Case> NotResolveds
        {
            get
            {
            return  from c in Cells
                    where !c.Resolved
                    select c;                
            }
        }


        public Grille(string name, DateTime creationTime,char[] possibleValues,  char[][] grille) {
            if (grille.Length == 0)
                throw new InvalidGrilleException("The grid is empty");
            if (grille.Length != grille[0].Length)
                throw new InvalidGrilleException("All lines are not of the same size");
            if (grille.Any(line => line.Count() != grille[0].Length))
                throw new InvalidGrilleException("The grid is not a square");
            if (Math.Sqrt(grille.Length) % 1 != 0)
                throw new InvalidGrilleException("Cannot form valid areas with this size");
            if (!grille.All(line => line.All(c => c == '.' || possibleValues.Contains(c))))
                throw new InvalidGrilleException("All values are not in possible values");
            
            PossibleValues = possibleValues;

            this.Content = grille.Select((line,y) =>
                line.Select((value,x) => 
                    value == '.' 
                        ? new Case(this,x,y,new ObservableCollection<char>(possibleValues.ToList()))
                        : new Case(this, x, y, new ObservableCollection<char>() { value })
                ).ToArray()
            ).ToArray();
            this.Name = name;
            this.CreationDate = creationTime;
        }

        private Grille(Grille other)
        {
            PossibleValues = other.PossibleValues;

            Content = other.Content
                .Select((line, y) =>
                    line.Select((value, x) => 
                        new Case(value, this)
                    ).ToArray()
                ).ToArray();

            Name = other.Name;
            CreationDate = other.CreationDate;
        }

        public void Resolve() {
            if (Resolved || !IsValid) return;
            
            PropagerCertitudes();
            for (var i = 0; i < 3 && !Resolved; ++i)
            {
                if (Resolved || !IsValid) return;
                //algo rapide, on peut les utiliser souvent
                for (var j = 0; i < 5; ++i)
                {
                    UnSeulCandidat();
                    TrouverJumeaux();
                    TrouverInteraction();
                    GroupeIsoles();
                }
                if (Resolved || !IsValid) return;
                //algo longt, on evite de les utiliser quand c'est pas nessecaire
                UniqueGroup();
                Burma();
            }
            ForceBrute();
        }
        
        // pour les valeures déja trouvées, on retires les valeures dans les lignes colones region corespondantes
        public void PropagerCertitudes() {
            //on retire les lignes qu'il manque
            foreach (var cell in Resolveds) 
                foreach (var element in ConcernedCells(cell.X, cell.Y))
                    element.Remove(cell.Value);
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
                //Debug.WriteLine("value : {0}", value);
                foreach (var area in Areas)
                {
                    ////Debug.WriteLine("area {0}",area);
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
                        ////Debug.WriteLine("line has jumeaux : " + lineWithValue.First().GetString());
                        ////Debug.WriteLine("Found line {0}",lineWithValue.First().First().Y);
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
                        ////Debug.WriteLine("column has jumeaux : " + columnWithValue.First().GetString());
                        ////Debug.WriteLine("Found colulms {0}", columnWithValue.First().First().X);
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
            ////Debug.WriteLine("end of function");
        }

        public IEnumerable<Case> ConcernedCells(int x, int y)
        {
            var areaLength = (int)Math.Sqrt((double)Content.Length);
            return (from cell in Cells
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
                ////Debug.WriteLine("value : " + value);
                foreach (var areaRow in AreaLines)
                {
                    for (var i = 0; i < AreaLineCount; ++i)
                    {
                        var areaTarget = areaRow.Where(area =>
                            area.Lines
                                .ElementAt(i)
                                .All(c => !c.Hypotheses.Contains(value))
                        );
                        if (areaTarget.Count() == AreaLineCount - 1)
                        {
                            var toEmpty = areaRow.Except(areaTarget).First();
                            ////Debug.WriteLine("lines : ");

                            ////Debug.WriteLine("to empty(row) : " + toEmpty);
                            ////Debug.WriteLine("line : " + i);
                            ////Debug.WriteLine("except(row) : " + toEmpty.Lines.ElementAt(i).GetString());
                            foreach (var cell in toEmpty.Except(toEmpty.Lines.ElementAt(i)))
                            {
                                cell.Remove(value);
                            }
                        }
                    }
                }

                foreach (var areaColumn in AreaColumns)
                {
                    for (var i = 0; i < AreaLineCount; ++i)
                    {
                        var areaTarget = areaColumn.Where(area => area.Columns.ElementAt(i).All(c => !c.Hypotheses.Contains(value)));
                        if (areaTarget.Count() == AreaLineCount - 1)
                        {

                            var toEmpty = areaColumn.Except(areaTarget).First();

                            ////Debug.WriteLine("to empty(column) : " + toEmpty);
                            ////Debug.WriteLine("except(column) : " + toEmpty.Columns.ElementAt(i).GetString());
                            ////Debug.WriteLine("column : " + i);

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
            foreach (var groupe in AllGroups)
            {
                foreach (var cell in groupe)
                {
                    var sub = from c in groupe
                        where !c.Hypotheses.Except(cell.Hypotheses).Any()
                        select c;
                    if (sub.Count() == cell.Hypotheses.Count())
                    {
                        foreach (var toEmpty in groupe.Except(sub))
                        {
                            foreach (var value in cell.Hypotheses.ToArray())
                            {
                                toEmpty.Remove(value);
                            }
                        }
                    }
                }
            }
        }

        public void Burma()
        {
            foreach (var value in PossibleValues)
            {
                ////Debug.WriteLine("value : " + value);
                for (var nbColone = PossibleValues.Length - 1; nbColone > 0; --nbColone)
                {
                    for(var nbValue = nbColone; nbValue > 1; --nbValue) //1 => solved...
                    {
                        ////Debug.WriteLine("n : " + nbColone);
                        ////Debug.WriteLine("c : " + nbValue);

                        {
                            ////Debug.WriteLine("column...");
                            var columns =  from column in Columns
                                           where column.Count(cell => cell.Hypotheses.Contains(value)) == nbValue
                                           select column;
                             var rows = columns
                                .SelectMany(column => 
                                    column
                                    .Where(cell => 
                                        cell.Hypotheses.Contains(value))
                                    .Select(cell => cell.Y)
                                )
                                .Distinct()
                                .Select(x => Lines.ElementAt(x));
                            ////Debug.WriteLine("columns : " + columns.Aggregate("", (c, n) => c + "|" + n.GetString()));
                            ////Debug.WriteLine("rows : " + rows.Aggregate("", (c, n) => c + "|" + n.GetString()));
                            if (columns.Count() == rows.Count() && columns.Count() == nbColone &&
                                rows.Select(c => c.Where(cs => columns.Any(column => column.Contains(cs)))).All(row => row.Count(cell => cell.Hypotheses.Contains(value)) == nbValue))
                                //can remove
                            {
                                ////Debug.WriteLine("FOUUUUUUUUUUUUUUND !!!!");
                                ////Debug.WriteLine("found : " + columns.Aggregate("",(c,n) => c + "|" + n.GetString()));
                                ////Debug.WriteLine("remove at : " + rows.Aggregate("", (c, n) => c + "|" + n.GetString()));
                                ////Debug.WriteLine("excluded : " + columns.Select(c => c).Aggregate("",(c,n) => c + "|" + n));
                                ////Debug.WriteLine("removing : " + rows.Select(row => row.Difference(columns.SelectMany(c => c))).Aggregate("", (c, n) => c + "|" + n.GetString()));
                                ////Debug.WriteLine("giving : " + rows.Select(row => row.Difference(columns.SelectMany(c => c))).Aggregate("", (c, n) => c + "|" + n.GetString()));
                                ////Debug.WriteLine("flatened : " + rows.SelectMany(row => row.Difference(columns.SelectMany(c => c))).GetString());
                                foreach (var cell in rows.SelectMany(row => row.Difference(columns.SelectMany(c => c))).ToArray())
                                {
                                    ////Debug.WriteLine("removing ({0},{1})",cell.X,cell.Y);
                                    cell.Remove(value);                                    
                                }
                                ////Debug.WriteLine("end of found");
                            }
                        }
                        {
                            ////Debug.WriteLine("row...");

                            var rows = from line in Lines
                                       where line.Count(cell => cell.Hypotheses.Contains(value)) == nbValue
                                       select line;
                            var columns = rows
                                .SelectMany(column => 
                                    column
                                        .Where(cell => cell.Hypotheses.Contains(value))
                                        .Select(cell => cell.X)
                                )
                                .Distinct()
                                .Select(y => Lines.ElementAt(y));
                             ////Debug.WriteLine("rows : " + rows.Aggregate("", (c, n) => c + "|" + n.GetString()));
                             ////Debug.WriteLine("columns : " + columns.Aggregate("", (c, n) => c + "|" + n.GetString()));

                             if (rows.Count() == columns.Count() && rows.Count() == nbColone
                                 && columns.Select(column => column.Where(c => rows.Any(row => row.Contains(c)))).All(colunm => colunm.Count(cell => cell.Hypotheses.Contains(value)) == nbValue))   //can remove
                             {
                                 ////Debug.WriteLine("FOUUUUUUUUUUUUUUND !!!!");

                                 ////Debug.WriteLine("found : " + rows.Aggregate("", (c, n) => c + "|" + n.GetString()));
                                 ////Debug.WriteLine("remove at : " + columns.Aggregate("", (c, n) => c + "|" + n.GetString()));
                                 ////Debug.WriteLine("excluded : " + rows.SelectMany(c => c).GetString());
                                 ////Debug.WriteLine("removing : " + columns.Select(column => column.Difference(rows.SelectMany(c => c))).Aggregate("", (c, n) => c + "|" + n.GetString()));
                                 ////Debug.WriteLine("giving" + columns.Select(column => column.Difference(rows.SelectMany(c => c))).Aggregate("", (c, n) => c + "|" + n.GetString()));
                                 ////Debug.WriteLine("flatened : " + columns.SelectMany(column => column.Difference(rows.SelectMany(c => c))).GetString());
                                 foreach (
                                     var cell in
                                         columns.SelectMany(column => column.Difference(rows.SelectMany(c => c))).ToArray())
                                 {
                                     ////Debug.WriteLine("removing ({0},{1})", cell.X, cell.Y);
                                     cell.Remove(value);
                                 }
                                 ////Debug.WriteLine("end of found");
                             }

                        }
                    }
                }
            }
        }
        public void UniqueGroup()
        {
            foreach (var groupe in AllGroups)
            {
                //toute les valeures qui non pas été trouvée dans le groupe
                var concerned = (from cell in groupe
                                where cell.Hypotheses.Count() > 1
                                select cell).ToArray();
                var values = concerned
                                .SelectMany(c => c.Hypotheses)
                                .Distinct()
                                .ToArray();
                foreach (var compination in values.GetCombination())
                {
                    for (var i = 1; i < compination.Count() - 1; ++i)
                    {
                        var cells = concerned.Where(cell => !cell.Hypotheses.Except(compination).Any());
                        if (cells.Count() == i)
                        {
                            foreach (var cell in cells)
                            {
                                foreach (var value in values.Except(compination).ToArray())
                                {
                                    cell.Remove(value);                                
                                }
                            }
                        }
                    }
                }
            }
        }

        public void XYWing()
        {
            var cells = NotResolveds.ToArray();
            foreach (var XY in NotResolveds.Where(c => c.Hypotheses.Count() == 2))
            {
                var X = XY.Hypotheses.First();
                var Y = XY.Hypotheses.ElementAt(1);
                ////Debug.WriteLine("X : " + X);
                ////Debug.WriteLine("Y : " + Y);
                ////Debug.WriteLine("XY : " + XY);
                var XZs = from cell in ConcernedCells(XY.X, XY.Y)
                         where cell.Hypotheses.Count() == 2
                         where cell.Hypotheses.Contains(X)
                         where ! cell.Hypotheses.Contains(Y)
                         select cell;
                foreach (var XZ in XZs)
                {
                    var Z = XZ.Hypotheses.Single(c => c != X);
                    ////Debug.WriteLine("XZ : " + XZ);
                    ////Debug.WriteLine("Z : " + Z);
                    var YZs = from cell in ConcernedCells(XY.X, XY.Y)
                              where cell.Hypotheses.Count() == 2
                              where cell.Hypotheses.Contains(Z)
                              where cell.Hypotheses.Contains(Y)
                              select cell;

                    foreach (var YZ in YZs)
                    {
                        ////Debug.WriteLine("YZ : " + YZ);
                        var XYZs = ConcernedCells(XZ.X, XZ.Y).Intersect(ConcernedCells(YZ.X, YZ.Y))
                                    .Where(cell =>
                                            cell.Hypotheses.Count() == 3
                                        &&  cell.Hypotheses.Contains(X)
                                        &&  cell.Hypotheses.Contains(Y)
                                        &&  cell.Hypotheses.Contains(Z)
                                    );
                        foreach (var XYZ in XYZs)
                        {
                            //Debug.WriteLine("XYZ : " + XYZ);
                            XYZ.Remove(Z);
                        }
                    }
                }
            }
        }

        public void copy(Grille grille)
        {
            Content = grille.Content.Select((line,y) =>
                line.Select((value,x) => 
                    new Case(value,this)
                ).ToArray()
            ).ToArray();
        }
        public void ForceBrute()
        {
            var cellsByHypotesisNumber = from c in Cells
                                         where !c.Resolved
                                         orderby c.Hypotheses.Count()
                                         select c;
            foreach (var cell in cellsByHypotesisNumber)
            {
                foreach (var choice in cell.Hypotheses)
                {
                    var trial = new Grille(this);
                    var changed = trial.Cells.Single(c => c.X == cell.X && c.Y == cell.Y);
                    changed.Value = choice;
                    trial.Resolve();
                    if (trial.IsValid && trial.Resolved)
                        copy(trial);
                }
            }
        }
    }


    public class InvalidGrilleException : Exception {
        public InvalidGrilleException(string message)
            : base(message) { }
    }
}
