﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CedMod.Components;
using CentralAuth;
using HarmonyLib;
using MEC;
using NorthwoodLib.Pools;
using RemoteAdmin;
using RemoteAdmin.Communication;
using VoiceChat;

namespace CedMod.Patches
{
    [HarmonyPatch(typeof(RaPlayerList), nameof(RaPlayerList.ReceiveData), new Type[] { typeof(CommandSender), typeof(string) })]
    public static class RaPlayerListPatch
    {
        public static Dictionary<string, CoroutineHandle> Handles = new Dictionary<string, CoroutineHandle>();

        public static bool Prefix(RaPlayerList __instance, CommandSender sender, string data)
        {
            if (Handles.ContainsKey(sender.SenderId) && Handles[sender.SenderId].IsRunning)
                return false;
            
            Handles[sender.SenderId] = Timing.RunCoroutine(RaPlayerCoRoutine(__instance, sender, data));
            return false;
        }

        public static IEnumerator<float> RaPlayerCoRoutine(RaPlayerList __instance, CommandSender sender, string data)
        {
            string[] spData = data.Split(' ');
            if (spData.Length != 3) 
                yield break;

            // Check if the message is valid
            if (!int.TryParse(spData[0], out int value1) || !int.TryParse(spData[1], out int sortingId))
                yield break;

            // Check whether the sorting type is defined.
            if (!Enum.IsDefined(typeof(RaPlayerList.PlayerSorting), sortingId))
                yield break;

            bool isSilent = value1 == 1;
            bool isDescending = spData[2].Equals("1");
            RaPlayerList.PlayerSorting sorter = (RaPlayerList.PlayerSorting) sortingId;

            bool viewHiddenBadges = CommandProcessor.CheckPermissions(sender, PlayerPermissions.ViewHiddenBadges);
            bool viewHiddenGlobalBadges = CommandProcessor.CheckPermissions(sender, PlayerPermissions.ViewHiddenGlobalBadges);

            StringBuilder stringBuilder = StringBuilderPool.Shared.Rent("\n");
            stringBuilder.Clear();
            
            var plr = CedModPlayer.Get(sender.SenderId);

            if (!RemoteAdminModificationHandler.IngameUserPreferencesMap.ContainsKey(plr) && !RemoteAdminModificationHandler.Singleton.Requesting.Contains(plr.UserId))
            {
                RemoteAdminModificationHandler.Singleton.ResolvePreferences(plr, null);
            }

            if (CommandProcessor.CheckPermissions(sender, PlayerPermissions.AdminChat))
            {
                if (RemoteAdminModificationHandler.IngameUserPreferencesMap.ContainsKey(plr) && RemoteAdminModificationHandler.IngameUserPreferencesMap[plr].ShowReportsInRemoteAdmin)
                {
                    var openCount = RemoteAdminModificationHandler.ReportsList.Count(s => s.Status == HandleStatus.NoResponse);
                    var inProgressCount = RemoteAdminModificationHandler.ReportsList.Count(s => s.Status == HandleStatus.InProgress);

                    if (openCount == 0)
                    {
                        stringBuilder.Append("<size=0>(").Append(-1).Append(")</size>");
                        stringBuilder.Append("<color=green>[No Open Reports]</color>");
                    }
                    else
                    {
                        stringBuilder.Append("<size=0>(").Append(-1).Append(")</size>");
                        stringBuilder.Append($"{(RemoteAdminModificationHandler.UiBlink ? "[<color=yellow>⚠</color>] " : " ")}<color=red>[{openCount} Open Report{(openCount == 1 ? "" : "s")}]</color>");
                    }

                    stringBuilder.AppendLine();

                    if (inProgressCount == 0)
                    {
                        stringBuilder.Append("<size=0>(").Append(-2).Append(")</size>");
                        stringBuilder.Append("<color=green>[No InProgress Reports]</color>");
                    }
                    else
                    {
                        stringBuilder.Append("<size=0>(").Append(-2).Append(")</size>");
                        stringBuilder.Append($"{(RemoteAdminModificationHandler.UiBlink ? "[<color=yellow>⚠</color>] " : " ")}<color=orange>[{inProgressCount} Report{(inProgressCount == 1 ? "" : "s")} Inprogress]</color>");
                    }

                    stringBuilder.AppendLine();
                }
            }

            foreach (ReferenceHub hub in isDescending ? __instance.SortPlayersDescending(sorter) : __instance.SortPlayers(sorter))
            {
                if (PlayerAuthenticationManager.OnlineMode && (hub.Mode == ClientInstanceMode.DedicatedServer || hub.Mode == ClientInstanceMode.Unverified))
                    continue;

                bool inOverwatch = hub.serverRoles.IsInOverwatch;
                bool isMuted = VoiceChatMutes.IsMuted(hub, false);

                stringBuilder.Append(RaPlayerList.GetPrefix(hub, viewHiddenBadges, viewHiddenGlobalBadges));

                if (inOverwatch)
                    stringBuilder.Append(RaPlayerList.OverwatchBadge);
                if (isMuted)
                    stringBuilder.Append(RaPlayerList.MutedBadge);

                stringBuilder.Append("<color={RA_ClassColor}>(").Append(hub.PlayerId).Append(") ");

                if (RemoteAdminModificationHandler.IngameUserPreferencesMap.ContainsKey(plr) && RemoteAdminModificationHandler.IngameUserPreferencesMap[plr].ShowWatchListUsersInRemoteAdmin)
                {
                    if (RemoteAdminModificationHandler.GroupWatchlist.Any(s => s.UserIds.Contains(hub.authManager.UserId)))
                    {
                        if (RemoteAdminModificationHandler.GroupWatchlist.Count(s => s.UserIds.Contains(hub.authManager.UserId)) >= 2)
                        {
                            stringBuilder.Append($"<size=15><color=#00FFF6>[WMG{RemoteAdminModificationHandler.GroupWatchlist.Count(s => s.UserIds.Contains(hub.authManager.UserId))}]</color></size> ");
                        }
                        else
                        {
                            stringBuilder.Append($"<size=15><color=#00FFF6>[WG{RemoteAdminModificationHandler.GroupWatchlist.FirstOrDefault(s => s.UserIds.Contains(hub.authManager.UserId)).Id}]</color></size> ");
                        }
                    }
                    else if (RemoteAdminModificationHandler.Watchlist.Any(s => s.Userid == hub.authManager.UserId))
                    {
                        stringBuilder.Append($"<size=15><color=#00FFF6>[WL]</color></size> ");
                    }
                }

                stringBuilder.Append(hub.nicknameSync.CombinedName.Replace("\n", string.Empty).Replace("RA_", string.Empty)).Append("</color>");
                stringBuilder.AppendLine();
            }
            sender.RaReply(string.Format("${0} {1}", (object) __instance.DataId, (object) StringBuilderPool.Shared.ToStringReturn(stringBuilder)), true, !isSilent, string.Empty);
        }
    }
}