namespace osu_rx.Configuration
{
    public class Setting
    {
        public string Name { get; private set; }

        public string RawValue { get; set; }

        public Setting(string key, string value)
        {
            Name = key;
            RawValue = value;
        }
    }
}
