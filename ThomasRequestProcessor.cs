using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BT.SaaS.IspssAdapter;
using BT.SaaS.IspssAdapter.Dnp;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Configuration;
using com.bt.util.logging;
namespace BT.SaaS.MSEOAdapter
{
    public class ThomasRequestProcessor
    {
        const string BACID_IDENTIFER_NAMEPACE = "VAS_BILLINGACCOUNT_ID";
        const string BTSKY_SERVICECODE_NAMEPACE = "BTSPORT:DIGITAL";
        const string CAKID_IDENTIFER_NAMEPACE = "CAKID";
        const string GotResponseFromDnPWithBusinessError = "GotResponseFromDnPWithBusinessError";
        const string StartedDnPCall = "StartedDnPCall";
        const string GotResponseFromDnP = "GotResponseFromDnP";
        const string Accepted = "Accepted";
        const string Ignored = "Ignored";
        const string Completed = "Completed";
        const string Failed = "Failed";
        const string MakingDnPcall = "Making DnP call";
        const string NonFunctionalExceptionFromDnP = "Non Functional Exception from DNP";
        const string AcceptedNotificationSent = "Accepted Notification sent";
        const string IgnoredNotificationSent = "Ignored notification sent: ";
        const string CompletedNotificationSent = "Completed Notification sent";
        const string FailedNotificationSent = "Failure Notification sent";
        const string SendingRequesttoDnP = "Sending the Request to DNP";
        const string ModifyProfileAHTDone = "Sending the Request (ModifyProfileAhtDone) to DNP";
        const string GotResponsefromDnPNotification = "recieved  the Response from  DNP";
        const string GotResponsefromDnPModifyProfileAHTDone = "Recieved response from (ModifyProfileAhtDone) DNP";
        const string FailureResponseFromDnP = "recieved failure response from DnP ";
        const string FailedErrorMessage = "Order failed with error messgae: ";
        const string NonFunctionalException = "reached max number of Retry ";
        const string NullResponseFromDnP = "Response is null from DnP";
        const string DnPAdminstratorFailedResponse = "recieved failure response from DnP (Administrator): ";
        const string BTCOM = "BTCOM";
        const string OffnetScode = "S0398055";
        const string EEScode = "S0342911";
        const string HDRScode = "S0443464";
        private const string ACTION_CEASED = "CEASED";
        private const string ACTION_SEARCH = "SEARCH";
        private const string ACTION_LINK = "LINK";
        private const string ACTION_UPDATE = "UPDATE";
        private const string ACTION_DELETE = "DELETE";
        private const string ACTION_UNLINK = "UNLINK";
        private const string ACTION_INSERT = "INSERT";
        private const string ACTION_EXPIRED = "EXPIRED";
        private const string DEFAULT_ROLE = "DEFAULT";
        private const string ADMIN_ROLE = "ADMIN";
        private const string SUSPENDED = "SUSPENDED";
        private const string INACTIVE = "INACTIVE";
        private const string ACTIVE = "ACTIVE";

        System.DateTime ActivityStartTime = System.DateTime.Now; Guid guid = new Guid(); string bptmTxnId = string.Empty;
        public void ThomasRequestMapper(OrderRequest requestOrderRequest, Guid btguid, System.DateTime ActivityStartedTime, ref E2ETransaction e2eData)
        {
            MSEOOrderNotification notification = null;
            bool IsExsitingBacID = false;
            bool isAHTDone = false;
            string action = string.Empty;
            string reason = string.Empty;
            guid = btguid;
            this.ActivityStartTime = ActivityStartedTime;

            ThomasParameters thomasParameters = new ThomasParameters();
            try
            {
                this.bptmTxnId = BPTMHelper.GetTxnIdFromE2eData(e2eData.toString());

                thomasParameters.orderKey = requestOrderRequest.Order.OrderIdentifier.Value;
                notification = new MSEOOrderNotification(requestOrderRequest);

                if (requestOrderRequest.Order.OrderItem[0].Action != null && requestOrderRequest.Order.OrderItem[0].Action.Code != null)
                {
                    action = requestOrderRequest.Order.OrderItem[0].Action.Code.ToString();
                    thomasParameters.OrderAction = action;
                    if (requestOrderRequest.Order.OrderItem[0].Action.Reason != null && !string.IsNullOrEmpty(requestOrderRequest.Order.OrderItem[0].Action.Reason))
                        thomasParameters.Reason = requestOrderRequest.Order.OrderItem[0].Action.Reason.ToString();
                }

                foreach (InstanceCharacteristic insChar in requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic)
                {
                    if ((insChar.Name.Equals("BillAccountNumber", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                    {
                        thomasParameters.BillAccountNumber = insChar.Value;
                    }
                    else if ((insChar.Name.Equals("BTOneId", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                    {
                        thomasParameters.BtOneId = insChar.Value;
                    }
                    else if ((insChar.Name.Equals("customerid", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                    {
                        thomasParameters.CustomerID = insChar.Value;
                    }
                    else if ((insChar.Name.Equals("serviceid", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                    {
                        thomasParameters.ServiceID = insChar.Value;
                    }
                    else if ((insChar.Name.Equals("scode", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                    {
                        thomasParameters.Scode = insChar.Value;
                    }
                    else if ((insChar.Name.Equals("BTS_APP_CODE", StringComparison.OrdinalIgnoreCase)))
                    {
                        thomasParameters.IsBTS_APP_CODEAttrExist = true;
                        if (!(string.IsNullOrEmpty(insChar.Value)))
                        {
                            thomasParameters.BTS_APP_CODE = insChar.Value;
                        }
                        if (!(string.IsNullOrEmpty(insChar.PreviousValue)))
                        {
                            thomasParameters.PreviousBTS_APP_CODE = insChar.PreviousValue;
                        }
                        if (thomasParameters.BTS_APP_CODE.Equals(OffnetScode) || thomasParameters.BTS_APP_CODE.Equals(EEScode))
                        {
                            thomasParameters.IsOffnetflow = true;
                        }
                    }
                    else if ((insChar.Name.Equals("HDR", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!(string.IsNullOrEmpty(insChar.Value)))
                        {
                            thomasParameters.HDR = insChar.Value;
                        }
                    }
                    else if ((insChar.Name.Equals("HDR_Add", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)) && insChar.Value.Equals("y", StringComparison.OrdinalIgnoreCase))
                    {
                        thomasParameters.IsHDRAdd = true;
                    }
                    else if ((insChar.Name.Equals("HDR_Delete", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)) && insChar.Value.Equals("y", StringComparison.OrdinalIgnoreCase))
                    {
                        thomasParameters.IsHDRDelete = true;
                    }
                    else if ((insChar.Name.Equals("Super", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                    {
                        thomasParameters.Super = insChar.Value;
                    }
                }

                //if (!string.IsNullOrEmpty(requestOrderRequest.StandardHeader.E2e.E2EDATA))
                //    e2eData = requestOrderRequest.StandardHeader.E2e.E2EDATA;

                if (action.Equals("cancel", StringComparison.OrdinalIgnoreCase)|| action.Equals("Reactivate", StringComparison.OrdinalIgnoreCase))
                {
                    ClientServiceInstanceV1[] clientServiceInstnce = DnpWrapper.getServiceInstanceV1(thomasParameters.BillAccountNumber, BACID_IDENTIFER_NAMEPACE, BTSKY_SERVICECODE_NAMEPACE);

                    if (action.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                        ThomasCeaseMapper(clientServiceInstnce, thomasParameters, notification, 0, ref e2eData);
                    else if (action.Equals("Reactivate", StringComparison.OrdinalIgnoreCase))
                        ThomasReactivateMapper(clientServiceInstnce, thomasParameters, notification, 0, ref e2eData);
                }
                else if (!String.IsNullOrEmpty(thomasParameters.BillAccountNumber))
                {
                    GetClientProfileV1Res bacProfileResponse = null;
                    GetClientProfileV1Res cakProfileResponse = null;
                    ClientServiceInstanceV1[] cakServiceResponse = null;
                    ClientServiceInstanceV1[] bacServiceResponse = null;

                    bacProfileResponse = DnpWrapper.GetClientProfileV1ForThomas(thomasParameters.BillAccountNumber, BACID_IDENTIFER_NAMEPACE, thomasParameters.orderKey);
                    cakServiceResponse = DnpWrapper.getServiceInstanceV1ForThomas(thomasParameters.CustomerID, CAKID_IDENTIFER_NAMEPACE, thomasParameters.orderKey);

                    if (cakServiceResponse == null)
                    {
                        bacServiceResponse = DnpWrapper.getServiceInstanceV1ForThomas(thomasParameters.BillAccountNumber, BACID_IDENTIFER_NAMEPACE, thomasParameters.orderKey);
                    }

                    if (bacProfileResponse != null && bacProfileResponse.clientProfileV1 != null)
                    {
                        IsExsitingBacID = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(thomasParameters.BillAccountNumber, StringComparison.OrdinalIgnoreCase));
                        if (IsExsitingBacID)
                        {
                            BT.SaaS.IspssAdapter.Dnp.ClientIdentity bacClientIdentity = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(thomasParameters.BillAccountNumber, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                            if (bacClientIdentity.clientIdentityValidation != null)
                                isAHTDone = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                            else
                            {
                                if (bacProfileResponse.clientProfileV1.clientServiceInstanceV1 != null && bacProfileResponse.clientProfileV1.clientServiceInstanceV1.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(BTSKY_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase) && csi.clientServiceRole != null && csi.clientServiceRole.ToList().Exists(csr => csr.id.Equals("ADMIN", StringComparison.Ordinal) && csr.clientServiceRoleStatus.value.Equals("INACTIVE", StringComparison.OrdinalIgnoreCase))))
                                    thomasParameters.IsAdminRoleStatusChangeRequired = true;
                            }
                        }
                        if (bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(i => i.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                        {
                            thomasParameters.BtOneId = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                        }
                    }
                    else if (thomasParameters.IsOffnetflow && string.IsNullOrEmpty(thomasParameters.BtOneId))
                    {
                        cakProfileResponse = DnpWrapper.GetClientProfileV1ForThomas(thomasParameters.CustomerID, CAKID_IDENTIFER_NAMEPACE, thomasParameters.orderKey);
                        if (cakProfileResponse != null && cakProfileResponse.clientProfileV1 != null && cakProfileResponse.clientProfileV1.client != null && cakProfileResponse.clientProfileV1.client.clientIdentity != null && cakProfileResponse.clientProfileV1.client.clientIdentity.Count() > 0
                            && cakProfileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM")))
                        {
                            thomasParameters.BtOneId = cakProfileResponse.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM")).FirstOrDefault().value;
                        }
                    }
                    ClientServiceInstanceV1[] serviceIntances = null;
                    ClientServiceInstanceCharacteristic[] serviceIntanceChars = null;
                    if (cakServiceResponse != null && cakServiceResponse.Length > 0)
                    {
                        serviceIntances = cakServiceResponse;
                    }

                    if (bacServiceResponse != null && bacServiceResponse.Length > 0)
                    {
                        if (serviceIntances != null)
                            serviceIntances = serviceIntances.Concat(bacServiceResponse).ToArray();
                        else
                            serviceIntances = bacServiceResponse;
                    }

                    if (serviceIntances != null)
                    {
                        ClientServiceInstanceV1 serviceInstance = null;
                        thomasParameters.ServiceInstanceExists = serviceIntances.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BTSKY_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase));
                        if (thomasParameters.ServiceInstanceExists)
                            serviceInstance = serviceIntances.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(BTSKY_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                        if (serviceInstance != null)
                        {
                            if (serviceInstance.clientServiceInstanceCharacteristic != null)
                                serviceIntanceChars = serviceInstance.clientServiceInstanceCharacteristic;

                            foreach (ClientServiceInstanceCharacteristic srvcChar in serviceInstance.clientServiceInstanceCharacteristic)
                            {
                                if (srvcChar.name != null && (srvcChar.name.Equals("scode", StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrEmpty(srvcChar.value))
                                {
                                    thomasParameters.IsScodeExist = true;
                                }

                                if (srvcChar.name != null && (srvcChar.name.Equals("BTS_APP_CODE", StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrEmpty(srvcChar.value))
                                {
                                    thomasParameters.IsBTS_APP_CODEExistinDnP = true;
                                }
                            }

                            if (thomasParameters.IsOffnetflow && serviceInstance.clientServiceRole != null && serviceInstance.clientServiceRole.Count() > 0)
                            {
                                if (serviceInstance.clientServiceRole.ToList().Exists(csr => csr.id.Equals("ADMIN") && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(CAKID_IDENTIFER_NAMEPACE))))
                                {
                                    thomasParameters.IsAdminRoleexistsonCAK = true;
                                }
                            }
                            //Thomas provide/Reactivate
                            if (action.Equals("create", StringComparison.OrdinalIgnoreCase))
                            {
                                if (serviceInstance.serviceIdentity.ToList().Exists(sID => sID.domain.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.OrdinalIgnoreCase) && sID.value.Equals(thomasParameters.BillAccountNumber, StringComparison.OrdinalIgnoreCase)))
                                {
                                    //Thomas reactiavte scenario
                                    thomasParameters.ServiceInstanceStatus = serviceInstance.clientServiceInstanceStatus.value;
                                    if (serviceIntanceChars != null && serviceIntanceChars.Length > 0)
                                    {
                                        if (serviceInstance.clientServiceInstanceStatus.value.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase) && serviceIntanceChars.ToList().Exists(ic => ic.name.Equals("BTAPP_SERVICE_ID", StringComparison.OrdinalIgnoreCase) && ic.value.Equals(thomasParameters.ServiceID, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            throw new DnpException("BTSPORT:DIGITAL service is already exists for " + thomasParameters.BillAccountNumber + " in DNP");
                                        }
                                        else
                                        {
                                            //checking switch state and scode in DnP for reactivate
                                            if (thomasParameters.ServiceInstanceStatus.Equals("ceased", StringComparison.OrdinalIgnoreCase)
                                                && ConfigurationManager.AppSettings["DNPThomasMigrationSwitch"].ToString().Equals("off", StringComparison.OrdinalIgnoreCase) && !thomasParameters.IsScodeExist)
                                            {
                                                throw new DnpException("Ignoring Reactivate request as DnP migration switch is OFF and scode doesnot exist in DnP");
                                            }
                                            else
                                            {
                                                if (isAHTDone)
                                                {
                                                    notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false, ActivityStartTime, guid, bptmTxnId);
                                                    Logger.Write(thomasParameters.orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.MessageTrace);
                                                    LogHelper.LogActivityDetails(bptmTxnId, guid, AcceptedNotificationSent, TimeSpan.Zero, "ThomasTrace", thomasParameters.orderKey);
                                                    ModifyProfileAhtDone(thomasParameters, notification, 0, ref e2eData);
                                                }
                                                else
                                                {
                                                    notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false, ActivityStartTime, guid, bptmTxnId);
                                                    Logger.Write(thomasParameters.orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.MessageTrace);
                                                    LogHelper.LogActivityDetails(bptmTxnId, guid, AcceptedNotificationSent, TimeSpan.Zero, "ThomasTrace", thomasParameters.orderKey);
                                                    thomasParameters.Identity = thomasParameters.CustomerID;
                                                    thomasParameters.IdentityDomain = CAKID_IDENTIFER_NAMEPACE;
                                                    if (thomasParameters.IsAdminRoleStatusChangeRequired)
                                                        MCPCalltoUpdateThomasServiceState(thomasParameters, notification,0, ref e2eData);
                                                    else
                                                        CreateThomasServiceInstance(thomasParameters, notification, 0, ref e2eData);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //Update the existing Thomas service with BAC as SI (Slow Track followed by Fast Track)
                                    if (isAHTDone)
                                    {
                                        notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false, ActivityStartTime, guid, bptmTxnId);
                                        Logger.Write(thomasParameters.orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.MessageTrace);
                                        LogHelper.LogActivityDetails(bptmTxnId, guid, AcceptedNotificationSent, TimeSpan.Zero, "ThomasTrace", thomasParameters.orderKey);
                                        ModifyProfileAhtDone(thomasParameters, notification, 0, ref e2eData);
                                    }
                                    else
                                    {
                                        notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false, ActivityStartTime, guid, bptmTxnId);
                                        Logger.Write(thomasParameters.orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.MessageTrace);
                                        LogHelper.LogActivityDetails(bptmTxnId, guid, AcceptedNotificationSent, TimeSpan.Zero, "ThomasTrace", thomasParameters.orderKey);

                                        if (thomasParameters.IsOffnetflow && (!string.IsNullOrEmpty(thomasParameters.BtOneId)) && thomasParameters.IsAdminRoleexistsonCAK)
                                        {
                                            CreateOffnetSportservicewithBAC(thomasParameters, notification, 0, ref e2eData);
                                        }
                                        else
                                        {
                                            thomasParameters.Identity = thomasParameters.CustomerID;
                                            thomasParameters.IdentityDomain = CAKID_IDENTIFER_NAMEPACE;
                                            CreateThomasServiceInstance(thomasParameters, notification, 0, ref e2eData);
                                        }
                                    }
                                }
                            }
                            //thomas regrade
                            else if (action.Equals("modify", StringComparison.OrdinalIgnoreCase))
                            {
                                if (serviceInstance.serviceIdentity.ToList().Exists(sID => sID.domain.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.OrdinalIgnoreCase) && sID.value.Equals(thomasParameters.BillAccountNumber, StringComparison.OrdinalIgnoreCase)))
                                {
                                    //setting serviceInstanceStatus to modify to reuse existing code
                                    thomasParameters.ServiceInstanceStatus = "modify";
                                    if (isAHTDone)
                                    {
                                        notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false, ActivityStartTime, guid, bptmTxnId);
                                        Logger.Write(thomasParameters.orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.MessageTrace);
                                        LogHelper.LogActivityDetails(bptmTxnId, guid, AcceptedNotificationSent, TimeSpan.Zero, "ThomasTrace", thomasParameters.orderKey);

                                        ModifyProfileAhtDone(thomasParameters, notification, 0, ref e2eData);
                                    }
                                    else
                                    {
                                        notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false, ActivityStartTime, guid, bptmTxnId);
                                        Logger.Write(thomasParameters.orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.MessageTrace);
                                        LogHelper.LogActivityDetails(bptmTxnId, guid, AcceptedNotificationSent, TimeSpan.Zero, "ThomasTrace", thomasParameters.orderKey);

                                        thomasParameters.Identity = thomasParameters.CustomerID;
                                        thomasParameters.IdentityDomain = CAKID_IDENTIFER_NAMEPACE;
                                        CreateThomasServiceInstance(thomasParameters, notification, 0, ref e2eData);
                                    }
                                }
                                else
                                {
                                    throw new DnpException("Ignoring regrade request as there is no Thomas service with BACID");
                                }
                            }
                        }
                    }
                    // create thomas service only during provision    
                    else if (action.Equals("create", StringComparison.OrdinalIgnoreCase))
                    {
                        if (isAHTDone)
                        {
                            notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false, ActivityStartTime, guid, bptmTxnId);
                            Logger.Write(thomasParameters.orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.MessageTrace);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, AcceptedNotificationSent, TimeSpan.Zero, "ThomasTrace", thomasParameters.orderKey);

                            ModifyProfileAhtDone(thomasParameters, notification, 0, ref e2eData);
                        }
                        else if (thomasParameters.IsOffnetflow && !string.IsNullOrEmpty(thomasParameters.BtOneId) && !isAHTDone)
                        {
                            CreateOffnetSportservicewithBAC(thomasParameters, notification, 0, ref e2eData);
                        }
                        else
                        {
                            //sending accepted notification 
                            notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false, ActivityStartTime, guid, bptmTxnId);
                            Logger.Write(thomasParameters.orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.MessageTrace);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, AcceptedNotificationSent, TimeSpan.Zero, "ThomasTrace", thomasParameters.orderKey);

                            thomasParameters.Identity = thomasParameters.BillAccountNumber;
                            thomasParameters.IdentityDomain = BACID_IDENTIFER_NAMEPACE;
                            CreateThomasServiceInstance(thomasParameters, notification, 0, ref e2eData);
                        }
                    }
                    // ignoring the request during regrade scenario if no thomas service exist 
                    else
                    {
                        throw new DnpException("Ignoring regrade request as there is no THOMAS service mapped for both CAKID and BACID");
                    }
                }
                else
                {
                    //sending accepted notification
                    notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false, ActivityStartTime, guid, bptmTxnId);
                    Logger.Write(thomasParameters.orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.MessageTrace);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, AcceptedNotificationSent, TimeSpan.Zero, "ThomasTrace", thomasParameters.orderKey);

                    thomasParameters.Identity = thomasParameters.CustomerID;
                    thomasParameters.IdentityDomain = CAKID_IDENTIFER_NAMEPACE;
                    if (thomasParameters.IsOffnetflow)
                    {
                        if (!string.IsNullOrEmpty(thomasParameters.BtOneId))
                        {
                            CreateOffnetThomasService(thomasParameters, notification, 0, ref e2eData);
                        }
                        else
                        {
                            CreateThomasServiceInstance(thomasParameters, notification, 0, ref e2eData);
                        }

                    }
                    else
                        CreateThomasServiceInstance(thomasParameters, notification, 0, ref e2eData);
                }
            }

            catch (DnpException exception)
            {
                notification.sendNotification(false, true, "001", exception.Message, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                Logger.Write(thomasParameters.orderKey + "," + Ignored + "," + IgnoredNotificationSent + exception.Message, Logger.TypeEnum.MessageTrace);
                Logger.Write(thomasParameters.orderKey + "," + Ignored + "," + IgnoredNotificationSent + exception.Message, Logger.TypeEnum.ExceptionTrace);
                LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "," + IgnoredNotificationSent + exception.Message, TimeSpan.Zero, "ThomasTrace", thomasParameters.orderKey);

            }
            catch (Exception ex)
            {
                notification.sendNotification(false, true, "001", ex.Message, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                Logger.Write(thomasParameters.orderKey + "," + Ignored + "," + IgnoredNotificationSent + ex.Message, Logger.TypeEnum.MessageTrace);
                Logger.Write(thomasParameters.orderKey + "," + Ignored + "," + IgnoredNotificationSent + ex.Message, Logger.TypeEnum.ExceptionTrace);
                LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "," + IgnoredNotificationSent + ex.Message, TimeSpan.Zero, "ThomasTrace", thomasParameters.orderKey);
            }
        }

        private void CreateOffnetSportservicewithBAC(ThomasParameters thomasParams, MSEOOrderNotification notification, short retryCount, ref E2ETransaction e2eData)
        {
            manageClientProfileV1Request1 request1 = new manageClientProfileV1Request1();
            request1.manageClientProfileV1Request = new ManageClientProfileV1Request();
            manageClientProfileV1Response1 profileResponse1 = new manageClientProfileV1Response1();

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

            ManageClientProfileV1Req manageClientProfileV1Req1 = new ManageClientProfileV1Req();

            ClientServiceInstanceV1[] clientServiceInstanceV1 = null;

            List<ClientServiceInstanceCharacteristic> clientSrvcInsCharsList = new List<ClientServiceInstanceCharacteristic>();
            ClientServiceInstanceCharacteristic clientSrvcInsChar = null;

            List<ClientIdentity> clientIdentityList = new List<ClientIdentity>();
            ClientIdentity clientIdentity = null;
            try
            {
                manageClientProfileV1Req1 = new ManageClientProfileV1Req();

                manageClientProfileV1Req1.clientProfileV1 = new ClientProfileV1();
                manageClientProfileV1Req1.clientProfileV1.client = new Client();
                manageClientProfileV1Req1.clientProfileV1.client.action = "UPDATE";

                clientIdentity = new ClientIdentity();
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = BTCOM;
                clientIdentity.value = thomasParams.BtOneId;
                clientIdentity.action = "SEARCH";
                clientIdentityList.Add(clientIdentity);

                if (thomasParams.IsAdminRoleexistsonCAK)
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = CAKID_IDENTIFER_NAMEPACE;
                    clientIdentity.value = thomasParams.CustomerID;
                    clientIdentity.action = "DELETE";
                    clientIdentityList.Add(clientIdentity);
                }

                clientIdentity = new ClientIdentity();
                clientIdentity.value = thomasParams.BillAccountNumber;
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                clientIdentity.action = "INSERT";
                clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                clientIdentity.clientIdentityStatus.value = "ACTIVE";
                clientIdentity.clientIdentityValidation = new ClientIdentityValidation[1];
                clientIdentity.clientIdentityValidation[0] = new ClientIdentityValidation();
                clientIdentity.clientIdentityValidation[0].action = "INSERT";
                clientIdentity.clientIdentityValidation[0].name = "ACCOUNTTRUSTMETHOD";
                clientIdentity.clientIdentityValidation[0].value = "claimed";

                clientIdentityList.Add(clientIdentity);

                #region role insert
                List<ClientServiceRole> ListclientServicerole = new List<ClientServiceRole>();
                ClientServiceRole clientServiceRole = null;

                //as part of 136847 created admin role for CAKID need to delete and insert bac admin.
                if (thomasParams.IsAdminRoleexistsonCAK)
                {
                    clientServiceRole = new ClientServiceRole();
                    clientServiceRole.id = "ADMIN";
                    clientServiceRole.clientIdentity = new ClientIdentity[1];
                    clientServiceRole.clientIdentity[0] = new ClientIdentity();
                    clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = CAKID_IDENTIFER_NAMEPACE;
                    clientServiceRole.clientIdentity[0].value = thomasParams.CustomerID;
                    clientServiceRole.clientIdentity[0].action = "SEARCH";
                    clientServiceRole.action = "DELETE";
                    ListclientServicerole.Add(clientServiceRole);
                }
                else
                {
                    clientServiceRole = new ClientServiceRole();
                    clientServiceRole.id = "DEFAULT";
                    clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                    clientServiceRole.clientServiceRoleStatus.value = "ACTIVE";
                    if (!string.IsNullOrEmpty(thomasParams.BtOneId))
                    {
                        clientServiceRole.clientIdentity = new ClientIdentity[1];
                        clientServiceRole.clientIdentity[0] = new ClientIdentity();
                        clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = BTCOM;
                        clientServiceRole.clientIdentity[0].value = thomasParams.BtOneId;
                        clientServiceRole.clientIdentity[0].action = "INSERT";
                    }
                    clientServiceRole.action = "INSERT";
                    ListclientServicerole.Add(clientServiceRole);
                }

                clientServiceRole = new ClientServiceRole();
                clientServiceRole.id = "ADMIN";
                clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                clientServiceRole.clientServiceRoleStatus.value = "ACTIVE";

                clientServiceRole.clientIdentity = new ClientIdentity[1];
                clientServiceRole.clientIdentity[0] = new ClientIdentity();
                clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                clientServiceRole.clientIdentity[0].value = thomasParams.BillAccountNumber;
                clientServiceRole.clientIdentity[0].action = "INSERT";
                clientServiceRole.action = "INSERT";
                ListclientServicerole.Add(clientServiceRole);
                #endregion

                if (thomasParams.ServiceInstanceExists)
                {
                    clientServiceInstanceV1 = new ClientServiceInstanceV1[2];

                    clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                    clientServiceInstanceV1[0].action = "LINK";
                    clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                    clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BTSKY_SERVICECODE_NAMEPACE;

                    clientServiceInstanceV1[1] = new ClientServiceInstanceV1();
                    clientServiceInstanceV1[1].action = "UPDATE";
                    clientServiceInstanceV1[1].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                    clientServiceInstanceV1[1].clientServiceInstanceIdentifier.value = BTSKY_SERVICECODE_NAMEPACE;

                    ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];

                    serviceIdentity[0] = new ServiceIdentity();
                    serviceIdentity[0].domain = CAKID_IDENTIFER_NAMEPACE;
                    serviceIdentity[0].value = thomasParams.CustomerID;
                    serviceIdentity[0].action = "SEARCH";
                    clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;

                    ServiceIdentity[] serviceIdentity1 = new ServiceIdentity[3];

                    serviceIdentity1[0] = new ServiceIdentity();
                    serviceIdentity1[0].domain = CAKID_IDENTIFER_NAMEPACE;
                    serviceIdentity1[0].value = thomasParams.CustomerID;
                    serviceIdentity1[0].action = "SEARCH";

                    serviceIdentity1[1] = new ServiceIdentity();
                    serviceIdentity1[1].domain = BACID_IDENTIFER_NAMEPACE;
                    serviceIdentity1[1].value = thomasParams.BillAccountNumber;
                    serviceIdentity1[1].action = "LINK";

                    serviceIdentity1[2] = new ServiceIdentity();
                    serviceIdentity1[2].domain = CAKID_IDENTIFER_NAMEPACE;
                    serviceIdentity1[2].value = thomasParams.CustomerID;
                    serviceIdentity1[2].action = "UNLINK";

                    clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                    clientSrvcInsChar.value = thomasParams.ServiceID;
                    clientSrvcInsChar.name = "BTAPP_SERVICE_ID";
                    clientSrvcInsChar.action = "UPDATE";
                    clientSrvcInsCharsList.Add(clientSrvcInsChar);

                    clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                    clientSrvcInsChar.value = thomasParams.Scode;
                    clientSrvcInsChar.name = "SCODE";
                    if (thomasParams.IsScodeExist)
                        clientSrvcInsChar.action = "UPDATE";
                    else
                        clientSrvcInsChar.action = "INSERT";
                    clientSrvcInsCharsList.Add(clientSrvcInsChar);

                    if (!string.IsNullOrEmpty(thomasParams.BTS_APP_CODE))
                    {
                        clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                        clientSrvcInsChar.value = thomasParams.BTS_APP_CODE;
                        clientSrvcInsChar.name = "BTS_APP_CODE";
                        if (thomasParams.IsBTS_APP_CODEExistinDnP)
                            clientSrvcInsChar.action = "UPDATE";
                        else
                            clientSrvcInsChar.action = "INSERT";
                        clientSrvcInsCharsList.Add(clientSrvcInsChar);
                    }

                    if (!string.IsNullOrEmpty(thomasParams.HDR))
                    {
                        ClientServiceInstanceCharacteristic clientSrvcChar = new ClientServiceInstanceCharacteristic();
                        clientSrvcChar.value = thomasParams.HDR;
                        clientSrvcChar.name = "HDR";
                        if (thomasParams.OrderAction.Equals("Create", StringComparison.OrdinalIgnoreCase))
                            clientSrvcChar.action = "FORCE_INS";
                        else
                        {
                            if (thomasParams.IsHDRAdd)
                                clientSrvcChar.action = "FORCE_INS";
                            else if (thomasParams.IsHDRDelete)
                                clientSrvcChar.action = "DELETE";
                        }
                        if (thomasParams.IsHDRDelete && !Convert.ToBoolean(ConfigurationManager.AppSettings["isDnPMigrationforHDRCompleted"]))
                        {
                            //donot delete the HDR from serviceinstance chars.
                        }
                        else
                            clientSrvcInsCharsList.Add(clientSrvcChar);
                    }

                    clientServiceInstanceV1[1].clientServiceInstanceCharacteristic = clientSrvcInsCharsList.ToArray();
                    clientServiceInstanceV1[1].clientServiceRole = ListclientServicerole.ToArray();
                    clientServiceInstanceV1[1].serviceIdentity = serviceIdentity1;

                    manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;
                }
                //Thomas Provision on BAC
                else
                {
                    clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                    clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                    clientServiceInstanceV1[0].name = Guid.NewGuid().ToString();

                    clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                    clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BTSKY_SERVICECODE_NAMEPACE;

                    clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                    clientServiceInstanceV1[0].clientServiceInstanceStatus.value = "ACTIVE";
                    //clientServiceInstanceV1[0].clientServiceInstanceCharacteristic = new ClientServiceInstanceCharacteristic[2];
                    //clientServiceInstanceV1[0].clientServiceInstanceCharacteristic[0] = new ClientServiceInstanceCharacteristic();
                    //clientServiceInstanceV1[0].clientServiceInstanceCharacteristic[0].value = btssid;
                    //clientServiceInstanceV1[0].clientServiceInstanceCharacteristic[0].name = "BTAPP_SERVICE_ID";
                    //clientServiceInstanceV1[0].clientServiceInstanceCharacteristic[0].action = "INSERT";
                    //clientServiceInstanceV1[0].clientServiceInstanceCharacteristic[1] = new ClientServiceInstanceCharacteristic();
                    //clientServiceInstanceV1[0].clientServiceInstanceCharacteristic[1].value = scode;
                    //clientServiceInstanceV1[0].clientServiceInstanceCharacteristic[1].name = "SCODE";
                    //clientServiceInstanceV1[0].clientServiceInstanceCharacteristic[1].action = "INSERT";
                    //clientServiceInstanceV1[0].action = "INSERT";

                    clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                    clientSrvcInsChar.value = thomasParams.ServiceID;
                    clientSrvcInsChar.name = "BTAPP_SERVICE_ID";
                    clientSrvcInsChar.action = "INSERT";
                    clientSrvcInsCharsList.Add(clientSrvcInsChar);

                    clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                    clientSrvcInsChar.value = thomasParams.Scode;
                    clientSrvcInsChar.name = "SCODE";
                    clientSrvcInsChar.action = "INSERT";
                    clientSrvcInsCharsList.Add(clientSrvcInsChar);

                    if (!string.IsNullOrEmpty(thomasParams.BTS_APP_CODE))
                    {
                        clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                        clientSrvcInsChar.value = thomasParams.BTS_APP_CODE;
                        clientSrvcInsChar.name = "BTS_APP_CODE";
                        clientSrvcInsChar.action = "INSERT";
                        clientSrvcInsCharsList.Add(clientSrvcInsChar);
                    }

                    if (!string.IsNullOrEmpty(thomasParams.HDR))
                    {
                        clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                        clientSrvcInsChar.value = thomasParams.HDR;
                        clientSrvcInsChar.name = "HDR";
                        if (thomasParams.OrderAction.Equals("Create", StringComparison.OrdinalIgnoreCase))
                            clientSrvcInsChar.action = "FORCE_INS";
                        else
                        {
                            if (thomasParams.IsHDRAdd)
                                clientSrvcInsChar.action = "FORCE_INS";
                            else if (thomasParams.IsHDRDelete)
                                clientSrvcInsChar.action = "DELETE";
                        }
                        if (thomasParams.IsHDRDelete && !Convert.ToBoolean(ConfigurationManager.AppSettings["isDnPMigrationforHDRCompleted"]))
                        {
                            //donot delete the HDR from serviceinstance chars.
                        }
                        else
                            clientSrvcInsCharsList.Add(clientSrvcInsChar);
                    }
                    else if (thomasParams.IsOffnetflow)
                    {
                        ClientServiceInstanceCharacteristic clientSrvcChar = new ClientServiceInstanceCharacteristic();
                        clientSrvcChar.value = HDRScode;
                        clientSrvcChar.name = "HDR";
                        clientSrvcChar.action = "FORCE_INS";
                        clientSrvcInsCharsList.Add(clientSrvcChar);
                    }

                    clientServiceInstanceV1[0].clientServiceInstanceCharacteristic = clientSrvcInsCharsList.ToArray();
                    clientServiceInstanceV1[0].action = "INSERT";

                    ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                    serviceIdentity[0] = new ServiceIdentity();
                    serviceIdentity[0].value = thomasParams.BillAccountNumber;
                    serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                    serviceIdentity[0].action = "LINK";

                    clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                    clientServiceInstanceV1[0].clientServiceRole = ListclientServicerole.ToArray();

                    manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;
                }

                manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                request1.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;
                request1.manageClientProfileV1Request.standardHeader = headerBlock;

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, BTSKY_SERVICECODE_NAMEPACE);
                request1.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                request1.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                Logger.Write(thomasParams.orderKey + "," + MakingDnPcall + "," + ModifyProfileAHTDone, Logger.TypeEnum.MessageTrace);
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, thomasParams.orderKey, request1.SerializeObject(), ConfigurationManager.AppSettings["BTSportsProductCode"].ToString());
                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey, request1.SerializeObject());

                profileResponse1 = DnpWrapper.manageClientProfileV1Thomas(request1, thomasParams.orderKey);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey, profileResponse1.SerializeObject());
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, thomasParams.orderKey, profileResponse1.SerializeObject(), ConfigurationManager.AppSettings["BTSportsProductCode"].ToString());
                Logger.Write(thomasParams.orderKey + "," + GotResponseFromDnP + "," + GotResponsefromDnPModifyProfileAHTDone, Logger.TypeEnum.MessageTrace);

                if (profileResponse1 != null
                   && profileResponse1.manageClientProfileV1Response != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    //sending completed notification
                    //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA;
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                    Logger.Write(thomasParams.orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.MessageTrace);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, CompletedNotificationSent, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                }
                else
                {
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedErrorMessage + errorMessage, Logger.TypeEnum.ExceptionTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + FailedErrorMessage + errorMessage, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + DnPAdminstratorFailedResponse + errorMessage, Logger.TypeEnum.MessageTrace);
                            if (retryCount++ < System.Int32.Parse(ConfigurationManager.AppSettings["RetryCountForThomasProvide"]))
                            {
                                if (retryCount == System.Int32.Parse(ConfigurationManager.AppSettings["RetryCountForThomasProvide"]))
                                {
                                    Logger.Write(thomasParams.orderKey + "," + Failed + "," + NonFunctionalException + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.ExceptionTrace);
                                    //Logger.Write("Non Functional Exception from DNP, reached max number of Retry , we are shutdowning service" + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.EmailTrace);
                                    //Process.GetCurrentProcess().Kill();

                                    //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA;
                                    e2eData.logMessage(GotResponseFromDnP, "");
                                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                                    else
                                        e2eData.endOutboundCall(e2eData.toString());

                                    notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                                    Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                                }
                                else
                                {
                                    Thread.Sleep(System.Int32.Parse(ConfigurationManager.AppSettings["SleepTimeoutInMilliSec"]));
                                    CreateOffnetThomasService(thomasParams, notification, retryCount, ref e2eData);
                                }
                            }
                        }
                        else
                        {
                            //sending failure notification
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailureResponseFromDnP + errorMessage, Logger.TypeEnum.MessageTrace);
                            e2eData.logMessage(GotResponseFromDnP, "");
                            if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                                e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                            else
                                e2eData.endOutboundCall(e2eData.toString());
                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                        }
                    }
                    else
                    {
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.MessageTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + NullResponseFromDnP, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                        //sending failure notification
                        e2eData.logMessage(GotResponseFromDnP, "");
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailureResponseFromDnP + ex.Message, Logger.TypeEnum.MessageTrace);
                LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + FailedErrorMessage + ex.Message, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                //sending failure notification
                e2eData.logMessage(GotResponseFromDnP, "");
                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());
                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
            }
            finally
            {
                clientSrvcInsCharsList = null;
            }
        }

        private void CreateOffnetThomasService(ThomasParameters thomasParams, MSEOOrderNotification notification, short retryCount, ref E2ETransaction e2eData)
        {
            manageClientProfileV1Request1 request1 = new manageClientProfileV1Request1();
            request1.manageClientProfileV1Request = new ManageClientProfileV1Request();
            manageClientProfileV1Response1 profileResponse1 = new manageClientProfileV1Response1();

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

            ManageClientProfileV1Req manageClientProfileV1Req1 = new ManageClientProfileV1Req();
            List<ClientServiceInstanceCharacteristic> listClientSrvcInstnceChar = new List<ClientServiceInstanceCharacteristic>();
            ClientServiceInstanceCharacteristic clientSrvcChar = null;

            List<ClientIdentity> clientIdentityList = new List<ClientIdentity>();
            ClientIdentity clientIdentity = null;
            try
            {
                manageClientProfileV1Req1 = new ManageClientProfileV1Req();

                manageClientProfileV1Req1.clientProfileV1 = new ClientProfileV1();
                manageClientProfileV1Req1.clientProfileV1.client = new Client();
                manageClientProfileV1Req1.clientProfileV1.client.action = "UPDATE";

                clientIdentity = new ClientIdentity();
                clientIdentity.value = thomasParams.BtOneId;
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = BTCOM;
                clientIdentity.action = "SEARCH";
                clientIdentityList.Add(clientIdentity);

                clientIdentity = new ClientIdentity();
                clientIdentity.value = thomasParams.CustomerID;
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = CAKID_IDENTIFER_NAMEPACE;
                clientIdentity.action = "INSERT";
                clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                clientIdentity.clientIdentityStatus.value = "ACTIVE";
                clientIdentityList.Add(clientIdentity);

                ClientServiceInstanceV1[] clientServiceInstanceV1 = null;
                clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                clientServiceInstanceV1[0].name = Guid.NewGuid().ToString();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BTSKY_SERVICECODE_NAMEPACE;

                clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                clientServiceInstanceV1[0].clientServiceInstanceStatus.value = "ACTIVE";

                clientSrvcChar = new ClientServiceInstanceCharacteristic();
                clientSrvcChar.value = thomasParams.ServiceID;
                clientSrvcChar.name = "BTAPP_SERVICE_ID";
                clientSrvcChar.action = "INSERT";
                listClientSrvcInstnceChar.Add(clientSrvcChar);

                clientSrvcChar = new ClientServiceInstanceCharacteristic();
                clientSrvcChar.value = thomasParams.Scode;
                clientSrvcChar.name = "SCODE";
                clientSrvcChar.action = "INSERT";
                listClientSrvcInstnceChar.Add(clientSrvcChar);

                if (!string.IsNullOrEmpty(thomasParams.BTS_APP_CODE))
                {
                    clientSrvcChar = new ClientServiceInstanceCharacteristic();
                    clientSrvcChar.value = thomasParams.BTS_APP_CODE;
                    clientSrvcChar.name = "BTS_APP_CODE";
                    clientSrvcChar.action = "INSERT";
                    listClientSrvcInstnceChar.Add(clientSrvcChar);
                }

                if (!string.IsNullOrEmpty(thomasParams.HDR))
                {
                    clientSrvcChar = new ClientServiceInstanceCharacteristic();
                    clientSrvcChar.value = thomasParams.HDR;
                    clientSrvcChar.name = "HDR";
                    if (thomasParams.OrderAction.Equals("Create", StringComparison.OrdinalIgnoreCase))
                        clientSrvcChar.action = "FORCE_INS";
                    else
                    {
                        if (thomasParams.IsHDRAdd)
                            clientSrvcChar.action = "FORCE_INS";
                        else if (thomasParams.IsHDRDelete)
                            clientSrvcChar.action = "DELETE";
                    }
                    if (thomasParams.IsHDRDelete && !Convert.ToBoolean(ConfigurationManager.AppSettings["isDnPMigrationforHDRCompleted"]))
                    {
                        //donot delete the HDR from serviceinstance chars.
                    }
                    else
                        listClientSrvcInstnceChar.Add(clientSrvcChar);
                }
                else if (thomasParams.IsOffnetflow)
                {
                    clientSrvcChar = new ClientServiceInstanceCharacteristic();
                    clientSrvcChar.value = HDRScode;
                    clientSrvcChar.name = "HDR";
                    clientSrvcChar.action = "FORCE_INS";
                    listClientSrvcInstnceChar.Add(clientSrvcChar);
                }

                clientServiceInstanceV1[0].clientServiceInstanceCharacteristic = listClientSrvcInstnceChar.ToArray();
                clientServiceInstanceV1[0].action = "INSERT";

                ClientServiceRole[] clientServiceRole = new ClientServiceRole[2];

                clientServiceRole[0] = new ClientServiceRole();
                clientServiceRole[0].id = "ADMIN";
                clientServiceRole[0].clientServiceRoleStatus = new ClientServiceRoleStatus();
                clientServiceRole[0].clientServiceRoleStatus.value = "ACTIVE";

                clientServiceRole[0].clientIdentity = new ClientIdentity[1];
                clientServiceRole[0].clientIdentity[0] = new ClientIdentity();
                clientServiceRole[0].clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                clientServiceRole[0].clientIdentity[0].managedIdentifierDomain.value = CAKID_IDENTIFER_NAMEPACE;
                clientServiceRole[0].clientIdentity[0].value = thomasParams.CustomerID;
                clientServiceRole[0].clientIdentity[0].action = "INSERT";
                clientServiceRole[0].action = "INSERT";

                clientServiceRole[1] = new ClientServiceRole();
                clientServiceRole[1].id = "DEFAULT";
                clientServiceRole[1].clientServiceRoleStatus = new ClientServiceRoleStatus();
                clientServiceRole[1].clientServiceRoleStatus.value = "ACTIVE";
                if (!string.IsNullOrEmpty(thomasParams.BtOneId))
                {
                    clientServiceRole[1].clientIdentity = new ClientIdentity[1];
                    clientServiceRole[1].clientIdentity[0] = new ClientIdentity();
                    clientServiceRole[1].clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientServiceRole[1].clientIdentity[0].managedIdentifierDomain.value = BTCOM;
                    clientServiceRole[1].clientIdentity[0].value = thomasParams.BtOneId;
                    clientServiceRole[1].clientIdentity[0].action = "INSERT";
                }
                clientServiceRole[1].action = "INSERT";
                clientServiceInstanceV1[0].clientServiceRole = clientServiceRole;

                ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                serviceIdentity[0] = new ServiceIdentity();
                serviceIdentity[0].value = thomasParams.Identity;
                serviceIdentity[0].domain = thomasParams.IdentityDomain;
                serviceIdentity[0].action = "LINK";

                clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;

                manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;
                manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();

                request1.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;
                request1.manageClientProfileV1Request.standardHeader = headerBlock;

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, BTSKY_SERVICECODE_NAMEPACE);
                request1.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                request1.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                Logger.Write(thomasParams.orderKey + "," + MakingDnPcall + "," + ModifyProfileAHTDone, Logger.TypeEnum.MessageTrace);
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, thomasParams.orderKey, request1.SerializeObject(), ConfigurationManager.AppSettings["BTSportsProductCode"].ToString());
                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey, request1.SerializeObject());

                profileResponse1 = DnpWrapper.manageClientProfileV1Thomas(request1, thomasParams.orderKey);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey, profileResponse1.SerializeObject());
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, thomasParams.orderKey, profileResponse1.SerializeObject(), ConfigurationManager.AppSettings["BTSportsProductCode"].ToString());
                Logger.Write(thomasParams.orderKey + "," + GotResponseFromDnP + "," + GotResponsefromDnPModifyProfileAHTDone, Logger.TypeEnum.MessageTrace);

                if (profileResponse1 != null
                   && profileResponse1.manageClientProfileV1Response != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    //sending completed notification
                    //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA;
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                    Logger.Write(thomasParams.orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.MessageTrace);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, CompletedNotificationSent, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                }
                else
                {
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedErrorMessage + errorMessage, Logger.TypeEnum.ExceptionTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + FailedErrorMessage + errorMessage, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + DnPAdminstratorFailedResponse + errorMessage, Logger.TypeEnum.MessageTrace);
                            if (retryCount++ < System.Int32.Parse(ConfigurationManager.AppSettings["RetryCountForThomasProvide"]))
                            {
                                if (retryCount == System.Int32.Parse(ConfigurationManager.AppSettings["RetryCountForThomasProvide"]))
                                {
                                    Logger.Write(thomasParams.orderKey + "," + Failed + "," + NonFunctionalException + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.ExceptionTrace);
                                    //Logger.Write("Non Functional Exception from DNP, reached max number of Retry , we are shutdowning service" + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.EmailTrace);
                                    //Process.GetCurrentProcess().Kill();

                                    //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA;
                                    e2eData.logMessage(GotResponseFromDnP, "");
                                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                                    else
                                        e2eData.endOutboundCall(e2eData.toString());

                                    notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                                    Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                                }
                                else
                                {
                                    Thread.Sleep(System.Int32.Parse(ConfigurationManager.AppSettings["SleepTimeoutInMilliSec"]));
                                    CreateOffnetThomasService(thomasParams, notification, retryCount, ref e2eData);
                                }
                            }
                        }
                        else
                        {
                            //sending failure notification
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailureResponseFromDnP + errorMessage, Logger.TypeEnum.MessageTrace);
                            e2eData.logMessage(GotResponseFromDnP, "");
                            if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                                e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                            else
                                e2eData.endOutboundCall(e2eData.toString());
                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                        }
                    }
                    else
                    {
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.MessageTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + NullResponseFromDnP, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                        //sending failure notification
                        e2eData.logMessage(GotResponseFromDnP, "");
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailureResponseFromDnP + ex.Message, Logger.TypeEnum.MessageTrace);
                LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + FailedErrorMessage + ex.Message, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                //sending failure notification
                e2eData.logMessage(GotResponseFromDnP, "");
                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());
                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
            }
            finally
            {
                listClientSrvcInstnceChar = null;
            }
        }

        public void CreateThomasServiceInstance(ThomasParameters thomasParams, MSEOOrderNotification notification, short retryCount, ref E2ETransaction e2eData)
        {
            manageServiceInstanceV1Request1 request = new manageServiceInstanceV1Request1();
            manageServiceInstanceV1Response1 profileResponse = null;

            request.manageServiceInstanceV1Request = new ManageServiceInstanceV1Request();
            request.manageServiceInstanceV1Request.standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            request.manageServiceInstanceV1Request.standardHeader.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            request.manageServiceInstanceV1Request.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            request.manageServiceInstanceV1Request.standardHeader.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
            ManageServiceInstanceV1Req manageServiceInstanceV1Req1 = new ManageServiceInstanceV1Req();
            List<ClientServiceInstanceCharacteristic> listClientSrvcInstnceChar = new List<ClientServiceInstanceCharacteristic>();
            ClientServiceInstanceCharacteristic clientSrvcChar = null;
            try
            {
                if (!string.IsNullOrEmpty(thomasParams.ServiceInstanceStatus) && (thomasParams.ServiceInstanceStatus.ToLower() == "ceased" || thomasParams.ServiceInstanceStatus.ToLower() == "active" || thomasParams.ServiceInstanceStatus.ToLower() == "modify"))
                {
                    ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];

                    serviceIdentity[0] = new ServiceIdentity();
                    serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                    serviceIdentity[0].value = thomasParams.BillAccountNumber;
                    serviceIdentity[0].action = "SEARCH";

                    manageServiceInstanceV1Req1.clientServiceInstanceV1 = new ClientServiceInstanceV1();
                    manageServiceInstanceV1Req1.clientServiceInstanceV1.action = "UPDATE";
                    manageServiceInstanceV1Req1.clientServiceInstanceV1.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                    manageServiceInstanceV1Req1.clientServiceInstanceV1.clientServiceInstanceIdentifier.value = BTSKY_SERVICECODE_NAMEPACE;

                    manageServiceInstanceV1Req1.clientServiceInstanceV1.clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                    manageServiceInstanceV1Req1.clientServiceInstanceV1.clientServiceInstanceStatus.value = "ACTIVE";

                    //update only Scode for regrade
                    if (!thomasParams.ServiceInstanceStatus.ToLower().Equals("modify"))
                    {
                        clientSrvcChar = new ClientServiceInstanceCharacteristic();
                        clientSrvcChar.value = thomasParams.ServiceID;
                        clientSrvcChar.name = "BTAPP_SERVICE_ID";
                        clientSrvcChar.action = "UPDATE";
                        listClientSrvcInstnceChar.Add(clientSrvcChar);

                        if (!string.IsNullOrEmpty(thomasParams.BTS_APP_CODE))
                        {
                            clientSrvcChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcChar.value = thomasParams.BTS_APP_CODE;
                            clientSrvcChar.name = "BTS_APP_CODE";
                            if (thomasParams.IsBTS_APP_CODEExistinDnP)
                                clientSrvcChar.action = "UPDATE";
                            else
                                clientSrvcChar.action = "INSERT";
                            listClientSrvcInstnceChar.Add(clientSrvcChar);
                        }
                    }

                    //Insert or delete HDR during regrade
                    if (thomasParams.ServiceInstanceStatus.ToLower().Equals("modify"))
                    {
                        if (!string.IsNullOrEmpty(thomasParams.HDR))
                        {
                            clientSrvcChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcChar.value = thomasParams.HDR;
                            clientSrvcChar.name = "HDR";
                            if (thomasParams.OrderAction.Equals("Create", StringComparison.OrdinalIgnoreCase))
                                clientSrvcChar.action = "FORCE_INS";
                            else
                            {
                                if (thomasParams.IsHDRAdd)
                                    clientSrvcChar.action = "FORCE_INS";
                                else if (thomasParams.IsHDRDelete)
                                    clientSrvcChar.action = "DELETE";
                            }
                            if (thomasParams.IsHDRDelete && !Convert.ToBoolean(ConfigurationManager.AppSettings["isDnPMigrationforHDRCompleted"]))
                            {
                                // don't delete HDR char from serviceinstance char.
                            }
                            else
                                listClientSrvcInstnceChar.Add(clientSrvcChar);
                        }
                    }
                    else if (thomasParams.ServiceInstanceStatus.Equals("CEASED", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(thomasParams.HDR))
                        {
                            clientSrvcChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcChar.value = thomasParams.HDR;
                            clientSrvcChar.name = "HDR";
                            if (thomasParams.OrderAction.Equals("Create", StringComparison.OrdinalIgnoreCase))
                                clientSrvcChar.action = "FORCE_INS";
                            else
                            {
                                if (thomasParams.IsHDRAdd)
                                    clientSrvcChar.action = "FORCE_INS";
                                else if (thomasParams.IsHDRDelete)
                                    clientSrvcChar.action = "DELETE";
                            }
                            if (thomasParams.IsHDRDelete && !Convert.ToBoolean(ConfigurationManager.AppSettings["isDnPMigrationforHDRCompleted"]))
                            {
                                // don't delete HDR from serviceinstace char.
                            }
                            else
                                listClientSrvcInstnceChar.Add(clientSrvcChar);
                        }
                    }
                    if (thomasParams.ServiceInstanceStatus.ToLower().Equals("modify"))
                    {
                        //updating or deleting BTS_APP_CODE only if the attribute is present in the input request.
                        if (thomasParams.IsBTS_APP_CODEAttrExist)
                        {
                            if (string.IsNullOrEmpty(thomasParams.BTS_APP_CODE))
                            {
                                if (thomasParams.IsBTS_APP_CODEExistinDnP)
                                {
                                    clientSrvcChar = new ClientServiceInstanceCharacteristic();
                                    clientSrvcChar.name = "BTS_APP_CODE";
                                    clientSrvcChar.action = "DELETE";
                                    listClientSrvcInstnceChar.Add(clientSrvcChar);
                                }
                            }
                            else
                            {
                                clientSrvcChar = new ClientServiceInstanceCharacteristic();
                                clientSrvcChar.value = thomasParams.BTS_APP_CODE;
                                clientSrvcChar.name = "BTS_APP_CODE";
                                if (thomasParams.IsBTS_APP_CODEExistinDnP)
                                {
                                    clientSrvcChar.action = "UPDATE";
                                }
                                else clientSrvcChar.action = "INSERT";
                                listClientSrvcInstnceChar.Add(clientSrvcChar);
                            }
                        }
                    }

                    clientSrvcChar = new ClientServiceInstanceCharacteristic();
                    clientSrvcChar.value = thomasParams.Scode;
                    clientSrvcChar.name = "SCODE";
                    if (thomasParams.IsScodeExist)
                        clientSrvcChar.action = "UPDATE";
                    else
                        clientSrvcChar.action = "INSERT";
                    listClientSrvcInstnceChar.Add(clientSrvcChar);

                    manageServiceInstanceV1Req1.clientServiceInstanceV1.clientServiceInstanceCharacteristic = listClientSrvcInstnceChar.ToArray();

                    manageServiceInstanceV1Req1.clientServiceInstanceV1.serviceIdentity = serviceIdentity;
                }
                else
                {
                    manageServiceInstanceV1Req1 = new ManageServiceInstanceV1Req();
                    manageServiceInstanceV1Req1.clientServiceInstanceV1 = new ClientServiceInstanceV1();

                    ClientServiceInstanceV1[] clientServiceInstanceV1 = null;
                    clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                    clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                    if (thomasParams.ServiceInstanceExists)
                    {
                        ServiceIdentity[] serviceIdentity = new ServiceIdentity[3];

                        serviceIdentity[0] = new ServiceIdentity();
                        serviceIdentity[0].domain = thomasParams.IdentityDomain;
                        serviceIdentity[0].value = thomasParams.Identity;
                        serviceIdentity[0].action = "SEARCH";

                        serviceIdentity[1] = new ServiceIdentity();
                        serviceIdentity[1].domain = BACID_IDENTIFER_NAMEPACE;
                        serviceIdentity[1].value = thomasParams.BillAccountNumber;
                        serviceIdentity[1].action = "LINK";

                        serviceIdentity[2] = new ServiceIdentity();
                        serviceIdentity[2].domain = thomasParams.IdentityDomain;
                        serviceIdentity[2].value = thomasParams.Identity;
                        serviceIdentity[2].action = "UNLINK";

                        clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                        clientServiceInstanceV1[0].action = "UPDATE";
                        clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                        clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BTSKY_SERVICECODE_NAMEPACE;

                        clientSrvcChar = new ClientServiceInstanceCharacteristic();
                        clientSrvcChar.value = thomasParams.ServiceID;
                        clientSrvcChar.name = "BTAPP_SERVICE_ID";
                        clientSrvcChar.action = "UPDATE";
                        listClientSrvcInstnceChar.Add(clientSrvcChar);

                        clientSrvcChar = new ClientServiceInstanceCharacteristic();
                        clientSrvcChar.value = thomasParams.Scode;
                        clientSrvcChar.name = "SCODE";
                        if (thomasParams.IsScodeExist)
                            clientSrvcChar.action = "UPDATE";
                        else
                            clientSrvcChar.action = "INSERT";
                        listClientSrvcInstnceChar.Add(clientSrvcChar);

                        if (!string.IsNullOrEmpty(thomasParams.BTS_APP_CODE))
                        {
                            clientSrvcChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcChar.value = thomasParams.BTS_APP_CODE;
                            clientSrvcChar.name = "BTS_APP_CODE";
                            if (thomasParams.IsBTS_APP_CODEExistinDnP)
                                clientSrvcChar.action = "UPDATE";
                            else
                                clientSrvcChar.action = "INSERT";
                            listClientSrvcInstnceChar.Add(clientSrvcChar);
                        }

                        if (!string.IsNullOrEmpty(thomasParams.HDR))
                        {
                            clientSrvcChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcChar.value = thomasParams.HDR;
                            clientSrvcChar.name = "HDR";
                            if (thomasParams.OrderAction.Equals("Create", StringComparison.OrdinalIgnoreCase))
                                clientSrvcChar.action = "FORCE_INS";
                            else
                            {
                                if (thomasParams.IsHDRAdd)
                                    clientSrvcChar.action = "FORCE_INS";
                                else if (thomasParams.IsHDRDelete)
                                    clientSrvcChar.action = "DELETE";
                            }
                            if (thomasParams.IsHDRDelete && !Convert.ToBoolean(ConfigurationManager.AppSettings["isDnPMigrationforHDRCompleted"]))
                            {
                                //donot delete the HDR from serviceinstance chars.
                            }
                            else
                                listClientSrvcInstnceChar.Add(clientSrvcChar);
                        }
                        if (!string.IsNullOrEmpty(thomasParams.Super))
                        {
                            clientSrvcChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcChar.value = thomasParams.Super;
                            clientSrvcChar.name = "Super";
                            clientSrvcChar.action = "FORCE_INS";
                            listClientSrvcInstnceChar.Add(clientSrvcChar);
                        }

                        clientServiceInstanceV1[0].clientServiceInstanceCharacteristic = listClientSrvcInstnceChar.ToArray();

                        clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                    }
                    else
                    {
                        clientServiceInstanceV1[0].name = Guid.NewGuid().ToString();
                        clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                        clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BTSKY_SERVICECODE_NAMEPACE;

                        clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                        clientServiceInstanceV1[0].clientServiceInstanceStatus.value = "ACTIVE";

                        clientSrvcChar = new ClientServiceInstanceCharacteristic();
                        clientSrvcChar.value = thomasParams.ServiceID;
                        clientSrvcChar.name = "BTAPP_SERVICE_ID";
                        clientSrvcChar.action = "INSERT";
                        listClientSrvcInstnceChar.Add(clientSrvcChar);

                        clientSrvcChar = new ClientServiceInstanceCharacteristic();
                        clientSrvcChar.value = thomasParams.Scode;
                        clientSrvcChar.name = "SCODE";
                        clientSrvcChar.action = "INSERT";
                        listClientSrvcInstnceChar.Add(clientSrvcChar);

                        if (!string.IsNullOrEmpty(thomasParams.BTS_APP_CODE))
                        {
                            clientSrvcChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcChar.value = thomasParams.BTS_APP_CODE;
                            clientSrvcChar.name = "BTS_APP_CODE";
                            clientSrvcChar.action = "INSERT";
                            listClientSrvcInstnceChar.Add(clientSrvcChar);
                        }

                        if (!string.IsNullOrEmpty(thomasParams.HDR))
                        {
                            clientSrvcChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcChar.value = thomasParams.HDR;
                            clientSrvcChar.name = "HDR";
                            if (thomasParams.OrderAction.Equals("Create", StringComparison.OrdinalIgnoreCase))
                                clientSrvcChar.action = "FORCE_INS";
                            else
                            {
                                if (thomasParams.IsHDRAdd)
                                    clientSrvcChar.action = "FORCE_INS";
                                else if (thomasParams.IsHDRDelete)
                                    clientSrvcChar.action = "DELETE";
                            }
                            if (thomasParams.IsHDRDelete && !Convert.ToBoolean(ConfigurationManager.AppSettings["isDnPMigrationforHDRCompleted"]))
                            {
                                //donot delete the HDR from serviceinstance chars.
                            }
                            else
                                listClientSrvcInstnceChar.Add(clientSrvcChar);
                        }
                        else if (thomasParams.IsOffnetflow)
                        {
                            clientSrvcChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcChar.value = HDRScode;
                            clientSrvcChar.name = "HDR";
                            clientSrvcChar.action = "FORCE_INS";
                            listClientSrvcInstnceChar.Add(clientSrvcChar);
                        }
                        if (!string.IsNullOrEmpty(thomasParams.Super))
                        {
                            clientSrvcChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcChar.value = thomasParams.Super;
                            clientSrvcChar.name = "Super";
                            clientSrvcChar.action = "FORCE_INS";
                            listClientSrvcInstnceChar.Add(clientSrvcChar);
                        }

                        clientServiceInstanceV1[0].clientServiceInstanceCharacteristic = listClientSrvcInstnceChar.ToArray();
                        clientServiceInstanceV1[0].action = "INSERT";

                        ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                        serviceIdentity[0] = new ServiceIdentity();
                        serviceIdentity[0].value = thomasParams.Identity;
                        serviceIdentity[0].domain = thomasParams.IdentityDomain;
                        serviceIdentity[0].action = "LINK";

                        clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                    }

                    manageServiceInstanceV1Req1.clientServiceInstanceV1 = clientServiceInstanceV1[0];
                }

                request.manageServiceInstanceV1Request.manageServiceInstanceV1Req = manageServiceInstanceV1Req1;

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, BTSKY_SERVICECODE_NAMEPACE);

                request.manageServiceInstanceV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                request.manageServiceInstanceV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                Logger.Write(thomasParams.orderKey + "," + MakingDnPcall + "," + SendingRequesttoDnP, Logger.TypeEnum.MessageTrace);
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, thomasParams.orderKey, request.SerializeObject(), ConfigurationManager.AppSettings["BTSportsProductCode"].ToString());
                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey, request.SerializeObject());

                profileResponse = DnpWrapper.manageServiceInstanceV1Thomas(request, thomasParams.orderKey);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey, profileResponse.SerializeObject());
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, thomasParams.orderKey, profileResponse.SerializeObject(), ConfigurationManager.AppSettings["BTSportsProductCode"].ToString());
                Logger.Write(thomasParams.orderKey + "," + GotResponseFromDnP + "," + GotResponsefromDnPNotification, Logger.TypeEnum.MessageTrace);

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

                    if (thomasParams.OrderAction.ToLower().Equals("create") && !string.IsNullOrEmpty(thomasParams.BillAccountNumber) && ConfigurationManager.AppSettings["BTsportsdelegation"].Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        ClientServiceInstanceV1[] gsiResponse = null;
                        List<string> SportsDelegateDNslist = new List<string>();
                        gsiResponse = DnpWrapper.getServiceInstanceV1(thomasParams.BillAccountNumber, BACID_IDENTIFER_NAMEPACE, string.Empty);
                        if (gsiResponse != null)
                        {
                            if (IsDelegateRoleExists(gsiResponse, ref SportsDelegateDNslist))
                            {
                                CreateSportsDelegateRoles(thomasParams, notification, ref e2eData, SportsDelegateDNslist);//rolerequest response = GetRoleIdentity(thomasParams.BillAccountNumber);//***** here will call the getroleidentity funcation using bacid**********
                            }
                            else
                            {
                                notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                                Logger.Write(thomasParams.orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.MessageTrace);
                                LogHelper.LogActivityDetails(bptmTxnId, guid, CompletedNotificationSent, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                            }
                        }

                    }
                    else
                    {
                        notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                        Logger.Write(thomasParams.orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.MessageTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, CompletedNotificationSent, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                    }
                }
                else
                {
                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description;
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedErrorMessage + errorMessage, Logger.TypeEnum.ExceptionTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + FailedErrorMessage + errorMessage, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + DnPAdminstratorFailedResponse + errorMessage, Logger.TypeEnum.MessageTrace);
                            //Logger.Write(orderKey + " : recieved failure response from DnP (Administrator) " + errorMessage, Logger.TypeEnum.ExceptionTrace);
                            //retry
                            if (retryCount++ < System.Int32.Parse(ConfigurationManager.AppSettings["RetryCountForThomasProvide"]))
                            {
                                if (retryCount == System.Int32.Parse(ConfigurationManager.AppSettings["RetryCountForThomasProvide"]))
                                {
                                    Logger.Write(thomasParams.orderKey + "," + Failed + "," + NonFunctionalException + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.ExceptionTrace);
                                    //Logger.Write("Non Functional Exception from DNP, reached max number of Retry , we are shutdowning service" + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.EmailTrace);

                                    //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA;
                                    e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);

                                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                                        e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                                    else
                                        e2eData.endOutboundCall(e2eData.toString());
                                    //sending failure notification
                                    notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                                    Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                                }
                                else
                                {
                                    Thread.Sleep(System.Int32.Parse(ConfigurationManager.AppSettings["SleepTimeoutInMilliSec"]));
                                    CreateThomasServiceInstance(thomasParams, notification, retryCount, ref e2eData);
                                }
                            }
                        }
                        else
                        {
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailureResponseFromDnP + errorMessage, Logger.TypeEnum.MessageTrace);

                            e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                            if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                                e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                            else
                                e2eData.endOutboundCall(e2eData.toString());
                            //sending failure notification
                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                        }
                    }
                    else
                    {
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.MessageTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + NullResponseFromDnP, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //sending failure notification
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailureResponseFromDnP + ex.Message, Logger.TypeEnum.MessageTrace);
                LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + FailedErrorMessage + ex.Message, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                //sending failure notification
                //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = e2eData;
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedErrorMessage + ex.Message, Logger.TypeEnum.ExceptionTrace);
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
            }
            finally
            {
                listClientSrvcInstnceChar = null;
            }
        }

        public void ModifyProfileAhtDone(ThomasParameters thomasParams, MSEOOrderNotification notification, short retryCount, ref E2ETransaction e2eData)
        {
            manageClientProfileV1Request1 request1 = new manageClientProfileV1Request1();
            request1.manageClientProfileV1Request = new ManageClientProfileV1Request();
            manageClientProfileV1Response1 profileResponse1 = new manageClientProfileV1Response1();

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

            ManageClientProfileV1Req manageClientProfileV1Req1 = new ManageClientProfileV1Req();
            List<ClientServiceInstanceCharacteristic> clientSrvcInsCharsList = new List<ClientServiceInstanceCharacteristic>();
            ClientServiceInstanceCharacteristic clientSrvcInsChar = null;
            try
            {
                if (!string.IsNullOrEmpty(thomasParams.ServiceInstanceStatus) && (thomasParams.ServiceInstanceStatus.ToLower() == "ceased" || thomasParams.ServiceInstanceStatus.ToLower() == "active" || thomasParams.ServiceInstanceStatus.ToLower() == "modify"))
                {
                    manageClientProfileV1Req1 = new ManageClientProfileV1Req();

                    manageClientProfileV1Req1.clientProfileV1 = new ClientProfileV1();
                    manageClientProfileV1Req1.clientProfileV1.client = new Client();
                    manageClientProfileV1Req1.clientProfileV1.client.action = "UPDATE";

                    ClientIdentity[] clientIdentity = new ClientIdentity[1];
                    clientIdentity[0] = new ClientIdentity();
                    clientIdentity[0].value = thomasParams.BillAccountNumber;
                    clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                    clientIdentity[0].action = "SEARCH";

                    ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];

                    serviceIdentity[0] = new ServiceIdentity();
                    serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                    serviceIdentity[0].value = thomasParams.BillAccountNumber;
                    serviceIdentity[0].action = "SEARCH";

                    ClientServiceInstanceV1[] clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                    clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                    clientServiceInstanceV1[0].action = "UPDATE";
                    clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                    clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BTSKY_SERVICECODE_NAMEPACE;

                    clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                    clientServiceInstanceV1[0].clientServiceInstanceStatus.value = "ACTIVE";

                    //update only Scode for regrade
                    if (!thomasParams.ServiceInstanceStatus.ToLower().Equals("modify"))
                    {
                        clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                        clientSrvcInsChar.value = thomasParams.ServiceID;
                        clientSrvcInsChar.name = "BTAPP_SERVICE_ID";
                        clientSrvcInsChar.action = "UPDATE";
                        clientSrvcInsCharsList.Add(clientSrvcInsChar);

                        if (!string.IsNullOrEmpty(thomasParams.BTS_APP_CODE))
                        {
                            clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcInsChar.value = thomasParams.BTS_APP_CODE;
                            clientSrvcInsChar.name = "BTS_APP_CODE";
                            if (thomasParams.IsBTS_APP_CODEExistinDnP)
                                clientSrvcInsChar.action = "UPDATE";
                            else
                                clientSrvcInsChar.action = "INSERT";
                            clientSrvcInsCharsList.Add(clientSrvcInsChar);
                        }
                    }

                    if (thomasParams.ServiceInstanceStatus.ToLower().Equals("modify"))
                    {
                        //updating or deleting BTS_APP_CODE only if the attribute is present in the input request.
                        if (thomasParams.IsBTS_APP_CODEAttrExist)
                        {
                            if (string.IsNullOrEmpty(thomasParams.BTS_APP_CODE))
                            {
                                if (thomasParams.IsBTS_APP_CODEExistinDnP)
                                {
                                    clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                                    clientSrvcInsChar.name = "BTS_APP_CODE";
                                    clientSrvcInsChar.action = "DELETE";
                                    clientSrvcInsCharsList.Add(clientSrvcInsChar);
                                }
                            }
                            else
                            {
                                clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                                clientSrvcInsChar.value = thomasParams.BTS_APP_CODE;
                                clientSrvcInsChar.name = "BTS_APP_CODE";
                                if (thomasParams.IsBTS_APP_CODEExistinDnP)
                                {
                                    clientSrvcInsChar.action = "UPDATE";
                                }
                                else clientSrvcInsChar.action = "INSERT";
                                clientSrvcInsCharsList.Add(clientSrvcInsChar);
                            }
                        }
                    }

                    //Insert or delete HDR during regrade
                    if (thomasParams.ServiceInstanceStatus.ToLower().Equals("modify"))
                    {
                        if (!string.IsNullOrEmpty(thomasParams.HDR))
                        {
                            ClientServiceInstanceCharacteristic clientSrvcChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcChar.value = thomasParams.HDR;
                            clientSrvcChar.name = "HDR";
                            if (thomasParams.OrderAction.Equals("Create", StringComparison.OrdinalIgnoreCase))
                                clientSrvcChar.action = "FORCE_INS";
                            else
                            {
                                if (thomasParams.IsHDRAdd)
                                    clientSrvcChar.action = "FORCE_INS";
                                else if (thomasParams.IsHDRDelete)
                                    clientSrvcChar.action = "DELETE";
                            }
                            if (thomasParams.IsHDRDelete && !Convert.ToBoolean(ConfigurationManager.AppSettings["isDnPMigrationforHDRCompleted"]))
                            {
                                //donot delete the HDR from serviceinstance chars.
                            }
                            else
                                clientSrvcInsCharsList.Add(clientSrvcChar);
                        }
                    }
                    else if (thomasParams.ServiceInstanceStatus.Equals("CEASED", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(thomasParams.HDR))
                        {
                            ClientServiceInstanceCharacteristic clientSrvcChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcChar.value = thomasParams.HDR;
                            clientSrvcChar.name = "HDR";
                            if (thomasParams.OrderAction.Equals("Create", StringComparison.OrdinalIgnoreCase))
                                clientSrvcChar.action = "FORCE_INS";
                            else
                            {
                                if (thomasParams.IsHDRAdd)
                                    clientSrvcChar.action = "FORCE_INS";
                                else if (thomasParams.IsHDRDelete)
                                    clientSrvcChar.action = "DELETE";
                            }
                            if (thomasParams.IsHDRDelete && !Convert.ToBoolean(ConfigurationManager.AppSettings["isDnPMigrationforHDRCompleted"]))
                            {
                                //donot delete the HDR from serviceinstance chars.
                            }
                            else
                                clientSrvcInsCharsList.Add(clientSrvcChar);
                        }
                    }

                    clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                    clientSrvcInsChar.value = thomasParams.Scode;
                    clientSrvcInsChar.name = "SCODE";
                    if (thomasParams.IsScodeExist)
                        clientSrvcInsChar.action = "UPDATE";
                    else
                        clientSrvcInsChar.action = "INSERT";
                    clientSrvcInsCharsList.Add(clientSrvcInsChar);

                    clientServiceInstanceV1[0].clientServiceInstanceCharacteristic = clientSrvcInsCharsList.ToArray();

                    ClientServiceRole[] clientServiceRole = new ClientServiceRole[2];
                    clientServiceRole[0] = new ClientServiceRole();
                    clientServiceRole[0].id = "ADMIN";
                    clientServiceRole[0].clientServiceRoleStatus = new ClientServiceRoleStatus();
                    clientServiceRole[0].clientServiceRoleStatus.value = "ACTIVE";

                    clientServiceRole[0].clientIdentity = new ClientIdentity[1];
                    clientServiceRole[0].clientIdentity[0] = new ClientIdentity();
                    clientServiceRole[0].clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientServiceRole[0].clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                    clientServiceRole[0].clientIdentity[0].value = thomasParams.BillAccountNumber;
                    clientServiceRole[0].clientIdentity[0].action = "SEARCH";
                    clientServiceRole[0].action = "UPDATE";

                    clientServiceRole[1] = new ClientServiceRole();
                    clientServiceRole[1].id = "DEFAULT";
                    clientServiceRole[1].clientServiceRoleStatus = new ClientServiceRoleStatus();
                    clientServiceRole[1].clientServiceRoleStatus.value = "ACTIVE";
                    if (!string.IsNullOrEmpty(thomasParams.BtOneId))
                    {
                        clientServiceRole[1].clientIdentity = new ClientIdentity[1];
                        clientServiceRole[1].clientIdentity[0] = new ClientIdentity();
                        clientServiceRole[1].clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientServiceRole[1].clientIdentity[0].managedIdentifierDomain.value = BTCOM;
                        clientServiceRole[1].clientIdentity[0].value = thomasParams.BtOneId;
                        clientServiceRole[1].clientIdentity[0].action = "SEARCH";
                    }
                    clientServiceRole[1].action = "UPDATE";

                    manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;
                    manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1[0].clientServiceRole = clientServiceRole;
                    manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentity;
                    manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                }
                //Thomas Provision on BAC or Slow track
                else
                {
                    manageClientProfileV1Req1.clientProfileV1 = new ClientProfileV1();
                    manageClientProfileV1Req1.clientProfileV1.client = new Client();
                    manageClientProfileV1Req1.clientProfileV1.client.action = "UPDATE";

                    List<ClientIdentity> ListClientIdentity = new List<ClientIdentity>();
                    ClientIdentity clientIdentity = null;

                    clientIdentity = new ClientIdentity();
                    clientIdentity.value = thomasParams.BillAccountNumber;
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                    clientIdentity.action = "SEARCH";
                    ListClientIdentity.Add(clientIdentity);

                    if (thomasParams.IsAdminRoleexistsonCAK)
                    {
                        clientIdentity = new ClientIdentity();
                        clientIdentity.value = thomasParams.CustomerID;
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientIdentity.managedIdentifierDomain.value = CAKID_IDENTIFER_NAMEPACE;
                        clientIdentity.action = "DELETE";
                        ListClientIdentity.Add(clientIdentity);
                    }

                    ClientServiceInstanceV1[] clientServiceInstanceV1 = null;

                    List<ClientServiceRole> ListclientServicerole = new List<ClientServiceRole>();
                    ClientServiceRole clientServiceRole = null;

                    //as part of 136847 created admin role for CAKID need to delete and insert bac admin.
                    if (thomasParams.IsAdminRoleexistsonCAK)
                    {
                        clientServiceRole = new ClientServiceRole();
                        clientServiceRole.id = "ADMIN";
                        clientServiceRole.clientIdentity = new ClientIdentity[1];
                        clientServiceRole.clientIdentity[0] = new ClientIdentity();
                        clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = CAKID_IDENTIFER_NAMEPACE;
                        clientServiceRole.clientIdentity[0].value = thomasParams.CustomerID;
                        clientServiceRole.clientIdentity[0].action = "SEARCH";
                        clientServiceRole.action = "DELETE";
                        ListclientServicerole.Add(clientServiceRole);
                    }
                    else
                    {
                        clientServiceRole = new ClientServiceRole();
                        clientServiceRole.id = "DEFAULT";
                        clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                        clientServiceRole.clientServiceRoleStatus.value = "ACTIVE";
                        if (!string.IsNullOrEmpty(thomasParams.BtOneId))
                        {
                            clientServiceRole.clientIdentity = new ClientIdentity[1];
                            clientServiceRole.clientIdentity[0] = new ClientIdentity();
                            clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                            clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = BTCOM;
                            clientServiceRole.clientIdentity[0].value = thomasParams.BtOneId;
                            clientServiceRole.clientIdentity[0].action = "INSERT";
                        }
                        clientServiceRole.action = "INSERT";
                        ListclientServicerole.Add(clientServiceRole);
                    }
                    clientServiceRole = new ClientServiceRole();
                    clientServiceRole.id = "ADMIN";
                    clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                    clientServiceRole.clientServiceRoleStatus.value = "ACTIVE";

                    clientServiceRole.clientIdentity = new ClientIdentity[1];
                    clientServiceRole.clientIdentity[0] = new ClientIdentity();
                    clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                    clientServiceRole.clientIdentity[0].value = thomasParams.BillAccountNumber;
                    clientServiceRole.clientIdentity[0].action = "INSERT";
                    clientServiceRole.action = "INSERT";
                    ListclientServicerole.Add(clientServiceRole);

                    //Slow Track followed by Fast Track order
                    if (thomasParams.ServiceInstanceExists)
                    {
                        clientServiceInstanceV1 = new ClientServiceInstanceV1[2];

                        clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                        clientServiceInstanceV1[0].action = "LINK";
                        clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                        clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BTSKY_SERVICECODE_NAMEPACE;

                        clientServiceInstanceV1[1] = new ClientServiceInstanceV1();
                        clientServiceInstanceV1[1].action = "UPDATE";
                        clientServiceInstanceV1[1].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                        clientServiceInstanceV1[1].clientServiceInstanceIdentifier.value = BTSKY_SERVICECODE_NAMEPACE;

                        ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];

                        serviceIdentity[0] = new ServiceIdentity();
                        serviceIdentity[0].domain = CAKID_IDENTIFER_NAMEPACE;
                        serviceIdentity[0].value = thomasParams.CustomerID;
                        serviceIdentity[0].action = "SEARCH";
                        clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;

                        ServiceIdentity[] serviceIdentity1 = new ServiceIdentity[3];

                        serviceIdentity1[0] = new ServiceIdentity();
                        serviceIdentity1[0].domain = CAKID_IDENTIFER_NAMEPACE;
                        serviceIdentity1[0].value = thomasParams.CustomerID;
                        serviceIdentity1[0].action = "SEARCH";

                        serviceIdentity1[1] = new ServiceIdentity();
                        serviceIdentity1[1].domain = BACID_IDENTIFER_NAMEPACE;
                        serviceIdentity1[1].value = thomasParams.BillAccountNumber;
                        serviceIdentity1[1].action = "LINK";

                        serviceIdentity1[2] = new ServiceIdentity();
                        serviceIdentity1[2].domain = CAKID_IDENTIFER_NAMEPACE;
                        serviceIdentity1[2].value = thomasParams.CustomerID;
                        serviceIdentity1[2].action = "UNLINK";

                        //ClientServiceInstanceCharacteristic[] clientServiceInstanceChar = new ClientServiceInstanceCharacteristic[2];
                        //clientServiceInstanceChar[0] = new ClientServiceInstanceCharacteristic();
                        //clientServiceInstanceChar[0].value = btssid;
                        //clientServiceInstanceChar[0].name = "BTAPP_SERVICE_ID";
                        //clientServiceInstanceChar[0].action = "UPDATE";

                        //clientServiceInstanceChar[1] = new ClientServiceInstanceCharacteristic();
                        //clientServiceInstanceChar[1].value = scode;
                        //clientServiceInstanceChar[1].name = "SCODE";
                        //if(isScodeExist)
                        //clientServiceInstanceChar[1].action = "UPDATE";
                        //else
                        //    clientServiceInstanceChar[1].action = "INSERT";

                        clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                        clientSrvcInsChar.value = thomasParams.ServiceID;
                        clientSrvcInsChar.name = "BTAPP_SERVICE_ID";
                        clientSrvcInsChar.action = "UPDATE";
                        clientSrvcInsCharsList.Add(clientSrvcInsChar);

                        clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                        clientSrvcInsChar.value = thomasParams.Scode;
                        clientSrvcInsChar.name = "SCODE";
                        if (thomasParams.IsScodeExist)
                            clientSrvcInsChar.action = "UPDATE";
                        else
                            clientSrvcInsChar.action = "INSERT";
                        clientSrvcInsCharsList.Add(clientSrvcInsChar);

                        if (!string.IsNullOrEmpty(thomasParams.BTS_APP_CODE))
                        {
                            clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcInsChar.value = thomasParams.BTS_APP_CODE;
                            clientSrvcInsChar.name = "BTS_APP_CODE";
                            if (thomasParams.IsBTS_APP_CODEExistinDnP)
                                clientSrvcInsChar.action = "UPDATE";
                            else
                                clientSrvcInsChar.action = "INSERT";
                            clientSrvcInsCharsList.Add(clientSrvcInsChar);
                        }

                        if (!string.IsNullOrEmpty(thomasParams.Super))
                        {
                            clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcInsChar.value = thomasParams.Super;
                            clientSrvcInsChar.name = "Super";
                            clientSrvcInsChar.action = "FORCE_INS";
                            clientSrvcInsCharsList.Add(clientSrvcInsChar);
                        }

                        if (!string.IsNullOrEmpty(thomasParams.HDR))
                        {
                            ClientServiceInstanceCharacteristic clientSrvcChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcChar.value = thomasParams.HDR;
                            clientSrvcChar.name = "HDR";
                            if (thomasParams.OrderAction.Equals("Create", StringComparison.OrdinalIgnoreCase))
                                clientSrvcChar.action = "FORCE_INS";
                            else
                            {
                                if (thomasParams.IsHDRAdd)
                                    clientSrvcChar.action = "FORCE_INS";
                                else if (thomasParams.IsHDRDelete)
                                    clientSrvcChar.action = "DELETE";
                            }
                            if (thomasParams.IsHDRDelete && !Convert.ToBoolean(ConfigurationManager.AppSettings["isDnPMigrationforHDRCompleted"]))
                            {
                                //donot delete the HDR from serviceinstance chars.
                            }
                            else
                                clientSrvcInsCharsList.Add(clientSrvcChar);
                        }

                        clientServiceInstanceV1[1].clientServiceInstanceCharacteristic = clientSrvcInsCharsList.ToArray();
                        clientServiceInstanceV1[1].serviceIdentity = serviceIdentity1;
                    }

                    //Thomas Provision on BAC
                    else
                    {
                        clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                        clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                        clientServiceInstanceV1[0].name = Guid.NewGuid().ToString();

                        clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                        clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BTSKY_SERVICECODE_NAMEPACE;

                        clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                        clientServiceInstanceV1[0].clientServiceInstanceStatus.value = "ACTIVE";
                        //clientServiceInstanceV1[0].clientServiceInstanceCharacteristic = new ClientServiceInstanceCharacteristic[2];
                        //clientServiceInstanceV1[0].clientServiceInstanceCharacteristic[0] = new ClientServiceInstanceCharacteristic();
                        //clientServiceInstanceV1[0].clientServiceInstanceCharacteristic[0].value = btssid;
                        //clientServiceInstanceV1[0].clientServiceInstanceCharacteristic[0].name = "BTAPP_SERVICE_ID";
                        //clientServiceInstanceV1[0].clientServiceInstanceCharacteristic[0].action = "INSERT";
                        //clientServiceInstanceV1[0].clientServiceInstanceCharacteristic[1] = new ClientServiceInstanceCharacteristic();
                        //clientServiceInstanceV1[0].clientServiceInstanceCharacteristic[1].value = scode;
                        //clientServiceInstanceV1[0].clientServiceInstanceCharacteristic[1].name = "SCODE";
                        //clientServiceInstanceV1[0].clientServiceInstanceCharacteristic[1].action = "INSERT";
                        //clientServiceInstanceV1[0].action = "INSERT";

                        clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                        clientSrvcInsChar.value = thomasParams.ServiceID;
                        clientSrvcInsChar.name = "BTAPP_SERVICE_ID";
                        clientSrvcInsChar.action = "INSERT";
                        clientSrvcInsCharsList.Add(clientSrvcInsChar);

                        clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                        clientSrvcInsChar.value = thomasParams.Scode;
                        clientSrvcInsChar.name = "SCODE";
                        clientSrvcInsChar.action = "INSERT";
                        clientSrvcInsCharsList.Add(clientSrvcInsChar);

                        if (!string.IsNullOrEmpty(thomasParams.BTS_APP_CODE))
                        {
                            clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcInsChar.value = thomasParams.BTS_APP_CODE;
                            clientSrvcInsChar.name = "BTS_APP_CODE";
                            clientSrvcInsChar.action = "INSERT";
                            clientSrvcInsCharsList.Add(clientSrvcInsChar);
                        }
                        if (!string.IsNullOrEmpty(thomasParams.Super))
                        {
                            clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcInsChar.value = thomasParams.Super;
                            clientSrvcInsChar.name = "Super";
                            clientSrvcInsChar.action = "FORCE_INS";
                            clientSrvcInsCharsList.Add(clientSrvcInsChar);
                        }
                        if (!string.IsNullOrEmpty(thomasParams.HDR))
                        {
                            clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcInsChar.value = thomasParams.HDR;
                            clientSrvcInsChar.name = "HDR";
                            if (thomasParams.OrderAction.Equals("Create", StringComparison.OrdinalIgnoreCase))
                                clientSrvcInsChar.action = "FORCE_INS";
                            else
                            {
                                if (thomasParams.IsHDRAdd)
                                    clientSrvcInsChar.action = "FORCE_INS";
                                else if (thomasParams.IsHDRDelete)
                                    clientSrvcInsChar.action = "DELETE";
                            }
                            if (thomasParams.IsHDRDelete && !Convert.ToBoolean(ConfigurationManager.AppSettings["isDnPMigrationforHDRCompleted"]))
                            {
                                //donot delete the HDR from serviceinstance chars.
                            }
                            else
                                clientSrvcInsCharsList.Add(clientSrvcInsChar);
                        }
                        else if (thomasParams.IsOffnetflow)
                        {
                            clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                            clientSrvcInsChar.value = HDRScode;
                            clientSrvcInsChar.name = "HDR";
                            clientSrvcInsChar.action = "FORCE_INS";
                            clientSrvcInsCharsList.Add(clientSrvcInsChar);
                        }
                        clientServiceInstanceV1[0].clientServiceInstanceCharacteristic = clientSrvcInsCharsList.ToArray();
                        clientServiceInstanceV1[0].action = "INSERT";

                        ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                        serviceIdentity[0] = new ServiceIdentity();
                        serviceIdentity[0].value = thomasParams.BillAccountNumber;
                        serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                        serviceIdentity[0].action = "LINK";

                        clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;

                    }

                    manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;
                    manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1[0].clientServiceRole = ListclientServicerole.ToArray();
                    manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = ListClientIdentity.ToArray();
                }

                request1.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;
                request1.manageClientProfileV1Request.standardHeader = headerBlock;

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, BTSKY_SERVICECODE_NAMEPACE);
                request1.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                request1.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                Logger.Write(thomasParams.orderKey + "," + MakingDnPcall + "," + ModifyProfileAHTDone, Logger.TypeEnum.MessageTrace);
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, thomasParams.orderKey, request1.SerializeObject(), ConfigurationManager.AppSettings["BTSportsProductCode"].ToString());
                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey, request1.SerializeObject());

                profileResponse1 = DnpWrapper.manageClientProfileV1Thomas(request1, thomasParams.orderKey);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey, profileResponse1.SerializeObject());
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, thomasParams.orderKey, profileResponse1.SerializeObject(), ConfigurationManager.AppSettings["BTSportsProductCode"].ToString());
                Logger.Write(thomasParams.orderKey + "," + GotResponseFromDnP + "," + GotResponsefromDnPModifyProfileAHTDone, Logger.TypeEnum.MessageTrace);

                if (profileResponse1 != null
                   && profileResponse1.manageClientProfileV1Response != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    //sending completed notification
                    //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA;
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    // need confirmation about flase or true
                    if (thomasParams.OrderAction.ToLower().Equals("create") && !string.IsNullOrEmpty(thomasParams.BillAccountNumber) && ConfigurationManager.AppSettings["BTsportsdelegation"].Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        ClientServiceInstanceV1[] gsiResponse = null;
                        List<string> SportsDelegateDNslist = new List<string>();

                        gsiResponse = DnpWrapper.getServiceInstanceV1(thomasParams.BillAccountNumber, BACID_IDENTIFER_NAMEPACE, string.Empty);
                        if (gsiResponse != null)
                        {
                            if (IsDelegateRoleExists(gsiResponse, ref SportsDelegateDNslist))
                            {
                                CreateSportsDelegateRoles(thomasParams, notification, ref e2eData, SportsDelegateDNslist);
                            }
                            else
                            {
                                notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                                Logger.Write(thomasParams.orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.MessageTrace);
                                LogHelper.LogActivityDetails(bptmTxnId, guid, CompletedNotificationSent, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                            }
                        }

                    }
                    else
                    {
                        notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                        Logger.Write(thomasParams.orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.MessageTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, CompletedNotificationSent, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                    }
                }
                else
                {
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedErrorMessage + errorMessage, Logger.TypeEnum.ExceptionTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + FailedErrorMessage + errorMessage, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + DnPAdminstratorFailedResponse + errorMessage, Logger.TypeEnum.MessageTrace);
                            if (retryCount++ < System.Int32.Parse(ConfigurationManager.AppSettings["RetryCountForThomasProvide"]))
                            {
                                if (retryCount == System.Int32.Parse(ConfigurationManager.AppSettings["RetryCountForThomasProvide"]))
                                {
                                    Logger.Write(thomasParams.orderKey + "," + Failed + "," + NonFunctionalException + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.ExceptionTrace);
                                    //Logger.Write("Non Functional Exception from DNP, reached max number of Retry , we are shutdowning service" + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.EmailTrace);
                                    //Process.GetCurrentProcess().Kill();

                                    //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA;
                                    e2eData.logMessage(GotResponseFromDnP, "");
                                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                                    else
                                        e2eData.endOutboundCall(e2eData.toString());

                                    notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                                    Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                                }
                                else
                                {
                                    Thread.Sleep(System.Int32.Parse(ConfigurationManager.AppSettings["SleepTimeoutInMilliSec"]));
                                    ModifyProfileAhtDone(thomasParams, notification, retryCount, ref e2eData);
                                }
                            }
                        }
                        else
                        {
                            //sending failure notification
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailureResponseFromDnP + errorMessage, Logger.TypeEnum.MessageTrace);
                            e2eData.logMessage(GotResponseFromDnP, "");
                            if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                                e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                            else
                                e2eData.endOutboundCall(e2eData.toString());
                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                        }
                    }
                    else
                    {
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.MessageTrace);

                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + FailedErrorMessage + NullResponseFromDnP, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);

                        //sending failure notification
                        e2eData.logMessage(GotResponseFromDnP, "");
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailureResponseFromDnP + ex.Message, Logger.TypeEnum.MessageTrace);

                LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + FailureResponseFromDnP + ex.Message, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);

                //sending failure notification
                e2eData.logMessage(GotResponseFromDnP, "");
                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());
                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
            }
            finally
            {
                clientSrvcInsCharsList = null;
            }
        }

        public void ProjectHestonMapper(OrderRequest requestOrderRequest, ref E2ETransaction e2eData, string orderKey, MSEOOrderNotification notification)
        {
            notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
            Logger.Write(orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.MessageTrace);
        }
        /// <summary>
        /// checking that profile contains any delegate roles 
        /// </summary>
        /// <param name="gsiResponse">gsiResponse of the profile using bacid</param>
        /// <returns>true if delegate roles exists</returns>
        public bool IsDelegateRoleExists(ClientServiceInstanceV1[] gsiResponse, ref List<string> SportsDelegateDNslist)
        {
            bool isDelegateRoleExists = false;

            List<string> SportsDelegatelist = new List<string>();
            List<string> SpringDelegateDNslist = new List<string>();

            try
            {
                if (gsiResponse != null)
                {
                    foreach (ClientServiceInstanceV1 serviceIntance in gsiResponse)
                    {
                        if (serviceIntance.clientServiceInstanceIdentifier.value.Equals(BTSKY_SERVICECODE_NAMEPACE))
                        {
                            if (serviceIntance.clientServiceRole != null && serviceIntance.clientServiceRole.Count() > 0)
                            {
                                if (serviceIntance.clientServiceRole.ToList().Exists(csr => csr.id.Equals("SERVICE_USER", StringComparison.OrdinalIgnoreCase) || csr.id.Equals("SERVICE_MANAGER", StringComparison.OrdinalIgnoreCase)))
                                {
                                    foreach (ClientServiceRole role in serviceIntance.clientServiceRole.Where(csr => csr.id.Equals("SERVICE_USER", StringComparison.OrdinalIgnoreCase) || csr.id.Equals("SERVICE_MANAGER", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        if (role != null && role.clientIdentity != null)
                                        {
                                            if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("MOBILE_MSISDN", StringComparison.OrdinalIgnoreCase)) &&
                                                role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                string dn = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("MOBILE_MSISDN", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                string btcom = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                SportsDelegatelist.Add(dn + ";" + btcom);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (serviceIntance.clientServiceInstanceIdentifier.value == "SPRING_GSM")
                        {
                            if (serviceIntance.clientServiceRole != null && serviceIntance.clientServiceRole.Count() > 0)
                            {
                                if (serviceIntance.clientServiceRole.ToList().Exists(csr => csr.id.Equals("SERVICE_USER", StringComparison.OrdinalIgnoreCase) || csr.id.Equals("SERVICE_MANAGER", StringComparison.OrdinalIgnoreCase)))
                                {
                                    foreach (ClientServiceRole role in serviceIntance.clientServiceRole.Where(csr => csr.id.Equals("SERVICE_USER", StringComparison.OrdinalIgnoreCase) || csr.id.Equals("SERVICE_MANAGER", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        if (role != null && role.clientIdentity != null)
                                        {
                                            if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("MOBILE_MSISDN", StringComparison.OrdinalIgnoreCase)) &&
                                                role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                string dn = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("MOBILE_MSISDN", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                string btcom = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                SpringDelegateDNslist.Add(dn + ";" + btcom);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // comparing the spring and sports delegate DN attributes.
                    // it will return the dn's which are present in spring but not in sports.
                    IEnumerable<string> results = SpringDelegateDNslist.Except(SportsDelegatelist);
                    foreach (string dn in results)
                    {
                        SportsDelegateDNslist.Add(dn);
                    }
                    if (SportsDelegateDNslist != null && SportsDelegateDNslist.Count() != 0)
                    {
                        isDelegateRoleExists = true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return isDelegateRoleExists;
        }

        /// <summary>
        /// creating delegate role for BTSport
        /// </summary>
        /// <param name="thomasParams">thomasparameters object for bacid and orderkey</param>
        /// <param name="notification">notifiction object for notifications</param>
        /// <param name="e2eData">e2e data</param>
        public void CreateSportsDelegateRoles(ThomasParameters thomasParams, MSEOOrderNotification notification, ref E2ETransaction e2eData, List<string> SportsDelegateDNslist)
        {
            string getUrl = ConfigurationManager.AppSettings["GetRoleRequestbyBac"].ToString();
            string postUrl = ConfigurationManager.AppSettings["ManageRoleRequest"].ToString();
            string idEmailType = "EMAIL";
            string BTS_APP_CODE = "S0336492";
            string requestStatus = "ACCEPTED";
            string requestKey = string.Empty;
            string errorMsg = string.Empty;
            bool isDNExists = false;
            bool isSuccess = false;
            bool isServiceRoleExists = false;
            BT.ESB.RoBT.GetServiceRoleRequests roleResponse = new BT.ESB.RoBT.GetServiceRoleRequests();
            EsbResponse esbResponse = new EsbResponse();

            try
            {
                if (!string.IsNullOrEmpty(thomasParams.BillAccountNumber))
                {
                    getUrl = getUrl + "?BACNumber=" + thomasParams.BillAccountNumber + "&STACK=CCP";
                }
                roleResponse = ESBRestCallWrapper.GetRoleRequestfromESB(getUrl);

                if (roleResponse != null && roleResponse.ServiceRoleRequest != null && roleResponse.ServiceRoleRequest.Count() > 0)
                {
                    foreach (BT.ESB.RoBT.ServiceRoleRequest serviceRoleRequest in roleResponse.ServiceRoleRequest)
                    {
                        if (requestStatus.Equals(serviceRoleRequest.RequestStatus, StringComparison.OrdinalIgnoreCase))
                        {
                            errorMsg = string.Empty;

                            ServiceRoleRequest modifyServiceRole = new ServiceRoleRequest();
                            if (serviceRoleRequest.SourceMessageID != null)
                                modifyServiceRole.SourceMessageID = serviceRoleRequest.SourceMessageID;
                            modifyServiceRole.AuthKey = " ";//*** through get request we are not getting authkey, so passing null value*****
                            modifyServiceRole.AuthKeyType = "NONE";
                            modifyServiceRole.Request = ServiceRoleRequestRequest.DELEGATE;

                            //**** adding requestor values*******
                            if (serviceRoleRequest.Requestor != null)
                            {
                                Requestor requestor = new Requestor();
                                if (serviceRoleRequest.Requestor.RequestorFirstName != null)
                                    requestor.RequestorFirstName = serviceRoleRequest.Requestor.RequestorFirstName;
                                else
                                {
                                    errorMsg = "RequestorFirstName is missing";
                                }
                                if (serviceRoleRequest.Requestor.RequestorLastName != null)
                                    requestor.RequestorLastName = serviceRoleRequest.Requestor.RequestorLastName;
                                else
                                {
                                    errorMsg = "RequestorLastName is missing";
                                }
                                if (serviceRoleRequest.Requestor.RequestorID != null)
                                    requestor.RequestorID = serviceRoleRequest.Requestor.RequestorID;
                                else
                                {
                                    errorMsg = "RequestorID is missing";
                                }
                                requestor.RequestorIDType = new RequestorRequestorIDType();
                                if (serviceRoleRequest.Requestor.RequestorIDType != null)
                                    requestor.RequestorIDType = (RequestorRequestorIDType)serviceRoleRequest.Requestor.RequestorIDType;
                                else
                                {
                                    errorMsg = "RequestorIDType is missing";
                                }


                                //******adding advisor in requestor*********
                                requestor.Advisor = new Advisor();
                                if (serviceRoleRequest.Requestor.Advisor != null)
                                {
                                    if (serviceRoleRequest.Requestor.Advisor.AdvisorID != null)
                                        requestor.Advisor.AdvisorID = serviceRoleRequest.Requestor.Advisor.AdvisorID;
                                    else
                                    {
                                        errorMsg = "AdvisorID is missing";
                                    }
                                    if (serviceRoleRequest.Requestor.Advisor.AdvisorIDType != null)
                                        requestor.Advisor.AdvisorIDType = serviceRoleRequest.Requestor.Advisor.AdvisorIDType;
                                    else
                                    {
                                        errorMsg = "AdvisorIDType is missing";
                                    }
                                    if (requestor.Advisor.AdvisorReference != null)
                                        requestor.Advisor.AdvisorReference = serviceRoleRequest.Requestor.Advisor.AdvisorReference;
                                }
                                else
                                {
                                    requestor.Advisor = null;
                                }
                                modifyServiceRole.Requestor = requestor;
                            }
                            else
                            {
                                errorMsg = "Requestor is missing";
                                break;
                            }

                            //***** adding Target values*********
                            if (serviceRoleRequest.Target != null)
                            {
                                modifyServiceRole.Target = new Target();
                                if (serviceRoleRequest.Target.TargetDOB != null)
                                    modifyServiceRole.Target.TargetDOB = serviceRoleRequest.Target.TargetDOB;
                                if (serviceRoleRequest.Target.TargetDOBSpecified != null)
                                    modifyServiceRole.Target.TargetDOBSpecified = serviceRoleRequest.Target.TargetDOBSpecified;
                                if (serviceRoleRequest.Target.TargetFirstName != null)
                                    modifyServiceRole.Target.TargetFirstName = serviceRoleRequest.Target.TargetFirstName;
                                else
                                {
                                    errorMsg = "TargetFirstName is missing";
                                }
                                if (serviceRoleRequest.Target.TargetLastName != null)
                                    modifyServiceRole.Target.TargetLastName = serviceRoleRequest.Target.TargetLastName;
                                else
                                {
                                    errorMsg = "TargetLastName is missing";
                                }
                                if (serviceRoleRequest.Target.TargetKCIType != null)
                                {
                                    TargetTargetKCIType tragetKciType = new TargetTargetKCIType();
                                    List<TargetTargetKCIType> kciTypeList = new List<TargetTargetKCIType>();
                                    foreach (BT.ESB.RoBT.TargetTargetKCIType kciType in serviceRoleRequest.Target.TargetKCIType)
                                    {
                                        tragetKciType = (TargetTargetKCIType)kciType;
                                        kciTypeList.Add(tragetKciType);
                                    }
                                    modifyServiceRole.Target.TargetKCIType = kciTypeList.ToArray();
                                }
                                else
                                {
                                    errorMsg = "TargetKCIType is missing";
                                }
                                if (serviceRoleRequest.Target.TargetSMSNumber != null)
                                    modifyServiceRole.Target.TargetSMSNumber = serviceRoleRequest.Target.TargetSMSNumber;
                                if (serviceRoleRequest.Target.Identifiers != null)
                                {
                                    BT.ESB.RoBT.Identifier identifiers = new BT.ESB.RoBT.Identifier();
                                    foreach (BT.ESB.RoBT.Identifier identifier in serviceRoleRequest.Target.Identifiers)
                                    {
                                        if (!idEmailType.Equals(identifier.IDType.ToString()))
                                        {
                                            modifyServiceRole.Target.TargetID = identifier.ID;
                                            modifyServiceRole.Target.TargetIDType = new TargetTargetIDType();
                                            modifyServiceRole.Target.TargetIDType = TargetTargetIDType.BTID;
                                        }
                                    }
                                }
                                else
                                {
                                    errorMsg = "TargetIdentifiers is missing";
                                }
                            }
                            else
                            {
                                errorMsg = "Target is missing";
                                break;
                            }

                            //***** ServiceRole *************
                            if (serviceRoleRequest.ServiceRole != null)
                            {
                                ServiceRole serviceRole = new ServiceRole();
                                if (serviceRoleRequest.ServiceRole.BACNumber != null)
                                    serviceRole.BACNumber = serviceRoleRequest.ServiceRole.BACNumber;
                                else
                                {
                                    errorMsg = "BACNumber is missing";
                                    break;
                                }
                                if (serviceRoleRequest.ServiceRole.RoleDetails != null)
                                {
                                    List<ServiceRoleDetails> roleDetailsList = new List<ServiceRoleDetails>();
                                    isDNExists = false;

                                    foreach (BT.ESB.RoBT.ServiceRoleDetails getRoleDetails in serviceRoleRequest.ServiceRole.RoleDetails)
                                    {
                                        if (getRoleDetails.Service.Equals("SPRING_GSM", StringComparison.OrdinalIgnoreCase) && !getRoleDetails.Service.Equals("BTSPORT", StringComparison.OrdinalIgnoreCase))
                                        {
                                            ServiceRoleDetails roleDetails = new ServiceRoleDetails();
                                            roleDetails.Role = "SERVICE_USER";
                                            roleDetails.Ordinality = ServiceRoleDetailsOrdinality.Secondary;
                                            roleDetails.Service = "BTSPORT";

                                            Parameter roleIdentity = null;
                                            List<Parameter> roleIdentityList = new List<Parameter>();
                                            if (getRoleDetails.RoleIdentities != null)
                                            {
                                                foreach (BT.ESB.RoBT.Parameter roleidentitits in getRoleDetails.RoleIdentities)
                                                {
                                                    // BTR-91821
                                                    // checking the dn in roleidentities and btcom with targetid which are present in the spring delegate not in sport delegate, 
                                                    // if not present then adding the roledetails to the request 
                                                    // if present then ignore.
                                                    if (SportsDelegateDNslist != null)
                                                    {
                                                        foreach (string dn in SportsDelegateDNslist)
                                                        {
                                                            string[] dnBtcom = dn.ToString().Split(';');
                                                            if ((dnBtcom[0].Equals(roleidentitits.Value)) && (roleidentitits.Name.Equals("MOBILE_MSISDN", StringComparison.OrdinalIgnoreCase)) &&
                                                                 modifyServiceRole.Target.TargetID.Equals(dnBtcom[1]))
                                                            {
                                                                roleIdentity = new Parameter();
                                                                roleIdentity.Name = roleidentitits.Name;
                                                                roleIdentity.Value = roleidentitits.Value;
                                                                roleIdentityList.Add(roleIdentity);
                                                            }
                                                        }
                                                        if (roleidentitits.Name.Equals("RVSID", StringComparison.OrdinalIgnoreCase))
                                                        {
                                                            roleIdentity = new Parameter();
                                                            roleIdentity.Name = roleidentitits.Name;
                                                            roleIdentity.Value = roleidentitits.Value;
                                                            roleIdentityList.Add(roleIdentity);
                                                        }
                                                    }
                                                }
                                                if (roleIdentityList != null && roleIdentityList.Count() != 2)
                                                {
                                                    isDNExists = true;
                                                    break;
                                                }
                                                roleDetails.RoleIdentities = roleIdentityList.ToArray();
                                            }
                                            if (isDNExists)
                                            {
                                                continue;
                                            }
                                            Parameter characteristics = new Parameter();
                                            characteristics.Name = "BTS_APP_CODE";
                                            characteristics.Value = BTS_APP_CODE;

                                            roleDetails.Characteristics = new Parameter[1];
                                            roleDetails.Characteristics[0] = characteristics;

                                            roleDetailsList.Add(roleDetails);
                                        }
                                    }
                                    if (roleDetailsList != null && roleDetailsList.Count() != 0)
                                        serviceRole.RoleDetails = roleDetailsList.ToArray();
                                    else
                                        errorMsg = "with that role details already delegate role is created";
                                }
                                else
                                {
                                    errorMsg = "RoleDetails is missing";
                                }

                                modifyServiceRole.ServiceRole = new ServiceRole[1];
                                modifyServiceRole.ServiceRole[0] = serviceRole;
                            }
                            else
                            {
                                errorMsg = "ServiceRole is missing";
                                break;
                            }

                            //******* Sending manageroleRequest to ESB
                            if (string.IsNullOrEmpty(errorMsg))
                            {
                                if (ESBRestCallWrapper.PostManageRoleRequest(postUrl, modifyServiceRole, ref esbResponse))
                                {

                                    if ((esbResponse == null) || !esbResponse.ErrorCode.Equals("000"))
                                    {
                                        isSuccess = false;
                                        //notification.sendNotification(false, false, "0041", "error in deserializing the ESBREesponse", ref e2eData);
                                        //Logger.Write(thomasParams.orderKey + "," + Failed + " error in deserializing the ESBREesponse or error code " + esbResponse.ErrorCode + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                                    }
                                    else
                                    {
                                        isSuccess = true;
                                        isServiceRoleExists = true;
                                        requestKey = esbResponse.Requestkey;
                                    }
                                }
                                else
                                {
                                    isSuccess = false;
                                    //notification.sendNotification(false, false, "0041", "error while posting managerolerequest to ESB after provision", ref e2eData);
                                    //Logger.Write(thomasParams.orderKey + "," + Failed + " error while posting managerolerequest to ESB ," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                                }
                            }
                            else
                            {
                                //notification.sendNotification(false, false, "0041", "error while posting managerolerequest to ESB after provision", ref e2eData);
                                //Logger.Write(thomasParams.orderKey + "," + Failed + "with error"+ errorMsg +"while posting managerolerequest to ESB ," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                            }
                        }
                    }

                    notification.sendNotification(true, false, "004", requestKey, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                    Logger.Write(thomasParams.orderKey + ", with " + requestKey + " of delegation " + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.MessageTrace);
                }


            }
            catch (Exception ex)
            {
                notification.sendNotification(true, false, "004", requestKey, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                Logger.Write(thomasParams.orderKey + ", with " + requestKey + " of delegation failed with " + ex.Message + "," + "", Logger.TypeEnum.MessageTrace);
            }
        }

        public void ThomasCeaseMapper(ClientServiceInstanceV1[] clientServiceInstnce,ThomasParameters thomasParameters, MSEOOrderNotification notification, short retryCount, ref E2ETransaction e2eData)
        {
            string dnpScode = string.Empty;

            //ClientServiceInstanceV1[] clientServiceInstnce = DnpWrapper.getServiceInstanceV1(thomasParameters.BillAccountNumber, BACID_IDENTIFER_NAMEPACE, BTSKY_SERVICECODE_NAMEPACE);

            if (clientServiceInstnce != null && clientServiceInstnce.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BTSKY_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
            {
                ClientServiceInstanceV1 clientService = clientServiceInstnce.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(BTSKY_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                if (thomasParameters.Reason != null && thomasParameters.Reason.Equals("suspend", StringComparison.OrdinalIgnoreCase))
                {
                    if (clientService.clientServiceInstanceStatus.value.Equals("suspended", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new DnpException("BT Sports Serivce is in suspended state with the Bill Account Number" + thomasParameters.BillAccountNumber);
                    }
                    else
                    {
                        notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false, ActivityStartTime, guid, bptmTxnId);
                        Logger.Write(thomasParameters.orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.MessageTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, AcceptedNotificationSent, TimeSpan.Zero, "ThomasTrace", thomasParameters.orderKey);

                        UpdateThomasServiceState(thomasParameters, notification, retryCount, ref e2eData);
                    }
                }
                else if (!clientService.clientServiceInstanceStatus.value.Equals("ceased", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(thomasParameters.Reason))
                {
                    if (clientService.clientServiceInstanceCharacteristic != null && clientService.clientServiceInstanceCharacteristic.ToList().Exists(si => si.name.Equals("SCODE", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(si.value)))
                    {
                        dnpScode = clientService.clientServiceInstanceCharacteristic.ToList().Where(si => si.name.Equals("SCODE", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                    }
                    if (string.IsNullOrEmpty(dnpScode) || thomasParameters.Scode.Equals(dnpScode, StringComparison.OrdinalIgnoreCase))
                    {
                        notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false, ActivityStartTime, guid, bptmTxnId);
                        Logger.Write(thomasParameters.orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.MessageTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, AcceptedNotificationSent, TimeSpan.Zero, "ThomasTrace", thomasParameters.orderKey);

                        if (clientService.clientServiceRole != null && clientService.clientServiceRole.Count() > 0)
                        {
                            if (clientService.clientServiceRole.ToList().Exists(sr => sr.id.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase)))
                                thomasParameters.Reason = "onbillaccount-yes";
                            else
                                thomasParameters.Reason = "onbillaccount-delinked";
                            CeaseThomasService(thomasParameters, notification, retryCount, ref e2eData);
                        }
                        else
                        {
                            thomasParameters.Reason = "onbillaccount-no";
                            CeaseThomasServiceforStandalone(thomasParameters, notification, retryCount, ref e2eData);
                        }                       
                    }
                    else
                    {
                        throw new DnpException("Unable to find BT Sport service with specified Scode " + thomasParameters.Scode + "for BAC " + thomasParameters.BillAccountNumber + " at DNP end");
                    }
                }
                else
                {
                    throw new DnpException("BTSPORT service for the given BAC is in " + clientService.clientServiceInstanceStatus.value + " state.");
                }
            }
            else if (string.IsNullOrEmpty(thomasParameters.Reason))
            {
                ClientServiceInstanceV1[] clientServiceInstnceWithCAK = DnpWrapper.getServiceInstanceV1(thomasParameters.CustomerID, "CAKID", BTSKY_SERVICECODE_NAMEPACE);

                if (clientServiceInstnceWithCAK != null && clientServiceInstnceWithCAK.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BTSKY_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                {
                    ClientServiceInstanceV1 clientService = clientServiceInstnceWithCAK.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(BTSKY_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (clientService.clientServiceInstanceCharacteristic != null && clientService.clientServiceInstanceCharacteristic.ToList().Exists(si => si.name.Equals("SCODE", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(si.value)))
                    {
                        dnpScode = clientService.clientServiceInstanceCharacteristic.ToList().Where(si => si.name.Equals("SCODE", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                    }
                    if (string.IsNullOrEmpty(dnpScode) || thomasParameters.Scode.Equals(dnpScode, StringComparison.OrdinalIgnoreCase))
                    {
                        if (clientService.clientServiceInstanceStatus.value.ToLower() == "active")
                        {
                            notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false, ActivityStartTime, guid, bptmTxnId);
                            Logger.Write(thomasParameters.orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.MessageTrace);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, AcceptedNotificationSent, TimeSpan.Zero, "ThomasTrace", thomasParameters.orderKey);

                            thomasParameters.Reason = "oncakid";
                            // need to trigger CeaseThomas call.
                            CeaseThomasServiceforStandalone(thomasParameters, notification, retryCount, ref e2eData);
                        }
                        else
                        {
                            throw new DnpException("NO Any BTSPORT is in ACTIVE state with CAKID");
                        }
                    }
                    else
                    {
                        throw new DnpException("Unable to find BT Sport service with specified scode" + thomasParameters.Scode + "for CAKID " + thomasParameters.CustomerID + " at DNP end");
                    }
                }
                else
                {
                    throw new DnpException("No BTSPORT Service in the DnP with BillAccount and Customer ID");
                }
            }
            else throw new DnpException("No BT Sports Service in the DnP with the Bill Account Number" + thomasParameters.BillAccountNumber + " at DNP end");
        }
        /// <summary>
        /// to update the service status value as suspend.
        /// </summary>
        /// <param name="thomasParams">thomasParams</param>
        /// <param name="notification">notification</param>
        /// <param name="retryCount">retryCount</param>
        /// <param name="e2eData">e2eData</param>
        public void UpdateThomasServiceState(ThomasParameters thomasParams, MSEOOrderNotification notification, short retryCount, ref E2ETransaction e2eData)
        {
            manageServiceInstanceV1Request1 request = new manageServiceInstanceV1Request1();
            manageServiceInstanceV1Response1 profileResponse = null;
            try
            {
                request.manageServiceInstanceV1Request = new ManageServiceInstanceV1Request();
                request.manageServiceInstanceV1Request.standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                request.manageServiceInstanceV1Request.standardHeader.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                request.manageServiceInstanceV1Request.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                request.manageServiceInstanceV1Request.standardHeader.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

                request.manageServiceInstanceV1Request.manageServiceInstanceV1Req = new ManageServiceInstanceV1Req();

                ClientServiceInstanceV1[] clientServiceInstanceV1 = new ClientServiceInstanceV1[1];

                clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                clientServiceInstanceV1[0].action = ACTION_UPDATE;
                clientServiceInstanceV1[0].serviceIdentity = new ServiceIdentity[1];
                clientServiceInstanceV1[0].serviceIdentity[0] = new ServiceIdentity();
                clientServiceInstanceV1[0].serviceIdentity[0].action = ACTION_SEARCH;
                clientServiceInstanceV1[0].serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                clientServiceInstanceV1[0].serviceIdentity[0].value = thomasParams.BillAccountNumber;

                clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BTSKY_SERVICECODE_NAMEPACE;
                clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                if(thomasParams.Reason.Equals("DebtResume",StringComparison.OrdinalIgnoreCase))
                    clientServiceInstanceV1[0].clientServiceInstanceStatus.value = ACTIVE;
                else if (thomasParams.Reason.Equals("suspend", StringComparison.OrdinalIgnoreCase))
                    clientServiceInstanceV1[0].clientServiceInstanceStatus.value = SUSPENDED;

                request.manageServiceInstanceV1Request.manageServiceInstanceV1Req.clientServiceInstanceV1 = new ClientServiceInstanceV1();
                request.manageServiceInstanceV1Request.manageServiceInstanceV1Req.clientServiceInstanceV1 = clientServiceInstanceV1[0];

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, BTSKY_SERVICECODE_NAMEPACE);

                request.manageServiceInstanceV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                request.manageServiceInstanceV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                Logger.Write(thomasParams.orderKey + "," + MakingDnPcall + "," + SendingRequesttoDnP, Logger.TypeEnum.MessageTrace);
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, thomasParams.orderKey, request.SerializeObject(), ConfigurationManager.AppSettings["BTSportsProductCode"].ToString());
                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey, request.SerializeObject());

                profileResponse = DnpWrapper.manageServiceInstanceV1Thomas(request, thomasParams.orderKey);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey, profileResponse.SerializeObject());
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, thomasParams.orderKey, profileResponse.SerializeObject(), ConfigurationManager.AppSettings["BTSportsProductCode"].ToString());
                Logger.Write(thomasParams.orderKey + "," + GotResponseFromDnP + "," + GotResponsefromDnPNotification, Logger.TypeEnum.MessageTrace);

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

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                    Logger.Write(thomasParams.orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.MessageTrace);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, CompletedNotificationSent, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                }
                else
                {
                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description;
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedErrorMessage + errorMessage, Logger.TypeEnum.ExceptionTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + FailedErrorMessage + errorMessage, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + DnPAdminstratorFailedResponse + errorMessage, Logger.TypeEnum.MessageTrace);
                            //Logger.Write(orderKey + " : recieved failure response from DnP (Administrator) " + errorMessage, Logger.TypeEnum.ExceptionTrace);
                            //retry
                            if (retryCount++ < System.Int32.Parse(ConfigurationManager.AppSettings["RetryCountForThomasProvide"]))
                            {
                                if (retryCount == System.Int32.Parse(ConfigurationManager.AppSettings["RetryCountForThomasProvide"]))
                                {
                                    Logger.Write(thomasParams.orderKey + "," + Failed + "," + NonFunctionalException + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.ExceptionTrace);
                                    //Logger.Write("Non Functional Exception from DNP, reached max number of Retry , we are shutdowning service" + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.EmailTrace);

                                    //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA;
                                    e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);

                                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                                        e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                                    else
                                        e2eData.endOutboundCall(e2eData.toString());
                                    //sending failure notification
                                    notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                                    Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                                }
                                else
                                {
                                    Thread.Sleep(System.Int32.Parse(ConfigurationManager.AppSettings["SleepTimeoutInMilliSec"]));
                                    UpdateThomasServiceState(thomasParams, notification, retryCount, ref e2eData);
                                }
                            }
                        }
                        else
                        {
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailureResponseFromDnP + errorMessage, Logger.TypeEnum.MessageTrace);

                            e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                            if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                                e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                            else
                                e2eData.endOutboundCall(e2eData.toString());
                            //sending failure notification
                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                        }
                    }
                    else
                    {
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.MessageTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + NullResponseFromDnP, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //sending failure notification
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailureResponseFromDnP + ex.Message, Logger.TypeEnum.MessageTrace);
                LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + FailedErrorMessage + ex.Message, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                //sending failure notification
                //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = e2eData;
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedErrorMessage + ex.Message, Logger.TypeEnum.ExceptionTrace);
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
            }
        }
        /// <summary>
        /// to Cease a Standalone serivce
        /// </summary>
        /// <param name="thomasParams"></param>
        /// <param name="notification"></param>
        /// <param name="retryCount"></param>
        /// <param name="e2eData"></param>
        public void CeaseThomasServiceforStandalone(ThomasParameters thomasParams, MSEOOrderNotification notification, short retryCount, ref E2ETransaction e2eData)
        {
            manageServiceInstanceV1Request1 request = new manageServiceInstanceV1Request1();
            manageServiceInstanceV1Response1 profileResponse = null;
            try
            {
                request.manageServiceInstanceV1Request = new ManageServiceInstanceV1Request();
                request.manageServiceInstanceV1Request.standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();                
                request.manageServiceInstanceV1Request.standardHeader.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                request.manageServiceInstanceV1Request.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                request.manageServiceInstanceV1Request.standardHeader.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

                request.manageServiceInstanceV1Request.manageServiceInstanceV1Req = new ManageServiceInstanceV1Req();

                ClientServiceInstanceV1 clientServiceInstance = null;


                if (thomasParams.Reason.Equals("onbillaccount-no", StringComparison.OrdinalIgnoreCase))
                {
                    clientServiceInstance = new ClientServiceInstanceV1();
                    clientServiceInstance.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                    clientServiceInstance.clientServiceInstanceIdentifier.value = BTSKY_SERVICECODE_NAMEPACE;
                    clientServiceInstance.serviceIdentity = new ServiceIdentity[1];
                    clientServiceInstance.serviceIdentity[0] = new ServiceIdentity();
                    clientServiceInstance.serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                    clientServiceInstance.serviceIdentity[0].value = thomasParams.BillAccountNumber;
                    clientServiceInstance.serviceIdentity[0].action = ACTION_SEARCH;
                    clientServiceInstance.clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                    clientServiceInstance.clientServiceInstanceStatus.value = ACTION_CEASED;
                    clientServiceInstance.action = ACTION_UPDATE;

                }
                else if (thomasParams.Reason.Equals("oncakid", StringComparison.OrdinalIgnoreCase))
                {
                    clientServiceInstance = new ClientServiceInstanceV1();
                    clientServiceInstance.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                    clientServiceInstance.clientServiceInstanceIdentifier.value = BTSKY_SERVICECODE_NAMEPACE;
                    clientServiceInstance.serviceIdentity = new ServiceIdentity[1];
                    clientServiceInstance.serviceIdentity[0] = new ServiceIdentity();
                    clientServiceInstance.serviceIdentity[0].domain = CAKID_IDENTIFER_NAMEPACE;
                    clientServiceInstance.serviceIdentity[0].value = thomasParams.CustomerID;
                    clientServiceInstance.serviceIdentity[0].action = ACTION_SEARCH;
                    clientServiceInstance.clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                    clientServiceInstance.clientServiceInstanceStatus.value = ACTION_CEASED;
                    clientServiceInstance.action = ACTION_UPDATE;

                }

                List<ClientServiceInstanceCharacteristic> listserivceinstancechars = new List<ClientServiceInstanceCharacteristic>();
                listserivceinstancechars = CreateServiceInstanceChars(thomasParams);

                if (listserivceinstancechars != null && listserivceinstancechars.Count() > 0)
                    clientServiceInstance.clientServiceInstanceCharacteristic = listserivceinstancechars.ToArray();

                request.manageServiceInstanceV1Request.manageServiceInstanceV1Req.clientServiceInstanceV1 = new ClientServiceInstanceV1();
                request.manageServiceInstanceV1Request.manageServiceInstanceV1Req.clientServiceInstanceV1 = clientServiceInstance;

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, BTSKY_SERVICECODE_NAMEPACE);

                request.manageServiceInstanceV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                request.manageServiceInstanceV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                Logger.Write(thomasParams.orderKey + "," + MakingDnPcall + "," + SendingRequesttoDnP, Logger.TypeEnum.MessageTrace);
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, thomasParams.orderKey, request.SerializeObject(), ConfigurationManager.AppSettings["BTSportsProductCode"].ToString());
                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey, request.SerializeObject());

                profileResponse = DnpWrapper.manageServiceInstanceV1Thomas(request, thomasParams.orderKey);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey, profileResponse.SerializeObject());
               // MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, thomasParams.orderKey, profileResponse.SerializeObject(), ConfigurationManager.AppSettings["BTSportsProductCode"].ToString());
                Logger.Write(thomasParams.orderKey + "," + GotResponseFromDnP + "," + GotResponsefromDnPNotification, Logger.TypeEnum.MessageTrace);


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

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                    Logger.Write(thomasParams.orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.MessageTrace);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, CompletedNotificationSent, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                }
                else
                {
                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description;
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedErrorMessage + errorMessage, Logger.TypeEnum.ExceptionTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + FailedErrorMessage + errorMessage, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + DnPAdminstratorFailedResponse + errorMessage, Logger.TypeEnum.MessageTrace);
                            //Logger.Write(orderKey + " : recieved failure response from DnP (Administrator) " + errorMessage, Logger.TypeEnum.ExceptionTrace);
                            //retry
                            if (retryCount++ < System.Int32.Parse(ConfigurationManager.AppSettings["RetryCountForThomasProvide"]))
                            {
                                if (retryCount == System.Int32.Parse(ConfigurationManager.AppSettings["RetryCountForThomasProvide"]))
                                {
                                    Logger.Write(thomasParams.orderKey + "," + Failed + "," + NonFunctionalException + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.ExceptionTrace);
                                    //Logger.Write("Non Functional Exception from DNP, reached max number of Retry , we are shutdowning service" + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.EmailTrace);

                                    //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA;
                                    e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);

                                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                                        e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                                    else
                                        e2eData.endOutboundCall(e2eData.toString());
                                    //sending failure notification
                                    notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                                    Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                                }
                                else
                                {
                                    Thread.Sleep(System.Int32.Parse(ConfigurationManager.AppSettings["SleepTimeoutInMilliSec"]));
                                    CeaseThomasServiceforStandalone(thomasParams, notification, retryCount, ref e2eData);
                                }
                            }
                        }
                        else
                        {
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailureResponseFromDnP + errorMessage, Logger.TypeEnum.MessageTrace);

                            e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                            if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                                e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                            else
                                e2eData.endOutboundCall(e2eData.toString());
                            //sending failure notification
                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                        }
                    }
                    else
                    {
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.MessageTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + NullResponseFromDnP, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //sending failure notification
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailureResponseFromDnP + ex.Message, Logger.TypeEnum.MessageTrace);
                LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + FailedErrorMessage + ex.Message, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                //sending failure notification
                //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = e2eData;
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedErrorMessage + ex.Message, Logger.TypeEnum.ExceptionTrace);
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
            }
        }
        /// <summary>
        /// to Cease a serivce
        /// </summary>
        /// <param name="thomasParams"></param>
        /// <param name="notification"></param>
        /// <param name="retryCount"></param>
        /// <param name="e2eData"></param>
        public void CeaseThomasService(ThomasParameters thomasParams, MSEOOrderNotification notification, short retryCount, ref E2ETransaction e2eData)
        {
            manageClientServiceV1Response1 profileResponse = null;
            manageClientServiceV1Request1 profileRequest = new manageClientServiceV1Request1();
            try
            {
                profileRequest.manageClientServiceV1Request = new ManageClientServiceV1Request();
                ManageClientServiceV1Req manageClientServiceV1Req = new ManageClientServiceV1Req();

                ClientServiceRole[] clientServiceRole = null;

                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();


                ClientServiceInstanceSearchCriteriaV1 clientServiceInstanceSearchCriteria = new ClientServiceInstanceSearchCriteriaV1();
                clientServiceInstanceSearchCriteria.serviceIdentityDomain = BACID_IDENTIFER_NAMEPACE;
                clientServiceInstanceSearchCriteria.serviceIdentityValue = thomasParams.BillAccountNumber;
                clientServiceInstanceSearchCriteria.serviceCode = BTSKY_SERVICECODE_NAMEPACE;
                clientServiceInstanceSearchCriteria.identifierDomainCode = BACID_IDENTIFER_NAMEPACE;
                clientServiceInstanceSearchCriteria.identifierValue = thomasParams.BillAccountNumber;

                ClientServiceInstanceV1 clientServiceInstance = new ClientServiceInstanceV1();
                clientServiceInstance.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstance.clientServiceInstanceIdentifier.value = BTSKY_SERVICECODE_NAMEPACE;
                clientServiceInstance.serviceIdentity = new ServiceIdentity[1];
                clientServiceInstance.serviceIdentity[0] = new ServiceIdentity();
                clientServiceInstance.serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                clientServiceInstance.serviceIdentity[0].value = thomasParams.BillAccountNumber;
                clientServiceInstance.serviceIdentity[0].action = ACTION_SEARCH;
                clientServiceInstance.clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                clientServiceInstance.clientServiceInstanceStatus.value = ACTION_CEASED;
                clientServiceInstance.action = ACTION_UPDATE;

                if (thomasParams.Reason.Equals("onbillaccount-delinked", StringComparison.OrdinalIgnoreCase))
                    clientServiceRole = new ClientServiceRole[1];
                else
                    clientServiceRole = new ClientServiceRole[2];

                clientServiceRole[0] = new ClientServiceRole();
                clientServiceRole[0].id = ADMIN_ROLE;
                clientServiceRole[0].clientServiceRoleStatus = new ClientServiceRoleStatus();
                clientServiceRole[0].clientServiceRoleStatus.value = INACTIVE;

                clientServiceRole[0].clientIdentity = new ClientIdentity[1];
                clientServiceRole[0].clientIdentity[0] = new ClientIdentity();
                clientServiceRole[0].clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                clientServiceRole[0].clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                clientServiceRole[0].clientIdentity[0].value = thomasParams.BillAccountNumber;
                clientServiceRole[0].clientIdentity[0].action = ACTION_SEARCH;
                clientServiceRole[0].action = ACTION_UPDATE;

                if (!thomasParams.Reason.Equals("onbillaccount-delinked", StringComparison.OrdinalIgnoreCase))
                {
                    clientServiceRole[1] = new ClientServiceRole();
                    clientServiceRole[1].id = DEFAULT_ROLE;
                    clientServiceRole[1].clientServiceRoleStatus = new ClientServiceRoleStatus();
                    clientServiceRole[1].clientServiceRoleStatus.value = INACTIVE;
                    clientServiceRole[1].action = ACTION_UPDATE;
                }

                clientServiceInstance.clientServiceRole = clientServiceRole;

                List<ClientServiceInstanceCharacteristic> listserivceinstancechars = new List<ClientServiceInstanceCharacteristic>();
                listserivceinstancechars = CreateServiceInstanceChars(thomasParams);

                if (listserivceinstancechars != null && listserivceinstancechars.Count() > 0)
                    clientServiceInstance.clientServiceInstanceCharacteristic = listserivceinstancechars.ToArray();

                manageClientServiceV1Req.clientServiceInstanceSearchCriteria = clientServiceInstanceSearchCriteria;
                manageClientServiceV1Req.clientServiceInstanceV1 = clientServiceInstance;
                profileRequest.manageClientServiceV1Request.manageClientServiceV1Req = manageClientServiceV1Req;
                profileRequest.manageClientServiceV1Request.standardHeader = headerBlock;

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, BTSKY_SERVICECODE_NAMEPACE);

                profileRequest.manageClientServiceV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                profileRequest.manageClientServiceV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                Logger.Write(thomasParams.orderKey + "," + MakingDnPcall + "," + SendingRequesttoDnP, Logger.TypeEnum.MessageTrace);
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, thomasParams.orderKey, profileRequest.SerializeObject(), ConfigurationManager.AppSettings["BTSportsProductCode"].ToString());
                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey, profileRequest.SerializeObject());

                profileResponse = DnpWrapper.manageClientServiceV1(profileRequest, thomasParams.orderKey);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey, profileResponse.SerializeObject());
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, thomasParams.orderKey, profileResponse.SerializeObject(), ConfigurationManager.AppSettings["BTSportsProductCode"].ToString());
                Logger.Write(thomasParams.orderKey + "," + GotResponseFromDnP + "," + GotResponsefromDnPNotification, Logger.TypeEnum.MessageTrace);
                
                if (profileResponse != null
                    && profileResponse.manageClientServiceV1Response != null
                    && profileResponse.manageClientServiceV1Response.standardHeader != null
                    && profileResponse.manageClientServiceV1Response.standardHeader.serviceState != null
                    && profileResponse.manageClientServiceV1Response.standardHeader.serviceState.stateCode != null
                    && profileResponse.manageClientServiceV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse != null && profileResponse.manageClientServiceV1Response != null && profileResponse.manageClientServiceV1Response.standardHeader != null && profileResponse.manageClientServiceV1Response.standardHeader.e2e != null && profileResponse.manageClientServiceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageClientServiceV1Response.standardHeader.e2e.E2EDATA))
                        e2eData.endOutboundCall(profileResponse.manageClientServiceV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                    Logger.Write(thomasParams.orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.MessageTrace);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, CompletedNotificationSent, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                }
                else
                {
                    if (profileResponse != null && profileResponse.manageClientServiceV1Response != null && profileResponse.manageClientServiceV1Response.manageClientServiceV1Res != null && profileResponse.manageClientServiceV1Response.manageClientServiceV1Res.messages != null && profileResponse.manageClientServiceV1Response.manageClientServiceV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse.manageClientServiceV1Response.manageClientServiceV1Res.messages[0].description;
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedErrorMessage + errorMessage, Logger.TypeEnum.ExceptionTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + FailedErrorMessage + errorMessage, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + DnPAdminstratorFailedResponse + errorMessage, Logger.TypeEnum.MessageTrace);
                            //Logger.Write(orderKey + " : recieved failure response from DnP (Administrator) " + errorMessage, Logger.TypeEnum.ExceptionTrace);
                            //retry
                            if (retryCount++ < System.Int32.Parse(ConfigurationManager.AppSettings["RetryCountForThomasProvide"]))
                            {
                                if (retryCount == System.Int32.Parse(ConfigurationManager.AppSettings["RetryCountForThomasProvide"]))
                                {
                                    Logger.Write(thomasParams.orderKey + "," + Failed + "," + NonFunctionalException + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.ExceptionTrace);
                                    //Logger.Write("Non Functional Exception from DNP, reached max number of Retry , we are shutdowning service" + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.EmailTrace);

                                    //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA;
                                    e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);

                                    if (profileResponse != null && profileResponse.manageClientServiceV1Response != null && profileResponse.manageClientServiceV1Response.standardHeader != null && profileResponse.manageClientServiceV1Response.standardHeader.e2e != null && profileResponse.manageClientServiceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageClientServiceV1Response.standardHeader.e2e.E2EDATA))
                                        e2eData.endOutboundCall(profileResponse.manageClientServiceV1Response.standardHeader.e2e.E2EDATA);
                                    else
                                        e2eData.endOutboundCall(e2eData.toString());
                                    //sending failure notification
                                    notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                                    Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                                }
                                else
                                {
                                    Thread.Sleep(System.Int32.Parse(ConfigurationManager.AppSettings["SleepTimeoutInMilliSec"]));
                                    CeaseThomasService(thomasParams, notification, retryCount, ref e2eData);
                                }
                            }
                        }
                        else
                        {
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailureResponseFromDnP + errorMessage, Logger.TypeEnum.MessageTrace);

                            e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                            if (profileResponse != null && profileResponse.manageClientServiceV1Response != null && profileResponse.manageClientServiceV1Response.standardHeader != null && profileResponse.manageClientServiceV1Response.standardHeader.e2e != null && profileResponse.manageClientServiceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageClientServiceV1Response.standardHeader.e2e.E2EDATA))
                                e2eData.endOutboundCall(profileResponse.manageClientServiceV1Response.standardHeader.e2e.E2EDATA);
                            else
                                e2eData.endOutboundCall(e2eData.toString());
                            //sending failure notification
                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                        }
                    }
                    else
                    {
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.MessageTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + NullResponseFromDnP, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse != null && profileResponse.manageClientServiceV1Response != null && profileResponse.manageClientServiceV1Response.standardHeader != null && profileResponse.manageClientServiceV1Response.standardHeader.e2e != null && profileResponse.manageClientServiceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageClientServiceV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageClientServiceV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //sending failure notification
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailureResponseFromDnP + ex.Message, Logger.TypeEnum.MessageTrace);
                LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + FailedErrorMessage + ex.Message, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                //sending failure notification
                //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = e2eData;
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse != null && profileResponse.manageClientServiceV1Response != null && profileResponse.manageClientServiceV1Response.standardHeader != null && profileResponse.manageClientServiceV1Response.standardHeader.e2e != null && profileResponse.manageClientServiceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageClientServiceV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse.manageClientServiceV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedErrorMessage + ex.Message, Logger.TypeEnum.ExceptionTrace);
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
            }
        }

        public void ThomasReactivateMapper(ClientServiceInstanceV1[] clientServiceInstnce, ThomasParameters thomasParameters, MSEOOrderNotification notification, short retryCount, ref E2ETransaction e2eData)
        {
            if (thomasParameters.Reason != null && thomasParameters.Reason.Equals("debtresume", StringComparison.OrdinalIgnoreCase))
            {
                if (clientServiceInstnce != null && clientServiceInstnce.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BTSKY_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                {
                    ClientServiceInstanceV1 clientService = clientServiceInstnce.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(BTSKY_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (clientService.clientServiceInstanceStatus.value.Equals("active", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new DnpException("BT Sports Serivce is in active state with Bill Account Number");
                    }
                    else
                    {
                        notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false, ActivityStartTime, guid, bptmTxnId);
                        Logger.Write(thomasParameters.orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.MessageTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, AcceptedNotificationSent, TimeSpan.Zero, "ThomasTrace", thomasParameters.orderKey);

                        UpdateThomasServiceState(thomasParameters, notification, retryCount, ref e2eData);
                    }
                }
                else
                {
                    throw new DnpException("No BT Sports Service in the DnP with Bill Account Number");
                }
            }
        }

        /// <summary>
        /// updating the serivceinstancechars to delete
        /// </summary>
        /// <param name="thomasParams"></param>
        /// <returns></returns>
        public static List<ClientServiceInstanceCharacteristic> CreateServiceInstanceChars(ThomasParameters thomasParams)
        {
            List<ClientServiceInstanceCharacteristic> listserivceinstancechars = new List<ClientServiceInstanceCharacteristic>();
            ClientServiceInstanceCharacteristic srvcinstancechar = null;

            if (!string.IsNullOrEmpty(thomasParams.BTS_APP_CODE))
            {
                srvcinstancechar = new ClientServiceInstanceCharacteristic();
                srvcinstancechar.name = "BTS_APP_CODE";
                srvcinstancechar.action = ACTION_DELETE;

                listserivceinstancechars.Add(srvcinstancechar);
            }
            if (!string.IsNullOrEmpty(thomasParams.Super))
            {
                srvcinstancechar = new ClientServiceInstanceCharacteristic();
                srvcinstancechar.name = "SUPER";
                srvcinstancechar.action = ACTION_DELETE;

                listserivceinstancechars.Add(srvcinstancechar);
            }

            if (Convert.ToBoolean(ConfigurationManager.AppSettings["isDnPMigrationforHDRCompleted"]))
            {
                if (!string.IsNullOrEmpty(thomasParams.HDR))
                {
                    srvcinstancechar = new ClientServiceInstanceCharacteristic();
                    srvcinstancechar.name = "HDR";
                    srvcinstancechar.action = ACTION_DELETE;

                    listserivceinstancechars.Add(srvcinstancechar);
                }
            }

            return listserivceinstancechars;
        }

        public void MCPCalltoUpdateThomasServiceState(ThomasParameters thomasParams, MSEOOrderNotification notification, short retryCount, ref E2ETransaction e2eData)
        {
            manageClientProfileV1Request1 request1 = new manageClientProfileV1Request1();
            request1.manageClientProfileV1Request = new ManageClientProfileV1Request();
            manageClientProfileV1Response1 profileResponse1 = new manageClientProfileV1Response1();

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

            ManageClientProfileV1Req manageClientProfileV1Req1 = new ManageClientProfileV1Req();
            List<ClientServiceInstanceCharacteristic> clientSrvcInsCharsList = new List<ClientServiceInstanceCharacteristic>();
            ClientServiceInstanceCharacteristic clientSrvcInsChar = null;
            try
            {

                manageClientProfileV1Req1 = new ManageClientProfileV1Req();

                manageClientProfileV1Req1.clientProfileV1 = new ClientProfileV1();
                manageClientProfileV1Req1.clientProfileV1.client = new Client();
                manageClientProfileV1Req1.clientProfileV1.client.action = "UPDATE";

                ClientIdentity[] clientIdentity = new ClientIdentity[1];
                clientIdentity[0] = new ClientIdentity();
                clientIdentity[0].value = thomasParams.BillAccountNumber;
                clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                clientIdentity[0].action = "SEARCH";

                ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];

                serviceIdentity[0] = new ServiceIdentity();
                serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity[0].value = thomasParams.BillAccountNumber;
                serviceIdentity[0].action = "SEARCH";

                ClientServiceInstanceV1[] clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                clientServiceInstanceV1[0].action = "UPDATE";
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BTSKY_SERVICECODE_NAMEPACE;

                clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                clientServiceInstanceV1[0].clientServiceInstanceStatus.value = "ACTIVE";

                if (!string.IsNullOrEmpty(thomasParams.HDR))
                {
                    ClientServiceInstanceCharacteristic clientSrvcChar = new ClientServiceInstanceCharacteristic();
                    clientSrvcChar.value = thomasParams.HDR;
                    clientSrvcChar.name = "HDR";
                    if (thomasParams.OrderAction.Equals("Create", StringComparison.OrdinalIgnoreCase))
                        clientSrvcChar.action = "FORCE_INS";
                    else
                    {
                        if (thomasParams.IsHDRAdd)
                            clientSrvcChar.action = "FORCE_INS";
                        else if (thomasParams.IsHDRDelete)
                            clientSrvcChar.action = "DELETE";
                    }
                    if (thomasParams.IsHDRDelete && !Convert.ToBoolean(ConfigurationManager.AppSettings["isDnPMigrationforHDRCompleted"]))
                    {
                        //donot delete the HDR from serviceinstance chars.
                    }
                    else
                        clientSrvcInsCharsList.Add(clientSrvcChar);
                }

                if (!string.IsNullOrEmpty(thomasParams.BTS_APP_CODE))
                {
                    clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                    clientSrvcInsChar.value = thomasParams.BTS_APP_CODE;
                    clientSrvcInsChar.name = "BTS_APP_CODE";
                    clientSrvcInsChar.action = "FORCE_INS";
                    clientSrvcInsCharsList.Add(clientSrvcInsChar);
                }

                if (!string.IsNullOrEmpty(thomasParams.Scode))
                {
                    clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                    clientSrvcInsChar.value = thomasParams.Scode;
                    clientSrvcInsChar.name = "SCODE";
                    clientSrvcInsChar.action = "FORCE_INS";
                    clientSrvcInsCharsList.Add(clientSrvcInsChar);
                }

                if (!string.IsNullOrEmpty(thomasParams.ServiceID))
                {
                    clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                    clientSrvcInsChar.value = thomasParams.ServiceID;
                    clientSrvcInsChar.name = "BTAPP_SERVICE_ID";
                    clientSrvcInsChar.action = "FORCE_INS";
                    clientSrvcInsCharsList.Add(clientSrvcInsChar);
                }
                if (!string.IsNullOrEmpty(thomasParams.Super))
                {
                    clientSrvcInsChar = new ClientServiceInstanceCharacteristic();
                    clientSrvcInsChar.value = thomasParams.Super;
                    clientSrvcInsChar.name = "Super";
                    clientSrvcInsChar.action = "FORCE_INS";
                    clientSrvcInsCharsList.Add(clientSrvcInsChar);
                }

                clientServiceInstanceV1[0].clientServiceInstanceCharacteristic = clientSrvcInsCharsList.ToArray();

                ClientServiceRole[] clientServiceRole = new ClientServiceRole[1];
                clientServiceRole[0] = new ClientServiceRole();
                clientServiceRole[0].id = "ADMIN";
                clientServiceRole[0].clientServiceRoleStatus = new ClientServiceRoleStatus();
                clientServiceRole[0].clientServiceRoleStatus.value = "ACTIVE";

                clientServiceRole[0].clientIdentity = new ClientIdentity[1];
                clientServiceRole[0].clientIdentity[0] = new ClientIdentity();
                clientServiceRole[0].clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                clientServiceRole[0].clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                clientServiceRole[0].clientIdentity[0].value = thomasParams.BillAccountNumber;
                clientServiceRole[0].clientIdentity[0].action = "SEARCH";
                clientServiceRole[0].action = "UPDATE";                

                manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;
                manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1[0].clientServiceRole = clientServiceRole;
                manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentity;
                manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                request1.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;
                request1.manageClientProfileV1Request.standardHeader = headerBlock;

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, BTSKY_SERVICECODE_NAMEPACE);
                request1.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                request1.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                Logger.Write(thomasParams.orderKey + "," + MakingDnPcall + "," + ModifyProfileAHTDone, Logger.TypeEnum.MessageTrace);
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, thomasParams.orderKey, request1.SerializeObject(), ConfigurationManager.AppSettings["BTSportsProductCode"].ToString());
                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPcall, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey, request1.SerializeObject());

                profileResponse1 = DnpWrapper.manageClientProfileV1Thomas(request1, thomasParams.orderKey);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey, profileResponse1.SerializeObject());
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, thomasParams.orderKey, profileResponse1.SerializeObject(), ConfigurationManager.AppSettings["BTSportsProductCode"].ToString());
                Logger.Write(thomasParams.orderKey + "," + GotResponseFromDnP + "," + GotResponsefromDnPModifyProfileAHTDone, Logger.TypeEnum.MessageTrace);

                if (profileResponse1 != null
                   && profileResponse1.manageClientProfileV1Response != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    //sending completed notification
                    //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA;
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    // need confirmation about flase or true
                    if (thomasParams.OrderAction.ToLower().Equals("create") && !string.IsNullOrEmpty(thomasParams.BillAccountNumber) && ConfigurationManager.AppSettings["BTsportsdelegation"].Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        ClientServiceInstanceV1[] gsiResponse = null;
                        List<string> SportsDelegateDNslist = new List<string>();

                        gsiResponse = DnpWrapper.getServiceInstanceV1(thomasParams.BillAccountNumber, BACID_IDENTIFER_NAMEPACE, string.Empty);
                        if (gsiResponse != null)
                        {
                            if (IsDelegateRoleExists(gsiResponse, ref SportsDelegateDNslist))
                            {
                                CreateSportsDelegateRoles(thomasParams, notification, ref e2eData, SportsDelegateDNslist);
                            }
                            else
                            {
                                notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                                Logger.Write(thomasParams.orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.MessageTrace);
                                LogHelper.LogActivityDetails(bptmTxnId, guid, CompletedNotificationSent, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                            }
                        }

                    }
                    else
                    {
                        notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                        Logger.Write(thomasParams.orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.MessageTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, CompletedNotificationSent, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                    }
                }
                else
                {
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedErrorMessage + errorMessage, Logger.TypeEnum.ExceptionTrace);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + FailedErrorMessage + errorMessage, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);
                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + DnPAdminstratorFailedResponse + errorMessage, Logger.TypeEnum.MessageTrace);
                            if (retryCount++ < System.Int32.Parse(ConfigurationManager.AppSettings["RetryCountForThomasProvide"]))
                            {
                                if (retryCount == System.Int32.Parse(ConfigurationManager.AppSettings["RetryCountForThomasProvide"]))
                                {
                                    Logger.Write(thomasParams.orderKey + "," + Failed + "," + NonFunctionalException + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.ExceptionTrace);
                                    //Logger.Write("Non Functional Exception from DNP, reached max number of Retry , we are shutdowning service" + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.EmailTrace);
                                    //Process.GetCurrentProcess().Kill();

                                    //notification.NotificationResponse.StandardHeader.E2e.E2EDATA = profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA;
                                    e2eData.logMessage(GotResponseFromDnP, "");
                                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                                    else
                                        e2eData.endOutboundCall(e2eData.toString());

                                    notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                                    Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                                }
                                else
                                {
                                    Thread.Sleep(System.Int32.Parse(ConfigurationManager.AppSettings["SleepTimeoutInMilliSec"]));
                                    ModifyProfileAhtDone(thomasParams, notification, retryCount, ref e2eData);
                                }
                            }
                        }
                        else
                        {
                            //sending failure notification
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailureResponseFromDnP + errorMessage, Logger.TypeEnum.MessageTrace);
                            e2eData.logMessage(GotResponseFromDnP, "");
                            if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                                e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                            else
                                e2eData.endOutboundCall(e2eData.toString());
                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                            Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                        }
                    }
                    else
                    {
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.MessageTrace);

                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + FailedErrorMessage + NullResponseFromDnP, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);

                        //sending failure notification
                        e2eData.logMessage(GotResponseFromDnP, "");
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                        Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailureResponseFromDnP + ex.Message, Logger.TypeEnum.MessageTrace);

                LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "," + FailureResponseFromDnP + ex.Message, System.DateTime.Now - ActivityStartTime, "ThomasTrace", thomasParams.orderKey);

                //sending failure notification
                e2eData.logMessage(GotResponseFromDnP, "");
                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());
                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true, ActivityStartTime, guid, bptmTxnId);
                Logger.Write(thomasParams.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.MessageTrace);
            }
            finally
            {
                clientSrvcInsCharsList = null;
            }
        }            
    }

    public class ThomasParameters
    {
        private string orderkey = string.Empty;

        public string orderKey
        {
            get { return orderkey; }
            set { orderkey = value; }
        }

        private string billAccountNumber = string.Empty;

        public string BillAccountNumber
        {
            get { return billAccountNumber; }
            set { billAccountNumber = value; }
        }

        private string customerID = string.Empty;

        public string CustomerID
        {
            get { return customerID; }
            set { customerID = value; }
        }

        private string serviceID = string.Empty;

        public string ServiceID
        {
            get { return serviceID; }
            set { serviceID = value; }
        }

        private string scode = string.Empty;

        public string Scode
        {
            get { return scode; }
            set { scode = value; }
        }

        private string bts_app_code = string.Empty;

        public string BTS_APP_CODE
        {
            get { return bts_app_code; }
            set { bts_app_code = value; }
        }

        private string previousBTS_APP_CODE = string.Empty;

        public string PreviousBTS_APP_CODE
        {
            get { return previousBTS_APP_CODE; }
            set { previousBTS_APP_CODE = value; }
        }

        private string btoneid = string.Empty;

        public string BtOneId
        {
            get { return btoneid; }
            set { btoneid = value; }
        }

        private string serviceInstanceStatus = string.Empty;

        public string ServiceInstanceStatus
        {
            get { return serviceInstanceStatus; }
            set { serviceInstanceStatus = value; }
        }

        private string identity = string.Empty;

        public string Identity
        {
            get { return identity; }
            set { identity = value; }
        }

        private string identityDomain = string.Empty;

        public string IdentityDomain
        {
            get { return identityDomain; }
            set { identityDomain = value; }
        }

        //private string action = string.Empty;

        //public string Action
        //{
        //    get { return action; }
        //    set { action = value; }
        //}

        //string e2eData = string.Empty;

        private bool serviceInstanceExists = false;

        public bool ServiceInstanceExists
        {
            get { return serviceInstanceExists; }
            set { serviceInstanceExists = value; }
        }

        private bool isScodeExist = false;

        public bool IsScodeExist
        {
            get { return isScodeExist; }
            set { isScodeExist = value; }
        }

        private bool isBTS_APP_CODEExistinDnP = false;

        public bool IsBTS_APP_CODEExistinDnP
        {
            get { return isBTS_APP_CODEExistinDnP; }
            set { isBTS_APP_CODEExistinDnP = value; }
        }

        private bool isBTS_APP_CODEAttrExist = false;

        public bool IsBTS_APP_CODEAttrExist
        {
            get { return isBTS_APP_CODEAttrExist; }
            set { isBTS_APP_CODEAttrExist = value; }
        }

        private string orderAction = string.Empty;
        public string OrderAction
        {
            get { return orderAction; }
            set { orderAction = value; }
        }

        private string hdr = string.Empty;
        public string HDR
        {
            get { return hdr; }
            set { hdr = value; }
        }

        private string hdr_action = string.Empty;
        public string HDR_Action
        {
            get { return hdr_action; }
            set { hdr_action = value; }
        }

        private bool isHDRAdd = false;

        public bool IsHDRAdd
        {
            get { return isHDRAdd; }
            set { isHDRAdd = value; }
        }

        private bool isHDRDelete = false;

        public bool IsHDRDelete
        {
            get { return isHDRDelete; }
            set { isHDRDelete = value; }
        }

        private bool isAdminRoleexistsonCAK = false;
        public bool IsAdminRoleexistsonCAK
        {
            get { return isAdminRoleexistsonCAK; }
            set { isAdminRoleexistsonCAK = value; }
        }

        private bool isOffnetflow = false;
        public bool IsOffnetflow
        {
            get { return isOffnetflow; }
            set { isOffnetflow = value; }
        }

        private string super = string.Empty;
        public string Super
        {
            get { return super; }
            set { super = value; }
        }

        private string reason = string.Empty;        
        public string Reason
        {
            get { return reason; }
            set { reason = value; }
        }

        private bool isAdminRoleStatusChangeRequired = false;
        public bool IsAdminRoleStatusChangeRequired
        {
            get { return isAdminRoleStatusChangeRequired; }
            set { isAdminRoleStatusChangeRequired = value; }
        }

    }
}
