using System.Globalization;
using AsignacionMaestros.Core;
using AsignacionMaestros.Forms.Datos;

namespace AsignacionMaestros.Forms;

/// <summary>
/// Vista de solo lectura: una tablita por maestro (como las del deck) con las materias que ofrece
/// (columnas) y las horas en que las ofrece (filas). Si el maestro ya tiene asignado ese cruce,
/// la celda muestra el grupo.
/// </summary>
public sealed class FrmDisponibilidad : Form
{
    private readonly IReadOnlyList<Maestro> _maestros;
    private readonly IReadOnlyList<Slot> _slots;
    private readonly Dictionary<int, MateriaItem> _materias;
    private readonly IReadOnlyList<BloqueItem> _bloques;
    private readonly Font _negrita;

    private readonly TextBox _txtFiltro = new() { Width = 260, PlaceholderText = "Maestro o materia…" };
    private readonly Label _lblResumen = new() { AutoSize = true, Margin = new Padding(16, 8, 0, 0) };
    private readonly FlowLayoutPanel _contenedor = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        WrapContents = true,
        FlowDirection = FlowDirection.LeftToRight,
        Padding = new Padding(8),
    };

    public FrmDisponibilidad(
        IReadOnlyList<Maestro> maestros, IReadOnlyList<Slot> slots,
        IReadOnlyList<MateriaItem> materias, IReadOnlyList<BloqueItem> bloques)
    {
        _maestros = maestros;
        _slots = slots;
        _materias = materias.ToDictionary(x => x.Id);
        _bloques = bloques;
        _negrita = new Font(Font, FontStyle.Bold);

        Text = "Disponibilidad de maestros";
        Size = new Size(1100, 720);
        StartPosition = FormStartPosition.CenterParent;

        var leyenda = new Label
        {
            AutoSize = true,
            Margin = new Padding(24, 8, 0, 0),
            Text = "✓ = disponible   ·   5A = ya asignado a ese grupo   ·   vacío = no puede a esa hora",
        };
        var panelSuperior = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8) };
        panelSuperior.Controls.Add(new Label { Text = "Buscar:", AutoSize = true, Margin = new Padding(0, 8, 4, 0) });
        panelSuperior.Controls.Add(_txtFiltro);
        panelSuperior.Controls.Add(_lblResumen);
        panelSuperior.Controls.Add(leyenda);

        Controls.Add(_contenedor);
        Controls.Add(panelSuperior);

        _txtFiltro.TextChanged += (_, _) => Reconstruir();
        Reconstruir();
    }

    private string NombreMateria(int id) =>
        _materias.TryGetValue(id, out var m) ? m.Nombre : $"Materia {id}";

    private void Reconstruir()
    {
        var filtro = _txtFiltro.Text.Trim();
        var lista = _maestros.OrderBy(m => m.Clave, StringComparer.Ordinal).Where(m => Coincide(m, filtro)).ToList();

        _contenedor.SuspendLayout();
        var viejos = _contenedor.Controls.Cast<Control>().ToList();
        _contenedor.Controls.Clear();
        foreach (var c in viejos) c.Dispose();
        foreach (var m in lista) _contenedor.Controls.Add(CrearTarjeta(m));
        _contenedor.ResumeLayout();

        _lblResumen.Text = $"{lista.Count} de {_maestros.Count} maestros";
    }

    private bool Coincide(Maestro m, string filtro)
    {
        if (filtro.Length == 0) return true;
        if (Contiene(m.Clave, filtro) || Contiene(m.Nombre, filtro)) return true;
        return m.Ofrece.Select(o => o.IdMateria).Distinct().Any(id =>
            Contiene(NombreMateria(id), filtro) || (_materias.TryGetValue(id, out var x) && Contiene(x.Clave, filtro)));
    }

    // Ignora mayúsculas y acentos: "matematicas" encuentra "Matemáticas".
    private static bool Contiene(string texto, string filtro) =>
        CultureInfo.CurrentCulture.CompareInfo.IndexOf(
            texto, filtro, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    private Control CrearTarjeta(Maestro m)
    {
        var asignados = _slots.Where(s => s.IdMaestro == m.Id)
                              .GroupBy(s => (s.IdMateria, s.Bloque))
                              .ToDictionary(g => g.Key, g => g.First());

        var materiasIds = m.Ofrece.Select(o => o.IdMateria)
                           .Concat(asignados.Keys.Select(k => k.Item1))
                           .Distinct()
                           .OrderBy(NombreMateria, StringComparer.CurrentCultureIgnoreCase)
                           .ToList();

        var caja = new GroupBox
        {
            Text = $"{m.Clave} · {m.Nombre}   —   {materiasIds.Count} materias · {asignados.Count} asignadas",
            Margin = new Padding(8),
        };

        if (materiasIds.Count == 0)
        {
            caja.Size = new Size(430, 70);
            caja.Controls.Add(new Label
            {
                Text = "Sin disponibilidad registrada.",
                AutoSize = true,
                Location = new Point(12, 30),
                ForeColor = Color.Gray,
            });
            return caja;
        }

        var grid = CrearTabla(m, materiasIds, asignados);
        grid.Location = new Point(10, 22);
        caja.Controls.Add(grid);
        caja.Size = new Size(Math.Max(grid.Width + 20, 430), grid.Height + 34);
        return caja;
    }

    private DataGridView CrearTabla(Maestro m, List<int> materiasIds, Dictionary<(int, int), Slot> asignados)
    {
        const int anchoHora = 70, anchoMateria = 112, altoEncabezado = 56, altoFila = 24;

        var grid = new DataGridView
        {
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AllowUserToResizeColumns = false,
            RowHeadersVisible = false,
            ScrollBars = ScrollBars.None,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = altoEncabezado,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.FixedSingle,
        };
        grid.RowTemplate.Height = altoFila;
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Hora",
            Width = anchoHora,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        });
        foreach (var id in materiasIds)
        {
            var clave = _materias.TryGetValue(id, out var info) ? info.Clave : "";
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = NombreMateria(id),
                ToolTipText = $"{clave} · {NombreMateria(id)}",
                Width = anchoMateria,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            });
        }

        foreach (var b in _bloques)
        {
            var fila = grid.Rows[grid.Rows.Add()];
            fila.Cells[0].Value = b.Etiqueta;
            Pintar(fila.Cells[0], Color.WhiteSmoke, negrita: false);

            for (var j = 0; j < materiasIds.Count; j++)
            {
                var celda = fila.Cells[j + 1];
                if (asignados.TryGetValue((materiasIds[j], b.Id), out var slot))
                {
                    celda.Value = slot.Paquete;
                    Pintar(celda, Color.LightSkyBlue, negrita: true);
                }
                else if (m.Ofrecer(materiasIds[j], b.Id))
                {
                    celda.Value = "✓";
                    Pintar(celda, Color.Honeydew, negrita: false);
                }
                else
                {
                    Pintar(celda, Color.White, negrita: false);
                }
            }
        }

        grid.ClearSelection();
        grid.Size = new Size(anchoHora + materiasIds.Count * anchoMateria + 3,
                             altoEncabezado + _bloques.Count * altoFila + 3);
        return grid;
    }

    private void InitializeComponent()
    {

    }

    // La celda no cambia de color al hacer clic: es una vista, no un editor.
    private void Pintar(DataGridViewCell celda, Color fondo, bool negrita)
    {
        celda.Style.BackColor = fondo;
        celda.Style.SelectionBackColor = fondo;
        celda.Style.ForeColor = Color.Black;
        celda.Style.SelectionForeColor = Color.Black;
        if (negrita) celda.Style.Font = _negrita;
    }
}
