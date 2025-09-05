using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using System.Diagnostics;
using System.ServiceModel;
using BT.SaaS.EventServiceProxy;
using BT.SaaS.Core.Shared.Entities;
using BT.SaaS.Core.Shared.Attributes;
using System.Collections.Generic;

namespace BT.SaaS.IspssAdapter
{
    public sealed class Logger
    {

        public delegate void EventFlusher(List<Event> lstRequests);

        private Logger()
        { }

        private static string getCallingFunction()
        {
            string callingFunction = null;
            try
            {
                StackTrace stackTrace = new StackTrace(new StackFrame(2, false));
                callingFunction = stackTrace.ToString();
            }
            catch (Exception)
            {
                callingFunction = "";
            }

            return (callingFunction);
        }

        public static void Debug(string message)
        {
            message = getCallingFunction() + message;

            try
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace(message);
            }
            catch (Exception e)
            {
                System.Diagnostics.EventLog.WriteEntry("SAASAdapter", "Failed to publish exception to BT log: " + e.Message + " " + e.StackTrace + " Original message: " + message);
            }
        }

        public static void Publish(Exception exception)
        {
            try
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.Publish(exception);
            }
            catch (Exception e)
            {
                System.Diagnostics.EventLog.WriteEntry("SAASAdapter", "Failed to publish exception to BT log: " + e.Message + " " + e.StackTrace + " Original error: " + exception.Message + " " + exception.StackTrace);
            }
        }

        public static Event GetEvent(string eventName, string orderKey, Order orderData)
        {
            Event eventTrace = new Event();
            eventTrace.EventType = "ISPSS-Placeorder";
            eventTrace.ActivityID = ActionIDs.DEFAULTACTION;
            eventTrace.ServiceProviderID = 1;
            eventTrace.Name = eventName;
            eventTrace.OrderKey = orderKey;
            eventTrace.Status = OrderStatusEnum.@new; //equivalent to "Not Processed" event status
            eventTrace.EventDate = DateTime.Now;

            if (orderData != null)
                eventTrace.Order = orderData;

            return eventTrace;
        }

        public static void RaiseEvent(string eventName, string orderKey, Order orderData)
        {
            try
            {
                Event eventTrace = GetEvent(eventName, orderKey, orderData);
                List<Event> lstRequests = new List<Event>(1);
                lstRequests.Add(eventTrace);
                RaiseEvent(lstRequests);
            }
            catch (Exception generalException)
            {
                //swollow all exceptions here - non critical            
                Publish(generalException);
            }
        }

        public static void FlushEvents(ref List<Event> lstRequest)
        {
            if (lstRequest.Count > 0)
            {
                List<Event> lstRequestHolder = new List<Event>(lstRequest.Count);
                lstRequestHolder.AddRange(lstRequest.ToArray()); //hold incoming data into temporary variable
                lstRequest = new List<Event>(); //clear incoming list
                EventFlusher delegateFlushing = new EventFlusher(RaiseEvent);
                delegateFlushing.BeginInvoke(lstRequestHolder, null, null);
            }
        }

        public static void RaiseEvent(List<Event> lstRequests)
        {
            try
            {
                using (ChannelFactory<IEventService> eventingFactory = new ChannelFactory<IEventService>("httpEventService"))
                {
                    IEventService svcChannel = eventingFactory.CreateChannel();
                    foreach (Event request in lstRequests)
                    {
                        svcChannel.RaiseEvent(request);
                    }
                }
            }
            catch (Exception generalException)
            {
                Publish(generalException);
            }
        }
        public enum TypeEnum
        {
            MessageTrace = 0,
            ExceptionTrace = 1,
            EmailTrace = 2,
            MQNotificationsExceptionTrace = 3,
            SpringMessageTrace = 4,
            SpringExceptionTrace = 5,
            CfsidMessageTrace =6,
            MessageManagerTrace = 7,
            VoInfinityTrace = 8,
            VoInfinityTraceException = 18,
            GetClientProfilefunctinException = 9,
            EESpringMessageTrace=11,
            EESpringExceptionTrace=12,
            HCSWarrantyMessageTrace=13,
            HCSWarrantyExceptionTrace=14,
            BTPlusMarkerMessageTrace=15,
            BTPlusMarkerExceptionTrace=16
        };

        public static void Write(string Message, TypeEnum TraceType)
        {
            try
            {
                if (TypeEnum.MessageTrace == TraceType)
                {
                    Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(Message, "MessgaeTrace");
                }
                else if (TypeEnum.ExceptionTrace == TraceType)
                {
                    Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(Message, "ExceptionTrace");
                }
                else if (TypeEnum.EmailTrace == TraceType)
                {
                    Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(Message, "MailLogTrace");
                }
                else if (TypeEnum.MQNotificationsExceptionTrace == TraceType)
                {
                    Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(Message, "MQNotificationsExceptionTrace");
                }
                else if (TypeEnum.SpringMessageTrace == TraceType)
                {
                    Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(Message, "SpringMessageTrace");
                }
                else if (TypeEnum.SpringExceptionTrace == TraceType)
                {
                    Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(Message, "SpringExceptionTrace");
                }
                else if (TypeEnum.EESpringMessageTrace == TraceType)
                {
                    Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(Message, "EESpringMessageTrace");
                }
                else if (TypeEnum.EESpringExceptionTrace == TraceType)
                {
                    Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(Message, "EESpringExceptionTrace");
                }
                else if(TypeEnum.CfsidMessageTrace == TraceType)
                {
                    Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(Message, "CfsidMessageTrace");
                }
                else if (TypeEnum.MessageManagerTrace == TraceType)
                {
                    Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(Message, "MessageManagerTrace");
                }
                else if (TypeEnum.VoInfinityTrace == TraceType)
                {
                    Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(Message, "VoInfinityTrace");
                }
                else if (TypeEnum.VoInfinityTraceException == TraceType)
                {
                    Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(Message, "VoInfinityTraceException");
                }
                else if (TypeEnum.HCSWarrantyMessageTrace == TraceType)
                {
                    Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(Message, "HCSWarrantyMessageTrace");
                }
                else if (TypeEnum.HCSWarrantyExceptionTrace == TraceType)
                {
                    Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(Message, "HCSWarrantyExceptionTrace");
                }
                else if (TypeEnum.HCSWarrantyMessageTrace == TraceType)
                {
                    Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(Message, "BTPlusMarkerMessageTrace");
                }
                else if (TypeEnum.HCSWarrantyExceptionTrace == TraceType)
                {
                    Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(Message, "BTPlusMarkerExceptionTrace");
                }
            }
            catch (Exception ex)
            {

            }

        }
    }
}
