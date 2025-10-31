using System;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.Network.Encryption;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network
{
    public class LoginHandshake : IDisposable
    {
        /// <summary>
        /// LoginHandshake supports only one connection. Trying to connect multiple times in one client would need a rework.
        /// </summary>
        public static LoginHandshake Instance {
            get
            {
                if (field == null)
                    field = new();

                return field;
            }

            private set;
        }

        private ushort _retries;
        private int _reconnectTryCounter = 1;
        private long _reconnectTime;
        private bool _isDisposed;
        private uint _pingTime;

        private LoginHandshake() { }

        public LoginSteps CurrentLoginStep { get; private set; } = LoginSteps.Main;
        public ServerListEntry[] Servers { get; private set; }
        public CityInfo[] Cities { get; set; }
        public string[] Characters { get; private set; }
        public byte ServerIndex { get; private set; }
        public static string Account { get; private set; }
        private static string Password { get; set; }
        public static string IP { get; set; }
        public static ushort Port { get; set; }
        public (int min, int max) LoginDelay { get; private set; }
        public bool ShouldReconnect { get; set; }
        public static bool Reconnect { get; set; }
        public ushort LastServerNum { get; private set; }
        public string LastServerName { get; private set; }
        public byte ErrorPacket { get; private set; } = byte.MaxValue;
        public byte ErrorCode { get; private set; } = byte.MaxValue;
        public string ErrorMessage { get; private set; } = string.Empty;
        public uint CharacterListFlags { get; private set; }

        public event EventHandler<LoginSteps> LoginStepChanged;
        public event EventHandler<SocketError> ConnectionFailed;
        public event EventHandler<string> ErrorOccurred;

        public void Connect(string account, string password, string ip, ushort port)
        {
            if (CurrentLoginStep == LoginSteps.Connecting)
            {
                return;
            }

            Account = account;
            Password = password;
            IP = ip;
            Port = port;

            Log.TraceDebug($"[HandShake] Start login to: {IP},{Port}");

            if (!Reconnect)
            {
                SetLoginStep(LoginSteps.Connecting);
            }

            AsyncNetClient.Socket.Connected -= OnNetClientConnected;
            AsyncNetClient.Socket.Disconnected -= OnNetClientDisconnected;
            AsyncNetClient.Socket?.Disconnect();
            AsyncNetClient.Socket = new AsyncNetClient();
            AsyncNetClient.Socket.Connected += OnNetClientConnected;
            AsyncNetClient.Socket.Disconnected += OnNetClientDisconnected;
            System.Threading.Tasks.Task<bool> status = AsyncNetClient.Socket.Connect(ip, port);
        }

        public void Disconnect()
        {
            Log.TraceDebug("[HandShake] Disconnecting...");
            AsyncNetClient.Socket.Connected -= OnNetClientConnected;
            AsyncNetClient.Socket.Disconnected -= OnNetClientDisconnected;
            AsyncNetClient.Socket?.Disconnect();
        }

        private void SetError(byte packet = byte.MaxValue, byte code = byte.MaxValue, string msg = "")
        {
            ErrorPacket = packet;
            ErrorCode = code;
            ErrorMessage = msg;
        }

        public void SelectServer(byte index, string serverName)
        {
            Log.TraceDebug($"[HandShake] Selecting server {serverName}.");
            if (CurrentLoginStep == LoginSteps.ServerSelection)
            {
                for (byte i = 0; i < Servers.Length; i++)
                {
                    if (Servers[i].Index == index)
                    {
                        ServerIndex = i;
                        break;
                    }
                }

                LastServerNum = (ushort)(1 + ServerIndex);
                LastServerName = Servers[ServerIndex].Name;

                SetLoginStep(LoginSteps.LoginInToServer);

                AsyncNetClient.Socket.Send_SelectServer(index);
            }
        }

        /// <summary>
        /// Call in Update() of login scene
        /// </summary>
        /// <param name="reconnectTime">In ms</param>
        public void HandleReconnect(int reconnectTime)
        {
            if (Reconnect && (CurrentLoginStep == LoginSteps.PopUpMessage || CurrentLoginStep == LoginSteps.Main) && !AsyncNetClient.Socket.IsConnected)
            {
                if (_reconnectTime >= Time.Ticks)
                    return;

                Log.TraceDebug($"[HandShake] Reconnecting...");
                if (!string.IsNullOrEmpty(Account))
                {
                    Connect(Account, Password, IP, Port);
                }
                else
                {
                    Reconnect = false;
                }

                if (reconnectTime < 1000)
                {
                    reconnectTime = 1000;
                }

                _reconnectTime = (long)Time.Ticks + reconnectTime;
                _reconnectTryCounter++;
            }
        }

        public void ServerListReceived(ref StackDataReader p)
        {
            Log.TraceDebug($"[HandShake] Got server list.");
            byte flags = p.ReadUInt8();
            ushort count = p.ReadUInt16BE();
            DisposeAllServerEntries();
            Servers = new ServerListEntry[count];

            for (ushort i = 0; i < count; i++)
            {
                Servers[i] = ServerListEntry.Create(ref p);
            }

            SetLoginStep(LoginSteps.ServerSelection);
        }

        public event NotifierEventHandler ReceiveCharacterListNotifier;
        public void ReceiveCharacterList(ref StackDataReader p)
        {
            Log.TraceDebug($"[HandShake] Got character list.");
            ParseCharacterList(ref p);
            ParseCities(ref p);
            CharacterListFlags = p.ReadUInt32BE();

            SetLoginStep(LoginSteps.CharacterSelection);
            ReceiveCharacterListNotifier?.Invoke();
        }

        public event NotifierEventHandler UpdateCharacterListNotifier;
        public void UpdateCharacterList(ref StackDataReader p)
        {
            Log.TraceDebug($"[HandShake] Updated character list.");
            ParseCharacterList(ref p);

            if (CurrentLoginStep != LoginSteps.PopUpMessage)
            {
                SetError();
            }

            SetLoginStep(LoginSteps.CharacterSelection);
            UpdateCharacterListNotifier?.Invoke();
        }

        public void HandleErrorCode(ref StackDataReader p)
        {
            byte code = p.ReadUInt8();
            SetError(p[0], code);
            SetLoginStep(LoginSteps.PopUpMessage);
            LoginDelay = default;
        }

        public void HandleLoginDelayPacket(ref StackDataReader p)
        {
            byte delay = p.ReadUInt8();
            LoginDelay = ((delay - 1) * 10, delay * 10);
        }

        public void HandleRelayServerPacket(ref StackDataReader p, bool ignoreRelay)
        {
            Log.TraceDebug($"[HandShake] Got server relay packet.");
            uint ip = p.ReadUInt32LE(); // use LittleEndian here
            ushort port = p.ReadUInt16BE();
            uint seed = p.ReadUInt32BE();

            byte[] ipBytes = new byte[]
            {
                    (byte)(ip & 0xFF),
                    (byte)((ip >> 8) & 0xFF),
                    (byte)((ip >> 16) & 0xFF),
                    (byte)((ip >> 24) & 0xFF)
                    };

            string finalIP = new IPAddress(ipBytes).ToString();

            if (ignoreRelay || ip == 0)
            {
                Log.TraceDebug("Ignoring relay server packet IP address");
                finalIP = IP;
                port = Port;
            }

            AfterRelayConnect(finalIP, port, seed);
        }

        public int GetServerIndexByName(string name)
        {
            if (!string.IsNullOrWhiteSpace(name) && Servers != null)
            {
                for (int i = 0; i < Servers.Length; i++)
                {
                    if (Servers[i].Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public CityInfo GetCity(int index)
        {
            if (Cities != null && index < Cities.Length)
            {
                return Cities[index];
            }

            return null;
        }

        public void SendSelectCharacter(uint index)
        {
            if (Characters != null && index < (uint)Characters.Length)
            {
                AsyncNetClient.Socket.Send_SelectCharacter(index, Characters[index], AsyncNetClient.Socket.LocalIP);
                SetLoginStep(LoginSteps.EnteringBritania);
            }
            else
            {
                Log.Warn($"[HandShake] Invalid character index {index}.");
            }
        }

        public void SendDeleteCharacter(uint index)
        {
            if (CurrentLoginStep == LoginSteps.CharacterSelection)
            {
                AsyncNetClient.Socket.Send_DeleteCharacter((byte)index, AsyncNetClient.Socket.LocalIP);
            }
        }

        public void SendPing()
        {
            if ((CurrentLoginStep == LoginSteps.CharacterCreation || CurrentLoginStep == LoginSteps.CharacterSelection) && Time.Ticks > _pingTime)
            {
                // Note that this will not be an ICMP ping, so it's better that this *not* be affected by -no_server_ping.

                if (AsyncNetClient.Socket.IsConnected)
                {
                    AsyncNetClient.Socket.Statistics.SendPing();
                }

                _pingTime = Time.Ticks + 60000;
            }
        }

        internal void SetLoginStep(LoginSteps step)
        {
            _pingTime = Time.Ticks + 60000;
            Log.TraceDebug($"[HandShake] Set login step to {step}.");
            CurrentLoginStep = step;
            LoginStepChanged?.Invoke(this, step);
        }

        private void OnNetClientConnected(object sender, EventArgs e)
        {
            Log.Info("[HandShake] Connected!");
            SetLoginStep(LoginSteps.VerifyingAccount);

            uint address = AsyncNetClient.Socket.LocalIP;

            AsyncNetClient.Encryption?.Initialize(true, address);

            if (Client.Game.UO.Version >= ClientVersion.CV_6040)
            {
                uint clientVersion = (uint)Client.Game.UO.Version;

                byte major = (byte)(clientVersion >> 24);
                byte minor = (byte)(clientVersion >> 16);
                byte build = (byte)(clientVersion >> 8);
                byte extra = (byte)clientVersion;

                AsyncNetClient.Socket.Send_Seed(address, major, minor, build, extra);
            }
            else
            {
                AsyncNetClient.Socket.Send_Seed_Old(address);
            }

            AsyncNetClient.Socket.Send_FirstLogin(Account, Password);
        }

        private void OnNetClientDisconnected(object sender, SocketError e)
        {
            Log.Warn("[HandShake] Disconnected");

            if (CurrentLoginStep == LoginSteps.CharacterCreation)
            {
                return;
            }

            if (e == SocketError.Success)
            {
                return;
            }

            Characters = null;
            DisposeAllServerEntries();

            if (ShouldReconnect)
            {
                Reconnect = true;
                SetError(msg: string.Format(
                                             ResGeneral.ReconnectPleaseWait01,
                                             _reconnectTryCounter,
                                             StringHelper.AddSpaceBeforeCapital(e.ToString())
                                         ));
            }
            else
            {
                SetError(msg: string.Format(
                                                  ResGeneral.ConnectionLost0,
                                                  StringHelper.AddSpaceBeforeCapital(e.ToString())
                                              ));
            }

            SetLoginStep(LoginSteps.PopUpMessage);
            ConnectionFailed?.Invoke(this, e);
        }

        private void AfterRelayConnect(string ip, ushort port, uint seed)
        {
            AsyncNetClient.Socket.Connected -= OnNetClientConnected;
            AsyncNetClient.Socket.Disconnected -= OnNetClientDisconnected;
            AsyncNetClient.Socket.Disconnect().Wait();
            AsyncNetClient.Socket = new AsyncNetClient();

            _retries++;
            Log.TraceDebug($"[HandShake] Reconnecting to relay server...");
            AsyncNetClient.Socket.Connect(ip, port).Wait(3000);

            if (AsyncNetClient.Socket.IsConnected)
            {
                EncryptionHelper.Instance?.Initialize(false, seed);
                AsyncNetClient.Socket.EnableCompression();
                unsafe
                {
                    Span<byte> b = stackalloc byte[4]
                    {
                        (byte)(seed >> 24),
                        (byte)(seed >> 16),
                        (byte)(seed >> 8),
                        (byte)seed
                    };
                    AsyncNetClient.Socket.Send(b, true, true);
                }

                AsyncNetClient.Socket.Send_SecondLogin(Account, Password, seed);
                Log.TraceDebug($"[HandShake] Sent second login.");
            }
            else
            {
                Log.TraceDebug($"[HandShake] Failed to connect, trying again.");
                if (_retries > 5)
                {
                    _retries = 0;
                    SetError(msg: "Failed to connect to game server after multiple attempts.");
                    SetLoginStep(LoginSteps.PopUpMessage);
                    ErrorOccurred?.Invoke(this, ErrorMessage);
                    return;
                }

                AfterRelayConnect(ip, port, seed);
            }
        }

        private void ParseCharacterList(ref StackDataReader p)
        {
            int count = p.ReadUInt8();
            Characters = new string[count];

            for (ushort i = 0; i < count; i++)
            {
                Characters[i] = p.ReadASCII(30).TrimEnd('\0');
                p.Skip(30);
            }
        }

        private void ParseCities(ref StackDataReader p)
        {
            byte count = p.ReadUInt8();
            Cities = new CityInfo[count];

            bool isNew = Client.Game.UO.Version >= ClientVersion.CV_70130;

            Vector2[] oldtowns =
            {
                new(105, 130), new(245, 90),
                new(165, 200), new(395, 160),
                new(200, 305), new(335, 250),
                new(160, 395), new(100, 250),
                new(270, 130), new(0xFFFF, 0xFFFF)
            };

            for (int i = 0; i < count; i++)
            {
                CityInfo cityInfo;

                if (isNew)
                {
                    byte cityIndex = p.ReadUInt8();
                    string cityName = p.ReadASCII(32);
                    string cityBuilding = p.ReadASCII(32);
                    ushort cityX = (ushort)p.ReadUInt32BE();
                    ushort cityY = (ushort)p.ReadUInt32BE();
                    sbyte cityZ = (sbyte)p.ReadUInt32BE();
                    uint cityMapIndex = p.ReadUInt32BE();
                    uint cityDescription = p.ReadUInt32BE();
                    p.Skip(4);

                    cityInfo = new CityInfo
                    (
                        cityIndex,
                        cityName,
                        cityBuilding,
                        Client.Game.UO.FileManager.Clilocs.GetString((int)cityDescription),
                        cityX,
                        cityY,
                        cityZ,
                        cityMapIndex,
                        isNew
                    );
                }
                else
                {
                    byte cityIndex = p.ReadUInt8();
                    string cityName = p.ReadASCII(31);
                    string cityBuilding = p.ReadASCII(31);

                    cityInfo = new CityInfo
                    (
                        cityIndex,
                        cityName,
                        cityBuilding,
                        string.Empty,
                        (ushort)oldtowns[i % oldtowns.Length].X,
                        (ushort)oldtowns[i % oldtowns.Length].Y,
                        0,
                        0,
                        isNew
                    );
                }

                Cities[i] = cityInfo;
            }
        }

        private void DisposeAllServerEntries()
        {
            if (Servers != null)
            {
                for (int i = 0; i < Servers.Length; i++)
                {
                    if (Servers[i] != null)
                    {
                        Servers[i].Dispose();
                        Servers[i] = null;
                    }
                }

                Servers = null;
            }
        }

        public void Dispose()
        {
            Instance = null;

            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            AsyncNetClient.Socket.Disconnected -= OnNetClientDisconnected;
            AsyncNetClient.Socket.Connected -= OnNetClientConnected;

            DisposeAllServerEntries();
            Characters = null;
            Cities = null;
        }
    }

    public delegate void NotifierEventHandler();

    public class CityInfo
    {
        public CityInfo
        (
            int index,
            string city,
            string building,
            string description,
            ushort x,
            ushort y,
            sbyte z,
            uint map,
            bool isNew
        )
        {
            Index = index;
            City = city;
            Building = building;
            Description = description;
            X = x;
            Y = y;
            Z = z;
            Map = map;
            IsNewCity = isNew;
        }

        public readonly string Building;
        public readonly string City;
        public readonly string Description;
        public readonly int Index;
        public readonly bool IsNewCity;
        public readonly uint Map;
        public readonly ushort X, Y;
        public readonly sbyte Z;
    }
}
