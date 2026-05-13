using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace VpnProduct.Desktop;

public class MainForm : Form
{
    private readonly TextBox apiUrlTextBox = new();
    private readonly TextBox emailTextBox = new();
    private readonly TextBox passwordTextBox = new();

    private readonly Button connectButton = new();
    private readonly Button disconnectButton = new();

    private readonly Label statusLabel = new();

    public MainForm()
    {
        Text = "VpnProduct Client";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 460);
        Size = new Size(760, 460);
        AutoScaleMode = AutoScaleMode.Dpi;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            ColumnCount = 2,
            RowCount = 6
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var apiLabel = new Label
        {
            Text = "API URL",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        apiUrlTextBox.Dock = DockStyle.Fill;
        apiUrlTextBox.Text = "http://61.70.3.87:5049";

        var emailLabel = new Label
        {
            Text = "Email",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        emailTextBox.Dock = DockStyle.Fill;

        var passwordLabel = new Label
        {
            Text = "Password",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        passwordTextBox.Dock = DockStyle.Fill;
        passwordTextBox.UseSystemPasswordChar = true;

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };

        connectButton.Text = "Login & Connect";
        connectButton.Width = 180;
        connectButton.Height = 44;

        disconnectButton.Text = "Disconnect";
        disconnectButton.Width = 180;
        disconnectButton.Height = 44;

        buttonPanel.Controls.Add(connectButton);
        buttonPanel.Controls.Add(disconnectButton);

        statusLabel.Text = "Status: Idle";
        statusLabel.Dock = DockStyle.Fill;
        statusLabel.AutoSize = false;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;

        root.Controls.Add(apiLabel, 0, 0);
        root.Controls.Add(apiUrlTextBox, 1, 0);

        root.Controls.Add(emailLabel, 0, 1);
        root.Controls.Add(emailTextBox, 1, 1);

        root.Controls.Add(passwordLabel, 0, 2);
        root.Controls.Add(passwordTextBox, 1, 2);

        root.Controls.Add(buttonPanel, 1, 3);

        root.Controls.Add(statusLabel, 0, 4);
        root.SetColumnSpan(statusLabel, 2);

        Controls.Add(root);

        connectButton.Click += ConnectButton_Click;
        disconnectButton.Click += DisconnectButton_Click;
    }

    private async void ConnectButton_Click(object? sender, EventArgs e)
    {
        try
        {
            var apiUrl = apiUrlTextBox.Text.Trim().TrimEnd('/');
            var email = emailTextBox.Text.Trim();
            var password = passwordTextBox.Text;

            if (string.IsNullOrWhiteSpace(apiUrl) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                statusLabel.Text = "Status: API URL, Email, Password are required.";
                return;
            }

            statusLabel.Text = "Status: Logging in...";

            using var http = new HttpClient();

            var loginBody = JsonSerializer.Serialize(new
            {
                email,
                password
            });

            var loginResponse = await http.PostAsync(
                $"{apiUrl}/api/auth/login",
                new StringContent(loginBody, Encoding.UTF8, "application/json"));

            var loginJson = await loginResponse.Content.ReadAsStringAsync();

            if (!loginResponse.IsSuccessStatusCode)
            {
                statusLabel.Text = $"Status: Login HTTP error {loginResponse.StatusCode}";
                return;
            }

            var login = JsonSerializer.Deserialize<LoginResult>(
                loginJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (login == null || !login.Success)
            {
                statusLabel.Text = $"Status: Login failed. {login?.Message}";
                return;
            }

            if (string.IsNullOrWhiteSpace(login.PeerId))
            {
                statusLabel.Text = "Status: Login OK, but no VPN peer assigned.";
                return;
            }

            statusLabel.Text = "Status: Downloading config...";

            var configUrl = $"{apiUrl}/api/vpnpeers/{login.PeerId}/config-file";
            var config = await http.GetStringAsync(configUrl);

            Directory.CreateDirectory(@"C:\ProgramData\VpnProduct");

            var confPath = @"C:\ProgramData\VpnProduct\wg0.conf";
            await File.WriteAllTextAsync(confPath, config);

            statusLabel.Text = "Status: Starting tunnel...";

            var wgExe = @"C:\Program Files\WireGuard\wireguard.exe";

            if (!File.Exists(wgExe))
            {
                statusLabel.Text = "Status: WireGuard not installed.";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = wgExe,
                Arguments = $"/installtunnelservice \"{confPath}\"",
                Verb = "runas",
                UseShellExecute = true
            });

            statusLabel.Text = "Status: Connected";
        }
        catch (Exception ex)
        {
            statusLabel.Text = "Status: " + ex.Message;
        }
    }

    private void DisconnectButton_Click(object? sender, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = "stop WireGuardTunnel$wg0",
                Verb = "runas",
                UseShellExecute = true
            });

            statusLabel.Text = "Status: Disconnected";
        }
        catch (Exception ex)
        {
            statusLabel.Text = "Status: " + ex.Message;
        }
    }

    private sealed class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string PeerId { get; set; } = string.Empty;
    }
}