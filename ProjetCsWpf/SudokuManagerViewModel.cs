using System;
using System.Collections.ObjectModel;

namespace ProjetCsWpf
{
    public class SudokuManagerViewModel
    {
        public string FileName { get; set; }
        public string Title { get; set; }
        public ObservableCollection<Grille> Sudokus { get; set; }
        public Grille SelectedSudoku { get; set; }

        public SudokuManagerViewModel(string title)
        {
            Title = title;
            SelectedSudoku = null;
            Sudokus = new ObservableCollection<Grille>();
        }


        public void GetFiles(string path)
        {
            Sudokus.Clear();
            foreach (var sudoku in SudokuFile.Read(path))
            {
                Sudokus.Add(sudoku);
            }
            FileName = path;
        }

        public void SaveFile(string path)
        {
            SudokuFile.Write(path, Sudokus);
        }
    }

    public class SudokuViewModel
    {
        public string Name { get; private set; }
        public string Title { get; private set; }
        public DateTime Date { get; private set; }
        public ObservableCollection<ObservableCollection<char>> Values { get; private set; }
        public ObservableCollection<char> PossiblesValues { get; set; }
        public SudokuViewModel(string name, string title, DateTime date)
        {
            Name = name;
            Title = title;
            Date = date;
            Values = new ObservableCollection<ObservableCollection<char>>();
            PossiblesValues = new ObservableCollection<char>()
            {
                '1',
                '2',
                '3',
                '4',
                '5',
                '6',
                '7',
                '8',
                '9',
            };
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
