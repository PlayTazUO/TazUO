using System.Collections.Generic;
using ClassicUO.Game;

namespace ClassicUO.Network.PacketHandlers.Helpers;

internal static class SharedStore
{
    private static readonly List<uint> _cliLocRequests = [];
    private static readonly HashSet<uint> _cliLocRequestsDedup = [];
    private static readonly List<uint> _customHouseRequests = [];
    private static readonly HashSet<uint> _customHouseRequestsDedup = [];

    public static uint RequestedGridLoot { get; set; }

    public static void AddMegaCliLocRequest(uint serial)
    {
        if (_cliLocRequestsDedup.Add(serial))
            _cliLocRequests.Add(serial);
    }

    public static void AddCustomHouseRequest(uint serial)
    {
        if (_customHouseRequestsDedup.Add(serial))
            _customHouseRequests.Add(serial);
    }

    public static void SendMegaCliLocRequests(World world)
    {
        if (world.ClientFeatures.TooltipsEnabled && _cliLocRequests.Count != 0)
        {
            if (Client.Game.UO.Version >= Utility.ClientVersion.CV_5090)
            {
                AsyncNetClient.Socket.Send_MegaClilocRequest(_cliLocRequests);

                // Rebuild dedup from what remains so those serials can be re-requested later
                _cliLocRequestsDedup.Clear();
                foreach (uint s in _cliLocRequests)
                    _cliLocRequestsDedup.Add(s);
            }
            else
            {
                foreach (uint serial in _cliLocRequests)
                    AsyncNetClient.Socket.Send_MegaClilocRequest_Old(serial);

                _cliLocRequests.Clear();
                _cliLocRequestsDedup.Clear();
            }
        }

        if (_customHouseRequests.Count > 0)
        {
            for (int i = 0; i < _customHouseRequests.Count; ++i)
                AsyncNetClient.Socket.Send_CustomHouseDataRequest(_customHouseRequests[i]);

            _customHouseRequests.Clear();
            _customHouseRequestsDedup.Clear();
        }
    }
}
