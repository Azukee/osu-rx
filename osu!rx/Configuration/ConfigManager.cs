using WindowsInput.Native;

namespace osu_rx.Configuration
{
    public class ConfigManager
    {
        private SimpleIniConfig config;

        public PlayStyles PlayStyle
        {
            get => config.ReadValue<PlayStyles>("PlayStyle", PlayStyles.Singletap);
            set => config.SetValue<PlayStyles>("PlayStyle", value);
        }

        public VirtualKeyCode PrimaryKey
        {
            get => config.ReadValue<VirtualKeyCode>("PrimaryKey", VirtualKeyCode.VK_Z);
            set => config.SetValue<VirtualKeyCode>("PrimaryKey", value);
        }

        public VirtualKeyCode SecondaryKey
        {
            get => config.ReadValue<VirtualKeyCode>("SecondaryKey", VirtualKeyCode.VK_X);
            set => config.SetValue<VirtualKeyCode>("SecondaryKey", value);
        }

        public VirtualKeyCode HitWindow100Key
        {
            get => config.ReadValue<VirtualKeyCode>("HitWindow100Key", VirtualKeyCode.SPACE);
            set => config.SetValue<VirtualKeyCode>("HitWindow100Key", value);
        }

        public int MaxSingletapBPM
        {
            get => config.ReadValue<int>("MaxSingletapBPM", 250);
            set => config.SetValue<int>("MaxSingletapBPM", value);
        }

        public int AudioOffset
        {
            get => config.ReadValue<int>("AudioOffset", 5);
            set => config.SetValue<int>("AudioOffset", value);
        }

        public bool UseCustomWindowTitle
        {
            get => config.ReadValue<bool>("UseCustomWindowTitle", false);
            set => config.SetValue<bool>("UseCustomWindowTitle", value);
        }

        public string CustomWindowTitle
        {
            get => config.ReadValue<string>("CustomWindowTitle", string.Empty);
            set => config.SetValue<string>("CustomWindowTitle", value);
        }

        public bool EnableHitScan
        {
            get => config.ReadValue<bool>("EnableHitScan", true);
            set => config.SetValue<bool>("EnableHitScan", value);
        }

        //TODO: expose those in next update through settings if nothing changes
        public bool EnableHitScanPrediction
        {
            get => config.ReadValue<bool>("H_EnablePrediction", true);
            set => config.SetValue<bool>("H_EnablePrediction", value);
        }

        public float HitScanRadiusMultiplier
        {
            get => config.ReadValue<float>("H_RadiusMultiplier", 0.9f);
            set => config.SetValue<float>("H_RadiusMultiplier", value);
        }

        public int HitScanRadiusAdditional
        {
            get => config.ReadValue<int>("H_RadiusAdditional", 50);
            set => config.SetValue<int>("H_RadiusAdditional", value);
        }

        public int HitScanMaxDistance
        {
            get => config.ReadValue<int>("H_MaxDistance", 30);
            set => config.SetValue<int>("H_MaxDistance", value);
        }

        public ConfigManager() => config = new SimpleIniConfig();
    }

    public enum PlayStyles
    {
        Singletap,
        Alternate,
        MouseOnly,
        TapX
    }
}
