using System.Diagnostics;
using System.IO;

namespace KatHub.LolConfig
{
    public class LolConfigService
    {
        private static LolConfigService instance;
        private static string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lol_config.json");
        private static LolConfigState currentState = LolConfigState.Unknown;
        public static LolConfigService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LolConfigService();
                }
                return instance;
            }
        }
        public static async void IntentarModificarLolConfig(bool lockear)
        {
            string lolConfigPath = Path.Combine(
                                @"C:\",
                                "Riot Games",
                                "League of Legends",
                                "Config",
                                "PersistedSettings.json"
                            );
            if (!File.Exists(lolConfigPath))
            {
                Debug.WriteLine("Settings file not found at: " + lolConfigPath);
                return;
            }
            try
            {
                FileInfo fileInfo = new(lolConfigPath);
                fileInfo.IsReadOnly = lockear;
            }
            catch (IOException ex)
            {
                Debug.WriteLine("Error modifying file attributes: " + ex.Message);
            }
        }

        public static async Task<LolConfigState> CheckLolConfigLocker_State()
        {
            try
            {
                string lolConfigPath = Path.Combine(
                    @"C:\",
                    "Riot Games",
                    "League of Legends",
                    "Config",
                    "PersistedSettings.json"
                );

                FileAttributes attributes = File.GetAttributes(lolConfigPath);
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    return LolConfigState.Locked;
                }
                else
                {
                    return LolConfigState.Unlocked;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return LolConfigState.Unknown;
            }

        }
    }
}