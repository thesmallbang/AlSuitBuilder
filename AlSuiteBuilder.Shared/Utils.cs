using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AlSuitBuilder.Shared
{
    public static class Utils
    {
        #region Decal Helpers
        /// <summary>
        /// Attempts to write a message to the ingame chat window.  If there is no character logged in
        /// this will fail.
        /// </summary>
        /// <param name="cmd"></param>
        public static void WriteToChat(string cmd)
        {
            try
            {
                CoreManager.Current.Actions.AddChatText($"[Al] {cmd}", 5);
            }
            catch { }
        }
        #endregion

        /// <summary>
        /// Returns true if a character is logged in
        /// </summary>
        /// <returns></returns>
        public static bool IsLoggedIn()
        {
            try
            {
                return CoreManager.Current.CharacterFilter.LoginStatus > 0;
            }
            catch { }

            return false;
        }

        #region Logging
        /// <summary>
        /// Logs an exception to exceptions.txt file next to the plugin dll
        /// </summary>
        /// <param name="ex">exception to log</param>
        public static void LogException(Exception ex)
        {
            try
            {
                WriteLog(ex.ToString() + "\n");
            }
            catch { }
        }

        public static void WriteLog(string message, bool writeToConsole = false)
        {
            try
            {
                var assemblyDirectory = @"c:\temp\aclog\";
                var msg = $"{DateTime.Now} {message}";
                using (StreamWriter writer = new StreamWriter(System.IO.Path.Combine(assemblyDirectory, "exceptions.txt"), true))
                {
                    writer.WriteLine(msg);
                    writer.Close();
                }
                if (writeToConsole)
                    Console.WriteLine(msg);
            }
            catch { }
        }

        public static void WriteWorkItemToLog(string message, WorkItem workItem, bool writeToConsole = false)
        {
            StringBuilder msg = new StringBuilder();
            AppendMessageLine(msg, 0, $"workItem ({workItem.Id}: {workItem.ItemName} {{");
            AppendMessageLine(msg, 1, $"message: {message}");
            AppendMessageLine(msg, 1, $"materialId: {workItem.MaterialId}");
            AppendMessageLine(msg, 1, $"character: {workItem.Character}");
            AppendMessageLine(msg, 1, $"setId: {workItem.SetId}");
            AppendMessageLine(msg, 1, $"spells: [{string.Join(", ", new List<int>(workItem.Requirements).ConvertAll(r => r.ToString()).ToArray())}]");
            AppendMessageLine(msg, 0, "}");

            WriteLog(msg.ToString(), writeToConsole);
        }

        private static void AppendMessageLine(StringBuilder sb, int tabs, string text)
        {
            for (int i = 0; i < tabs; i++)
                sb.Append('\t');
            sb.AppendLine(text);
        }
        #endregion
    }
}
