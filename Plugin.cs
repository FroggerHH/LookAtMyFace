using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.IO;

#pragma warning disable CS0618
namespace LookAtMyFace
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        #region values
        private const string ModName = "LookAtMyFace", ModVersion = "1.0.1", ModGUID = "com.Frogger." + ModName;
        private static readonly Harmony harmony = new(ModGUID);
        public static Plugin _self;
        #endregion
        #region ConfigSettings
        static readonly string ConfigFileName = $"{ModGUID}.cfg";
        DateTime LastConfigChange;
        public static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = _self.Config.Bind(group, name, value, description);
            return configEntry;
        }
        private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }
        private void SetCfgValue<T>(Action<T> setter, ConfigEntry<T> config)
        {
            setter(config.Value);
            config.SettingChanged += (_, _) => setter(config.Value);
        }
        public enum Toggle
        {
            On = 1,
            Off = 0
        }
        #endregion
        #region configs
        static ConfigEntry<float> hemletAlfaConfig;
        public static float hemletAlfa = 0.2f;
        #endregion

        private void Awake()
        {
            _self = this;
            harmony.PatchAll(typeof(Pacth));

            #region config
            Config.SaveOnConfigSet = false;

            hemletAlfaConfig = config("General", "Hemlet Alfa", hemletAlfa, "");

            SetupWatcherOnConfigFile();
            Config.ConfigReloaded += (_, _) => { UpdateConfiguration(); };
            Config.SaveOnConfigSet = true;
            Config.Save();
            #endregion

        }

        #region Patch
        [HarmonyPatch]
        public static class Pacth
        {
            [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetHelmetEquiped)), HarmonyPostfix]
            private static void SetHelmetPacth(VisEquipment __instance)
            {
                if (!__instance.m_helmetItemInstance) return;
                __instance.m_helmetHideHair = false;
                __instance.m_helmetHideBeard = false;

                bool flag = false;
                if (__instance.m_nViewOverride)
                {
                    flag = __instance.m_nViewOverride.name.ToLower().Contains("stand") || __instance.m_nViewOverride.name.ToLower().Contains("item");
                }
                if (!flag) __instance.m_helmetItemInstance.SetActive(false);
            }
        }
        #endregion
        #region Config
        public void SetupWatcherOnConfigFile()
        {
            FileSystemWatcher fileSystemWatcherOnConfig = new(Paths.ConfigPath, ConfigFileName);
            fileSystemWatcherOnConfig.Changed += ConfigChanged;
            fileSystemWatcherOnConfig.IncludeSubdirectories = true;
            fileSystemWatcherOnConfig.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            fileSystemWatcherOnConfig.EnableRaisingEvents = true;
        }
        private void ConfigChanged(object sender, FileSystemEventArgs e)
        {
            if ((DateTime.Now - LastConfigChange).TotalSeconds <= 5.0)
            {
                return;
            }
            LastConfigChange = DateTime.Now;
            try
            {
                Config.Reload();
                Debug("Reloading Config...");
            }
            catch
            {
                DebugError("Can't reload Config");
            }
        }
        private void UpdateConfiguration()
        {
            hemletAlfa = hemletAlfaConfig.Value;

            Debug("Configuration Received");
        }
        #endregion
        #region tools
        public void Debug(string msg)
        {
            Logger.LogInfo(msg);
        }
        public void DebugError(string msg)
        {
            Logger.LogError($"{msg} Write to the developer and moderator if this happens often.");
        }
        /*public class ConfigurationManagerAttributes
        {
            public int? Order;
            public bool? HideSettingName;
            public bool? HideDefaultButton;
            public string? DispName;
            public Action<ConfigEntryBase>? CustomDrawer;
        }*/
        #endregion

    }
}