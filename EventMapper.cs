using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BT.SaaS.Core.Shared.Entities;
using MSEO = BT.SaaS.MSEOAdapter;
using  SAASISV = BT.Core.ISVAdapter.ISVService.Entities;
using System.Configuration;
using BT.SaaS.IspssAdapter;

namespace BT.SaaS.MSEOAdapter
{
    public static class EventMapper
    {
        public static Event FrameEvent(OrderRequest requestOrderRequest, string eventType, OrderStatusEnum status, string req)
        {
            string isvOrd = string.Empty;
            const string ExecuteRequest = "Executing CallToISVAdpater DNP - 9- Correlation :0";
            const string ExecuteSuccessResponse = "Executing Response From ISVAdpater DNP - 9- Correlation :0 - Response Status :True";
            const string ExecuteFailedResponse = "Executing Response From ISVAdpater DNP - 9- Correlation :0 - Response Status :False";
            const string ExceptionOccurred = "Exception Occured  at MSEO while framing request to DNP";
            const int ReqActivityID = 11101;
            const int ResActivityID = 11100;
            Event evnt = null;
            try
            {
                evnt = new BT.SaaS.Core.Shared.Entities.Event();
                evnt.ID = 0;
                evnt.EventDate = System.DateTime.Now;
                if (eventType.Equals("ExecuteRequest", StringComparison.OrdinalIgnoreCase))
                {
                    evnt.EventType = ExecuteRequest;
                    evnt.ActivityID = ReqActivityID;
                }
                else if (eventType.Equals("ExecuteSuccessResponse", StringComparison.OrdinalIgnoreCase))
                {
                    evnt.EventType = ExecuteSuccessResponse;
                    evnt.ActivityID = ResActivityID;
                }
                else if (eventType.Equals("ExecuteFailedResponse", StringComparison.OrdinalIgnoreCase))
                {
                    evnt.EventType = ExecuteFailedResponse;
                    evnt.ActivityID = ResActivityID;
                }
                else if (eventType.Equals("ExceptionOccurred", StringComparison.OrdinalIgnoreCase))
                {
                    evnt.EventType = ExceptionOccurred;
                    evnt.ActivityID = ResActivityID;
                }

                evnt.Status = status;
                evnt.OrderID = new Guid(requestOrderRequest.Order.OrderIdentifier.Value);

                evnt.OrderKey = requestOrderRequest.Order.OrderIdentifier.Value;
                if (eventType.Equals("ExecuteRequest", StringComparison.OrdinalIgnoreCase))
                {
                    isvOrd = PrepareISVorder(requestOrderRequest, evnt.OrderID, eventType, status.ToString());
                    evnt.EventData.Add("Data", isvOrd);
                }
                else
                {
                    evnt.EventData.Add("Data", req);

                }
                evnt.Order = new BT.SaaS.Core.Shared.Entities.Order();
                evnt.Order.Header.OrderKey = requestOrderRequest.Order.OrderIdentifier.Value;
                evnt.Order.Header.OrderID = evnt.OrderID;
                evnt.Order.Header.ServiceProviderID = Settings1.Default.ConsumerServiceProviderId;
                evnt.Order.Header.Status = status;
            }
            catch(Exception ex)
            {
                throw ex;
            }
            return evnt;


        }
        public static string PrepareISVorder(OrderRequest requestOrderRequest, Guid orderID, string eventype,string status)
        {
            const string DNPServiceCode = "DNP";
            const string DNPServiceID = "9";
            SAASISV.ISVOrder ISVOrder = null;
            try
            {
                ISVOrder = new BT.Core.ISVAdapter.ISVService.Entities.ISVOrder();
                ISVOrder.Header = new BT.Core.ISVAdapter.ISVService.Entities.ISVOrderHeader();
                ISVOrder.Header.OrderId = orderID;
                ISVOrder.Header.Status = status;
                ISVOrder.Header.EffectiveDateTime = System.DateTime.Now;
                ISVOrder.Header.OrderDateTime = System.DateTime.Now;
                ISVOrder.Header.OrderKey = requestOrderRequest.Order.OrderIdentifier.Value;

                ISVOrder.ServiceActionOrderItem = new BT.Core.ISVAdapter.ISVService.Entities.ServiceActionOrderItem();
                ISVOrder.ServiceActionOrderItem.XmlIdKey = "0";
                ISVOrder.ServiceActionOrderItem.Header.ServiceAction = BT.Core.ISVAdapter.ISVService.Entities.ServiceActionEnum.createClient;
                ISVOrder.ServiceActionOrderItem.Header.RequestAction = BT.Core.ISVAdapter.ISVService.Entities.ServiceRequestedActionEnum.provide;
                ISVOrder.ServiceActionOrderItem.Header.Status = BT.Core.ISVAdapter.ISVService.Entities.DataStatusEnum.notDone;
                ISVOrder.ServiceActionOrderItem.Header.ServiceCode = DNPServiceCode;
                ISVOrder.ServiceActionOrderItem.Header.ServiceId = DNPServiceID;
                ISVOrder.ServiceActionOrderItem.Header.ServiceEndPointUrl = ConfigurationManager.AppSettings["DnPEPUrl"];
                ISVOrder.ServiceActionOrderItem.Header.CorrelationId = "0";
                ISVOrder.ServiceActionOrderItem.IsvResponse = new BT.Core.ISVAdapter.ISVService.Entities.ISVResponse();

                if (new string[] { "ExecuteRequest", "ExecuteFailedResponse" }.Contains(eventype))
                {
                    ISVOrder.ServiceActionOrderItem.IsvResponse.ResponseCode = false;
                }
                else
                {
                    ISVOrder.ServiceActionOrderItem.IsvResponse.ResponseCode = true;
                }

                ISVOrder.ServiceActionOrderItem.ServiceRoles = new List<BT.Core.ISVAdapter.ISVService.Entities.ServiceRole>();

                SAASISV.ServiceRole srvcRole = new BT.Core.ISVAdapter.ISVService.Entities.ServiceRole();
                srvcRole.RoleInstanceXmlRef = "RI-0";
                srvcRole.RoleType = "ADMIN";
                srvcRole.roleAttributes = new List<BT.Core.ISVAdapter.ISVService.Entities.Attribute>();

                foreach (MSEO.Instance instance in requestOrderRequest.Order.OrderItem[0].Instance)
                {
                    foreach (MSEO.InstanceCharacteristic instanceCharacteristic in instance.InstanceCharacteristic)
                    {
                        SAASISV.Attribute attribute = new BT.Core.ISVAdapter.ISVService.Entities.Attribute();
                        attribute.action = BT.Core.ISVAdapter.ISVService.Entities.DataActionEnum.add;
                        attribute.Name = instanceCharacteristic.Name;
                        attribute.Value = instanceCharacteristic.Value;
                        srvcRole.roleAttributes.Add(attribute);
                    }
                }
                ISVOrder.ServiceActionOrderItem.ServiceRoles.Add(srvcRole);
            }
            catch (Exception exp)
            {
                throw exp;
            }

            return ISVOrder.ToString();
        }

        public static void RaiseEvnt(OrderRequest requestOrderRequest, string eventType , OrderStatusEnum status, string req)
        {
            try
            {
                Event evnt = FrameEvent(requestOrderRequest, eventType, status, req);
                List<Event> lstEvnt = new List<BT.SaaS.Core.Shared.Entities.Event>();
                lstEvnt.Add(evnt);
                Logger.RaiseEvent(lstEvnt);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
