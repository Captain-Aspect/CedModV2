﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using LabApi.Features.Console;
using Newtonsoft.Json;

namespace CedMod
{
    public class CacheHandler
    {
        public static void Loop()
        {
            while (!Shutdown._quitting && CedModMain.Singleton.CacheHandler != null)
            {
                try
                {
                    foreach (var file in Directory.GetFiles(Path.Combine(CedModMain.PluginConfigFolder, "CedMod", "Internal")))
                    {
                        var fileData = new FileInfo(file);
                        
                        if (fileData.Name.StartsWith("tempb-"))
                        {
                            var fileContent = File.ReadAllText(file);
                            fileContent = EnsureValidJson(fileContent);
                            var dat1 = JsonConvert.DeserializeObject<Dictionary<string, object>>(fileContent);
                            if (!int.TryParse(dat1["BanDuration"].ToString(), out int dat))
                            {
                                Logger.Info($"Fixing broke pending ban");
                                dat1["BanDuration"] = 1440;
                            }
                            else
                            {
                                dat1["BanDuration"] = dat;
                            }

                            fileContent = JsonConvert.SerializeObject(dat1);
                            
                            Dictionary<string, string> result = (Dictionary<string, string>) API.APIRequest("Auth/Ban", fileContent, false, "POST").Result;
                            if (result == null)
                            {
                                Logger.Error($"Ban api request still failed, retrying later");
                                continue;
                            }
                            Logger.Info($"Ban api request succeeded");
                            File.Delete(file);
                        }
                        else if (fileData.Name.StartsWith("tempm-"))
                        {
                            var fileContent = File.ReadAllText(file);
                            fileContent = EnsureValidJson(fileContent);
                            var dat1 = JsonConvert.DeserializeObject<Dictionary<string, object>>(fileContent);
                            if (!int.TryParse(dat1["Muteduration"].ToString(), out int dat))
                            {
                                Logger.Info($"Fixing broke pending mute");
                                dat1["Muteduration"] = 1440;
                            }
                            else
                            {
                                dat1["Muteduration"] = dat;
                            }

                            if (dat1.ContainsKey("Userid"))
                                dat1["UserId"] = dat1["Userid"];
                            
                            fileContent = JsonConvert.SerializeObject(dat1);
                            
                            Dictionary<string, string> result = (Dictionary<string, string>) API.APIRequest($"api/Mute/{dat1["UserId"]}", fileContent, false, "POST").Result;
                            if (result == null)
                            {
                                Logger.Error($"Mute api request still failed, retrying later");
                                continue;
                            }
                            Logger.Info($"Mute api request succeeded");
                            File.Delete(file);
                        }
                        else if (fileData.Name.StartsWith("tempum-"))
                        {
                            var fileContent = File.ReadAllText(file);
                            fileContent = EnsureValidJson(fileContent);
                            Dictionary<string, string> result = (Dictionary<string, string>) API.APIRequest($"api/Mute/{fileContent}", "", false, "DELETE").Result;
                            if (result == null)
                            {
                                Logger.Error($"Unmute api request still failed, retrying later");
                                continue;
                            }
                            Logger.Info($"Unmute api request succeeded");
                            File.Delete(file);
                        }
                        else if (fileData.Name.StartsWith("tempd-"))
                        {
                            var date = File.GetLastWriteTime(file);
                            if (date < DateTime.UtcNow.AddDays(-30))
                            {
                                File.Delete(file);
                            }
                        }
                    }

                    WaitForSecond(10, (o) => !Shutdown._quitting && CedModMain.Singleton.CacheHandler != null);
                }
                catch (Exception e)
                {
                    Logger.Error($"Failed to process cache: {e}");
                    WaitForSecond(10, (o) => !Shutdown._quitting && CedModMain.Singleton.CacheHandler != null);
                }
            }
        }

        public static void WaitForSecond(int i, Predicate<object> predicate)
        {
            int wait = i;
            while (wait >= 0 && predicate.Invoke(i))
            {
                Thread.Sleep(1000);
                wait--;
            }
        }

        public static string EnsureValidJson(string json)
        {
            json = json.Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);
            return json;
        }
    }
}
