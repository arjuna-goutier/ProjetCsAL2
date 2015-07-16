using System.Collections.Generic;
using System.Linq;

namespace ProjetCsWpf
{
    public class CellGroup:IEnumerable<Case>
    {
        private readonly IEnumerable<Case> cells;

        public IEnumerable<IEnumerable<Case>> Columns
        {
            get
            {
                return from cell in cells
                       group cell by cell.X into column
                       select column;
            }
        }

        public IEnumerable<IEnumerable<Case>> Lines
        {
            get
            {
                return from cell in cells
                       group cell by cell.Y into line
                       select line;                
            }
        }
        public CellGroup(IEnumerable<Case> cells) {
            this.cells = cells;
        }

        public CellGroup(params Case[] cells):this(cells as IEnumerable<Case>) {
        }

        //regarde si toute les cellules sont identiques
        public bool IsValid {
            get {
                return cells.Where(c => c.Resolved)
                            .UniqueValues(new CellValueComparator());
            }
        }
        public override string ToString()
        {
            return cells.GetString();
            //cells.Aggregate("",(s,cell) => s + string.Format("({1},{2}) -> {0};",(cell.Resolved ?  cell.Value.ToString() : cell.Hypotheses.Aggregate("",(c,n) => c+ ","+n)),cell.X,cell.Y));
        }

        public IEnumerator<Case> GetEnumerator()
        {
            return cells.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return cells.GetEnumerator();
        }
    }
}
