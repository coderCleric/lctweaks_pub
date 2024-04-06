using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LCTweaks
{
    public class LCTConfig
    {
        //Input related configs
        public ConfigEntry<bool> ToggleSprint { get; private set; }
        public ConfigEntry<float> DropAllDelay { get; private set; }

        //Scanner related configs
        public ConfigEntry<int> MaxScannables { get; private set; }
        public ConfigEntry<bool> ScanInShip { get; private set; }

        //Terminal related configs
        public ConfigEntry<bool> TerminalBoom { get; private set; }
        public ConfigEntry<bool> MuteNearTerm { get; private set; }
        public ConfigEntry<bool> HealthOnTerm { get; private set; }

        /**
         * Make a new instance of the LCTConfig
         */
        public LCTConfig(ConfigFile cfg) 
        {

            //Input
            ToggleSprint = cfg.Bind<bool>("Input",
                "ToggleSprint",
                false,
                "If true, pressing the sprint key will keep the player sprinting until they stop moving.");
            DropAllDelay = cfg.Bind<float>("Input",
                "DropAllDelay",
                1f,
                "How many seconds drop must be held to drop all items.\n" +
                "Must be greater than or equal to the drop scrap delay");

            //Scanner
            MaxScannables = cfg.Bind<int>("Scanner",
                "MaxScannableObjects",
                13,
                "Sets how many items the scanner can pick up at once.\n" +
                "Must be at least 13.");
            ScanInShip = cfg.Bind<bool>("Scanner",
                "ScanThroughWallsInShip",
                false,
                "If true, scrap in the ship can be scanned through walls.");

            //Terminal
            TerminalBoom = cfg.Bind<bool>("Terminal",
                "TerminalMineExplosion",
                false,
                "If true, disabling mines from the terminal will trigger them to explode.");
            MuteNearTerm = cfg.Bind<bool>("Terminal",
                "MuteNearTerminal",
                false,
                "Makes it so that eyeless dogs cannot hear noises that come from within 5 meters of the terminal.");
            HealthOnTerm = cfg.Bind<bool>("Terminal",
                "ShowHealthOnRadar",
                false,
                "If true, player indicators on radar will change with health.");

            //Bound the configs
            DropAllDelay.Value = Mathf.Max(DropAllDelay.Value, 0.1f);
            MaxScannables.Value = Math.Max(MaxScannables.Value, 13);
        }
    }
}
