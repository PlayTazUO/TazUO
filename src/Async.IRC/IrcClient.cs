using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Async.IRC;

public sealed class IrcClient : IAsyncDisposable
{
    private TcpClient? _tcpClient;
    private StreamWriter? _writer;
    private StreamReader? _reader;
    private CancellationTokenSource? _readLoopCts;
    private Task? _readLoopTask;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private string _nickname = "User";
    private volatile bool _connected;

    // ── Events ──────────────────────────────────────────────────────────────

    /// <summary>Fired when a direct (private) message is received addressed to our nick.</summary>
    public event EventHandler<IrcMessageEventArgs>? PrivateMessageReceived;

    /// <summary>Fired when a message is received in a channel.</summary>
    public event EventHandler<IrcMessageEventArgs>? ChannelMessageReceived;

    /// <summary>Fired when any user (including ourselves) joins a channel.</summary>
    public event EventHandler<IrcChannelJoinedEventArgs>? ChannelJoined;

    /// <summary>Fired when any user parts (leaves) a channel.</summary>
    public event EventHandler<IrcChannelPartedEventArgs>? ChannelParted;

    /// <summary>Fired when any user quits the server.</summary>
    public event EventHandler<IrcUserQuitEventArgs>? UserQuit;

    /// <summary>Fired for each batch of names received (353 RPL_NAMREPLY).</summary>
    public event EventHandler<IrcNamesEventArgs>? NamesReceived;

    /// <summary>Fired once the server sends RPL_WELCOME (001), indicating a successful login.</summary>
    public event EventHandler? Connected;

    /// <summary>Fired when the connection is lost or <see cref="DisconnectAsync"/> completes.</summary>
    public event EventHandler? Disconnected;

    /// <summary>Fired when the connection attempt fails before the read loop starts.</summary>
    public event EventHandler<IrcConnectionFailedEventArgs>? ConnectionFailed;

    /// <summary>Fired for every raw line sent or received. Useful for logging and debugging.</summary>
    public event EventHandler<IrcRawLineEventArgs>? RawLineReceived;

    // ── Properties ──────────────────────────────────────────────────────────

    public bool IsConnected => _connected;
    public string Nickname => _nickname;

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Connects to an IRC server and begins the registration handshake.
    /// Pass <paramref name="useSsl"/>=true (or use port 6697) for TLS connections.
    /// </summary>
    public async Task ConnectAsync(string host, int port, string nickname = "User", bool useSsl = false, CancellationToken ct = default)
    {
        if (_connected)
            await DisconnectAsync();

        _nickname = nickname;

        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(host, port, ct);

            Stream stream = _tcpClient.GetStream();

            if (useSsl)
            {
                var sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
                await sslStream.AuthenticateAsClientAsync(host);
                stream = sslStream;
            }

            _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\r\n" };
            _reader = new StreamReader(stream, Encoding.UTF8);

            _readLoopCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _readLoopTask = ReadLoopAsync(_readLoopCts.Token);

            await SendRawAsync($"NICK {_nickname}");
            await SendRawAsync($"USER {_nickname} 0 * :{_nickname}");
        }
        catch (Exception ex)
        {
            CleanupResources();
            ConnectionFailed?.Invoke(this, new IrcConnectionFailedEventArgs(ex));
        }
    }

    /// <summary>
    /// Sets the client's nickname. If already connected, sends a NICK command to the server.
    /// </summary>
    public async Task SetNicknameAsync(string nickname)
    {
        _nickname = nickname;
        if (_connected)
            await SendRawAsync($"NICK {nickname}");
    }

    /// <summary>Joins an IRC channel. A leading '#' is added automatically if omitted.</summary>
    public Task JoinChannelAsync(string channel)
    {
        if (!channel.StartsWith('#') && !channel.StartsWith('&'))
            channel = "#" + channel;
        return SendRawAsync($"JOIN {channel}");
    }

    public Task LeaveChannelAsync(string channel)
    {
        if (!channel.StartsWith('#') && !channel.StartsWith('&'))
            channel = "#" + channel;
        return SendRawAsync($"PART {channel}");
    }

    /// <summary>Sends a PRIVMSG to a channel or user.</summary>
    public Task SendMessageAsync(string target, string message)
        => SendRawAsync($"PRIVMSG {target} :{message}");

    /// <summary>Sends QUIT and closes the connection.</summary>
    public async Task DisconnectAsync()
    {
        if (!_connected && _tcpClient == null)
            return;

        try
        {
            if (_connected)
                await SendRawAsync("QUIT :Goodbye");
        }
        catch { /* stream may already be closed */ }

        _readLoopCts?.Cancel();
        try { if (_readLoopTask != null) await _readLoopTask; } catch { }

        bool wasConnected = _connected;
        CleanupResources();

        if (wasConnected)
            Disconnected?.Invoke(this, EventArgs.Empty);
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _writeLock.Dispose();
    }

    // ── Internal ────────────────────────────────────────────────────────────

    private void CleanupResources()
    {
        _writer?.Dispose();
        _reader?.Dispose();
        _tcpClient?.Dispose();
        _writer = null;
        _reader = null;
        _tcpClient = null;
        _readLoopCts?.Dispose();
        _readLoopCts = null;
        _readLoopTask = null;
        _connected = false;
    }

    private async Task SendRawAsync(string line)
    {
        if (_writer == null) return;

        await _writeLock.WaitAsync();
        try
        {
            RawLineReceived?.Invoke(this, new IrcRawLineEventArgs(line, false));
            await _writer.WriteLineAsync(line);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _reader != null)
            {
                string? line = await _reader.ReadLineAsync(ct);
                if (line == null) break; // server closed the connection

                RawLineReceived?.Invoke(this, new IrcRawLineEventArgs(line, true));
                HandleLine(line);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { /* network drop */ }
        finally
        {
            if (_connected)
            {
                _connected = false;
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void HandleLine(string line)
    {
        var msg = IrcMessage.Parse(line);

        switch (msg.Command)
        {
            case "PING":
            {
                string token = msg.Trailing ?? (msg.Params.Length > 0 ? msg.Params[0] : "");
                _ = SendRawAsync($"PONG :{token}");
                break;
            }

            case "001": // RPL_WELCOME — registration complete
                _connected = true;
                Connected?.Invoke(this, EventArgs.Empty);
                break;

            case "JOIN":
            {
                string nick = msg.NickFromPrefix() ?? msg.Prefix ?? string.Empty;
                string channel = msg.Params.Length > 0 ? msg.Params[0] : msg.Trailing ?? string.Empty;
                ChannelJoined?.Invoke(this, new IrcChannelJoinedEventArgs(channel, nick));
                break;
            }

            case "PART":
            {
                string nick = msg.NickFromPrefix() ?? msg.Prefix ?? string.Empty;
                string channel = msg.Params.Length > 0 ? msg.Params[0] : string.Empty;
                string reason = msg.Trailing ?? string.Empty;
                ChannelParted?.Invoke(this, new IrcChannelPartedEventArgs(channel, nick, reason));
                break;
            }

            case "QUIT":
            {
                string nick = msg.NickFromPrefix() ?? msg.Prefix ?? string.Empty;
                string reason = msg.Trailing ?? string.Empty;
                UserQuit?.Invoke(this, new IrcUserQuitEventArgs(nick, reason));
                break;
            }

            case "353": // RPL_NAMREPLY — batch of nicks for a channel
            {
                // params: [me, channel-type(=/@/*), #channel]
                string channel = msg.Params.Length >= 3 ? msg.Params[2] : (msg.Params.Length >= 2 ? msg.Params[1] : string.Empty);
                string[] nicks = (msg.Trailing ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                NamesReceived?.Invoke(this, new IrcNamesEventArgs(channel, nicks));
                break;
            }

            case "PRIVMSG":
            {
                string nick = msg.NickFromPrefix() ?? msg.Prefix ?? string.Empty;
                string target = msg.Params.Length > 0 ? msg.Params[0] : string.Empty;
                string message = msg.Trailing ?? string.Empty;
                var args = new IrcMessageEventArgs(nick, target, message);

                if (target.StartsWith('#') || target.StartsWith('&'))
                    ChannelMessageReceived?.Invoke(this, args);
                else
                    PrivateMessageReceived?.Invoke(this, args);
                break;
            }

            case "433" or "432" or "431" or "436": // ERR_NICKNAMEINUSE
            {
                _nickname += Random.Shared.Next(9);
                _ = SendRawAsync($"NICK {_nickname}");
                break;
            }
        }
    }
}
