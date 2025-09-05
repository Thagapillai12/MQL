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
using System.Net;
using System.Threading.Tasks;
using System.Data;
namespace BT.SaaS.MSEOAdapter
{
    public class BTCONSUMERBROADBANDProcessor
    {
        #region Constants
        const string BACID_IDENTIFER_NAMEPACE = "VAS_BILLINGACCOUNT_ID";
        const string BTCOM = "BTCOM";
        //NAYANAGL-70258 fix
        const string EEONLINEID = "EEONLINEID";
        const string CONKID = "CONKID";

        
        const string BT_CONSUMER_BROADBAND_NAMEPACE = "BT_CONSUMER_BROADBAND";
        const string MOBILE_NAMESPACE = "MOBILE";
        const string RBSID_NAMESPACE = "RBSID";
        const string BBSID_NAMESPACE = "BBSID";

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
        private const string INACTIVE = "INACTIVE";
        private const string DEBTSUSPEND = "DEBTSUSPEND"; //CCP61
        private const string ADMIN_ROLE = "ADMIN";
        private const string action_forceinsert = "FORCE_INS";


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
        const string GotResponsefromDNP = "Got Response from DNP";
        #endregion

        System.DateTime ActivityStartTime = System.DateTime.Now;
        Guid guid = new Guid(); string bptmTxnId = string.Empty;
        bool isDefaultRoleExists = false;
        BT.Helpers.RESTHelper restHelpers = new Helpers.RESTHelper();
        public int MAX_RETRY_COUNT = Convert.ToInt32(ConfigurationManager.AppSettings["RetryCountForThomasProvide"].ToString());
        public void BTCONSUMERBROADBANDReqeustMapper(OrderRequest requestOrderRequest, Guid btguid, System.DateTime ActivityStartedTime, ref E2ETransaction e2eData)
        {
           
            LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "Inside BTCONSUMERBROADBANDReqeustMapper: " + e2eData.toString(), TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", "");
            restHelpers.WriteToFile("Inside BTCONSUMERBROADBANDReqeustMapper");
            MSEOOrderNotification notification = null;
            bool isServiceAlreadyExist = false;           
            string action = string.Empty;
            string reason = string.Empty;
            guid = btguid;
            this.ActivityStartTime = ActivityStartedTime;
            string dnpBBSID = string.Empty;
            BroadBandParameters paramters = new BroadBandParameters();
           

            try
            {
                paramters.orderKey = requestOrderRequest.Order.OrderIdentifier.Value;
                notification = new MSEOOrderNotification(requestOrderRequest);
                this.bptmTxnId = BPTMHelper.GetTxnIdFromE2eData(e2eData.toString());

                if (requestOrderRequest != null && requestOrderRequest.StandardHeader != null && requestOrderRequest.StandardHeader.ServiceAddressing != null && requestOrderRequest.StandardHeader.ServiceAddressing.From.Contains("queue"))
                    paramters.IsOFSOrder = true;    

                if (requestOrderRequest.Order.OrderItem[0].Action != null && requestOrderRequest.Order.OrderItem[0].Action.Code != null)
                {
                    action = requestOrderRequest.Order.OrderItem[0].Action.Code.ToString();
                    if (requestOrderRequest.Order.OrderItem[0].Action.Reason != null)
                    {
                        reason = requestOrderRequest.Order.OrderItem[0].Action.Reason.ToString();
                    }
                }
                restHelpers.WriteToFile("Before MapRequestParameters");

                MapRequestParameters(requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic, ref paramters);
              
                if (!String.IsNullOrEmpty(paramters.Billingaccountnumber))
                {
                    GetClientProfileV1Res bacProfileResponse = null;
                    ClientServiceInstanceV1[] bacServiceResponse = null;
                    ClientServiceInstanceV1[] bacgsiResponse = null;
                    ClientServiceInstanceV1 gsisrvcInstance = null;
                    ClientServiceInstanceV1 srvcInstance = null;
                    GetBatchProfileV1Res response1 = null;
                    LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "Before DnpWrapper.GetClientProfileV1ForThomas: " + e2eData.toString(), TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                    restHelpers.WriteToFile("Before DnpWrapper.GetClientProfileV1ForThomas"+ paramters.orderKey);
                    bacProfileResponse = DnpWrapper.GetClientProfileV1ForThomas(paramters.Billingaccountnumber, BACID_IDENTIFER_NAMEPACE, paramters.orderKey);
                    restHelpers.WriteToFile("After DnpWrapper.GetClientProfileV1ForThomas");
                    //Newly writt
                   // var bbsid = bacProfileResponse.clientProfileV1.clientServiceInstanceV1.ToList().Exists(CSI => CSI.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE) && CSI.clientServiceRole.ToList().Exists(si => si.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BBSID"))).FirstOrDefault().value;


                    if (bacProfileResponse != null && bacProfileResponse.clientProfileV1 != null && bacProfileResponse.clientProfileV1.client != null && bacProfileResponse.clientProfileV1.client.clientIdentity != null)
                    {
                        paramters.IsExixstingAccount = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(paramters.Billingaccountnumber, StringComparison.OrdinalIgnoreCase));
                        if (paramters.IsExixstingAccount)
                        {
                            BT.SaaS.IspssAdapter.Dnp.ClientIdentity bacClientIdentity = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(paramters.Billingaccountnumber, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                            if (bacClientIdentity.clientIdentityValidation != null)
                                paramters.IsAHTDone = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                        }
                        if (bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(i => i.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                        {
                            paramters.BtOneId = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                        }
                        //ACCEL-6666
                        if (bacProfileResponse.clientProfileV1.clientServiceInstanceV1.ToList().Exists(CSI => CSI.clientServiceInstanceIdentifier.value.Equals("MANAGE_ACCOUNT") && CSI.clientServiceInstanceCharacteristic != null && CSI.clientServiceInstanceCharacteristic.ToList().Exists(csic => csic.name.Equals("BACORGANISATIONNAME", StringComparison.OrdinalIgnoreCase) && csic.value.StartsWith("EE", StringComparison.OrdinalIgnoreCase))))
                        {
                            paramters.isEEAccount = true;
                           
                        }
                        else
                        {
                            if (paramters.BtOneId != null)
                            {
                                paramters.isEEAccount = false;
                            }
                            else
                            {
                                paramters.isEEAccount = true;
                            }
                        }
                        if (bacProfileResponse.clientProfileV1.client.clientIdentity != null && bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("EEONLINEID", StringComparison.OrdinalIgnoreCase)))
                        {
                            paramters.EEOnlineId = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("EEONLINEID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                        }
                        else if (bacProfileResponse.clientProfileV1.client.clientIdentity != null && bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("CONKID", StringComparison.OrdinalIgnoreCase)))
                        {
                            paramters.ConKid = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("CONKID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                        }
                        //Mobile case We are checking client Service Role= Default Role is exists or not

                        if  (bacProfileResponse.clientProfileV1.clientServiceInstanceV1 != null && bacProfileResponse.clientProfileV1.clientServiceInstanceV1.ToList().Exists(CSI => CSI.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE) && CSI.serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && si.value.Equals(paramters.Billingaccountnumber, StringComparison.OrdinalIgnoreCase))))
                        {
                            if (bacProfileResponse.clientProfileV1.clientServiceInstanceV1.ToList().Where(CSI => CSI.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE)).FirstOrDefault().clientServiceRole.ToList().Exists(cr => cr.id.Equals(DEFAULT_ROLE, StringComparison.OrdinalIgnoreCase)))
                            {
                                isDefaultRoleExists = true;
                            }
                        }
                        ////Newly Implemented for getting BBSID VALUE from DNP
                        //foreach (ClientServiceInstanceV1 csi in bacProfileResponse.clientProfileV1.clientServiceInstanceV1.ToArray())
                        //{
                        //    if (csi.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase))
                        //    {
                        //        foreach (ClientServiceRole role in csi.clientServiceRole.ToList())
                        //        {
                        //            //NAYANAGL-69135 fix
                        //            if (role != null && role.clientIdentity != null)
                        //            {
                        //                if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BBSID", StringComparison.OrdinalIgnoreCase)))
                        //                {
                        //                     getBBSIDfromDNP = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BBSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                        //                }
                        //            }
                        //        }
                        //    }
                        //}

                                //ACCEL-6666 end
                        if (bacProfileResponse.clientProfileV1.clientServiceInstanceV1 != null && bacProfileResponse.clientProfileV1.clientServiceInstanceV1.ToList().Exists(CSI => CSI.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE) && CSI.serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && si.value.Equals(paramters.Billingaccountnumber, StringComparison.OrdinalIgnoreCase))))
                        {
                            paramters.isServiceExixsting = true;
                            srvcInstance = bacProfileResponse.clientProfileV1.clientServiceInstanceV1.ToList().Where(CSI => CSI.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE)).FirstOrDefault();
                            //isDefaultRoleExists= bacProfileResponse.clientProfileV1.clientServiceInstanceV1.ToList().Where(CSI => CSI.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE)).FirstOrDefault().clientServiceRole.ToList().Exists(cr=>cr.id.Equals(DEFAULT_ROLE, StringComparison.OrdinalIgnoreCase));
                            paramters.serviceStatus = srvcInstance.clientServiceInstanceStatus.value.ToString();
                        }
                        else
                        {
                            //for AHT Delinked scenarios...
                            bacServiceResponse = DnpWrapper.getServiceInstanceV1(paramters.Billingaccountnumber, BACID_IDENTIFER_NAMEPACE, BT_CONSUMER_BROADBAND_NAMEPACE);

                            if ((bacServiceResponse != null) && (bacServiceResponse.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase))))
                            {
                                paramters.isServiceExixsting = true;

                                srvcInstance = bacServiceResponse.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                paramters.serviceStatus = srvcInstance.clientServiceInstanceStatus.value.ToString();

                                paramters.IsMobileExists = IsMOBILEIdentityExists(srvcInstance,ref paramters);

                                //Newly Implemented for getting BBSID VALUE from DNP
                               
                                        foreach (ClientServiceRole role in srvcInstance.clientServiceRole.ToList())
                                        {
                                            //NAYANAGL-69135 fix
                                            if (role != null && role.clientIdentity != null)
                                            {
                                                if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BBSID", StringComparison.OrdinalIgnoreCase)))
                                                {
                                            dnpBBSID = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BBSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                }
                                            }
                                        }
                                    
                                
                            }
                        }
                    }
                    else
                    {
                        restHelpers.WriteToFile("Before DnpWrapper.getServiceInstanceV1");
                        bacServiceResponse = DnpWrapper.getServiceInstanceV1(paramters.Billingaccountnumber, BACID_IDENTIFER_NAMEPACE, BT_CONSUMER_BROADBAND_NAMEPACE);
                        restHelpers.WriteToFile("After DnpWrapper.getServiceInstanceV1");
                        if ((bacServiceResponse != null) && (bacServiceResponse.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase))))
                        {
                            paramters.isServiceExixsting = true;

                            srvcInstance = bacServiceResponse.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                            paramters.serviceStatus = srvcInstance.clientServiceInstanceStatus.value.ToString();
                            paramters.IsMobileExists = IsMOBILEIdentityExists(srvcInstance, ref paramters);
                        }
                    }


                    //Newly implemented for BBSID value from DNP
                    bacgsiResponse = DnpWrapper.getServiceInstanceV1(paramters.Billingaccountnumber, BACID_IDENTIFER_NAMEPACE, BT_CONSUMER_BROADBAND_NAMEPACE);

                    if ((bacgsiResponse != null) && (bacgsiResponse.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase))))
                    {
                        gsisrvcInstance = bacgsiResponse.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                        //Newly Implemented for getting BBSID VALUE from DNP
                        foreach (ClientServiceRole role in srvcInstance.clientServiceRole.ToList())
                        {
                            //NAYANAGL-69135 fix
                            if (role != null && role.clientIdentity != null)
                            {
                                if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BBSID", StringComparison.OrdinalIgnoreCase)))
                                {
                                    dnpBBSID = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BBSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                }
                            }
                        }

                    }

                    if (IsCHOPRegrade(requestOrderRequest))
                    {
                        if (paramters.isServiceExixsting)
                        {
                            GetRBSIDforRegrade(requestOrderRequest, ref paramters);

                            if (!string.IsNullOrEmpty(paramters.NewRBSID))
                                MapCHOPRegradeRequest(paramters, notification, ref e2eData);
                           
                            if (string.IsNullOrEmpty(paramters.NewRBSID) && !string.IsNullOrEmpty(paramters.Hybrid_Tag))
                            {
                                notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false);
                                LogHelper.LogActivityDetails(bptmTxnId, guid, Accepted, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);

                                UpdateHybridTagtoCHOPServiceStatus(paramters, notification, string.Empty, ref e2eData);
                            }
                            if (string.IsNullOrEmpty(paramters.NewRBSID) && string.IsNullOrEmpty(paramters.Hybrid_Tag))
                            {
                                notification.sendNotification(false, false, "001", "The new RBSID value not received in the reqeust to update in CHO profile", ref e2eData, true);
                                LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + " The new RBSID value not received in the reqeust to update in CHO profile", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                            }
                        }
                        else
                        {
                            notification.sendNotification(false, false, "001", "The given bac doesn't have BTConsumerBroadBand Service to regrade", ref e2eData, true);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "The given bac doesn't have BTConsumerBroadBand Service to regrade", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                        }
                    }

                    else if (action.Equals("create", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!paramters.isServiceExixsting)
                        {
                            //sending accepted notification                         
                            notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false);
                            
                            LogHelper.LogActivityDetails(bptmTxnId, guid, Accepted, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                           
                            CreateBTCONSUMERBROADBANDService(paramters, notification, ref e2eData, requestOrderRequest, ACTIVE);
                        }
                        else if (paramters.isServiceExixsting && !paramters.IsMobileExists)
                        {
                            notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, Accepted, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                           
                                InsertMobileTOCHOService(paramters, notification, ref e2eData);
                        }
                        else
                        {
                            notification.sendNotification(false, false, "001", "The given bac already have BTConsumerBroadBand Service", ref e2eData, true);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + " The given bac already have BTConsumerBroadBand Service", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                        }

                    }                    
                    //CCP61
                    else if (action.Equals("reactivate", StringComparison.OrdinalIgnoreCase))
                    {
                        if (bacProfileResponse == null)
                        {
                            response1 = DnpWrapper.GetServiceUserProfilesV1ForDante(paramters.Billingaccountnumber, BACID_IDENTIFER_NAMEPACE);

                            if(response1 != null && response1.clientProfileV1 != null && response1.clientProfileV1.Count() > 0)
                            {
                                foreach (ClientProfileV1 clientProfiles in response1.clientProfileV1)
                                {
                                    if (clientProfiles.clientServiceInstanceV1 != null)
                                    {
                                        if (clientProfiles.clientServiceInstanceV1.ToList().Exists(ip => ip.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            paramters.isServiceExixsting = true;
                                        }
                                    }
                                }
                            }
                           
                        }

                        if (paramters.isServiceExixsting && paramters.serviceStatus.Equals("debtsuspend", StringComparison.OrdinalIgnoreCase) && reason != null && reason.Equals("DebtResume", StringComparison.OrdinalIgnoreCase))
                        {
                            if (paramters.IsOFSOrder)
                            {
                                notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false);
                                LogHelper.LogActivityDetails(bptmTxnId, guid, Accepted, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                            }

                            //ACCEL-6666
                            if (bacProfileResponse != null && bacProfileResponse.clientProfileV1 != null && bacProfileResponse.clientProfileV1.client != null && bacProfileResponse.clientProfileV1.client.clientIdentity != null)
                            {
                                foreach (ClientServiceInstanceV1 csi in bacProfileResponse.clientProfileV1.clientServiceInstanceV1.ToArray())
                                {
                                    if (csi.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase))
                                    {
                                        foreach (ClientServiceRole role in csi.clientServiceRole.ToList())
                                        {
                                            //NAYANAGL-69135 fix
                                            if (role != null && role.clientIdentity != null)
                                            {
                                                if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BBSID", StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    paramters.isBBSIDExist = true;
                                                  string bbsid=  role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BBSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                    break;
                                                }
                                            }
                                        }

                                        if (!paramters.isBBSIDExist)
                                        {
                                            if (!string.IsNullOrEmpty(paramters.EEOnlineId) && paramters.isEEAccount)
                                            {
                                                IsClientIdentityExists(paramters.BBSID, BBSID_NAMESPACE, requestOrderRequest, paramters.EEOnlineId, "EEONLINEID", ref e2eData);
                                            }
                                            else if (!string.IsNullOrEmpty(paramters.ConKid) && paramters.isEEAccount)
                                            {
                                                IsClientIdentityExists(paramters.BBSID, BBSID_NAMESPACE, requestOrderRequest, paramters.ConKid, "CONKID", ref e2eData);
                                            }
                                            else if (!string.IsNullOrEmpty(paramters.BtOneId) && !paramters.isEEAccount)
                                            {
                                                IsClientIdentityExists(paramters.BBSID, BBSID_NAMESPACE, requestOrderRequest, paramters.BtOneId, BTCOM , ref e2eData);
                                            }
                                            InsertBBSIDRole(paramters, ref e2eData);
                                        }
                                    }
                                }

                            }
                            else if (response1 != null && response1.clientProfileV1 != null && response1.clientProfileV1.Count() > 0)
                            {
                                foreach (ClientProfileV1 clientProfile in response1.clientProfileV1)
                                {
                                    if (clientProfile.clientServiceInstanceV1 != null)
                                    {
                                        if (clientProfile.clientServiceInstanceV1.ToList().Exists(ip => ip.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            if (clientProfile.clientServiceInstanceV1 != null && clientProfile.clientServiceInstanceV1.ToList().Exists(ip => ip.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase) &&
                                                 ip.clientServiceRole.ToList().Exists(csr => csr.id.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase) && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BBSID", StringComparison.OrdinalIgnoreCase)))))
                                            {
                                                paramters.isBBSIDExist = true;
                                            }
                                            if (clientProfile.client.clientIdentity.ToList().Exists(ic => ic.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                paramters.BtOneId = clientProfile.client.clientIdentity.ToList().Where(ic => ic.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                            }
                                            else if (clientProfile.client.clientIdentity.ToList().Exists(ic => ic.managedIdentifierDomain.value.Equals("EEONLINEID", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                paramters.EEOnlineId = clientProfile.client.clientIdentity.ToList().Where(ic => ic.managedIdentifierDomain.value.Equals("EEONLINEID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                            }
                                            else if (clientProfile.client.clientIdentity.ToList().Exists(ic => ic.managedIdentifierDomain.value.Equals("CONKID", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                paramters.ConKid = clientProfile.client.clientIdentity.ToList().Where(ic => ic.managedIdentifierDomain.value.Equals("CONKID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                            }
                                            if (!paramters.isBBSIDExist)
                                            {
                                                if (!string.IsNullOrEmpty(paramters.EEOnlineId))
                                                {
                                                    IsClientIdentityExists(paramters.BBSID, BBSID_NAMESPACE, requestOrderRequest, paramters.EEOnlineId, "EEONLINEID", ref e2eData);
                                                }
                                                else if (!string.IsNullOrEmpty(paramters.ConKid))
                                                {
                                                    IsClientIdentityExists(paramters.BBSID, BBSID_NAMESPACE, requestOrderRequest, paramters.ConKid, "CONKID", ref e2eData);
                                                }
                                                else if (!string.IsNullOrEmpty(paramters.BtOneId))
                                                {
                                                    IsClientIdentityExists(paramters.BBSID, BBSID_NAMESPACE, requestOrderRequest, paramters.BtOneId, BTCOM, ref e2eData);
                                                }
                                                InsertBBSIDRole(paramters, ref e2eData);
                                            }

                                        }
                                    }
                                }
                            }
                            else
                            {
                                srvcInstance = bacServiceResponse.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                if (srvcInstance != null)
                                {
                                    if (srvcInstance.clientServiceRole != null)
                                    {
                                        if (srvcInstance.clientServiceRole.ToList().Exists(si => si.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BBSID"))))
                                        {
                                            paramters.isBBSIDExist = true;
                                        }
                                    }
                                    if (!paramters.isBBSIDExist)
                                    {
                                        InsertBBSIDRole(paramters, ref e2eData);
                                    }
                                }
                            }
                            //modified CON-73882 - start
                            ModifyBTCONSUMERBROADBANDServiceStatus(paramters, notification, ACTIVE, ref e2eData,requestOrderRequest,true,reason, dnpBBSID);
                            //modified CON-73882 - end
                        }
                        else if (reason.Equals("DebtResume", StringComparison.OrdinalIgnoreCase)&&!paramters.isServiceExixsting)
                        {
                            CreateBTCONSUMERBROADBANDService(paramters, notification, ref e2eData, requestOrderRequest, ACTIVE, true,reason, dnpBBSID);
                        }
                        else
                        {
                            notification.sendNotification(false, false, "001", "The given bac doesn't have BTConsumerBroadBand Service to debtresume or service is not in debtsuspend state", ref e2eData, true);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "The given bac doesn't have BTConsumerBroadBand Service to debtresume or service is not in debtsuspend state", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                        }

                    }
                    //CCP61
                    else if (action.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                    {
                        if(bacProfileResponse == null)
                        {
                            response1 = DnpWrapper.GetServiceUserProfilesV1ForDante(paramters.Billingaccountnumber, BACID_IDENTIFER_NAMEPACE);

                            if (response1 != null && response1.clientProfileV1 != null && response1.clientProfileV1.Count() > 0)
                            {
                                foreach (ClientProfileV1 clientProfiles in response1.clientProfileV1)
                                {
                                    if (clientProfiles.clientServiceInstanceV1 != null)
                                    {
                                        if (clientProfiles.clientServiceInstanceV1.ToList().Exists(ip => ip.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            paramters.isServiceExixsting = true;
                                        }
                                    }
                                }
                            }
                        }
                        if (paramters.isServiceExixsting)
                        {
                            //sending accepted notification 
                            notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, Accepted, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                            if (reason != null && reason.Equals("suspend", StringComparison.OrdinalIgnoreCase))
                            {                               
                                //ACCEL-6666
                                if (bacProfileResponse != null && bacProfileResponse.clientProfileV1 != null && bacProfileResponse.clientProfileV1.client != null && bacProfileResponse.clientProfileV1.client.clientIdentity != null)
                                {
                                    foreach (ClientServiceInstanceV1 csi in bacProfileResponse.clientProfileV1.clientServiceInstanceV1.ToArray())
                                    {
                                        if (csi.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase))
                                        {
                                            foreach (ClientServiceRole role in csi.clientServiceRole.ToList())
                                            {
                                                //NAYANAGL-69135 fix
                                                if (role != null && role.clientIdentity != null)
                                                {
                                                    if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BBSID", StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        paramters.isBBSIDExist = true;
                                                        break;
                                                    }
                                                }
                                            }

                                            if (!paramters.isBBSIDExist)
                                            {
                                                if (!string.IsNullOrEmpty(paramters.EEOnlineId) && paramters.isEEAccount)
                                                {
                                                    IsClientIdentityExists(paramters.BBSID, BBSID_NAMESPACE, requestOrderRequest, paramters.EEOnlineId, "EEONLINEID", ref e2eData);
                                                }
                                                else if (!string.IsNullOrEmpty(paramters.ConKid) && paramters.isEEAccount)
                                                {
                                                    IsClientIdentityExists(paramters.BBSID, BBSID_NAMESPACE, requestOrderRequest, paramters.ConKid, "CONKID", ref e2eData);
                                                }
                                                else if (!string.IsNullOrEmpty(paramters.BtOneId) && !paramters.isEEAccount)
                                                {
                                                    IsClientIdentityExists(paramters.BBSID, BBSID_NAMESPACE, requestOrderRequest, paramters.BtOneId, BTCOM , ref e2eData);
                                                }
                                                InsertBBSIDRole(paramters, ref e2eData);
                                            }
                                        }
                                    }

                                }
                                else if (response1 != null && response1.clientProfileV1 != null && response1.clientProfileV1.Count() > 0)
                                {
                                    foreach (ClientProfileV1 clientProfile in response1.clientProfileV1)
                                    {
                                        if (clientProfile.clientServiceInstanceV1 != null)
                                        {
                                            if (clientProfile.clientServiceInstanceV1.ToList().Exists(ip => ip.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                            {
                                                if (clientProfile.clientServiceInstanceV1 != null && clientProfile.clientServiceInstanceV1.ToList().Exists(ip => ip.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase) &&
                                                     ip.clientServiceRole.ToList().Exists(csr => csr.id.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase) && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BBSID", StringComparison.OrdinalIgnoreCase)))))
                                                {
                                                    paramters.isBBSIDExist = true;
                                                }
                                                if (clientProfile.client.clientIdentity.ToList().Exists(ic => ic.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    paramters.BtOneId = clientProfile.client.clientIdentity.ToList().Where(ic => ic.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;                                                   
                                                }                    
                                                else  if (clientProfile.client.clientIdentity.ToList().Exists(ic => ic.managedIdentifierDomain.value.Equals("EEONLINEID", StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    paramters.EEOnlineId = clientProfile.client.clientIdentity.ToList().Where(ic => ic.managedIdentifierDomain.value.Equals("EEONLINEID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                }
                                                else if (clientProfile.client.clientIdentity.ToList().Exists(ic => ic.managedIdentifierDomain.value.Equals("CONKID", StringComparison.OrdinalIgnoreCase)))
                                                {
                                                     paramters.ConKid = clientProfile.client.clientIdentity.ToList().Where(ic => ic.managedIdentifierDomain.value.Equals("CONKID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                }                                                
                                                if (!paramters.isBBSIDExist)
                                                {
                                                    if (!string.IsNullOrEmpty(paramters.EEOnlineId))
                                                    {
                                                        IsClientIdentityExists(paramters.BBSID, BBSID_NAMESPACE, requestOrderRequest, paramters.EEOnlineId, "EEONLINEID", ref e2eData);
                                                    }
                                                    else if (!string.IsNullOrEmpty(paramters.ConKid))
                                                    {
                                                        IsClientIdentityExists(paramters.BBSID, BBSID_NAMESPACE, requestOrderRequest, paramters.ConKid, "CONKID", ref e2eData);
                                                    }
                                                    else if (!string.IsNullOrEmpty(paramters.BtOneId))
                                                    {
                                                        IsClientIdentityExists(paramters.BBSID, BBSID_NAMESPACE, requestOrderRequest, paramters.BtOneId, BTCOM , ref e2eData);
                                                    }
                                                    InsertBBSIDRole(paramters, ref e2eData);
                                                }

                                            }
                                        }
                                    }
                                }
                                else
                                {
                                        srvcInstance = bacServiceResponse.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                        if (srvcInstance != null)
                                        {
                                            if (srvcInstance.clientServiceRole != null)
                                            {
                                                if (srvcInstance.clientServiceRole.ToList().Exists(si => si.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BBSID"))))
                                                {
                                                    paramters.isBBSIDExist = true;
                                                }
                                            }
                                            if (!paramters.isBBSIDExist)
                                            {
                                                InsertBBSIDRole(paramters, ref e2eData);
                                            }
                                        }                                   
                                }
                                restHelpers.WriteToFile("Before  ModifyBTCONSUMERBROADBANDServiceStatus" );
                                //modified CON-73882 - start
                                ModifyBTCONSUMERBROADBANDServiceStatus(paramters, notification, DEBTSUSPEND, ref e2eData,requestOrderRequest,true, reason);
                                //modified CON-73882 - end
                                restHelpers.WriteToFile("After  ModifyBTCONSUMERBROADBANDServiceStatus");
                            }
                            else
                            {
                                #region not in use
                                /*bacServiceResponse = DnpWrapper.getServiceInstanceV1(paramters.Billingaccountnumber, BACID_IDENTIFER_NAMEPACE, BT_CONSUMER_BROADBAND_NAMEPACE);
                                srvcInstance = bacServiceResponse.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                var srvroleidentities = srvcInstance.clientServiceRole.ToList();
                                Dictionary<string, string> _srvroleidentities = new Dictionary<string, string>();
                                foreach (var _clientIdentity in srvroleidentities)
                                {
                                    foreach (var item in _clientIdentity.clientIdentity) 
                                   {

                                        if (!item.managedIdentifierDomain.value.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.OrdinalIgnoreCase))
                                        {
                                            _srvroleidentities.Add(item.managedIdentifierDomain.value, item.value);
                                        }
                                    }
                                }
                                paramters.BBSID = _srvroleidentities.Keys.Contains("BBSID") ? _srvroleidentities["BBSID"] : string.Empty;
                                paramters.RBSID = _srvroleidentities.Keys.Contains("RBSID") ? _srvroleidentities["RBSID"] : string.Empty;
                                paramters.MSISDN = _srvroleidentities.Keys.Contains("MOBILE") ? _srvroleidentities["MOBILE"] : string.Empty;
                                bool _bbsidexist = false;
                                bool _rbsidexist = false;
                                bool _mobilexist = false;
                                bool _isbbsidclientdelete = false;
                                string identities = string.Join(",", _srvroleidentities.Values);
                                string identitydomains = string.Join(",", _srvroleidentities.Keys);
                                string url = string.Format("{0}/v2/domains/{1}/users/{2}/BatchProfile", ConfigurationManager.AppSettings["DnPBaseUrl"], identitydomains, identities);
                                string _e2edata = e2eData.toString();
                                string response = DnpRestCallWrapper.DoRESTCall("GET", url, ref _e2edata, true, new NetworkCredential { UserName = ConfigurationManager.AppSettings["DnPUser"], Password = ConfigurationManager.AppSettings["DnPPassword"] },null,"SAASMSEO");
                                if (!string.IsNullOrEmpty(response) && !response.Contains("RESTCALLFAILURE"))
                                {
                                    var batchtDetailsResponse = BT.Helpers.XmlHelper.DeserializeObject<DNP.Rest.getClientProfileV1Res>(response);
                                    foreach (var profile in batchtDetailsResponse.clientProfileV1)
                                    {
                                        if (profile != null && profile.client != null && profile.client.clientIdentity != null && profile.client.clientIdentity.Count() > 0 &&
                                            profile.client.clientIdentity.Count() == 1 && profile.client.clientIdentity.ToList().Exists(r => r.domain.Equals("BBSID", StringComparison.OrdinalIgnoreCase) && r.value.Equals(paramters.BBSID, StringComparison.OrdinalIgnoreCase)))
                                             _isbbsidclientdelete = true;                                        
                                        if ( !string.IsNullOrEmpty(paramters.BBSID) && profile!=null && profile.clientServiceInstanceV1!=null && profile.clientServiceInstanceV1.ToList().Exists(i => i.serviceCode != "BT_CONSUMER_BROADBOND" && i.serviceRole != null && i.serviceRole.Count() > 0 && i.serviceRole.ToList().Exists(j => j.clientIdentity != null && j.clientIdentity.Count() > 0 && j.clientIdentity.ToList().Exists(r => r.domain.Equals("BBSID", StringComparison.OrdinalIgnoreCase) && r.value.Equals(paramters.BBSID, StringComparison.OrdinalIgnoreCase)))))
                                            _bbsidexist = true;
                                        if(!string.IsNullOrEmpty(paramters.RBSID) && profile != null && profile.clientServiceInstanceV1 != null && profile.clientServiceInstanceV1.ToList().Exists(i => i.serviceCode != "BT_CONSUMER_BROADBOND" && i.serviceRole != null && i.serviceRole.Count() > 0 && i.serviceRole.ToList().Exists(j => j.clientIdentity != null && j.clientIdentity.Count() > 0 && j.clientIdentity.ToList().Exists(r => r.domain.Equals("RBSID", StringComparison.OrdinalIgnoreCase) && r.value.Equals(paramters.RBSID, StringComparison.OrdinalIgnoreCase)))))
                                            _rbsidexist = true;
                                        if (!string.IsNullOrEmpty(paramters.MSISDN) && profile != null && profile.clientServiceInstanceV1 != null && profile.clientServiceInstanceV1.ToList().Exists(i => i.serviceCode != "BT_CONSUMER_BROADBOND" && i.serviceRole != null && i.serviceRole.Count() > 0 && i.serviceRole.ToList().Exists(j => j.clientIdentity != null && j.clientIdentity.Count() > 0 && j.clientIdentity.ToList().Exists(r => r.domain.Equals("MOBILE", StringComparison.OrdinalIgnoreCase) && r.value.Equals(paramters.MSISDN, StringComparison.OrdinalIgnoreCase)))))
                                            _mobilexist = true;
                                    }
                                    DeleteCHOPService(paramters,notification, ref e2eData, _bbsidexist, _rbsidexist, _mobilexist, _isbbsidclientdelete);
                                }
                                */
                                #endregion
                                DeletenonAHTCHOPService(paramters, notification, ref e2eData);
                            }
                        }
                        else if (reason.Equals("suspend", StringComparison.OrdinalIgnoreCase) && !paramters.isServiceExixsting)
                        {
                            restHelpers.WriteToFile("action=cancel and reason.Equals=suspend and !paramters.isServiceExixsting");
                            notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, Accepted, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);

                            restHelpers.WriteToFile("Before CreateBTCONSUMERBROADBANDService ");
                            CreateBTCONSUMERBROADBANDService(paramters, notification, ref e2eData, requestOrderRequest, DEBTSUSPEND, true, reason);
                            restHelpers.WriteToFile("After CreateBTCONSUMERBROADBANDService ");
                        }
                        else
                        {
                            notification.sendNotification(false, false, "001", "The given bac doesn't have BTConsumerBroadBand Service to cease or service is not in active state", ref e2eData, true);
                            LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "The given bac doesn't have BTConsumerBroadBand Service to cease or service is not in active state", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                        }
                    }
                    else
                    {
                        notification.sendNotification(false, false, "001", "Bad Request from MQService", ref e2eData, true);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "Bad Request from MQService", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                    }
                }
                else
                {
                    notification.sendNotification(false, false, "001", "BAC is missing in the Request", ref e2eData, true);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "BAC is missing in the Request", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                }
            }

            catch (DnpException exception)
            {
                notification.sendNotification(false, false, "777", exception.Message, ref e2eData, true);
                LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + exception.Message, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                LogHelper.LogErrorMessage(exception.Message.ToString(), bptmTxnId, " in BTPlusMarkerReqeustMapper method with orderkey as " + paramters.orderKey);
            }
            catch (Exception ex)
            {
                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true);
                LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + ex.Message, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                LogHelper.LogErrorMessage(ex.Message.ToString(), bptmTxnId, " in BTPlusMarkerReqeustMapper method with orderkey as " + paramters.orderKey);
            }
        }

        public void ModifyBBSIDMapper(OrderRequest requestOrderRequest, ref E2ETransaction e2eData)
        {
            guid = Guid.NewGuid();            
            this.ActivityStartTime = System.DateTime.Now;

            BroadBandParameters paramters = new BroadBandParameters();

            ClientServiceInstanceV1[] bacServiceResponse = null;
           
            try
            {
                paramters.orderKey = requestOrderRequest.Order.OrderIdentifier.Value;

                this.bptmTxnId = BPTMHelper.GetTxnIdFromE2eData(e2eData.toString());

                MapRequestParameters(requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic, ref paramters);

                bacServiceResponse = DnpWrapper.getServiceInstanceV1(paramters.Billingaccountnumber, BACID_IDENTIFER_NAMEPACE, BT_CONSUMER_BROADBAND_NAMEPACE);

                if ((bacServiceResponse != null) && (bacServiceResponse.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase))))
                {
                    paramters.isServiceExixsting = true;

                    if (paramters.isServiceExixsting)
                    {
                        GetRBSIDforRegrade(requestOrderRequest, ref paramters);

                        if (!string.IsNullOrEmpty(paramters.NewBBSID))
                        {
                            MapCHOPRegradeRequestforBBSID(paramters, ref e2eData);
                        }
                        else
                        {
                            // log capture
                            LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "NEW BBSID not present in the request", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                        }

                    }
                    else
                    {
                        // log capture
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "BAC doesn't have chop service to update BBSID", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                    }
                }
                else
                {
                    // log capture
                    LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "BAC doesn't have chop service to update BBSID", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "Exception occured while updating BBSID "+ex.Message.ToString(), System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
            }
        }
        public void ModifyRBSIDMapperforCHOP(OrderRequest requestOrderRequest, ref E2ETransaction e2eData)
        {
            guid = Guid.NewGuid();
            this.ActivityStartTime = System.DateTime.Now;
            MSEOOrderNotification notification = null;

            BroadBandParameters paramters = new BroadBandParameters();

            ClientServiceInstanceV1[] bacServiceResponse = null;

            try
            {
                paramters.orderKey = requestOrderRequest.Order.OrderIdentifier.Value;

                this.bptmTxnId = BPTMHelper.GetTxnIdFromE2eData(e2eData.toString());

                MapRequestParameters(requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic, ref paramters);

                bacServiceResponse = DnpWrapper.getServiceInstanceV1(paramters.Billingaccountnumber, BACID_IDENTIFER_NAMEPACE, BT_CONSUMER_BROADBAND_NAMEPACE);

                if ((bacServiceResponse != null) && (bacServiceResponse.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase))))
                {
                    paramters.isServiceExixsting = true;

                    if (paramters.isServiceExixsting)
                    {
                        GetRBSIDforRegrade(requestOrderRequest, ref paramters);

                        if (!string.IsNullOrEmpty(paramters.NewRBSID))
                        {
                            MapCHOPRegradeRequest(paramters, notification, ref e2eData,true);
                        }
                        else
                        {
                            // log capture
                            LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "NEW RBSID not present in the request", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                        }
                    }
                    else
                    {
                        // log capture
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "BAC doesn't have chop service to update RBSID", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                    }
                }
                else
                {
                    // log capture
                    LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "BAC doesn't have chop service to update RBSID", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "Exception occured while updating RBSID " + ex.Message.ToString(), System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
            }
        }

        public void CreateBTCONSUMERBROADBANDService(BroadBandParameters paramters, MSEOOrderNotification notification, ref E2ETransaction e2eData, OrderRequest requestOrderRequest,string serviceStatus,bool isKillSessionRequired=false,string ActionReason="",string DNPBBSID="")
        {
            restHelpers.WriteToFile("inside CreateBTCONSUMERBROADBANDService");
            LogHelper.LogActivityDetails(bptmTxnId, guid, "inside CreateBTCONSUMERBROADBANDService() ", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey,"" );
            bool isRBSIDExists = false;
            bool isMsisdnExists = false;
			bool isBBSSIDExist=false;
            //for KillsessionFailures
            string actionRequest = string.Empty;
            string reason = string.Empty;
            LogHelper.LogActivityDetails(bptmTxnId, guid, "Before requestOrderRequest " , System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, "");
            if (requestOrderRequest != null && requestOrderRequest.Order.OrderItem[0].Action != null)
            {
                LogHelper.LogActivityDetails(bptmTxnId, guid, "inside requestOrderRequest", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, "");
                actionRequest = Convert.ToString(requestOrderRequest.Order.OrderItem[0].Action.Code);
                LogHelper.LogActivityDetails(bptmTxnId, guid, "actionRequest"+actionRequest, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, "");
                if (!string.IsNullOrEmpty(requestOrderRequest.Order.OrderItem[0].Action.Reason))
                {
                    reason = Convert.ToString(requestOrderRequest.Order.OrderItem[0].Action.Reason);
                    restHelpers.WriteToFile("inside CreateBTCONSUMERBROADBANDService Action:: "+actionRequest+"Reason:::"+reason);
                }
                    LogHelper.LogActivityDetails(bptmTxnId, guid, "reason is::" + reason, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, "");
            }


            LogHelper.LogActivityDetails(bptmTxnId, guid, "After requestOrderRequest", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, "");
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

                List<ClientIdentity> clientIdentityList = new List<ClientIdentity>();
                LogHelper.LogActivityDetails(bptmTxnId, guid, "inside CreateBTCONSUMERBROADBANDService()paramters.RBSID is :: "+ paramters.RBSID, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, "");
                if (!string.IsNullOrEmpty(paramters.RBSID))
                    if (IsClientIdentityExists(paramters.RBSID, RBSID_NAMESPACE))
                        isRBSIDExists = true;
                LogHelper.LogActivityDetails(bptmTxnId, guid, "inside CreateBTCONSUMERBROADBANDService()paramters.MSISDN is :: " + paramters.MSISDN, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, "");
                if (!string.IsNullOrEmpty(paramters.MSISDN))
                    if (IsClientIdentityExists(GetMSISDN(paramters.MSISDN), MOBILE_NAMESPACE))
                        isMsisdnExists = true;
                LogHelper.LogActivityDetails(bptmTxnId, guid, "paramters.BBSID is:: " + paramters.BBSID, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, "");

                if (!string.IsNullOrEmpty(paramters.BBSID))
                    if (IsClientIdentityExists(paramters.BBSID, BBSID_NAMESPACE))
                    {
                        isBBSSIDExist = true;
                        if (paramters.IsAHTDone || isRBSIDExists || isMsisdnExists)
                        {
                            if (paramters.IsAHTDone)
                            {
                                //ACCEL-6666 - introduced if condition alone
                                if (!paramters.isEEAccount)
                                {
                                    if (IsClientIdentityExists(paramters.BBSID, BBSID_NAMESPACE, requestOrderRequest, paramters.BtOneId, BTCOM, ref e2eData)) ;
                                }
                                //ACCEL-6666
                                else if (paramters.isEEAccount && !string.IsNullOrEmpty(paramters.EEOnlineId))
                                {
                                    if (IsClientIdentityExists(paramters.BBSID, BBSID_NAMESPACE, requestOrderRequest, paramters.EEOnlineId, "EEONLINEID", ref e2eData)) ;
                                }
                                else if (paramters.isEEAccount && !string.IsNullOrEmpty(paramters.ConKid))
                                {
                                    if (IsClientIdentityExists(paramters.BBSID, BBSID_NAMESPACE, requestOrderRequest, paramters.ConKid, "CONKID", ref e2eData)) ;
                                }
                            }
                            else if (isRBSIDExists)
                            {
                                if (IsClientIdentityExists(paramters.BBSID, BBSID_NAMESPACE, requestOrderRequest, paramters.RBSID, RBSID_NAMESPACE, ref e2eData)) ;
                            }
                            else if (isMsisdnExists)
                            {
                                if (IsClientIdentityExists(paramters.BBSID, BBSID_NAMESPACE, requestOrderRequest, GetMSISDN(paramters.MSISDN), MOBILE_NAMESPACE, ref e2eData)) ;
                            }
                        }
                    }

                ClientIdentity clientIdentity = null;
                if (paramters.IsAHTDone)
                {
                    manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_UPDATE;

                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                    clientIdentity.value = paramters.Billingaccountnumber;
                    clientIdentity.action = ACTION_SEARCH;

                    clientIdentityList.Add(clientIdentity);
                }                
                else
                {
                    if (isRBSIDExists || isMsisdnExists || isBBSSIDExist)
                        manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_UPDATE;               
                    else
                    {
                        manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_INSERT;

                        manageClientProfileV1Req1.clientProfileV1.client.clientStatus = new ClientStatus();
                        manageClientProfileV1Req1.clientProfileV1.client.clientStatus.value = ACTIVE;
                        manageClientProfileV1Req1.clientProfileV1.client.clientOrganisation = new ClientOrganisation();
                        manageClientProfileV1Req1.clientProfileV1.client.clientOrganisation.id = "BTRetailConsumer";
                        manageClientProfileV1Req1.clientProfileV1.client.type = "CUSTOMER";
                    }
                }
                //Here MSIDN is there taking as a search else Insert
                if (!string.IsNullOrEmpty(paramters.MSISDN))
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = MOBILE_NAMESPACE;                    
                    if (isMsisdnExists)
                    {
                        //is msisdn already exists in dnp no need to change msisdn value.
                        clientIdentity.value = GetMSISDN(paramters.MSISDN);
                        clientIdentity.action = ACTION_SEARCH;
                    }
                    else
                    {
                        clientIdentity.value = GetMSISDN(paramters.MSISDN);
                        clientIdentity.action = ACTION_INSERT;
                    }
                    clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity.clientIdentityStatus.value = ACTIVE;

                    //if it's AHT profile no need to search with MSiSDN so ignoring the CI
                    if (!(paramters.IsAHTDone && isMsisdnExists))
                        clientIdentityList.Add(clientIdentity);
                }
                //Here RBSID is there taking as a search else Insert
                if (!string.IsNullOrEmpty(paramters.RBSID))
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = RBSID_NAMESPACE;
                    clientIdentity.value = paramters.RBSID;
                    if (isRBSIDExists)
                        clientIdentity.action = ACTION_SEARCH;
                    else
                        clientIdentity.action = ACTION_INSERT;
                    clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity.clientIdentityStatus.value = ACTIVE;

                    //if it's AHT profile or msisdn already exists then no need to search with RBSID so ignoring the CI
                    if (!((paramters.IsAHTDone || isMsisdnExists) && isRBSIDExists))
                        clientIdentityList.Add(clientIdentity);
                }
                //Here BBSID is there taking as a search else Insert
                if (!string.IsNullOrEmpty(paramters.BBSID))
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BBSID_NAMESPACE;
                    clientIdentity.value = paramters.BBSID;
					if(isBBSSIDExist)
						clientIdentity.action = ACTION_SEARCH;
					else 
						clientIdentity.action = ACTION_INSERT;
                    clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity.clientIdentityStatus.value = ACTIVE;

					//if it's AHT profile or msisdn already exists or RBSID exists no need to search with BBSID so ignoring the CI
                    if (!((paramters.IsAHTDone || isMsisdnExists || isRBSIDExists) && isBBSSIDExist))
						clientIdentityList.Add(clientIdentity);
                }

                ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                serviceIdentity[0] = new ServiceIdentity();
                serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity[0].value = paramters.Billingaccountnumber;
                serviceIdentity[0].action = ACTION_LINK;

                ClientServiceInstanceV1[] clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BT_CONSUMER_BROADBAND_NAMEPACE;
                clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                clientServiceInstanceV1[0].clientServiceInstanceStatus.value = serviceStatus;
                clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                clientServiceInstanceV1[0].action = ACTION_INSERT;
                //LogHelper.LogActivityDetails(bptmTxnId, guid, "Hybrid_Tag " + paramters.Hybrid_Tag, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, "");
                if (!string.IsNullOrEmpty(paramters.Hybrid_Tag))
                {
                    ClientServiceInstanceCharacteristic srvcinstChars = null;
                    List<ClientServiceInstanceCharacteristic> listInstanceChars = new List<ClientServiceInstanceCharacteristic>();

                    srvcinstChars = new ClientServiceInstanceCharacteristic();
                    srvcinstChars.name = "Hybrid_Tag";
                    srvcinstChars.value = paramters.Hybrid_Tag;
                    srvcinstChars.action = "FORCE_INS";
                    listInstanceChars.Add(srvcinstChars);

                    clientServiceInstanceV1[0].clientServiceInstanceCharacteristic = listInstanceChars.ToArray();
                }
                LogHelper.LogActivityDetails(bptmTxnId, guid, "Before CRBB CreateServiceRoles()", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, "");
                //calling this method to get the service roles
                clientServiceInstanceV1[0].clientServiceRole = CreateServiceRoles(paramters, string.Empty,false,false,isRBSIDExists, isMsisdnExists).ToArray();
                LogHelper.LogActivityDetails(bptmTxnId, guid, "After CRBB CreateServiceRoles()", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, "");
                manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;

                profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;
                profileRequest.manageClientProfileV1Request.standardHeader = headerblock;

                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall+ "Before DNP call in CRBB:: ", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, profileRequest.SerializeObject());

                profileResponse1 = DnpWrapper.manageClientProfileV1Thomas(profileRequest, paramters.orderKey);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP+ "After DNP response in CRBB::", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, profileResponse1.SerializeObject());

                
                //DNP Success
                if (profileResponse1 != null
                   && profileResponse1.manageClientProfileV1Response != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    //Completed response
                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, Completed, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "After DNP suceess response CALL: " + "Order Type is" + actionRequest + "Reason is:" + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                    
                    //if (paramters.IsOFSOrder)
                    //{
                    //    //commented as part of CON-73882
                    //   // notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                    //    //LogHelper.LogActivityDetails(bptmTxnId, guid, Completed, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                    //}
                                     
                    //modified as part of CON-73882 start
                    bool IsKillSessionRequiredatMSEOend = Convert.ToBoolean(ConfigurationManager.AppSettings["IsKillSessionRequiredatMSEOend"].ToString());
                    string strApigeeKillsession = ConfigurationManager.AppSettings["APIGEE_KillSession"].ToString();
                    //Newly added for OFS not sending BBSID for debtresume cases those cases we are handling
                    if (ActionReason.Equals("DebtResume")) //Only for DebtResume cases
                    {
                        if (!string.IsNullOrEmpty(paramters.BBSID))
                        {
                            // paramters.BBSID as it is will take
                        }
                        else
                        {
                            paramters.BBSID = DNPBBSID.ToUpper();
                        }
                    }


                    LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "Before KillsessionActivity isKillSessionRequired: " + isKillSessionRequired + "Reason is:" + reason + "BBReasonIs::" + ActionReason + ": " + "IsKillSessionRequiredatMSEOend::"+ IsKillSessionRequiredatMSEOend+ "strApigeeKillsession::"+ strApigeeKillsession, TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", "paramters.BBSID:::"+ paramters.BBSID);
                    if (isKillSessionRequired && IsKillSessionRequiredatMSEOend && !string.IsNullOrEmpty(paramters.BBSID))
                    {
                        LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "KillsessionActivity CRBB StartedBeforeESBActivity: "+ e2eData.toString(), TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                        //For testing
                        string E2EData = e2eData.toString();
                        //ESB Killsession trigger
                        if (strApigeeKillsession == "OFF" && !string.IsNullOrEmpty(paramters.BBSID))
                        {
                            LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "Inside CreateBT ESBActivity: " + e2eData.toString(), TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                            System.Threading.Tasks.Task.Run(() => ESBRestCallWrapper.KillSessionRequest(paramters.BBSID, E2EData,actionRequest, ActionReason, paramters.orderKey));
                            LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "After CreateBT ESBActivity: " + e2eData.toString() + "Action is" + actionRequest + "Reason is:" + reason+ "BBReasonIs::" + ActionReason , TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                        }//APIGW Killsession trigger
                        else if (strApigeeKillsession == "ON")
                        {
                            LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "InsideAPIGWActivity: " + e2eData.toString() + "Action is" + actionRequest + "Reason is:" + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                            int retryCount = 0;
                            string baseUrl = ConfigurationManager.AppSettings["killsession_APIGEEurl"].ToString();
                            string bbsId = string.Empty;
                            if (!string.IsNullOrEmpty(paramters.BBSID))
                             bbsId = paramters.BBSID.ToUpper();
                            bool success = false;
                            while (!success)
                            {
                                while (retryCount < ReadConfig.MAX_RETRY_Count_KillSession)
                                {
                                    try
                                    {
                                        string result = ESBRestCallWrapper.APIGWKillSessionRequest(bbsId, baseUrl, "POST");

                                        LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "AfterAPIGWActivity: POST " + "Action is" + actionRequest + "Reason is:" + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                                        if (result == "20200") //success directly we need to call GET method
                                        {
                                            retryCount = ReadConfig.MAX_RETRY_Count_KillSession;

                                        }
                                        else if(result == "20016" || result == "20500")//Here we are capturing the POST call error descrition
                                        {
                                            retryCount++;
                                            if (retryCount >= ReadConfig.MAX_RETRY_Count_KillSession)
                                            {
                                                LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :APIGWKillSession" + " exception in CreateBTCBB service method POSTCALLAPI  ErrorDescription is: " + result + "Action is::" + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);

                                                success = true;
                                                break;
                                            }
                                        }
                                        else //Bad Request or other 
                                        {
                                            retryCount++;
                                            if (retryCount >= ReadConfig.MAX_RETRY_Count_KillSession)
                                            {
                                                LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :APIGWKillSession" + " exception in CreateBTCBB service method POSTCALLAPI  ErrorDescription is: " + result + "Action is::" + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);

                                                success = true;
                                                break;
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        retryCount++;
                                        if (retryCount >= ReadConfig.MAX_RETRY_Count_KillSession)
                                        {
                                            //implement logic for failure capturing.
                                            LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :APIGWKillSession" + " exception in POSTCALLAPI Catch block CreateBTCBB service method  ErrorDescription is: " + ex.Message.ToString() + "Action is " + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                                            success = true;
                                            break;
                                        }
                                    }

                                }//End of while MAX_RETRY_Count_KillSession


                                int getRetryCount = 0;
                                
                                while (getRetryCount < ReadConfig.MAX_RETRY_Count_KillSession)
                                {
                                    try
                                    {
                                        //This is the final status to capture the if failed any into logs
                                        string result = ESBRestCallWrapper.APIGWKillSessionRequest(bbsId, baseUrl, "GET");
                                        if (result == "20200") // Success
                                        {
                                            getRetryCount = ReadConfig.MAX_RETRY_Count_KillSession;
                                            success = true;
                                            break;
                                        }
                                        else if (result == "20016" || result == "20500") //if it fails call POst call
                                        {
                                            getRetryCount++;
                                            string resultPost = ESBRestCallWrapper.APIGWKillSessionRequest(bbsId, baseUrl, "POST");
                                            if (resultPost == "20200")//Success
                                            {
                                                getRetryCount = ReadConfig.MAX_RETRY_Count_KillSession;
                                                success = true;
                                                break;
                                            }
                                            else
                                            {
                                                if (getRetryCount >= ReadConfig.MAX_RETRY_Count_KillSession)
                                                {
                                                    //implement logic for failure capturing.
                                                    LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :APIGWKillSession " + " exception in CreateBTCBB service method GETCALLAPI ErrorDescription is: " + result + "Action is " + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                                                    success = true;
                                                    break;
                                                }
                                            }
                                        }
                                        else if(result == "20021" || result == "20017") //Failed Case  GET CALL
                                        {

                                            getRetryCount++;
                                            Task.Delay(Convert.ToInt32(ConfigurationManager.AppSettings["KillSessionWaitTime"].ToString()));
                                            if (getRetryCount >= ReadConfig.MAX_RETRY_Count_KillSession)
                                            {
                                                //implement logic for failure capturing.
                                                LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :APIGWKillSession " + " exception in CreateBTCBB service method GETCALLAPI ErrorDescription is: " + result + "Action is " + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                                                success = true;
                                                break;
                                            }
                                        }
                                        else //Failed Case  GET CALL Only for Bad request
                                        {
                                            getRetryCount++;
                                            if (getRetryCount >= ReadConfig.MAX_RETRY_Count_KillSession)
                                            {
                                                //implement logic for failure capturing.
                                                LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :APIGWKillSession " + " exception in CreateBTCBB service method GETCALLAPI ErrorDescription is: " + result + "Action is " + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                                                success = true;
                                                break;
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        getRetryCount++;
                                        if (getRetryCount >= ReadConfig.MAX_RETRY_Count_KillSession)
                                        {
                                            //implement logic for failure capturing.
                                            LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :APIGWKillSession" + " exception in CreateBTCBB service method GETCALLAPI Catch Block ErrorDescription is: " + ex.Message.ToString() + "Action is " + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                                            success = true;
                                            break;
                                        }
                                    }

                                }//end Get call While 
                            }
                        } // end APIGEE swith 
                    }
                    
                }//DNP Failed
                else
                {
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //sending failure notification
                        if (paramters.IsOFSOrder)
                        {
                            //Killsession logging for DNP
                            LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :DNP" + " exception in CreateBTCBB service method paramters.IsOFSOrder ErrorDescription is: " + errorMessage + "Action is " + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                            LogHelper.LogErrorMessage(Failed, bptmTxnId, "failed notification sent in CreateBTCBB service with paramters.orderKey as " + paramters.orderKey);
                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true);
                            
                        }
                        else
                        {
                            LogHelper.LogErrorMessage(Failed, bptmTxnId, "failed notification sent in create BTCBB service with paramters.orderKey as " + paramters.orderKey);
                            //Killsession logging for DNP
                            LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :DNP" + " exception in CreateBTCBB service method ErrorDescription is: " + errorMessage + "Action is " + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                            throw new DnpException(errorMessage);
                        }
                    }
                    else
                    {
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                        { 
                            e2eData.endOutboundCall(e2eData.toString());
                        //Killsession logging for DNP
                        LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :DNP" + " exception in CreateBTCBB service method ErrorDescription is: " + "Response is null from DnP" + "Action is " + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                        LogHelper.LogErrorMessage(NullResponseFromDnP, bptmTxnId, NullResponseFromDnP + "failed notification sent in create BTCBB service with paramters.orderKey as " + paramters.orderKey+ "BBSID ::" + paramters.BBSID);
                        //sending failure notification
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true);
                //Killsession logging for DNP
                LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :DNP" + " exception in Catch CreateBTCBB service method ErrorDescription is: " + ex.Message.ToString() + "Action is " + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                LogHelper.LogErrorMessage(ex.Message.ToString(), bptmTxnId, "exception in create BTCBB service method with paramters.orderKey as " + paramters.orderKey);
            }
            finally
            {
                paramters = null;
            }
        }

        //ACCEL-6666
        public void InsertBBSIDRole(BroadBandParameters paramters, ref E2ETransaction e2eData)
        {
            manageClientProfileV1Response1 profileResponse;
            manageClientProfileV1Request1 profileRequest = new manageClientProfileV1Request1();
            profileRequest.manageClientProfileV1Request = new ManageClientProfileV1Request();
            ManageClientProfileV1Req manageClientProfileV1Req = new ManageClientProfileV1Req();

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerblock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerblock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerblock.serviceAddressing.from = @"http://www.profile.com?SAASMSEO";
            headerblock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
            headerblock.serviceState.stateCode = "OK";

            try
            {
                manageClientProfileV1Req.clientProfileV1 = new ClientProfileV1();
                manageClientProfileV1Req.clientProfileV1.client = new Client();
                manageClientProfileV1Req.clientProfileV1.client.action = ACTION_UPDATE;

                manageClientProfileV1Req.clientProfileV1.client.clientIdentity = new ClientIdentity[1];
                manageClientProfileV1Req.clientProfileV1.client.clientIdentity[0] = new ClientIdentity();
                manageClientProfileV1Req.clientProfileV1.client.clientIdentity[0].action = ACTION_SEARCH;
                manageClientProfileV1Req.clientProfileV1.client.clientIdentity[0].value = paramters.Billingaccountnumber;
                manageClientProfileV1Req.clientProfileV1.client.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                manageClientProfileV1Req.clientProfileV1.client.clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;

                ClientServiceInstanceV1[] clientServiceInstance = new ClientServiceInstanceV1[1];
                clientServiceInstance[0] = new ClientServiceInstanceV1();
                clientServiceInstance[0].action = ACTION_UPDATE;
                clientServiceInstance[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstance[0].clientServiceInstanceIdentifier.value = BT_CONSUMER_BROADBAND_NAMEPACE;

                clientServiceInstance[0].clientServiceRole = new ClientServiceRole[1];
                clientServiceInstance[0].clientServiceRole[0] = new ClientServiceRole();
                clientServiceInstance[0].clientServiceRole[0].id = DEFAULT_ROLE;
                clientServiceInstance[0].clientServiceRole[0].clientIdentity = new ClientIdentity[1];
                clientServiceInstance[0].clientServiceRole[0].clientIdentity[0] = new ClientIdentity();
                clientServiceInstance[0].clientServiceRole[0].clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                clientServiceInstance[0].clientServiceRole[0].clientIdentity[0].managedIdentifierDomain.value = "BBSID";
                clientServiceInstance[0].clientServiceRole[0].clientIdentity[0].value = paramters.BBSID;

                clientServiceInstance[0].clientServiceRole[0].clientIdentity[0].action = ACTION_INSERT;
                clientServiceInstance[0].clientServiceRole[0].action = ACTION_UPDATE;

                ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                serviceIdentity[0] = new ServiceIdentity();
                serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity[0].value = paramters.Billingaccountnumber;
                serviceIdentity[0].action = ACTION_SEARCH;

                manageClientProfileV1Req.clientProfileV1.clientServiceInstanceV1 = clientServiceInstance;
                profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req;
                profileRequest.manageClientProfileV1Request.standardHeader = headerblock;

                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall + " to insert BBSID role in the existing profile ", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, profileRequest.SerializeObject());

                #region Call DnP
                profileResponse = DnpWrapper.manageClientProfileV1Thomas(profileRequest, paramters.orderKey);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " to insert BBSID role in the existing profile ", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, profileResponse.SerializeObject());

                if (profileResponse != null
                && profileResponse.manageClientProfileV1Response != null
                && profileResponse.manageClientProfileV1Response.standardHeader != null
                && profileResponse.manageClientProfileV1Response.standardHeader.serviceState != null
                && profileResponse.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                && profileResponse.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse != null && profileResponse.manageClientProfileV1Response != null && profileResponse.manageClientProfileV1Response.standardHeader != null && profileResponse.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                        e2eData.endOutboundCall(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2eData.endOutboundCall(e2eData.toString());
                }
                else
                {
                    string downStreamError = string.Empty;
                    if (profileResponse != null && profileResponse.manageClientProfileV1Response != null && profileResponse.manageClientProfileV1Response.manageClientProfileV1Res != null && profileResponse.manageClientProfileV1Response.manageClientProfileV1Res.messages != null && profileResponse.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        downStreamError = profileResponse.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, downStreamError);
                        if (profileResponse != null && profileResponse.manageClientProfileV1Response != null && profileResponse.manageClientProfileV1Response.standardHeader != null && profileResponse.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "failed to insert BBSID as role in DnP with error msg as " + downStreamError, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                    }
                    else
                    {
                        downStreamError = "Response is null from DnP";

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse != null && profileResponse.manageClientProfileV1Response != null && profileResponse.manageClientProfileV1Response.standardHeader != null && profileResponse.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + NullResponseFromDnP + "Received null response while inserting BBSID as role in DnP ", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                    }
                }
            }
            catch (Exception ex)
            {
                string downStreamError = ex.Message.ToString();
                LogHelper.LogActivityDetails(bptmTxnId, guid, "Exception in " + " inserting BBSID role ", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, downStreamError);
            }
            #endregion
        }

        public void CeaseBTCONSUMERBROADBANDService(BroadBandParameters paramters, MSEOOrderNotification notification, ref E2ETransaction e2eData)
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
                serviceInstance[0].clientServiceInstanceIdentifier.value = BT_CONSUMER_BROADBAND_NAMEPACE;
                serviceInstance[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                serviceInstance[0].clientServiceInstanceStatus.value = INACTIVE;
                serviceInstance[0].action = ACTION_UPDATE;

                List<ServiceIdentity> listServiceIdentity = new List<ServiceIdentity>();
                ServiceIdentity serviceIdentity = new ServiceIdentity();
                serviceIdentity.domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity.value = paramters.Billingaccountnumber;
                serviceIdentity.action = ACTION_SEARCH;

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

                //Logger.Write(paramters.orderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.BTPlusMarkerMessageTrace);

                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, profileRequest.SerializeObject());
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, paramters.orderKey, profileRequest.SerializeObject(), "BTConsumerBroadBand");

                profileResponse = DnpWrapper.manageServiceInstanceV1Thomas(profileRequest, paramters.orderKey);

                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, paramters.orderKey, profileResponse.SerializeObject(), "BTConsumerBroadBand");
                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, profileResponse.SerializeObject());

                if (profileResponse != null
                    && profileResponse.manageServiceInstanceV1Response != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                        e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    //restHelpers.WriteToFile("Success");
                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, Completed, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                    //Logger.Write(paramters.orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                }
                else
                {
                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description;

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                        if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //sending failure notification
                        notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true);
                        LogHelper.LogErrorMessage(errorMessage, bptmTxnId, "failernotification in Ceasing BTCBB service method with paramters.orderKey as " + paramters.orderKey);

                    }
                    else
                    {
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //sending failure notification
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true);
                        LogHelper.LogErrorMessage(NullResponseFromDnP, bptmTxnId, "failernotification in Ceasing BTCBB service method with paramters.orderKey as " + paramters.orderKey);
                        //Logger.Write(paramters.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                    }
                }
            }

            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true);
                LogHelper.LogErrorMessage(ex.Message.ToString(), bptmTxnId, "exception in in Ceasing BTCBB service method with paramters.orderKey as " + paramters.orderKey);
                //Logger.Write(paramters.orderKey + "," + Failed + "," + FailedErrorMessage + ex.Message, Logger.TypeEnum.BTPlusMarkerExceptionTrace);
                //Logger.Write(paramters.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.BTPlusMarkerMessageTrace);
            }
        }

        public void ModifyBTCONSUMERBROADBANDClientIdentity(BroadBandParameters paramters, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            manageClientIdentityResponse1 resp = new manageClientIdentityResponse1();
            manageClientIdentityRequest1 req = new manageClientIdentityRequest1();
            ManageClientIdentityRequest manageclientidentityreq = new ManageClientIdentityRequest();
            manageclientidentityreq.manageClientIdentityReq = new ManageClientIdentityReq();
            try
            {
                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerblock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                headerblock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                headerblock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                headerblock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                headerblock.serviceState.stateCode = "OK";

                manageclientidentityreq.manageClientIdentityReq.clientSearchCriteria = new ClientSearchCriteria();
                manageclientidentityreq.manageClientIdentityReq.clientSearchCriteria.identifierDomainCode = BBSID_NAMESPACE;
                manageclientidentityreq.manageClientIdentityReq.clientSearchCriteria.identifierValue = paramters.oldBBSID;

                manageclientidentityreq.manageClientIdentityReq.clientIdentity = new ClientIdentity();
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.action = "UPDATE";
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.managedIdentifierDomain.value = BBSID_NAMESPACE;
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.value = paramters.BBSID;

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, BT_CONSUMER_BROADBAND_NAMEPACE);

                headerblock.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                headerblock.e2e.E2EDATA = e2eData.toString();

                manageclientidentityreq.standardHeader = headerblock;
                req.manageClientIdentityRequest = manageclientidentityreq;

                //Logger.Write(paramters.orderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.SpringMessageTrace);
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, paramters.orderKey, req.SerializeObject(), "BTConsumerBroadBand");
                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, req.SerializeObject());

                resp = SpringDnpWrapper.manageClientIdentity(req, paramters.orderKey);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, resp.SerializeObject());
                // Logger.Write(paramters.orderKey + "," + GotResponsefromDNP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.SpringMessageTrace);
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, paramters.orderKey, resp.SerializeObject(), "BTConsumerBroadBand");

                if (resp != null
                   && resp.manageClientIdentityResponse != null
                   && resp.manageClientIdentityResponse.standardHeader != null
                   && resp.manageClientIdentityResponse.standardHeader.serviceState != null
                   && resp.manageClientIdentityResponse.standardHeader.serviceState.stateCode != null
                   && resp.manageClientIdentityResponse.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (resp.manageClientIdentityResponse.standardHeader.e2e != null && !string.IsNullOrEmpty(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA))
                    {
                        e2eData.endOutboundCall(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA);
                    }
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                    Logger.Write(paramters.orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                }
                else
                {
                    if (resp != null && resp.manageClientIdentityResponse != null
                        && resp.manageClientIdentityResponse.manageClientIdentityRes != null
                        && resp.manageClientIdentityResponse.manageClientIdentityRes.messages != null
                        && resp.manageClientIdentityResponse.manageClientIdentityRes.messages[0].description != null)
                    {
                        string errorMessage = resp.manageClientIdentityResponse.manageClientIdentityRes.messages[0].description;
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                        if (resp.manageClientIdentityResponse.standardHeader.e2e != null && !string.IsNullOrEmpty(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true);
                        LogHelper.LogErrorMessage(errorMessage, bptmTxnId, "failernotification in Ceasing BTCBB service method with paramters.orderKey as " + paramters.orderKey);

                        //Logger.Write(paramters.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);

                    }
                    else
                    {
                        // Logger.Write(paramters.orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.SpringMessageTrace);
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (resp != null && resp.manageClientIdentityResponse != null
                            && resp.manageClientIdentityResponse.standardHeader != null
                            && resp.manageClientIdentityResponse.standardHeader.e2e != null
                            && resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA != null &&
                            !String.IsNullOrEmpty(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true);
                        LogHelper.LogErrorMessage(NullResponseFromDnP, bptmTxnId, "failernotification in modifing BTCBB service method with paramters.orderKey as " + paramters.orderKey);
                        // Logger.Write(paramters.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                    }
                }

            }
            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (resp != null && resp.manageClientIdentityResponse != null
                    && resp.manageClientIdentityResponse.standardHeader != null &&
                    resp.manageClientIdentityResponse.standardHeader.e2e != null &&
                    resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA != null &&
                    !String.IsNullOrEmpty(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA))

                    e2eData.endOutboundCall(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true);
                LogHelper.LogErrorMessage(ex.Message.ToString(), bptmTxnId, "exception in in Ceasing BTCBB service method with paramters.orderKey as " + paramters.orderKey);
                //Logger.Write(parameters.orderKey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.SpringExceptionTrace);
                //Logger.Write(parameters.orderKey + "," + Failed + "," + FailureResponseFromDnP + ex.Message, Logger.TypeEnum.SpringMessageTrace);
                //Logger.Write(parameters.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
            }

        }

        public void ModifyBTCONSUMERBROADBANDServiceStatus(BroadBandParameters paramters, MSEOOrderNotification notification, string serviceStatus, ref E2ETransaction e2eData, OrderRequest requestOrderRequest,bool isKillSessionRequired=false, string ActionReason = "", string DNPBBSID = "")
        {
            restHelpers.WriteToFile("Inside  ModifyBTCONSUMERBROADBANDServiceStatus");
            //for KillsessionFailures
            string actionRequest = string.Empty;
            string reason = string.Empty;
            if (requestOrderRequest != null && requestOrderRequest.Order.OrderItem[0].Action != null)
            {
                actionRequest = Convert.ToString(requestOrderRequest.Order.OrderItem[0].Action.Code);
                if (!string.IsNullOrEmpty(requestOrderRequest.Order.OrderItem[0].Action.Reason))
                {
                    reason = Convert.ToString(requestOrderRequest.Order.OrderItem[0].Action.Reason);
                    restHelpers.WriteToFile("Inside  ModifyBTCONSUMERBROADBANDServiceStatus" +"Action::"+ actionRequest+"reason:::"+reason);
                }
               
            }

            manageServiceInstanceV1Response1 profileResponse = null;
            manageServiceInstanceV1Request1 profileRequest = new manageServiceInstanceV1Request1();
            profileRequest.manageServiceInstanceV1Request = new ManageServiceInstanceV1Request();

            ManageServiceInstanceV1Req manageServiceInstanceV1Req = new ManageServiceInstanceV1Req();

            try
            {
                ClientServiceInstanceV1[] serviceInstance = new ClientServiceInstanceV1[1];
                serviceInstance[0] = new ClientServiceInstanceV1();

                serviceInstance[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                serviceInstance[0].clientServiceInstanceIdentifier.value = BT_CONSUMER_BROADBAND_NAMEPACE;
                serviceInstance[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                serviceInstance[0].clientServiceInstanceStatus.value = serviceStatus;
                serviceInstance[0].action = ACTION_UPDATE;

                List<ServiceIdentity> listServiceIdentity = new List<ServiceIdentity>();
                ServiceIdentity serviceIdentity = new ServiceIdentity();
                serviceIdentity.domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity.value = paramters.Billingaccountnumber;
                serviceIdentity.action = ACTION_SEARCH;


                listServiceIdentity.Add(serviceIdentity);

                serviceInstance[0].serviceIdentity = listServiceIdentity.ToArray();

                if (!string.IsNullOrEmpty(paramters.Hybrid_Tag))
                {
                    ClientServiceInstanceCharacteristic srvcinstChars = null;
                    List<ClientServiceInstanceCharacteristic> listInstanceChars = new List<ClientServiceInstanceCharacteristic>();

                    srvcinstChars = new ClientServiceInstanceCharacteristic();
                    srvcinstChars.name = "Hybrid_Tag";
                    srvcinstChars.value = paramters.Hybrid_Tag;
                    srvcinstChars.action = "FORCE_INS";
                    listInstanceChars.Add(srvcinstChars);

                    serviceInstance[0].clientServiceInstanceCharacteristic = listInstanceChars.ToArray();
                }

                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                //manageServiceInstanceV1Req.clientServiceInstanceV1 = clientServiceInstance;
                profileRequest.manageServiceInstanceV1Request.standardHeader = headerBlock;

                manageServiceInstanceV1Req.clientServiceInstanceV1 = serviceInstance[0];

                profileRequest.manageServiceInstanceV1Request.manageServiceInstanceV1Req = manageServiceInstanceV1Req;

                //Logger.Write(paramters.orderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.BTPlusMarkerMessageTrace);

                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, profileRequest.SerializeObject());
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, paramters.orderKey, profileRequest.SerializeObject(), "BTConsumerBroadBand");
                restHelpers.WriteToFile("Before DnpWrapper.manageServiceInstanceV1Thomas :::" + paramters.orderKey);
                profileResponse = DnpWrapper.manageServiceInstanceV1Thomas(profileRequest, paramters.orderKey);
                restHelpers.WriteToFile("After DnpWrapper.manageServiceInstanceV1Thomas :::" + paramters.orderKey);

                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, paramters.orderKey, profileResponse.SerializeObject(), "BTConsumerBroadBand");
                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, profileResponse.SerializeObject());
                //DNP Success
                if (profileResponse != null
                    && profileResponse.manageServiceInstanceV1Response != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    restHelpers.WriteToFile("manageServiceInstanceV1Thomas Success :::" + paramters.orderKey);
                    e2eData.logMessage(GotResponseFromDnP, "");
                    //Completed response
                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, Completed, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                        e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2eData.endOutboundCall(e2eData.toString());
                    string E2EData = e2eData.toString();

                    bool IsKillSessionRequiredatMSEOend = Convert.ToBoolean(ConfigurationManager.AppSettings["IsKillSessionRequiredatMSEOend"].ToString());
                    string strApigeeKillsession = ConfigurationManager.AppSettings["APIGEE_KillSession"].ToString();
                    //Newly added for OFS not sending BBSID for debtresume cases those cases we are handling
                    if (ActionReason.Equals("DebtResume")) //Only for DebtResume cases
                    {
                        if (!string.IsNullOrEmpty(paramters.BBSID))
                        {
                            // paramters.BBSID as it is will take
                        }
                        else
                        {
                            paramters.BBSID = DNPBBSID.ToUpper();
                        }
                    }
                    LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "Before ModifyCBB KillsessionActivity isKillSessionRequired: " + isKillSessionRequired + "Reason is:::" + reason + "BBReasonIs::"+ ActionReason+ ":: " + "IsKillSessionRequiredatMSEOend::" + IsKillSessionRequiredatMSEOend + "strApigeeKillsession::" + strApigeeKillsession, TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", "paramters.BBSID:::" + paramters.BBSID);
                    //modified as part of con 73882
                    if (isKillSessionRequired && IsKillSessionRequiredatMSEOend && !string.IsNullOrEmpty(paramters.BBSID))
                    {
                        LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "KillsessionActivity ModifyBB StartedBeforeESBActivity: " + e2eData.toString(), TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);

                        if (strApigeeKillsession == "OFF" && !string.IsNullOrEmpty(paramters.BBSID))
                        {
                            LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "InsideModifyESBActivity: " , TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", "paramters.BBSID::"+ paramters.BBSID); 
                            System.Threading.Tasks.Task.Run(() => ESBRestCallWrapper.KillSessionRequest(paramters.BBSID, E2EData, actionRequest, ActionReason, paramters.orderKey));
                            LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "AfterESBActivity: ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", "paramters.BBSID::" + paramters.BBSID); 
                        }
                        else if (strApigeeKillsession == "ON")
                        {
                           
                            //changes by suneetha.
                            int retryCount = 0;
                            string baseUrl = ConfigurationManager.AppSettings["killsession_APIGEEurl"].ToString();
                            restHelpers.WriteToFile("strApigeeKillsession == ON::APIgeeKillsessionURL::"+ baseUrl);
                            bool success = false;
                            paramters.BBSID = paramters.BBSID.ToUpper();
                            while (!success)
                            {
                                while (retryCount < ReadConfig.MAX_RETRY_Count_KillSession)
                                {
                                    try
                                    {
                                        restHelpers.WriteToFile("before executing ESBRestCallWrapper.APIGWKillSessionRequest::" + paramters.BBSID);
                                        string result = ESBRestCallWrapper.APIGWKillSessionRequest(paramters.BBSID, baseUrl, "POST");
                                        restHelpers.WriteToFile("before executing ESBRestCallWrapper.APIGWKillSessionRequest::" + paramters.BBSID+"Result::::"+result);
                                        if (result == "20200") //success directly we need to call GET method
                                        {
                                            retryCount = ReadConfig.MAX_RETRY_Count_KillSession;

                                        }
                                        else if(result == "20016" || result == "20500")//Here we are capturing the POST call error descrition
                                        {
                                            retryCount++;
                                            if (retryCount >= ReadConfig.MAX_RETRY_Count_KillSession)
                                            {
                                                //implement logic for failure capturing.
                                                LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :APIGWKillSession " + " exception in ModifyBTCBB service method POSTCALLAPI ErrorDescription is: " + result + "Action is" + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                                                success = true;
                                                break;
                                            }
                                        }
                                        else //Bad Request or other 
                                        {
                                            retryCount++;
                                            if (retryCount >= ReadConfig.MAX_RETRY_Count_KillSession)
                                            {
                                                //implement logic for failure capturing.
                                                LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :APIGWKillSession " + " exception in ModifyBTCBB service method POSTCALLAPI ErrorDescription is: " + result + "Action is" + actionRequest + "Reason is " + reason+ "BBReasonIs::" + ActionReason  + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                                                success = true;
                                                break;
                                            }
                                        }
                                    }
                                    catch (TimeoutException ex)
                                    {
                                        retryCount++;
                                        if (retryCount >= ReadConfig.MAX_RETRY_Count_KillSession)
                                        {
                                            //implement logic for failure capturing.
                                            LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :APIGWKillSession " + " exception in ModifyBTCBB service method POSTCALLAPI Catch Block ErrorDescription is: " + ex.Message.ToString() + "Action is" + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                                            success = true;
                                        }
                                    }

                                }//end of while MAX_RETRY_Count_KillSession


                                int getRetryCount = 0;
                                //For Get call purpose
                                while (getRetryCount < ReadConfig.MAX_RETRY_Count_KillSession)
                                {
                                   // Task.Delay(TimeSpan.Parse(ConfigurationManager.AppSettings["KillSessionWaitTime"].ToString()));
                                    try
                                    {
                                        string result = ESBRestCallWrapper.APIGWKillSessionRequest(paramters.BBSID,baseUrl, "GET");
                                        if (result == "20200")//GET CALL Success
                                        {
                                            getRetryCount = ReadConfig.MAX_RETRY_Count_KillSession;
                                            success = true;
                                            break;
                                        }
                                        else if (result == "20016" || result == "20500") //failed case call POst call
                                        {
                                            getRetryCount++;
                                            //Post call again
                                            string resultPost = ESBRestCallWrapper.APIGWKillSessionRequest(paramters.BBSID, baseUrl, "POST");
                                            if (resultPost == "20200")//Success
                                            {
                                                getRetryCount = ReadConfig.MAX_RETRY_Count_KillSession;
                                                success = true;
                                                break;
                                            }
                                            else 
                                            { 
                                             if (getRetryCount >= ReadConfig.MAX_RETRY_Count_KillSession)
                                              {
                                                //implement logic for failure capturing.
                                                LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :APIGWKillSession " + " exception in ModifyBTCBB service method GETCALLAPI ErrorDescription is: " + result + "Action is" + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                                                retryCount++;
                                                success = true;
                                                break;
                                             }
                                            }
                                        }
                                        else if (result == "20021" || result == "20017") //Failed Case  GET CALL
                                        {
                                            getRetryCount++;
                                            Task.Delay(Convert.ToInt32(ConfigurationManager.AppSettings["KillSessionWaitTime"].ToString()));
                                            if (getRetryCount >= ReadConfig.MAX_RETRY_Count_KillSession)
                                            {
                                                //implement logic for failure capturing.
                                                LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :APIGWKillSession " + " exception in ModifyBB service method GETCALLAPI ErrorDescription is: " + result + "Action is " + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                                                //retryCount++;
                                                success = true;
                                                break;
                                            }
                                        }
                                        else //Bad Request or other 
                                        {
                                            getRetryCount++;
                                            if (getRetryCount >= ReadConfig.MAX_RETRY_Count_KillSession)
                                            {
                                                //implement logic for failure capturing.
                                                LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :APIGWKillSession " + " exception in ModifyBB service method GETCALLAPI ErrorDescription is: " + result + "Action is " + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                                                //retryCount++;
                                                success = true;
                                                break;
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        getRetryCount++;
                                        if (getRetryCount >= ReadConfig.MAX_RETRY_Count_KillSession)
                                        {
                                            //implement logic for failure capturing.
                                            LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :APIGWKillSession " + " exception in ModifyBTCBB service method GETCALLAPI Catch Block ErrorDescription is: " + ex.Message.ToString() + "Action is" + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                                            success = true;
                                        }
                                    }
                                }
                            }
                        }
                    }//KIllsessionEnd
                    //modified CON-73882 - end
                    
                }
                else
                { //DNP Failed
                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description != null)
                    {
                        restHelpers.WriteToFile("DNP failed in ModifyCBB");
                        string errorMessage = profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description;

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                        if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //sending failure notification
                        LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :DNP" + " exception in ModifyBTCBB service method ErrorDescription is: " + errorMessage + "Action is" + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                        LogHelper.LogErrorMessage(errorMessage, bptmTxnId, "failednotification in modify BTCBB service status method with paramters.orderKey as " + paramters.orderKey);
                        notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true);
                        
                    }
                    else
                    {
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                        {
                            e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                            LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :DNP" + " exception in ModifyBTCBB service method ErrorDescription is: " + "Response is null from DnP" + "Action is" + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                        }
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //sending failure notification
                        LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :DNP" + " exception in ModifyBTCBB service method ErrorDescription is: " + "Response is null from DnP" + "Action is" + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                        LogHelper.LogErrorMessage(NullResponseFromDnP, bptmTxnId, "failernotification in modify BTCBB service status method with paramters.orderKey as " + paramters.orderKey);
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true);
                     }
                }
            }

            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true);
                LogHelper.LogActivityDetails("bptmTxnId", Guid.NewGuid(), "FailedAt :DNP" + " Catch block exception in ModifyBTCBB service method ErrorDescription is: " + ex.Message.ToString() + "Action is" + actionRequest + "Reason is " + reason + "BBReasonIs::" + ActionReason + "BBSID :" + paramters.BBSID + "RBSID :" + paramters.RBSID + ": ", TimeSpan.Zero, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                LogHelper.LogErrorMessage(ex.Message.ToString(), bptmTxnId, "exception in modify BTCBB service status method with paramters.orderKey as " + paramters.orderKey);
            }
        }

        public bool IsClientIdentityExists(string identity, string identityDomain)
        {
            bool result = false;
            BT.DNP.Rest.getClientProfileV1Res getProfileResponse = new BT.DNP.Rest.getClientProfileV1Res();           

            identity = identity.ToLower();
            string url = ConfigurationManager.AppSettings["VoiceGetSIPurl"].Replace("{identifier_Value}", identity).ToString();
            url = url.Replace("SIP_DEVICE", identityDomain);

            VoiceInfinityRequestProcessor processor = new VoiceInfinityRequestProcessor();
            getProfileResponse = processor.GetSIPDetailsRestCall(url);

            if (getProfileResponse != null && getProfileResponse.clientProfileV1 != null && getProfileResponse.clientProfileV1[0].client != null && getProfileResponse.clientProfileV1[0].client.clientIdentity != null && getProfileResponse.clientProfileV1[0].client.clientIdentity.Count() > 0
                && getProfileResponse.clientProfileV1[0].client.clientIdentity.ToList().Exists(ci => ci.domain.Equals(identityDomain, StringComparison.OrdinalIgnoreCase)))
            {
                result = true;               
            }
            return result;
        }

        public bool IsClientIdentityExists(string identity, string identityDomain, OrderRequest order, string identity1, string identityDomain1,ref E2ETransaction e2eData)
        {
            bool result = false;            
            BT.DNP.Rest.getClientProfileV1Res getProfileResponse = new BT.DNP.Rest.getClientProfileV1Res();

            BroadBandParameters parameters = new BroadBandParameters();

            parameters.orderKey = order.Order.OrderIdentifier.Value;            

            MapRequestParameters(order.Order.OrderItem[0].Instance[0].InstanceCharacteristic, ref parameters);

            identity = identity.ToLower();
            string url = ConfigurationManager.AppSettings["VoiceGetSIPurl"].Replace("{identifier_Value}", identity).ToString();
            url = url.Replace("SIP_DEVICE", identityDomain);

            VoiceInfinityRequestProcessor processor = new VoiceInfinityRequestProcessor();
            getProfileResponse = processor.GetSIPDetailsRestCall(url);

            if (getProfileResponse != null && getProfileResponse.clientProfileV1 != null && getProfileResponse.clientProfileV1[0].client != null && getProfileResponse.clientProfileV1[0].client.clientIdentity != null && getProfileResponse.clientProfileV1[0].client.clientIdentity.Count() > 0
                && getProfileResponse.clientProfileV1[0].client.clientIdentity.ToList().Exists(ci => ci.domain.Equals(identityDomain, StringComparison.OrdinalIgnoreCase)))
            {
                result = true;
                if (getProfileResponse.clientProfileV1[0].client.clientIdentity.Count() == 1)
                {
                    //Need to make linkclient profile to link.
                    LinkClientProfile(parameters, ref e2eData, identity1, identityDomain1,false);
                }
            }
            return result;
        }

        public bool IsInsertBBSIDTOCHOServiceRole(OrderRequest order, string bac, string BBSID, string identity, string identityDomain, string orderKey, ref string downStreamError, ref E2ETransaction e2eData)
        {
            bool result = false;
            ClientIdentity bbsidIdentity = new ClientIdentity();
            bool isBBSSIDExist = false;

            this.bptmTxnId = BPTMHelper.GetTxnIdFromE2eData(e2eData.toString());

            if (IsClientIdentityExists(BBSID, BBSID_NAMESPACE, order, identity,identityDomain,ref e2eData))
                isBBSSIDExist = true;

            BroadBandParameters parameters = null;
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
                if (!isBBSSIDExist && !string.IsNullOrEmpty(identity) && !string.IsNullOrEmpty(identityDomain))
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = identityDomain;
                    clientIdentity.value = identity;
                    clientIdentity.action = ACTION_SEARCH;

                    clientIdentityList.Add(clientIdentity);
                }
                if (!string.IsNullOrEmpty(BBSID))
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BBSID_NAMESPACE;
                    clientIdentity.value = BBSID;
                    if (isBBSSIDExist)
                        clientIdentity.action = ACTION_SEARCH;
                    else
                        clientIdentity.action = ACTION_INSERT;
                    clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity.clientIdentityStatus.value = ACTIVE;

                    clientIdentityList.Add(clientIdentity);
                }

                ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                serviceIdentity[0] = new ServiceIdentity();
                serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity[0].value = bac;
                serviceIdentity[0].action = ACTION_SEARCH;

                ClientServiceInstanceV1[] clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BT_CONSUMER_BROADBAND_NAMEPACE;
                clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                clientServiceInstanceV1[0].action = ACTION_UPDATE;

                //calling this method to get the service roles
                clientServiceInstanceV1[0].clientServiceRole = CreateServiceRoles(parameters, BBSID, false, isBBSSIDExist).ToArray();

                manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;

                profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;
                profileRequest.manageClientProfileV1Request.standardHeader = headerblock;

                //Logger.Write(orderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.BTPlusMarkerMessageTrace);// need to add message trace 
                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall + " to insert BBSID in role and identity ", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", orderKey, profileRequest.SerializeObject());
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, orderKey, profileRequest.SerializeObject(), "BTConsumerBroadBand");

                profileResponse1 = DnpWrapper.manageClientProfileV1Thomas(profileRequest, orderKey);

                // MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, orderKey, profileResponse1.SerializeObject(), "BTConsumerBroadBand");
                //Logger.Write(orderKey + "," + GotResponseFromDnP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.BTPlusMarkerMessageTrace);                
                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " to insert BBSID in role and identity ", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", orderKey, profileResponse1.SerializeObject());

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

                    result = true;

                    LogHelper.LogActivityDetails(bptmTxnId, guid, Completed, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", orderKey);
                }
                else
                {
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        downStreamError = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, downStreamError);
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        //LogHelper.LogErrorMessage(Failed, bptmTxnId, "failed to Delete & Insert BBSID as role and identity in DnP" + orderKey);

                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + "failed to Delete & Insert BBSID as role and identity in DnP with error msg as " + downStreamError, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", orderKey);
                    }
                    else
                    {
                        downStreamError = "Response is null from DnP";

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        //LogHelper.LogErrorMessage(NullResponseFromDnP, bptmTxnId, NullResponseFromDnP + "Received null response in Delete & Insert BBSID as role and identity in DnP " + orderKey);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + NullResponseFromDnP + "Received null response in Delete & Insert BBSID as role and identity in DnP ", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", orderKey);
                    }
                }
            }
            catch (Exception ex)
            {
                downStreamError = ex.Message.ToString();
                
                LogHelper.LogActivityDetails(bptmTxnId, guid, Failed + " with exception as "+downStreamError, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", orderKey);

                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());
            }

            return result;
        }

        public bool updateBBSIDToCHOService(string BBSID, string bac, string orderKey, ref E2ETransaction e2eData, ref string downStreamError)
        {
            bool result = false;

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

                if (!string.IsNullOrEmpty(BBSID))
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BBSID_NAMESPACE;
                    clientIdentity.value = BBSID;
                    clientIdentity.action = ACTION_SEARCH;
                    clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity.clientIdentityStatus.value = ACTIVE;

                    clientIdentityList.Add(clientIdentity);
                }

                ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                serviceIdentity[0] = new ServiceIdentity();
                serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity[0].value = bac;
                serviceIdentity[0].action = ACTION_SEARCH;

                ClientServiceInstanceV1[] clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BT_CONSUMER_BROADBAND_NAMEPACE;
                clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                clientServiceInstanceV1[0].action = ACTION_UPDATE;

                ClientServiceRole clientServcieRole = new ClientServiceRole();
                clientServcieRole.id = DEFAULT_ROLE;
                clientServcieRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                clientServcieRole.clientServiceRoleStatus.value = ACTIVE;
                clientServcieRole.action = ACTION_INSERT;

                clientIdentity = new ClientIdentity();
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = BBSID_NAMESPACE;
                clientIdentity.value = BBSID;
                clientIdentity.action = ACTION_INSERT;
                clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                clientIdentity.clientIdentityStatus.value = ACTIVE;

                clientServcieRole.clientIdentity = new ClientIdentity[1];
                clientServcieRole.clientIdentity[0] = clientIdentity;

                //calling this method to get the service roles
                clientServiceInstanceV1[0].clientServiceRole = new ClientServiceRole[1];
                clientServiceInstanceV1[0].clientServiceRole[0] = clientServcieRole;

                manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;

                profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;
                profileRequest.manageClientProfileV1Request.standardHeader = headerblock;

                //Logger.Write(orderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.BTPlusMarkerMessageTrace);// need to add message trace 
                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall + " to insert BBSID in role and identity ", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", orderKey, profileRequest.SerializeObject());
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, orderKey, profileRequest.SerializeObject(), "BTConsumerBroadBand");

                profileResponse1 = DnpWrapper.manageClientProfileV1Thomas(profileRequest, orderKey);

                // MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, orderKey, profileResponse1.SerializeObject(), "BTConsumerBroadBand");
                //Logger.Write(orderKey + "," + GotResponseFromDnP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.BTPlusMarkerMessageTrace);                
                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " to insert BBSID in role and identity ", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", orderKey, profileResponse1.SerializeObject());

                if (profileResponse1 != null
                   && profileResponse1.manageClientProfileV1Response != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                {

                    //e2eData.logMessage(GotResponseFromDnP, "");
                    //if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    //    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                    //else
                    //    e2eData.endOutboundCall(e2eData.toString());

                    result = true;

                    LogHelper.LogActivityDetails(bptmTxnId, guid, Completed, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", orderKey);
                }
                else
                {
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        downStreamError = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;

                        //e2eData.businessError(GotResponseFromDnPWithBusinessError, downStreamError);
                        //if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                        //    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        //else
                        //    e2eData.endOutboundCall(e2eData.toString());

                        LogHelper.LogErrorMessage(Failed, bptmTxnId, "failed to Delete & Insert BBSID as role and identity in DnP" + orderKey);
                    }
                    else
                    {
                        downStreamError = "Response is null from DnP";

                        //e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        //if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                        //    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        //else
                        //    e2eData.endOutboundCall(e2eData.toString());

                        LogHelper.LogErrorMessage(NullResponseFromDnP, bptmTxnId, NullResponseFromDnP + "Received null response in Delete & Insert BBSID as role and identity in DnP " + orderKey);
                    }
                }
            }
            catch (Exception ex)
            {
                downStreamError = ex.Message.ToString();

                //e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                //if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                //    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                //else
                //    e2eData.endOutboundCall(e2eData.toString());
            }

            return result;
        }

        public static List<ClientServiceRole> CreateServiceRoles(BroadBandParameters paramters, string BBSID, bool isCHORegrade = false, bool isBBSSIDExist = false, bool isRBSSIDExist = false, bool isMSISDNExist = false, bool isBBSIDRegrade = false,bool insertMobile=false,bool isDefaultRoleExists = false)
        {
            List<ClientServiceRole> clientServiceRoles = new List<ClientServiceRole>();
            ClientServiceRole clientServcieRole = null;
            List<ClientIdentity> clientIdentityList = new List<ClientIdentity>();
            ClientIdentity clientIdentity = null; ClientIdentity roleIdentity = null; List<ClientIdentity> roleIdentities = new List<ClientIdentity>();

            if (isCHORegrade && !isBBSIDRegrade)
            {
                clientServcieRole = new ClientServiceRole();
                clientServcieRole.id = DEFAULT_ROLE;
                clientServcieRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                clientServcieRole.clientServiceRoleStatus.value = ACTIVE;
                //if (isRBSSIDExist)
                //    clientServcieRole.action = ACTION_INSERT;
                //else
                    clientServcieRole.action = ACTION_UPDATE;

                clientServcieRole.clientIdentity = new ClientIdentity[1];
                clientServcieRole.clientIdentity[0] = new ClientIdentity();
                clientServcieRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                clientServcieRole.clientIdentity[0].managedIdentifierDomain.value = RBSID_NAMESPACE;
                clientServcieRole.clientIdentity[0].value = paramters.NewRBSID;
                clientServcieRole.clientIdentity[0].action = ACTION_INSERT;
                clientServcieRole.clientIdentity[0].clientIdentityStatus = new ClientIdentityStatus();
                clientServcieRole.clientIdentity[0].clientIdentityStatus.value = ACTIVE;

                clientServiceRoles.Add(clientServcieRole);
            }
            else if (isCHORegrade && isBBSIDRegrade)
            {
                clientServcieRole = new ClientServiceRole();
                clientServcieRole.id = DEFAULT_ROLE;
                clientServcieRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                clientServcieRole.clientServiceRoleStatus.value = ACTIVE;
                //if (isRBSSIDExist)
                //    clientServcieRole.action = ACTION_INSERT;
                //else
                    clientServcieRole.action = ACTION_UPDATE;

                clientServcieRole.clientIdentity = new ClientIdentity[1];
                clientServcieRole.clientIdentity[0] = new ClientIdentity();
                clientServcieRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                clientServcieRole.clientIdentity[0].managedIdentifierDomain.value = BBSID_NAMESPACE;
                clientServcieRole.clientIdentity[0].value = paramters.NewBBSID;
                clientServcieRole.clientIdentity[0].action = ACTION_INSERT;
                clientServcieRole.clientIdentity[0].clientIdentityStatus = new ClientIdentityStatus();
                clientServcieRole.clientIdentity[0].clientIdentityStatus.value = ACTIVE;

                clientServiceRoles.Add(clientServcieRole);
            }
            else if (insertMobile)
            {
                clientServcieRole = new ClientServiceRole();
                clientServcieRole.id = DEFAULT_ROLE;
                clientServcieRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                clientServcieRole.clientServiceRoleStatus.value = ACTIVE;    
                //Newly Implemented for Default Role handling for Mobile case with Hybrid case
                if(isDefaultRoleExists)
                    clientServcieRole.action = ACTION_UPDATE;
                else
                    clientServcieRole.action = ACTION_INSERT;

                clientServcieRole.clientIdentity = new ClientIdentity[1];
                clientServcieRole.clientIdentity[0] = new ClientIdentity();
                clientServcieRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                clientServcieRole.clientIdentity[0].managedIdentifierDomain.value = MOBILE_NAMESPACE;
                clientServcieRole.clientIdentity[0].value = GetMSISDN(paramters.MSISDN);
                clientServcieRole.clientIdentity[0].action = ACTION_INSERT;
                clientServcieRole.clientIdentity[0].clientIdentityStatus = new ClientIdentityStatus();
                clientServcieRole.clientIdentity[0].clientIdentityStatus.value = ACTIVE;

                clientServiceRoles.Add(clientServcieRole);
            }
            else
            {
                if (paramters != null)
                {
                    if (paramters.IsAHTDone)
                    {
                        clientServcieRole = new ClientServiceRole();
                        clientServcieRole.id = ADMIN_ROLE;
                        clientServcieRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                        clientServcieRole.clientServiceRoleStatus.value = ACTIVE;
                        clientServcieRole.clientIdentity = new ClientIdentity[1];
                        clientServcieRole.clientIdentity[0] = new ClientIdentity();
                        clientServcieRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientServcieRole.clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                        clientServcieRole.clientIdentity[0].value = paramters.Billingaccountnumber;
                        clientServcieRole.clientIdentity[0].action = ACTION_INSERT;
                        clientServcieRole.clientIdentity[0].clientIdentityStatus = new ClientIdentityStatus();
                        clientServcieRole.clientIdentity[0].clientIdentityStatus.value = ACTIVE;
                        clientServcieRole.action = ACTION_INSERT;

                        clientServiceRoles.Add(clientServcieRole);
                    }

                    clientServcieRole = new ClientServiceRole();
                    clientServcieRole.id = DEFAULT_ROLE;
                    clientServcieRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                    clientServcieRole.clientServiceRoleStatus.value = ACTIVE;
                    clientServcieRole.action = ACTION_INSERT;

                    if (!string.IsNullOrEmpty(paramters.MSISDN))
                    {
                        roleIdentity = new ClientIdentity();
                        roleIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        roleIdentity.managedIdentifierDomain.value = MOBILE_NAMESPACE;
                        if (isMSISDNExist)
                            roleIdentity.value = GetMSISDN(paramters.MSISDN);
                        else
                            roleIdentity.value = GetMSISDN(paramters.MSISDN);
                        roleIdentity.action = ACTION_INSERT;
                        roleIdentity.clientIdentityStatus = new ClientIdentityStatus();
                        roleIdentity.clientIdentityStatus.value = ACTIVE;
                        roleIdentities.Add(roleIdentity);
                    }

                    if (!string.IsNullOrEmpty(paramters.RBSID))
                    {
                        roleIdentity = new ClientIdentity();
                        roleIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        roleIdentity.managedIdentifierDomain.value = RBSID_NAMESPACE;
                        roleIdentity.value = paramters.RBSID;
                        roleIdentity.action = ACTION_INSERT;
                        roleIdentity.clientIdentityStatus = new ClientIdentityStatus();
                        roleIdentity.clientIdentityStatus.value = ACTIVE;
                        roleIdentities.Add(roleIdentity);
                    }

                    if (!string.IsNullOrEmpty(paramters.BBSID))
                    {

                        roleIdentity = new ClientIdentity();
                        roleIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        roleIdentity.managedIdentifierDomain.value = BBSID_NAMESPACE;
                        roleIdentity.value = paramters.BBSID;
                        roleIdentity.action = ACTION_INSERT;
                        roleIdentity.clientIdentityStatus = new ClientIdentityStatus();
                        roleIdentity.clientIdentityStatus.value = ACTIVE;
                        roleIdentities.Add(roleIdentity);
                    }
                    clientServcieRole.clientIdentity = roleIdentities.ToArray();
                    clientServiceRoles.Add(clientServcieRole);
                }

                else if (!string.IsNullOrEmpty(BBSID))
                {
                    clientServcieRole = new ClientServiceRole();
                    clientServcieRole.id = DEFAULT_ROLE;
                    clientServcieRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                    clientServcieRole.clientServiceRoleStatus.value = ACTIVE;
                    //if (isBBSSIDExist)
                    //    clientServcieRole.action = ACTION_INSERT;
                    //else
                        clientServcieRole.action = ACTION_UPDATE;

                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BBSID_NAMESPACE;
                    clientIdentity.value = BBSID;
                    clientIdentity.action = ACTION_INSERT;
                    clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity.clientIdentityStatus.value = ACTIVE;
                    clientIdentityList.Add(clientIdentity);
                    clientServcieRole.clientIdentity = clientIdentityList.ToArray();

                    clientServiceRoles.Add(clientServcieRole);
                }
            }
            //clientServiceRoles.Add(clientServcieRole);
            return clientServiceRoles;
        }

        public void MapCHOPRegradeRequest(BroadBandParameters paramters, MSEOOrderNotification notification, ref E2ETransaction e2eData,bool isNotificationnotRequired=false)
        {
            bool isOldRBSIDLinked = false;
            bool isNewRBSIDExists = false;
            string identity = string.Empty;
            string identityDomain = string.Empty;

            GetClientProfileV1Res rbsidProfileResponse = null;
            ClientServiceInstanceV1[] bacServiceResponse = null;

            if (!paramters.RBSID.Equals(paramters.NewRBSID, StringComparison.OrdinalIgnoreCase))
            {
                rbsidProfileResponse = DnpWrapper.GetClientProfileV1ForThomas(paramters.NewRBSID, RBSID_NAMESPACE, paramters.orderKey);

                bacServiceResponse = DnpWrapper.getServiceInstanceV1(paramters.Billingaccountnumber, BACID_IDENTIFER_NAMEPACE, BT_CONSUMER_BROADBAND_NAMEPACE);

                if (String.IsNullOrEmpty(paramters.RBSID))
                {
                    GetRBSIDfromBACProfile(bacServiceResponse, ref paramters);                    
                }

                if (paramters.RBSID.Equals(paramters.NewRBSID, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(paramters.Hybrid_Tag))
                {
                    if(!isNotificationnotRequired)
                        notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, Accepted, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);

                    UpdateHybridTagtoCHOPServiceStatus(paramters, notification, string.Empty, ref e2eData);
                }
                else if (paramters.RBSID.Equals(paramters.NewRBSID, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(paramters.Hybrid_Tag))
                {
                    //throw new Exception("Old and New RBSID values are equal in the Request");
                    if (!isNotificationnotRequired)
                        notification.sendNotification(false, false, "001", "Old and New RBSID values are equal in the Request", ref e2eData, true);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "Old and New RBSID values are equal in the Request", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                }
                else
                {
                    if (!isNotificationnotRequired)
                        notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, Accepted, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);

                    if (bacServiceResponse != null && bacServiceResponse.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null && si.clientServiceRole.Count() > 0 && si.clientServiceRole.ToList().Exists(csr => csr.id.Equals(DEFAULT_ROLE, StringComparison.OrdinalIgnoreCase) && csr.clientIdentity != null && csr.clientIdentity.Count() > 0 && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(RBSID_NAMESPACE, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(paramters.RBSID,StringComparison.OrdinalIgnoreCase)))))
                    {
                        isOldRBSIDLinked = true;
                    }
                    else if (paramters.isServiceExixsting && bacServiceResponse != null && bacServiceResponse.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (bacServiceResponse.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null && si.clientServiceRole.Count() > 0 && si.clientServiceRole.ToList().Exists(csr => csr.id.Equals(DEFAULT_ROLE, StringComparison.OrdinalIgnoreCase) && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(MOBILE_NAMESPACE, StringComparison.OrdinalIgnoreCase)))))
                        {
                            ClientServiceRole serviceRole = bacServiceResponse.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null && si.clientServiceRole.Count() > 0 && si.clientServiceRole.ToList().Exists(csr => csr.id.Equals(DEFAULT_ROLE, StringComparison.OrdinalIgnoreCase) && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(MOBILE_NAMESPACE, StringComparison.OrdinalIgnoreCase)))).FirstOrDefault().clientServiceRole.FirstOrDefault();
                            identity = serviceRole.clientIdentity.FirstOrDefault().value.ToString();
                            identityDomain = serviceRole.clientIdentity.FirstOrDefault().managedIdentifierDomain.value;
                        }
                        else
                        {
                            ClientServiceRole serviceRole = bacServiceResponse.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null && si.clientServiceRole.Count() > 0 && si.clientServiceRole.ToList().Exists(csr => csr.id.Equals(DEFAULT_ROLE, StringComparison.OrdinalIgnoreCase))).FirstOrDefault().clientServiceRole.FirstOrDefault();
                            identity = serviceRole.clientIdentity.FirstOrDefault().value.ToString();
                            identityDomain = serviceRole.clientIdentity.FirstOrDefault().managedIdentifierDomain.value;
                        }
                    }
                    if (rbsidProfileResponse != null && rbsidProfileResponse.clientProfileV1 != null && rbsidProfileResponse.clientProfileV1.client != null && rbsidProfileResponse.clientProfileV1.client.clientIdentity != null
                        && rbsidProfileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(RBSID_NAMESPACE, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(paramters.NewRBSID,StringComparison.OrdinalIgnoreCase)))
                    {
                        isNewRBSIDExists = true;

                        if (rbsidProfileResponse.clientProfileV1.client.clientIdentity.Count() == 1)
                        {
                            //LinkClientProfile(paramters, ref e2eData, identity, identityDomain, true);
                        }
                    }
                    if ((isOldRBSIDLinked && isNewRBSIDExists) || (isOldRBSIDLinked && !isNewRBSIDExists))
                    {
                        //Batch call to delete the old value and link to the new value
                        UpdateRBSIDinCHOService(paramters, isNewRBSIDExists, notification, ref e2eData,isNotificationnotRequired);
                    }
                    else if ((!isOldRBSIDLinked && isNewRBSIDExists) || (!isOldRBSIDLinked && !isNewRBSIDExists))
                    {
                        InsertRBSIDTOCHOService(paramters, identity, identityDomain, isNewRBSIDExists, notification, ref e2eData, isNotificationnotRequired);
                    }
                }
            }
            else if (paramters.RBSID.Equals(paramters.NewRBSID, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(paramters.Hybrid_Tag))
            {
                if (!isNotificationnotRequired)
                    notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData, false);
                LogHelper.LogActivityDetails(bptmTxnId, guid, Accepted, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);

                UpdateHybridTagtoCHOPServiceStatus(paramters, notification, string.Empty, ref e2eData);
            }
            else if (paramters.RBSID.Equals(paramters.NewRBSID, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(paramters.Hybrid_Tag))
            {
                if (!isNotificationnotRequired)
                    notification.sendNotification(false, false, "001", "Old and New RBSID values are equal in the Request", ref e2eData, true);
                LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "Old and New RBSID values are equal in the Request", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
            }
        }

        public void MapCHOPRegradeRequestforBBSID(BroadBandParameters paramters, ref E2ETransaction e2eData)
        {
            bool isOldBBSIDLinked = false;
            bool isNewBBSIDExists = false;
            string identity = string.Empty;
            string identityDomain = string.Empty;

            GetClientProfileV1Res bbsidProfileResponse = null;
            ClientServiceInstanceV1[] bacServiceResponse = null;
            ClientServiceInstanceV1 chopResponse = null;


            bbsidProfileResponse = DnpWrapper.GetClientProfileV1ForThomas(paramters.NewBBSID, BBSID_NAMESPACE, paramters.orderKey);

            bacServiceResponse = DnpWrapper.getServiceInstanceV1(paramters.Billingaccountnumber, BACID_IDENTIFER_NAMEPACE, BT_CONSUMER_BROADBAND_NAMEPACE);

            //get old BBSID from profile
            GetBBSIDfromBACProfile(bacServiceResponse, ref paramters);


            if (paramters.BBSID.Equals(paramters.NewBBSID, StringComparison.OrdinalIgnoreCase))
            {               
                LogHelper.LogActivityDetails(bptmTxnId, guid, Ignored + "Old and New BBSID values are equal in the Request", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
            }
           
            else
            {
                if (bacServiceResponse != null && bacServiceResponse.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null && si.clientServiceRole.Count() > 0 && si.clientServiceRole.ToList().Exists(csr => csr.id.Equals(DEFAULT_ROLE, StringComparison.OrdinalIgnoreCase) && csr.clientIdentity != null && csr.clientIdentity.Count() > 0 && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BBSID_NAMESPACE, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(paramters.BBSID,StringComparison.OrdinalIgnoreCase)))))
                {
                    isOldBBSIDLinked = true;
                }
                if (paramters.isServiceExixsting && bacServiceResponse != null && bacServiceResponse.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                {
                    if (bacServiceResponse.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null && si.clientServiceRole.Count() > 0 && si.clientServiceRole.ToList().Exists(csr => csr.id.Equals(DEFAULT_ROLE, StringComparison.OrdinalIgnoreCase) && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(MOBILE_NAMESPACE, StringComparison.OrdinalIgnoreCase)))))
                    {
                        chopResponse = bacServiceResponse.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                        foreach (ClientServiceRole serviceRole in chopResponse.clientServiceRole)
                        {
                            if (serviceRole.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(MOBILE_NAMESPACE, StringComparison.OrdinalIgnoreCase)&&!string.IsNullOrEmpty(ci.value)))
                            {
                                identity = serviceRole.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(MOBILE_NAMESPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                identityDomain = MOBILE_NAMESPACE;
                            }                                             
                        }
                    }
                    else if(!paramters.IsBBSIDRoleDelete)
                    {
                        ClientServiceRole serviceRole = bacServiceResponse.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null && si.clientServiceRole.Count() > 0 && si.clientServiceRole.ToList().Exists(csr => csr.id.Equals(DEFAULT_ROLE, StringComparison.OrdinalIgnoreCase))).FirstOrDefault().clientServiceRole.FirstOrDefault();
                        identity = serviceRole.clientIdentity.FirstOrDefault().value.ToString();
                        identityDomain = serviceRole.clientIdentity.FirstOrDefault().managedIdentifierDomain.value;
                    }
                }
                if (bbsidProfileResponse != null && bbsidProfileResponse.clientProfileV1 != null && bbsidProfileResponse.clientProfileV1.client != null && bbsidProfileResponse.clientProfileV1.client.clientIdentity != null
                    && bbsidProfileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BBSID_NAMESPACE, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(paramters.NewBBSID,StringComparison.OrdinalIgnoreCase)))
                {
                    isNewBBSIDExists = true;

                    if (bbsidProfileResponse.clientProfileV1.client.clientIdentity.Count() == 1)
                    {
                        LinkClientProfile(paramters, ref e2eData, identity, identityDomain, true);
                    }
                }
                if ((isOldBBSIDLinked && isNewBBSIDExists) || (isOldBBSIDLinked && !isNewBBSIDExists))
                {
                    //Batch call to delete the old value and link to the new value
                    UpdateBBSIDinCHOService(paramters, identity, identityDomain, isNewBBSIDExists, ref e2eData);
                }
                else if ((!isOldBBSIDLinked && isNewBBSIDExists) || (!isOldBBSIDLinked && !isNewBBSIDExists))
                {
                    InsertBBSIDTOCHOService(paramters, identity, identityDomain, isNewBBSIDExists, ref e2eData);
                }
            }            
        }
        
        public static void deleteidentity(string BBSID, string unvid)
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
                manageClientProfileV1Req1.clientProfileV1.client.clientIdentifier = new ClientIdentifier();
                manageClientProfileV1Req1.clientProfileV1.client.clientIdentifier.value = unvid;

                List<ClientIdentity> clientIdentityList = new List<ClientIdentity>();

                ClientIdentity clientIdentity = null;

                if (!string.IsNullOrEmpty(BBSID))
                {
                    //clientIdentity = new ClientIdentity();
                    //clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    //clientIdentity.managedIdentifierDomain.value = BBSID_NAMESPACE;
                    //clientIdentity.value = BBSID;
                    //clientIdentity.action = ACTION_SEARCH;
                    //clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                    //clientIdentity.clientIdentityStatus.value = ACTIVE;

                    //clientIdentityList.Add(clientIdentity);

                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BBSID_NAMESPACE;
                    clientIdentity.value = BBSID;
                    clientIdentity.action = ACTION_DELETE;
                    clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity.clientIdentityStatus.value = ACTIVE;

                    clientIdentityList.Add(clientIdentity);
                }

                manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();

                profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;
                profileRequest.manageClientProfileV1Request.standardHeader = headerblock;

                //Logger.Write(orderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.BTPlusMarkerMessageTrace);// need to add message trace 
                //LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall + " to insert BBSID in role and identity ", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", orderKey, profileRequest.SerializeObject());
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, orderKey, profileRequest.SerializeObject(), "BTConsumerBroadBand");

                profileResponse1 = DnpWrapper.manageClientProfileV1Thomas(profileRequest, "orderKey");

                // MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, orderKey, profileResponse1.SerializeObject(), "BTConsumerBroadBand");
                //Logger.Write(orderKey + "," + GotResponseFromDnP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.BTPlusMarkerMessageTrace);                
                //LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " to insert BBSID in role and identity ", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", orderKey, profileResponse1.SerializeObject());

                if (profileResponse1 != null
                   && profileResponse1.manageClientProfileV1Response != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                   && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                {

                    //e2eData.logMessage(GotResponseFromDnP, "");
                    //if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    //    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                    //else
                    //    e2eData.endOutboundCall(e2eData.toString());

                    // result = true;

                    // LogHelper.LogActivityDetails(bptmTxnId, guid, Completed, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", orderKey);
                }
            }
            catch (Exception ex)
            {

            }
        }


        #region ValidateCHOPRegradeRequest
        /// <summary>
        /// Verify the list of Order items and check if the
        /// request contains atleast  one pair of Create-Cancel with service code BT_CONSUMER_BROADBAND:
        /// If yes then it is a CHOP Regrade request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static bool IsCHOPRegrade(OrderRequest request)
        {
            int cancelCount = 0;
            int createCount = 0;
            bool result;
            foreach (OrderItem orderItem in request.Order.OrderItem)
            {
                if (orderItem.Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                {
                    if (orderItem.Instance[0] != null && orderItem.Instance[0].InstanceCharacteristic != null && orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(x => x.Name != null && x.Name.Equals("ServiceCode", StringComparison.OrdinalIgnoreCase) && x.Value != null && x.Value.Equals("BT_CONSUMER_BROADBAND", StringComparison.OrdinalIgnoreCase)))
                        cancelCount++;
                }
                else if (orderItem.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase))
                {
                    if (orderItem.Instance[0] != null && orderItem.Instance[0].InstanceCharacteristic != null && orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(x => x.Name != null && x.Name.Equals("ServiceCode", StringComparison.OrdinalIgnoreCase) && x.Value != null && x.Value.Equals("BT_CONSUMER_BROADBAND", StringComparison.OrdinalIgnoreCase)))
                        createCount++;
                }
            }
            if (cancelCount >= 1 && createCount >= 1)
            {
                result = true;
            }
            else
            {
                result = false;
            }
            return result;
        }
        #endregion

        #region MapRequestParameters
        /// <summary>
        /// Map list of request parameters to Broadband params object..      
        /// </summary>
        /// <param name="instanceChars"></param>
        /// <returns>paramters</returns>
        public static void MapRequestParameters(InstanceCharacteristic[] instanceChars, ref BroadBandParameters paramters)
        {
            foreach (InstanceCharacteristic insChar in instanceChars)
            {
                if ((insChar.Name.Equals("BillAccountNumber", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                {
                    paramters.Billingaccountnumber = insChar.Value;
                }
                if ((insChar.Name.Equals("MSISDN", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                {
                    paramters.MSISDN = insChar.Value;
                }
                if ((insChar.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                {
                    paramters.RBSID = insChar.Value;
                }
                if ((insChar.Name.Equals("BTWSID", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                {
                    paramters.BBSID = insChar.Value;
                }
                else if ((insChar.Name.Equals("WholesaleServiceId", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                {
                    paramters.BBSID = insChar.Value;
                }
                else if ((insChar.Name.Equals("BBSID", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                {
                    paramters.BBSID = insChar.Value;
                }
                if ((insChar.Name.Equals("BTWSID", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.PreviousValue)))
                {
                    paramters.oldBBSID = insChar.PreviousValue;
                }
                if ((insChar.Name.Equals("BtOneId", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                {
                    paramters.BtOneId = insChar.Value;
                }
                if ((insChar.Name.Equals("Hybrid_Tag", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(insChar.Value)))
                {
                    paramters.Hybrid_Tag = insChar.Value;
                }
            }
        }
        #endregion

        #region GetRBSIDforRegrade
        /// <summary>
        /// Get the new RBSID value from input request..      
        /// </summary>
        /// <param name="reqOrder"></param>
        /// <param name="paramters"></param>
        public static void GetRBSIDforRegrade(OrderRequest reqOrder, ref BroadBandParameters paramters)
        {
            // To get new RBSID.
            if (reqOrder.Order.OrderItem.ToList().Exists(x => x.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase))&&reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic != null
                && reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic.ToList().Exists(x => x.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase)))
                paramters.NewRBSID = reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic.ToList().Where(x => x.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
            else if (reqOrder.Order.OrderItem.ToList().Exists(x => x.Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase)) && reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic != null
                && reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic.ToList().Exists(x => x.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase)))
                paramters.NewRBSID = reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic.ToList().Where(x => x.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

            // To get hybrid tag from create orderItem.
            if (reqOrder.Order.OrderItem.ToList().Exists(x => x.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)) && reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic != null
                && reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic.ToList().Exists(x => x.Name.Equals("Hybrid_Tag", StringComparison.OrdinalIgnoreCase)))
                paramters.Hybrid_Tag = reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic.ToList().Where(x => x.Name.Equals("Hybrid_Tag", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

            //To get old RBSID
            if (reqOrder.Order.OrderItem.ToList().Exists(x => x.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)) && reqOrder.Order.OrderItem.ToList().Exists(x => x.Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase))
               && reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic.ToList().Exists(x => x.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase)))
                paramters.RBSID = reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic.ToList().Where(x => x.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
            else if (reqOrder.Order.OrderItem.ToList().Exists(x => x.Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase)) && reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic != null
               && reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic.ToList().Exists(x => x.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase)))
                paramters.RBSID = reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic.ToList().Where(x => x.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().PreviousValue;
            else if (reqOrder.Order.OrderItem.ToList().Exists(x => x.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)) && reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic != null
               && reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic.ToList().Exists(x => x.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(x.PreviousName)))
                paramters.RBSID = reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic.ToList().Where(x => x.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().PreviousValue;
            else
                paramters.RBSID = string.Empty;

            // To get new BBSID.
            if (reqOrder.Order.OrderItem.ToList().Exists(x => x.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)) && reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic != null
               && reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic.ToList().Exists(x => x.Name.Equals("BTWSID", StringComparison.OrdinalIgnoreCase)))
                paramters.NewBBSID = reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic.ToList().Where(x => x.Name.Equals("BTWSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
            else if (reqOrder.Order.OrderItem.ToList().Exists(x => x.Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase)) && reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic != null
               && reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic.ToList().Exists(x => x.Name.Equals("BTWSID", StringComparison.OrdinalIgnoreCase)))
                paramters.NewBBSID = reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("Modify", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic.ToList().Where(x => x.Name.Equals("BTWSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

            ////To get old BBSID
            //if (reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic != null
            //   && reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic.ToList().Exists(x => x.Name.Equals("BTWSID", StringComparison.OrdinalIgnoreCase)))
            //    paramters.BBSID = reqOrder.Order.OrderItem.ToList().Where(x => x.Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance.FirstOrDefault().InstanceCharacteristic.ToList().Where(x => x.Name.Equals("BTWSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

        }
        #endregion

        #region GetMSISDN
        /// <summary>
        /// Returns 12 digit MSISDN for input passed..      
        /// </summary>
        /// <param name="MSISDN"></param>        
        public static string GetMSISDN(string MSISDN)
        {
            return MSISDN.Length.Equals(12) ? MSISDN : MSISDN.Length.Equals(11) ? "44" + MSISDN.Remove(0, 1) : "44" + MSISDN;
        }
        #endregion

        public static void GetRBSIDfromBACProfile(ClientServiceInstanceV1[] bacServiceResponse, ref BroadBandParameters parameters)
        {
            if (bacServiceResponse != null && bacServiceResponse.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null && si.clientServiceRole.Count() > 0 && si.clientServiceRole.ToList().Exists(csr => csr.id.Equals(DEFAULT_ROLE, StringComparison.OrdinalIgnoreCase) && csr.clientIdentity != null && csr.clientIdentity.Count() > 0 && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(RBSID_NAMESPACE, StringComparison.OrdinalIgnoreCase)))))
            {
                ClientServiceInstanceV1 serviceInstance = bacServiceResponse.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                ClientServiceRole serviceRole = serviceInstance.clientServiceRole.ToList().Where(csr => csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(RBSID_NAMESPACE, StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
                parameters.RBSID = serviceRole.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(RBSID_NAMESPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
            }
        }

        public static void GetBBSIDfromBACProfile(ClientServiceInstanceV1[] bacServiceResponse, ref BroadBandParameters parameters)
        {
            if (bacServiceResponse != null && bacServiceResponse.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null && si.clientServiceRole.Count() > 0 && si.clientServiceRole.ToList().Exists(csr => csr.id.Equals(DEFAULT_ROLE, StringComparison.OrdinalIgnoreCase) && csr.clientIdentity != null && csr.clientIdentity.Count() > 0 && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BBSID_NAMESPACE, StringComparison.OrdinalIgnoreCase)))))
            {
                ClientServiceInstanceV1 serviceInstance = bacServiceResponse.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(BT_CONSUMER_BROADBAND_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                ClientServiceRole serviceRole = serviceInstance.clientServiceRole.ToList().Where(csr => csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BBSID_NAMESPACE, StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
                parameters.BBSID = serviceRole.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(BBSID_NAMESPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                if (serviceRole != null && serviceRole.clientIdentity != null && serviceRole.clientIdentity.Count() > 0 && serviceRole.clientIdentity.Count() == 1)
                {
                    parameters.IsBBSIDRoleDelete = true;
                }
            }
            else
                parameters.BBSID = string.Empty;
        }

        public static bool IsMOBILEIdentityExists(ClientServiceInstanceV1 srvcInstance, ref BroadBandParameters parameters)
        {
            bool isMobileIdentityExists = false;

            if (srvcInstance != null && srvcInstance.clientServiceRole != null && srvcInstance.clientServiceRole.Count() > 0 && srvcInstance.clientServiceRole.ToList().Exists(csr => csr.id.Equals("DEAFULT", StringComparison.OrdinalIgnoreCase) && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("MOBILE", StringComparison.OrdinalIgnoreCase))))
                isMobileIdentityExists = true;
            else
            {
                if (srvcInstance.clientServiceRole != null && srvcInstance.clientServiceRole.Count() > 0 && srvcInstance.clientServiceRole.ToList().Exists(csr => csr.id.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase) && csr.clientIdentity != null && csr.clientIdentity.Count() > 0))
                {
                    ClientServiceRole serviceRole = srvcInstance.clientServiceRole.ToList().Where(csr => csr.id.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    parameters.Idenity = serviceRole.clientIdentity.FirstOrDefault().value.ToString();
                    parameters.IdenityDomain = serviceRole.clientIdentity.FirstOrDefault().managedIdentifierDomain.value;
                }
            }

            return isMobileIdentityExists;
        }
        /// <summary>
        /// UpdateRBSIDinCHOService to update the RSBID value as per the regrade request
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="isNewRBSIDExists"></param>
        /// <param name="e2eData"></param>
        public void UpdateRBSIDinCHOService(BroadBandParameters parameters, bool isNewRBSIDExists, MSEOOrderNotification notification, ref E2ETransaction e2eData,bool isNotificationnotRequired=false)
        {
            manageBatchProfilesV1Response1 batchResponse = null;
            manageBatchProfilesV1Request1 batchProfileRequest = null;
            try
            {
                batchProfileRequest = new manageBatchProfilesV1Request1();
                batchProfileRequest.manageBatchProfilesV1Request = new ManageBatchProfilesV1Request();

                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerblock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                headerblock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                headerblock.serviceAddressing.from = @"http://www.profile.com?SAASMSEO";
                headerblock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                headerblock.serviceState.stateCode = "OK";
                List<ClientProfileV1> clientProfileList = new List<ClientProfileV1>();
                ClientProfileV1 clientProfile = null;
                ClientServiceInstanceV1[] clientServiceInstanceV1 = null;
                List<ClientIdentity> clientIdentityList = null;
                ClientIdentity clientIdentity = null;

                ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                serviceIdentity[0] = new ServiceIdentity();
                serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity[0].value = parameters.Billingaccountnumber;
                serviceIdentity[0].action = ACTION_SEARCH;

                #region delete defaultrole
                clientProfile = new ClientProfileV1();
                clientProfile.client = new Client();
                clientProfile.client.action = ACTION_UPDATE;

                clientIdentityList = new List<ClientIdentity>();
                clientIdentity = new ClientIdentity();
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = RBSID_NAMESPACE;
                clientIdentity.value = parameters.RBSID;
                clientIdentity.action = ACTION_SEARCH;
                clientIdentityList.Add(clientIdentity);
                clientProfile.client.clientIdentity = clientIdentityList.ToArray();

                clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BT_CONSUMER_BROADBAND_NAMEPACE;
                clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                clientServiceInstanceV1[0].action = ACTION_UPDATE;
                clientServiceInstanceV1[0].clientServiceRole = new ClientServiceRole[1];
                clientServiceInstanceV1[0].clientServiceRole[0] = new ClientServiceRole();
                clientServiceInstanceV1[0].clientServiceRole[0].id = DEFAULT_ROLE;
                clientServiceInstanceV1[0].clientServiceRole[0].clientIdentity = new ClientIdentity[1];
                clientServiceInstanceV1[0].clientServiceRole[0].clientIdentity[0] = new ClientIdentity();
                clientServiceInstanceV1[0].clientServiceRole[0].clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                clientServiceInstanceV1[0].clientServiceRole[0].clientIdentity[0].managedIdentifierDomain.value = RBSID_NAMESPACE;
                clientServiceInstanceV1[0].clientServiceRole[0].clientIdentity[0].value = parameters.RBSID;
                clientServiceInstanceV1[0].clientServiceRole[0].clientIdentity[0].action = ACTION_DELETE;
                clientServiceInstanceV1[0].clientServiceRole[0].action = ACTION_UPDATE;
                clientProfile.clientServiceInstanceV1 = clientServiceInstanceV1;
                clientProfileList.Add(clientProfile);
                #endregion

                #region RBSID profile
                clientProfile = new ClientProfileV1();
                clientProfile.client = new Client();
                clientProfile.client.action = ACTION_UPDATE;

                clientIdentityList = new List<ClientIdentity>();
                if (isNewRBSIDExists)
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = RBSID_NAMESPACE;
                    clientIdentity.value = parameters.NewRBSID;
                    clientIdentity.action = ACTION_SEARCH;
                    clientIdentityList.Add(clientIdentity);
                }
                else
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = RBSID_NAMESPACE;
                    clientIdentity.value = parameters.RBSID;
                    clientIdentity.action = ACTION_SEARCH;
                    clientIdentityList.Add(clientIdentity);

                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = RBSID_NAMESPACE;
                    clientIdentity.value = parameters.NewRBSID;
                    clientIdentity.action = ACTION_INSERT;
                    clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity.clientIdentityStatus.value = ACTIVE;

                    clientIdentityList.Add(clientIdentity);

                }
                clientProfile.client.clientIdentity = clientIdentityList.ToArray();

                clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BT_CONSUMER_BROADBAND_NAMEPACE;
                clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                clientServiceInstanceV1[0].action = ACTION_UPDATE;
                clientServiceInstanceV1[0].clientServiceRole = CreateServiceRoles(parameters, string.Empty, true, false, isNewRBSIDExists).ToArray();

                if (!string.IsNullOrEmpty(parameters.Hybrid_Tag))
                {
                    ClientServiceInstanceCharacteristic srvcinstChars = null;
                    List<ClientServiceInstanceCharacteristic> listInstanceChars = new List<ClientServiceInstanceCharacteristic>();

                    srvcinstChars = new ClientServiceInstanceCharacteristic();
                    srvcinstChars.name = "Hybrid_Tag";
                    srvcinstChars.value = parameters.Hybrid_Tag;
                    srvcinstChars.action = "FORCE_INS";
                    listInstanceChars.Add(srvcinstChars);

                    clientServiceInstanceV1[0].clientServiceInstanceCharacteristic = listInstanceChars.ToArray();
                }

                clientProfile.clientServiceInstanceV1 = clientServiceInstanceV1;
                clientProfileList.Add(clientProfile);
                #endregion

                batchProfileRequest.manageBatchProfilesV1Request.ManageBatchProfilesV1Req = clientProfileList.ToArray();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader = headerblock;
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e = new IspssAdapter.Dnp.E2E();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey, batchProfileRequest.SerializeObject());

                batchResponse = SpringDnpWrapper.manageBatchProfilesV1(batchProfileRequest, parameters.orderKey);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey, batchResponse.SerializeObject());

                if (batchResponse != null
                       && batchResponse.manageBatchProfilesV1Response != null
                       && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                       && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState != null
                       && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode != null
                       && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))
                    {
                        e2eData.endOutboundCall(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA);
                    }
                    else
                        e2eData.endOutboundCall(e2eData.toString());
                    if(!isNotificationnotRequired)
                        notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, Completed, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey);
                }
                else
                {
                    if (batchResponse != null && batchResponse.manageBatchProfilesV1Response != null
                        && batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res != null
                        && batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages != null
                        && batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages[0].description != null)
                    {
                        string errorMessage = batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages[0].description;

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);

                        if (batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        if (!isNotificationnotRequired)
                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true);
                        LogHelper.LogErrorMessage(Failed, bptmTxnId, "failed to Insert RBSID as role and identity in DnP" + parameters.orderKey);
                    }
                    else
                    {
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");

                        if (batchResponse != null && batchResponse.manageBatchProfilesV1Response != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA != null &&
                            !String.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))

                            e2eData.endOutboundCall(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        if (!isNotificationnotRequired)
                            notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true);
                        LogHelper.LogErrorMessage(NullResponseFromDnP, bptmTxnId, "Received null response in Insert RBSID as role and identity in DnP " + parameters.orderKey);
                    }
                }
            }

            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);

                if (batchResponse != null && batchResponse.manageBatchProfilesV1Response != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA != null &&
                            !String.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))

                    e2eData.endOutboundCall(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                if (!isNotificationnotRequired)
                    notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true);
                LogHelper.LogErrorMessage(ex.Message.ToString(), bptmTxnId, "exception in inserting RBSID to CHO service service method with paramters.orderKey as " + parameters.orderKey);
            }
            finally
            {
                parameters = null;
            }
        }

        /// <summary>
        /// insert new RBSID to CHO server as old rbsid value not inserted in the service.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="e2eData"></param>
        public void InsertRBSIDTOCHOService(BroadBandParameters parameters, string identity, string identityDomain, bool isNewRBSIDExists, MSEOOrderNotification notification, ref E2ETransaction e2eData,bool isNotificationnotRequired=false)
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

                if (isNewRBSIDExists)
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = RBSID_NAMESPACE;
                    clientIdentity.value = parameters.NewRBSID;
                    clientIdentity.action = ACTION_SEARCH;

                    clientIdentityList.Add(clientIdentity);
                }
                else
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    if (parameters.IsAHTDone)
                    {
                        clientIdentity.managedIdentifierDomain.value = BTCOM;
                        clientIdentity.value = parameters.BtOneId;
                    }
                    else if (!string.IsNullOrEmpty(identity) && !string.IsNullOrEmpty(identityDomain))
                    {
                        clientIdentity.managedIdentifierDomain.value = identityDomain;
                        clientIdentity.value = identity;
                    }
                    clientIdentity.action = ACTION_SEARCH;

                    clientIdentityList.Add(clientIdentity);

                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = RBSID_NAMESPACE;
                    clientIdentity.value = parameters.NewRBSID;
                    clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity.clientIdentityStatus.value = ACTIVE;
                    clientIdentity.action = ACTION_INSERT;

                    clientIdentityList.Add(clientIdentity);
                }

                ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                serviceIdentity[0] = new ServiceIdentity();
                serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity[0].value = parameters.Billingaccountnumber;
                serviceIdentity[0].action = ACTION_SEARCH;

                ClientServiceInstanceV1[] clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BT_CONSUMER_BROADBAND_NAMEPACE;
                clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                clientServiceInstanceV1[0].action = ACTION_UPDATE;

                if (!string.IsNullOrEmpty(parameters.Hybrid_Tag))
                {
                    ClientServiceInstanceCharacteristic srvcinstChars = null;
                    List<ClientServiceInstanceCharacteristic> listInstanceChars = new List<ClientServiceInstanceCharacteristic>();

                    srvcinstChars = new ClientServiceInstanceCharacteristic();
                    srvcinstChars.name = "Hybrid_Tag";
                    srvcinstChars.value = parameters.Hybrid_Tag;
                    srvcinstChars.action = "FORCE_INS";
                    listInstanceChars.Add(srvcinstChars);

                    clientServiceInstanceV1[0].clientServiceInstanceCharacteristic = listInstanceChars.ToArray();
                }

                //calling this method to get the service roles
                clientServiceInstanceV1[0].clientServiceRole = CreateServiceRoles(parameters, string.Empty, true, false, isNewRBSIDExists).ToArray();

                manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;

                profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;
                profileRequest.manageClientProfileV1Request.standardHeader = headerblock;

                profileRequest.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                profileRequest.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey, profileRequest.SerializeObject());

                profileResponse1 = DnpWrapper.manageClientProfileV1Thomas(profileRequest, parameters.orderKey);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey, profileResponse1.SerializeObject());

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
                    if (!isNotificationnotRequired)
                        notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, Completed, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey);
                }
                else
                {
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        if (!isNotificationnotRequired)
                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true);
                        LogHelper.LogErrorMessage(Failed, bptmTxnId, "failed to Insert RBSID as role and identity in DnP" + parameters.orderKey);
                    }
                    else
                    {
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        if (!isNotificationnotRequired)
                            notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true);
                        LogHelper.LogErrorMessage(NullResponseFromDnP, bptmTxnId, NullResponseFromDnP + "Received null response in Insert RBSID as role and identity in DnP " + parameters.orderKey);
                    }
                }
            }
            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());
                if (!isNotificationnotRequired)
                    notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true);
                LogHelper.LogErrorMessage(ex.Message.ToString(), bptmTxnId, "exception in inserting RBSID to CHO service service method with paramters.orderKey as " + parameters.orderKey);
            }
            finally
            {
                parameters = null;
            }
        }

        public void InsertMobileTOCHOService(BroadBandParameters parameters, MSEOOrderNotification notification, ref E2ETransaction e2eData)
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
                if (parameters.IsAHTDone)
                {
                    //NAYANAGL-70258 fix
                    //EE Profile journeys
                    if (parameters.isEEAccount)
                    {
                        if (!string.IsNullOrEmpty(parameters.EEOnlineId))
                        {
                            clientIdentity.managedIdentifierDomain.value = EEONLINEID;
                            clientIdentity.value = parameters.EEOnlineId;
                        }
                        else if (!string.IsNullOrEmpty(parameters.ConKid))
                        {
                            clientIdentity.managedIdentifierDomain.value = CONKID;
                            clientIdentity.value = parameters.ConKid;
                        }
                    }//BT journeys
                    else
                    {
                        clientIdentity.managedIdentifierDomain.value = BTCOM;
                        clientIdentity.value = parameters.BtOneId;
                    }
                }
                else if (!string.IsNullOrEmpty(parameters.Idenity) && !string.IsNullOrEmpty(parameters.IdenityDomain))
                {
                    clientIdentity.managedIdentifierDomain.value = parameters.IdenityDomain;
                    clientIdentity.value = parameters.Idenity;
                }
                clientIdentity.action = ACTION_SEARCH;

                clientIdentityList.Add(clientIdentity);

                clientIdentity = new ClientIdentity();
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = MOBILE_NAMESPACE;
                clientIdentity.value = GetMSISDN(parameters.MSISDN);
                clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                clientIdentity.clientIdentityStatus.value = ACTIVE;
                clientIdentity.action = ACTION_INSERT;

                clientIdentityList.Add(clientIdentity);

                ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                serviceIdentity[0] = new ServiceIdentity();
                serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity[0].value = parameters.Billingaccountnumber;
                serviceIdentity[0].action = ACTION_SEARCH;

                ClientServiceInstanceV1[] clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BT_CONSUMER_BROADBAND_NAMEPACE;
                clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                clientServiceInstanceV1[0].action = ACTION_UPDATE;

                if (!string.IsNullOrEmpty(parameters.Hybrid_Tag))
                {
                    ClientServiceInstanceCharacteristic srvcinstChars = null;
                    List<ClientServiceInstanceCharacteristic> listInstanceChars = new List<ClientServiceInstanceCharacteristic>();

                    srvcinstChars = new ClientServiceInstanceCharacteristic();
                    srvcinstChars.name = "Hybrid_Tag";
                    srvcinstChars.value = parameters.Hybrid_Tag;
                    srvcinstChars.action = "FORCE_INS";
                    listInstanceChars.Add(srvcinstChars);

                    clientServiceInstanceV1[0].clientServiceInstanceCharacteristic = listInstanceChars.ToArray();
                }

                //calling this method to get the service roles
                clientServiceInstanceV1[0].clientServiceRole = CreateServiceRoles(parameters, string.Empty, false, false, false, false, false, true, isDefaultRoleExists).ToArray();

                manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;

                profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;
                profileRequest.manageClientProfileV1Request.standardHeader = headerblock;

                profileRequest.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                profileRequest.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey, profileRequest.SerializeObject());

                profileResponse1 = DnpWrapper.manageClientProfileV1Thomas(profileRequest, parameters.orderKey);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey, profileResponse1.SerializeObject());

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
                    LogHelper.LogActivityDetails(bptmTxnId, guid, Completed, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey);
                }
                else
                {
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        
                        notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true);
                        LogHelper.LogErrorMessage(Failed, bptmTxnId, "failed to Insert Mobile as role and identity in DnP" + parameters.orderKey);
                    }
                    else
                    {
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true);
                        LogHelper.LogErrorMessage(NullResponseFromDnP, bptmTxnId, NullResponseFromDnP + "Received null response in Insert RBSID as role and identity in DnP " + parameters.orderKey);
                    }
                }
            }
            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());
                
                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true);
                LogHelper.LogErrorMessage(ex.Message.ToString(), bptmTxnId, "exception in inserting Mobile to CHO service service method with paramters.orderKey as " + parameters.orderKey);
            }
            finally
            {
                parameters = null;
            }
        }

        /// <summary>
        /// to update the hybrid tag value
        /// </summary>
        /// <param name="paramters"></param>
        /// <param name="notification"></param>
        /// <param name="serviceStatus"></param>
        /// <param name="e2eData"></param>
        public void UpdateHybridTagtoCHOPServiceStatus(BroadBandParameters paramters, MSEOOrderNotification notification, string serviceStatus, ref E2ETransaction e2eData)
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
                serviceInstance[0].clientServiceInstanceIdentifier.value = BT_CONSUMER_BROADBAND_NAMEPACE;
                serviceInstance[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                serviceInstance[0].action = ACTION_UPDATE;

                List<ServiceIdentity> listServiceIdentity = new List<ServiceIdentity>();
                ServiceIdentity serviceIdentity = new ServiceIdentity();
                serviceIdentity.domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity.value = paramters.Billingaccountnumber;
                serviceIdentity.action = ACTION_SEARCH;


                listServiceIdentity.Add(serviceIdentity);

                serviceInstance[0].serviceIdentity = listServiceIdentity.ToArray();

                if (!string.IsNullOrEmpty(paramters.Hybrid_Tag))
                {
                    ClientServiceInstanceCharacteristic srvcinstChars = null;
                    List<ClientServiceInstanceCharacteristic> listInstanceChars = new List<ClientServiceInstanceCharacteristic>();

                    srvcinstChars = new ClientServiceInstanceCharacteristic();
                    srvcinstChars.name = "Hybrid_Tag";
                    srvcinstChars.value = paramters.Hybrid_Tag;
                    srvcinstChars.action = "FORCE_INS";
                    listInstanceChars.Add(srvcinstChars);

                    serviceInstance[0].clientServiceInstanceCharacteristic = listInstanceChars.ToArray();
                }

                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                //manageServiceInstanceV1Req.clientServiceInstanceV1 = clientServiceInstance;
                profileRequest.manageServiceInstanceV1Request.standardHeader = headerBlock;

                manageServiceInstanceV1Req.clientServiceInstanceV1 = serviceInstance[0];

                profileRequest.manageServiceInstanceV1Request.manageServiceInstanceV1Req = manageServiceInstanceV1Req;

                //Logger.Write(paramters.orderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.BTPlusMarkerMessageTrace);

                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, profileRequest.SerializeObject());
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, paramters.orderKey, profileRequest.SerializeObject(), "BTConsumerBroadBand");

                profileResponse = DnpWrapper.manageServiceInstanceV1Thomas(profileRequest, paramters.orderKey);

                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, paramters.orderKey, profileResponse.SerializeObject(), "BTConsumerBroadBand");
                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, profileResponse.SerializeObject());

                if (profileResponse != null
                    && profileResponse.manageServiceInstanceV1Response != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                        e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, Completed, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                    //Logger.Write(paramters.orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                }
                else
                {
                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description;

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                        if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //sending failure notification
                        notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true);
                        LogHelper.LogErrorMessage(errorMessage, bptmTxnId, "failernotification in modify BTCBB service status method with paramters.orderKey as " + paramters.orderKey);

                    }
                    else
                    {
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //sending failure notification
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true);
                        LogHelper.LogErrorMessage(NullResponseFromDnP, bptmTxnId, "failernotification in modify BTCBB service status method with paramters.orderKey as " + paramters.orderKey);
                        //Logger.Write(paramters.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                    }
                }
            }

            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true);
                LogHelper.LogErrorMessage(ex.Message.ToString(), bptmTxnId, "exception in modify BTCBB service status method with paramters.orderKey as " + paramters.orderKey);
                //Logger.Write(paramters.orderKey + "," + Failed + "," + FailedErrorMessage + ex.Message, Logger.TypeEnum.BTPlusMarkerExceptionTrace);
                //Logger.Write(paramters.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.BTPlusMarkerMessageTrace);
            }
        }

        public void UpdateBBSIDinCHOService(BroadBandParameters parameters, string identity, string identityDomain, bool isNewBBSIDExists, ref E2ETransaction e2eData)
        {
            manageBatchProfilesV1Response1 batchResponse = null;
            manageBatchProfilesV1Request1 batchProfileRequest = null;
            try
            {
                batchProfileRequest = new manageBatchProfilesV1Request1();
                batchProfileRequest.manageBatchProfilesV1Request = new ManageBatchProfilesV1Request();

                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerblock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                headerblock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                headerblock.serviceAddressing.from = @"http://www.profile.com?SAASMSEO";
                headerblock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                headerblock.serviceState.stateCode = "OK";
                List<ClientProfileV1> clientProfileList = new List<ClientProfileV1>();
                ClientProfileV1 clientProfile = null;
                ClientServiceInstanceV1[] clientServiceInstanceV1 = null;
                List<ClientIdentity> clientIdentityList = null;
                ClientIdentity clientIdentity = null;

                ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                serviceIdentity[0] = new ServiceIdentity();
                serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity[0].value = parameters.Billingaccountnumber;
                serviceIdentity[0].action = ACTION_SEARCH;

                #region delete defaultrole
                clientProfile = new ClientProfileV1();
                clientProfile.client = new Client();
                clientProfile.client.action = ACTION_UPDATE;

                clientIdentityList = new List<ClientIdentity>();
                clientIdentity = new ClientIdentity();
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = BBSID_NAMESPACE;
                clientIdentity.value = parameters.BBSID;
                clientIdentity.action = ACTION_SEARCH;
                clientIdentityList.Add(clientIdentity);
                clientProfile.client.clientIdentity = clientIdentityList.ToArray();

                clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BT_CONSUMER_BROADBAND_NAMEPACE;
                clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                clientServiceInstanceV1[0].action = ACTION_UPDATE;
                clientServiceInstanceV1[0].clientServiceRole = new ClientServiceRole[1];
                clientServiceInstanceV1[0].clientServiceRole[0] = new ClientServiceRole();
                clientServiceInstanceV1[0].clientServiceRole[0].id = DEFAULT_ROLE;
                clientServiceInstanceV1[0].clientServiceRole[0].clientIdentity = new ClientIdentity[1];
                clientServiceInstanceV1[0].clientServiceRole[0].clientIdentity[0] = new ClientIdentity();
                clientServiceInstanceV1[0].clientServiceRole[0].clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                clientServiceInstanceV1[0].clientServiceRole[0].clientIdentity[0].managedIdentifierDomain.value = BBSID_NAMESPACE;
                clientServiceInstanceV1[0].clientServiceRole[0].clientIdentity[0].value = parameters.BBSID;
                clientServiceInstanceV1[0].clientServiceRole[0].clientIdentity[0].action = ACTION_DELETE;
                if(parameters.IsBBSIDRoleDelete)
                    clientServiceInstanceV1[0].clientServiceRole[0].action = ACTION_DELETE;
                else
                    clientServiceInstanceV1[0].clientServiceRole[0].action = ACTION_UPDATE;
                clientProfile.clientServiceInstanceV1 = clientServiceInstanceV1;
                clientProfileList.Add(clientProfile);
                #endregion

                #region BBSID profile
                clientProfile = new ClientProfileV1();
                clientProfile.client = new Client();
                clientProfile.client.action = ACTION_UPDATE;

                clientIdentityList = new List<ClientIdentity>();
                if (isNewBBSIDExists)
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BBSID_NAMESPACE;
                    clientIdentity.value = parameters.NewBBSID;
                    clientIdentity.action = ACTION_SEARCH;
                    clientIdentityList.Add(clientIdentity);
                }
                else
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    if (parameters.IsAHTDone)
                    {
                        clientIdentity.managedIdentifierDomain.value = BTCOM;
                        clientIdentity.value = parameters.BtOneId;
                    }
                    else if (!string.IsNullOrEmpty(identity) && !string.IsNullOrEmpty(identityDomain))
                    {
                        clientIdentity.managedIdentifierDomain.value = identityDomain;
                        clientIdentity.value = identity;
                    }
                    clientIdentity.action = ACTION_SEARCH;

                    clientIdentityList.Add(clientIdentity);

                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BBSID_NAMESPACE;
                    clientIdentity.value = parameters.NewBBSID;
                    clientIdentity.action = ACTION_INSERT;
                    clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity.clientIdentityStatus.value = ACTIVE;

                    clientIdentityList.Add(clientIdentity);

                }
                clientProfile.client.clientIdentity = clientIdentityList.ToArray();

                clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BT_CONSUMER_BROADBAND_NAMEPACE;
                clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                clientServiceInstanceV1[0].action = ACTION_UPDATE;
                clientServiceInstanceV1[0].clientServiceRole = CreateServiceRoles(parameters, string.Empty, true, false, isNewBBSIDExists, false, true).ToArray();

                if (!string.IsNullOrEmpty(parameters.Hybrid_Tag))
                {
                    ClientServiceInstanceCharacteristic srvcinstChars = null;
                    List<ClientServiceInstanceCharacteristic> listInstanceChars = new List<ClientServiceInstanceCharacteristic>();

                    srvcinstChars = new ClientServiceInstanceCharacteristic();
                    srvcinstChars.name = "Hybrid_Tag";
                    srvcinstChars.value = parameters.Hybrid_Tag;
                    srvcinstChars.action = "FORCE_INS";
                    listInstanceChars.Add(srvcinstChars);

                    clientServiceInstanceV1[0].clientServiceInstanceCharacteristic = listInstanceChars.ToArray();
                }

                clientProfile.clientServiceInstanceV1 = clientServiceInstanceV1;
                clientProfileList.Add(clientProfile);
                #endregion

                batchProfileRequest.manageBatchProfilesV1Request.ManageBatchProfilesV1Req = clientProfileList.ToArray();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader = headerblock;
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e = new IspssAdapter.Dnp.E2E();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey, batchProfileRequest.SerializeObject());

                batchResponse = SpringDnpWrapper.manageBatchProfilesV1(batchProfileRequest, parameters.orderKey);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey, batchResponse.SerializeObject());

                if (batchResponse != null
                       && batchResponse.manageBatchProfilesV1Response != null
                       && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                       && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState != null
                       && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode != null
                       && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))
                    {
                        e2eData.endOutboundCall(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA);
                    }
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                   // notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, Completed, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey);
                }
                else
                {
                    if (batchResponse != null && batchResponse.manageBatchProfilesV1Response != null
                        && batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res != null
                        && batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages != null
                        && batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages[0].description != null)
                    {
                        string errorMessage = batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages[0].description;

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);

                        if (batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        //notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true);
                        LogHelper.LogErrorMessage(Failed, bptmTxnId, "failed to Insert BBSID as role and identity in DnP" + parameters.orderKey);
                    }
                    else
                    {
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");

                        if (batchResponse != null && batchResponse.manageBatchProfilesV1Response != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA != null &&
                            !String.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))

                            e2eData.endOutboundCall(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true);
                        LogHelper.LogErrorMessage(NullResponseFromDnP, bptmTxnId, "Received null response in Insert BBSID as role and identity in DnP " + parameters.orderKey);
                    }
                }
            }

            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);

                if (batchResponse != null && batchResponse.manageBatchProfilesV1Response != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA != null &&
                            !String.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))

                    e2eData.endOutboundCall(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                //notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true);
                LogHelper.LogErrorMessage(ex.Message.ToString(), bptmTxnId, "exception in inserting BBSID to CHO service service method with paramters.orderKey as " + parameters.orderKey);
            }
            finally
            {
                parameters = null;
            }
        }

        public void InsertBBSIDTOCHOService(BroadBandParameters parameters, string identity, string identityDomain, bool isNewBBSIDExists, ref E2ETransaction e2eData)
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

                if (isNewBBSIDExists)
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BBSID_NAMESPACE;
                    clientIdentity.value = parameters.NewBBSID;
                    clientIdentity.action = ACTION_SEARCH;

                    clientIdentityList.Add(clientIdentity);
                }
                else
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    if (parameters.IsAHTDone)
                    {
                        clientIdentity.managedIdentifierDomain.value = BTCOM;
                        clientIdentity.value = parameters.BtOneId;
                    }
                    else if (!string.IsNullOrEmpty(identity) && !string.IsNullOrEmpty(identityDomain))
                    {
                        clientIdentity.managedIdentifierDomain.value = identityDomain;
                        clientIdentity.value = identity;
                    }
                    clientIdentity.action = ACTION_SEARCH;

                    clientIdentityList.Add(clientIdentity);

                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BBSID_NAMESPACE;
                    clientIdentity.value = parameters.NewBBSID;
                    clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity.clientIdentityStatus.value = ACTIVE;
                    clientIdentity.action = ACTION_INSERT;

                    clientIdentityList.Add(clientIdentity);
                }

                ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                serviceIdentity[0] = new ServiceIdentity();
                serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity[0].value = parameters.Billingaccountnumber;
                serviceIdentity[0].action = ACTION_SEARCH;

                ClientServiceInstanceV1[] clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BT_CONSUMER_BROADBAND_NAMEPACE;
                clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                clientServiceInstanceV1[0].action = ACTION_UPDATE;

                if (!string.IsNullOrEmpty(parameters.Hybrid_Tag))
                {
                    ClientServiceInstanceCharacteristic srvcinstChars = null;
                    List<ClientServiceInstanceCharacteristic> listInstanceChars = new List<ClientServiceInstanceCharacteristic>();

                    srvcinstChars = new ClientServiceInstanceCharacteristic();
                    srvcinstChars.name = "Hybrid_Tag";
                    srvcinstChars.value = parameters.Hybrid_Tag;
                    srvcinstChars.action = "FORCE_INS";
                    listInstanceChars.Add(srvcinstChars);

                    clientServiceInstanceV1[0].clientServiceInstanceCharacteristic = listInstanceChars.ToArray();
                }

                //calling this method to get the service roles
                clientServiceInstanceV1[0].clientServiceRole = CreateServiceRoles(parameters, string.Empty, true, false, isNewBBSIDExists, false, true).ToArray();

                manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;

                profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;
                profileRequest.manageClientProfileV1Request.standardHeader = headerblock;

                profileRequest.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                profileRequest.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey, profileRequest.SerializeObject());

                profileResponse1 = DnpWrapper.manageClientProfileV1Thomas(profileRequest, parameters.orderKey);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey, profileResponse1.SerializeObject());

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

                    //notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, Completed, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey);
                }
                else
                {
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        //notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true);
                        LogHelper.LogErrorMessage(Failed, bptmTxnId, "failed to Insert BBSID as role and identity in DnP" + parameters.orderKey);
                    }
                    else
                    {
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        //notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true);
                        LogHelper.LogErrorMessage(NullResponseFromDnP, bptmTxnId, NullResponseFromDnP + "Received null response in Insert BBSID as role and identity in DnP " + parameters.orderKey);
                    }
                }
            }
            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                //notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true);
                LogHelper.LogErrorMessage(ex.Message.ToString(), bptmTxnId, "exception in inserting BBSID to CHO service service method with paramters.orderKey as " + parameters.orderKey);
            }
            finally
            {
                parameters = null;
            }
        }

        ///// <summary>
        ///// 
        ///// </summary>
        //public void ModifyRBSIDinCHOP(BroadBandParameters parameters,ref E2ETransaction e2eData)
        //{           
        //    manageClientIdentityResponse1 resp = new manageClientIdentityResponse1();
        //    manageClientIdentityRequest1 req = new manageClientIdentityRequest1();
        //    ManageClientIdentityRequest manageclientidentityreq = new ManageClientIdentityRequest();
        //    manageclientidentityreq.manageClientIdentityReq = new ManageClientIdentityReq();
        //    try
        //    {
        //        BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerblock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
        //        headerblock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
        //        headerblock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
        //        headerblock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
        //        headerblock.serviceState.stateCode = "OK";

        //        manageclientidentityreq.manageClientIdentityReq.clientSearchCriteria = new ClientSearchCriteria();
        //        manageclientidentityreq.manageClientIdentityReq.clientSearchCriteria.identifierDomainCode = RBSID_NAMESPACE;
        //        manageclientidentityreq.manageClientIdentityReq.clientSearchCriteria.identifierValue = parameters.RBSID;

        //        manageclientidentityreq.manageClientIdentityReq.clientIdentity = new ClientIdentity();
        //        manageclientidentityreq.manageClientIdentityReq.clientIdentity.action = "UPDATE";
        //        manageclientidentityreq.manageClientIdentityReq.clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
        //        manageclientidentityreq.manageClientIdentityReq.clientIdentity.managedIdentifierDomain.value = RBSID_NAMESPACE;
        //        manageclientidentityreq.manageClientIdentityReq.clientIdentity.value = parameters.NewRBSID;

        //        e2eData.logMessage(StartedDnPCall, "");
        //        e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, BT_CONSUMER_BROADBAND_NAMEPACE);

        //        headerblock.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
        //        headerblock.e2e.E2EDATA = e2eData.toString();

        //        manageclientidentityreq.standardHeader = headerblock;
        //        req.manageClientIdentityRequest = manageclientidentityreq;                

        //        resp = SpringDnpWrapper.manageClientIdentity(req, parameters.orderKey);               

        //        if (resp != null
        //           && resp.manageClientIdentityResponse != null
        //           && resp.manageClientIdentityResponse.standardHeader != null
        //           && resp.manageClientIdentityResponse.standardHeader.serviceState != null
        //           && resp.manageClientIdentityResponse.standardHeader.serviceState.stateCode != null
        //           && resp.manageClientIdentityResponse.standardHeader.serviceState.stateCode == "0")
        //        {
        //            e2eData.logMessage(GotResponseFromDnP, "");
        //            if (resp.manageClientIdentityResponse.standardHeader.e2e != null && !string.IsNullOrEmpty(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA))
        //            {
        //                e2eData.endOutboundCall(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA);
        //            }
        //            else
        //                e2eData.endOutboundCall(e2eData.toString());

        //            notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);

        //        }
        //        else
        //        {
        //            if (resp != null && resp.manageClientIdentityResponse != null
        //                && resp.manageClientIdentityResponse.manageClientIdentityRes != null
        //                && resp.manageClientIdentityResponse.manageClientIdentityRes.messages != null
        //                && resp.manageClientIdentityResponse.manageClientIdentityRes.messages[0].description != null)
        //            {
        //                string errorMessage = resp.manageClientIdentityResponse.manageClientIdentityRes.messages[0].description;
        //                e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
        //                if (resp.manageClientIdentityResponse.standardHeader.e2e != null && !string.IsNullOrEmpty(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA))
        //                    e2eData.endOutboundCall(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA);
        //                else
        //                    e2eData.endOutboundCall(e2eData.toString());

        //                notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true);

        //            }
        //            else
        //            {
        //                Logger.Write(orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.SpringMessageTrace);
        //                e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
        //                if (resp != null && resp.manageClientIdentityResponse != null
        //                    && resp.manageClientIdentityResponse.standardHeader != null
        //                    && resp.manageClientIdentityResponse.standardHeader.e2e != null
        //                    && resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA != null &&
        //                    !String.IsNullOrEmpty(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA))
        //                    e2eData.endOutboundCall(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA);
        //                else
        //                    e2eData.endOutboundCall(e2eData.toString());

        //                notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true);

        //            }
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);

        //        if (resp != null && resp.manageClientIdentityResponse != null
        //            && resp.manageClientIdentityResponse.standardHeader != null &&
        //            resp.manageClientIdentityResponse.standardHeader.e2e != null &&
        //            resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA != null &&
        //            !String.IsNullOrEmpty(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA))

        //            e2eData.endOutboundCall(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA);
        //        else
        //            e2eData.endOutboundCall(e2eData.toString());

        //        notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true);

        //    }
        //}      

        public void DeleteCHOPService(BroadBandParameters parameters, MSEOOrderNotification notification, ref E2ETransaction e2eData, bool bbisdexist, bool rbisexist, bool mobileexist,bool _isbbsidclientdelete)
        {
            if (!parameters.IsAHTDone)
            {
               // DeletenonAHTCHOPService(parameters, notification, ref e2eData, bbisdexist, rbisexist, mobileexist, _isbbsidclientdelete);
            }
            else
            {
                manageBatchProfilesV1Response1 batchResponse = null;
                manageBatchProfilesV1Request1 batchProfileRequest = null;
                try
                {
                    batchProfileRequest = new manageBatchProfilesV1Request1();
                    batchProfileRequest.manageBatchProfilesV1Request = new ManageBatchProfilesV1Request();

                    BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerblock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                    headerblock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                    headerblock.serviceAddressing.from = @"http://www.profile.com?SAASMSEO";
                    headerblock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                    headerblock.serviceState.stateCode = "OK";
                    List<ClientProfileV1> clientProfileList = new List<ClientProfileV1>();
                    ClientProfileV1 clientProfile = null;
                    ClientServiceInstanceV1[] clientServiceInstanceV1 = null;
                    List<ClientIdentity> clientIdentityList = null;
                    ClientIdentity clientIdentity = null;

                    ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                    serviceIdentity[0] = new ServiceIdentity();
                    serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                    serviceIdentity[0].value = parameters.Billingaccountnumber;
                    serviceIdentity[0].action = ACTION_SEARCH;

                    #region Service Deletion
                    clientProfile = new ClientProfileV1();
                    clientProfile.client = new Client();
                    clientProfile.client.action = ACTION_UPDATE;

                    clientIdentityList = new List<ClientIdentity>();
                    if (parameters.IsAHTDone)
                    {
                        clientIdentity = new ClientIdentity();
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                        clientIdentity.value = parameters.Billingaccountnumber;
                        clientIdentity.action = ACTION_SEARCH;
                        clientIdentityList.Add(clientIdentity);
                    }
                    else if (!string.IsNullOrEmpty(parameters.RBSID))
                    {
                        clientIdentity = new ClientIdentity();
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientIdentity.managedIdentifierDomain.value = RBSID_NAMESPACE;
                        clientIdentity.value = parameters.RBSID;
                        clientIdentity.action = ACTION_SEARCH;
                        clientIdentityList.Add(clientIdentity);
                    }
                    else if (!string.IsNullOrEmpty(parameters.MSISDN))
                    {
                        clientIdentity = new ClientIdentity();
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientIdentity.managedIdentifierDomain.value = MOBILE_NAMESPACE;
                        clientIdentity.value = parameters.MSISDN;
                        clientIdentity.action = ACTION_SEARCH;
                        clientIdentityList.Add(clientIdentity);
                    }
                    clientProfile.client.clientIdentity = clientIdentityList.ToArray();
                    clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                    clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                    clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                    clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BT_CONSUMER_BROADBAND_NAMEPACE;
                    clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                    clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                    clientServiceInstanceV1[0].action = ACTION_DELETE;
                    clientProfile.clientServiceInstanceV1 = clientServiceInstanceV1;
                    clientProfileList.Add(clientProfile);
                    #endregion

                    if (rbisexist)
                    {
                        #region delete RBSID Identity
                        clientProfile = new ClientProfileV1();
                        clientProfile.client = new Client();
                        clientProfile.client.action = ACTION_UPDATE;

                        clientIdentityList = new List<ClientIdentity>();
                        clientIdentity = new ClientIdentity();
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientIdentity.managedIdentifierDomain.value = RBSID_NAMESPACE;
                        clientIdentity.value = parameters.RBSID;
                        clientIdentity.action = ACTION_SEARCH;
                        clientIdentityList.Add(clientIdentity);
                        //  clientProfile.client.clientIdentity = clientIdentityList.ToArray();


                        clientIdentity = new ClientIdentity();
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientIdentity.managedIdentifierDomain.value = RBSID_NAMESPACE;
                        clientIdentity.value = parameters.RBSID;
                        clientIdentity.action = ACTION_DELETE;
                        clientIdentityList.Add(clientIdentity);
                        clientProfile.client.clientIdentity = clientIdentityList.ToArray();
                        clientProfileList.Add(clientProfile);
                        #endregion
                    }
                    if (bbisdexist)
                    {
                        #region delete BBSID Identity
                        clientProfile = new ClientProfileV1();
                        clientProfile.client = new Client();
                        clientProfile.client.action = _isbbsidclientdelete ? ACTION_DELETE : ACTION_UPDATE;

                        clientIdentityList = new List<ClientIdentity>();
                        clientIdentity = new ClientIdentity();
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientIdentity.managedIdentifierDomain.value = BBSID_NAMESPACE;
                        clientIdentity.value = parameters.BBSID;
                        clientIdentity.action = ACTION_SEARCH;
                        clientIdentityList.Add(clientIdentity);


                        if (!_isbbsidclientdelete)
                        {
                            clientIdentity = new ClientIdentity();
                            clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                            clientIdentity.managedIdentifierDomain.value = BBSID_NAMESPACE;
                            clientIdentity.value = parameters.BBSID;
                            clientIdentity.action = ACTION_DELETE;
                            clientIdentityList.Add(clientIdentity);
                        }
                        clientProfile.client.clientIdentity = clientIdentityList.ToArray();
                        clientProfileList.Add(clientProfile);
                        #endregion
                    }
                    if (mobileexist)
                    {
                        #region delete MOBILE Identity
                        clientProfile = new ClientProfileV1();
                        clientProfile.client = new Client();
                        clientProfile.client.action = ACTION_UPDATE;

                        clientIdentityList = new List<ClientIdentity>();
                        clientIdentity = new ClientIdentity();
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientIdentity.managedIdentifierDomain.value = MOBILE_NAMESPACE;
                        clientIdentity.value = parameters.MSISDN;
                        clientIdentity.action = ACTION_SEARCH;
                        clientIdentityList.Add(clientIdentity);
                        //  clientProfile.client.clientIdentity = clientIdentityList.ToArray();


                        clientIdentity = new ClientIdentity();
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientIdentity.managedIdentifierDomain.value = MOBILE_NAMESPACE;
                        clientIdentity.value = parameters.MSISDN;
                        clientIdentity.action = ACTION_DELETE;
                        clientIdentityList.Add(clientIdentity);
                        clientProfile.client.clientIdentity = clientIdentityList.ToArray();
                        clientProfileList.Add(clientProfile);
                        #endregion
                    }

                    batchProfileRequest.manageBatchProfilesV1Request.ManageBatchProfilesV1Req = clientProfileList.ToArray();
                    batchProfileRequest.manageBatchProfilesV1Request.standardHeader = headerblock;
                    batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e = new IspssAdapter.Dnp.E2E();
                    batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                    LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey, batchProfileRequest.SerializeObject());

                    batchResponse = SpringDnpWrapper.manageBatchProfilesV1(batchProfileRequest, parameters.orderKey);

                    LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey, batchResponse.SerializeObject());

                    if (batchResponse != null
                           && batchResponse.manageBatchProfilesV1Response != null
                           && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                           && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState != null
                           && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode != null
                           && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode == "0")
                    {
                        e2eData.logMessage(GotResponseFromDnP, "");
                        if (batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))
                        {
                            e2eData.endOutboundCall(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA);
                        }
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                        LogHelper.LogActivityDetails(bptmTxnId, guid, Completed, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey);
                    }
                    else
                    {
                        if (batchResponse != null && batchResponse.manageBatchProfilesV1Response != null
                            && batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res != null
                            && batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages != null
                            && batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages[0].description != null)
                        {
                            string errorMessage = batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages[0].description;

                            e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);

                            if (batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))
                                e2eData.endOutboundCall(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA);
                            else
                                e2eData.endOutboundCall(e2eData.toString());

                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true);
                            LogHelper.LogErrorMessage(Failed, bptmTxnId, "failed to Delete CHOP service and identities in DnP" + parameters.orderKey);
                        }
                        else
                        {
                            e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");

                            if (batchResponse != null && batchResponse.manageBatchProfilesV1Response != null
                                && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                                && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null
                                && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA != null &&
                                !String.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))

                                e2eData.endOutboundCall(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA);
                            else
                                e2eData.endOutboundCall(e2eData.toString());
                            notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true);
                            LogHelper.LogErrorMessage(NullResponseFromDnP, bptmTxnId, "Received null response in Delete CHOP service and identities in DnP" + parameters.orderKey);
                        }
                    }
                }

                catch (Exception ex)
                {
                    e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);

                    if (batchResponse != null && batchResponse.manageBatchProfilesV1Response != null
                                && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                                && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null
                                && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA != null &&
                                !String.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))

                        e2eData.endOutboundCall(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true);
                    LogHelper.LogErrorMessage(ex.Message.ToString(), bptmTxnId, "exception in Delete CHOP service and identities in DnP with paramters.orderKey as " + parameters.orderKey);
                }
                finally
                {
                    parameters = null;
                }
            }
        }
        /// <summary>
        /// To Delete the CHOP service and Mobile identity as part of CCPE2E-150809
        /// </summary>
        /// <param name="paramters"></param>
        /// <param name="notification"></param>
        /// <param name="e2eData"></param>
        public void MCPCalltoDeleteCHOPService(BroadBandParameters paramters, MSEOOrderNotification notification, ref E2ETransaction e2eData)
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

                List<ClientIdentity> clientIdentityList = new List<ClientIdentity>();

                ClientIdentity clientIdentity = null;


                manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_UPDATE;


                if (!string.IsNullOrEmpty(paramters.MSISDN))
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = MOBILE_NAMESPACE;
                    clientIdentity.value = paramters.MSISDN;
                    clientIdentity.action = ACTION_SEARCH;
                    clientIdentityList.Add(clientIdentity);

                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = MOBILE_NAMESPACE;
                    clientIdentity.value = paramters.MSISDN;
                    clientIdentity.action = ACTION_DELETE;
                    clientIdentityList.Add(clientIdentity);
                }


                ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                serviceIdentity[0] = new ServiceIdentity();
                serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity[0].value = paramters.Billingaccountnumber;
                serviceIdentity[0].action = ACTION_SEARCH;

                ClientServiceInstanceV1[] clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BT_CONSUMER_BROADBAND_NAMEPACE;
                clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                clientServiceInstanceV1[0].action = ACTION_DELETE;

                manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;

                profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;
                profileRequest.manageClientProfileV1Request.standardHeader = headerblock;

                //Logger.Write(paramters.orderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.BTPlusMarkerMessageTrace);// need to add message trace 
                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, profileRequest.SerializeObject());
                //MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, paramters.orderKey, profileRequest.SerializeObject(), "BTConsumerBroadBand");

                profileResponse1 = DnpWrapper.manageClientProfileV1Thomas(profileRequest, paramters.orderKey);

                // MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, paramters.orderKey, profileResponse1.SerializeObject(), "BTConsumerBroadBand");
                //Logger.Write(paramters.orderKey + "," + GotResponseFromDnP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.BTPlusMarkerMessageTrace);                
                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey, profileResponse1.SerializeObject());

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
                    LogHelper.LogActivityDetails(bptmTxnId, guid, Completed, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", paramters.orderKey);
                    //Logger.Write(paramters.orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                }
                else
                {
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;

                        //Logger.Write(paramters.orderKey + "," + Failed + "," + FailedErrorMessage + errorMessage, Logger.TypeEnum.BTPlusMarkerExceptionTrace);

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //sending failure notification
                        notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true);
                        //LogHelper.LogErrorMessage(errorMessage, bptmTxnId, "failed notification sent in create BTCBB service with error as: " + profileResponse1.SerializeObject() + "\n with orderkey as: " + paramters.orderKey);
                        LogHelper.LogErrorMessage(Failed, bptmTxnId, "failed notification sent in Delete CHOP service with orderKey as " + paramters.orderKey);

                    }
                    else
                    {
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        //sending failure notification
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true);
                        LogHelper.LogErrorMessage(NullResponseFromDnP, bptmTxnId, NullResponseFromDnP + "failed notification sent in Delete CHOP service with orderKey as " + paramters.orderKey);
                        //Logger.Write(paramters.orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.BTPlusMarkerMessageTrace);
                    }
                }

            }
            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true);
                LogHelper.LogErrorMessage(ex.Message.ToString(), bptmTxnId, "exception in Delete CHOP service with orderKey as " + paramters.orderKey);
            }
            finally
            {
                paramters = null;
            }
        }
        /// <summary>
        /// To Delete the CHOP service and Mobile identity as part of CCPE2E-150809
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="notification"></param>
        /// <param name="e2eData"></param>
        public void DeletenonAHTCHOPService(BroadBandParameters parameters, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            manageBatchProfilesV1Response1 batchResponse = null;
            manageBatchProfilesV1Request1 batchProfileRequest = null;
            try
            {
                batchProfileRequest = new manageBatchProfilesV1Request1();
                batchProfileRequest.manageBatchProfilesV1Request = new ManageBatchProfilesV1Request();

                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerblock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                headerblock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                headerblock.serviceAddressing.from = @"http://www.profile.com?SAASMSEO";
                headerblock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                headerblock.serviceState.stateCode = "OK";
                List<ClientProfileV1> clientProfileList = new List<ClientProfileV1>();
                ClientProfileV1 clientProfile = null;
                ClientServiceInstanceV1[] clientServiceInstanceV1 = null;
                List<ClientIdentity> clientIdentityList = null;
                ClientIdentity clientIdentity = null;

                ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                serviceIdentity[0] = new ServiceIdentity();
                serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity[0].value = parameters.Billingaccountnumber;
                serviceIdentity[0].action = ACTION_SEARCH;

                #region Service Deletion
                clientProfile = new ClientProfileV1();
                clientProfile.client = new Client();
                clientProfile.client.action = ACTION_UPDATE;
                clientIdentityList = new List<ClientIdentity>();
                if (!string.IsNullOrEmpty(parameters.MSISDN))
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = MOBILE_NAMESPACE;
                    clientIdentity.value = GetMSISDN(parameters.MSISDN);
                    clientIdentity.action = ACTION_SEARCH;
                    clientIdentityList.Add(clientIdentity);
                }   
                else if (!string.IsNullOrEmpty(parameters.RBSID))
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = RBSID_NAMESPACE;
                    clientIdentity.value = parameters.RBSID;
                    clientIdentity.action = ACTION_SEARCH;
                    clientIdentityList.Add(clientIdentity);
                }
                else if (!string.IsNullOrEmpty(parameters.BBSID))
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = BBSID_NAMESPACE;
                    clientIdentity.value = parameters.BBSID;
                    clientIdentity.action = ACTION_SEARCH;
                    clientIdentityList.Add(clientIdentity);
                }

                clientProfile.client.clientIdentity = clientIdentityList.ToArray();
                clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = BT_CONSUMER_BROADBAND_NAMEPACE;
                clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                clientServiceInstanceV1[0].action = ACTION_DELETE;
                clientProfile.clientServiceInstanceV1 = clientServiceInstanceV1;
                clientProfileList.Add(clientProfile);
                #endregion
                #region delete mobile/RBSID Identity along with client.

                if (!String.IsNullOrEmpty(parameters.MSISDN))
                {
                    clientProfile = new ClientProfileV1();
                    clientProfile.client = new Client();
                    clientProfile.client.action = ACTION_UPDATE;

                    clientIdentityList = new List<ClientIdentity>();


                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = MOBILE_NAMESPACE;
                    clientIdentity.value = GetMSISDN(parameters.MSISDN);
                    clientIdentity.action = ACTION_SEARCH;
                    clientIdentityList.Add(clientIdentity);

                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = MOBILE_NAMESPACE;
                    clientIdentity.value = GetMSISDN(parameters.MSISDN);
                    clientIdentity.action = ACTION_DELETE;
                    clientIdentityList.Add(clientIdentity);

                    clientProfile.client.clientIdentity = clientIdentityList.ToArray();
                    clientProfileList.Add(clientProfile);
                }
                
                #endregion                

                batchProfileRequest.manageBatchProfilesV1Request.ManageBatchProfilesV1Req = clientProfileList.ToArray();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader = headerblock;
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e = new IspssAdapter.Dnp.E2E();
                batchProfileRequest.manageBatchProfilesV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey, batchProfileRequest.SerializeObject());

                batchResponse = SpringDnpWrapper.manageBatchProfilesV1(batchProfileRequest, parameters.orderKey);

                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey, batchResponse.SerializeObject());

                if (batchResponse != null
                       && batchResponse.manageBatchProfilesV1Response != null
                       && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                       && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState != null
                       && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode != null
                       && batchResponse.manageBatchProfilesV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))
                    {
                        e2eData.endOutboundCall(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA);
                    }
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData, true);
                    LogHelper.LogActivityDetails(bptmTxnId, guid, Completed, System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey);
                }
                else
                {
                    if (batchResponse != null && batchResponse.manageBatchProfilesV1Response != null
                        && batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res != null
                        && batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages != null
                        && batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages[0].description != null)
                    {
                        string errorMessage = batchResponse.manageBatchProfilesV1Response.manageBatchProfilesV1Res.messages[0].description;

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);

                        if (batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        notification.sendNotification(false, false, "777", errorMessage, ref e2eData, true);
                        LogHelper.LogErrorMessage(Failed, bptmTxnId, "failed to Delete CHOP service and identities in DnP" + parameters.orderKey);
                    }
                    else
                    {
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");

                        if (batchResponse != null && batchResponse.manageBatchProfilesV1Response != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA != null &&
                            !String.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))

                            e2eData.endOutboundCall(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData, true);
                        LogHelper.LogErrorMessage(NullResponseFromDnP, bptmTxnId, "Received null response in Delete CHOP service and identities in DnP" + parameters.orderKey);
                    }
                }
            }

            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);

                if (batchResponse != null && batchResponse.manageBatchProfilesV1Response != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA != null &&
                            !String.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))

                    e2eData.endOutboundCall(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData, true);
                LogHelper.LogErrorMessage(ex.Message.ToString(), bptmTxnId, "exception in Delete CHOP service and identities in DnP with paramters.orderKey as " + parameters.orderKey);
            }
            finally
            {
                parameters = null;
            }
        }


        public void LinkClientProfile(BroadBandParameters parameters, ref E2ETransaction e2eData, string identityValue, string identityDomain, bool isBBRegrade)
        {
            linkClientProfilesResponse1 profileResponse = null;
            linkClientProfilesRequest1 lcpRequest = new linkClientProfilesRequest1();
            LinkClientProfilesRequest request = new LinkClientProfilesRequest();
            try
            {
                request.primaryClientIdentity = new ClientIdentity();
                request.primaryClientIdentity.action = ACTION_SEARCH;
                request.primaryClientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                request.primaryClientIdentity.managedIdentifierDomain.value = identityDomain;
                request.primaryClientIdentity.value = identityValue;

                request.linkClientIdentity = new ClientIdentity();
                request.linkClientIdentity.action = "MERGE";

                request.linkClientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                request.linkClientIdentity.managedIdentifierDomain.value = BBSID_NAMESPACE;
                if (isBBRegrade)
                    request.linkClientIdentity.value = parameters.NewBBSID;
                else
                    request.linkClientIdentity.value = parameters.BBSID;

                request.linkClientIdentity.clientCredential = new ClientCredential[1];
                request.linkClientIdentity.clientCredential[0] = new ClientCredential();
                request.linkClientIdentity.clientCredential[0].credentialValue = "AHTMergeIgnorePassword";
                request.linkClientIdentity.clientCredential[0].clientCredentialStatus = new ClientCredentialStatus();
                request.linkClientIdentity.clientCredential[0].clientCredentialStatus.value = "ACTIVE";


                request.action = "MERGE";

                IspssAdapter.Dnp.StandardHeaderBlock headerblock = new IspssAdapter.Dnp.StandardHeaderBlock();
                headerblock.serviceAddressing = new IspssAdapter.Dnp.ServiceAddressing();
                headerblock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                headerblock.serviceState = new IspssAdapter.Dnp.ServiceState();
                headerblock.serviceState.stateCode = "OK";
                request.standardHeader = headerblock;

                request.standardHeader.e2e = new IspssAdapter.Dnp.E2E();
                request.standardHeader.e2e.E2EDATA = e2eData.toString();
                lcpRequest.linkClientProfilesRequest = request;
                
                LogHelper.LogActivityDetails(bptmTxnId, guid, MakingDnPCall + " to link BBSID to existing chop profile ", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey, lcpRequest.SerializeObject());
                
                profileResponse = SpringDnpWrapper.LinkClientProfileV1Call(lcpRequest);
                                      
                LogHelper.LogActivityDetails(bptmTxnId, guid, GotResponseFromDnP + " to link BBSID to existing chop profile ", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey, profileResponse.SerializeObject());               
                
                if (profileResponse != null
                && profileResponse.linkClientProfilesResponse != null
                && profileResponse.linkClientProfilesResponse.standardHeader != null
                && profileResponse.linkClientProfilesResponse.standardHeader.serviceState != null
                && profileResponse.linkClientProfilesResponse.standardHeader.serviceState.stateCode != null
                && profileResponse.linkClientProfilesResponse.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse != null && profileResponse.linkClientProfilesResponse != null && profileResponse.linkClientProfilesResponse.standardHeader != null && profileResponse.linkClientProfilesResponse.standardHeader.e2e != null && profileResponse.linkClientProfilesResponse.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.linkClientProfilesResponse.standardHeader.e2e.E2EDATA))
                        e2eData.endOutboundCall(profileResponse.linkClientProfilesResponse.standardHeader.e2e.E2EDATA);
                    else
                        e2eData.endOutboundCall(e2eData.toString());
                }
                else
                {
                    string downStreamError = string.Empty;
                    if (profileResponse != null && profileResponse.linkClientProfilesResponse != null && profileResponse.linkClientProfilesResponse.messages != null && profileResponse.linkClientProfilesResponse.messages[0].description != null)
                    {
                        downStreamError = profileResponse.linkClientProfilesResponse.messages[0].description;

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, downStreamError);
                        if (profileResponse != null && profileResponse.linkClientProfilesResponse != null && profileResponse.linkClientProfilesResponse.standardHeader != null && profileResponse.linkClientProfilesResponse.standardHeader.e2e != null && profileResponse.linkClientProfilesResponse.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.linkClientProfilesResponse.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.linkClientProfilesResponse.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());                       
                    }
                    else
                    {
                        downStreamError = "Response is null from DnP";

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse != null && profileResponse.linkClientProfilesResponse != null && profileResponse.linkClientProfilesResponse.standardHeader != null && profileResponse.linkClientProfilesResponse.standardHeader.e2e != null && profileResponse.linkClientProfilesResponse.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.linkClientProfilesResponse.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.linkClientProfilesResponse.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());                        
                    }                    
                }
            }
            catch (Exception ex)
            {
                string downStreamError = ex.Message.ToString();

                LogHelper.LogActivityDetails(bptmTxnId, guid, "Exception in " + " linking BBSID to existing chop profile ", System.DateTime.Now - ActivityStartTime, "BTCONSUMERBROADBANDTrace", parameters.orderKey, downStreamError);

                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);
                if (profileResponse != null && profileResponse.linkClientProfilesResponse != null && profileResponse.linkClientProfilesResponse.standardHeader != null && profileResponse.linkClientProfilesResponse.standardHeader.e2e != null && profileResponse.linkClientProfilesResponse.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.linkClientProfilesResponse.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse.linkClientProfilesResponse.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());
            }
        }
    }

    public class BroadBandParameters
    {       
        private bool isAHTDone = false;

        public bool IsAHTDone
        {
            get { return isAHTDone; }
            set { isAHTDone = value; }
        }

        private bool isExixstingAccount = false;

        public bool IsExixstingAccount
        {
            get { return isExixstingAccount; }
            set { isExixstingAccount = value; }
        }

        private bool IsServiceExixsting = false;

        public bool isServiceExixsting
        {
            get { return IsServiceExixsting; }
            set { IsServiceExixsting = value; }
        }

        private string MSISDN1 = string.Empty;

        public string MSISDN
        {
            get { return MSISDN1; }
            set { MSISDN1 = value; }
        }


        private string RBSID1 = string.Empty;

        public string RBSID
        {
            get { return RBSID1; }
            set { RBSID1 = value; }
        }

        private string newRBSID = string.Empty;

        public string NewRBSID
        {
            get { return newRBSID; }
            set { newRBSID = value; }
        }

        private string BBSID1 = string.Empty;

        public string BBSID
        {
            get { return BBSID1; }
            set { BBSID1 = value; }
        }

        private string oldBBSID1 = string.Empty;

        public string oldBBSID
        {
            get { return oldBBSID1; }
            set { oldBBSID1 = value; }
        }

        private bool serviceInstanceExists = false;

        public bool ServiceInstanceExists
        {
            get { return serviceInstanceExists; }
            set { serviceInstanceExists = value; }
        }

        private string reason = string.Empty;

        public string Reason
        {
            get { return reason; }
            set { reason = value; }
        }
        private string billingaccountnumber = string.Empty;

        public string Billingaccountnumber
        {
            get { return billingaccountnumber; }
            set { billingaccountnumber = value; }
        }

        private string btOneid = string.Empty;
        public string BtOneId
        {
            get { return btOneid; }
            set { btOneid = value; }
        }

        private string orderkey = string.Empty;
        public string orderKey
        {
            get { return orderkey; }
            set { orderkey = value; }
        }

        private string servicestatus = string.Empty;
        public string serviceStatus
        {
            get { return servicestatus; }
            set { servicestatus = value; }
        }

        private string hybridTag = string.Empty;
        public string Hybrid_Tag
        {
            get { return hybridTag; }
            set { hybridTag = value; }
        }
        private string newBBSID = string.Empty;
        public string NewBBSID
        {
            get { return newBBSID; }
            set { newBBSID = value; }
        }

        private bool isBBSIDRoleDelete = false;
        public bool IsBBSIDRoleDelete
        {
            get { return isBBSIDRoleDelete; }
            set { isBBSIDRoleDelete = value; }
        }

        private bool isMobileExists = false;
        public bool IsMobileExists
        {
            get { return isMobileExists; }
            set { isMobileExists = value; }
        }

        private bool isOFSOrder = false;
        public bool IsOFSOrder
        {
            get { return isOFSOrder; }
            set { isOFSOrder = value; }
        }

        private string idenity = string.Empty;
        public string Idenity
        {
            get { return idenity; }
            set { idenity = value; }
        }

        private string idenityDomain = string.Empty;
        public string IdenityDomain
        {
            get { return idenityDomain; }
            set { idenityDomain = value; }
        }

        //ACCEL-6666
        private string eeOnlineid = string.Empty;
        public string EEOnlineId
        {
            get { return eeOnlineid; }
            set { eeOnlineid = value; }
        }

        private string conKid = string.Empty;
        public string ConKid
        {
            get { return conKid; }
            set { conKid = value; }
        }

        private bool eeAccount = false;
        public bool isEEAccount
        {
            get { return eeAccount; }
            set { eeAccount = value; }
        }

        private bool bbSidExist = false;
        public bool isBBSIDExist
        {
            get { return bbSidExist; }
            set { bbSidExist = value; }
        }
    }

}
