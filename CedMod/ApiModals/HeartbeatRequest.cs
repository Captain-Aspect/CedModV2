﻿using System.Collections.Generic;

namespace CedMod.ApiModals
{
    public class HeartbeatRequest
    {
        public List<PlayerObject> Players { get; set; } //is only sent if playerstats are enabled
        public List<EventModal> Events { get; set; } //is only sent if events are enabled
        public string PluginVersion { get; set; }
        public string PluginCommitHash { get; set; }
        public bool UpdateStats { get; set; }
        public bool TrackingEnabled { get; set; }
        public string ExiledVersion { get; set; }
        public string ScpSlVersion { get; set; }
        public string FileHash { get; set; }
        public string CedModVersionIdentifier { get; set; }
        public string KeyHash { get; set; }
        public bool IsVerified { get; set; }
        public double RealTps { get; set; }
        public double Tps { get; set; }
        public double FrameTime { get; set; }
        public short TargetTps { get; set; }
        public int CurrentSeed { get; set; }
    }
}