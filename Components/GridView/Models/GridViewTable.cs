using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
    public class GridViewColumns
    {
        public GridViewColumn[] Columns { get; set; }
        public static GridViewColumns Parse(DataColumnCollection dataColumnCollection)
        {
            List<GridViewColumn> columns = new();

            foreach (DataColumn dataColumn in dataColumnCollection)
                columns.Add(new GridViewColumn()
                {
                    Name = dataColumn.ColumnName
                });

            return new GridViewColumns()
            {
                Columns = columns.ToArray()
            };
        }
    }
    public class GridViewRows
    {
        public GridViewRow[] Rows { get; set; }
        public static GridViewRows Parse(DataRowCollection dataRowCollection)
        {
            List<GridViewRow> rows = new();

            foreach (DataRow dataRow in dataRowCollection)
            {
                List<GridViewCell> cells = new();

                foreach (object dataCell in dataRow.ItemArray)
                    cells.Add(new GridViewCell() { Value = dataCell });

                GridViewRow row = new()
                {
                    Cells = cells.ToArray()
                };

                rows.Add(row);
            }

            return new GridViewRows()
            {
                Rows = rows.ToArray()
            };
        }
    }

    public class GridViewColumn
    {
        public string Name { get; set; }
        public GridViewDataType DataType { get; set; }
    }

    public class GridViewRow
    {
        public bool Selected { get; set; }
        public string CssClasses
        {
            get
            {
                List<string> classes = new();

                if (Selected)
                    classes.Add("_selected");

                return string.Join(' ', classes);
            }
        }
        public GridViewCell[] Cells { get; set; }
    }

    public class GridViewCell
    {
        public object Value { get; set; }
        public string FormattedValue
        {
            get
            {
                return Value.ToString();
            }
        }
    }

    public enum GridViewDataType
    {
        String,
        Integer,
        Double
    }
}
