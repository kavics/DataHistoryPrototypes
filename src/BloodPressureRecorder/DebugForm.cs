namespace BloodPressureRecorder;

public partial class DebugForm : Form
{
    public DebugForm(string dump)
    {
        InitializeComponent();

        textBox1.Text = dump;
    }

    private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == (char)Keys.Enter)
            Close();
        if (e.KeyChar == (char)Keys.Escape)
            Close();
    }

    private void textBox1_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        Close();
    }

    private void label1_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        Close();
    }

    private void DebugForm_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        Close();
    }

    private void DebugForm_KeyPress(object sender, KeyPressEventArgs e)
    {
        Close();
    }
}
