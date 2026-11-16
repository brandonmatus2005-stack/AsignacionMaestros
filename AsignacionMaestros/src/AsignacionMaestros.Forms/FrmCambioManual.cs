using AsignacionMaestros.Core;

namespace AsignacionMaestros.Forms;

/// <summary>
/// Cambio manual de maestro para un slot. Muestra solo opciones válidas (el Core ya las filtró)
/// y devuelve lo elegido: una opción, o "quitar maestro". No guarda nada: eso lo hace FrmPrincipal.
/// </summary>
public sealed class FrmCambioManual : Form
{
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
    };
    private readonly Button _btnAplicar = new() { Text = "Aplicar", AutoSize = true, Enabled = false };
    private readonly Button _btnQuitar = new() { Text = "Quitar maestro", AutoSize = true };
    private readonly Button _btnCancelar = new() { Text = "Cancelar", AutoSize = true, DialogResult = DialogResult.Cancel };

    public OpcionManual? OpcionElegida { get; private set; }
    public bool QuitarMaestro { get; private set; }

    public FrmCambioManual(string slot, string situacionActual, IReadOnlyList<OpcionManual> opciones, bool puedeQuitar)
    {
        Text = "Cambio manual de maestro";
        Size = new Size(820, 460);
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;

        var lblSlot = new Label
        {
            Text = slot,
            Dock = DockStyle.Top,
            Height = 34,
            Padding = new Padding(8, 8, 8, 0),
            Font = new Font(Font, FontStyle.Bold),
        };
        var textoActual = situacionActual + (opciones.Count == 0
            ? $"{Environment.NewLine}No hay otro maestro que pueda dar esta materia a esta hora (o están fijos en otro slot)."
            : "");
        var lblActual = new Label { Text = textoActual, Dock = DockStyle.Top, Height = 64, Padding = new Padding(8, 4, 8, 0) };

        _grid.Columns.Add("Tipo", "Opción");
        _grid.Columns.Add("Detalle", "Qué pasa");
        _grid.Columns["Tipo"]!.FillWeight = 28;
        _grid.Columns["Detalle"]!.FillWeight = 100;
        foreach (var o in opciones)
        {
            var i = _grid.Rows.Add(NombreTipo(o.Tipo), o.Descripcion);
            _grid.Rows[i].Tag = o;
        }

        _btnQuitar.Visible = puedeQuitar;

        var botones = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 46,
            Padding = new Padding(6),
            FlowDirection = FlowDirection.RightToLeft,
        };
        botones.Controls.Add(_btnCancelar);     // con RightToLeft, el primero queda a la derecha
        botones.Controls.Add(_btnAplicar);
        botones.Controls.Add(_btnQuitar);

        // Orden de agregado: el Fill primero y los demás después (así WinForms acomoda bien los bordes).
        Controls.Add(_grid);
        Controls.Add(lblActual);
        Controls.Add(lblSlot);
        Controls.Add(botones);
        CancelButton = _btnCancelar;

        _grid.SelectionChanged += (_, _) => _btnAplicar.Enabled = _grid.CurrentRow?.Tag is OpcionManual;
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Aplicar(); };
        _btnAplicar.Click += (_, _) => Aplicar();
        _btnQuitar.Click += (_, _) =>
        {
            QuitarMaestro = true;
            DialogResult = DialogResult.OK;
        };

        _btnAplicar.Enabled = _grid.CurrentRow?.Tag is OpcionManual;
    }

    private void Aplicar()
    {
        if (_grid.CurrentRow?.Tag is not OpcionManual opcion) return;
        OpcionElegida = opcion;
        DialogResult = DialogResult.OK;
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        // 
        // FrmCambioManual
        // 
        ClientSize = new Size(933, 451);
        Name = "FrmCambioManual";
        Load += FrmCambioManual_Load;
        ResumeLayout(false);

    }

    private static string NombreTipo(TipoOpcion tipo) => tipo switch
    {
        TipoOpcion.Libre => "Asignar (está libre)",
        TipoOpcion.Intercambio => "Intercambiar",
        TipoOpcion.Reemplazo => "Reemplazar (deja otro pendiente)",
        _ => tipo.ToString(),
    };

    private void FrmCambioManual_Load(object sender, EventArgs e)
    {

    }
}
