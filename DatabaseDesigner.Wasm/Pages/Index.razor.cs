using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using DatabaseDesigner.Core.Models;
using DatabaseDesigner.Wasm.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DatabaseDesigner.Wasm.Pages
{
    public partial class Index : IDisposable
    {
        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        public Diagram Diagram { get; } = new Diagram(new DiagramOptions
        {
            GridSize = 40,
            AllowMultiSelection = true,
            Links = new DiagramLinkOptions
            {
                Factory = (diagram, sourcePort) =>
                {
                    return new LinkModel(sourcePort, null)
                    {
                        // Router = Routers.Orthogonal,
                        PathGenerator = PathGenerators.Smooth,
                    };
                }
            }
        });

        public void Dispose()
        {
            Diagram.Links.Added -= OnLinkAdded;
            Diagram.Links.Removed -= Diagram_LinkRemoved;
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            Diagram.RegisterModelComponent<Table, TableNode>();
            Diagram.SuspendRefresh = true;            
            // Tworzenie 200 tabel równomiernie rozmieszczonych
            CreateTablesGrid(20, 10); // 20 kolumn x 10 wierszy = 200 tabel
            Diagram.SuspendRefresh = false;
            // Diagram.Links.Added += OnLinkAdded;
            // Diagram.Links.Removed += Diagram_LinkRemoved;
        }

        private void CreateTablesGrid(int columns, int rows)
        {
            var tables = new Table[rows, columns];
            var spacing = 400; // Odległość między tabelami

            // Tworzenie tabel
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    var table = new Table(new Point(col * spacing, row * spacing))
                    {
                        Name = $"Table_{row}_{col}"
                    };
                    
                    tables[row, col] = table;
                    Diagram.Nodes.Add(table);
                }
            }

            // Łączenie tabel - dolny port kolumny Id z górnym portem kolumny Test w tabeli poniżej
            for (int row = 0; row < rows - 1; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    var currentTable = tables[row, col];
                    var bottomTable = tables[row + 1, col];

                    // Znajdź kolumnę Id (Primary) w tabeli źródłowej
                    var sourceIdColumn = currentTable.Columns.FirstOrDefault(c => c.Primary);
                    // Znajdź kolumnę Test w tabeli docelowej
                    var targetTestColumn = bottomTable.Columns.FirstOrDefault(c => !c.Primary && c.Name == "Test");

                    if (sourceIdColumn != null && targetTestColumn != null)
                    {
                        var sourcePort = currentTable.GetPort(sourceIdColumn, PortAlignment.Bottom);
                        var targetPort = bottomTable.GetPort(targetTestColumn, PortAlignment.Top);
                        if (sourcePort != null && targetPort != null)
                        {
                            var link = new LinkModel(sourcePort, targetPort)
                            {
                                PathGenerator = PathGenerators.Smooth,
                            };
                            Diagram.Links.Add(link);
                        }
                    }
                }
            }
        }

        private void ConnectTables(Table sourceTable, Table targetTable)
        {
            // Znajdź kolumnę Id (Primary) w tabeli źródłowej
            var sourceIdColumn = sourceTable.Columns.FirstOrDefault(c => c.Primary);
            if (sourceIdColumn == null) return;

            // Znajdź kolumnę Test w tabeli docelowej
            var targetTestColumn = targetTable.Columns.FirstOrDefault(c => !c.Primary && c.Name == "Test");
            if (targetTestColumn == null) return;

            // Pobierz porty
            var sourcePort = sourceTable.GetPort(sourceIdColumn);
            var targetPort = targetTable.GetPort(targetTestColumn);
            
            if (sourcePort != null && targetPort != null)
            {
                // Utwórz połączenie
                var link = new LinkModel(sourcePort, targetPort)
                {
                    // Router = Routers.Orthogonal,
                    PathGenerator = PathGenerators.Smooth,
                };
                
                Diagram.Links.Add(link);
            }
        }

        private void OnLinkAdded(BaseLinkModel link)
        {
            link.TargetPortChanged += OnLinkTargetPortChanged;
        }

        private void OnLinkTargetPortChanged(BaseLinkModel link, PortModel oldPort, PortModel newPort)
        {
            link.Labels.Add(new LinkLabelModel(link, "1..*", -40, new Point(0, -30)));
            link.Refresh();
            
            ((newPort ?? oldPort) as ColumnPort).Column.Refresh();
        }

        private void Diagram_LinkRemoved(BaseLinkModel link)
        {
            link.TargetPortChanged -= OnLinkTargetPortChanged;

            if (!link.IsAttached)
                return;

            var sourceCol = (link.SourcePort as ColumnPort).Column;
            var targetCol = (link.TargetPort as ColumnPort).Column;
            (sourceCol.Primary ? targetCol : sourceCol).Refresh();
        }

        private void NewTable()
        {
            Diagram.Nodes.Add(new Table());
        }

        private async Task ShowJson()
        {
            var json = JsonConvert.SerializeObject(new
            {
                Nodes = Diagram.Nodes.Cast<object>(),
                Links = Diagram.Links.Cast<object>()
            }, Formatting.Indented, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            await JSRuntime.InvokeVoidAsync("console.log", json);
        }

        private void Debug()
        {
            Console.WriteLine(Diagram.Container);
            foreach (var port in Diagram.Nodes.ToList()[0].Ports)
                Console.WriteLine(port.Position);
        }
    }
}
