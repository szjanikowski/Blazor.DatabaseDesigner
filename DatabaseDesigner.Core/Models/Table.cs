using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace DatabaseDesigner.Core.Models
{
    public class Table : NodeModel
    {
        public Table(Point position = null) : base(position, RenderLayer.HTML)
        {
            Columns = new List<Column>
            {
                new Column
                {
                    Name = "Id",
                    Type = ColumnType.Integer,
                    Primary = true
                },
                new Column
                {
                    Name = "Test",
                    Type = ColumnType.Integer
                }
            };

            foreach (var col in Columns)
            {
                AddPort(col, PortAlignment.Top);
                AddPort(col, PortAlignment.Bottom);
            }
        }

        public string Name { get; set; } = "Table";
        public List<Column> Columns { get; }
        public bool HasPrimaryColumn => Columns.Any(c => c.Primary);

        public ColumnPort GetPort(Column column, PortAlignment alignment = PortAlignment.Bottom)
            => Ports.Cast<ColumnPort>().FirstOrDefault(p => p.Column == column && p.Alignment == alignment);

        public void AddPort(Column column, PortAlignment alignment) => AddPort(new ColumnPort(this, column, alignment));
    }
}
