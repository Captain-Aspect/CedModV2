﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CedMod.Addons.Events;
using CedMod.Addons.QuerySystem;
using CedMod.Addons.QuerySystem.WS;
using CedMod.Components;
using HarmonyLib;
using UnityEngine;
using MEC;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Core.Extensions;
using PluginAPI.Helpers;
using PluginAPI.Loader;
using PluginAPI.Loader.Features;
using Object = UnityEngine.Object;

namespace CedMod
{
    
    public class CedModMain
    {
        private Harmony _harmony;
        public static CedModMain Singleton;
        public static string FileHash { get; set; } = "";

        public static string GitCommitHash = String.Empty;
        public static string VersionIdentifier = String.Empty;

        public static string PluginConfigFolder = "";
        public static string PluginLocation = "";
        public static PluginDirectory GameModeDirectory;
        public static Assembly Assembly;
        public static PluginHandler Handler;

        public const string Version = "3.3.0";

        [PluginConfig]
        public Config Config;

        [PluginEntryPoint("CedMod", Version, "SCP:SL Moderation system https://cedmod.nl/About", "ced777ric#0001")]
        void LoadPlugin()
        {
            Handler = PluginHandler.Get(this);
            Timing.CallDelayed(5, () =>
            {
                if (!AppDomain.CurrentDomain.GetAssemblies().Any(s => s.GetName().Name == "NWAPIPermissionSystem"))
                    Timing.CallContinuously(1, () => Log.Error("You do not have the NWAPIPermissionSystem Installed, CedMod Requires the NWAPIPermission system ih order to operate properly, please download it here: https://github.com/CedModV2/NWAPIPermissionSystem"));
            });
            
            PluginLocation = Handler.PluginFilePath;
            PluginConfigFolder = Handler.PluginDirectoryPath;
            

            Assembly = Assembly.GetExecutingAssembly();
            CosturaUtility.Initialize();
            
            PluginAPI.Events.EventManager.RegisterEvents<Handlers.Player>(this);
            PluginAPI.Events.EventManager.RegisterEvents<Handlers.Server>(this);
            
            PluginAPI.Events.EventManager.RegisterEvents<QueryMapEvents>(this);
            PluginAPI.Events.EventManager.RegisterEvents<QueryServerEvents>(this);
            PluginAPI.Events.EventManager.RegisterEvents<QueryPlayerEvents>(this);
            
            PluginAPI.Events.EventManager.RegisterEvents<EventManagerServerEvents>(this);
            PluginAPI.Events.EventManager.RegisterEvents<EventManagerPlayerEvents>(this);
            
            PluginAPI.Events.EventManager.RegisterEvents<AutoUpdater>(this);
            FactoryManager.RegisterPlayerFactory(this, new CedModPlayerFactory());
            

            try
            {
                GameModeDirectory = new PluginDirectory(Path.Combine(PluginConfigFolder, "CedModEvents"));
                var file = File.Open(PluginLocation, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileHash = GetHashCode(file, new MD5CryptoServiceProvider());
                file.Dispose();
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
            
            _harmony = new Harmony("com.cedmod.patch");
            _harmony.PatchAll();
            
            using (Stream stream = Assembly.GetManifestResourceStream("CedMod.version.txt"))
            using (StreamReader reader = new StreamReader(stream))
            {
                GitCommitHash = reader.ReadToEnd();
            }
            
            using (Stream stream = Assembly.GetManifestResourceStream("CedMod.versionIdentifier.txt"))
            using (StreamReader reader = new StreamReader(stream))
            {
                VersionIdentifier = reader.ReadToEnd();
            }
            
            if (File.Exists(Path.Combine(PluginConfigFolder, "CedMod", "dev.txt")))
            {
                Log.Info("Plugin running as Dev");
                QuerySystem.CurrentMaster = QuerySystem.DevPanelUrl;
                QuerySystem.IsDev = true;
            }

            Singleton = this;
            
            ThreadDispatcher dispatcher = Object.FindObjectOfType<ThreadDispatcher>();
            if (dispatcher == null)
                CustomNetworkManager.singleton.gameObject.AddComponent<ThreadDispatcher>();
            
            AutoUpdater updater = Object.FindObjectOfType<AutoUpdater>();
            if (updater == null)
                updater = CustomNetworkManager.singleton.gameObject.AddComponent<AutoUpdater>();
            
            RemoteAdminModificationHandler remoteAdminModificationHandler = Object.FindObjectOfType<RemoteAdminModificationHandler>();
            if (remoteAdminModificationHandler == null)
                remoteAdminModificationHandler = CustomNetworkManager.singleton.gameObject.AddComponent<RemoteAdminModificationHandler>();

            if (File.Exists(Path.Combine(PluginConfigFolder, "CedMod", $"QuerySystemSecretKey-{Server.Port}.txt")))
            {
                // Start the HTTP server.
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        WebSocketSystem.Start();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                    }
                });
            }
            else
                Log.Warning("Plugin is not setup properly, please use refer to the cedmod setup guide"); //todo link guide
            
            QuerySystem.QueryMapEvents = new QueryMapEvents();
            QuerySystem.QueryServerEvents = new QueryServerEvents();
            QuerySystem.QueryPlayerEvents = new QueryPlayerEvents();

            if (!Directory.Exists(Path.Combine(PluginConfigFolder, "CedModEvents")))
            {
                Directory.CreateDirectory(Path.Combine(PluginConfigFolder, "CedModEvents"));
            }
            
            if (!Directory.Exists(Path.Combine(PluginConfigFolder, "CedMod")))
            {
                Directory.CreateDirectory(Path.Combine(PluginConfigFolder, "CedMod"));
            }
            
            int successes = 0;
            
            Dictionary<Type, PluginHandler> handlers = new Dictionary<Type, PluginHandler>();
            Dictionary<PluginHandler, string> pluginLocations = new Dictionary<PluginHandler, string>();

            foreach (var file in Directory.GetFiles(Path.Combine(PluginConfigFolder, "CedModEvents"), "*.dll"))
            {
                var assembly = Assembly.Load(File.ReadAllBytes(file));
                
                var types = assembly.GetTypes();

                foreach (var entryType in types)
                {
                    if (!entryType.IsValidEntrypoint()) continue;

                    if (!AssemblyLoader.Plugins.ContainsKey(assembly)) AssemblyLoader.Plugins.Add(assembly, new Dictionary<Type, PluginHandler>());

                    if (!AssemblyLoader.Plugins[assembly].ContainsKey(entryType))
                    {
                        var pluginType = new PluginHandler(GameModeDirectory, entryType, entryType, types);
                        pluginLocations.Add(pluginType, file);
                        handlers.Add(entryType, pluginType);
                        AssemblyLoader.Plugins[assembly].Add(entryType, pluginType);
                        successes++;
                    }
                }
            }

            foreach (var gamemode in handlers)
            {
                if (EventManager.AvailableEventPlugins.Contains(gamemode.Value))
                {
                    Log.Error($"Found duplicate Event: {gamemode.Value.PluginName} Located at: {pluginLocations[gamemode.Value]} Please only have one of each Event installed");
                    continue;
                }
                EventManager.AvailableEventPlugins.Add(gamemode.Value);
                try
                {
                    gamemode.Value.Load();
                }
                catch (Exception e)
                {
                    Log.Info($"Failed to load {gamemode.Value.PluginName}.\n{e}");
                    continue;
                }

                foreach (var type in gamemode.Key.Assembly.GetTypes().Where(x => typeof(IEvent).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract))
                {
                    if (Config.EventManager.Debug)
                        Log.Debug($"Checked {gamemode.Value.PluginName} for CedMod-Events functionality, IEvent inherited");
                    var constructor = type.GetConstructor(Type.EmptyTypes);
                    if (constructor == null)
                    {
                        if (Config.EventManager.Debug)
                            Log.Debug($"Checked {gamemode.Value.PluginName} Constructor is null, cannot continue");
                        continue;
                    }
                    else
                    {
                        IEvent @event = constructor.Invoke(null) as IEvent;
                        if (@event == null) 
                            continue;
                        EventManager.AvailableEvents.Add(@event);
                        Log.Info($"Successfully registered {@event.EventName} By {@event.EvenAuthor} ({@event.EventPrefix})");
                    }
                }
            }
        }

        [PluginUnload]
        public void OnDisabled()
        {
            _harmony.UnpatchAll();
            Singleton = null;
            
            ThreadDispatcher dispatcher = Object.FindObjectOfType<ThreadDispatcher>();
            if (dispatcher != null)
                Object.Destroy(dispatcher);
            
            AutoUpdater updater = Object.FindObjectOfType<AutoUpdater>();
            if (updater != null)
                Object.Destroy(updater);
            
            RemoteAdminModificationHandler remoteAdminModificationHandler = Object.FindObjectOfType<RemoteAdminModificationHandler>();
            if (remoteAdminModificationHandler != null)
                Object.Destroy(remoteAdminModificationHandler);
            
            WebSocketSystem.Stop();
            
            QuerySystem.QueryMapEvents = null;
            QuerySystem.QueryServerEvents = null;
            QuerySystem.QueryPlayerEvents = null;
            
            EventManager.EventManagerServerEvents = null;
            EventManager.EventManagerPlayerEvents = null;

            foreach (var plugin in EventManager.AvailableEventPlugins)
            {
                try
                {
                    plugin.Unload();
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to disable GameMode: {plugin.PluginName}\n{e}");
                }
            }
        }
        
        internal static string GetHashCode(Stream stream, HashAlgorithm cryptoService)
        {
            using (cryptoService)
            {
                var hash = cryptoService.ComputeHash(stream);
                var hashString = Convert.ToBase64String(hash);
                return hashString.TrimEnd('=');
            }
        }
    }
}