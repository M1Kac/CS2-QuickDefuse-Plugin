using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Generic;

namespace QuickDefuse
{
    public static class WireColors
    {
        public static readonly Dictionary<string, string> wireColorCodes = new()
        {
            { "Red", ChatColors.Red.ToString() },  
            { "Blue", ChatColors.LightBlue.ToString() },  
            { "Yellow", ChatColors.Yellow.ToString() },  
            { "Green", ChatColors.Green.ToString() }  
        };

        public static string GetWireColor(string wire)
        {
            return wireColorCodes.TryGetValue(wire, out string? color) ? color : ChatColors.Default.ToString();
        }
    }
}
