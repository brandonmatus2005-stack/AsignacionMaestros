namespace AsignacionMaestros.Forms;

/// <summary>Muestra qué cambiaría antes de guardar. Aplicar = DialogResult.OK.</summary>
public sealed class FrmVistaPrevia : Form
{
    public FrmVistaPrevia(string resumen, IEnumerable<string[]> filas)
    {
        Text = "Vista previa de cambios";
        Size = new Size(760, 480);
        StartPosition = FormStartPosition.CenterParent;

        var lblResumen = new Label
        {
            Text = resumen,
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(8),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };
        foreach (var titulo in new[] { "Slot", "Antes", "Después", "Estado" })
            grid.Columns.Add(titulo, titulo);
        foreach (var fila in filas)
            grid.Rows.Add(fila);

        var botones = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 46,
            Padding = new Padding(6),
            FlowDirection = FlowDirection.RightToLeft,
        };
        var btnAplicar = new Button { Text = "Aplicar", DialogResult = DialogResult.OK, AutoSize = true };
        var btnCancelar = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, AutoSize = true };
        botones.Controls.Add(btnAplicar);
        botones.Controls.Add(btnCancelar);

        Controls.Add(grid);
        Controls.Add(lblResumen);
        Controls.Add(botones);
        AcceptButton = btnAplicar;
        CancelButton = btnCancelar;
    }

    private void InitializeComponent()
    {

    }
}
