﻿using System;
using System.Collections.Generic;
using System.Linq;
using AdminToys;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using Mirror;
using UnityEngine;

namespace CedMod.Addons.AdminSitSystem.Commands.Jail
{
    public class Create : ICommand
    {
        public string Command { get; } = "create";

        public string[] Aliases { get; } = {
            "cr",
            "c"
        };

        public string Description { get; } = "Assigns an available jail location to your player.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender,
            out string response)
        {
            if (!sender.HasPermissions("cedmod.jail"))
            {
                response = "no permission";
                return false;
            }

            if (!AdminSitHandler.Singleton.AdminSitLocations.Any(s => !s.InUse))
            {
                response = "There are no locations available.";
                return false;
            }

            var loc = AdminSitHandler.Singleton.AdminSitLocations.FirstOrDefault(s => !s.InUse);
            var plr = CedModPlayer.Get((sender as CommandSender).SenderId);
            if (plr == null)
            {
                response = $"Invoker could not be found.";
                return false;
            }
            
            if (AdminSitHandler.Singleton.Sits.Any(s => s.Players.Any(s => s.UserId == plr.UserId)))
            {
                response = "You are already part of a jail.";
                return false;
            }
            
            AdminToyBase adminToyBase = null;
            foreach (GameObject gameObject in NetworkClient.prefabs.Values)
            {
                if (gameObject == null)
                    continue;
                AdminToyBase component;
                if (gameObject.TryGetComponent<AdminToyBase>(out component))
                {
                    if (string.Equals("LightSource", component.CommandName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        adminToyBase = UnityEngine.Object.Instantiate<AdminToyBase>(component);
                        adminToyBase.transform.position = loc.SpawnPosition;
                        NetworkServer.Spawn(adminToyBase.gameObject);
                    }
                }
            }

            var sit = new AdminSit()
            {
                AssociatedReportId = 0,
                InitialDuration = 0,
                InitialReason = "",
                Location = loc,
                SpawnedObjects = new List<AdminToyBase>()
                {
                    adminToyBase,
                },
                Players = new List<AdminSitPlayer>()
            };
            
            AdminSitHandler.Singleton.Sits.Add(sit);

            loc.InUse = true;
            
            JailParentCommand.AddPlr(plr, sit);

            response = "";
            foreach (var plr1 in arguments)
            {
                Player cmPlr = CedModPlayer.Get(plr1);
                if (cmPlr == null)
                {
                    response += $"{plr1} Could not be found!\n";
                }
                
                JailParentCommand.AddPlr(cmPlr, sit);
            }

            response += "Jail assigned. Use jail add {playerId} to add someone and jail remove {playerId}";
            return false;
        }
    }
}