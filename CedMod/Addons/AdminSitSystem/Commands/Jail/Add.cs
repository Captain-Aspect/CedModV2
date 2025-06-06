﻿using System;
using System.Linq;
using CommandSystem;
using LabApi.Features.Permissions;

namespace CedMod.Addons.AdminSitSystem.Commands.Jail
{
    public class Add : ICommand
    {
        public string Command { get; } = "add";

        public string[] Aliases { get; } = {
            "a"
        };

        public string Description { get; } = "Adds the specified player to your current jail.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender,
            out string response)
        {
            if (!sender.HasPermissions("cedmod.jail"))
            {
                response = "no permission";
                return false;
            }

            if (arguments.Count < 1)
            {
                response = "This command requires a PlayerId to be specified";
                return false;
            }
            
            var invoker = CedModPlayer.Get((sender as CommandSender).SenderId);
            if (!AdminSitHandler.Singleton.Sits.Any(s => s.Players.Any(s => s.UserId == invoker.UserId)))
            {
                response = "You are currently not part of any jail.";
                return false;
            }
            
            if (invoker == null)
            {
                response = $"Invoker could not be found.";
                return false;
            }

            var sit = AdminSitHandler.Singleton.Sits.FirstOrDefault(s => s.Players.Any(s => s.UserId == invoker.UserId));
            
            var plr = CedModPlayer.Get(arguments.At(0));
            if (plr is null)
            {
                response = $"Player '{arguments.At(0)}' could not be found";
                return false;
            }
            if (AdminSitHandler.Singleton.Sits.Any(s => s.Players.Any(s => s.UserId == plr.UserId)))
            {
                response = "The specified player is already part of a jail.";
                return false;
            }
            
            JailParentCommand.AddPlr(plr, sit);

            response = "Player Added, use jail remove {playerId} to remove the player.";
            return false;
        }
    }
}