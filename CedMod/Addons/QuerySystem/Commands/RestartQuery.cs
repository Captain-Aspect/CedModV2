﻿using System;
using System.Threading.Tasks;
using CedMod.Addons.QuerySystem.WS;
using CommandSystem;
using LabApi.Features.Permissions;
#if !EXILED

#else
using Exiled.Permissions.Extensions;
#endif

namespace CedMod.Addons.QuerySystem.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class RestartQueryCommand : ICommand
    {
        public string Command { get; } = "restartqueryserver";

        public string[] Aliases { get; } = new string[]
        {
        };

        public string Description { get; } = "restarts querysystem";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender,
            out string response)
        {
            if (!sender.HasPermissions("cedmod.restartquery"))
            {
                response = "No permission";
                return false;
            }

            Task.Run(async () =>
            { 
                WebSocketSystem.Stop();
                await WebSocketSystem.Start();
            });
            response = "Query server restarted";
            return true;
        }
    }
}