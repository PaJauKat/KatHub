namespace KatHub.LolConfig
{
    public enum LolConfigState
    {
        Unknown,
        Locked,
        Unlocked,
    }
    public static class LolConfigStateExtensions
    {
        public static string ToFriendlyString(this LolConfigState state)
        {
            return state switch
            {
                LolConfigState.Locked => "Unlock",
                LolConfigState.Unlocked => "Lock",
                _ => "Desconocido"
            };
        }
    }
}