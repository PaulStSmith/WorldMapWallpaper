namespace DesktopImageChanger
{
    [Flags]
    public enum SPIF
    {
        None = 0x00,
        /// <summary>Writes the new system-wide parameter setting to the user profile.</summary>
        UpdateIniFile = 0x01,
        /// <summary>Broadcasts the WM_SETTINGCHANGE message after updating the user profile.</summary>
        SendChange = 0x02,
    }
}