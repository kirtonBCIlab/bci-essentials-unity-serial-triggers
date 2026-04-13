using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Text.RegularExpressions;
using Debug = UnityEngine.Debug;

namespace BCIEssentials.SerialTriggers
{
    public static class SerialPortUtilities
    {
        public static string[] GetAvailablePorts()
        {
            try
            {
                return SerialPort.GetPortNames();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to enumerate serial ports: {ex.Message}");
                return Array.Empty<string>();
            }
        }


        public static string[] GetAvailablePortsWithDescriptions()
        {
            string[] ports = GetAvailablePorts();
            if (ports.Length == 0)
                return ports;

            var descriptions = GetPortDeviceNames();
            var result = new string[ports.Length];
            for (int i = 0; i < ports.Length; i++)
            {
                if (descriptions.TryGetValue(ports[i], out string deviceName))
                    result[i] = $"{ports[i]} - {deviceName}";
                else
                    result[i] = ports[i];
            }
            return result;
        }

        public static Dictionary<string, string> GetPortDeviceNames()
        {
            Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            try
            {
                using Process nameListingProcess = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = "-NoProfile -Command \"Get-CimInstance Win32_PnPEntity "
                        + "| Where-Object { $_.Name -match 'COM\\d+' } "
                        + "| ForEach-Object { $_.Name }\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                );
                string output = nameListingProcess.StandardOutput.ReadToEnd();
                nameListingProcess.WaitForExit(2000);

                foreach (var line in output.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    var match = Regex.Match(trimmed, @"\((COM\d+)\)");
                    if (match.Success)
                    {
                        string port = match.Groups[1].Value;
                        string description = trimmed.Replace(match.Value, "");
                        result[port] = description.Trim();
                    }
                }
            }
            catch { }
#endif
            return result;
        }
    }
}