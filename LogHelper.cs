using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using BT.SaaS.Core.Shared.Entities;

namespace BT.SaaS.MSEOAdapter
{
    public static class LogHelper
    {
        public enum TypeEnum
        {
            MessageTrace = 0,
            ExceptionTrace = 1,
            LogRequest = 2
        };

        public static void Write(string message, TypeEnum traceType)
        {
            try
            {
                if (bool.Parse(ConfigurationManager.AppSettings["isLoggingEnabled"]))
                {
                    if (TypeEnum.MessageTrace == traceType)
                    {
                        Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(message, "MessgaeTrace");
                    }
                    else if (TypeEnum.ExceptionTrace == traceType)
                    {
                        Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(message, "ExceptionTrace");
                    }
                    else if (TypeEnum.LogRequest == traceType)
                    {
                        Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(message, "VirgilLogRequest");
                    }

                }
            }
            catch (Exception ex)
            {
                // ignored
            }
        }
     
        public static void LogActivityDetails(string e2eTransactionId, Guid wfInstanceId, string activityName, TimeSpan activityDuration,string formatter,params object[] wfVariables)
        {
            // Prepare logString            
            string logString = string.Format("{0},ACT-{1},{2}|WFInstanceId:{3},", e2eTransactionId, activityName, activityDuration.TotalSeconds, wfInstanceId);

            if (string.IsNullOrEmpty(formatter))
                formatter = "VASEligilityTrace";

            if (wfVariables != null && wfVariables.Length > 0)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append(logString);

                for (int i = 0; i < wfVariables.Length; i++)
                {
                    sb.Append("WFVar");
                    sb.Append(i);
                    sb.Append(":");

                    if (wfVariables[i] is string)
                        sb.Append(wfVariables[i]);                    
                    sb.Append(",");
                }

                LogMessage(formatter, logString, sb.ToString(), System.Diagnostics.TraceEventType.Verbose);
            }
            else
            {
                LogMessage(formatter, logString, string.Empty, System.Diagnostics.TraceEventType.Information);
            }
        }

        public static void LogWFEventDetails(string e2eTransactionId, string wfName, Guid wfInstanceId, string wfEvent, TimeSpan wfDuration, string wfMessage)
        {
            if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsLoggingEnabledforGetcalls"]))
            {
                // Prepare logString
                string logString = string.Format("{0},WF-{1},{2}|WFInstanceId:{3},WFEvent:{4},", e2eTransactionId, wfName, wfDuration.TotalSeconds, wfInstanceId, wfEvent);

                if (!string.IsNullOrEmpty(wfMessage))
                    logString = string.Concat(logString, ",", "Message:", wfMessage);

                // Log Workflow Events Information in MCIRWFTrace
                LogMessage("VASEligilityTrace", logString, "", System.Diagnostics.TraceEventType.Information);
            }
        }

        /// <summary>
        /// Method used to Log message (as Information) and Xml (as Verbose) in given traceFile name
        /// </summary>
        /// <param name="logMessage">Single line log mesage to be logged as Information</param>
        /// <param name="traceName">TraceFile Name</param>
        /// <param name="logMessageWithXml">Single line log message along with Xml string to be logged as Verbose</param>
        /// <param name="traceType">Trace type (if other than Information to be used)</param>
        private static void LogMessage(string traceName, string logMessage, string logMessageWithXml, System.Diagnostics.TraceEventType logTraceType)
        {
            try
            {
                if (!string.IsNullOrEmpty(logMessage))
                {
                    Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(logMessage, traceName);                  
                }

                if (!string.IsNullOrEmpty(logMessageWithXml))
                {
                    // Default Information Logging
                    logMessageWithXml = logMessageWithXml.Replace("\n", string.Empty).Replace("\r", string.Empty);

                    Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(logMessageWithXml, traceName, -1, 1, System.Diagnostics.TraceEventType.Verbose);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                LogErrorMessage(ex.Message, "Logger.LogMessage"," ");
            }
        }

        /// <summary>
        /// Method used to Log message (as Information) and Xml (as Verbose) in given traceFile name
        /// </summary>
        /// <param name="logString">Single line log mesage to be logged as Information</param>
        /// <param name="traceName">TraceFile Name</param>
        /// <param name="logStringWithXml">Single line log message along with Xml string to be logged as Verbose</param>
        public static void LogErrorMessage(string errorMessage, string bptmTransactionId, string callingFunctionName)
        {
            try
            {
                if (string.IsNullOrEmpty(callingFunctionName))
                    callingFunctionName = "UnKnown";

                if (string.IsNullOrEmpty(bptmTransactionId))
                    bptmTransactionId = "BPTME2eTxnId";

                string logString = string.Format("{0},Ex-{1}|Error:{2},", bptmTransactionId, callingFunctionName, errorMessage);
                Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(logString, "VASEligilityExceptionTrace", -1, 1, System.Diagnostics.TraceEventType.Error);
            }
            catch (Exception ex)
            {
                // Log Exception in Error Trace File
            }
        }

    }
}
