using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows;

namespace ProjetCsWpf
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static SudokuManagerViewModel SudokuManager { get; set; }

        static App ()
        {
            SudokuManager = new SudokuManagerViewModel("mon Titre");
        }
    }
}
