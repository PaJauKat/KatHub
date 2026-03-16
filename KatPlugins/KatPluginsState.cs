namespace KatHub.KatPlugins
{
    public enum KatPluginsState
    {
        Unknown,
        Crackear,
        Install,
        Update,
        UpToDate
    }

    public static class KatPluginsStateExtensions
    {
        public static string ToFriendlyString(this KatPluginsState state)
        {
            return state switch
            {
                KatPluginsState.Crackear => "Crackear",
                KatPluginsState.Install => "Install",
                KatPluginsState.Update => "Update",
                KatPluginsState.UpToDate => "Unistall",
                _ => "Desconocido"
            };
        }
    }
}