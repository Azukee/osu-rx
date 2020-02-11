using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace osu_rx.Configuration
{
    public class SimpleIniConfig
    {
        private readonly NumberFormatInfo numberFormat = new CultureInfo(@"en-US", false).NumberFormat;

        private string configFilename = @"config.ini";
        private List<Setting> localConfig = new List<Setting>();

        public SimpleIniConfig()
        {
            if (!File.Exists(configFilename))
                File.Create(configFilename).Close();

            LoadConfig();
        }

        ~SimpleIniConfig() => SaveConfig();

        public T ReadValue<T>(string key, T defaultValue)
        {
            Setting setting = localConfig.Find(s => s.Name == key);
            if (setting != default(Setting))
            {
                if (typeof(T).IsEnum)
                {
                    int intValue = 0;
                    if (int.TryParse(setting.RawValue, out intValue))
                        return (T)(object)intValue;
                    else
                        return (T)Enum.Parse(typeof(T), setting.RawValue, true);
                }
                else
                {
                    switch (Type.GetTypeCode(typeof(T)))
                    {
                        case TypeCode.String:
                            return (T)ReadString(setting.RawValue);
                        case TypeCode.Boolean:
                            return (T)ReadBool(setting.RawValue);
                        case TypeCode.Int32:
                            return (T)ReadInt32(setting.RawValue);
                        case TypeCode.Single:
                            return (T)ReadFloat(setting.RawValue);
                        case TypeCode.Double:
                            return (T)ReadDouble(setting.RawValue);
                        default:
                            return (T)(object)setting.RawValue;
                    }
                }
            }
            else //creating new config key
            {
                localConfig.Add(new Setting(key, defaultValue.ToString()));
                SaveConfig(); //temporary workaround until i figure out a better way of saving config on exit
                return defaultValue;
            }
        }

        private object ReadString(string rawValue) => rawValue;

        private object ReadBool(string rawValue) => bool.Parse(rawValue);

        private object ReadInt32(string rawValue) => int.Parse(rawValue);

        private object ReadFloat(string rawValue) => float.Parse(rawValue, numberFormat);

        private object ReadDouble(string rawValue) => double.Parse(rawValue, numberFormat);

        public void SetValue<T>(string key, T newValue)
        {
            int settingIndex = localConfig.FindIndex(s => s.Name == key);
            if (settingIndex != -1)
                localConfig[settingIndex].RawValue = newValue.ToString();
            else //creating new config key
                localConfig.Add(new Setting(key, newValue.ToString()));
            SaveConfig(); //temporary shit until i figure out a better way
        }

        public void LoadConfig()
        {
            string[] cfgLines = File.ReadAllLines(configFilename);

            foreach (var line in cfgLines)
            {
                string[] split = line.Split('=');
                localConfig.Add(new Setting(split.First().Trim(), split.Last().Trim()));
            }
        }

        public void SaveConfig()
        {
            File.WriteAllText(configFilename, string.Empty);
            foreach (var setting in localConfig)
                File.AppendAllText(configFilename, $"{setting.Name} = {setting.RawValue.ToString(numberFormat)}" + Environment.NewLine);
        }
    }
}
