using System.Windows;
using System.Windows.Media;

namespace KatHub.KatPlugins
{
    public enum KatPluginsState
    {
        Unknown,
        Crackear,
        Install,
        Update,
        UpToDate,
        MultipleVersions
    }

    public static class KatPluginsStateExtensions
    {
        public static string ToFriendlyString(this KatPluginsState state)
        {
            return state switch
            {
                KatPluginsState.Crackear => "Install",
                KatPluginsState.Install => "Install",
                KatPluginsState.Update => "Update",
                KatPluginsState.UpToDate => "Unistall",
                KatPluginsState.MultipleVersions => "Fix",
                _ => "Desconocido"
            };
        }

        public static Visibility GetMainButtonVisibility(this KatPluginsState state) => state switch
        {
            KatPluginsState.Unknown => Visibility.Collapsed,
            KatPluginsState.Crackear => Visibility.Visible,
            KatPluginsState.Install => Visibility.Visible,
            KatPluginsState.Update => Visibility.Visible,
            KatPluginsState.UpToDate => Visibility.Visible,
            KatPluginsState.MultipleVersions => Visibility.Visible,
            _ => Visibility.Collapsed
        };

        public static string GetStatusText(this KatPluginsState state) => state switch
        {
            KatPluginsState.Unknown => "Error checking state",
            KatPluginsState.Crackear => "",
            KatPluginsState.Install => "Plugin not detected",
            KatPluginsState.Update => $"New version available: v{KatPluginsService.LatestJarVersion}",
            KatPluginsState.UpToDate => $"Running latest version v{KatPluginsService.CurrentKatPluginsVersion} ✔",
            KatPluginsState.MultipleVersions => "Multiple versions detected!",
            _ => ""
        };

        public static Brush GetStatusColor(this KatPluginsState state) => state switch
        {
            KatPluginsState.UpToDate => Brushes.LightGreen,
            KatPluginsState.Unknown => Brushes.Gray,
            KatPluginsState.MultipleVersions => Brushes.Yellow,
            _ => Brushes.LightCoral // Default para Install, Update, Crackear
        };

        public static Visibility GetUninstallVisibility(this KatPluginsState state) => state switch
        {
            KatPluginsState.Unknown => Visibility.Collapsed,
            KatPluginsState.Crackear => Visibility.Collapsed,
            KatPluginsState.Install => Visibility.Visible,
            KatPluginsState.Update => Visibility.Visible,
            KatPluginsState.UpToDate => Visibility.Visible,
            KatPluginsState.MultipleVersions => Visibility.Visible,
            _ => Visibility.Collapsed
        };

        public static Brush GetButtonColor(this KatPluginsState state)
        {
            // Usamos tus variables de color predefinidas
            return state == KatPluginsState.UpToDate ? MainWindow.KatGray : MainWindow.KatRed;
        }
    }
}