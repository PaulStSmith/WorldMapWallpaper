namespace WorldMapWallpaper
{
    /// <summary>
    /// Flags to specify how the system parameter information function (SPI) applies changes.
    /// </summary>
    [Flags]
    public enum SPIF
    {
        /// <summary>
        /// No flags.
        /// </summary>
        None = 0x00,

        /// <summary>
        /// Writes the new system-wide parameter setting to the user profile.
        /// </summary>
        UpdateIniFile = 0x01,

        /// <summary>
        /// Broadcasts the WM_SETTINGCHANGE message after updating the user profile.
        /// </summary>
        SendChange = 0x02,
    }
}