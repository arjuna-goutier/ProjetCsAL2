using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
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
            var bindingExpression = BtnResolveAll.GetBindingExpression(IsEnabledProperty);
            if (bindingExpression != null)
                bindingExpression.UpdateTarget();
            var bindingExpression1 = BtnSave.GetBindingExpression(IsEnabledProperty);
            if (bindingExpression1 != null)
                bindingExpression1.UpdateTarget();
        }

        private void LbxSudokus_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DisplayGrid();
        }

        void DisplayGrid()
        {
            FrontGrille.Children.Clear();
            var Sudoku = App.SudokuManager.SelectedSudoku;
            if (Sudoku == null) return;
            var tb = new TextBlock {
                Text = Sudoku.Name
            };
            var stateConverter = new StateToColorConverter();
            var grid = new Grid ()
            {
                Background = Sudoku.IsValid
                                ? Sudoku.Resolved
                                    ? Brushes.LightGreen
                                    : Brushes.White
                                : Brushes.Red
            };

            for (var i = 0; i < Sudoku.AreaLineCount; ++i)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.RowDefinitions.Add(new RowDefinition());
            }
            
            FrontGrille.Children.Add(grid);
            for (var i = 0; i < Sudoku.AreaLineCount; ++i) {
                for (var j = 0; j < Sudoku.AreaLineCount; ++j)
                {
                    var area = Sudoku.AreaLines.ElementAt(i).ElementAt(j);
                    var areaGrid = new Grid(){};
                    for (var x = 0; x < Sudoku.AreaLineCount; ++x)
                    {
                        areaGrid.ColumnDefinitions.Add(new ColumnDefinition());
                        areaGrid.RowDefinitions.Add(new RowDefinition());
                    }

                    var border = new Border
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Child = areaGrid
                    };
                    for (var x = 0; x < Sudoku.AreaLineCount; ++x)
                    {
                        for (var y = 0; y < Sudoku.AreaLineCount; ++y)
                        {
                            var cell = area.Lines.ElementAt(y).ElementAt(x);
                            var areaBorder = new Border
                            {
                                BorderThickness = new Thickness(0.25),
                                BorderBrush = Brushes.Black
                            };
                            if (cell.Resolved)
                            {
                                areaBorder.Child = new Viewbox
                                {
                                    Stretch = Stretch.Fill,
                                    Child = new TextBlock
                                    {
                                        Margin = new Thickness(5),
                                        Text = cell.Value.ToString(),
                                        TextAlignment = TextAlignment.Center,
                                        TextWrapping = TextWrapping.Wrap
                                    }
                                };
                            }
                            else
                            {
                                var cellGridSizeX =(int) Math.Ceiling(Math.Sqrt(cell.Hypotheses.Count()));
                                var cellGridSizeY = Math.Ceiling((double)cell.Hypotheses.Count() / cellGridSizeX);
                                var cellGrid = new Grid();
                                for (var z = 0; z < cellGridSizeX; ++z)
                                    cellGrid.ColumnDefinitions.Add(new ColumnDefinition());
                                for (var z = 0; z < cellGridSizeY; ++z)
                                    cellGrid.RowDefinitions.Add(new RowDefinition());
                                var cellX = 0;
                                var cellY = 0;
                                foreach (var hypothesis in cell.Hypotheses)
                                {
                                    var txtHyp = new TextBlock
                                    {
                                        Text = hypothesis.ToString(),
                                        TextAlignment = TextAlignment.Center,
                                        TextWrapping = TextWrapping.Wrap
                                    };
                                    Grid.SetColumn(txtHyp, cellX++);
                                    Grid.SetRow(txtHyp, cellY);
                                    if (cellX == cellGridSizeX) {
                                        cellX = 0;
                                        ++cellY;
                                    }
                                    cellGrid.Children.Add(txtHyp);
                                }
                                areaBorder.Child = cellGrid;
                            }
                            Grid.SetColumn(areaBorder,x);
                            Grid.SetRow(areaBorder, y);
                            areaGrid.Children.Add(areaBorder);
                        }
                    }
                    Grid.SetColumn(border, j);
                    Grid.SetRow(border, i);
                    grid.Children.Add(border);
                }
            }
            TxtSolvingTime.Text = Sudoku.Resolved
                    ? "temps de resolution : " + Sudoku.SolvingTime + " milisecondes"
                    : "";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (App.SudokuManager.SelectedSudoku == null) return;
            ////Debug.WriteLine(App.SudokuManager.FileName + ".PropagerCertitudes");
            App.SudokuManager.SelectedSudoku.PropagerCertitudes();
            updateView();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (App.SudokuManager.SelectedSudoku == null) return;
            ////Debug.WriteLine(App.SudokuManager.FileName + ".UnSeulCandidat");
            App.SudokuManager.SelectedSudoku.UnSeulCandidat();
            updateView();
        }

        private void Btn_Trouver_Jumeaux_Click(object sender, RoutedEventArgs e)
        {
            if (App.SudokuManager.SelectedSudoku == null) return;
            ////Debug.WriteLine(App.SudokuManager.FileName + ".TrouverJumeaux");
            App.SudokuManager.SelectedSudoku.TrouverJumeaux();
            updateView();
        }

        private void Btn_Trouver_Interaction_Click(object sender, RoutedEventArgs e)
        {
            if (App.SudokuManager.SelectedSudoku == null) return;
            ////Debug.WriteLine(App.SudokuManager.SelectedSudoku.Name + ".TrouverInteraction");
            App.SudokuManager.SelectedSudoku.TrouverInteraction();
            updateView();
        }

        private void Btn_Trouver_GroupeIsole_Click(object sender, RoutedEventArgs e)
        {
            if (App.SudokuManager.SelectedSudoku == null) return;
            ////Debug.WriteLine(App.SudokuManager.SelectedSudoku.Name + ".GroupeIsoles");
            App.SudokuManager.SelectedSudoku.GroupeIsoles();
            updateView();
        }

        private void BtnResoleAll_OnCLick(object sender, RoutedEventArgs e)
        {
            var worker = new BackgroundWorker {
                WorkerReportsProgress = true
            };
            worker.DoWork += (s, ev) => {
                foreach (var sudoku in App.SudokuManager.Sudokus.ToArray()) {
                    //each sudoku will be solved asynchronously
                    var watch = Stopwatch.StartNew();
                    watch.Start();
                    sudoku.Resolve();
                    watch.Stop();
                    sudoku.SolvingTime = watch.ElapsedMilliseconds;
                }
            };
            worker.RunWorkerAsync();
        }

        private void Btn_Burma_Click(object sender, RoutedEventArgs e)
        {
            if (App.SudokuManager.SelectedSudoku == null) return;
            ////Debug.WriteLine(App.SudokuManager.SelectedSudoku.Name + ".Burma");

            App.SudokuManager.SelectedSudoku.Burma();
            updateView();
        }

        private void updateView()
        {
            LbxSudokus.Items.Refresh();
            DisplayGrid();            
        }

        private void Btn_Groupe_Unique_Click(object sender, RoutedEventArgs e)
        {
            if (App.SudokuManager.SelectedSudoku == null)
                return;
            ////Debug.WriteLine(App.SudokuManager.SelectedSudoku.Name + ".FileName");

            App.SudokuManager.SelectedSudoku.UniqueGroup();
            updateView();
        }

        private void Resolve(Grille sudoku,bool thenDisplay = false)
        {
            var worker = new BackgroundWorker();
            worker.DoWork += (s, ev) =>
            {
                var watch = Stopwatch.StartNew();
                watch.Start();
                sudoku.Resolve();
                watch.Stop();
                sudoku.SolvingTime = watch.ElapsedMilliseconds;
            };
            if (thenDisplay)
            {
                worker.RunWorkerCompleted += (s, ev) =>
                {
                    updateView();
                };                
            }
            worker.RunWorkerAsync();

        }
        private void BtnResolve_OnCLick(object sender, RoutedEventArgs e)
        {
            if (App.SudokuManager.SelectedSudoku == null)
                return;
            Resolve(App.SudokuManager.SelectedSudoku,true);
        }

        private void Btn_Brute_Click(object sender, RoutedEventArgs e)
        {
            if (App.SudokuManager.SelectedSudoku == null) return;
            ////Debug.WriteLine(App.SudokuManager.SelectedSudoku.Name + ".ForceBrute");
            App.SudokuManager.SelectedSudoku.ForceBrute();
            updateView();
        }

        private void Btn_XY_WING_Click(object sender, RoutedEventArgs e)
        {
            if (App.SudokuManager.SelectedSudoku == null) return;
            ////Debug.WriteLine(App.SudokuManager.SelectedSudoku.Name + ".XYWing");
            App.SudokuManager.SelectedSudoku.XYWing();
            updateView();
        }

        private void BtnSave_OnCLick(object sender, RoutedEventArgs e)
        {
            var fileDialog = new SaveFileDialog
            {
                DefaultExt = ".sud"
            };
            var result = fileDialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK) return;

            var file = fileDialog.FileName;
            App.SudokuManager.SaveFile(file);
        }
    }
}
