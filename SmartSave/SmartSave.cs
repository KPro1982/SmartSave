using System.Collections.Generic;
using System.IO;
using BepInEx;
using HarmonyLib;
using System.Windows.Forms;
using System;
using System.Runtime.InteropServices;
using BepInEx.Configuration;
using UnityEngine;

namespace SmartSave
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
   

    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class SmartSave : BaseUnityPlugin
    {
       
        
        public const string PluginGUID = "com.kpro.smartsave";
        public const string PluginName = "SmartSave";
        public const string PluginVersion = "1.0";

        private readonly Harmony harmony = new Harmony("com.kpro.smartsave");
        public SaveModel savedGamesData;
        public static string RestoreGamePath;
        public static bool RestoreGameOnShutdown;



        private void Awake()
        {
            savedGamesData = new SaveModel(Utils.GetSaveDataPath() + "/characters/");
            CreateConfigValues();
            harmony.PatchAll();
            RestoreGamePath = "";
        }


        public ConfigEntry<KeyboardShortcut> RestoreKey;
        public ConfigEntry<KeyboardShortcut> SaveNowKey;
        public ConfigEntry<bool> AutoLogOut;
        public ConfigEntry<bool> WindowsOperatingSystem;

        private void CreateConfigValues()
        {
            Config.SaveOnConfigSet = true;

            RestoreKey = Config.Bind("Save and Restore", "RestoreKey",
                new KeyboardShortcut(KeyCode.R, KeyCode.LeftControl), "Restore a saved game");
            SaveNowKey = Config.Bind("Save and Restore", "SaveNowKey",
                new KeyboardShortcut(KeyCode.S, KeyCode.LeftControl), "Save now");
            AutoLogOut = Config.Bind("Save and Restore", "AutoLogOut",true,"If checked, game will automatically log out when restoring a game.");
            WindowsOperatingSystem = Config.Bind("Save and Restore", "WindowsOS",true,"If checked, use windows OS for restore feature. Unchecked will use compatibility mode.");
        }

        


        private void Update()
        {
            if (ZInput.instance != null && ZNet.m_world != null)
            {
                if (RestoreKey != null && RestoreKey.Value.IsDown() && WindowsOperatingSystem.Value)
                {
                    GameCamera.instance.m_mouseCapture = false;
                    GameCamera.instance.UpdateMouseCapture();

                        var ofn = OpenFileDiag.OpenFileDialog();
                        
                        if (ofn.lpstrFile != "")
                        {
                            RestoreGamePath = ofn.lpstrFile;
                            RestoreGameOnShutdown = true;
                            GameCamera.instance.m_mouseCapture = true;
                            GameCamera.instance.UpdateMouseCapture();
                        }
                        else
                        {
                            RestoreGameOnShutdown = false;
                            GameCamera.instance.m_mouseCapture = true;
                            GameCamera.instance.UpdateMouseCapture();
                        }

                        if (RestoreGameOnShutdown && AutoLogOut.Value)
                        {
                            Game.instance.Logout();
                            
                        }
                        
                }

                if (SaveNowKey != null && SaveNowKey.Value.IsDown())
                {

                    Game.instance.SavePlayerProfile(false);

                }
            }
        }

        [HarmonyPatch(typeof(Game))]
        [HarmonyPatch("SavePlayerProfile")]
        public class SavePlayerProfile_Patch
        {
            static void Postfix(Game __instance, bool setLogoutPoint)
            {

                if (setLogoutPoint && RestoreGameOnShutdown)
                {
                    string original = Utils.GetSaveDataPath() + "/characters/" + __instance.GetPlayerProfile().m_filename +
                                    ".fch";
                    string backup = Utils.GetSaveDataPath() + "/characters/" + __instance.GetPlayerProfile().m_filename +
                                    ".bak";
                    
                    if(RestoreGamePath != null && RestoreGamePath != "")
                    {
                        if (File.Exists(backup + ".bak"))
                        {
                            File.Delete(backup + ".bak");
                        }

                        if (File.Exists(backup))
                        {
                            File.Move(backup, backup + ".bak");
                        }

                        try
                        {
                            File.Move(original, backup);
                        }
                        catch
                        {
                            return;
                        }

                        
                        File.Copy(RestoreGamePath, original);
                    }
                    
                }
                
            }
        }
        

        /*
        private bool RestoreSaveData(string savePath)
        {
            PlayerProfile pProfile = Game.m_instance.GetPlayerProfile();
            try
            {
                ZPackage zpackage = LoadSmartSave(savePath);
                if (zpackage == null)
                {
                    ZLog.LogWarning("No player data");
                    return false;
                }

                int num = zpackage.ReadInt();
                if (!Version.IsPlayerVersionCompatible(num))
                {
                    ZLog.Log("Player data is not compatible, ignoring");
                    //return false;
                }

                if (num >= 28)
                {
                    pProfile.m_playerStats.m_kills = zpackage.ReadInt();
                    pProfile.m_playerStats.m_deaths = zpackage.ReadInt();
                    pProfile.m_playerStats.m_crafts = zpackage.ReadInt();
                    pProfile.m_playerStats.m_builds = zpackage.ReadInt();
                }

                pProfile.m_worldData.Clear();
                int num2 = zpackage.ReadInt();
                for (int i = 0; i < num2; i++)
                {
                    long num3 = zpackage.ReadLong();
                    PlayerProfile.WorldPlayerData worldPlayerData =
                        new PlayerProfile.WorldPlayerData();
                    worldPlayerData.m_haveCustomSpawnPoint = zpackage.ReadBool();
                    worldPlayerData.m_spawnPoint = zpackage.ReadVector3();
                    worldPlayerData.m_haveLogoutPoint = zpackage.ReadBool();
                    worldPlayerData.m_logoutPoint = zpackage.ReadVector3();
                    if (num >= 30)
                    {
                        worldPlayerData.m_haveDeathPoint = zpackage.ReadBool();
                        worldPlayerData.m_deathPoint = zpackage.ReadVector3();
                    }

                    worldPlayerData.m_homePoint = zpackage.ReadVector3();
                    if (num >= 29 && zpackage.ReadBool())
                    {
                        worldPlayerData.m_mapData = zpackage.ReadByteArray();
                    }

                    pProfile.m_worldData.Add(num3, worldPlayerData);
                }

                pProfile.m_playerName = zpackage.ReadString();
                pProfile.m_playerID = zpackage.ReadLong();
                pProfile.m_startSeed = zpackage.ReadString();
                if (zpackage.ReadBool())
                {
                    pProfile.m_playerData = zpackage.ReadByteArray();
                }
                else
                {
                    pProfile.m_playerData = null;
                }
            }
            catch (Exception ex)
            {
                ZLog.LogWarning("Exception while loading player profile:" + pProfile.m_filename +
                                " , " + ex.ToString());
            }

            return true;
        }

        private ZPackage LoadSmartSave(string savePath)
        {
            string text = savePath;
            FileStream fileStream;
            try
            {
                fileStream = File.OpenRead(text);
            }
            catch
            {
                ZLog.Log("  failed to load " + text);
                return null;
            }

            byte[] data;
            try
            {
                BinaryReader binaryReader = new BinaryReader(fileStream);
                int num = binaryReader.ReadInt32();
                data = binaryReader.ReadBytes(num);
                int num2 = binaryReader.ReadInt32();
                binaryReader.ReadBytes(num2);
            }
            catch
            {
                ZLog.LogError("  error loading player.dat");
                fileStream.Dispose();
                return null;
            }

            fileStream.Dispose();
            return new ZPackage(data);
        }


        static string FormatCreateDate(string rawCreateDate)
        {
            rawCreateDate = rawCreateDate.Replace(':', '.');
            rawCreateDate = rawCreateDate.Replace('-', '.');
            return rawCreateDate.Replace('T', '.');
        } */
    }
    


    [HarmonyPatch(typeof(PlayerProfile))]
    [HarmonyPatch("SavePlayerToDisk")]
    public class SavePlayerToDisk_Patch
    {
        static void Postfix(PlayerProfile __instance)
        {
            int curDay = EnvMan.instance.GetDay(ZNet.instance.GetTimeSeconds());
            var curFractionalDay =
                (EnvMan.instance.m_totalSeconds / (double) EnvMan.instance.m_dayLengthSec);
            var curHour = (int) ((curFractionalDay - (int) curFractionalDay) * 24);


            string curWorldPath = ZNet.m_world.GetDBPath();
            string curWorld = Path.GetFileNameWithoutExtension(curWorldPath);

            string source = Utils.GetSaveDataPath() + "/characters/" + __instance.m_filename +
                            ".fch";

            string cName = __instance.m_filename;
            string firstChar = cName.Substring(0, 1), remainingChars = cName.Substring(1);
            cName = firstChar.ToUpper() + remainingChars;
            
            
            if (File.Exists(source))
            {
                //   string sourceCreateDate =  FormatCreateDate(File.GetLastWriteTime(source).ToString("s"));
                string dest = Utils.GetSaveDataPath() + "/characters/" + cName +
                              "_" + curWorld + "_Day " + curDay.ToString() + "_" +
                              curHour.ToString("00") + ".sav";

                try
                {
                    File.Copy(source, dest);
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"SmartSave");
                }
                catch
                {
                }
            }
        }
    }


    
}