namespace KeplerCMS.Areas.MyHabbo.Helpers
{
    /// <summary>The skin ids of the home editor's skin menu, as widget (w_skin_*) or note (n_skin_*) classes.</summary>
    public static class SkinIdToString
    {
        private static readonly string[] Skins =
        {
            null, "defaultskin", "speechbubbleskin", "metalskin", "noteitskin", "notepadskin", "goldenskin", "hc_machineskin", "hc_pillowskin",
        };

        /// <summary>The Habbo Club skins (the menu offers them to club members only).</summary>
        public static bool IsClubSkin(int skinId) => skinId == 7 || skinId == 8;

        public static string Convert(int skinId) => "w_skin_" + Name(skinId);

        public static string ConvertNote(int skinId) => "n_skin_" + Name(skinId);

        private static string Name(int skinId) => skinId >= 1 && skinId < Skins.Length ? Skins[skinId] : Skins[1];
    }
}
