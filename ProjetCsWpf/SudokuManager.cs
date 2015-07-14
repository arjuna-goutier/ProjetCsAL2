using System;

namespace ProjetCsWpf
{
    static class SudokuManager
    {
        public static void DisplayValidity()
        {
            try {
                Console.WriteLine("Enter the file's path : ");
                var path = Console.ReadLine();
                foreach (var grille in SudokuReader.ReadAll(path))
                    Console.WriteLine("{0} is {1}", grille.Name, grille.IsValid ? "valid" : "not valid");
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
            Console.ReadLine();
        }
        public static void DisplayResolved() {
            try {
                Console.WriteLine("Enter the file's path : ");
                var path = @"C:\Users\Moi\Desktop\Sudokus.sud"; //Console.ReadLine();

                foreach (var grille in SudokuReader.ReadAll(path))
                {
                    grille.Resolve();
                    Console.WriteLine(grille);
                    Console.WriteLine(grille.IsValid);
                    //Console.WriteLine("{0} is {1}", grille.Name, grille.IsValid ? "valid" : "not valid");
                    //Console.WriteLine("{0} is {1}", grille.Name, grille.IsValid ? "valid" : "not valid");
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
            Console.ReadLine();
        }
    }
}
