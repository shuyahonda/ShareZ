using Windows.System;

namespace ShareZ.Models;

public class HotkeyBinding
{
    public string Name { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public VirtualKeyModifiers Modifiers { get; set; }
    public VirtualKey Key { get; set; }
    public bool IsEnabled { get; set; } = true;

    public string DisplayString
    {
        get
        {
            var parts = new List<string>();
            if (Modifiers.HasFlag(VirtualKeyModifiers.Control)) parts.Add("Ctrl");
            if (Modifiers.HasFlag(VirtualKeyModifiers.Shift)) parts.Add("Shift");
            if (Modifiers.HasFlag(VirtualKeyModifiers.Menu)) parts.Add("Alt");
            if (Modifiers.HasFlag(VirtualKeyModifiers.Windows)) parts.Add("Win");
            parts.Add(Key.ToString());
            return string.Join(" + ", parts);
        }
    }
}

public class UploadDestination
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "Imgur", "Custom", "FTP", "S3"
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string ResponseUrlPattern { get; set; } = string.Empty;
    public string ResponseDeleteUrlPattern { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
