using System.ComponentModel;
using System.Data;
using Microsoft.Extensions.DependencyInjection;

namespace BloodPressureRecorder;

public enum ConnectionStatus { Offline, Connecting, Connected }

public partial class MainForm : Form
{
    private readonly IServiceProvider _services;
    private readonly string? _initInfo;
    private ConnectionStatus _connectionState;
    private int _connectionCheckCounter;

    public MainForm(IServiceProvider services, string? initInfo)
    {
        InitializeComponent();

        _services = services;
        _initInfo = initInfo;

        _connectionState = ConnectionStatus.Offline;
        toolStripStatusLabel1.Text = "\u0098";
        toolStripStatusLabel1.ForeColor = Color.Red;
        toolStripStatusLabel1.ToolTipText = "Offline";

        connectionCheckTimer.Enabled = true;
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        CheckConnection();
        if (_initInfo == null)
            return;
        var form = new DebugForm(_initInfo);
        form.ShowDialog();
    }

    private void CheckConnection()
    {
        _connectionState = ConnectionStatus.Connecting;
        toolStripStatusLabel1.ForeColor = Color.Gold;
        toolStripStatusLabel1.ToolTipText = "Connecting...";

        var connector = new BackgroundWorker();

        connector.DoWork += (o, args) =>
        {
            var result = _services.GetRequiredService<IDataHandler>().CheckConnectionAsync(default)
                .GetAwaiter().GetResult();
            args.Result = result;
        };

        connector.RunWorkerCompleted += (o, args) =>
        {
            var connected = (bool)(args.Result ?? false);
            if (connected)
            {
                _connectionCheckCounter = 30;
                _connectionState = ConnectionStatus.Connected;
                toolStripStatusLabel1.ForeColor = Color.Green;
                toolStripStatusLabel1.ToolTipText = "Online";
            }
            else
            {
                _connectionCheckCounter = 5;
                _connectionState = ConnectionStatus.Offline;
                toolStripStatusLabel1.ForeColor = Color.Red;
                toolStripStatusLabel1.ToolTipText = "Offline";
            }
        };

        connector.RunWorkerAsync();
    }

    private void saveButton_Click(object sender, EventArgs e)
    {
        var data = new BloodPressureData
        {
            Time = DateTime.UtcNow,
            Syst = int.TryParse(textBox1.Text, out var syst) ? syst : 0,
            Dias = int.TryParse(textBox2.Text, out var dias) ? dias : 0,
            Puls = int.TryParse(textBox3.Text, out var puls) ? puls : 0,
        };
        SaveData(data);
    }
    private void SaveData(BloodPressureData data)
    {
        toolStripStatusLabel2.Text = "Saving...";

        var saver = new BackgroundWorker();

        saver.DoWork += (o, args) =>
        {
            if (args.Argument != null)
            {
                var data = (BloodPressureData)args.Argument;
                _services.GetRequiredService<IDataHandler>().SaveDataAsync(data, default)
                    .GetAwaiter().GetResult();
            }
        };

        saver.RunWorkerCompleted += (o, args) =>
        {
            toolStripStatusLabel2.Text = "Saved";
            textBox1.Text = string.Empty;
            textBox2.Text = string.Empty;
            textBox3.Text = string.Empty;
            textBox1.Focus();
        };

        saver.RunWorkerAsync(data);
    }

    private void historyButton_Click(object sender, EventArgs e)
    {
        LoadHistory();
    }

    private void LoadHistory()
    {
        toolStripStatusLabel2.Text = "Loading history...";

        var loader = new BackgroundWorker();
        string historyDump = "...not loaded...";

        loader.DoWork += (o, args) =>
        {
            var history = _services.GetRequiredService<IDataHandler>()
                .LoadHistoryAsync(default).GetAwaiter().GetResult();

            using var writer = new StringWriter();
            writer.WriteLine("TIME                 SYS/DIA  PUL");
            writer.WriteLine("-------------------  -------  ---");
            foreach (var item in history)
                writer.WriteLine(
                    $"{item.Recorded:yyyy-MM-dd HH:mm:ss}  {item.Syst,3}/{item.Dias,-3}  {item.Puls,3}");

            historyDump = writer.GetStringBuilder().ToString();
        };

        loader.RunWorkerCompleted += (o, args) =>
        {
            toolStripStatusLabel2.Text = "History loaded.";
            var form = new DebugForm(historyDump);
            form.ShowDialog();
        };

        loader.RunWorkerAsync();
    }

    private void connectionCheckTimer_Tick(object sender, EventArgs e)
    {
        if (_connectionState == ConnectionStatus.Connecting)
            return;
        if(--_connectionCheckCounter <= 0)
            CheckConnection();
    }
}