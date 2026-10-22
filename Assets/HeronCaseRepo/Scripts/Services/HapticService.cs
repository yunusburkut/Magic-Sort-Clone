using Lofelt.NiceVibrations;

// Haptics through Feel's Nice Vibrations presets, tuned per platform (Core Haptics on iOS, amplitude
// control on Android) instead of a raw vibrator buzz.
public static class HapticService
{
    // light tick on tube select
    public static void PlaySelect()
    {
        HapticPatterns.PlayPreset(HapticPatterns.PresetType.Selection);
    }

    // a no on an invalid move, in time with the shake
    public static void PlayInvalid()
    {
        HapticPatterns.PlayPreset(HapticPatterns.PresetType.Warning);
    }

    // soft thud when the poured liquid lands in a tube
    public static void PlayPourLand()
    {
        HapticPatterns.PlayPreset(HapticPatterns.PresetType.SoftImpact);
    }

    // light knock when a tube put back down hits the table
    public static void PlayDropLand()
    {
        HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);
    }

    // a tube is complete
    public static void PlayWin()
    {
        HapticPatterns.PlayPreset(HapticPatterns.PresetType.Success);
    }
}
