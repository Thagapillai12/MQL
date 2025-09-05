using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using BT.SaaS.IspssAdapter;
using BT.SaaS.IspssAdapter.Dnp;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Configuration;
using com.bt.util.logging;
using MSEO = BT.SaaS.MSEOAdapter;

namespace BT.SaaS.MSEOAdapter
{
    public class BTPlusMarkerProcessor
    {
        #region Constants
        const string BACID_IDENTIFER_NAMEPACE = "VAS_BILLINGACCOUNT_ID";
        const string BTCOM = "BTCOM";
        const string BT_PLUS_MARKER_NAMEPACE = "BT_PLUS_MARKER";
        const string Associate_BT_Plus_Marker_Namespace = "ASSOCIATE_BT_PLUS_MARKER";
        const string GotResponseFromDnPWithBusinessError = "GotResponseFromDnPWithBusinessError";
        const string StartedDnPCall = "StartedDnPCall";
        const string GotResponseFromDnP = "GotResponseFromDnP";

        private const string ACTION_SEARCH = "SEARCH";
        private const string ACTION_UPDATE = "UPDATE";
        private const string ACTION_DELETE = "DELETE";
        private const string ACTION_INSERT = "INSERT";
        private const string ACTION_LINK = "LINK";
        private const string DEFAULT_ROLE = "DEFAULT";
        private const string ACTIVE = "ACTIVE";
        private const string ADMIN_ROLE = "ADMIN";

        const string Accepted = "Accepted";
        const string Ignored = "Ignored";
        const string Completed = "Completed";
        const string Failed = "Failed";
        const string Errored = "Errored";
        const string MakingDnPCall = "Making DnP call";

        const string AcceptedNotificationSent = "Accepted Notification Sent for the Order";
        const string IgnoredNotificationSent = "Ignored Notification Sent for the Order";
        const string CompletedNotificationSent = "Completed Notification Sent for the Order";
        const string FailedNotificationSent = "Failure Notification Sent for the Order";
        const string FailedErrorMessage = "Order failed with error messgae: ";
        const string SendingRequestToDNP = "Sending the Request to DNP";
        const string ReceivedResponsefromDNP = "Recieved Response from DNP";
        const string FailureResponseFromDnP = "Recieved failure response from DnP";
        const string NullResponseFromDnP = "Response is null from DnP";
        const string DnPAdminstratorFailedResponse = "Non Functional Exception from DNP(Administrator): ";
        #endregion

        System.DateTime ActivityStartTime = System.DateTime.Now;
        Guid guid = new Guid(); string bptmTxnId = string.Empty;

        public BTPlusMarkerProcessor()
        {
            //this.ActivityStartTime = System.DateTime.Now;                      
        }

        public void BTPlusMarkerReqeustMapper(OrderRequest requestOrderRequest, Guid btguid, System.DateTime ActivityStartedTime, ref E2ETransaction e2eData)
        {
            MSEOOrderNotification notification = null;
            bool IsExsitingBacID = false;
            bool isAHTDone = false;
            bool isServiceAlreadyExist = false;
            string action = string.Empty;
            string reason = string.Empty;
            string OrderKey = string.Empty;
            string BACNumber = string.Empty;
            string BtOneId = string.Empty;
            guid = btguid;
            this.ActivityStartTime = ActivityStartedTime;

            try
            {
                OrderKey = requestOrderRequest.Order.OrderIdentifier.Value;
                notification = new MSEOOrderNotification(requestOrderRequest);
                this.bptmTxnId = BPTMHelper.GetTxnIdFromE2eData(e2eData.toString());

                if (requestOrderRequest.Order.OrderItem[0].Action != null && requestOrderRequest.Order.OrderItem[0].Action.Code != null)
                {
                    action = requestOrderRequest.Order.OrderItem[0].Action.Code.ToString();
                    //hcsParameters.OrderAction = action;
                    if (requestOrderRequest.Order.OrderItem[0].Action.Reason != null)
                    {
                        reason = requestOrderRequest.Order.OrderItem[0].Action.Reason.ToString();
                    }

                    // add a condition to check the associate btplus marker request

                }

                foreach (InstanceCharacteristic insChar in requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic)
                {
                    if ((insChar.Name.Equals("BillAccountNumber", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                    {
                        BACNumber = insChar.Value;
                        break;
                    }
                }
                if (!String.IsNullOrEmpty(BACNumber))
                {
                    GetClientProfileV1Res bacProfileResponse = null;
                    ClientServiceInstanceV1[] bacServiceResponse = null;

                    bacProfileResponse = DnpWrapper.GetClientProfileV1ForThomas(BACNumber, BACID_IDENTIFER_NAMEPACE, OrderKey);

                    if (bacProfileResponse != null && bacProfileResponse.clientProfileV1 != null && bacProfileResponse.clientProfileV1.client != null && bacProfileResponse.clientProfileV1.client.clientIdentity!=null)
                    {
                        IsExsitingBacID = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(BACNumber, StringComparison.OrdinalIgnoreCase));
                        if (IsExsitingBacID)
                        {
                            BT.SaaS.IspssAdapter.Dnp.ClientIdentity bacClientIdentity = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(BACNumber, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                            if (bacClientIdentity.clientIdentityValidation != null)
                                isAHTDone = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                        }
                        if (bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(i => i.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                        {
                            BtOneId = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                        }
                        if (bacProfileResponse.clientProfileV1.clientServiceInstanceV1 != null && bacProfileResponse.clientProfileV1.clientServiceInstanceV1.ToList().Exists(CSI => CSI.clientServiceInstanceIdentifier.value.Equals(BT_PLUS_MARKER_NAMEPACE) && CSI.serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && si.value.Equals(BACNumber, StringComparison.OrdinalIgnoreCase))))
                        {
                            isServiceAlreadyExist = true;
                        }
                        else
                        {
                            //BTPlus for AHT Delinked scenarios...
                            bacServiceResponse = DnpWrapper.getServiceInstanceV1(BACNumber, BACID_IDENTIFER_NAMEPACE, BT_PLUS_MARKER_NAMEPACE);

                            if ((bacServiceResponse != null) && (bacServiceResponse.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BT_PLUS_MARKER_NAMEPACE, StringComparison.OrdinalIgnoreCase))))
                            {
                                isServiceAlreadyExist = true;
                            }
                        }
                    }
                    else
                    {
                        bacServiceResponse = DnpWrapper.getServiceInstanceV1(BACNumber, BACID_IDENTIFER_NAMEPACE, BT_PLUS_MARKER_NAMEPACE);

                        if ((bacServiceResponse != null) && (bacServiceResponse.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BT_PLUS_MARKER_NAMEPACE, StringComparison.OrdinalIgnoreCase))))
                        {
                            isServiceAlreadyExist = true;
                        }
                    }

                    if (action.Equals("create", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!isServiceAlreadyExist)
                        {
                            //sending accepted notification                         
                            notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, Accepted, System.DateTime.Now - ActivityStartTime, "BTPlusMarkerTrace", OrderKey);

                            if (isAHTDone)
                                MCPforBTPlusMarker(BACNumber, BtOneId, BT_PLUS_MARKER_NAMEPACE, OrderKey, notification, ref e2eData);
                            else
                                MSIforBTPlusMarker(BACNumber, action, BT_PLUS_MARKER_NAMEPACE, OrderKey, notification, ref e2eData);
                        }
                        else
                        {
                            notification.sendNotification(false, false, "001", "The given bac already have BTPlusMarker Service", ref e2eData, true);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + " The given bac already have BTPlusMarker Service", System.DateTime.Now - ActivityStartTime, "BTPlusMarkerTrace", OrderKey);                            
                        }

                    }
                    else if (action.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                    {
                        if (isServiceAlreadyExist)
                        {
                            //sending accepted notification 
                            notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, Accepted, System.DateTime.Now - ActivityStartTime, "BTPlusMarkerTrace", OrderKey);

                            MSIforBTPlusMarker(BACNumber, action, BT_PLUS_MARKER_NAMEPACE, OrderKey, notification, ref e2eData);
                        }
                        else
                        {
                            notification.sendNotification(false, false, "001", "The given bac doesn't have BTPlusMarker Service to cease", ref e2eData, true);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "The given bac doesn't have BTPlusMarker Service to cease", System.DateTime.Now - ActivityStartTime, "BTPlusMarkerTrace", OrderKey);                            
                        }
                    }
                    else
                    {
                        notification.sendNotification(false, false, "001", "Bad Request from MQService", ref e2eData, true);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "Bad Request from MQService", System.DateTime.Now - ActivityStartTime, "BTPlusMarkerTrace", OrderKey);                            
                    }
                }
            }

            catch (DnpException exception)
            {
                notification.sendNotification(false, false, "001", exception.Message, ref e2eData, true);
                LogHelper.LogErrorMessage(exception.Message.ToString(), bptmTxnId, " in BTPlusMarkerReqeustMapper method with orderkey as "+OrderKey);
                //Logger.Write(OrderKey + "," + Ignored + "," + IgnoredNotificationSent + exception.Message, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                //Logger.Write(OrderKey + "," + Ignored + "," + IgnoredNotificationSent + exception.Message, Logger.TypeEnum.BTPlusMarkerExceptionTrace);

            }
            catch (Exception ex)
            {
                notification.sendNotification(false, false, "001", ex.Message, ref e2eData, true);
                LogHelper.LogErrorMessage(ex.Message.ToString(), bptmTxnId, " in BTPlusMarkerReqeustMapper method with orderkey as " + OrderKey);

                //Logger.Write(OrderKey + "," + Ignored + "," + IgnoredNotificationSent + ex.Message, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                //Logger.Write(OrderKey + "," + Ignored + "," + IgnoredNotificationSent + ex.Message, Logger.TypeEnum.BTPlusMarkerExceptionTrace);
            }
        }
        /// <summary>
        /// to create/cancel service instance 
        /// </summary>
        /// <param name="BAC">billingaccount number</param>
        /// <param name="action">either create/cancel</param>
        /// <returns></returns>
        public void MSIforBTPlusMarker(string BAC, string action, string serviceCode, string OrderKey, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            manageServiceInstanceV1Response1 profileResponse = null;
            manageServiceInstanceV1Request1 profileRequest = new manageServiceInstanceV1Request1();
            profileRequest.manageServiceInstanceV1Request = new ManageServiceInstanceV1Request();

            ManageServiceInstanceV1Req manageServiceInstanceV1Req = new ManageServiceInstanceV1Req();

            try
            {
                ClientServiceInstanceV1[] serviceInstance = new ClientServiceInstanceV1[1];
                serviceInstance[0] = new ClientServiceInstanceV1();

                serviceInstance[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                serviceInstance[0].clientServiceInstanceIdentifier.value = serviceCode;
                serviceInstance[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                serviceInstance[0].clientServiceInstanceStatus.value = ACTIVE;
                serviceInstance[0].action = ACTION_INSERT;
                List<ServiceIdentity> listServiceIdentity = new List<ServiceIdentity>();
                ServiceIdentity serviceIdentity = new ServiceIdentity();
                serviceIdentity.domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity.value = BAC;
                if (action.ToLower().Equals("create"))
                {
                    serviceIdentity.action = ACTION_LINK;
                    serviceInstance[0].action = ACTION_INSERT;
                }
                else if (action.ToLower().Equals("cancel"))
                {
                    serviceIdentity.action = ACTION_SEARCH;
                    serviceInstance[0].action = ACTION_DELETE;
                }

                listServiceIdentity.Add(serviceIdentity);

                serviceInstance[0].serviceIdentity = listServiceIdentity.ToArray();

                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                //manageServiceInstanceV1Req.clientServiceInstanceV1 = clientServiceInstance;
                profileRequest.manageServiceInstanceV1Request.standardHeader = headerBlock;

                manageServiceInstanceV1Req.clientServiceInstanceV1 = serviceInstance[0];

                profileRequest.manageServiceInstanceV1Request.manageServiceInstanceV1Req = manageServiceInstanceV1Req;

                //Logger.Write(OrderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.BTPlusMarkerMessageTrace);

                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall, System.DateTime.Now - ActivityStartTime, "BTPlusMarkerTrace", OrderKey, profileRequest.SerializeObject());
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, OrderKey, profileRequest.SerializeObject(), ConfigurationManager.AppSettings["BTPlusMarkerProductCode"].ToString());

                profileResponse = DnpWrapper.manageServiceInstanceV1Thomas(profileRequest, OrderKey);

                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, OrderKey, profileResponse.SerializeObject(), ConfigurationManager.AppSettings["BTPlusMarkerProductCode"].ToString());
                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "BTPlusMarkerTrace", OrderKey, profileResponse.SerializeObject());
                //Logger.Write(OrderKey + "," + GotResponseFromDnP + "," + GotResponseFromDnP, Logger.TypeEnum.BTPlusMarkerMessageTrace);

                if (profileResponse != null
                    && profileResponse.manageServiceInstanceV1Response != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    //sending completed notification
                    //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA;
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                        e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, Completed, System.DateTime.Now - ActivityStartTime, "BTPlusMarkerTrace", OrderKey);
                    //Logger.Write(OrderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                }
                else
                {
                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description;
                        //Logger.Write(OrderKey + "," + Failed + "," + FailedErrorMessage + errorMessage, Logger.TypeEnum.BTPlusMarkerExceptionTrace);
                        LogHelper.LogErrorMessage(errorMessage, bptmTxnId, "Error in MSIforBTPlusMarker method with orderkey as " + OrderKey);

                        if (errorMessage.Contains("Administrator"))
                        {
                            LogHelper.LogErrorMessage(DnPAdminstratorFailedResponse, bptmTxnId, "DnPAdminstrator error in MSIforBTPlusMarker method with orderkey as " + OrderKey);
                            //Logger.Write(OrderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.BTPlusMarkerExceptionTrace);
                            //Logger.Write(OrderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                        }
                        else
                        {
                            LogHelper.LogErrorMessage(FailureResponseFromDnP + " " + errorMessage, bptmTxnId, "DnP error in MSIforBTPlusMarker method with orderkey as " + OrderKey);
                            //Logger.Write(OrderKey + "," + Failed + "," + FailureResponseFromDnP + errorMessage, Logger.TypeEnum.BTPlusMarkerMessageTrace);

                            e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                            if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                                e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                            else
                                e2eData.endOutboundCall(e2eData.toString());
                            //sending failure notification
                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true);
                            LogHelper.LogErrorMessage(Failed, bptmTxnId, "failernotification in MSIforBTPlusMarker method with orderkey as " + OrderKey);
                            //Logger.Write(OrderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                        }
                    }
                    else
                    {
                        LogHelper.LogErrorMessage(NullResponseFromDnP, bptmTxnId, "NullResponseFromDnP in MSIforBTPlusMarker method with orderkey as " + OrderKey);
                        //Logger.Write(OrderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.BTPlusMarkerMessageTrace);

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //sending failure notification
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true);
                        LogHelper.LogErrorMessage(Failed, bptmTxnId, "failernotification in MSIforBTPlusMarker method with orderkey as " + OrderKey);
                        //Logger.Write(OrderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                    }
                }
            }

            catch (Exception ex)
            {
                //Logger.Write(OrderKey + "," + Failed + "," + FailureResponseFromDnP + ex.Message, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                LogHelper.LogErrorMessage(FailureResponseFromDnP, bptmTxnId, "FailureResponseFromDnP in MSIforBTPlusMarker method with orderkey as " + OrderKey);

                //sending failure notification
                //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = e2eData;
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true);
                LogHelper.LogErrorMessage(ex.Message.ToString(), bptmTxnId, "exception in in MSIforBTPlusMarker method with orderkey as " + OrderKey);
                //Logger.Write(OrderKey + "," + Failed + "," + FailedErrorMessage + ex.Message, Logger.TypeEnum.BTPlusMarkerExceptionTrace);
                //Logger.Write(OrderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.BTPlusMarkerMessageTrace);
            }
        }

        /// <summary>
        /// to create new service for AHT profile
        /// </summary>
        /// <param name="BAC">billingaccountnumber</param>
        /// <param name="BtOneId">btoneid</param>
        /// <param name="serviceCode">it depends on service</param>
        /// <returns></returns>
        public void MCPforBTPlusMarker(string BAC, string BtOneId, string serviceCode, string OrderKey, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            manageClientProfileV1Response1 profileResponse1 = null;
            manageClientProfileV1Request1 profileRequest = new manageClientProfileV1Request1();
            profileRequest.manageClientProfileV1Request = new ManageClientProfileV1Request();

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerblock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerblock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerblock.serviceAddressing.from = @"http://www.profile.com?SAASMSEO";
            headerblock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
            headerblock.serviceState.stateCode = "OK";
            try
            {
                ManageClientProfileV1Req manageClientProfileV1Req1 = new ManageClientProfileV1Req();
                manageClientProfileV1Req1.clientProfileV1 = new ClientProfileV1();

                manageClientProfileV1Req1.clientProfileV1.client = new Client();
                manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_UPDATE;

                List<ClientIdentity> clientIdentityList = new List<ClientIdentity>();

                ClientIdentity clientIdentity = null;

                clientIdentity = new ClientIdentity();
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                clientIdentity.value = BAC;
                clientIdentity.action = ACTION_SEARCH;

                clientIdentityList.Add(clientIdentity);

                ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                serviceIdentity[0] = new ServiceIdentity();
                serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity[0].value = BAC;
                serviceIdentity[0].action = ACTION_LINK;

                ClientServiceInstanceV1[] clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = serviceCode;
                clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                clientServiceInstanceV1[0].clientServiceInstanceStatus.value = ACTIVE;
                clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                clientServiceInstanceV1[0].action = ACTION_INSERT;

                //calling this method to get the service roles
                clientServiceInstanceV1[0].clientServiceRole = CreateServiceRoles(BAC, BtOneId);

                manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;

                profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;
                profileRequest.manageClientProfileV1Request.standardHeader = headerblock;

                //Logger.Write(OrderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.BTPlusMarkerMessageTrace);// need to add message trace 
                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall, System.DateTime.Now - ActivityStartTime, "BTPlusMarkerTrace", OrderKey, profileRequest.SerializeObject());
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, OrderKey, profileRequest.SerializeObject(), ConfigurationManager.AppSettings["BTPlusMarkerProductCode"].ToString());

                profileResponse1 = DnpWrapper.manageClientProfileV1Thomas(profileRequest, OrderKey);

                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, OrderKey, profileResponse1.SerializeObject(), ConfigurationManager.AppSettings["BTPlusMarkerProductCode"].ToString());
                //Logger.Write(OrderKey + "," + GotResponseFromDnP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.BTPlusMarkerMessageTrace);                
                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "BTPlusMarkerTrace", OrderKey,profileResponse1.SerializeObject());

                if (profileResponse1 != null
                   && profileResponse1.manageClientProfileV1Response != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, Completed, System.DateTime.Now - ActivityStartTime, "BTPlusMarkerTrace", OrderKey);
                    //Logger.Write(OrderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                }
                else
                {
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                        LogHelper.LogErrorMessage(errorMessage, bptmTxnId, "Error in MSIforBTPlusMarker method with orderkey as " + OrderKey);
                        //Logger.Write(OrderKey + "," + Failed + "," + FailedErrorMessage + errorMessage, Logger.TypeEnum.BTPlusMarkerExceptionTrace);
                        if (errorMessage.Contains("Administrator"))
                        {
                            LogHelper.LogErrorMessage(DnPAdminstratorFailedResponse, bptmTxnId, "DnPAdminstrator error in MCPforBTPlusMarker method with orderkey as " + OrderKey);
                            //Logger.Write(OrderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.BTPlusMarkerExceptionTrace);
                            //Logger.Write(OrderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                        }
                        else
                        {
                            //Logger.Write(OrderKey + "," + Failed + "," + FailureResponseFromDnP + errorMessage, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                            LogHelper.LogErrorMessage(FailureResponseFromDnP, bptmTxnId, "FailureResponseFromDnP in MCPforBTPlusMarker method with orderkey as " + OrderKey);

                            e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                            if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                                e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                            else
                                e2eData.endOutboundCall(e2eData.toString());
                            //sending failure notification
                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true);
                            LogHelper.LogErrorMessage(Failed, bptmTxnId, "failed notification sent in MCPforBTPlusMarker method with orderkey as " + OrderKey);
                            //Logger.Write(OrderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                        }
                    }
                    else
                    {
                        //Logger.Write(OrderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                        LogHelper.LogErrorMessage(NullResponseFromDnP, bptmTxnId, NullResponseFromDnP + "in MCPforBTPlusMarker method with orderkey as " + OrderKey);

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //sending failure notification
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true);
                        LogHelper.LogErrorMessage(Failed, bptmTxnId, "failed notification sent in MCPforBTPlusMarker method with orderkey as " + OrderKey);
                        //Logger.Write(OrderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                    }
                }

            }
            catch (Exception ex)
            {
                //Logger.Write(OrderKey + "," + Failed + "," + FailureResponseFromDnP + ex.Message, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                LogHelper.LogErrorMessage(ex.Message.ToString(), bptmTxnId, "exception in MCPforBTPlusMarker method with orderkey as " + OrderKey);

                //sending failure notification
                //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = e2eData;
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true);
                LogHelper.LogErrorMessage(ex.Message.ToString(), bptmTxnId, "exception in MCPforBTPlusMarker method with orderkey as " + OrderKey);
                //Logger.Write(OrderKey + "," + Failed + "," + FailedErrorMessage + ex.Message, Logger.TypeEnum.BTPlusMarkerExceptionTrace);
                //Logger.Write(OrderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.BTPlusMarkerMessageTrace);
            }
            finally
            {

            }
        }

        /// <summary>
        /// to craete serviceroles for SI
        /// </summary>
        /// <param name="BAC">bac</param>
        /// <param name="BtOneId">btoneid</param>
        /// <returns></returns>
        public static ClientServiceRole[] CreateServiceRoles(string BAC, string BtOneId)
        {
            ClientServiceRole[] clientServiceRole = new ClientServiceRole[2];

            clientServiceRole[0] = new ClientServiceRole();
            clientServiceRole[0].id = ADMIN_ROLE;
            clientServiceRole[0].clientServiceRoleStatus = new ClientServiceRoleStatus();
            clientServiceRole[0].clientServiceRoleStatus.value = ACTIVE;
            clientServiceRole[0].clientIdentity = new ClientIdentity[1];
            clientServiceRole[0].clientIdentity[0] = new ClientIdentity();
            clientServiceRole[0].clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
            clientServiceRole[0].clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
            clientServiceRole[0].clientIdentity[0].value = BAC;
            clientServiceRole[0].clientIdentity[0].action = ACTION_INSERT;
            clientServiceRole[0].action = ACTION_INSERT;

            clientServiceRole[1] = new ClientServiceRole();
            clientServiceRole[1].id = DEFAULT_ROLE;
            clientServiceRole[1].clientServiceRoleStatus = new ClientServiceRoleStatus();
            clientServiceRole[1].clientServiceRoleStatus.value = ACTIVE;

            clientServiceRole[1].clientIdentity = new ClientIdentity[1];
            clientServiceRole[1].clientIdentity[0] = new ClientIdentity();
            clientServiceRole[1].clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
            clientServiceRole[1].clientIdentity[0].managedIdentifierDomain.value = BTCOM;
            clientServiceRole[1].clientIdentity[0].value = BtOneId;
            clientServiceRole[1].clientIdentity[0].action = ACTION_INSERT;
            clientServiceRole[1].action = ACTION_INSERT;

            return clientServiceRole;
        }


        public void AssociateBTPlusMarkerRequestMapper(OrderRequest requestOrderRequest, Guid btguid, System.DateTime ActivityStartedTime, ref E2ETransaction e2eData)
        {
            MSEOOrderNotification notification = null;
            bool IsExsitingBacID = false;
            bool isAHTDone = false;
            bool isServiceAlreadyExist = false;
            string action = string.Empty;
            string reason = string.Empty;
            string OrderKey = string.Empty;
            string BACNumber = string.Empty;
            string BtOneId = string.Empty;
            guid = btguid;
            this.ActivityStartTime = ActivityStartedTime;

            try
            {
                OrderKey = requestOrderRequest.Order.OrderIdentifier.Value;
                notification = new MSEOOrderNotification(requestOrderRequest);
                this.bptmTxnId = BPTMHelper.GetTxnIdFromE2eData(e2eData.toString());

                if (requestOrderRequest.Order.OrderItem[0].Action != null && requestOrderRequest.Order.OrderItem[0].Action.Code != null)
                {
                    action = requestOrderRequest.Order.OrderItem[0].Action.Code.ToString();
                    //hcsParameters.OrderAction = action;
                    if (requestOrderRequest.Order.OrderItem[0].Action.Reason != null)
                    {
                        reason = requestOrderRequest.Order.OrderItem[0].Action.Reason.ToString();
                    }
                }

                foreach (InstanceCharacteristic insChar in requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic)
                {
                    if ((insChar.Name.Equals("BillAccountNumber", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                    {
                        BACNumber = insChar.Value;
                        break;
                    }
                }
                if (!String.IsNullOrEmpty(BACNumber))
                {
                    GetClientProfileV1Res bacProfileResponse = null;
                    ClientServiceInstanceV1[] bacServiceResponse = null;

                    bacProfileResponse = DnpWrapper.GetClientProfileV1ForThomas(BACNumber, BACID_IDENTIFER_NAMEPACE, OrderKey);

                    if (bacProfileResponse != null && bacProfileResponse.clientProfileV1 != null && bacProfileResponse.clientProfileV1.client != null && bacProfileResponse.clientProfileV1.client.clientIdentity != null)
                    {
                        IsExsitingBacID = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(BACNumber, StringComparison.OrdinalIgnoreCase));
                        if (IsExsitingBacID)
                        {
                            BT.SaaS.IspssAdapter.Dnp.ClientIdentity bacClientIdentity = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(BACNumber, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                            if (bacClientIdentity.clientIdentityValidation != null)
                                isAHTDone = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                        }
                        if (bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(i => i.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                        {
                            BtOneId = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                        }
                        if (bacProfileResponse.clientProfileV1.clientServiceInstanceV1 != null && bacProfileResponse.clientProfileV1.clientServiceInstanceV1.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(Associate_BT_Plus_Marker_Namespace)))
                        {
                            isServiceAlreadyExist = true;
                        }
                        else
                        {
                            //BTPlus for AHT Delinked scenarios...
                            bacServiceResponse = DnpWrapper.getServiceInstanceV1(BACNumber, BACID_IDENTIFER_NAMEPACE, Associate_BT_Plus_Marker_Namespace);

                            if ((bacServiceResponse != null) && (bacServiceResponse.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(Associate_BT_Plus_Marker_Namespace, StringComparison.OrdinalIgnoreCase))))
                            {
                                isServiceAlreadyExist = true;
                            }
                        }
                    }
                    else
                    {
                        bacServiceResponse = DnpWrapper.getServiceInstanceV1(BACNumber, BACID_IDENTIFER_NAMEPACE, Associate_BT_Plus_Marker_Namespace);

                        if ((bacServiceResponse != null) && (bacServiceResponse.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(Associate_BT_Plus_Marker_Namespace, StringComparison.OrdinalIgnoreCase))))
                        {
                            isServiceAlreadyExist = true;
                        }
                    }

                    if (action.Equals("create", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!isServiceAlreadyExist)
                        {
                            //sending accepted notification                         
                            notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, Accepted, System.DateTime.Now - ActivityStartTime, "BTPlusMarkerTrace", OrderKey);

                            if (isAHTDone)
                                MCPforBTPlusMarker(BACNumber, BtOneId, Associate_BT_Plus_Marker_Namespace, OrderKey, notification, ref e2eData);
                            else
                                MSIforBTPlusMarker(BACNumber, action, Associate_BT_Plus_Marker_Namespace, OrderKey, notification, ref e2eData);
                        }
                        else
                        {
                            notification.sendNotification(false, false, "001", "The given bac already have AssociateBTPlusMarker Service", ref e2eData, true);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + " The given bac already have AssociateBTPlusMarker Service", System.DateTime.Now - ActivityStartTime, "BTPlusMarkerTrace", OrderKey);
                        }

                    }
                    else if (action.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                    {
                        if (isServiceAlreadyExist)
                        {
                            //sending accepted notification 
                            notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, Accepted, System.DateTime.Now - ActivityStartTime, "BTPlusMarkerTrace", OrderKey);

                            MSIforBTPlusMarker(BACNumber, action,Associate_BT_Plus_Marker_Namespace, OrderKey, notification, ref e2eData);
                        }
                        else
                        {
                            notification.sendNotification(false, false, "001", "The given bac doesn't have AssociateBTPlusMarker Service to cease", ref e2eData, true);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "The given bac doesn't have AssociateBTPlusMarker Service to cease", System.DateTime.Now - ActivityStartTime, "BTPlusMarkerTrace", OrderKey);
                        }
                    }
                    else
                    {
                        notification.sendNotification(false, false, "001", "Bad Request from MQService", ref e2eData, true);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "Bad Request from MQService", System.DateTime.Now - ActivityStartTime, "BTPlusMarkerTrace", OrderKey);
                    }
                }
            }

            catch (DnpException exception)
            {
                notification.sendNotification(false, false, "001", exception.Message, ref e2eData, true);
                LogHelper.LogErrorMessage(exception.Message.ToString(), bptmTxnId, " in BTPlusMarkerReqeustMapper method with orderkey as " + OrderKey);
                //Logger.Write(OrderKey + "," + Ignored + "," + IgnoredNotificationSent + exception.Message, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                //Logger.Write(OrderKey + "," + Ignored + "," + IgnoredNotificationSent + exception.Message, Logger.TypeEnum.BTPlusMarkerExceptionTrace);

            }
            catch (Exception ex)
            {
                notification.sendNotification(false, false, "001", ex.Message, ref e2eData, true);
                LogHelper.LogErrorMessage(ex.Message.ToString(), bptmTxnId, " in BTPlusMarkerReqeustMapper method with orderkey as " + OrderKey);

                //Logger.Write(OrderKey + "," + Ignored + "," + IgnoredNotificationSent + ex.Message, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                //Logger.Write(OrderKey + "," + Ignored + "," + IgnoredNotificationSent + ex.Message, Logger.TypeEnum.BTPlusMarkerExceptionTrace);
            }
        }
    
    }
}
