using AutoUpdaterDotNET;
using KatHub.KatPlugins;
using KatHub.LolConfig;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Media;

namespace KatHub
{
    public partial class MainWindow : Window
    {
        public static readonly SolidColorBrush KatRed = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e63946"));
        public static readonly SolidColorBrush KatGray = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#444"));
        private KatPluginsState currentKatPluginsState = KatPluginsState.Unknown;
        //public static string GITHUB_TOKEN = string.Empty;
        

        public MainWindow()
        {
            InitializeComponent();
            CheckForUpdates();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //GITHUB_TOKEN = await GetGitHubTokenFromConfig();
                currentKatPluginsState = await KatPluginsService.CalculateKatPluginsState();
                UpdateKatPluginsPanel(currentKatPluginsState);

                var lolConfigState = await LolConfigService.CheckLolConfigLocker_State();
                UpdateLolConfigLockerPanel(lolConfigState);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error al cargar estados: " + ex.Message);
            }
        }

        public void CheckForUpdates()
        {
            // Configuraciones primero
            AutoUpdater.ShowRemindLaterButton = false;
            AutoUpdater.LetUserSelectRemindLater = false;

            // Si quieres que la ventana sea pequeña y no una página completa:
            AutoUpdater.ShowSkipButton = false;

            AutoUpdater.Start("https://raw.githubusercontent.com/PaJauKat/KatHub/main/UpdateCheck.xml");
        }

        private static async Task<string> GetGitHubTokenFromConfig()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!File.Exists(configPath))
            {
                Debug.WriteLine("config.json not found at: " + configPath);
                return string.Empty;
            }
            try
            {
                string jsonText = File.ReadAllText(configPath);
                var node = JsonNode.Parse(jsonText);
                string token = node?["GitHub"]?["Token"]?.GetValue<string>() ?? string.Empty;
                return token;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error reading config.json: " + ex.Message);
                return string.Empty;
            }
        }

        private void UpdateKatPluginsPanel(KatPluginsState state)
        {
            KatPluginsStatusLabel.Text = state.GetStatusText();
            KatPluginsStatusLabel.Foreground = state.GetStatusColor();

            KatPluginsMainButton.Background = state.GetButtonColor();
            KatPluginsMainButton.Content = state.ToFriendlyString();
            KatPluginsMainButton.Visibility = state.GetMainButtonVisibility();

            UninstallKatButton.Visibility = state.GetUninstallVisibility();

            currentKatPluginsState = state;
        }

        private static void MostrarMensaje(bool exito, string ok, string error)
        {
            if (exito) MessageBox.Show(ok, "KatHub", MessageBoxButton.OK, MessageBoxImage.Information);
            else MessageBox.Show(error, "KatHub", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async void UninstallKatButton_Click(object sender, RoutedEventArgs e)
        {
            
            var result = MessageBox.Show("Are you sure you want to uninstall KatPlugins?",
                                 "Uninstall KatPlugins", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                UninstallKatButton.IsEnabled = false;
                KatPluginsMainButton.IsEnabled = false; // Evitamos que el usuario intente otras acciones mientras desinstala
                try
                {
                    bool exito = await KatPluginsService.ExecuteUninstallJar();
                    if (!exito)
                    {
                        MostrarMensaje(false,"", "Error uninstalling the plugin. Aborting crack/uninstall process.");
                    }
                    else
                    {
                        exito = await KatPluginsService.ExecuteCrackearRunelite(false);
                        MostrarMensaje(exito, "Plugin uninstalled successfully.", "Error uninstalling the plugin.");
                    }
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    currentKatPluginsState = await KatPluginsService.CalculateKatPluginsState();
                    UpdateKatPluginsPanel(currentKatPluginsState);
                    UninstallKatButton.IsEnabled = true;
                    KatPluginsMainButton.IsEnabled = true;
                }
            }

            
        }

        private async void KatPluginsMainButton_Click(object sender, RoutedEventArgs e)
        {
            KatPluginsMainButton.IsEnabled = false;
            var estadoInicial = currentKatPluginsState;

            try
            {
                switch (estadoInicial)
                {
                    case KatPluginsState.Crackear:
                        await KatPluginsService.ExecuteCrackearRunelite(true);
                        goto case KatPluginsState.Install; // Si el cracking fue exitoso, continuamos con la instalación sin esperar a que el usuario vuelva a hacer click.
                    case KatPluginsState.Install:
                    case KatPluginsState.Update:
                        await KatPluginsService.ExecuteInstallOrUpdateJar(new Progress<double>(p =>
                        {
                            KatProgressBar.Visibility = Visibility.Visible;
                            KatProgressBar.Value = p;
                            KatPluginsMainButton.Content = $"Installing... {p:P0}";
                        }));
                        KatProgressBar.Visibility = Visibility.Collapsed;
                        break;
                    case KatPluginsState.UpToDate:
                        await KatPluginsService.ExecuteUninstallJar();
                        await KatPluginsService.ExecuteCrackearRunelite(false);
                        break;
                    case KatPluginsState.MultipleVersions:
                        await KatPluginsService.ExecuteUninstallJar();
                        await KatPluginsService.ExecuteInstallOrUpdateJar(new Progress<double>(p =>
                        {
                            KatProgressBar.Visibility = Visibility.Visible;
                            KatProgressBar.Value = p;
                            KatPluginsMainButton.Content = $"Installing... {p:P0}";
                        }));
                        break;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception: {ex.Message}", "KatHub", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                currentKatPluginsState = await KatPluginsService.CalculateKatPluginsState();
                UpdateKatPluginsPanel(currentKatPluginsState);
                KatPluginsMainButton.IsEnabled = true;
            }
        }

        private async Task InstalarAccountManager()
        {
            string runeliteSettings = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".runelite",
                    "settings.json"
                );

            try
            {
                string text = File.ReadAllText(runeliteSettings);
                JsonNode node = JsonNode.Parse(text) ?? new JsonObject();
                JsonArray clientArgs;
                if (node["clientArguments"] is JsonArray existing)
                {
                    clientArgs = existing;
                }
                else
                {
                    clientArgs = new JsonArray();
                    node["clientArguments"] = clientArgs;
                }
                bool hasWriteCredentials = clientArgs.Any(x => x?.GetValue<string>() == "--insecure-write-credentials");
                if (!hasWriteCredentials)
                    clientArgs.Add("--insecure-write-credentials");

                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(runeliteSettings, node.ToJsonString(options));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error modificando settings.json: " + ex.Message, "KatHub", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string appsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KatHub", "Apps");
            string managerDir = Path.Combine(appsDir, "KatAccountManager");
            string archivoZip = Path.Combine(Path.GetTempPath(), "KatAccounts.zip");
            try
            {
                if (!Directory.Exists(appsDir))
                {
                    Directory.CreateDirectory(appsDir);
                }

                string urlDescarga = "https://github.com/PaJauKat/KatAccountManager/releases/download/v1.0/KatAccounts.zip";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("KatHub");
                var fileBytes = await client.GetByteArrayAsync(urlDescarga);
                await File.WriteAllBytesAsync(archivoZip, fileBytes);

                if (Directory.Exists(managerDir)) Directory.Delete(managerDir, true);
                ZipFile.ExtractToDirectory(archivoZip, managerDir);

                File.Delete(archivoZip);
                MessageBox.Show("Account Manager instalado correctamente en: " + managerDir, "KatHub", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al instalar el Account Manager: " + ex.Message, "KatHub", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void LolConfigLocker_Click(object sender, RoutedEventArgs e)
        {
            LolConfigLockerButton.IsEnabled = false;
            try
            {
                string optionClicked = (string)LolConfigLockerButton.Content;
                if (optionClicked == "Lock")
                {
                    LolConfigService.IntentarModificarLolConfig(true);
                }
                else if (optionClicked == "Unlock")
                {
                    LolConfigService.IntentarModificarLolConfig(false);
                }
                LolConfigState state = await LolConfigService.CheckLolConfigLocker_State();
                UpdateLolConfigLockerPanel(state);
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

        private void UpdateLolConfigLockerPanel(LolConfigState state)
        {
            switch (state)
            {
                case LolConfigState.Locked:
                    LolConfigLockerStatusLabel.Text = "Locked ✔";
                    LolConfigLockerStatusLabel.Foreground = System.Windows.Media.Brushes.LightGreen;

                    LolConfigLockerButton.Content = "Unlock";
                    LolConfigLockerButton.IsEnabled = true;
                    LolConfigLockerButton.Visibility = Visibility.Visible;
                    break;
                case LolConfigState.Unlocked:
                    LolConfigLockerStatusLabel.Text = "Unlocked ❌";
                    LolConfigLockerStatusLabel.Foreground = System.Windows.Media.Brushes.LightCoral;

                    LolConfigLockerButton.Content = "Lock";
                    LolConfigLockerButton.IsEnabled = true;
                    LolConfigLockerButton.Visibility = Visibility.Visible;
                    break;
                case LolConfigState.Unknown:
                    LolConfigLockerStatusLabel.Text = "Error checking state";
                    LolConfigLockerStatusLabel.Foreground = System.Windows.Media.Brushes.Gray;

                    LolConfigLockerButton.Content = "Error";
                    LolConfigLockerButton.IsEnabled = false;
                    LolConfigLockerButton.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        

        private void OpenAccountManager_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("Anal pa katarina");
                Process.Start("explorer.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

    }

}