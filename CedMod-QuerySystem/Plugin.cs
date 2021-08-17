﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CedMod.QuerySystem.WS;
using Exiled.API.Enums;
using Exiled.API.Features;
using HarmonyLib;

namespace CedMod.QuerySystem
{
    public class QuerySystem : Plugin<Config>
    {
        public MapEvents MapEvents;
        public ServerEvents ServerEvents;
        public PlayerEvents PlayerEvents;
        public static Harmony harmony;
        public static List<string> ReservedSlotUserids = new List<string>();

        /// <inheritdoc/>
        public override PluginPriority Priority { get; } = PluginPriority.Default;

        /// <inheritdoc/>
        /// 
        public static Dictionary<QueryUser, string> autheduers = new Dictionary<QueryUser, string>();

        public override string Author { get; } = "ced777ric#0001";

        public override string Name { get; } = "CedMod-WebAPI";

        public override string Prefix { get; } = "cm_WAPI";

        public static Config config;

        public static string PanelUrl = "frikanweb.cedmod.nl";

        public override void OnDisabled()
        {
            harmony.UnpatchAll();
            // Unload the event handlers.
            // Close the HTTP server.
            WebSocketSystem.Stop();
            //Exiled.Events.Handlers.Server.SendingRemoteAdminCommand += CommandHandler.HandleCommand;

            Exiled.Events.Handlers.Map.Decontaminating -= MapEvents.OnDecon;
            Exiled.Events.Handlers.Warhead.Starting -= MapEvents.OnWarheadStart;
            Exiled.Events.Handlers.Warhead.Stopping -= MapEvents.OnWarheadCancelled;
            Exiled.Events.Handlers.Warhead.Detonated -= MapEvents.OnWarheadDetonation;

            //Exiled.Events.Handlers.Server.SendingRemoteAdminCommand -= ServerEvents.OnCommand;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= ServerEvents.OnWaitingForPlayers;
            //Exiled.Events.Handlers.Server.SendingConsoleCommand -= ServerEvents.OnConsoleCommand;
            Exiled.Events.Handlers.Server.RoundStarted -= ServerEvents.OnRoundStart;
            Exiled.Events.Handlers.Server.RoundEnded -= ServerEvents.OnRoundEnd;
            Exiled.Events.Handlers.Server.RespawningTeam -= ServerEvents.OnRespawn;
            Exiled.Events.Handlers.Server.ReportingCheater -= ServerEvents.OnCheaterReport;

            Exiled.Events.Handlers.Player.ItemUsed -= PlayerEvents.OnUsedItem;
            Exiled.Events.Handlers.Scp079.InteractingTesla -= PlayerEvents.On079Tesla;
            Exiled.Events.Handlers.Player.EscapingPocketDimension -= PlayerEvents.OnPocketEscape;
            Exiled.Events.Handlers.Player.EnteringPocketDimension -= PlayerEvents.OnPocketEnter;
            Exiled.Events.Handlers.Player.ThrowingItem -= PlayerEvents.OnGrenadeThrown;
            Exiled.Events.Handlers.Player.Hurting -= PlayerEvents.OnPlayerHurt;
            Exiled.Events.Handlers.Player.Dying -= PlayerEvents.OnPlayerDeath;
            Exiled.Events.Handlers.Player.InteractingElevator -= PlayerEvents.OnElevatorInteraction;
            Exiled.Events.Handlers.Player.Handcuffing -= PlayerEvents.OnPlayerHandcuffed;
            Exiled.Events.Handlers.Player.RemovingHandcuffs -= PlayerEvents.OnPlayerFreed;
            Exiled.Events.Handlers.Player.Verified -= PlayerEvents.OnPlayerJoin;
            Exiled.Events.Handlers.Player.Left -= PlayerEvents.OnPlayerLeave;
            Exiled.Events.Handlers.Player.ChangingRole -= PlayerEvents.OnSetClass;

            MapEvents = null;
            ServerEvents = null;
            PlayerEvents = null;
        }

        public static string SecurityKey;

        public override void OnEnabled()
        {
            config = Config;
            // Load the event handlers.
            if (!Config.IsEnabled)
                return;

            if (SecurityKey != "None")
            {
                // Start the HTTP server.
                WS.WebSocketSystem.Start();
            }
            else
                Exiled.API.Features.Log.Warn("security_key is set to none plugin will not load due to security risks");

            harmony = new Harmony("com.cedmod.querysystem");
            harmony.PatchAll();
            
            MapEvents = new MapEvents();
            ServerEvents = new ServerEvents();
            PlayerEvents = new PlayerEvents();
            //Exiled.Events.Handlers.Server.SendingRemoteAdminCommand += CommandHandler.HandleCommand;

            Exiled.Events.Handlers.Map.Decontaminating += MapEvents.OnDecon;
            Exiled.Events.Handlers.Warhead.Starting += MapEvents.OnWarheadStart;
            Exiled.Events.Handlers.Warhead.Stopping += MapEvents.OnWarheadCancelled;
            Exiled.Events.Handlers.Warhead.Detonated += MapEvents.OnWarheadDetonation;

            //Exiled.Events.Handlers.Server.SendingRemoteAdminCommand += ServerEvents.OnCommand;
            Exiled.Events.Handlers.Server.WaitingForPlayers += ServerEvents.OnWaitingForPlayers;
            //Exiled.Events.Handlers.Server.SendingConsoleCommand += ServerEvents.OnConsoleCommand;
            Exiled.Events.Handlers.Server.RoundStarted += ServerEvents.OnRoundStart;
            Exiled.Events.Handlers.Server.RoundEnded += ServerEvents.OnRoundEnd;
            Exiled.Events.Handlers.Server.RespawningTeam += ServerEvents.OnRespawn;
            Exiled.Events.Handlers.Server.ReportingCheater += ServerEvents.OnCheaterReport;
            Exiled.Events.Handlers.Server.LocalReporting += ServerEvents.OnReport;

            Exiled.Events.Handlers.Player.ItemUsed += PlayerEvents.OnUsedItem;
            Exiled.Events.Handlers.Scp079.InteractingTesla += PlayerEvents.On079Tesla;
            Exiled.Events.Handlers.Player.EscapingPocketDimension += PlayerEvents.OnPocketEscape;
            Exiled.Events.Handlers.Player.EnteringPocketDimension += PlayerEvents.OnPocketEnter;
            Exiled.Events.Handlers.Player.ThrowingItem += PlayerEvents.OnGrenadeThrown;
            Exiled.Events.Handlers.Player.Hurting += PlayerEvents.OnPlayerHurt;
            Exiled.Events.Handlers.Player.Dying += PlayerEvents.OnPlayerDeath;
            Exiled.Events.Handlers.Player.InteractingElevator += PlayerEvents.OnElevatorInteraction;
            Exiled.Events.Handlers.Player.Handcuffing += PlayerEvents.OnPlayerHandcuffed;
            Exiled.Events.Handlers.Player.RemovingHandcuffs += PlayerEvents.OnPlayerFreed;
            Exiled.Events.Handlers.Player.Verified += PlayerEvents.OnPlayerJoin;
            Exiled.Events.Handlers.Player.Left += PlayerEvents.OnPlayerLeave;
            Exiled.Events.Handlers.Player.ChangingRole += PlayerEvents.OnSetClass;
        }

        public override void OnReloaded()
        {
        }

        public static string DecryptString(string cipherText, byte[] key, byte[] iv)
        {
            // Instantiate a new Aes object to perform string symmetric encryption
            Aes encryptor = Aes.Create();

            encryptor.Mode = CipherMode.CBC;
            //encryptor.KeySize = 256;
            //encryptor.BlockSize = 128;
            //encryptor.Padding = PaddingMode.Zeros;

            // Set key and IV
            encryptor.Key = key;
            encryptor.IV = iv;

            // Instantiate a new MemoryStream object to contain the encrypted bytes
            MemoryStream memoryStream = new MemoryStream();

            // Instantiate a new encryptor from our Aes object
            ICryptoTransform aesDecryptor = encryptor.CreateDecryptor();

            // Instantiate a new CryptoStream object to process the data and write it to the 
            // memory stream
            CryptoStream cryptoStream = new CryptoStream(memoryStream, aesDecryptor, CryptoStreamMode.Write);

            // Will contain decrypted plaintext
            string plainText = String.Empty;

            try
            {
                // Convert the ciphertext string into a byte array
                byte[] cipherBytes = Convert.FromBase64String(cipherText);

                // Decrypt the input ciphertext string
                cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);

                // Complete the decryption process
                cryptoStream.FlushFinalBlock();

                // Convert the decrypted data from a MemoryStream to a byte array
                byte[] plainBytes = memoryStream.ToArray();

                // Convert the decrypted byte array to string
                plainText = Encoding.ASCII.GetString(plainBytes, 0, plainBytes.Length);
            }
            finally
            {
                // Close both the MemoryStream and the CryptoStream
                memoryStream.Close();
                cryptoStream.Close();
            }

            // Return the decrypted data as a string
            return plainText;
        }
    }
}