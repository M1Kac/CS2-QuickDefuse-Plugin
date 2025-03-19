using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Config;
using System.Text.Json.Serialization;

namespace QuickDefuse
{
    public class QuickDefuseConfig : BasePluginConfig
    {
        [JsonPropertyName("AutoCloseMenu")]
        public int AutoCloseMenu { get; set; } = 5;
    }
}
