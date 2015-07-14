using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;

namespace ProjetCsWpf
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = App.SudokuManager;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            var result = fileDialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK) return;

            var file = fileDialog.FileName;
            App.SudokuManager.GetFiles(file);
        }

        private void LbxSudokus_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DisplayGrid();
        }

        void DisplayGrid()
        {
            FrontGrille.Children.Clear();
            var Sudoku = App.SudokuManager.SelectedSudoku;
            var tb = new TextBlock
            {
                Text = Sudoku.Name
            };

            var grid = new Grid
            {
                ShowGridLines = true
            };

            for (var i = 0; i < Sudoku.Taille; ++i)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.RowDefinitions.Add(new RowDefinition());
            }
            
            FrontGrille.Children.Add(grid);

            for (var i = 0; i < Sudoku.Taille; ++i)
            {
                for (var j = 0; j < Sudoku.Taille; ++j)
                {
                    var text = new TextBlock
                    {
                        Text = Sudoku.Content[i][j].Resolved
                            ? Sudoku.Content[i][j].Value.ToString()
                            : Sudoku.Content[i][j].Hypotheses.Select(c => c.ToString()).Aggregate((s, c) => s + ", " + c),
                        TextAlignment = TextAlignment.Center,
                        Foreground = Sudoku.IsValid
                                        ? Brushes.Black
                                        : Brushes.Red
                    };
                    Grid.SetColumn(text, j);
                    Grid.SetRow(text, i);
                    grid.Children.Add(text);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (App.SudokuManager.SelectedSudoku == null) return;
            Debug.WriteLine("__propagation__");
            App.SudokuManager.SelectedSudoku.PropagerCertitudes();
            DisplayGrid();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (App.SudokuManager.SelectedSudoku == null) return;
            Debug.WriteLine("__un seul candidat__");
            App.SudokuManager.SelectedSudoku.UnSeulCandidat();
            DisplayGrid();
        }

        private void Btn_Trouver_Jumeaux_Click(object sender, RoutedEventArgs e)
        {
            if (App.SudokuManager.SelectedSudoku == null) return;
            Debug.WriteLine("__jumeaux__");
            App.SudokuManager.SelectedSudoku.TrouverJumeaux();
            DisplayGrid();
        }

        private void Btn_Trouver_Interaction_Click(object sender, RoutedEventArgs e)
        {
            if (App.SudokuManager.SelectedSudoku == null) return;
            Debug.WriteLine("__interaction__");
            App.SudokuManager.SelectedSudoku.TrouverInteraction();
            DisplayGrid();
        }
    }
}
