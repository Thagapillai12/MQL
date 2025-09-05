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
using MSEO = BT.SaaS.MSEOAdapter;
using IBM.WMQ;
using System.Collections;
using System.Text;
using System.Net;
using System.Xml;
using BT.SaaS.IspssAdapter;
using com.bt.util.logging;

namespace BT.SaaS.MSEOAdapter
{
    public class MSEOOrderNotification
    {
        MSEO.OrderNotification orderNotification;
        MSEO.OrderRequest orderRequest;

        private MSEO.OrderNotification orderNotificationResponse;

        /// <summary>
        /// 
        /// </summary>
        public MSEO.OrderNotification NotificationResponse
        {
            get
            {
                return orderNotificationResponse;
            }
            set
            {
                orderNotificationResponse = value;
            }
        }

        public MSEOOrderNotification()
        {

        }

        public MSEOOrderNotification(OrderRequest orderRequest)
        {
            this.orderRequest = orderRequest;
        }

        public void sendNotification(bool requestStatus, bool isAck, string errorCode, string errorDescription, ref E2ETransaction myE2ETx, bool isEndInbound, System.DateTime? ActivityStartTime = null, Guid guid = new Guid(), string bptmTxnId = null)
        {
            MessagesFile Mf = new MessagesFile(ConfigurationManager.AppSettings["logMessagesFilename"].ToString());
            string orderStatus = string.Empty;
            string bacId = string.Empty;
            string sourceBac = string.Empty;
            string targetBac = string.Empty;
            string emailName = string.Empty;
            string MseoSentNotification = "MseoReceivedNotificationSend";
            string MseoBeforeCallingMessageQueue = "MseoBeforeCallingMessageQueue";
            orderNotification = new OrderNotification();

            orderNotification.Order = new Order[1];
            orderNotification.Order[0] = orderRequest.Order;
            orderNotification.StandardHeader = this.orderRequest.StandardHeader;

            //set the notification 
            orderNotification.Notification = new Notification();
            if (requestStatus)
            {
                orderNotification.Notification.Code = Settings1.Default.SucessCode;
                if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals("S0145887", StringComparison.InvariantCultureIgnoreCase))
                {
                    orderNotification.Order[0].Error = null;
                }
            }
            else
            {
                orderNotification.Notification.Code = Settings1.Default.FailedCode;
                orderNotification.Order[0].Error = new Error();
                orderNotification.Order[0].Error.Code = errorCode;
                orderNotification.Order[0].Error.Description = errorDescription;
            }

            //set the status at order level
            if (isAck)
            {
                if (requestStatus)
                {
                    orderNotification.Order[0].Status = Settings1.Default.AcceptedStatus;

                    foreach (OrderItem item in orderNotification.Order[0].OrderItem)
                    {

                        if (item.Specification[0].Identifier.Value1.Equals("S0145887", StringComparison.InvariantCultureIgnoreCase))                           
                        {
                            item.Error = null;
                        }

                        if (item.Status != Settings1.Default.IgnoredStatus)
                        {
                            item.Status = Settings1.Default.AcceptedStatus;
                        }
                    }
                }
                else
                {

                    if (errorCode == Settings1.Default.InvalideRequestCode)
                    {
                        orderNotification.Order[0].Status = Settings1.Default.IgnoredStatus;
                    }
                    else
                    {
                        orderNotification.Order[0].Status = Settings1.Default.RejectedStatus;
                    }
                    foreach (OrderItem item in orderNotification.Order[0].OrderItem)
                    {
                        if (item.Status != Settings1.Default.IgnoredStatus)
                        {
                            item.Status = Settings1.Default.RejectedStatus;
                        }
                    }
                }
            }
            else
            {
                if (requestStatus)
                {
                    orderNotification.Order[0].Status = Settings1.Default.CompleteStatus;
              
                    foreach (OrderItem item in orderNotification.Order[0].OrderItem)
                    {                       
                        if (item.Specification[0].Identifier.Value1.Equals("S0145887", StringComparison.InvariantCultureIgnoreCase))
                        {
                            item.Error = null;
                        }
                        //set the error code as 004 for esb robt deactivate completed order 
                        //and checking if not EESpring request.
                        else if (orderNotification.StandardHeader.ServiceAddressing.From.Contains(Settings1.Default.ESBQueueString)
                             && !(orderNotification.Order[0].OrderIdentifier.Value.StartsWith("EE",StringComparison.OrdinalIgnoreCase)))
                        {               
                            item.Error.Code = errorCode;
                        }
                        if (item.Status != Settings1.Default.IgnoredStatus)
                        {
                            item.Status = Settings1.Default.CompleteStatus;
                        }

                    }
                }
                else
                {
                    if (errorCode == Settings1.Default.InvalideRequestCode)
                    {
                        orderNotification.Order[0].Status = Settings1.Default.IgnoredStatus;
                        foreach (OrderItem item in orderNotification.Order[0].OrderItem)
                        {
                            item.Status = Settings1.Default.IgnoredStatus;
                        }
                    }
                    else
                    {
                        orderNotification.Order[0].Status = Settings1.Default.FailedStatus;
                        foreach (OrderItem item in orderNotification.Order[0].OrderItem)
                        {
                            if (item.Status != Settings1.Default.IgnoredStatus)
                            {
                                item.Status = Settings1.Default.FailedStatus;
                            }

                        }
                    }
                }
            }


            if (orderRequest.StandardHeader.ServiceAddressing.From != null && !orderRequest.StandardHeader.ServiceAddressing.From.Equals(Settings1.Default.BTComstring)
                && !orderRequest.StandardHeader.ServiceAddressing.From.Equals(Settings1.Default.DanteEmailString)
                && !orderRequest.StandardHeader.ServiceAddressing.From.Equals(Settings1.Default.NativeClientString, StringComparison.OrdinalIgnoreCase)
                && !orderRequest.StandardHeader.ServiceAddressing.From.Equals(Settings1.Default.MCSOString, StringComparison.OrdinalIgnoreCase)
                && !orderRequest.StandardHeader.ServiceAddressing.From.Equals(Settings1.Default.DEVAPIServiceAddress, StringComparison.OrdinalIgnoreCase)
                && !orderRequest.StandardHeader.ServiceAddressing.From.Equals(Settings1.Default.BUTString, StringComparison.OrdinalIgnoreCase)
                && !orderRequest.StandardHeader.ServiceAddressing.From.Equals(Settings1.Default.adminToolString, StringComparison.OrdinalIgnoreCase)
                && !orderRequest.StandardHeader.ServiceAddressing.From.Equals(Settings1.Default.ESBAPIServiceAddress, StringComparison.OrdinalIgnoreCase))
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : Sending response to " + orderRequest.StandardHeader.ServiceAddressing.From);
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : orderNotification header " + orderNotification.StandardHeader.SerializeObject());

                if (orderRequest.Order.Status != null && !String.IsNullOrEmpty(orderRequest.Order.Status))
                {
                    orderStatus = orderRequest.Order.Status.ToString();
                }
                if (orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)) && !String.IsNullOrEmpty(orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
                {
                    bacId = orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                }
                if (orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("sourcebillaccountnumber", StringComparison.OrdinalIgnoreCase)) && !String.IsNullOrEmpty(orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("sourcebillaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
                {
                    sourceBac = orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("sourcebillaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                }
                if (orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("targetbillaccountnumber", StringComparison.OrdinalIgnoreCase)) && !String.IsNullOrEmpty(orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("targetbillaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
                {
                    targetBac = orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("targetbillaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                }
                if (orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("emailname", StringComparison.OrdinalIgnoreCase)) && !String.IsNullOrEmpty(orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("emailname", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
                {
                    emailName = orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("emailname", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                }
                if (!(orderNotification.StandardHeader.E2e != null && orderNotification.StandardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(orderNotification.StandardHeader.E2e.E2EDATA)))
                {
                    orderNotification.StandardHeader.E2e = new E2E();
                }

                if (orderRequest.StandardHeader.ServiceAddressing.From.StartsWith(Settings1.Default.MQstring))
                {
                    myE2ETx.logMessage(MseoBeforeCallingMessageQueue, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; BAC<" + bacId + ">; OrderStatus<" + orderStatus + ">;");
                    if (isAck)
                        myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationOfs"].ToString(), E2ETransaction.vACK, "Order " + orderNotification.Order[0].Status);
                    else
                        myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationOfs"].ToString(), E2ETransaction.vRES, "Order " + orderNotification.Order[0].Status);
                    orderNotification.StandardHeader.E2e.E2EDATA = myE2ETx.toString();
                    
                    if (!orderNotification.Order[0].OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("vasfinternalmqtestingflag")))
                    {
                        writeToMQ();
                    }

                    // log the MQ out if the Thomas Request with create action
                    //if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSportsProductCode"], StringComparison.OrdinalIgnoreCase) && (orderNotification.Order[0].OrderItem[0].Action.Code.ToLower() == "create" || orderNotification.Order[0].OrderItem[0].Action.Code.ToLower() == "modify"))
                    if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSportsProductCode"], StringComparison.OrdinalIgnoreCase))
                    {
                        if (ConfigurationManager.AppSettings["isThomasNewJourneySwitch"].ToLower() == "true")
                        {
                            string orderKey = orderNotification.Order[0].OrderIdentifier.Value;
                            //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderKey, orderNotification.SerializeObject(), ConfigurationManager.AppSettings["BTSportsProductCode"]);
                            if (isAck)
                                LogHelper.LogActivityDetails(bptmTxnId, guid, "Accepted notification sent to OFS: ", (System.TimeSpan)(System.DateTime.Now - ActivityStartTime), "ThomasTrace", orderNotification.SerializeObject());
                            else if(errorCode.Equals("004"))
                                LogHelper.LogActivityDetails(bptmTxnId, guid, "Complete notification sent to OFS: ", (System.TimeSpan)(System.DateTime.Now - ActivityStartTime), "ThomasTrace", orderNotification.SerializeObject());
                            else
                                LogHelper.LogActivityDetails(bptmTxnId, guid, "Failure notification sent to OFS: ", (System.TimeSpan)(System.DateTime.Now - ActivityStartTime), "ThomasTrace", orderNotification.SerializeObject());
                        }
                    }
                    //log the MQ out if the Spring Request
                    if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"], StringComparison.OrdinalIgnoreCase)
                        && orderNotification.Order[0].OrderItem[0].Action.Code.ToLower() != "cancel")
                    {
                        if (ConfigurationManager.AppSettings["isSpringNewJourneySwitch"].ToLower() == "true")
                        {
                            string orderKey = orderNotification.Order[0].OrderIdentifier.Value;
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderKey, orderNotification.SerializeObject(), ConfigurationManager.AppSettings["BTSpringProductCode"]);
                        }
                    }
                    //log the MQ out if the HCSWarranty Request
                    if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["HCSProductCode"], StringComparison.OrdinalIgnoreCase))                       
                    {
                        if (ConfigurationManager.AppSettings["isHCSWarrantyNewJourneySwitch"].ToLower() == "true")
                        {
                            string orderKey = orderNotification.Order[0].OrderIdentifier.Value;
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderKey, orderNotification.SerializeObject(), ConfigurationManager.AppSettings["HCSProductCode"]);
                        }
                    }
                    //log the MQ out if the BTPlusMarker Request
                    if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTPlusMarkerProductCode"], StringComparison.OrdinalIgnoreCase)||
                        orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["AssociateBTPlusMarkerProductCode"], StringComparison.OrdinalIgnoreCase))
                    {
                        if (ConfigurationManager.AppSettings["isBTPlusMarkerNewJourneySwitch"].ToLower() == "true")
                        {
                            string orderKey = orderNotification.Order[0].OrderIdentifier.Value;
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderKey, orderNotification.SerializeObject(), ConfigurationManager.AppSettings["BTPlusMarkerProductCode"]);
                        }
                    }
                    //log the MQ out if the Voice Request
                    if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals("S0341469", StringComparison.OrdinalIgnoreCase))
                    {
                        //if (ConfigurationManager.AppSettings["isSpringNewJourneySwitch"].ToLower() == "true")
                        {
                            string orderKey = orderNotification.Order[0].OrderIdentifier.Value;
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderKey, orderNotification.SerializeObject(), "S0341469");
                        }
                    }
                    if (orderNotification.Order[0].OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("ServiceCode", StringComparison.OrdinalIgnoreCase) && (ic.Value.Equals("BT_CONSUMER_BROADBAND", StringComparison.OrdinalIgnoreCase))))
                    {
                        
                        string orderKey = orderNotification.Order[0].OrderIdentifier.Value;
                        LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), orderKey + ": sent notification to OFS: ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", orderNotification.SerializeObject());
                        //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderKey, orderNotification.SerializeObject(), "BTConsumerBroadBand");
                    }

                    if (ConfigurationManager.AppSettings["isMQNotificationLoggingEnabled"].Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!((orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"], StringComparison.OrdinalIgnoreCase)) ||
                            (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSportsProductCode"], StringComparison.OrdinalIgnoreCase))))
                        {
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderNotification.Order[0].OrderIdentifier.Value.ToString(), orderNotification.SerializeObject(), "MQNotification");
                        }
                    }

                    myE2ETx.logMessage(MseoSentNotification, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; BAC<" + bacId + ">; OrderStatus<" + orderStatus + ">;");
                    myE2ETx.endOutboundCall(null);
                    if(isEndInbound)
                        myE2ETx.endInboundCall();
                }

                else if (orderRequest.StandardHeader.ServiceAddressing.From.StartsWith(Settings1.Default.OVString, StringComparison.OrdinalIgnoreCase))
                {
                    myE2ETx.logMessage(MseoBeforeCallingMessageQueue, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; SOURCEBACID<" + sourceBac + ">; TARGETBACID<" + targetBac + ">; OrderStatus<" + orderStatus + ">;");
                    if (isAck)
                        myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationOneView"].ToString(), E2ETransaction.vACK, "Order " + orderNotification.Order[0].Status);
                    else
                        myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationOneView"].ToString(), E2ETransaction.vRES, "Order " + orderNotification.Order[0].Status);

                    orderNotification.StandardHeader.E2e.E2EDATA = myE2ETx.toString();
                    writeToOV();
                    if (ConfigurationManager.AppSettings.AllKeys.Contains("isMQNotificationLoggingEnabled") && ConfigurationManager.AppSettings["isMQNotificationLoggingEnabled"].Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderNotification.Order[0].OrderIdentifier.Value.ToString(), orderNotification.SerializeObject(), "MQNotification");
                    }
                    myE2ETx.logMessage(MseoSentNotification, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; SOURCEBACID<" + sourceBac + ">; TARGETBACID<" + targetBac + ">; OrderStatus<" + orderStatus + ">;");
                    myE2ETx.endOutboundCall(null);
                    if (isEndInbound)
                        myE2ETx.endInboundCall();
                }
                else if (orderRequest.StandardHeader.ServiceAddressing.From.StartsWith(Settings1.Default.ESBQueueString, StringComparison.OrdinalIgnoreCase))
                {
                    string orderKey = orderNotification.Order[0].OrderIdentifier.Value;
                    if (orderKey.StartsWith("EE", StringComparison.OrdinalIgnoreCase) || (ConfigurationManager.AppSettings["isSpringNewJourneySwitch"].ToLower() == "true"))
                    {
                        myE2ETx.logMessage(MseoBeforeCallingMessageQueue, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; BAC<" + bacId + ">; OrderStatus<" + orderStatus + ">;");
                        if (isAck)
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationESBMQ"].ToString(), E2ETransaction.vACK, "Order " + orderNotification.Order[0].Status);
                        else
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationESBMQ"].ToString(), E2ETransaction.vRES, "Order " + orderNotification.Order[0].Status);
                        orderNotification.StandardHeader.E2e.E2EDATA = myE2ETx.toString();
                        writeToESBMQ();
                        //log the ESBMQ out if the Spring Request
                        Logger.Write(orderKey + "," + " Sending Response to ESB : " + "," + orderNotification.SerializeObject(), Logger.TypeEnum.EESpringExceptionTrace);

                        if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"], StringComparison.OrdinalIgnoreCase)
                            && orderNotification.Order[0].OrderItem[0].Action.Code.ToLower() != "cancel")
                        {
                            if (ConfigurationManager.AppSettings["isSpringNewJourneySwitch"].ToLower() == "true")
                            {
                                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderKey, orderNotification.SerializeObject(), ConfigurationManager.AppSettings["BTSpringProductCode"]);
                            }
                        }

                        if (ConfigurationManager.AppSettings.AllKeys.Contains("isMQNotificationLoggingEnabled") && ConfigurationManager.AppSettings["isMQNotificationLoggingEnabled"].Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!(orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"], StringComparison.OrdinalIgnoreCase)))
                            {
                                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderNotification.Order[0].OrderIdentifier.Value.ToString(), orderNotification.SerializeObject(), "MQNotification");
                            }
                        }

                        myE2ETx.logMessage(MseoSentNotification, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; BAC<" + bacId + ">; OrderStatus<" + orderStatus + ">;");
                        myE2ETx.endOutboundCall(null);
                        if (isEndInbound)
                            myE2ETx.endInboundCall();
                    }
                    else
                    {
                        myE2ETx.logMessage(MseoBeforeCallingMessageQueue, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; EmailName<" + emailName + ">; OrderStatus<" + orderStatus + ">;");
                        if (isAck)
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationESB"].ToString(), E2ETransaction.vACK, "Order " + orderNotification.Order[0].Status);
                        else
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationESB"].ToString(), E2ETransaction.vRES, "Order " + orderNotification.Order[0].Status);

                        orderNotification.StandardHeader.E2e.E2EDATA = myE2ETx.toString();
                        writeToESB();

                        if (ConfigurationManager.AppSettings.AllKeys.Contains("isMQNotificationLoggingEnabled") && ConfigurationManager.AppSettings["isMQNotificationLoggingEnabled"].Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderNotification.Order[0].OrderIdentifier.Value.ToString(), orderNotification.SerializeObject(), "MQNotification");
                        }
                        myE2ETx.logMessage(MseoSentNotification, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; EmailName<" + emailName + ">; OrderStatus<" + orderStatus + ">;");
                        myE2ETx.endOutboundCall(null);
                        if (isEndInbound)
                            myE2ETx.endInboundCall();
                    }
                }
                else if (orderRequest.StandardHeader.ServiceAddressing.From.Equals(ConfigurationManager.AppSettings["BBMTServiceAddress"], StringComparison.OrdinalIgnoreCase))
                {
                    if (ConfigurationManager.AppSettings["BTIEmailErrorCodes"].Split(',').Contains(errorCode))
                    {
                        if (isAck)
                        {
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["BBMTServiceAddress"].ToString(), E2ETransaction.vACK, orderStatus);
                        }
                        else
                        {
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["BBMTServiceAddress"].ToString(), E2ETransaction.vRES, orderStatus);
                        }
                        orderNotification.StandardHeader.E2e.E2EDATA = myE2ETx.toString();
                        if (!orderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["VasContentFilteringProductFamily"], StringComparison.OrdinalIgnoreCase))
                        {
                            orderNotification.Order[0].OrderItem[0].HoldToTerm = null;
                            callOrderNotificationWS();
                        }
                        myE2ETx.logMessage(MseoSentNotification, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; BAC<" + bacId + ">; EMAILNAME<" + emailName + ">; OrderStatus<" + orderStatus + ">;");
                        myE2ETx.endOutboundCall(null);
                    }
                }
                else
                {
                    if (isAck)
                    {
                        if (orderRequest.Order.OrderItem.ToList().Exists(oi => oi.Specification[0].Identifier.Value1.Equals("S0145887", StringComparison.OrdinalIgnoreCase)))
                        {
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationRIOSS"].ToString(), E2ETransaction.vACK, orderStatus);
                        }
                        else
                        {
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationHestia"].ToString(), E2ETransaction.vACK, orderStatus);
                        }
                    }
                    else
                    {
                        if (orderRequest.Order.OrderItem.ToList().Exists(oi => oi.Specification[0].Identifier.Value1.Equals("S0145887", StringComparison.OrdinalIgnoreCase)))
                        {
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationRIOSS"].ToString(), E2ETransaction.vRES, orderStatus);
                        }
                        else
                        {
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationHestia"].ToString(), E2ETransaction.vRES, orderStatus);
                        }
                    }

                    orderNotification.StandardHeader.E2e.E2EDATA = myE2ETx.toString();
                    orderNotification.Order[0].OrderItem[0].HoldToTerm = null;
                    callOrderNotificationWS();

                    myE2ETx.logMessage(MseoSentNotification, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; BAC<" + bacId + ">; OrderStatus<" + orderStatus + ">;");
                    myE2ETx.endOutboundCall(null);
                }
            }

            this.NotificationResponse = orderNotification;
        }

        public void sendNotification(bool requestStatus, bool isAck, string errorCode, string errorDescription, ref E2ETransaction myE2ETx)
        {
            MessagesFile Mf = new MessagesFile(ConfigurationManager.AppSettings["logMessagesFilename"].ToString());
            string orderStatus = string.Empty;
            string bacId = string.Empty;
            string sourceBac = string.Empty;
            string targetBac = string.Empty;
            string emailName = string.Empty;
            string MseoSentNotification = "MseoReceivedNotificationSend";
            string MseoBeforeCallingMessageQueue = "MseoBeforeCallingMessageQueue";
            orderNotification = new OrderNotification();

            orderNotification.Order = new Order[1];
            orderNotification.Order[0] = orderRequest.Order;
            orderNotification.StandardHeader = this.orderRequest.StandardHeader;

            //set the notification 
            orderNotification.Notification = new Notification();
            if (requestStatus)
            {
                orderNotification.Notification.Code = Settings1.Default.SucessCode;
                if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals("S0145887", StringComparison.InvariantCultureIgnoreCase))
                {
                    orderNotification.Order[0].Error = null;
                }
            }
            else
            {
                orderNotification.Notification.Code = Settings1.Default.FailedCode;
                orderNotification.Order[0].Error = new Error();
                orderNotification.Order[0].Error.Code = errorCode;
                orderNotification.Order[0].Error.Description = errorDescription;
            }

            //set the status at order level
            if (isAck)
            {
                if (requestStatus)
                {
                    orderNotification.Order[0].Status = Settings1.Default.AcceptedStatus;

                    foreach (OrderItem item in orderNotification.Order[0].OrderItem)
                    {

                        if (item.Specification[0].Identifier.Value1.Equals("S0145887", StringComparison.InvariantCultureIgnoreCase))
                        {
                            item.Error = null;
                        }

                        if (item.Status != Settings1.Default.IgnoredStatus)
                        {
                            item.Status = Settings1.Default.AcceptedStatus;
                        }
                    }
                }
                else
                {

                    if (errorCode == Settings1.Default.InvalideRequestCode)
                    {
                        orderNotification.Order[0].Status = Settings1.Default.IgnoredStatus;
                    }
                    else
                    {
                        orderNotification.Order[0].Status = Settings1.Default.RejectedStatus;
                    }
                    foreach (OrderItem item in orderNotification.Order[0].OrderItem)
                    {
                        if (item.Status != Settings1.Default.IgnoredStatus)
                        {
                            item.Status = Settings1.Default.RejectedStatus;
                        }
                    }
                }
            }
            else
            {
                if (requestStatus)
                {
                    orderNotification.Order[0].Status = Settings1.Default.CompleteStatus;

                    foreach (OrderItem item in orderNotification.Order[0].OrderItem)
                    {
                        if (item.Specification[0].Identifier.Value1.Equals("S0145887", StringComparison.InvariantCultureIgnoreCase))
                        {
                            item.Error = null;
                        }
                        //set the error code as 004 for esb robt deactivate completed order 
                        //and checking if not EESpring request.
                        else if (orderNotification.StandardHeader.ServiceAddressing.From.Contains(Settings1.Default.ESBQueueString)
                             && !(orderNotification.Order[0].OrderIdentifier.Value.StartsWith("EE", StringComparison.OrdinalIgnoreCase)))
                        {
                            item.Error.Code = errorCode;
                        }
                        if (item.Status != Settings1.Default.IgnoredStatus)
                        {
                            item.Status = Settings1.Default.CompleteStatus;
                        }

                    }
                }
                else
                {
                    if (errorCode == Settings1.Default.InvalideRequestCode)
                    {
                        orderNotification.Order[0].Status = Settings1.Default.IgnoredStatus;
                        foreach (OrderItem item in orderNotification.Order[0].OrderItem)
                        {
                            item.Status = Settings1.Default.IgnoredStatus;
                        }
                    }
                    else
                    {
                        orderNotification.Order[0].Status = Settings1.Default.FailedStatus;
                        foreach (OrderItem item in orderNotification.Order[0].OrderItem)
                        {
                            if (item.Status != Settings1.Default.IgnoredStatus)
                            {
                                item.Status = Settings1.Default.FailedStatus;
                            }

                        }
                    }
                }
            }


            if (orderRequest.StandardHeader.ServiceAddressing.From != null && !orderRequest.StandardHeader.ServiceAddressing.From.Equals(Settings1.Default.BTComstring)
                && !orderRequest.StandardHeader.ServiceAddressing.From.Equals(Settings1.Default.DanteEmailString)
                && !orderRequest.StandardHeader.ServiceAddressing.From.Equals(Settings1.Default.NativeClientString, StringComparison.OrdinalIgnoreCase)
                && !orderRequest.StandardHeader.ServiceAddressing.From.Equals(Settings1.Default.MCSOString, StringComparison.OrdinalIgnoreCase)
                && !orderRequest.StandardHeader.ServiceAddressing.From.Equals(Settings1.Default.DEVAPIServiceAddress, StringComparison.OrdinalIgnoreCase)
                && !orderRequest.StandardHeader.ServiceAddressing.From.Equals(Settings1.Default.BUTString, StringComparison.OrdinalIgnoreCase)
                && !orderRequest.StandardHeader.ServiceAddressing.From.Equals(Settings1.Default.adminToolString, StringComparison.OrdinalIgnoreCase)
                && !orderRequest.StandardHeader.ServiceAddressing.From.Equals(Settings1.Default.ESBAPIServiceAddress, StringComparison.OrdinalIgnoreCase))
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : Sending response to " + orderRequest.StandardHeader.ServiceAddressing.From);
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : orderNotification header " + orderNotification.StandardHeader.SerializeObject());

                if (orderRequest.Order.Status != null && !String.IsNullOrEmpty(orderRequest.Order.Status))
                {
                    orderStatus = orderRequest.Order.Status.ToString();
                }
                if (orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)) && !String.IsNullOrEmpty(orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
                {
                    bacId = orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                }
                if (orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("sourcebillaccountnumber", StringComparison.OrdinalIgnoreCase)) && !String.IsNullOrEmpty(orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("sourcebillaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
                {
                    sourceBac = orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("sourcebillaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                }
                if (orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("targetbillaccountnumber", StringComparison.OrdinalIgnoreCase)) && !String.IsNullOrEmpty(orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("targetbillaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
                {
                    targetBac = orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("targetbillaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                }
                if (orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("emailname", StringComparison.OrdinalIgnoreCase)) && !String.IsNullOrEmpty(orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("emailname", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
                {
                    emailName = orderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("emailname", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                }
                if (!(orderNotification.StandardHeader.E2e != null && orderNotification.StandardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(orderNotification.StandardHeader.E2e.E2EDATA)))
                {
                    orderNotification.StandardHeader.E2e = new E2E();
                }

                if (orderRequest.StandardHeader.ServiceAddressing.From.StartsWith(Settings1.Default.MQstring))
                {
                    myE2ETx.logMessage(MseoBeforeCallingMessageQueue, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; BAC<" + bacId + ">; OrderStatus<" + orderStatus + ">;");
                    if (isAck)
                        myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationOfs"].ToString(), E2ETransaction.vACK, "Order " + orderNotification.Order[0].Status);
                    else
                        myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationOfs"].ToString(), E2ETransaction.vRES, "Order " + orderNotification.Order[0].Status);
                    orderNotification.StandardHeader.E2e.E2EDATA = myE2ETx.toString();

                    if (!orderNotification.Order[0].OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("vasfinternalmqtestingflag")))
                    {
                        writeToMQ();
                    }

                    // log the MQ out if the Thomas Request with create action
                    //if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSportsProductCode"], StringComparison.OrdinalIgnoreCase) && (orderNotification.Order[0].OrderItem[0].Action.Code.ToLower() == "create" || orderNotification.Order[0].OrderItem[0].Action.Code.ToLower() == "modify"))
                    if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSportsProductCode"], StringComparison.OrdinalIgnoreCase))
                    {
                        if (ConfigurationManager.AppSettings["isThomasNewJourneySwitch"].ToLower() == "true")
                        {
                            string orderKey = orderNotification.Order[0].OrderIdentifier.Value;
                            //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderKey, orderNotification.SerializeObject(), ConfigurationManager.AppSettings["BTSportsProductCode"]);
                            LogHelper.LogActivityDetails(orderKey, Guid.NewGuid(), orderKey + ": sent notification to OFS: ", TimeSpan.Zero, "ThomasTrace", orderNotification.SerializeObject());
                        }
                    }
                    //log the MQ out if the Spring Request
                    if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"], StringComparison.OrdinalIgnoreCase)
                        && orderNotification.Order[0].OrderItem[0].Action.Code.ToLower() != "cancel")
                    {
                        if (ConfigurationManager.AppSettings["isSpringNewJourneySwitch"].ToLower() == "true")
                        {
                            string orderKey = orderNotification.Order[0].OrderIdentifier.Value;
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderKey, orderNotification.SerializeObject(), ConfigurationManager.AppSettings["BTSpringProductCode"]);
                        }
                    }
                    //log the MQ out if the HCSWarranty Request
                    if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["HCSProductCode"], StringComparison.OrdinalIgnoreCase))
                    {
                        if (ConfigurationManager.AppSettings["isHCSWarrantyNewJourneySwitch"].ToLower() == "true")
                        {
                            string orderKey = orderNotification.Order[0].OrderIdentifier.Value;
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderKey, orderNotification.SerializeObject(), ConfigurationManager.AppSettings["HCSProductCode"]);
                        }
                    }
                    //log the MQ out if the Voice Request
                    if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals("S0341469", StringComparison.OrdinalIgnoreCase))
                    {
                        //if (ConfigurationManager.AppSettings["isSpringNewJourneySwitch"].ToLower() == "true")
                        {
                            string orderKey = orderNotification.Order[0].OrderIdentifier.Value;
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderKey, orderNotification.SerializeObject(), "S0341469");
                        }
                    }

                    if (ConfigurationManager.AppSettings["isMQNotificationLoggingEnabled"].Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!((orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"], StringComparison.OrdinalIgnoreCase)) ||
                            (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSportsProductCode"], StringComparison.OrdinalIgnoreCase))))
                        {
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderNotification.Order[0].OrderIdentifier.Value.ToString(), orderNotification.SerializeObject(), "MQNotification");
                        }
                    }

                    myE2ETx.logMessage(MseoSentNotification, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; BAC<" + bacId + ">; OrderStatus<" + orderStatus + ">;");
                    myE2ETx.endOutboundCall(null);
                }

                else if (orderRequest.StandardHeader.ServiceAddressing.From.StartsWith(Settings1.Default.OVString, StringComparison.OrdinalIgnoreCase))
                {
                    myE2ETx.logMessage(MseoBeforeCallingMessageQueue, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; SOURCEBACID<" + sourceBac + ">; TARGETBACID<" + targetBac + ">; OrderStatus<" + orderStatus + ">;");
                    if (isAck)
                        myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationOneView"].ToString(), E2ETransaction.vACK, "Order " + orderNotification.Order[0].Status);
                    else
                        myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationOneView"].ToString(), E2ETransaction.vRES, "Order " + orderNotification.Order[0].Status);

                    orderNotification.StandardHeader.E2e.E2EDATA = myE2ETx.toString();
                    writeToOV();
                    if (ConfigurationManager.AppSettings.AllKeys.Contains("isMQNotificationLoggingEnabled") && ConfigurationManager.AppSettings["isMQNotificationLoggingEnabled"].Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderNotification.Order[0].OrderIdentifier.Value.ToString(), orderNotification.SerializeObject(), "MQNotification");
                    }
                    myE2ETx.logMessage(MseoSentNotification, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; SOURCEBACID<" + sourceBac + ">; TARGETBACID<" + targetBac + ">; OrderStatus<" + orderStatus + ">;");
                    myE2ETx.endOutboundCall(null);
                }
                else if (orderRequest.StandardHeader.ServiceAddressing.From.StartsWith(Settings1.Default.ESBQueueString, StringComparison.OrdinalIgnoreCase))
                {
                    string orderKey = orderNotification.Order[0].OrderIdentifier.Value;
                    if (orderKey.StartsWith("EE", StringComparison.OrdinalIgnoreCase) || (ConfigurationManager.AppSettings["isSpringNewJourneySwitch"].ToLower() == "true"))
                    {
                        myE2ETx.logMessage(MseoBeforeCallingMessageQueue, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; BAC<" + bacId + ">; OrderStatus<" + orderStatus + ">;");
                        if (isAck)
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationESBMQ"].ToString(), E2ETransaction.vACK, "Order " + orderNotification.Order[0].Status);
                        else
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationESBMQ"].ToString(), E2ETransaction.vRES, "Order " + orderNotification.Order[0].Status);
                        orderNotification.StandardHeader.E2e.E2EDATA = myE2ETx.toString();
                        writeToESBMQ();
                        //log the ESBMQ out if the Spring Request
                        Logger.Write(orderKey + "," +  " Sending Response to ESB : " + "," + orderNotification.SerializeObject(), Logger.TypeEnum.EESpringExceptionTrace);
                        if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"], StringComparison.OrdinalIgnoreCase)
                            && orderNotification.Order[0].OrderItem[0].Action.Code.ToLower() != "cancel")
                        {
                            if (ConfigurationManager.AppSettings["isSpringNewJourneySwitch"].ToLower() == "true")
                            {
                                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderKey, orderNotification.SerializeObject(), ConfigurationManager.AppSettings["BTSpringProductCode"]);
                            }
                        }

                        if (ConfigurationManager.AppSettings.AllKeys.Contains("isMQNotificationLoggingEnabled") && ConfigurationManager.AppSettings["isMQNotificationLoggingEnabled"].Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!(orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"], StringComparison.OrdinalIgnoreCase)))
                            {
                                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderNotification.Order[0].OrderIdentifier.Value.ToString(), orderNotification.SerializeObject(), "MQNotification");
                            }
                        }

                        myE2ETx.logMessage(MseoSentNotification, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; BAC<" + bacId + ">; OrderStatus<" + orderStatus + ">;");
                        myE2ETx.endOutboundCall(null);

                    }
                    else
                    {
                        myE2ETx.logMessage(MseoBeforeCallingMessageQueue, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; EmailName<" + emailName + ">; OrderStatus<" + orderStatus + ">;");
                        if (isAck)
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationESB"].ToString(), E2ETransaction.vACK, "Order " + orderNotification.Order[0].Status);
                        else
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationESB"].ToString(), E2ETransaction.vRES, "Order " + orderNotification.Order[0].Status);

                        orderNotification.StandardHeader.E2e.E2EDATA = myE2ETx.toString();
                        writeToESB();

                        if (ConfigurationManager.AppSettings.AllKeys.Contains("isMQNotificationLoggingEnabled") && ConfigurationManager.AppSettings["isMQNotificationLoggingEnabled"].Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.MQ_OUT_LOG, orderNotification.Order[0].OrderIdentifier.Value.ToString(), orderNotification.SerializeObject(), "MQNotification");
                        }
                        myE2ETx.logMessage(MseoSentNotification, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; EmailName<" + emailName + ">; OrderStatus<" + orderStatus + ">;");
                        myE2ETx.endOutboundCall(null);
                    }

                }
                else if (orderRequest.StandardHeader.ServiceAddressing.From.Equals(ConfigurationManager.AppSettings["BBMTServiceAddress"], StringComparison.OrdinalIgnoreCase))
                {
                    if (ConfigurationManager.AppSettings["BTIEmailErrorCodes"].Split(',').Contains(errorCode))
                    {
                        if (isAck)
                        {
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["BBMTServiceAddress"].ToString(), E2ETransaction.vACK, orderStatus);
                        }
                        else
                        {
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["BBMTServiceAddress"].ToString(), E2ETransaction.vRES, orderStatus);
                        }
                        orderNotification.StandardHeader.E2e.E2EDATA = myE2ETx.toString();
                        if (!orderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["VasContentFilteringProductFamily"], StringComparison.OrdinalIgnoreCase))
                        {
                            orderNotification.Order[0].OrderItem[0].HoldToTerm = null;
                            callOrderNotificationWS();
                        }
                        myE2ETx.logMessage(MseoSentNotification, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; BAC<" + bacId + ">; EMAILNAME<" + emailName + ">; OrderStatus<" + orderStatus + ">;");
                        myE2ETx.endOutboundCall(null);
                    }
                }
                else
                {
                    if (isAck)
                    {
                        if (orderRequest.Order.OrderItem.ToList().Exists(oi => oi.Specification[0].Identifier.Value1.Equals("S0145887", StringComparison.OrdinalIgnoreCase)))
                        {
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationRIOSS"].ToString(), E2ETransaction.vACK, orderStatus);
                        }
                        else
                        {
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationHestia"].ToString(), E2ETransaction.vACK, orderStatus);
                        }
                    }
                    else
                    {
                        if (orderRequest.Order.OrderItem.ToList().Exists(oi => oi.Specification[0].Identifier.Value1.Equals("S0145887", StringComparison.OrdinalIgnoreCase)))
                        {
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationRIOSS"].ToString(), E2ETransaction.vRES, orderStatus);
                        }
                        else
                        {
                            myE2ETx.startOutboundCall(ConfigurationManager.AppSettings["destinationHestia"].ToString(), E2ETransaction.vRES, orderStatus);
                        }
                    }

                    orderNotification.StandardHeader.E2e.E2EDATA = myE2ETx.toString();
                    orderNotification.Order[0].OrderItem[0].HoldToTerm = null;
                    callOrderNotificationWS();

                    myE2ETx.logMessage(MseoSentNotification, "orderKey<" + orderRequest.Order.OrderIdentifier.Value + ">; BAC<" + bacId + ">; OrderStatus<" + orderStatus + ">;");
                    myE2ETx.endOutboundCall(null);
                }
            }

            this.NotificationResponse = orderNotification;
        }

        private void callOrderNotificationWS()
        {
            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter ordernotification : " + orderNotification.SerializeObject());
            ManageOrderComplete manageOrderobj = null;
            if (Settings1.Default.IsProxyRequired)
            {
                WebProxy proxy = new WebProxy(Settings1.Default.ProxyAddress, Settings1.Default.ProxyPort);
                manageOrderobj = new ManageOrderComplete(orderRequest.StandardHeader.ServiceAddressing.From, proxy);
            }
            else if (orderRequest.StandardHeader.ServiceAddressing.From.Equals(ConfigurationManager.AppSettings["BBMTServiceAddress"], StringComparison.OrdinalIgnoreCase))
            {
                manageOrderobj = new ManageOrderComplete(ConfigurationManager.AppSettings["BBMTResponseServiceURL"]);
            }
            else
            {
                manageOrderobj = new ManageOrderComplete(orderRequest.StandardHeader.ServiceAddressing.From);
            }
            try
            {
                //Send OrderComplete
                manageOrderobj.orderNotification(orderNotification);
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter ordernotification successfully sent to  : " + orderRequest.StandardHeader.ServiceAddressing.From);
            }
            catch (Exception serviceException)
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : Exception : " + serviceException.ToString());
            }
            finally
            {
                manageOrderobj.Dispose();
            }
        }

        private void writeToMQ()
        {
            try
            {
                foreach (OrderItem item in orderNotification.Order[0].OrderItem)
                {
                    item.Specification[0].Identifier.Value = null;
                    item.Specification[0].Identifier.Name = null;
                    item.Specification[0].Type = null;
                    item.Instance[0].Specification1.Identifier.Value = null;
                    item.Instance[0].Specification1.Identifier.Name = null;
                    item.Instance[0].Specification1.Type = null;
                    item.RequiredDateTime = null;
                    item.Action.Reason = null;
                }
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter ordernotification : " + orderNotification.SerializeObject());
                if (ConfigurationManager.AppSettings["MQResponsePort"] != string.Empty)
                {
                    MQEnvironment.Port = Convert.ToInt32(ConfigurationManager.AppSettings["MQResponsePort"]);
                }
                Hashtable props = new Hashtable();
                props.Add(MQC.HOST_NAME_PROPERTY, ConfigurationManager.AppSettings["MQResponseHostname"]);
                props.Add(MQC.CHANNEL_PROPERTY, ConfigurationManager.AppSettings["MQResponseChannel"]);
                if (ConfigurationManager.AppSettings["MQResponseUserName"] != string.Empty)
                {
                    props.Add(MQC.USER_ID_PROPERTY, ConfigurationManager.AppSettings["MQResponseUserName"]);
                }
                if (ConfigurationManager.AppSettings["MQResponsePassword"] != string.Empty)
                {
                    props.Add(MQC.PASSWORD_PROPERTY, ConfigurationManager.AppSettings["MQResponsePassword"]);
                }


                MQQueueManager qMan = new MQQueueManager(ConfigurationManager.AppSettings["MQResponseQueueManager"], props);
                try
                {
                    MQMessage mQMsg = new MQMessage();
                    MQQueue q;
                    int InOpenOptions = MQC.MQOO_OUTPUT | MQC.MQOO_FAIL_IF_QUIESCING;
                    q = qMan.AccessQueue(ConfigurationManager.AppSettings["MQResponseQueueName"], InOpenOptions);
                    try
                    {
                        UTF8Encoding utfEnc = new UTF8Encoding();

                        string s = orderNotification.SerializeObject();
                        s = s.Replace("schemaLocation", "xsi:schemaLocation");
                        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter ordernotification : " + s);
                        mQMsg.WriteBytes(s);
                        MQPutMessageOptions pmo = new MQPutMessageOptions();

                        q.Put(mQMsg, pmo);
                    }
                    catch (Exception ex1)
                    {
                        if (ConfigurationManager.AppSettings["isThomasNewJourneySwitch"].Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSportsProductCode"], StringComparison.OrdinalIgnoreCase)
                                && orderNotification.Order[0].OrderItem[0].Action.Code.ToLower() == "create")
                            {
                                Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + ",Errored, MQ Set Exception ex1- " + ex1.Message, Logger.TypeEnum.ExceptionTrace);
                            }
                        }
                        if (ConfigurationManager.AppSettings["isSpringNewJourneySwitch"].Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"], StringComparison.OrdinalIgnoreCase))//&& orderNotification.Order[0].OrderItem[0].Action.Code.ToLower() == "create"
                            {
                                Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + ",Errored, MQ Set Exception ex1- " + ex1.Message, Logger.TypeEnum.SpringExceptionTrace);
                            }
                        }
                        if (ConfigurationManager.AppSettings["notificationExceptionTraceEnable"].Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + @", MQ Set Exception ex1- " + ex1.Message, Logger.TypeEnum.MQNotificationsExceptionTrace);
                        }
                    }
                    finally
                    {
                        q.Close();
                    }
                    mQMsg = null;
                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter ordernotification successfully sent to queue : " + ConfigurationManager.AppSettings["MQResponseQueueName"] + " on manager : " + ConfigurationManager.AppSettings["MQResponseQueueManager"] + "; host : " + ConfigurationManager.AppSettings["MQResponseHostname"]);
                }
                catch (Exception ex2)
                {
                    if (ConfigurationManager.AppSettings["isThomasNewJourneySwitch"].Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSportsProductCode"], StringComparison.OrdinalIgnoreCase)
                            && orderNotification.Order[0].OrderItem[0].Action.Code.ToLower() == "create")
                        {
                            Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + ",Errored, MQ Set Exception ex2- " + ex2.Message, Logger.TypeEnum.ExceptionTrace);
                        }
                    }
                    if (ConfigurationManager.AppSettings["isSpringNewJourneySwitch"].Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"], StringComparison.OrdinalIgnoreCase)
                            && orderNotification.Order[0].OrderItem[0].Action.Code.ToLower() != "cancel")
                        {
                            Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + ",Errored, MQ Set Exception ex2- " + ex2.Message, Logger.TypeEnum.SpringExceptionTrace);
                        }
                    }
                    if (ConfigurationManager.AppSettings["notificationExceptionTraceEnable"].Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + @", MQ Set Exception ex2- " + ex2.Message, Logger.TypeEnum.MQNotificationsExceptionTrace);
                    }
                }
                finally
                {
                    qMan.Disconnect();
                }
            }
            catch (Exception mqInsertException)
            {
                if (ConfigurationManager.AppSettings["isThomasNewJourneySwitch"].Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSportsProductCode"], StringComparison.OrdinalIgnoreCase)
                        && orderNotification.Order[0].OrderItem[0].Action.Code.ToLower() == "create")
                    {
                        Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + ",Errored, MQ Set Exception ex3- " + mqInsertException.Message, Logger.TypeEnum.ExceptionTrace);
                    }
                }
                if (ConfigurationManager.AppSettings["isSpringNewJourneySwitch"].Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"], StringComparison.OrdinalIgnoreCase)
                        && orderNotification.Order[0].OrderItem[0].Action.Code.ToLower() != "cancel")                    
                    {
                        Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + ",Errored,MQ Set Exception ex3- " + mqInsertException.Message, Logger.TypeEnum.SpringExceptionTrace);
                    }
                }
                if (ConfigurationManager.AppSettings["notificationExceptionTraceEnable"].Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + @", MQ Set Exception ex3- " + mqInsertException.Message, Logger.TypeEnum.MQNotificationsExceptionTrace);
                }

                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : Exception : " + mqInsertException.ToString());
            }
            finally
            {

            }
        }
        private void writeToOV()
        {
            try
            {
                foreach (OrderItem item in orderNotification.Order[0].OrderItem)
                {
                    item.Specification[0].Identifier.Value = null;
                    item.Specification[0].Identifier.Name = null;
                    item.Specification[0].Type = null;
                    item.Instance[0].Specification1.Identifier.Value = null;
                    item.Instance[0].Specification1.Identifier.Name = null;
                    item.Instance[0].Specification1.Type = null;
                    item.RequiredDateTime = null;
                    item.Action.Reason = null;
                }
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter ordernotification : " + orderNotification.SerializeObject());
                if (ConfigurationManager.AppSettings["OVQueueResponsePort"] != string.Empty)
                {
                    MQEnvironment.Port = Convert.ToInt32(ConfigurationManager.AppSettings["OVQueueResponsePort"]);
                }
                Hashtable props = new Hashtable();
                props.Add(MQC.HOST_NAME_PROPERTY, ConfigurationManager.AppSettings["OVQueueResponseHostname"]);
                props.Add(MQC.CHANNEL_PROPERTY, ConfigurationManager.AppSettings["OVQueueResponseChannel"]);
                if (ConfigurationManager.AppSettings["OVQueueResponseUserName"] != string.Empty)
                {
                    props.Add(MQC.USER_ID_PROPERTY, ConfigurationManager.AppSettings["OVQueueResponseUserName"]);
                }
                if (ConfigurationManager.AppSettings["OVQueueResponsePassword"] != string.Empty)
                {
                    props.Add(MQC.PASSWORD_PROPERTY, ConfigurationManager.AppSettings["OVQueueResponsePassword"]);
                }
                MQQueueManager qMan = new MQQueueManager(ConfigurationManager.AppSettings["OVQueueResponseQueueManager"], props);
                try
                {
                    MQMessage mQMsg = new MQMessage();
                    MQQueue q;
                    int InOpenOptions = MQC.MQOO_OUTPUT | MQC.MQOO_FAIL_IF_QUIESCING;
                    q = qMan.AccessQueue(ConfigurationManager.AppSettings["OVQueueResponseQueueName"], InOpenOptions);
                    try
                    {
                        UTF8Encoding utfEnc = new UTF8Encoding();
                        //Updating Order notification encoding from UTF-16 to UTF-8
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(orderNotification.SerializeObject());

                        // The first child of a standard XML document is the XML declaration.
                        // The following code assumes and reads the first child as the XmlDeclaration.
                        if (doc.FirstChild.NodeType == XmlNodeType.XmlDeclaration)
                        {
                            // Get the encoding declaration.
                            XmlDeclaration decl = (XmlDeclaration)doc.FirstChild;
                            // Set the encoding declaration.
                            decl.Encoding = "UTF-8";
                        }
                        string s = doc.InnerXml;
                        s = s.Replace("schemaLocation", "xsi:schemaLocation");
                        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter ordernotification : " + s);
                        mQMsg.WriteBytes(s);
                        MQPutMessageOptions pmo = new MQPutMessageOptions();
                        q.Put(mQMsg, pmo);
                    }
                    catch (Exception ex1)
                    {
                        if (ConfigurationManager.AppSettings["notificationExceptionTraceEnable"].Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + @", OneView Set Exception ex1- " + ex1.Message, Logger.TypeEnum.MQNotificationsExceptionTrace);
                        }
                    }
                    finally
                    {
                        q.Close();
                    }
                    mQMsg = null;
                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter ordernotification successfully sent to queue : " + ConfigurationManager.AppSettings["OVQueueResponseQueueName"] + " on manager : " + ConfigurationManager.AppSettings["OVQueueResponseQueueManager"] + "; host : " + ConfigurationManager.AppSettings["OVQueueResponseHostname"]);
                }
                catch (Exception ex2)
                {
                    if (ConfigurationManager.AppSettings["notificationExceptionTraceEnable"].Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + @", OneView Set Exception ex2- " + ex2.Message, Logger.TypeEnum.MQNotificationsExceptionTrace);
                    }
                }
                finally
                {
                    qMan.Disconnect();
                }
            }
            catch (Exception mqInsertException)
            {
                if (ConfigurationManager.AppSettings["notificationExceptionTraceEnable"].Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + @", OneView Set Exception ex3- " + mqInsertException.Message, Logger.TypeEnum.MQNotificationsExceptionTrace);
                }
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : Exception : " + mqInsertException.ToString());
            }
            finally
            {
            }
        }

        private void writeToESB()
        {
            try
            {
                foreach (OrderItem item in orderNotification.Order[0].OrderItem)
                {
                    item.Specification[0].Identifier.Value = null;
                    item.Specification[0].Identifier.Name = null;
                    item.RequiredDateTime = null;
                    item.Action.Reason = null;
                }
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter ordernotification : " + orderNotification.SerializeObject());
                if (ConfigurationManager.AppSettings["ESBQueueResponsePort"] != string.Empty)
                {
                    MQEnvironment.Port = Convert.ToInt32(ConfigurationManager.AppSettings["ESBQueueResponsePort"]);
                }
                Hashtable props = new Hashtable();
                props.Add(MQC.HOST_NAME_PROPERTY, ConfigurationManager.AppSettings["ESBQueueResponseHostname"]);
                props.Add(MQC.CHANNEL_PROPERTY, ConfigurationManager.AppSettings["ESBQueueResponseChannel"]);
                if (ConfigurationManager.AppSettings["ESBQueueResponseUserName"] != string.Empty)
                {
                    props.Add(MQC.USER_ID_PROPERTY, ConfigurationManager.AppSettings["ESBQueueResponseUserName"]);
                }
                if (ConfigurationManager.AppSettings["ESBQueueResponsePassword"] != string.Empty)
                {
                    props.Add(MQC.PASSWORD_PROPERTY, ConfigurationManager.AppSettings["ESBQueueResponsePassword"]);
                }
                MQQueueManager qMan = new MQQueueManager(ConfigurationManager.AppSettings["ESBQueueResponseQueueManager"], props);
                try
                {
                    MQMessage mQMsg = new MQMessage();
                    MQQueue q;
                    int InOpenOptions = MQC.MQOO_OUTPUT | MQC.MQOO_FAIL_IF_QUIESCING;
                    q = qMan.AccessQueue(ConfigurationManager.AppSettings["ESBQueueResponseQueueName"], InOpenOptions);
                    try
                    {
                        UTF8Encoding utfEnc = new UTF8Encoding();

                        string s = orderNotification.SerializeObject();
                        s = s.Replace("schemaLocation", "xsi:schemaLocation");
                        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter ordernotification : " + s);
                        mQMsg.WriteBytes(s);
                        MQPutMessageOptions pmo = new MQPutMessageOptions();

                        q.Put(mQMsg, pmo);
                    }
                    catch (Exception ex1)
                    {
                        if (ConfigurationManager.AppSettings["notificationExceptionTraceEnable"].Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + @", ESB Set Exception ex1- " + ex1.Message, Logger.TypeEnum.MQNotificationsExceptionTrace);
                        }
                    }
                    finally
                    {
                        q.Close();
                    }
                    mQMsg = null;
                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter ordernotification successfully sent to queue : " + ConfigurationManager.AppSettings["ESBQueueResponseQueueName"] + " on manager : " + ConfigurationManager.AppSettings["ESBQueueResponseQueueManager"] + "; host : " + ConfigurationManager.AppSettings["ESBQueueResponseHostname"]);
                }
                catch (Exception ex2)
                {
                    if (ConfigurationManager.AppSettings["notificationExceptionTraceEnable"].Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + @", ESB Set Exception ex2- " + ex2.Message, Logger.TypeEnum.MQNotificationsExceptionTrace);
                    }
                }
                finally
                {
                    qMan.Disconnect();
                }
            }
            catch (Exception mqInsertException)
            {
                if (ConfigurationManager.AppSettings["notificationExceptionTraceEnable"].Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + @", ESB Set Exception ex3- " + mqInsertException.Message, Logger.TypeEnum.MQNotificationsExceptionTrace);
                }
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : Exception : " + mqInsertException.ToString());
            }
            finally
            {
            }
        }
        private void writeToESBMQ()
        {
            try
            {
                foreach (OrderItem item in orderNotification.Order[0].OrderItem)
                {
                    item.Specification[0].Identifier.Value = null;
                    item.Specification[0].Identifier.Name = null;
                    item.Specification[0].Type = null;
                    item.Instance[0].Specification1.Identifier.Value = null;
                    item.Instance[0].Specification1.Identifier.Name = null;
                    item.Instance[0].Specification1.Type = null;
                    item.RequiredDateTime = null;
                    item.Action.Reason = null;
                }
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter ordernotification : " + orderNotification.SerializeObject());
                if (ConfigurationManager.AppSettings["EEMQResponsePort"] != string.Empty)
                {
                    MQEnvironment.Port = Convert.ToInt32(ConfigurationManager.AppSettings["EEMQResponsePort"]);
                }
                Hashtable props = new Hashtable();
                props.Add(MQC.HOST_NAME_PROPERTY, ConfigurationManager.AppSettings["EEMQResponseHostname"]);
                props.Add(MQC.CHANNEL_PROPERTY, ConfigurationManager.AppSettings["EEMQResponseChannel"]);
                if (ConfigurationManager.AppSettings["EEMQResponseUserName"] != string.Empty)
                {
                    props.Add(MQC.USER_ID_PROPERTY, ConfigurationManager.AppSettings["EEMQResponseUserName"]);
                }
                if (ConfigurationManager.AppSettings["EEMQResponsePassword"] != string.Empty)
                {
                    props.Add(MQC.PASSWORD_PROPERTY, ConfigurationManager.AppSettings["EEMQResponsePassword"]);
                }


                MQQueueManager qMan = new MQQueueManager(ConfigurationManager.AppSettings["EEMQResponseQueueManager"], props);
                try
                {
                    MQMessage mQMsg = new MQMessage();
                    MQQueue q;
                    int InOpenOptions = MQC.MQOO_OUTPUT | MQC.MQOO_FAIL_IF_QUIESCING;
                    q = qMan.AccessQueue(ConfigurationManager.AppSettings["EEMQResponseQueueName"], InOpenOptions);
                    try
                    {
                        UTF8Encoding utfEnc = new UTF8Encoding();

                        string s = orderNotification.SerializeObject();
                        s = s.Replace("schemaLocation", "xsi:schemaLocation");
                        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter ordernotification : " + s);
                        mQMsg.WriteBytes(s);
                        MQPutMessageOptions pmo = new MQPutMessageOptions();

                        q.Put(mQMsg, pmo);
                    }
                    catch (Exception ex1)
                    {                        
                        if (ConfigurationManager.AppSettings["isSpringNewJourneySwitch"].Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"], StringComparison.OrdinalIgnoreCase))//&& orderNotification.Order[0].OrderItem[0].Action.Code.ToLower() == "create"
                            {
                                Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + ",Errored, MQ Set Exception ex1- " + ex1.Message, Logger.TypeEnum.EESpringExceptionTrace);
                            }
                        }
                        if (ConfigurationManager.AppSettings["notificationExceptionTraceEnable"].Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + @", ESBMQ Set Exception ex1- " + ex1.Message, Logger.TypeEnum.MQNotificationsExceptionTrace);
                        }
                    }
                    finally
                    {
                        q.Close();
                    }
                    mQMsg = null;
                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter ordernotification successfully sent to queue : " + ConfigurationManager.AppSettings["MQResponseQueueName"] + " on manager : " + ConfigurationManager.AppSettings["MQResponseQueueManager"] + "; host : " + ConfigurationManager.AppSettings["MQResponseHostname"]);
                }
                catch (Exception ex2)
                {                    
                    if (ConfigurationManager.AppSettings["isSpringNewJourneySwitch"].Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"], StringComparison.OrdinalIgnoreCase)
                            && orderNotification.Order[0].OrderItem[0].Action.Code.ToLower() != "cancel")
                        {
                            Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + ",Errored, ESBMQ Set Exception ex2- " + ex2.Message, Logger.TypeEnum.EESpringExceptionTrace);
                        }
                    }
                    if (ConfigurationManager.AppSettings["notificationExceptionTraceEnable"].Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + @", ESBMQ Set Exception ex2- " + ex2.Message, Logger.TypeEnum.MQNotificationsExceptionTrace);
                    }
                }
                finally
                {
                    qMan.Disconnect();
                }
            }
            catch (Exception mqInsertException)
            {                
                if (ConfigurationManager.AppSettings["isSpringNewJourneySwitch"].Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    if (orderNotification.Order[0].OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"], StringComparison.OrdinalIgnoreCase)
                        && orderNotification.Order[0].OrderItem[0].Action.Code.ToLower() != "cancel")
                    {
                        Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + ",Errored,ESBMQ Set Exception ex3- " + mqInsertException.Message, Logger.TypeEnum.EESpringExceptionTrace);
                    }
                }
                if (ConfigurationManager.AppSettings["notificationExceptionTraceEnable"].Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Write(orderNotification.Order[0].OrderIdentifier.Value + @", ESBMQ Set Exception ex3- " + mqInsertException.Message, Logger.TypeEnum.MQNotificationsExceptionTrace);
                }

                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : Exception : " + mqInsertException.ToString());
            }
            finally
            {

            }
        }
    }
}
