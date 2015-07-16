using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace ProjetCsWpf
{
    public static class SudokuFile
    {
        public const int NameIndex= 0;
        public const int DateIndex = 1;
        public const int PossibleValuesIndex = 2;
        public const int GrilleStartIndex = 3;
        public static readonly string separationLine = "//---------------------------" + Environment.NewLine;
        public static IEnumerable<Grille> Read(string path) {
            if (!File.Exists(path)) throw new FileNotFoundException("MSG_ERREURNOFICHIER");
            return from Grille in seperateGrille(File.ReadAllLines(path))
                   let grille = toGrille(Grille)
                   where grille != null
                   select grille;
        }

        private static IEnumerable<IEnumerable<string>> seperateGrille(string[] lines) {
            return lines.SplitWhen(x => x.StartsWith("-") || x.StartsWith("/"));
        }

        private static Grille toGrille(IEnumerable<string> lines) {
            try {
                return new Grille(
                    name: lines.ElementAt(NameIndex),
                    creationTime: DateTime.Parse(lines.ElementAt(DateIndex)),
                    possibleValues: lines.ElementAt(PossibleValuesIndex).ToArray(),
                    grille: lines.Skip(GrilleStartIndex).ToArrayArray()
                );
            }
            catch (InvalidGrilleException e)
            {
                return null;
                throw new InvalidSudokuFileException("MSG_ERREURFICHIERINCORRECT");
            }
        }

        public static void Write(string path, IEnumerable<Grille> sudokus)
        {
            using (var file = File.CreateText(path))
            {
                foreach (var s in new[] {separationLine}.Concat(sudokus.Select(Stringify).Rely(Environment.NewLine + separationLine)))
                {
                    file.Write(s);
                }
            }
        }

        private static string Stringify(Grille sudoku)
        {
            return sudoku.Name + Environment.NewLine
                +  sudoku.CreationDate + Environment.NewLine
                +  sudoku.PossibleValues.Aggregate("",(c,n) => c + n) + Environment.NewLine
                +  sudoku.Lines
                    .Select(line => 
                        line.Select(cell => 
                            cell.Resolved
                                ? cell.Value.ToString()
                                : "."
                        )
                    )
                    .Aggregate("",(current, line) =>
                        current + (current == "" ? "" : Environment.NewLine)
                        +  line.Aggregate((currentLine, value) =>
                            currentLine + value
                        )
                    );
        }
    }
    public class InvalidSudokuFileException : Exception {
        public InvalidSudokuFileException(string message) : base(message) { }
    }

}