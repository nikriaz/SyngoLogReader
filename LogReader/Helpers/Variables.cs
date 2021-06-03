using System;
using System.Collections.Generic;
using LogReader.Models;

namespace LogReader.Helpers
{
    public static class Variables
    {
        public static string selectedPath;

        public static IEnumerable<MessageSeverity> severities = new List<MessageSeverity>
        {
                new MessageSeverity() { Name = "Error", Severity = "E" },
                new MessageSeverity() { Name = "Warning", Severity = "W" },
                new MessageSeverity() { Name = "Information", Severity = "I" },
                new MessageSeverity() { Name = "Success", Severity = "S" }
        };

        public static List<(string Extension, byte[] Signature)> knownFilesList = new List<(string, byte[])>
        {
            ("gz", new byte[] { 0x1f, 0x8b, 0x08 }),
            ("zip", new byte[] { 0x50, 0x4b, 0x03 }),
            ("zip", new byte[] { 0x50, 0x4b, 0x05 }),
            ("zip", new byte[] { 0x50, 0x4b, 0x07 })
        };

        public static Dictionary<string, Status> states;

        public static int totalMessages;
        public static DateTime totalMinDt = DateTime.MaxValue;
        public static DateTime totalMaxDt = DateTime.MinValue;
        public static int distinctDays;

    }
}
