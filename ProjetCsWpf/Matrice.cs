using System.Collections.Generic;

namespace ProjetCsWpf
{
    //une sourcouche a la matrice pour hériter d'IEnumerable
    class Matrix<T> : IEnumerable<T>
    {
        public delegate T Initialise();
        public T[,] Values{get; private set;}

        public Matrix(int height, int width,Initialise initialise = null) {
            Values = new T[width,height];
            if (initialise == null) return;

            for (var y = 0; y < height; ++y)
                for (var x = 0; x < height; ++x)
                    Values[x, y] = initialise();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            return new WholeMatrixEnumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return new WholeMatrixEnumerator(this);
        }
        //enumerator de matrice
        abstract class MatrixEnumerator : IEnumerator<T> {
            protected int X = 0;
            protected int Y = 0;
            protected Matrix<T> Source;
            public T Current {
                get { return Source.Values[X, Y]; }
            }

            object System.Collections.IEnumerator.Current {
                get { return Source.Values[X, Y]; }
            }
            public abstract bool MoveNext();
            public abstract void Reset();

            protected MatrixEnumerator(Matrix<T> source,int x = 0, int y = 0) {
                Source = source;
                X = x;
                Y = y;
            }

            public void Dispose() {
                Source = null;
            }


        }
        class WholeMatrixEnumerator:MatrixEnumerator {
            public WholeMatrixEnumerator(Matrix<T> source) :
                base(source: source) { }

            public override bool MoveNext() {
                if (X < Source.Values.GetUpperBound(0))
                    ++X;
                else {
                    X = 0;
                    ++Y;
                }
                return Y <= Source.Values.GetUpperBound(1);
            }
            public override void Reset() {
                X = 0;
                Y = 0;
            }
        }
    }
}
