namespace AsignacionMaestros.Forms;

/// <summary>Pregunta qué limpiar. Por seguridad, Enter = Cancelar.</summary>
public sealed class FrmLimpiar : Form
{
    private readonly CheckBox _chkDisponibilidad;

    private void InitializeComponent()
    {

    }

    public bool IncluirDisponibilidad => _chkDisponibilidad.Checked;

    public FrmLimpiar(string resumen, int celdasDisponibilidad)
    {
        Text = "Limpiar horarios";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 220);

        var lblResumen = new Label { Text = resumen, Left = 16, Top = 16, Width = 428, Height = 90 };
        _chkDisponibilidad = new CheckBox
        {
            Text = $"También borrar la disponibilidad de los maestros ({celdasDisponibilidad} combinaciones materia-hora que ofrecen)",
            Left = 16,
            Top = 112,
            Width = 428,
            Height = 44,
        };
        var btnLimpiar = new Button { Text = "Limpiar", DialogResult = DialogResult.OK, Left = 268, Top = 172, Width = 84 };
        var btnCancelar = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Left = 360, Top = 172, Width = 84 };

        Controls.AddRange(new Control[] { lblResumen, _chkDisponibilidad, btnLimpiar, btnCancelar });
        AcceptButton = btnCancelar;
        CancelButton = btnCancelar;
    }
}
