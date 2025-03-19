using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using CS2ScreenMenuAPI;
using CS2ScreenMenuAPI.Internal;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API.Modules.Config;
using Microsoft.Extensions.Localization;

namespace QuickDefuse
{
    public class QuickDefuse : BasePlugin, IPluginConfig<QuickDefuseConfig>
    {
        public override string ModuleName => "QuickDefuse";
        public override string ModuleAuthor => "M1K@c";
        public override string ModuleVersion => "1.0.0";
        public override string ModuleDescription => "Adds a wire-cutting mini-game to bomb planting and defusing.";

        public required QuickDefuseConfig Config { get; set; }
        private Dictionary<int, string> bombWires = new();
        private static readonly string[] wireColors = { "Red", "Blue", "Yellow", "Green" };
        private Random random = new();
        private Dictionary<int, bool> isDefusing = new();
        private IStringLocalizer _Localizer = null!;

        public void OnConfigParsed(QuickDefuseConfig config)
        {
            Config = config;
        }

        public override void Load(bool hotReload)
        {
            _Localizer = Localizer ?? throw new InvalidOperationException("Localizer not available!");
        }

        [GameEventHandler]
        public HookResult OnBombPlanted(EventBombPlanted ev, GameEventInfo info)
        {
            var planter = ev.Userid;
            if (planter == null || !planter.IsValid) return HookResult.Continue;

            int userId = planter.UserId ?? 0;
            bombWires.Remove(userId);

            ShowWireSelectionMenu(planter);
            return HookResult.Continue;
        }


        private void ShowWireSelectionMenu(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            int userId = player.UserId ?? 0;
            string menuTitle = _Localizer?.GetString("select_wire_menu") ?? "Select a Wire Color";

            ScreenMenu menu = new(menuTitle, this)
            {
                IsSubMenu = false,
                FontName = "Verdana Bold",
                FreezePlayer = false
            };

            foreach (var color in wireColors)
            {
                menu.AddOption(color, (p, option) =>
                {
                    bombWires[userId] = option.Text;
                    string wireColor = WireColors.GetWireColor(option.Text);
                    string localizedMessage = _Localizer?.GetString("wire_selected", wireColor, option.Text) ?? 
                    $"You selected the {wireColor}{option.Text}{ChatColors.Default} wire.";
                    p.PrintToChat(localizedMessage);
                    MenuAPI.CloseActiveMenu(p);
                });
            }

            MenuAPI.OpenMenu(this, player, menu);
            
            Server.RunOnTick(Server.TickCount + (64 * Config.AutoCloseMenu), () =>
            {
                if (!bombWires.ContainsKey(userId))
                {
                    AssignRandomWire(userId, player);
                    MenuAPI.CloseActiveMenu(player);
                }
            });
        }

        [GameEventHandler]
        public HookResult OnBombBeginDefuse(EventBombBegindefuse ev, GameEventInfo info)
        {
            var defuser = ev.Userid;
            if (defuser == null || !defuser.IsValid) return HookResult.Continue;

            int userId = defuser.UserId ?? 0;
            isDefusing[userId] = true;

            ShowDefuseMenu(defuser);
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnBombAbortDefuse(EventBombAbortdefuse ev, GameEventInfo info)
        {
            var defuser = ev.Userid;
            if (defuser == null || !defuser.IsValid) return HookResult.Continue;

            int userId = defuser.UserId ?? 0;
            isDefusing[userId] = false;

            MenuAPI.CloseActiveMenu(defuser);
            return HookResult.Continue;
        }

        private void ShowDefuseMenu(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            int userId = player.UserId ?? 0;
            string playerName = player.PlayerName;

            if (!bombWires.ContainsKey(userId))
            {
                AssignRandomWire(userId, player);
            }

            string menuTitle = _Localizer?.GetString("cut_wire_menu") ?? "Cut a Wire";

            ScreenMenu menu = new(menuTitle, this)
            {
                IsSubMenu = false,
                FontName = "Verdana Bold",
                FreezePlayer = true
            };

            foreach (var color in wireColors)
            {
                menu.AddOption(color, (p, option) =>
                {
                    if (!bombWires.TryGetValue(userId, out string correctWire)) return;

                    if (option.Text == correctWire)
                    {
                        string localizedMessage = _Localizer?.GetString("bomb_defused", playerName) ??
                        $"[ QUICK-DEFUSE ] {playerName} chose the correct wire! Bomb has been defused.";
                        Server.PrintToChatAll(localizedMessage);
                        InstantDefuseBomb();
                        MenuAPI.CloseActiveMenu(p);
                    }
                    else
                    {
                        string wireColor = WireColors.GetWireColor(correctWire);
                        string localizedMessage = _Localizer?.GetString("bomb_exploded", playerName, wireColor, correctWire) ??
                        $"[ QUICK-DEFUSE ] {playerName} chose the wrong wire! The correct wire was {wireColor} {correctWire}{ChatColors.Default}.";
                        Server.PrintToChatAll(localizedMessage);
                        ExplodeBomb();
                        MenuAPI.CloseActiveMenu(p);
                    }
                });
            }

            MenuAPI.OpenMenu(this, player, menu);
        }

        private void InstantDefuseBomb()
        {
            var bomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
            if (bomb != null)
            {
                bomb.DefuseCountDown = Server.CurrentTime;
            }
        }

        public void ExplodeBomb()
        {
            var plantedBomb = FindPlantedBomb();
            if (plantedBomb == null) return;

            plantedBomb.TimerLength = 0f;
            plantedBomb.C4Blow = 0f;
        }

        private CPlantedC4? FindPlantedBomb()
        {
            return Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
        }

        private void AssignRandomWire(int userId, CCSPlayerController player)
        {
            string randomWire = wireColors[random.Next(wireColors.Length)];
            bombWires[userId] = randomWire;
        }
    }
}
