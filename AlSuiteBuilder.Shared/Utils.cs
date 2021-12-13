using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.IO;

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

        public static void WriteLog(string message)
        {
            try
            {
                var assemblyDirectory = @"c:\temp\aclog\";
                using (StreamWriter writer = new StreamWriter(System.IO.Path.Combine(assemblyDirectory, "exceptions.txt"), true))
                {
                    writer.WriteLine(message);
                    writer.Close();
                }
            }
            catch { }
        }
        #endregion
    }
}
