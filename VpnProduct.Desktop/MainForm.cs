using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace VpnProduct.Desktop;

public class MainForm : Form
{
    private readonly TextBox txtApiUrl = new();
    private readonly TextBox txtEmail = new();
    private readonly TextBox txtPassword = new();
    private readonly Button btnConnect = new();
    private readonly Button btnDisconnect = new();
    private readonly TextBox txtLog = new();

    public MainForm()
    {
        Text = "VpnProduct Client";
        Width = 720;
        Height = 560;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft JhengHei UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;

        var lblApi = new Label { Text = "API URL", Left = 20, Top = 20, Width = 120 };
        txtApiUrl.Left = 20;
        txtApiUrl.Top = 45;
        txtApiUrl.Width = 640;
        txtApiUrl.Text = "http://61.70.3.87:5049";

        var lblEmail = new Label { Text = "Email", Left = 20, Top = 90, Width = 120 };
        txtEmail.Left = 20;
        txtEmail.Top = 115;
        txtEmail.Width = 640;

        var lblPassword = new Label { Text = "Password", Left = 20, Top = 160, Width = 120 };
        txtPassword.Left = 20;
        txtPassword.Top = 185;
        txtPassword.Width = 640;
        txtPassword.PasswordChar = '*';

        btnConnect.Text = "Login & Connect";
        btnConnect.Left = 20;
        btnConnect.Top = 240;
        btnConnect.Width = 220;
        btnConnect.Height = 45;
        btnConnect.Click += BtnConnect_Click;

        btnDisconnect.Text = "Disconnect";
        btnDisconnect.Left = 260;
        btnDisconnect.Top = 240;
        btnDisconnect.Width = 180;
        btnDisconnect.Height = 45;
        btnDisconnect.Click += BtnDisconnect_Click;

        txtLog.Left = 20;
        txtLog.Top = 310;
        txtLog.Width = 640;
        txtLog.Height = 170;
        txtLog.Multiline = true;
        txtLog.ScrollBars = ScrollBars.Vertical;

        Controls.Add(lblApi);
        Controls.Add(txtApiUrl);
        Controls.Add(lblEmail);
        Controls.Add(txtEmail);
        Controls.Add(lblPassword);
        Controls.Add(txtPassword);
        Controls.Add(btnConnect);
        Controls.Add(btnDisconnect);
        Controls.Add(txtLog);
    }

    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        try
        {
            btnConnect.Enabled = false;

            Log("Login...");

            using var http = new HttpClient();

            var apiUrl = txtApiUrl.Text.Trim().TrimEnd('/');

            var response = await http.PostAsJsonAsync(
                $"{apiUrl}/api/auth/login",
                new
                {
                    email = txtEmail.Text.Trim(),
                    password = txtPassword.Text
                });

            var json = await response.Content.ReadAsStringAsync();
            Log(json);

            var result = JsonSerializer.Deserialize<LoginResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result == null || !result.Success)
            {
                MessageBox.Show(result?.Message ?? "Login failed");
                return;
            }

            Log("Downloading config...");

            var conf = await http.GetStringAsync(
                $"{apiUrl}/api/vpnpeers/{result.PeerId}/config-file");

            Directory.CreateDirectory(@"C:\ProgramData\VpnProduct");

            var confPath = @"C:\ProgramData\VpnProduct\wg0.conf";

            await File.WriteAllTextAsync(confPath, conf);

            Log("Config saved.");

            var wireGuardExe = @"C:\Program Files\WireGuard\wireguard.exe";

            if (!File.Exists(wireGuardExe))
            {
                MessageBox.Show("WireGuard not installed.");
                return;
            }

            Log("Removing old tunnel if exists...");
            RunAdmin("sc.exe", "stop WireGuardTunnel$wg0");
            RunAdmin("sc.exe", "delete WireGuardTunnel$wg0");

            await Task.Delay(1200);

            Log("Starting tunnel...");

            RunAdmin(
                wireGuardExe,
                $"/installtunnelservice \"{confPath}\"");

            Log("Connected.");
            MessageBox.Show("VPN Connected");
        }
        catch (Exception ex)
        {
            Log(ex.ToString());
            MessageBox.Show(ex.Message);
        }
        finally
        {
            btnConnect.Enabled = true;
        }
    }

    private async void BtnDisconnect_Click(object? sender, EventArgs e)
    {
        try
        {
            btnDisconnect.Enabled = false;

            Log("Stopping tunnel...");

            RunAdmin("sc.exe", "stop WireGuardTunnel$wg0");

            await Task.Delay(1200);

            Log("Deleting tunnel service...");

            RunAdmin("sc.exe", "delete WireGuardTunnel$wg0");

            Log("Disconnected.");
            MessageBox.Show("VPN Disconnected");
        }
        catch (Exception ex)
        {
            Log(ex.ToString());
            MessageBox.Show(ex.Message);
        }
        finally
        {
            btnDisconnect.Enabled = true;
        }
    }

    private static void RunAdmin(string fileName, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            Verb = "runas",
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        process?.WaitForExit();
    }

    private void Log(string text)
    {
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
    }

    private sealed class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string PeerId { get; set; } = "";
    }
}