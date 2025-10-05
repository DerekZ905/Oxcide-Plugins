using Oxide.Core.Libraries.Covalence;
using Oxide.Core;
using Newtonsoft.Json;
using System;

namespace Oxide.Plugins
{
    [Info("IPJoinNotifier", "DerekZ905", "1.2.0")]
    [Description("Broadcasts join messages with IP and location info to admins only.")]

    public class IPJoinNotifier : CovalencePlugin
    {
        private const string AdminPermission = "ipjoinnotifier.admin";
        private const string GeoApiUrl = "http://ip-api.com/json/";

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(AdminPermission, this);
        }

        private void OnUserConnected(IPlayer player)
        {
            if (player == null) return;

            var basePlayer = player.Object as BasePlayer;
            if (basePlayer?.net?.connection?.ipaddress == null)
                return;

            string ip = basePlayer.net.connection.ipaddress.Split(':')[0];

            // ðŸ”¸ Broadcast colored, one-line, big join message
            string broadcastMessage = $"<size=20><color=#FFA500>{player.Name} joined </color><color=#FFFF00>RevolutionX/PvE!</color></size>";
            server.Broadcast(broadcastMessage);

            // ðŸ”¹ Lookup location info and send to admins
            webrequest.Enqueue($"{GeoApiUrl}{ip}", null, (code, response) =>
            {
                if (code != 200 || string.IsNullOrWhiteSpace(response))
                {
                    PrintWarning($"Failed to fetch geo data for {player.Name} (IP: {ip}) - Code: {code}");
                    return;
                }

                try
                {
                    var geo = JsonConvert.DeserializeObject<GeoData>(response);

                    string adminMessage =
                        $"[{player.Name} (ID: {basePlayer.userID})]" +
                        $"\nâ–ª IP Address: {ip}" +
                        $"\nâ–ª State: {geo.RegionName}" +
                        $"\nâ–ª Country: {geo.Country}";

                    foreach (var p in players.Connected)
                    {
                        if (p.HasPermission(AdminPermission))
                            p.Message(adminMessage);
                    }
                }
                catch (Exception ex)
                {
                    PrintError($"Error parsing geo info for {player.Name}: {ex.Message}");
                }

            }, this);
        }

        #endregion

        #region GeoData Class

        private class GeoData
        {
            [JsonProperty("country")] public string Country { get; set; }
            [JsonProperty("regionName")] public string RegionName { get; set; }
        }

        #endregion
    }
}
