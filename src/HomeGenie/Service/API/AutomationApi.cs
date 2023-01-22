namespace HomeGenie.Service.API
{
    public class Automation
    {
        public static class Groups
        {
            public const string LightsOn
                = "Groups.LightsOn";
            public const string LightsOff
                = "Groups.LightsOff";
        }

        // commonly used commands
        public static class Control
        {
            public const string On
                = "Control.On";
            public const string Off
                = "Control.Off";
            public const string Level
                = "Control.Level";
            public const string ColorHsb
                = "Control.ColorHsb";
            public const string Toggle
                = "Control.Toggle";
        }
        
        // TODO: create constants for all other API commands!! 

    }

    public class Config
    {
        // TODO: ...
    }
}
