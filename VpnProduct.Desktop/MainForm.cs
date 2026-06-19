using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace VpnProduct.Desktop;

public class MainForm : Form
{
    private const string AppVersion = "3.0.0";
    private const string ApiBaseUrl = "https://yct.myftp.org";

    private const string Udp2RawServer = "61.70.3.87:80";
    private const string Udp2RawLocal = "127.0.0.1:51820";
    private const string Udp2RawKey = "VpnProduct2026";

    private readonly TextBox txtEmail = new();
    private readonly TextBox txtPassword = new();
    private readonly Button btnConnect = new();
    private readonly Button btnDisconnect = new();
    private readonly TextBox txtLog = new();
    private readonly Label lblServer = new();
    private readonly Label lblStatus = new();

    public MainForm()
    {
        Text = $"VpnProduct Client v{AppVersion}";
        Width = 720;
        Height = 560;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft JhengHei UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        lblServer.Text = $"Server: {ApiBaseUrl} / VPN TCP 80";
        lblServer.Left = 20;
        lblServer.Top = 20;
        lblServer.Width = 640;
        lblServer.Height = 28;

        var lblEmail = new Label
        {
            Text = "Email",
            Left = 20,
            Top = 65,
            Width = 120
        };

        txtEmail.Left = 20;
        txtEmail.Top = 90;
        txtEmail.Width = 640;

        var lblPassword = new Label
        {
            Text = "Password",
            Left = 20,
            Top = 135,
            Width = 120
        };

        txtPassword.Left = 20;
        txtPassword.Top = 160;
        txtPassword.Width = 640;
        txtPassword.PasswordChar = '*';

        btnConnect.Text = "Connect";
        btnConnect.Left = 20;
        btnConnect.Top = 215;
        btnConnect.Width = 180;
        btnConnect.Height = 45;
        btnConnect.Click += BtnConnect_Click;

        btnDisconnect.Text = "Disconnect";
        btnDisconnect.Left = 220;
        btnDisconnect.Top = 215;
        btnDisconnect.Width = 180;
        btnDisconnect.Height = 45;
        btnDisconnect.Click += BtnDisconnect_Click;

        lblStatus.Text = "Status: Ready";
        lblStatus.Left = 20;
        lblStatus.Top = 280;
        lblStatus.Width = 640;
        lblStatus.Height = 28;

        txtLog.Left = 20;
        txtLog.Top = 320;
        txtLog.Width = 640;
        txtLog.Height = 170;
        txtLog.Multiline = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.ReadOnly = true;

        Controls.Add(lblServer);
        Controls.Add(lblEmail);
        Controls.Add(txtEmail);
        Controls.Add(lblPassword);
        Controls.Add(txtPassword);
        Controls.Add(btnConnect);
        Controls.Add(btnDisconnect);
        Controls.Add(lblStatus);
        Controls.Add(txtLog);

        Log($"VpnProduct Client v{AppVersion}");
        Log($"API Server: {ApiBaseUrl}");
        Log($"VPN Mode: TCP 80 via udp2raw");
    }

    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        try
        {
            btnConnect.Enabled = false;
            SetStatus("Logging in...");

            var email = txtEmail.Text.Trim();
            var password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Please enter Email.");
                SetStatus("Ready");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter Password.");
                SetStatus("Ready");
                return;
            }

            using var http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            Log("Login...");

            var response = await http.PostAsJsonAsync(
                $"{ApiBaseUrl}/api/auth/login",
                new
                {
                    email,
                    password
                });

            var json = await response.Content.ReadAsStringAsync();
            Log(json);

            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show($"Login HTTP error: {response.StatusCode}");
                SetStatus("Login failed");
                return;
            }

            if (json.TrimStart().StartsWith("<"))
            {
                MessageBox.Show("Server returned HTML instead of JSON. Please check API URL / Nginx proxy.");
                SetStatus("Server error");
                return;
            }

            var result = JsonSerializer.Deserialize<LoginResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result == null)
            {
                MessageBox.Show("Login response parse failed.");
                SetStatus("Login failed");
                return;
            }

            if (!result.Success)
            {
                MessageBox.Show(result.Message);

                if (result.Message.Contains("expired", StringComparison.OrdinalIgnoreCase))
                {
                    SetStatus("Subscription expired");
                }
                else if (result.Message.Contains("confirm", StringComparison.OrdinalIgnoreCase))
                {
                    SetStatus("Email not confirmed");
                }
                else
                {
                    SetStatus("Login failed");
                }

                return;
            }

            SetStatus("Downloading config...");
            Log("Downloading config...");

            var conf = await http.GetStringAsync(
                $"{ApiBaseUrl}/api/vpnpeers/{result.PeerId}/config-file");

            conf = RewriteEndpointToLocalUdp2Raw(conf);

            Directory.CreateDirectory(@"C:\ProgramData\VpnProduct");

            var confPath = @"C:\ProgramData\VpnProduct\wg0.conf";

            await File.WriteAllTextAsync(confPath, conf);

            Log("Config saved:");
            Log(confPath);

            EnsureWireGuardInstalled();

            SetStatus("Stopping old tunnel...");
            Log("Removing old tunnel if exists...");

            RunAdmin("sc.exe", "stop WireGuardTunnel$wg0", ignoreError: true);
            RunAdmin("sc.exe", "delete WireGuardTunnel$wg0", ignoreError: true);

            await Task.Delay(1200);

            StopUdp2Raw();

            await Task.Delay(800);

            SetStatus("Starting TCP tunnel...");
            StartUdp2Raw();

            await Task.Delay(2500);

            SetStatus("Starting WireGuard...");
            Log("Starting WireGuard tunnel...");

            var wireGuardExe = GetWireGuardExePath();

            RunAdmin(
                wireGuardExe,
                $"/installtunnelservice \"{confPath}\"");

            SetStatus("Connected");
            Log("Connected via TCP 80.");

            MessageBox.Show("VPN Connected");
        }
        catch (Exception ex)
        {
            Log(ex.ToString());
            MessageBox.Show(ex.Message);
            SetStatus("Error");
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

            SetStatus("Stopping WireGuard...");
            Log("Stopping tunnel...");

            RunAdmin("sc.exe", "stop WireGuardTunnel$wg0", ignoreError: true);

            await Task.Delay(1200);

            SetStatus("Deleting tunnel service...");
            Log("Deleting tunnel service...");

            RunAdmin("sc.exe", "delete WireGuardTunnel$wg0", ignoreError: true);

            await Task.Delay(800);

            SetStatus("Stopping TCP tunnel...");
            StopUdp2Raw();

            SetStatus("Disconnected");
            Log("Disconnected.");

            MessageBox.Show("VPN Disconnected");
        }
        catch (Exception ex)
        {
            Log(ex.ToString());
            MessageBox.Show(ex.Message);
            SetStatus("Error");
        }
        finally
        {
            btnDisconnect.Enabled = true;
        }
    }

    private static string RewriteEndpointToLocalUdp2Raw(string conf)
    {
        return Regex.Replace(
            conf,
            @"Endpoint\s*=\s*[^\r\n]+",
            "Endpoint = 127.0.0.1:51820",
            RegexOptions.IgnoreCase);
    }

    private static string GetWireGuardExePath()
    {
        var path = @"C:\Program Files\WireGuard\wireguard.exe";

        if (File.Exists(path))
        {
            return path;
        }

        var x86Path = @"C:\Program Files (x86)\WireGuard\wireguard.exe";

        if (File.Exists(x86Path))
        {
            return x86Path;
        }

        return path;
    }

    private void EnsureWireGuardInstalled()
    {
        var wireGuardExe = GetWireGuardExePath();

        if (File.Exists(wireGuardExe))
        {
            Log("WireGuard detected.");
            return;
        }

        var msiPath = Path.Combine(
            AppContext.BaseDirectory,
            "wireguard-amd64-1.1.msi");

        if (!File.Exists(msiPath))
        {
            MessageBox.Show(
                "WireGuard is not installed and installer file was not found.\n\n" +
                "Expected:\n" + msiPath);

            throw new FileNotFoundException("WireGuard installer not found.", msiPath);
        }

        SetStatus("Installing VPN components...");
        Log("WireGuard not found. Installing...");

        RunAdmin(
            "msiexec.exe",
            $"/i \"{msiPath}\" /quiet /norestart");

        Task.Delay(2000).Wait();

        RunAdmin(
            "taskkill.exe",
            "/IM wireguard.exe /F",
            ignoreError: true);

        wireGuardExe = GetWireGuardExePath();

        if (!File.Exists(wireGuardExe))
        {
            throw new FileNotFoundException(
                "WireGuard installation completed but wireguard.exe was not found.",
                wireGuardExe);
        }

        Log("WireGuard installed.");
    }

    private static string GetUdp2RawPath()
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            "udp2raw.exe");
    }

    private void StartUdp2Raw()
    {
        var udp2raw = GetUdp2RawPath();

        if (!File.Exists(udp2raw))
        {
            throw new FileNotFoundException(
                "udp2raw.exe not found.",
                udp2raw);
        }

        Log("Starting udp2raw...");
        Log($"udp2raw server: {Udp2RawServer}");

        var args =
            $"-c -l\"{Udp2RawLocal}\" -r\"{Udp2RawServer}\" -k\"{Udp2RawKey}\" --raw-mode faketcp";

        Process.Start(new ProcessStartInfo
        {
            FileName = udp2raw,
            Arguments = args,
            Verb = "runas",
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        Log("udp2raw started.");
    }

    private void StopUdp2Raw()
    {
        Log("Stopping udp2raw...");

        RunAdmin(
            "taskkill.exe",
            "/IM udp2raw.exe /F",
            ignoreError: true);

        Log("udp2raw stopped.");
    }

    private static void RunAdmin(
        string fileName,
        string arguments,
        bool ignoreError = false)
    {
        try
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
        catch
        {
            if (!ignoreError)
            {
                throw;
            }
        }
    }

    private void SetStatus(string status)
    {
        lblStatus.Text = $"Status: {status}";
        Log($"Status: {status}");
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