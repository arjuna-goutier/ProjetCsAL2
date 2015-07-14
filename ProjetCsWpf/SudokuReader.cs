using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProjetCsWpf
{
    public static class SudokuReader
    {
        public const int NameIndex= 0;
        public const int DateIndex = 1;
        public const int PossibleValuesIndex = 2;
        public const int GrilleStartIndex = 3;

        public static IEnumerable<Grille> ReadAll(string path) {
            if (!File.Exists(path)) throw new FileNotFoundException("MSG_ERREURNOFICHIER");
            return from Grille in seperateGrille(File.ReadAllLines(path))
                   select toGrille(Grille);
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
            catch (InvalidGrilleException e) {
                throw new InvalidSudokuFileException("MSG_ERREURFICHIERINCORRECT");
            }
        }
    }
    public class InvalidSudokuFileException : Exception {
        public InvalidSudokuFileException(string message) : base(message) { }
    }

}