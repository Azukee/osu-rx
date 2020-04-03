using SimpleIniConfig;
using WindowsInput.Native;

namespace osu_rx.Configuration
{
    public class ConfigManager
    {
        private Config config;

        public PlayStyles PlayStyle
        {
            get => config.GetValue<PlayStyles>("PlayStyle", PlayStyles.Singletap);
            set => config.SetValue<PlayStyles>("PlayStyle", value);
        }

        public VirtualKeyCode PrimaryKey
        {
            get => config.GetValue<VirtualKeyCode>("PrimaryKey", VirtualKeyCode.VK_Z);
            set => config.SetValue<VirtualKeyCode>("PrimaryKey", value);
        }

        public VirtualKeyCode SecondaryKey
        {
            get => config.GetValue<VirtualKeyCode>("SecondaryKey", VirtualKeyCode.VK_X);
            set => config.SetValue<VirtualKeyCode>("SecondaryKey", value);
        }

        public VirtualKeyCode HitWindow100Key
        {
            get => config.GetValue<VirtualKeyCode>("HitWindow100Key", VirtualKeyCode.SPACE);
            set => config.SetValue<VirtualKeyCode>("HitWindow100Key", value);
        }

        public int MaxSingletapBPM
        {
            get => config.GetValue<int>("MaxSingletapBPM", 250);
            set => config.SetValue<int>("MaxSingletapBPM", value);
        }

        public int AudioOffset
        {
            get => config.GetValue<int>("AudioOffset", 0);
            set => config.SetValue<int>("AudioOffset", value);
        }

        public bool UseCustomWindowTitle
        {
            get => config.GetValue<bool>("UseCustomWindowTitle", false);
            set => config.SetValue<bool>("UseCustomWindowTitle", value);
        }

        public string CustomWindowTitle
        {
            get => config.GetValue<string>("CustomWindowTitle", string.Empty);
            set => config.SetValue<string>("CustomWindowTitle", value);
        }

        public bool EnableHitScan
        {
            get => config.GetValue<bool>("EnableHitScan", true);
            set => config.SetValue<bool>("EnableHitScan", value);
        }
        
        public bool HoldBeforeSpinner
        {
            get => config.GetValue<bool>("HoldBeforeSpinner", true);
            set => config.SetValue<bool>("HoldBeforeSpinner", value);
        }
        
        //TODO: expose those in next update through settings if nothing changes
        public bool EnableHitScanPrediction
        {
            get => config.GetValue<bool>("H_EnablePrediction", true);
            set => config.SetValue<bool>("H_EnablePrediction", value);
        }

        public float HitScanRadiusMultiplier
        {
            get => config.GetValue<float>("H_RadiusMultiplier", 0.9f);
            set => config.SetValue<float>("H_RadiusMultiplier", value);
        }

        public int HitScanRadiusAdditional
        {
            get => config.GetValue<int>("H_RadiusAdditional", 50);
            set => config.SetValue<int>("H_RadiusAdditional", value);
        }

        public int HitScanMaxDistance
        {
            get => config.GetValue<int>("H_MaxDistance", 30);
            set => config.SetValue<int>("H_MaxDistance", value);
        }

        public ConfigManager() => config = new Config();
    }

    public enum PlayStyles
    {
        Singletap,
        Alternate,
        MouseOnly,
        TapX
    }
}
