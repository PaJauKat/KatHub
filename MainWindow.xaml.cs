using Octokit;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Threading.Tasks;

namespace KatHub
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            try
            {


                string state = calculateKatPluginsState();
                InstallKatPluginsButton.Content = state;
                updateKatPluginsPanel(state);

                string lolConfigLockerState = CheckLolConfigLocker_State();
                updateLolConfigLockerState(lolConfigLockerState);
            }
            catch
            {
                // ignore errors when checking installation
            }
        }

        private JarRelease jarRelease;
        private string TOKEN = "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";



        private class JarRelease
        {
            public string version;
            public string downloadUrl;
        }

        private string calculateKatPluginsState()
        {
            string katPLuginButton;
            if (!IsRuneliteCracked())
            {
                katPLuginButton = "Crackear";
            }
            else
            {
                string lastVersion = obtainLastVersion();
                string currentVersion = obtainCurrentVersion();
                Debug.WriteLine("Current version:" + currentVersion);
                Debug.WriteLine("Last version:" + lastVersion);

                if (currentVersion == "-1")
                {
                    katPLuginButton = "Install";
                }
                else if (currentVersion.CompareTo(lastVersion) < 0)
                {
                    katPLuginButton = "Update";
                }
                else
                {
                    katPLuginButton = "Installed";
                }

            }

            return katPLuginButton;
        }

        private string obtainLastVersion()
        {
            try
            {
                var client = new GitHubClient(new ProductHeaderValue("KatHub"));
                client.Credentials = new Credentials("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");
                var release = client.Repository.Release.GetLatest("PaJauKat", "KatPlugins").Result;

                // Safely get version
                string version = release?.TagName?.TrimStart('v') ?? "-1";

                // Find asset whose name ends with ".jar"
                var asset = release?.Assets?.FirstOrDefault(x => !string.IsNullOrEmpty(x?.Name) && x.Name.EndsWith(".jar"));

                if (asset == null)
                {
                    return "-1";
                }


                if (string.IsNullOrEmpty(asset.Url))
                {
                    return "-1";
                }

                jarRelease = new JarRelease
                {
                    version = version,
                    downloadUrl = asset.Url
                };

                return version;
            }
            catch
            {
                return "-1";
            }
        }

        private string obtainCurrentVersion()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string runeliteDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".runelite",
                    "sideloaded-plugins"
                );
                string[] jars = Directory.GetFiles(runeliteDir, "KatPlugins-*.jar");
                if (jars.Length == 0 || jars.Length > 1)
                    return "-1";
                string version = Path.GetFileNameWithoutExtension(jars[0]).Split('-').Last();
                return version;
            }
            catch
            {
                return "-1";
            }
        }

        private bool IsRuneliteCracked()
        {
            try
            {
                string crackerFileName = "EthanVannInstaller.jar";
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string runeliteDir = Path.Combine(localAppData, "RuneLite");
                string configFile = Path.Combine(runeliteDir, "config.json");

                if (!Directory.Exists(runeliteDir))
                    return false;

                string crackerPath = Path.Combine(runeliteDir, crackerFileName);
                if (!File.Exists(crackerPath))
                    return false;

                if (!File.Exists(configFile))
                    return false;

                var jsonText = File.ReadAllText(configFile);
                var node = JsonNode.Parse(jsonText);
                if (node == null)
                    return false;

                var mainClass = node["mainClass"]?.GetValue<string>();
                if (mainClass != "ca.arnah.runelite.LauncherHijack")
                    return false;

                if (node["classPath"] is not JsonArray classPathArray)
                    return false;

                bool hasCracker = classPathArray.Any(x => x?.GetValue<string>() == crackerFileName);
                bool hasRuneliteJar = classPathArray.Any(x => x?.GetValue<string>() == "RuneLite.jar");

                return hasCracker && hasRuneliteJar;
            }
            catch
            {
                return false;
            }
        }

        private async Task CrackearRunelite()
        {
            string crackerFileName = "EthanVannInstaller.jar";
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string runeliteDir = Path.Combine(localAppData, "RuneLite");
            string configFile = Path.Combine(runeliteDir, "config.json");

            try
            {
                if (!Directory.Exists(runeliteDir))
                {
                    Console.WriteLine("Runelite directory not found. Please install RuneLite first.");
                    return;
                }

                string crackerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, crackerFileName);
                if (!File.Exists(crackerPath))
                {
                    Console.WriteLine($"Cracker file '{crackerFileName}' not found in application directory.");
                    return;
                }

                File.Copy(crackerPath, Path.Combine(runeliteDir, crackerFileName), true);
                Console.WriteLine("Cracker file copied successfully to RuneLite directory.");

                // Modificando el config.json para incluir el plugin sin perder otros datos
                if (File.Exists(configFile))
                {
                    var jsonText = File.ReadAllText(configFile);
                    var node = JsonNode.Parse(jsonText) ?? new JsonObject();

                    // Establece "mainClass"
                    node["mainClass"] = "ca.arnah.runelite.LauncherHijack";

                    // Asegura que exista "classPath" como array y añade los elementos si no están
                    JsonArray classPathArray;
                    if (node["classPath"] is JsonArray existing)
                    {
                        classPathArray = existing;
                    }
                    else
                    {
                        classPathArray = new JsonArray();
                        node["classPath"] = classPathArray;
                    }

                    bool hasCracker = classPathArray.Any(x => x?.GetValue<string>() == crackerFileName);
                    if (!hasCracker) classPathArray.Add(crackerFileName);

                    bool hasRuneliteJar = classPathArray.Any(x => x?.GetValue<string>() == "RuneLite.jar");
                    if (!hasRuneliteJar) classPathArray.Add("RuneLite.jar");

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(configFile, node.ToJsonString(options));
                    Console.WriteLine("Config updated successfully.");
                }
                else
                {
                    Console.WriteLine("Config file not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void updateKatPluginsPanel(string state)
        {
            if (state == "Installed")
            {
                InstallKatPluginsButton.Visibility = Visibility.Collapsed;
                UninstallKatPluginsButton.Visibility = Visibility.Visible;
                InstalledLabel.Visibility = Visibility.Visible;
            }
            else
            {
                InstallKatPluginsButton.Visibility = Visibility.Visible;
                UninstallKatPluginsButton.Visibility = Visibility.Collapsed;
                InstalledLabel.Visibility = Visibility.Collapsed;
            }
            InstallKatPluginsButton.Content = state;
        }

        private async void InstallKatPlugins_Click(object sender, RoutedEventArgs e)
        {

            InstallKatPluginsButton.IsEnabled = false;
            try
            {
                var currentState = InstallKatPluginsButton.Content?.ToString() ?? "";
                string salida = "";
                if (currentState == "Crackear")
                {
                    await CrackearRunelite();
                    salida = calculateKatPluginsState();
                    if(salida == "Install")
                    {
                        MessageBox.Show("Runelite crackeado correctamente. Ahora puedes instalar el plugin.", "KatHub", MessageBoxButton.OK, MessageBoxImage.Information);
                    }else
                    {
                        MessageBox.Show("Error al crackear Runelite.", "KatHub", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    return;
                }
                else if (currentState == "Install")
                {
                    await installarJar();
                    salida = calculateKatPluginsState();
                    if (salida == "Installed")
                    {
                        MessageBox.Show("Plugin instalado correctamente.", "KatHub", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Error al instalar el plugin.", "KatHub", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                InstallKatPluginsButton.IsEnabled = true;
            }
        }

        private async Task installarJar()
        {
            try
            {
                if (jarRelease == null)
                {
                    obtainLastVersion();
                }

                if (jarRelease == null)
                {
                    Debug.WriteLine("Error obteniendo la última versión del plugin.");
                    return;
                }

                string jarDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".runelite",
                    "sideloaded-plugins"
                );
                Directory.CreateDirectory(jarDir);
                string jarPath = Path.Combine(jarDir, $"KatPlugins-{jarRelease.version}.jar");



                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("KatHub");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TOKEN);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/octet-stream"));
                client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

                var fileBytes = await client.GetByteArrayAsync(jarRelease.downloadUrl);
                await File.WriteAllBytesAsync(jarPath, fileBytes);

                Debug.WriteLine("Descargado el plugin a: " + jarPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error descargando el plugin: " + ex.Message);
            }


        }

        private async Task uninstallJar()
        {
            try
            {
                string jarDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".runelite",
                    "sideloaded-plugins"
                );
                if (!Directory.Exists(jarDir))
                    return;

                var jars = Directory.GetFiles(jarDir, "KatPlugins-*.jar");
                foreach (var jar in jars)
                {
                    try
                    {
                        File.Delete(jar);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Failed to delete " + jar + " : " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error during uninstall: " + ex.Message);
            }
        }


        private async void UninstallKatPlugins_Click(object sender, RoutedEventArgs e)
        {
            UninstallKatPluginsButton.IsEnabled = false;
            try
            {
                await uninstallJar();
                string salida = calculateKatPluginsState();
                if (salida != "Installed")
                {
                    MessageBox.Show("Plugin desinstalado correctamente.", "KatHub", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("No se pudo desinstalar el plugin.", "KatHub", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                UninstallKatPluginsButton.IsEnabled = true;
            }
        }

        private void LolConfigLocker_Click(object sender, RoutedEventArgs e)
        {
            LolConfigLockerButton.IsEnabled = false;
            try
            {
                string optionClicked = (string)LolConfigLockerButton.Content;
                if(optionClicked == "Lock")
                {
                    intentarModificarLolConfig(true);
                }
                else if(optionClicked == "Unlock")
                {
                    intentarModificarLolConfig(false);
                }
                string state = CheckLolConfigLocker_State();
                updateLolConfigLockerState(state);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                LolConfigLockerButton.IsEnabled = true;
            }
        }

        private void intentarModificarLolConfig(bool lockear)
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

        private void updateLolConfigLockerState(string state)
        {
            switch (state)
            {
                case "Locked":
                    LolConfigLockerStatusLabel.Text = "Locked ✔";
                    LolConfigLockerStatusLabel.Foreground = System.Windows.Media.Brushes.LightGreen;

                    LolConfigLockerButton.Content = "Unlock";
                    LolConfigLockerButton.IsEnabled = true;
                    LolConfigLockerButton.Visibility = Visibility.Visible;
                    break;
                case "Unlocked":
                    LolConfigLockerStatusLabel.Text = "Unlocked ❌";
                    LolConfigLockerStatusLabel.Foreground = System.Windows.Media.Brushes.LightCoral;

                    LolConfigLockerButton.Content = "Lock";
                    LolConfigLockerButton.IsEnabled = true;
                    LolConfigLockerButton.Visibility = Visibility.Visible;
                    break;
                case "Error":
                    LolConfigLockerStatusLabel.Text = "Error checking state";
                    LolConfigLockerStatusLabel.Foreground = System.Windows.Media.Brushes.Gray;

                    LolConfigLockerButton.Content = "Error";
                    LolConfigLockerButton.IsEnabled = false;
                    LolConfigLockerButton.Visibility = Visibility.Collapsed;
                    break;

            }
        }

        private string CheckLolConfigLocker_State()
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
                    return "Locked";
                }
                else
                {
                    return "Unlocked";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return "Error";
            }

        }

        private void OpenAccountManager_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("Anal");
                Process.Start("explorer.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
        
}