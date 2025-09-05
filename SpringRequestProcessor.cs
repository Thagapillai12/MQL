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
using MSEO = BT.SaaS.MSEOAdapter;
namespace BT.SaaS.MSEOAdapter
{
    public class SpringRequestProcessor
    {
        #region Constants
        const string BACID_IDENTIFER_NAMEPACE = "VAS_BILLINGACCOUNT_ID";
        const string MOBILE_MSISDN_NAMESPACE = "MOBILE_MSISDN";
        const string SPRING_GSM_SERVICECODE_NAMEPACE = "SPRING_GSM";
        const string GotResponseFromDnPWithBusinessError = "GotResponseFromDnPWithBusinessError";
        const string StartedDnPCall = "StartedDnPCall";
        const string GotResponseFromDnP = "GotResponseFromDnP";
        private const string IMSI = "IMSI";
        private const string RVSID = "RVSID";
        private const string MSISDN = "MSISDN";
        private const string ACTION_SEARCH = "SEARCH";
        private const string ACTION_UPDATE = "UPDATE";
        private const string ACTION_DELETE = "DELETE";
        private const string ACTION_INSERT = "INSERT";
        private const string ACTION_LINK = "LINK";
        private const string DEFAULT_ROLE = "DEFAULT";
        private const string ACTIVE = "ACTIVE";
        private const string ADMIN_ROLE = "ADMIN";
        private const string numSIMDispatched = "1";
        private const string ACTION_FORCE_INSERT = "FORCE_INS";

        const string Accepted = "Accepted";
        const string Ignored = "Ignored";
        const string Completed = "Completed";
        const string Failed = "Failed";
        const string Errored = "Errored";
        const string MakingDnPCall = "Making DnP call";
        const string GotResponsefromDNP = "Got Response from DNP";

        const string AcceptedNotificationSent = "Accepted Notification Sent for the Order";
        const string IgnoredNotificationSent = "Ignored Notification Sent for the Order";
        const string CompletedNotificationSent = "Completed Notification Sent for the Order";
        const string FailedNotificationSent = "Failure Notification Sent for the Order";

        const string SendingRequestToDNP = "Sending the Request to DNP";
        const string ReceivedResponsefromDNP = "Recieved Response from DNP";
        const string FailureResponseFromDnP = "Recieved failure response from DnP";
        const string NullResponseFromDnP = "Response is null from DnP";
        const string DnPAdminstratorFailedResponse = "Non Functional Exception from DNP(Administrator): ";
        #endregion
        public void SpringRequestMapper(OrderRequest requestOrderRequest, ref E2ETransaction e2eData)
        {
            string orderKey = string.Empty;
            string billAccountNumber = string.Empty;
            string old_rvsid = string.Empty;
            string rvsidValue = string.Empty;
            string downStreamError = string.Empty;
            bool isSpringServiceExist = false;

            MSEOOrderNotification notification = null;
            GetBatchProfileV1Res bacProfileResponse = null;
            GetClientProfileV1Res gcp_response = null;
            SpringParameters dnpParameters = new SpringParameters();

            try
            {
                orderKey = requestOrderRequest.Order.OrderIdentifier.Value;
                notification = new MSEOOrderNotification(requestOrderRequest);
                foreach (MSEO.OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                {
                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)))
                        billAccountNumber = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;

                    if (!String.IsNullOrEmpty(billAccountNumber))
                    {
                        if ((orderItem.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase) || orderItem.Action.Code.Equals("modify", StringComparison.OrdinalIgnoreCase))
                            && orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("MSEOOrderType", StringComparison.OrdinalIgnoreCase) && ic.Value.Equals("GSMBTOneID", StringComparison.OrdinalIgnoreCase)))
                        {
                            bool isAHTDone = false;
                            string BtOneId = string.Empty;

                            notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData,false);
                            Logger.Write(orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.SpringMessageTrace);

                            gcp_response = SpringDnpWrapper.GetClientProfileV1ForSpring(billAccountNumber, BACID_IDENTIFER_NAMEPACE, orderKey, "provide");

                            Logger.Write(orderKey + "," + GotResponsefromDNP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.SpringMessageTrace);
                            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, orderKey, gcp_response.SerializeObject(), ConfigurationManager.AppSettings["BTSpringProductCode"].ToString());

                            if (gcp_response != null && gcp_response.clientProfileV1 != null && gcp_response.clientProfileV1.client != null &&
                                gcp_response.clientProfileV1.client.clientIdentity != null)
                            {
                                if (gcp_response.clientProfileV1.client.clientIdentity.ToList().Exists(i => i.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                                {
                                    BtOneId = gcp_response.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                }
                                if (gcp_response.clientProfileV1.client.clientIdentity.ToList().Exists(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(billAccountNumber, StringComparison.OrdinalIgnoreCase)))
                                {
                                    BT.SaaS.IspssAdapter.Dnp.ClientIdentity bacClientIdentity = gcp_response.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(billAccountNumber, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                    if (bacClientIdentity.clientIdentityValidation != null)
                                        isAHTDone = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                                }
                            }
                            InstanceCharacteristic[] arr = new InstanceCharacteristic[2];
                            arr[0] = new InstanceCharacteristic();
                            arr[0].Name = "BTOneID";
                            arr[0].Value = BtOneId;

                            arr[1] = new InstanceCharacteristic();
                            arr[1].Name = "AHTStatus";
                            arr[1].Value = isAHTDone.ToString();

                            notification.NotificationResponse.Order[0].OrderItem[0].Instance[0].InstanceCharacteristic = notification.NotificationResponse.Order[0].OrderItem[0].Instance[0].InstanceCharacteristic.Concat(arr).ToArray();

                            notification.sendNotification(true, false, "004", string.Empty, ref e2eData,true);
                            Logger.Write(orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                        }
                        else
                        {
                            bacProfileResponse = SpringDnpWrapper.GetServiceUserProfilesV1ForSpring(billAccountNumber, BACID_IDENTIFER_NAMEPACE, orderKey);

                            if (orderItem.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase))
                            {
                                if (bacProfileResponse != null && bacProfileResponse.clientProfileV1 != null && bacProfileResponse.clientProfileV1.Count() > 0)
                                {
                                    foreach (ClientProfileV1 clientProfile in bacProfileResponse.clientProfileV1)
                                    {
                                        if (clientProfile.client!=null && clientProfile.client.clientIdentity != null && clientProfile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(billAccountNumber, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            dnpParameters.IsExixstingAccount = true;
                                            BT.SaaS.IspssAdapter.Dnp.ClientIdentity bacClientIdentity = clientProfile.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(billAccountNumber, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                            if (bacClientIdentity.clientIdentityValidation != null)
                                                dnpParameters.IsAHTDone = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                                        }
                                        if (clientProfile.clientServiceInstanceV1 != null && clientProfile.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)
                                            && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(billAccountNumber, StringComparison.OrdinalIgnoreCase))))
                                        {
                                            dnpParameters.ServiceInstanceExists = clientProfile.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase));
                                            if (dnpParameters.ServiceInstanceExists)
                                            {
                                                ClientServiceInstanceV1 serviceInstance = clientProfile.clientServiceInstanceV1.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                                if (serviceInstance != null && serviceInstance.clientServiceRole.ToList().Where(ic => ic.id.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase)).ToList().Count == 0)
                                                {
                                                    if (MSIForSpringDeletion(SPRING_GSM_SERVICECODE_NAMEPACE, billAccountNumber, orderKey, ref e2eData, ref downStreamError))
                                                    {
                                                        dnpParameters.ServiceInstanceExists = false;
                                                    }
                                                    else
                                                    {
                                                        notification.sendNotification(false, false, "001", "Ignored as the existing profile doesn't have updated properly", ref e2eData, true);
                                                        Logger.Write(orderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                                                        break;
                                                    }

                                                }
                                                else
                                                {
                                                    if (clientProfile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(RVSID, StringComparison.OrdinalIgnoreCase)))
                                                        dnpParameters.OldRVSID = clientProfile.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(RVSID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    gcp_response = SpringDnpWrapper.GetClientProfileV1ForSpring(billAccountNumber, BACID_IDENTIFER_NAMEPACE, orderKey, "create");
                                    if (gcp_response != null && gcp_response.clientProfileV1 != null && gcp_response.clientProfileV1.client != null &&
                                        gcp_response.clientProfileV1.client.clientIdentity != null)
                                    {
                                        if (gcp_response.clientProfileV1.client.clientIdentity.Count()>0 && gcp_response.clientProfileV1.client.clientIdentity.ToList().Exists(id => id.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            dnpParameters.IsExixstingAccount = true;
                                            var ci = gcp_response.clientProfileV1.client.clientIdentity.ToList().Where(id => id.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                            if (ci.clientIdentityValidation != null)
                                                dnpParameters.IsAHTDone = ci.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                                        }
                                    }
                                }

                                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals(IMSI, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(insChar.Value)))
                                {
                                    dnpParameters.IMSI = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals(IMSI, StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                                }
                                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals(RVSID, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(insChar.Value)))
                                {
                                    dnpParameters.RVSID = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals(RVSID, StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                                }
                                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals(MSISDN, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(insChar.Value)))
                                {
                                    dnpParameters.MSISDN = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals(MSISDN, StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                                }
                                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("SimCardType", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(insChar.Value)))
                                {
                                    dnpParameters.SimCardType = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("SimCardType", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                                }
                                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("SSN", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(insChar.Value)))
                                {
                                    dnpParameters.SSN = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("SSN", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                                }
                                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("PUK_CODE", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(insChar.Value)))
                                {
                                    dnpParameters.PukCode = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("PUK_CODE", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                                }

                                notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData,false);
                                Logger.Write(orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.SpringMessageTrace);

                                CreateSpringServiceInstance(billAccountNumber, BACID_IDENTIFER_NAMEPACE, dnpParameters, orderKey, notification, ref e2eData);

                            }
                            else if (orderItem.Action.Code.Equals("modify", StringComparison.OrdinalIgnoreCase))
                            {
                                if (orderItem.Action.Reason.Equals("authorise", StringComparison.OrdinalIgnoreCase) || orderItem.Action.Reason.Equals("resetpin", StringComparison.OrdinalIgnoreCase))
                                {
                                    // need to reset or inseert pin in DNP.
                                    string inviteeMSISDN = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals(MSISDN, StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                                    UpdateAuthrizationStatus(inviteeMSISDN, orderItem.Action.Reason, dnpParameters, orderKey, notification, ref e2eData);
                                }
                                else if (!orderItem.Action.Reason.Equals("portin", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("RVSID", StringComparison.OrdinalIgnoreCase)))
                                        rvsidValue = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("RVSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

                                    dnpParameters.RVSID = rvsidValue;

                                    if (bacProfileResponse != null && bacProfileResponse.clientProfileV1 != null)
                                    {
                                        foreach (ClientProfileV1 clientProfile in bacProfileResponse.clientProfileV1)
                                        {
                                            if (clientProfile.clientServiceInstanceV1 != null && clientProfile.clientServiceInstanceV1.ToList().Exists(ip => ip.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                            {
                                                isSpringServiceExist = true;
                                                if (clientProfile.client.clientIdentity.ToList().Exists(ic => ic.managedIdentifierDomain.value.Equals("RVSID", StringComparison.OrdinalIgnoreCase) && ic.value.Equals(rvsidValue, StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    BT.SaaS.IspssAdapter.Dnp.ClientIdentity rvsidClientIdentity = clientProfile.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals("RVSID", StringComparison.OrdinalIgnoreCase) && i.value.Equals(rvsidValue, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                                    if (rvsidClientIdentity.clientIdentityValidation != null)
                                                    {
                                                        if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("replacementreason", StringComparison.OrdinalIgnoreCase)))
                                                        {
                                                            dnpParameters.replacementReason = true;
                                                            if (rvsidClientIdentity.clientIdentityValidation.ToList().Exists(civ => civ.name.Equals("num_sim_dispatched", StringComparison.OrdinalIgnoreCase)))
                                                            {
                                                                dnpParameters.NumSimDispatched = rvsidClientIdentity.clientIdentityValidation.ToList().Where(civ => civ.name.Equals("num_sim_dispatched", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                            }
                                                        }
                                                        else if (!String.IsNullOrEmpty(orderItem.Action.Reason))
                                                        {
                                                            dnpParameters.Reason = orderItem.Action.Reason.ToString();

                                                            if (orderItem.Action.Reason.Equals("sim_activation", StringComparison.OrdinalIgnoreCase))
                                                            {
                                                                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("imsi", StringComparison.OrdinalIgnoreCase)))
                                                                {
                                                                    dnpParameters.IMSI = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("imsi", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                                                    dnpParameters.oldIMSI = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("imsi", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().PreviousValue;
                                                                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("PUK_CODE", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(insChar.Value)))
                                                                    {
                                                                        dnpParameters.PukCode = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("PUK_CODE", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                                                                    }
                                                                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("SIMCARDTYPE", StringComparison.OrdinalIgnoreCase)))
                                                                    {
                                                                        dnpParameters.SimCardType = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("SIMCARDTYPE", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                                                                    }
                                                                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("SSN", StringComparison.OrdinalIgnoreCase)))
                                                                    {
                                                                        dnpParameters.SSN = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("SSN", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                                                                    }
                                                                }
                                                            }
                                                            else if (orderItem.Action.Reason.Equals("sim_creditlimit", StringComparison.OrdinalIgnoreCase))
                                                            {
                                                                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("INTERNATIONAL", StringComparison.OrdinalIgnoreCase)))
                                                                {
                                                                    dnpParameters.International = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("INTERNATIONAL", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                                                                }
                                                                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("PREMIUM_RATE_SERVICE", StringComparison.OrdinalIgnoreCase)))
                                                                {
                                                                    dnpParameters.PremiumRateService = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("PREMIUM_RATE_SERVICE", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                                                                }
                                                                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("ROAMING", StringComparison.OrdinalIgnoreCase)))
                                                                {
                                                                    dnpParameters.Roaming = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("ROAMING", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                                                                }
                                                                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("CREDIT_LIMIT", StringComparison.OrdinalIgnoreCase)))
                                                                {
                                                                    dnpParameters.CreditLimit = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("CREDIT_LIMIT", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                                                                }
                                                                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("DATA_ROAMING_LIMIT", StringComparison.OrdinalIgnoreCase)))
                                                                {
                                                                    dnpParameters.DataRoamingLimit = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("DATA_ROAMING_LIMIT", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                                                                }
                                                            }
                                                        }
                                                        if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(insChar => insChar.Name.Equals("PENDING_ORDER", StringComparison.OrdinalIgnoreCase)))
                                                        {
                                                            dnpParameters.NewPendingOrderValue = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("PENDING_ORDER", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                                                            if (rvsidClientIdentity.clientIdentityValidation.ToList().Exists(civ => civ.name.Equals("pending_order", StringComparison.OrdinalIgnoreCase)))
                                                            {
                                                                dnpParameters.DnpPendingOrderValue = rvsidClientIdentity.clientIdentityValidation.ToList().Where(civ => civ.name.Equals("pending_order", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                            }
                                                            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("PENDING_ORDER", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().PreviousValue != null)
                                                            {
                                                                dnpParameters.OldPendingOrderValue = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("PENDING_ORDER", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().PreviousValue;
                                                            }
                                                        }

                                                        notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData,false);
                                                        Logger.Write(orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.SpringMessageTrace);

                                                        //BTRCE-111909 need to delete invite roles and update MSISDN auth_leave flag
                                                        string[] InviteRolePendingordervalue = ConfigurationManager.AppSettings["InviteRolePendingordervalue"].ToString().Split(',');
                                                        if (!string.IsNullOrEmpty(dnpParameters.OldPendingOrderValue) && InviteRolePendingordervalue.Contains(dnpParameters.OldPendingOrderValue))
                                                        {
                                                            List<string> inviteroles = new List<string>();
                                                            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("msisdn", StringComparison.OrdinalIgnoreCase)))
                                                            {
                                                                string MSISDNvalue = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("msisdn", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                                                inviteroles = MSEOSaaSMapper.GetInviterolesList("GetinviterolesbyUser", MSISDNvalue, "MOBILE_MSISDN", string.Empty);
                                                                dnpParameters.Inviteroles = inviteroles;
                                                                dnpParameters.MSISDN = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("msisdn", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                                                dnpParameters.Billingaccountnumber = billAccountNumber;
                                                            }
                                                            else
                                                            {
                                                                notification.sendNotification(false, false, "001", "Ignored as MSISDN value is missing in the request", ref e2eData, true);
                                                                Logger.Write(orderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                                                            }
                                                        }
                                                        ModifySpringServiceInstance(billAccountNumber, dnpParameters, orderKey, notification, ref e2eData);
                                                    }
                                                    else
                                                    {
                                                        notification.sendNotification(false, false, "001", "Ignored as RVSID in the request is not having client identity validation in DnP", ref e2eData,true);
                                                        Logger.Write(orderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                                                    }
                                                }
                                                else
                                                {
                                                    // need to send new error code in case of auto cease camcellation. 00111
                                                    if ((orderItem.Action.Reason.Equals("CancellationForTransfer", StringComparison.OrdinalIgnoreCase)) || (orderItem.Action.Reason.Equals("AutoCancelledForTransfer", StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        notification.sendNotification(false, false, "0011", "Ignored as RVSID in the request is not mapped to bacid in DnP", ref e2eData,true);
                                                        Logger.Write(orderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                                                    }
                                                    else
                                                    {
                                                        notification.sendNotification(false, false, "001", "Ignored as RVSID in the request is not mapped to bacid in DnP", ref e2eData,true);
                                                        Logger.Write(orderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                                                    }
                                                }
                                            }
                                        }
                                        if (!isSpringServiceExist)
                                        {
                                            notification.sendNotification(false, false, "001", "Ignored as there is no SPRING service instance mapped to given bacID in DnP", ref e2eData,true);
                                            Logger.Write(orderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                                        }
                                    }
                                    else
                                    {
                                        notification.sendNotification(false, false, "001", "Ignored as there is no profile for given bacID in DnP", ref e2eData,true);
                                        Logger.Write(orderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                                    }
                                }
                                else
                                {
                                    dnpParameters.Reason = orderItem.Action.Reason.ToString();
                                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("msisdn", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        dnpParameters.MSISDN = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("msisdn", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                        dnpParameters.oldMSISDN = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("msisdn", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().PreviousValue;
                                    }
                                    notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData,false);
                                    Logger.Write(orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.SpringMessageTrace);

                                    ModifySpringServiceInstance(billAccountNumber, dnpParameters, orderKey, notification, ref e2eData);
                                }
                            }
                        }
                    }
                    else if (requestOrderRequest.Order != null && requestOrderRequest.Order.OrderItem != null && requestOrderRequest.Order.OrderItem.Count() > 0 && requestOrderRequest.Order.OrderItem[0].Action.Reason != null && requestOrderRequest.Order.OrderItem[0].Action.Reason != "")
                    {
                        if (requestOrderRequest.Order.OrderItem[0].Action.Reason.ToString().ToUpper().Trim().ToLower() == "crossaccountsimotosp" ||
                            requestOrderRequest.Order.OrderItem[0].Action.Reason.ToString().ToUpper().Trim().ToLower() == "crossaccounthstosp" ||
                            requestOrderRequest.Order.OrderItem[0].Action.Reason.ToString().ToUpper().Trim().ToLower() == "crossaccountsptosp" ||
                            requestOrderRequest.Order.OrderItem[0].Action.Reason.ToString().ToUpper().Trim().ToLower() == "crossaccountsptosimo" ||
                            requestOrderRequest.Order.OrderItem[0].Action.Reason.ToString().ToUpper().Trim().ToLower() == "crossaccountsptohs")
                        {
                            SharedPlanesCrossAcountRequestMapper(requestOrderRequest, ref e2eData);
                        }
                    }
                    else
                    {
                        notification.sendNotification(false, false, "001", "Invalid Request BillAccountNumber is not Present in the request", ref e2eData,true);
                        Logger.Write(orderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                    }
                }
            }
            catch (DnpException excep)
            {
                notification.sendNotification(false, false, "777", excep.Message, ref e2eData,true);

                Logger.Write(orderKey + "," + Errored + "," + FailedNotificationSent + "," + excep.Message, Logger.TypeEnum.SpringExceptionTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
            }
            catch (Exception ex)
            {
                notification.sendNotification(false, false, "001", ex.Message, ref e2eData,true);

                Logger.Write(orderKey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.SpringExceptionTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
            }
            finally
            {
                dnpParameters = null;
            }
        }
        public void CreateSpringServiceInstance(string identity, string identityDomain, SpringParameters dnpParameters, string orderKey, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            manageClientProfileV1Response1 profileResponse1 = null;
            manageClientProfileV1Request1 profileRequest = new manageClientProfileV1Request1();
            profileRequest.manageClientProfileV1Request = new ManageClientProfileV1Request();

            try
            {
                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

                ManageClientProfileV1Req manageClientProfileV1Req1 = new ManageClientProfileV1Req();
                manageClientProfileV1Req1.clientProfileV1 = new ClientProfileV1();

                List<ClientIdentity> clientIdentityList = new List<ClientIdentity>();

                ClientIdentity clientIdentity = null;

                List<ClientIdentityValidation> clientIdentityvalidationList = new List<ClientIdentityValidation>();
                ClientIdentityValidation clientIdentityValidation = null;

                List<ClientIdentityValidation> clientIdentityvalidationList1 = new List<ClientIdentityValidation>();
                ClientIdentityValidation clientIdentityValidation1 = null;

                List<ClientIdentityValidation> clientIdentityvalidationList2 = new List<ClientIdentityValidation>();
                ClientIdentityValidation clientIdentityValidation2 = null;

                clientIdentity = new ClientIdentity();
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = IMSI;
                clientIdentity.value = dnpParameters.IMSI;
                clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                clientIdentity.clientIdentityStatus.value = ACTIVE;

                clientIdentityValidation = new ClientIdentityValidation();
                clientIdentityValidation.name = "SSN";
                clientIdentityValidation.value = dnpParameters.SSN;
                clientIdentityValidation.action = ACTION_INSERT;
                clientIdentityvalidationList.Add(clientIdentityValidation);

                clientIdentityValidation = new ClientIdentityValidation();
                clientIdentityValidation.name = "SIM_CARD_TYPE";
                clientIdentityValidation.value = dnpParameters.SimCardType;
                clientIdentityValidation.action = ACTION_INSERT;
                clientIdentityvalidationList.Add(clientIdentityValidation);

                clientIdentityValidation = new ClientIdentityValidation();
                clientIdentityValidation.name = "PUK_CODE";
                clientIdentityValidation.value = dnpParameters.PukCode;
                clientIdentityValidation.action = ACTION_INSERT;
                clientIdentityvalidationList.Add(clientIdentityValidation);

                clientIdentity.clientIdentityValidation = clientIdentityvalidationList.ToArray();
                clientIdentity.action = ACTION_INSERT;
                clientIdentityList.Add(clientIdentity);

                clientIdentity = new ClientIdentity();
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = RVSID;
                clientIdentity.value = dnpParameters.RVSID;
                clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                clientIdentity.clientIdentityStatus.value = ACTIVE;

                clientIdentityValidation1 = new ClientIdentityValidation();
                clientIdentityValidation1.name = "NUM_SIM_DISPATCHED";
                clientIdentityValidation1.value = numSIMDispatched;
                clientIdentityValidation1.action = ACTION_INSERT;
                clientIdentityvalidationList1.Add(clientIdentityValidation1);

                clientIdentityValidation1 = new ClientIdentityValidation();
                clientIdentityValidation1.name = "PENDING_ORDER";
                clientIdentityValidation1.value = dnpParameters.NewPendingOrderValue;
                clientIdentityValidation1.action = ACTION_INSERT;
                clientIdentityvalidationList1.Add(clientIdentityValidation1);

                clientIdentityValidation1 = new ClientIdentityValidation();
                clientIdentityValidation1.name = "INTERNATIONAL";
                clientIdentityValidation1.value = dnpParameters.International;
                clientIdentityValidation1.action = ACTION_INSERT;
                clientIdentityvalidationList1.Add(clientIdentityValidation1);

                clientIdentityValidation1 = new ClientIdentityValidation();
                clientIdentityValidation1.name = "PREMIUM_RATE_SERVICE";
                clientIdentityValidation1.value = dnpParameters.PremiumRateService;
                clientIdentityValidation1.action = ACTION_INSERT;
                clientIdentityvalidationList1.Add(clientIdentityValidation1);

                clientIdentityValidation1 = new ClientIdentityValidation();
                clientIdentityValidation1.name = "ROAMING";
                clientIdentityValidation1.value = dnpParameters.Roaming;
                clientIdentityValidation1.action = ACTION_INSERT;
                clientIdentityvalidationList1.Add(clientIdentityValidation1);

                clientIdentityValidation1 = new ClientIdentityValidation();
                clientIdentityValidation1.name = "CREDIT_LIMIT";
                clientIdentityValidation1.value = dnpParameters.CreditLimit;
                clientIdentityValidation1.action = ACTION_INSERT;
                clientIdentityvalidationList1.Add(clientIdentityValidation1);

                clientIdentityValidation1 = new ClientIdentityValidation();
                clientIdentityValidation1.name = "DATA_ROAMING_LIMIT";
                clientIdentityValidation1.value = dnpParameters.DataRoamingLimit;
                clientIdentityValidation1.action = ACTION_INSERT;
                clientIdentityvalidationList1.Add(clientIdentityValidation1);

                clientIdentityValidation1 = new ClientIdentityValidation();
                clientIdentityValidation1.name = "PENDINGOPTIN";
                clientIdentityValidation1.value = dnpParameters.PendingOptIn;
                clientIdentityValidation1.action = ACTION_INSERT;
                clientIdentityvalidationList1.Add(clientIdentityValidation1);

                clientIdentityValidation1 = new ClientIdentityValidation();
                clientIdentityValidation1.name = "EUOPTIN";
                clientIdentityValidation1.value = dnpParameters.EUOPTIN;
                clientIdentityValidation1.action = ACTION_INSERT;
                clientIdentityvalidationList1.Add(clientIdentityValidation1);


                clientIdentityValidation1 = new ClientIdentityValidation();
                clientIdentityValidation1.name = "PC_PROFILE";
                clientIdentityValidation1.value = dnpParameters.PCProfile;
                clientIdentityValidation1.action = ACTION_INSERT;
                clientIdentityvalidationList1.Add(clientIdentityValidation1);

                clientIdentity.clientIdentityValidation = clientIdentityvalidationList1.ToArray();
                clientIdentity.action = ACTION_INSERT;
                clientIdentityList.Add(clientIdentity);

                clientIdentity = new ClientIdentity();
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = MOBILE_MSISDN_NAMESPACE;
                clientIdentity.value = dnpParameters.MSISDN;
                clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                clientIdentity.clientIdentityStatus.value = ACTIVE;

                clientIdentityValidation2 = new ClientIdentityValidation();
                clientIdentityValidation2.name = "AUTH_PIN";
                clientIdentityValidation2.value = "000000";
                clientIdentityValidation2.action = ACTION_INSERT;
                clientIdentityvalidationList2.Add(clientIdentityValidation2);

                clientIdentity.clientIdentityValidation = clientIdentityvalidationList2.ToArray();
                clientIdentity.action = ACTION_INSERT;
                clientIdentityList.Add(clientIdentity);

                if (!dnpParameters.ServiceInstanceExists)
                {
                    ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                    serviceIdentity[0] = new ServiceIdentity();
                    serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                    serviceIdentity[0].value = identity;
                    serviceIdentity[0].action = ACTION_LINK;

                    ClientServiceInstanceV1[] clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                    clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                    clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                    clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = "SPRING_GSM";
                    clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                    clientServiceInstanceV1[0].clientServiceInstanceStatus.value = ACTIVE;
                    clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                    clientServiceInstanceV1[0].action = ACTION_INSERT;

                    if (dnpParameters.IsExixstingAccount)
                    {
                        manageClientProfileV1Req1.clientProfileV1.client = new Client();
                        manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_UPDATE;

                        clientIdentity = new ClientIdentity();
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                        clientIdentity.value = identity;
                        clientIdentity.action = ACTION_SEARCH;

                        clientIdentityList.Add(clientIdentity);

                        if (dnpParameters.IsAHTDone)
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
                            clientServiceRole[0].clientIdentity[0].value = identity;
                            clientServiceRole[0].clientIdentity[0].action = ACTION_INSERT;
                            clientServiceRole[0].action = ACTION_INSERT;

                            clientServiceRole[1] = new ClientServiceRole();
                            clientServiceRole[1].id = DEFAULT_ROLE;
                            clientServiceRole[1].clientServiceRoleStatus = new ClientServiceRoleStatus();
                            clientServiceRole[1].clientServiceRoleStatus.value = ACTIVE;

                            clientServiceRole[1].clientIdentity = new ClientIdentity[3];
                            clientServiceRole[1].clientIdentity[0] = new ClientIdentity();
                            clientServiceRole[1].clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                            clientServiceRole[1].clientIdentity[0].managedIdentifierDomain.value = IMSI;
                            clientServiceRole[1].clientIdentity[0].value = dnpParameters.IMSI;
                            clientServiceRole[1].clientIdentity[0].action = ACTION_INSERT;

                            clientServiceRole[1].clientIdentity[1] = new ClientIdentity();
                            clientServiceRole[1].clientIdentity[1].managedIdentifierDomain = new ManagedIdentifierDomain();
                            clientServiceRole[1].clientIdentity[1].managedIdentifierDomain.value = RVSID;
                            clientServiceRole[1].clientIdentity[1].value = dnpParameters.RVSID;
                            clientServiceRole[1].clientIdentity[1].action = ACTION_INSERT;

                            clientServiceRole[1].clientIdentity[2] = new ClientIdentity();
                            clientServiceRole[1].clientIdentity[2].managedIdentifierDomain = new ManagedIdentifierDomain();
                            clientServiceRole[1].clientIdentity[2].managedIdentifierDomain.value = MOBILE_MSISDN_NAMESPACE;
                            clientServiceRole[1].clientIdentity[2].value = dnpParameters.MSISDN;
                            clientServiceRole[1].clientIdentity[2].action = ACTION_INSERT;
                            clientServiceRole[1].action = ACTION_INSERT;

                            clientServiceInstanceV1[0].clientServiceRole = clientServiceRole;
                        }
                        else
                        {
                            ClientServiceRole clientServiceRole = new ClientServiceRole();

                            clientServiceRole = new ClientServiceRole();
                            clientServiceRole.id = DEFAULT_ROLE;
                            clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                            clientServiceRole.clientServiceRoleStatus.value = ACTIVE;

                            clientServiceRole.clientIdentity = new ClientIdentity[3];
                            clientServiceRole.clientIdentity[0] = new ClientIdentity();
                            clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                            clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = IMSI;
                            clientServiceRole.clientIdentity[0].value = dnpParameters.IMSI;
                            clientServiceRole.clientIdentity[0].action = ACTION_INSERT;

                            clientServiceRole.clientIdentity[1] = new ClientIdentity();
                            clientServiceRole.clientIdentity[1].managedIdentifierDomain = new ManagedIdentifierDomain();
                            clientServiceRole.clientIdentity[1].managedIdentifierDomain.value = RVSID;
                            clientServiceRole.clientIdentity[1].value = dnpParameters.RVSID;
                            clientServiceRole.clientIdentity[1].action = ACTION_INSERT;

                            clientServiceRole.clientIdentity[2] = new ClientIdentity();
                            clientServiceRole.clientIdentity[2].managedIdentifierDomain = new ManagedIdentifierDomain();
                            clientServiceRole.clientIdentity[2].managedIdentifierDomain.value = MOBILE_MSISDN_NAMESPACE;
                            clientServiceRole.clientIdentity[2].value = dnpParameters.MSISDN;
                            clientServiceRole.clientIdentity[2].action = ACTION_INSERT;
                            clientServiceRole.action = ACTION_INSERT;

                            clientServiceInstanceV1[0].clientServiceRole = new ClientServiceRole[1];
                            clientServiceInstanceV1[0].clientServiceRole[0] = clientServiceRole;
                        }
                    }
                    else
                    {
                        manageClientProfileV1Req1.clientProfileV1.client = new Client();
                        manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_INSERT;
                        manageClientProfileV1Req1.clientProfileV1.client.clientOrganisation = new ClientOrganisation();
                        manageClientProfileV1Req1.clientProfileV1.client.clientOrganisation.id = "BTRetailConsumer";
                        manageClientProfileV1Req1.clientProfileV1.client.type = "CUSTOMER";
                        manageClientProfileV1Req1.clientProfileV1.client.clientStatus = new ClientStatus();
                        manageClientProfileV1Req1.clientProfileV1.client.clientStatus.value = "ACTIVE";

                        ClientServiceRole clientServiceRole = new ClientServiceRole();

                        clientServiceRole = new ClientServiceRole();
                        clientServiceRole.id = DEFAULT_ROLE;
                        clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                        clientServiceRole.clientServiceRoleStatus.value = ACTIVE;

                        clientServiceRole.clientIdentity = new ClientIdentity[3];
                        clientServiceRole.clientIdentity[0] = new ClientIdentity();
                        clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = IMSI;
                        clientServiceRole.clientIdentity[0].value = dnpParameters.IMSI;
                        clientServiceRole.clientIdentity[0].action = ACTION_INSERT;

                        clientServiceRole.clientIdentity[1] = new ClientIdentity();
                        clientServiceRole.clientIdentity[1].managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientServiceRole.clientIdentity[1].managedIdentifierDomain.value = RVSID;
                        clientServiceRole.clientIdentity[1].value = dnpParameters.RVSID;
                        clientServiceRole.clientIdentity[1].action = ACTION_INSERT;

                        clientServiceRole.clientIdentity[2] = new ClientIdentity();
                        clientServiceRole.clientIdentity[2].managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientServiceRole.clientIdentity[2].managedIdentifierDomain.value = MOBILE_MSISDN_NAMESPACE;
                        clientServiceRole.clientIdentity[2].value = dnpParameters.MSISDN;
                        clientServiceRole.clientIdentity[2].action = ACTION_INSERT;

                        clientServiceRole.action = ACTION_INSERT;

                        clientServiceInstanceV1[0].clientServiceRole = new ClientServiceRole[1];
                        clientServiceInstanceV1[0].clientServiceRole[0] = clientServiceRole;
                    }
                    manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                    manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;
                }
                else
                {
                    //if (inputParameterCollection.ContainsKey("isahtdone") && !(inputParameterCollection["isahtdone"].Equals("true", StringComparison.OrdinalIgnoreCase)))
                    //{
                    manageClientProfileV1Req1.clientProfileV1.client = new Client();
                    manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_UPDATE;

                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = RVSID;
                    clientIdentity.value = dnpParameters.OldRVSID;
                    clientIdentity.action = ACTION_SEARCH;
                    clientIdentityList.Add(clientIdentity);

                    ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                    serviceIdentity[0] = new ServiceIdentity();
                    serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                    serviceIdentity[0].value = identity;
                    serviceIdentity[0].action = ACTION_SEARCH;

                    ClientServiceInstanceV1[] clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                    clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                    clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                    clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = "SPRING_GSM";
                    clientServiceInstanceV1[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                    clientServiceInstanceV1[0].clientServiceInstanceStatus.value = ACTIVE;
                    clientServiceInstanceV1[0].serviceIdentity = serviceIdentity;
                    clientServiceInstanceV1[0].action = ACTION_UPDATE;

                    ClientServiceRole clientServiceRole = new ClientServiceRole();

                    clientServiceRole = new ClientServiceRole();
                    clientServiceRole.id = DEFAULT_ROLE;
                    clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                    clientServiceRole.clientServiceRoleStatus.value = ACTIVE;

                    clientServiceRole.clientIdentity = new ClientIdentity[3];
                    clientServiceRole.clientIdentity[0] = new ClientIdentity();
                    clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = IMSI;
                    clientServiceRole.clientIdentity[0].value = dnpParameters.IMSI;
                    clientServiceRole.clientIdentity[0].action = ACTION_INSERT;

                    clientServiceRole.clientIdentity[1] = new ClientIdentity();
                    clientServiceRole.clientIdentity[1].managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientServiceRole.clientIdentity[1].managedIdentifierDomain.value = RVSID;
                    clientServiceRole.clientIdentity[1].value = dnpParameters.RVSID;
                    clientServiceRole.clientIdentity[1].action = ACTION_INSERT;

                    clientServiceRole.clientIdentity[2] = new ClientIdentity();
                    clientServiceRole.clientIdentity[2].managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientServiceRole.clientIdentity[2].managedIdentifierDomain.value = MOBILE_MSISDN_NAMESPACE;
                    clientServiceRole.clientIdentity[2].value = dnpParameters.MSISDN;
                    clientServiceRole.clientIdentity[2].action = ACTION_INSERT;

                    clientServiceRole.action = ACTION_INSERT;

                    clientServiceInstanceV1[0].clientServiceRole = new ClientServiceRole[1];
                    clientServiceInstanceV1[0].clientServiceRole[0] = clientServiceRole;

                    manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                    manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientServiceInstanceV1;

                }

                profileRequest.manageClientProfileV1Request.standardHeader = headerBlock;
                profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, SPRING_GSM_SERVICECODE_NAMEPACE);

                profileRequest.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                profileRequest.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                Logger.Write(orderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.SpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, orderKey, profileRequest.SerializeObject(), dnpParameters.ProductCode);

                profileResponse1 = SpringDnpWrapper.manageClientProfileV1ForSpring(profileRequest, orderKey);

                Logger.Write(orderKey + "," + GotResponsefromDNP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.SpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, orderKey, profileResponse1.SerializeObject(), dnpParameters.ProductCode);

                if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null
                    && profileResponse1.manageClientProfileV1Response.standardHeader != null
                    && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState != null
                    && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                    && profileResponse1.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    {
                        e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                    }
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData,true);
                    Logger.Write(orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                }
                else
                {
                    if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0] != null
                        && profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse1.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                        if (profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        //if (errorMessage.Contains("Administrator"))
                        //{
                        //    Logger.Write(orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.SpringExceptionTrace);
                        //    Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.SpringMessageTrace);
                        //}
                        //else
                        //{
                        //    Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.SpringMessageTrace);
                        //}
                        // retryieng the create order if the service instance already exists.
                        if (errorMessage.Contains("Service Identity Value[" + identity + "] and prf_domain_cd [VAS_BILLINGACCOUNT_ID] already exists in profile store with given service")
                            || errorMessage.Contains("Administrator"))
                        {
                            dnpParameters.ServiceInstanceExists = false;
                            GetBatchProfileV1Res bacProfileResponse = null;
                            bacProfileResponse = SpringDnpWrapper.GetServiceUserProfilesV1ForSpring(identity, identityDomain, orderKey);
                            if (bacProfileResponse != null && bacProfileResponse.clientProfileV1 != null)
                            {
                                foreach (ClientProfileV1 clientProfile in bacProfileResponse.clientProfileV1)
                                {

                                    if (clientProfile.clientServiceInstanceV1 != null && clientProfile.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)
                                        && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(identity, StringComparison.OrdinalIgnoreCase))))
                                    {
                                        dnpParameters.ServiceInstanceExists = clientProfile.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase));

                                        if (clientProfile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(RVSID, StringComparison.OrdinalIgnoreCase)))
                                            dnpParameters.OldRVSID = clientProfile.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(RVSID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                    }
                                }
                            }
                            if (dnpParameters.ServiceInstanceExists)
                            {
                                CreateSpringServiceInstance(identity, identityDomain, dnpParameters, orderKey, notification, ref e2eData);
                            }
                            else
                            {
                                notification.sendNotification(false, false, "777", errorMessage, ref e2eData,true);
                                Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                            }
                        }
                        else
                        {
                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData,true);
                            Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                        }
                    }
                    else
                    {
                        Logger.Write(orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.SpringMessageTrace);

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse1 != null && profileResponse1.manageClientProfileV1Response != null && profileResponse1.manageClientProfileV1Response.standardHeader != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse1.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData,true);
                        Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
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

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData,true);
                Logger.Write(orderKey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.SpringExceptionTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP, Logger.TypeEnum.SpringMessageTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
            }
        }
        public void ModifySpringServiceInstance(string identity, SpringParameters dnpParameters, string orderKey, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            if (dnpParameters.Reason != null && dnpParameters.Reason.Equals("portin", StringComparison.OrdinalIgnoreCase))
            {
                ModifySpringClientIdentity(MOBILE_MSISDN_NAMESPACE, dnpParameters, orderKey, notification, ref e2eData);
            }
            else if (string.IsNullOrEmpty(dnpParameters.Reason) || (dnpParameters.Reason != null && dnpParameters.Reason.Equals("sim_creditlimit", StringComparison.OrdinalIgnoreCase)))
            {
                ModifySpringClientIdentity(RVSID, dnpParameters, orderKey, notification, ref e2eData);
            }
            else if (dnpParameters.Reason != null && ((dnpParameters.Reason.Equals("AutoCancelledForTransfer", StringComparison.OrdinalIgnoreCase)) || (dnpParameters.Reason.Equals("CancellationForTransfer", StringComparison.OrdinalIgnoreCase))))
            {
                ModifySpringClientIdentity(RVSID, dnpParameters, orderKey, notification, ref e2eData);
            }
            else if (dnpParameters.Reason != null && dnpParameters.Reason.Equals("sim_activation", StringComparison.OrdinalIgnoreCase))
            {
                ModifySpringActivationRequest(identity, dnpParameters, orderKey, notification, ref e2eData);
            }
        }

        public void ModifySpringClientIdentity(string domain, SpringParameters dnpParameters, string orderKey, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            string oldValue, newValue;
            string SPRING_DELIMITER_VALUE = ConfigurationManager.AppSettings["SpringPendingOrderDelimiter"];
            manageClientIdentityResponse1 resp = new manageClientIdentityResponse1();
            manageClientIdentityRequest1 req = new manageClientIdentityRequest1();
            ManageClientIdentityRequest manageclientidentityreq = new ManageClientIdentityRequest();
            manageclientidentityreq.manageClientIdentityReq = new ManageClientIdentityReq();
            try
            {
                if (String.Equals(domain, MOBILE_MSISDN_NAMESPACE, StringComparison.OrdinalIgnoreCase))
                {
                    oldValue = dnpParameters.oldMSISDN;
                    newValue = dnpParameters.MSISDN;
                }
                else
                {
                    oldValue = dnpParameters.RVSID;
                    newValue = dnpParameters.RVSID;
                }

                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerblock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                headerblock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                headerblock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                headerblock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                headerblock.serviceState.stateCode = "OK";

                manageclientidentityreq.manageClientIdentityReq.clientSearchCriteria = new ClientSearchCriteria();
                manageclientidentityreq.manageClientIdentityReq.clientSearchCriteria.identifierDomainCode = domain;
                manageclientidentityreq.manageClientIdentityReq.clientSearchCriteria.identifierValue = oldValue;

                manageclientidentityreq.manageClientIdentityReq.clientIdentity = new ClientIdentity();
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.action = "UPDATE";
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.managedIdentifierDomain.value = domain;
                manageclientidentityreq.manageClientIdentityReq.clientIdentity.value = newValue;

                if (String.Equals(domain, RVSID, StringComparison.OrdinalIgnoreCase))
                {
                    headerblock.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                    headerblock.e2e.E2EDATA = e2eData.toString();

                    List<ClientIdentityValidation> clientIdentityValidationList = new List<ClientIdentityValidation>();
                    ClientIdentityValidation clientIdentityValidation = null;
                    //SIM_REPLACEMENT :: update only NUM_SIM_DISPATCHED
                    if (dnpParameters.replacementReason == true)
                    {
                        clientIdentityValidation = new ClientIdentityValidation();
                        clientIdentityValidation.name = "NUM_SIM_DISPATCHED";
                        clientIdentityValidation.value = Convert.ToString(Convert.ToUInt32(dnpParameters.NumSimDispatched) + 1);
                        clientIdentityValidation.action = ACTION_UPDATE;
                        clientIdentityValidationList.Add(clientIdentityValidation);
                    }
                    else
                    {
                        //SIM_CREDITLIMIT :: update credit limit attributes 
                        if (dnpParameters.Reason != null && dnpParameters.Reason.Equals("sim_creditlimit", StringComparison.OrdinalIgnoreCase))
                        {
                            clientIdentityValidation = new ClientIdentityValidation();
                            clientIdentityValidation.name = "INTERNATIONAL";
                            if (!string.IsNullOrEmpty(dnpParameters.International))
                                clientIdentityValidation.value = dnpParameters.International;
                            else
                                clientIdentityValidation.value = " ";
                            clientIdentityValidation.action = ACTION_UPDATE;
                            clientIdentityValidationList.Add(clientIdentityValidation);

                            clientIdentityValidation = new ClientIdentityValidation();
                            clientIdentityValidation.name = "PREMIUM_RATE_SERVICE";
                            if (!string.IsNullOrEmpty(dnpParameters.PremiumRateService))
                                clientIdentityValidation.value = dnpParameters.PremiumRateService;
                            else
                                clientIdentityValidation.value = " ";
                            clientIdentityValidation.action = ACTION_UPDATE;
                            clientIdentityValidationList.Add(clientIdentityValidation);

                            clientIdentityValidation = new ClientIdentityValidation();
                            clientIdentityValidation.name = "ROAMING";
                            if (!string.IsNullOrEmpty(dnpParameters.Roaming))
                                clientIdentityValidation.value = dnpParameters.Roaming;
                            else
                                clientIdentityValidation.value = " ";
                            clientIdentityValidation.action = ACTION_UPDATE;
                            clientIdentityValidationList.Add(clientIdentityValidation);

                            clientIdentityValidation = new ClientIdentityValidation();
                            clientIdentityValidation.name = "CREDIT_LIMIT";
                            if (!string.IsNullOrEmpty(dnpParameters.CreditLimit))
                                clientIdentityValidation.value = dnpParameters.CreditLimit;
                            else
                                clientIdentityValidation.value = " ";
                            clientIdentityValidation.action = ACTION_UPDATE;
                            clientIdentityValidationList.Add(clientIdentityValidation);

                            clientIdentityValidation = new ClientIdentityValidation();
                            clientIdentityValidation.name = "DATA_ROAMING_LIMIT";
                            if (!string.IsNullOrEmpty(dnpParameters.DataRoamingLimit))
                                clientIdentityValidation.value = dnpParameters.DataRoamingLimit;
                            else
                                clientIdentityValidation.value = " ";
                            clientIdentityValidation.action = ACTION_UPDATE;
                            clientIdentityValidationList.Add(clientIdentityValidation);
                        }
                        //update PENDING_ORDER attribute
                        //PreGSM
                        if (!string.IsNullOrEmpty(dnpParameters.NewPendingOrderValue))
                        {
                            clientIdentityValidation = new ClientIdentityValidation();
                            clientIdentityValidation.name = "PENDING_ORDER";

                            if (dnpParameters.DnpPendingOrderValue == " ")
                            {
                                clientIdentityValidation.value = dnpParameters.NewPendingOrderValue;
                            }
                            else
                            {
                                List<string> pendingOrderAttrs = dnpParameters.DnpPendingOrderValue.Split(SPRING_DELIMITER_VALUE.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                                //PreGSM //Appending Pending_Order value coming in request to existence Pending_order value in DNP.
                                //If already DNP Pending_order value contains value coming in request just update the same dnp value again ..
                                //Else append the value coming in request with existence DNP value

                                if (pendingOrderAttrs.Contains(dnpParameters.NewPendingOrderValue,StringComparer.OrdinalIgnoreCase))
                                    clientIdentityValidation.value = dnpParameters.DnpPendingOrderValue;
                                else
                                    clientIdentityValidation.value = dnpParameters.DnpPendingOrderValue + SPRING_DELIMITER_VALUE + dnpParameters.NewPendingOrderValue;
                            }
                            clientIdentityValidation.action = ACTION_UPDATE;
                            clientIdentityValidationList.Add(clientIdentityValidation);
                        }
                        //PostGSM //Removing Pending_Order Attribute coming in request from DNP.
                        else if (!string.IsNullOrEmpty(dnpParameters.OldPendingOrderValue))
                        {
                            List<string> pendingOrderAttrs = dnpParameters.DnpPendingOrderValue.ToUpper().Split(SPRING_DELIMITER_VALUE.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                            if (pendingOrderAttrs != null && pendingOrderAttrs.Contains(dnpParameters.OldPendingOrderValue.ToUpper()))
                            {
                                clientIdentityValidation = new ClientIdentityValidation();
                                clientIdentityValidation.name = "PENDING_ORDER";
                                string pending_order = string.Empty;
                                pendingOrderAttrs.Remove(dnpParameters.OldPendingOrderValue.ToUpper());                                
                                pending_order = string.Join(SPRING_DELIMITER_VALUE, pendingOrderAttrs.ToArray());
                                clientIdentityValidation.value = string.IsNullOrEmpty(pending_order) ? " " : pending_order;
                                clientIdentityValidation.action = ACTION_UPDATE;
                                clientIdentityValidationList.Add(clientIdentityValidation);
                            }
                        }
                        //update PENDINGOPTIN only when PENDING_ORDER is null or empty 
                        if (clientIdentityValidationList.Exists(ci => ci.name.Equals("PENDING_ORDER", StringComparison.OrdinalIgnoreCase) && ci.value != null) && string.IsNullOrEmpty(clientIdentityValidationList.Where(ci => ci.name.Equals("PENDING_ORDER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value.Trim()))
                        {
                            clientIdentityValidation = new ClientIdentityValidation();
                            clientIdentityValidation.action = ACTION_UPDATE;
                            clientIdentityValidation.name = "PENDINGOPTIN";
                            clientIdentityValidation.value = dnpParameters.PendingOptIn;
                            clientIdentityValidationList.Add(clientIdentityValidation);
                        }
                    }

                    manageclientidentityreq.manageClientIdentityReq.clientIdentity.clientIdentityValidation = clientIdentityValidationList.ToArray();
                }

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, SPRING_GSM_SERVICECODE_NAMEPACE);

                headerblock.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                headerblock.e2e.E2EDATA = e2eData.toString();

                manageclientidentityreq.standardHeader = headerblock;
                req.manageClientIdentityRequest = manageclientidentityreq;

                Logger.Write(orderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.SpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, orderKey, req.SerializeObject(), dnpParameters.ProductCode);

                resp = SpringDnpWrapper.manageClientIdentity(req, orderKey);

                Logger.Write(orderKey + "," + GotResponsefromDNP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.SpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, orderKey, resp.SerializeObject(), dnpParameters.ProductCode);

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
                    if (dnpParameters.Inviteroles != null && dnpParameters.Inviteroles.Count() > 0)
                    {
                        string getinviterolebac = MSEOSaaSMapper.GetinviteRoleandBAClist(dnpParameters.Inviteroles, string.Empty);
                        DeleteInviteRoles(dnpParameters.Billingaccountnumber, dnpParameters.MSISDN, getinviterolebac, dnpParameters, orderKey, notification, ref e2eData);

                    }
                    else
                    {
                        notification.sendNotification(true, false, "004", string.Empty, ref e2eData,true);
                        Logger.Write(orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                    }

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

                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.SpringExceptionTrace);
                            Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.SpringMessageTrace);
                        }
                        else
                        {
                            Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.SpringMessageTrace);
                        }
                        notification.sendNotification(false, false, "777", errorMessage, ref e2eData,true);
                        Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                    }
                    else
                    {
                        Logger.Write(orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.SpringMessageTrace);
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (resp != null && resp.manageClientIdentityResponse != null
                            && resp.manageClientIdentityResponse.standardHeader != null
                            && resp.manageClientIdentityResponse.standardHeader.e2e != null
                            && resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA != null &&
                            !String.IsNullOrEmpty(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(resp.manageClientIdentityResponse.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData,true);
                        Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
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

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData,true);
                Logger.Write(orderKey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.SpringExceptionTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + ex.Message, Logger.TypeEnum.SpringMessageTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
            }
        }

        public void ModifySpringActivationRequest(string identity, SpringParameters dnpParameters, string orderKey, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            string SPRING_DELIMITER_VALUE = ConfigurationManager.AppSettings["SpringPendingOrderDelimiter"];
            manageBatchProfilesV1Request1 batchProfileRequest = null;
            manageBatchProfilesV1Response1 batchResponse = null;
            batchProfileRequest = new manageBatchProfilesV1Request1();
            batchProfileRequest.manageBatchProfilesV1Request = new ManageBatchProfilesV1Request();
            List<string> inviteroles = new List<string>();
            try
            {
                BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                standardHeader.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                standardHeader.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                List<ClientProfileV1> clientProfileArrayList = new List<ClientProfileV1>();

                ClientProfileV1 clientProfileArray = null;

                clientProfileArray = new ClientProfileV1();
                clientProfileArray.client = new Client();
                clientProfileArray.client.action = ACTION_UPDATE;


                List<ClientIdentity> clientIdentityList1 = null;
                clientIdentityList1 = new List<ClientIdentity>();

                List<ClientIdentityValidation> clientIdentityValidationList = null;
                clientIdentityValidationList = new List<ClientIdentityValidation>();

                ClientIdentity clientIdentity = null;
                clientIdentity = new ClientIdentity();
                clientIdentity.value = dnpParameters.RVSID;
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = RVSID;
                clientIdentity.action = ACTION_SEARCH;

                ClientIdentityValidation clientIdentityValidation = null;

                #region PendingOrderPreviouscodefor Ref
                //if (dnpParameters.DnpPendingOrderValue==" ")
                //{
                //    //PreGSM
                //    clientIdentityValidation.value = dnpParameters.NewPendingOrderValue;
                //}
                //else
                //{
                //    List<string> pendingOrderAttrs = dnpParameters.DnpPendingOrderValue.Split(SPRING_DELIMITER_VALUE.ToCharArray(),StringSplitOptions.RemoveEmptyEntries).ToList();
                //    if (String.IsNullOrEmpty(dnpParameters.OldPendingOrderValue))
                //    {
                //        //PreGSM //Appending Pending_Order value coming in request to existence Pending_order value in DNP.
                //        //If already DNP Pending_order value contains value coming in request just update the same dnp value again ..
                //        //Else append the value coming in request with existence DNP value
                //        if (pendingOrderAttrs.Contains(dnpParameters.NewPendingOrderValue))
                //            clientIdentityValidation.value = dnpParameters.DnpPendingOrderValue;
                //        else
                //            clientIdentityValidation.value = dnpParameters.DnpPendingOrderValue + SPRING_DELIMITER_VALUE + dnpParameters.NewPendingOrderValue;
                //    }
                //    else
                //    {
                //        //PostGSM //Removing Pending_Order Attribute coming in request from DNP.
                //        if (pendingOrderAttrs!=null && pendingOrderAttrs.Contains(dnpParameters.OldPendingOrderValue))
                //        {
                //            string pending_order = " ";
                //            pendingOrderAttrs.Remove(dnpParameters.OldPendingOrderValue);
                //            pending_order = string.Join(SPRING_DELIMITER_VALUE, pendingOrderAttrs.ToArray());
                //            clientIdentityValidation.value = string.IsNullOrEmpty(pending_order) ? " " : pending_order;
                //        }
                //    }
                //}
                //              
                #endregion

                //PreGSM
                if (!string.IsNullOrEmpty(dnpParameters.NewPendingOrderValue))
                {
                    clientIdentityValidation = new ClientIdentityValidation();
                    clientIdentityValidation.name = "PENDING_ORDER";

                    if (dnpParameters.DnpPendingOrderValue == " ")
                    {
                        clientIdentityValidation.value = dnpParameters.NewPendingOrderValue;
                    }
                    else
                    {
                        List<string> pendingOrderAttrs = dnpParameters.DnpPendingOrderValue.Split(SPRING_DELIMITER_VALUE.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                        //PreGSM //Appending Pending_Order value coming in request to existence Pending_order value in DNP.
                        //If already DNP Pending_order value contains value coming in request just update the same dnp value again ..
                        //Else append the value coming in request with existence DNP value
                        if (pendingOrderAttrs.Contains(dnpParameters.NewPendingOrderValue))
                            clientIdentityValidation.value = dnpParameters.DnpPendingOrderValue;
                        else
                            clientIdentityValidation.value = dnpParameters.DnpPendingOrderValue + SPRING_DELIMITER_VALUE + dnpParameters.NewPendingOrderValue;
                    }
                    clientIdentityValidation.action = ACTION_UPDATE;
                    clientIdentityValidationList.Add(clientIdentityValidation);

                }
                //PostGSM //Removing Pending_Order Attribute coming in request from DNP.
                else if (!string.IsNullOrEmpty(dnpParameters.OldPendingOrderValue))
                {
                    List<string> pendingOrderAttrs = dnpParameters.DnpPendingOrderValue.Split(SPRING_DELIMITER_VALUE.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                    if (pendingOrderAttrs != null && pendingOrderAttrs.Contains(dnpParameters.OldPendingOrderValue))
                    {
                        clientIdentityValidation = new ClientIdentityValidation();
                        clientIdentityValidation.name = "PENDING_ORDER";
                        string pending_order = string.Empty;
                        pendingOrderAttrs.Remove(dnpParameters.OldPendingOrderValue);
                        //remove pending_order coming in the request case_insensitive
                        //int index = pendingOrderAttrs.FindIndex(q => q.Equals(dnpParameters.OldPendingOrderValue, StringComparison.OrdinalIgnoreCase));
                        //pendingOrderAttrs.RemoveAt(index);
                        //pendingOrderAttrs.RemoveAll(x => x.Equals(pending_order, StringComparison.OrdinalIgnoreCase));
                        pending_order = string.Join(SPRING_DELIMITER_VALUE, pendingOrderAttrs.ToArray());
                        clientIdentityValidation.value = string.IsNullOrEmpty(pending_order) ? " " : pending_order;
                        clientIdentityValidation.action = ACTION_UPDATE;
                        clientIdentityValidationList.Add(clientIdentityValidation);
                    }

                }

                //update PENDINGOPTIN only when PENDING_ORDER is null or empty 
                if (clientIdentityValidationList.Exists(ci => ci.name.Equals("PENDING_ORDER", StringComparison.OrdinalIgnoreCase) && ci.value != null) && string.IsNullOrEmpty(clientIdentityValidationList.Where(ci => ci.name.Equals("PENDING_ORDER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value.Trim()))
                {
                    clientIdentityValidation = new ClientIdentityValidation();
                    clientIdentityValidation.action = ACTION_UPDATE;
                    clientIdentityValidation.name = "PENDINGOPTIN";
                    clientIdentityValidation.value = dnpParameters.PendingOptIn;
                    clientIdentityValidationList.Add(clientIdentityValidation);
                }
                clientIdentity.clientIdentityValidation = clientIdentityValidationList.ToArray();
                clientIdentityList1.Add(clientIdentity);

                if (!String.IsNullOrEmpty(dnpParameters.IMSI) && !String.IsNullOrEmpty(dnpParameters.oldIMSI))
                {
                    clientIdentity = new ClientIdentity();
                    clientIdentity.value = dnpParameters.oldIMSI;
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = IMSI;
                    clientIdentity.action = ACTION_DELETE;
                    clientIdentityList1.Add(clientIdentity);

                    // tget the invite role for the Old MSISDN.                   
                    inviteroles = MSEOSaaSMapper.GetInviterolesList("GetinviterolesbyUser", dnpParameters.oldIMSI, "IMSI", string.Empty);
                }
                clientProfileArray.client.clientIdentity = clientIdentityList1.ToArray();
                clientProfileArrayList.Add(clientProfileArray);
                if (!String.IsNullOrEmpty(dnpParameters.IMSI) && !String.IsNullOrEmpty(dnpParameters.oldIMSI))
                {
                    clientProfileArray = new ClientProfileV1();
                    clientProfileArray.client = new Client();
                    clientProfileArray.client.action = ACTION_UPDATE;
                    clientProfileArray.clientServiceInstanceV1 = new ClientServiceInstanceV1[1];
                    clientProfileArray.clientServiceInstanceV1[0] = new ClientServiceInstanceV1();
                    clientProfileArray.clientServiceInstanceV1[0].action = ACTION_LINK;
                    clientProfileArray.clientServiceInstanceV1[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                    clientProfileArray.clientServiceInstanceV1[0].clientServiceInstanceIdentifier.value = @"SPRING_GSM";
                    clientProfileArray.clientServiceInstanceV1[0].serviceIdentity = new ServiceIdentity[1];
                    clientProfileArray.clientServiceInstanceV1[0].serviceIdentity[0] = new ServiceIdentity();
                    clientProfileArray.clientServiceInstanceV1[0].serviceIdentity[0].action = ACTION_SEARCH;
                    clientProfileArray.clientServiceInstanceV1[0].serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                    clientProfileArray.clientServiceInstanceV1[0].serviceIdentity[0].value = identity;

                    clientIdentityList1 = new List<ClientIdentity>();
                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = RVSID;
                    clientIdentity.value = dnpParameters.RVSID;
                    clientIdentity.action = ACTION_SEARCH;
                    clientIdentityList1.Add(clientIdentity);

                    clientIdentity = new ClientIdentity();
                    clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity.managedIdentifierDomain.value = IMSI;
                    clientIdentity.value = dnpParameters.IMSI;
                    clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity.clientIdentityStatus.value = ACTIVE;
                    clientIdentity.action = ACTION_INSERT;

                    List<ClientIdentityValidation> clientIdentityValidationList1 = new List<ClientIdentityValidation>();
                    ClientIdentityValidation clientIdentityValidation1 = null;
                    clientIdentityValidation1 = new ClientIdentityValidation();
                    clientIdentityValidation1.name = "PUK_CODE";
                    clientIdentityValidation1.value = dnpParameters.PukCode;
                    clientIdentityValidation1.action = ACTION_INSERT;
                    clientIdentityValidationList1.Add(clientIdentityValidation1);

                    clientIdentityValidation1 = new ClientIdentityValidation();
                    clientIdentityValidation1.name = "SIM_CARD_TYPE";
                    clientIdentityValidation1.value = dnpParameters.SimCardType;
                    clientIdentityValidation1.action = ACTION_INSERT;
                    clientIdentityValidationList1.Add(clientIdentityValidation1);

                    clientIdentityValidation1 = new ClientIdentityValidation();
                    clientIdentityValidation1.name = "SSN";
                    clientIdentityValidation1.value = dnpParameters.SSN;
                    clientIdentityValidation1.action = ACTION_INSERT;
                    clientIdentityValidationList1.Add(clientIdentityValidation1);
                    clientIdentity.clientIdentityValidation = clientIdentityValidationList1.ToArray();
                    clientIdentityList1.Add(clientIdentity);

                    ClientServiceRole[] clientServiceRole1 = new ClientServiceRole[1];

                    clientServiceRole1[0] = new ClientServiceRole();
                    clientServiceRole1[0].id = DEFAULT_ROLE;
                    clientServiceRole1[0].clientServiceRoleStatus = new ClientServiceRoleStatus();
                    clientServiceRole1[0].clientServiceRoleStatus.value = ACTIVE;
                    clientServiceRole1[0].clientIdentity = new ClientIdentity[2];
                    clientServiceRole1[0].clientIdentity[0] = new ClientIdentity();
                    clientServiceRole1[0].clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientServiceRole1[0].clientIdentity[0].managedIdentifierDomain.value = RVSID;
                    clientServiceRole1[0].clientIdentity[0].value = dnpParameters.RVSID;
                    clientServiceRole1[0].clientIdentity[0].action = ACTION_SEARCH;
                    clientServiceRole1[0].clientIdentity[1] = new ClientIdentity();
                    clientServiceRole1[0].clientIdentity[1].managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientServiceRole1[0].clientIdentity[1].managedIdentifierDomain.value = IMSI;
                    clientServiceRole1[0].clientIdentity[1].value = dnpParameters.IMSI;
                    clientServiceRole1[0].clientIdentity[1].action = ACTION_INSERT;
                    clientServiceRole1[0].action = ACTION_UPDATE;

                    clientProfileArray.clientServiceInstanceV1[0].clientServiceRole = clientServiceRole1.ToArray();

                    clientProfileArray.client.clientIdentity = clientIdentityList1.ToArray();
                    clientProfileArrayList.Add(clientProfileArray);
                }

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, SPRING_GSM_SERVICECODE_NAMEPACE);

                standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                standardHeader.e2e.E2EDATA = e2eData.toString();

                batchProfileRequest.manageBatchProfilesV1Request.standardHeader = standardHeader;
                batchProfileRequest.manageBatchProfilesV1Request.ManageBatchProfilesV1Req = clientProfileArrayList.ToArray();

                Logger.Write(orderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.SpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, orderKey, batchProfileRequest.SerializeObject(), dnpParameters.ProductCode);

                batchResponse = SpringDnpWrapper.manageBatchProfilesV1(batchProfileRequest, orderKey);

                Logger.Write(orderKey + "," + GotResponsefromDNP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.SpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, orderKey, batchResponse.SerializeObject(), dnpParameters.ProductCode);

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

                    if (inviteroles != null && inviteroles.Count() > 0)
                    {
                        string getinviterolestring = MSEOSaaSMapper.GetinviteRoleandBAClist(inviteroles, string.Empty);
                        UpdateInviteRoleswithNewMSIDN(dnpParameters.oldIMSI, dnpParameters.IMSI, "IMSI", dnpParameters.Billingaccountnumber, getinviterolestring, dnpParameters, orderKey, notification, ref e2eData);
                    }
                    else
                    {
                        notification.sendNotification(true, false, "004", string.Empty, ref e2eData,true);
                        Logger.Write(orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                    }
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

                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.SpringExceptionTrace);
                            Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.SpringMessageTrace);
                        }
                        else
                        {
                            Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.SpringMessageTrace);
                        }

                        notification.sendNotification(false, false, "777", errorMessage, ref e2eData,true);
                        Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                    }
                    else
                    {
                        Logger.Write(orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.SpringMessageTrace);

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");

                        if (batchResponse != null && batchResponse.manageBatchProfilesV1Response != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null
                            && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA != null &&
                            !String.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))

                            e2eData.endOutboundCall(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData,true);
                        Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
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

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData,true);
                Logger.Write(orderKey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.SpringExceptionTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
            }
        }

        /// <summary>
        /// BTRCE- 111909 Delete Invite roles recived by SIM in same account transfer
        /// </summary>
        /// <param name="BACNumber"></param>
        /// <param name="MSISDNvalue"></param>
        /// <param name="inviteroles"></param>
        /// <param name="dnpParameters"></param>
        /// <param name="orderKey"></param>
        /// <param name="notification"></param>
        /// <param name="e2eData"></param> 
        public void DeleteInviteRoles(string BACNumber, string MSISDNvalue, string inviteroles, SpringParameters dnpParameters, string orderKey, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            manageClientProfileV1Response1 profileResponse = null;
            manageClientProfileV1Request1 profileRequest = new manageClientProfileV1Request1();
            profileRequest.manageClientProfileV1Request = new ManageClientProfileV1Request();

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

            ManageClientProfileV1Req manageClientProfileV1Req1 = new ManageClientProfileV1Req();
            manageClientProfileV1Req1.clientProfileV1 = new ClientProfileV1();
            List<ClientServiceInstanceV1> clientserviceinstanceList = new List<ClientServiceInstanceV1>();
            List<ClientIdentity> clientIdentityList = new List<ClientIdentity>();
            ClientServiceInstanceV1 clientServiceInstanceV1 = new ClientServiceInstanceV1();
            ClientIdentity clientIdentity = null;
            List<ClientIdentityValidation> clientIdentityvalidationList = new List<ClientIdentityValidation>();
            ClientIdentityValidation clientIdentityValidation = null;

            try
            {
                clientIdentity = new ClientIdentity();
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = MOBILE_MSISDN_NAMESPACE;
                clientIdentity.value = dnpParameters.MSISDN;
                clientIdentity.action = ACTION_SEARCH;

                clientIdentityValidation = new ClientIdentityValidation();
                clientIdentityValidation.name = "AUTH_PIN";
                clientIdentityValidation.value = "000000";
                clientIdentityValidation.action = ACTION_FORCE_INSERT;
                clientIdentityvalidationList.Add(clientIdentityValidation);
                clientIdentity.clientIdentityValidation = clientIdentityvalidationList.ToArray();

                clientIdentityList.Add(clientIdentity);

                string[] rolekeys = inviteroles.ToString().Split(',');
                foreach (string rolekey in rolekeys)
                {
                    string[] rolekeyList = rolekey.ToString().Split(':');
                    clientServiceInstanceV1 = new ClientServiceInstanceV1();
                    clientServiceInstanceV1 = CreateinviteServiceinstanceobject(ACTION_DELETE, rolekeyList[0].ToString(), rolekeyList[1].ToString());
                    clientserviceinstanceList.Add(clientServiceInstanceV1);
                }

                manageClientProfileV1Req1.clientProfileV1.client = new Client();
                manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_UPDATE;

                manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientserviceinstanceList.ToArray();


                profileRequest.manageClientProfileV1Request.standardHeader = headerBlock;
                profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, SPRING_GSM_SERVICECODE_NAMEPACE);

                profileRequest.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                profileRequest.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                Logger.Write(orderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.SpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, orderKey, profileRequest.SerializeObject(), dnpParameters.ProductCode);

                profileResponse = SpringDnpWrapper.manageClientProfileV1ForSpring(profileRequest, orderKey);

                Logger.Write(orderKey + "," + GotResponsefromDNP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.SpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, orderKey, profileResponse.SerializeObject(), dnpParameters.ProductCode);

                if (profileResponse != null && profileResponse.manageClientProfileV1Response != null
                    && profileResponse.manageClientProfileV1Response.standardHeader != null
                    && profileResponse.manageClientProfileV1Response.standardHeader.serviceState != null
                    && profileResponse.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                    && profileResponse.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    {
                        e2eData.endOutboundCall(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                    }
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData,true);
                    Logger.Write(orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                }
                else
                {
                    if (profileResponse != null && profileResponse.manageClientProfileV1Response != null
                        && profileResponse.manageClientProfileV1Response.manageClientProfileV1Res != null
                        && profileResponse.manageClientProfileV1Response.manageClientProfileV1Res.messages != null
                        && profileResponse.manageClientProfileV1Response.manageClientProfileV1Res.messages[0] != null
                        && profileResponse.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                        if (profileResponse.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.SpringExceptionTrace);
                            Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.SpringMessageTrace);
                        }
                        else
                        {
                            Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.SpringMessageTrace);
                        }

                        notification.sendNotification(false, false, "777", errorMessage, ref e2eData,true);
                        Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                    }
                    else
                    {
                        Logger.Write(orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.SpringMessageTrace);

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse != null && profileResponse.manageClientProfileV1Response != null && profileResponse.manageClientProfileV1Response.standardHeader != null && profileResponse.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData,true);
                        Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);

                if (profileResponse != null && profileResponse.manageClientProfileV1Response != null
                            && profileResponse.manageClientProfileV1Response.standardHeader != null
                            && profileResponse.manageClientProfileV1Response.standardHeader.e2e != null
                            && profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null &&
                            !String.IsNullOrEmpty(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))

                    e2eData.endOutboundCall(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData,true);
                Logger.Write(orderKey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.SpringExceptionTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
            }
            finally
            {
                dnpParameters = null;
            }
        }


        /// <summary>
        /// BTRCE-111909 creating service instance to delete invite role 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="roleKey"></param>
        /// <param name="invitorBAC"></param>
        /// <returns></returns> 
        public ClientServiceInstanceV1 CreateinviteServiceinstanceobject(string action, string roleKey, string invitorBAC)
        {
            ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
            serviceIdentity[0] = new ServiceIdentity();
            serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
            serviceIdentity[0].value = invitorBAC;
            serviceIdentity[0].action = ACTION_SEARCH;

            ClientServiceInstanceV1 clientsrvcinstance = new ClientServiceInstanceV1();
            clientsrvcinstance.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
            clientsrvcinstance.clientServiceInstanceIdentifier.value = "SPRING_GSM";
            clientsrvcinstance.clientServiceInstanceStatus = new ClientServiceInstanceStatus();
            clientsrvcinstance.clientServiceInstanceStatus.value = ACTIVE;
            clientsrvcinstance.serviceIdentity = serviceIdentity;
            clientsrvcinstance.action = ACTION_UPDATE;
            ClientServiceRole clientServiceRole = new ClientServiceRole();

            clientServiceRole = new ClientServiceRole();
            clientServiceRole.id = "INVITE";
            clientServiceRole.name = roleKey;
            clientServiceRole.action = ACTION_DELETE;

            clientsrvcinstance.clientServiceRole = new ClientServiceRole[1];
            clientsrvcinstance.clientServiceRole[0] = clientServiceRole;
            clientsrvcinstance.action = ACTION_UPDATE;

            return clientsrvcinstance;
        }

        //--------------Murali----------
        public static ClientServiceInstanceV1 CreatedelegateServiceinstanceobject(string action, string roleKey, string rolename, string servicename, string bacid)
        {
            ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
            serviceIdentity[0] = new ServiceIdentity();
            serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
            serviceIdentity[0].value = bacid;
            serviceIdentity[0].action = ACTION_SEARCH;

            ClientServiceInstanceV1 clientServiceInstanceV1 = new ClientServiceInstanceV1();

            clientServiceInstanceV1 = new ClientServiceInstanceV1();
            clientServiceInstanceV1.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
            clientServiceInstanceV1.clientServiceInstanceIdentifier.value = servicename;
            clientServiceInstanceV1.clientServiceInstanceStatus = new ClientServiceInstanceStatus();
            clientServiceInstanceV1.clientServiceInstanceStatus.value = ACTIVE;
            clientServiceInstanceV1.serviceIdentity = serviceIdentity;
            clientServiceInstanceV1.action = ACTION_UPDATE;

            ClientServiceRole clientServiceRole = new ClientServiceRole();
            clientServiceRole = new ClientServiceRole();

            clientServiceRole.id = roleKey;
            clientServiceRole.name = rolename;
            clientServiceRole.action = ACTION_DELETE;

            clientServiceInstanceV1.clientServiceRole = new ClientServiceRole[1];
            clientServiceInstanceV1.clientServiceRole[0] = clientServiceRole;

            return clientServiceInstanceV1;
        }
        //---------------------
        /// <summary>
        /// BTRCE-111909 need to update newMSIDN value in case of Port in journey.
        /// </summary>
        /// <param name="oldMSISDN"></param>
        /// <param name="newMSISDN"></param>
        /// <param name="inviterBAC"></param>
        /// <param name="inviteroles"></param>
        /// <param name="dnpParameters"></param>
        /// <param name="orderKey"></param>
        /// <param name="notification"></param>
        /// <param name="e2eData"></param>
        public void UpdateInviteRoleswithNewMSIDN(string oldidentity, string newidentity, string domain, string inviterBAC, string inviteroles, SpringParameters dnpParameters, string orderKey, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            manageClientProfileV1Response1 profileResponse = null;
            manageClientProfileV1Request1 profileRequest = new manageClientProfileV1Request1();
            profileRequest.manageClientProfileV1Request = new ManageClientProfileV1Request();

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

            ManageClientProfileV1Req manageClientProfileV1Req1 = new ManageClientProfileV1Req();
            manageClientProfileV1Req1.clientProfileV1 = new ClientProfileV1();
            List<ClientServiceInstanceV1> clientserviceinstanceList = new List<ClientServiceInstanceV1>();
            List<ClientIdentity> clientIdentityList = new List<ClientIdentity>();
            ClientServiceInstanceV1 clientsrvcinstance = new ClientServiceInstanceV1();
            ClientIdentity clientIdentity = null;
            List<ClientIdentityValidation> clientIdentityvalidationList = new List<ClientIdentityValidation>();
            ClientIdentityValidation clientIdentityValidation = null;

            try
            {
                clientIdentity = new ClientIdentity();
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = domain;
                clientIdentity.value = newidentity;
                clientIdentity.action = ACTION_SEARCH;
                clientIdentityList.Add(clientIdentity);


                string[] rolekeys = inviteroles.ToString().Split(',');
                foreach (string rolekey in rolekeys)
                {
                    string[] rolekeyList = rolekey.ToString().Split(':');
                    clientsrvcinstance = new ClientServiceInstanceV1();
                    ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                    serviceIdentity[0] = new ServiceIdentity();
                    serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                    serviceIdentity[0].value = rolekeyList[1].ToString();
                    serviceIdentity[0].action = ACTION_SEARCH;

                    clientsrvcinstance.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                    clientsrvcinstance.clientServiceInstanceIdentifier.value = "SPRING_GSM";
                    clientsrvcinstance.clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                    clientsrvcinstance.clientServiceInstanceStatus.value = ACTIVE;
                    clientsrvcinstance.serviceIdentity = serviceIdentity;
                    clientsrvcinstance.action = ACTION_UPDATE;
                    ClientServiceRole clientServiceRole = new ClientServiceRole();

                    clientServiceRole = new ClientServiceRole();
                    clientServiceRole.id = "INVITE";
                    clientServiceRole.name = rolekeyList[0].ToString();
                    clientServiceRole.action = ACTION_UPDATE;

                    clientServiceRole.clientIdentity = new ClientIdentity[1];
                    //clientServiceRole.clientIdentity[0] = new ClientIdentity();
                    //clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                    //clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = domain;
                    //clientServiceRole.clientIdentity[0].value = oldidentity;
                    //clientServiceRole.clientIdentity[0].action = ACTION_DELETE;

                    clientServiceRole.clientIdentity[0] = new ClientIdentity();
                    clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = domain;
                    clientServiceRole.clientIdentity[0].value = newidentity;
                    clientServiceRole.clientIdentity[0].action = ACTION_INSERT;

                    clientsrvcinstance.clientServiceRole = new ClientServiceRole[1];
                    clientsrvcinstance.clientServiceRole[0] = clientServiceRole;

                    clientserviceinstanceList.Add(clientsrvcinstance);
                }

                manageClientProfileV1Req1.clientProfileV1.client = new Client();
                manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_UPDATE;

                manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();
                manageClientProfileV1Req1.clientProfileV1.clientServiceInstanceV1 = clientserviceinstanceList.ToArray();


                profileRequest.manageClientProfileV1Request.standardHeader = headerBlock;
                profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, SPRING_GSM_SERVICECODE_NAMEPACE);

                profileRequest.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                profileRequest.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                Logger.Write(orderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.SpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, orderKey, profileRequest.SerializeObject(), dnpParameters.ProductCode);

                profileResponse = SpringDnpWrapper.manageClientProfileV1ForSpring(profileRequest, orderKey);

                Logger.Write(orderKey + "," + GotResponsefromDNP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.SpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, orderKey, profileResponse.SerializeObject(), dnpParameters.ProductCode);

                if (profileResponse != null && profileResponse.manageClientProfileV1Response != null
                    && profileResponse.manageClientProfileV1Response.standardHeader != null
                    && profileResponse.manageClientProfileV1Response.standardHeader.serviceState != null
                    && profileResponse.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                    && profileResponse.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    {
                        e2eData.endOutboundCall(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                    }
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData,true);
                    Logger.Write(orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                }
                else
                {
                    if (profileResponse != null && profileResponse.manageClientProfileV1Response != null
                        && profileResponse.manageClientProfileV1Response.manageClientProfileV1Res != null
                        && profileResponse.manageClientProfileV1Response.manageClientProfileV1Res.messages != null
                        && profileResponse.manageClientProfileV1Response.manageClientProfileV1Res.messages[0] != null
                        && profileResponse.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                        if (profileResponse.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.SpringExceptionTrace);
                            Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.SpringMessageTrace);
                        }
                        else
                        {
                            Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.SpringMessageTrace);
                        }

                        notification.sendNotification(false, false, "777", errorMessage, ref e2eData,true);
                        Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                    }
                    else
                    {
                        Logger.Write(orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.SpringMessageTrace);

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse != null && profileResponse.manageClientProfileV1Response != null && profileResponse.manageClientProfileV1Response.standardHeader != null && profileResponse.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData,true);
                        Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);

                if (profileResponse != null && profileResponse.manageClientProfileV1Response != null
                            && profileResponse.manageClientProfileV1Response.standardHeader != null
                            && profileResponse.manageClientProfileV1Response.standardHeader.e2e != null
                            && profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null &&
                            !String.IsNullOrEmpty(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))

                    e2eData.endOutboundCall(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData,true);
                Logger.Write(orderKey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.SpringExceptionTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
            }
            finally
            {
                dnpParameters = null;
            }

        }

        //pavan 
        /// <summary>
        /// Method to prepare request for DNP call - Shareplans
        /// </summary>
        /// <param name="requestOrderRequest">Input Order from OFS</param>
        /// <param name="e2eData"></param>
        public void SharedPlanesCrossAcountRequestMapper(OrderRequest requestOrderRequest, ref E2ETransaction e2eData)
        {
            #region declare
            string orderKey = string.Empty;
            string sourceBAC = string.Empty;
            string targetBAC = string.Empty;

            string imsi = string.Empty;
            string msisdn = string.Empty;
            string rvsid = string.Empty;
            string oldRvsIdSOurce = string.Empty;
            string targetRvsid = string.Empty;
            bool ServiceInstanceToBeDeleted = false;
            bool isTargetAHT = false;
            bool isSourceAHT = false;
            bool isExistingAccountTarget = false;
            bool isSpringsrvcExistsinTarget = false;

            string delegateSpring = string.Empty;//---------Murali-----------
            string delegateSport = string.Empty;//------Murali
            string delegateWifi = string.Empty;//------Murali


            MSEOOrderNotification notification = null;
            SpringParameters dnpParameters = new SpringParameters();

            manageBatchProfilesV1Response1 batchResponse = null;
            manageBatchProfilesV1Request1 batchProfileRequest = null;

            //BT.SaaS.IspssAdapter.Dnp.ClientIdentity clientIdentity = null;
            # endregion

            //-----------Murali--------
            ClientServiceInstanceV1[] gsiResponse = null;
            List<ClientIdentity> CIDelegateList = null;
            ClientIdentity ciDelegate = null;
            List<ClientServiceInstanceV1> CSIDelegateList = null;
            ClientServiceInstanceV1 csiDelegate = new ClientServiceInstanceV1();
            ClientProfileV1 clinetprofile = null;
            //--------------
            try
            {
                orderKey = requestOrderRequest.Order.OrderIdentifier.Value;
                notification = new MSEOOrderNotification(requestOrderRequest);

                foreach (MSEO.OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                {

                    string role_key_GSM = string.Empty;
                    #region populate values
                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("SourceGSMBAC", StringComparison.OrdinalIgnoreCase)))
                        sourceBAC = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("SourceGSMBAC", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;

                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("TargetGSMBAC", StringComparison.OrdinalIgnoreCase)))
                        targetBAC = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("TargetGSMBAC", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;

                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("IMSI", StringComparison.OrdinalIgnoreCase)))
                        imsi = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("IMSI", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;

                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("MSISDN", StringComparison.OrdinalIgnoreCase)))
                        msisdn = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("MSISDN", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;

                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("RVSID", StringComparison.OrdinalIgnoreCase)))
                        rvsid = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("RVSID", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;



                    #endregion
                    //call MangeBatchprofile -- for that prepare input parameter
                    //step:1 - prepare standardheader
                    batchProfileRequest = new manageBatchProfilesV1Request1();
                    batchProfileRequest.manageBatchProfilesV1Request = new ManageBatchProfilesV1Request();
                    List<SharedPlanesClientIdentityValidation> lstCIvals = new List<SharedPlanesClientIdentityValidation>();
                    List<ClientServiceInstanceV1> clientserviceInstanceList = new List<ClientServiceInstanceV1>();
                    List<string> inviteroles = new List<string>();
                    //code get 
                    GetBatchProfileV1Res ProfileResponse = null;
                    ProfileResponse = SpringDnpWrapper.GetServiceUserProfilesV1ForSpring(sourceBAC, BACID_IDENTIFER_NAMEPACE,orderKey);
                    if (ProfileResponse != null && ProfileResponse.clientProfileV1 != null)
                    {
                        foreach (ClientProfileV1 clientProfile in ProfileResponse.clientProfileV1)
                        {
                            if ((clientProfile.clientServiceInstanceV1 != null) && (clientProfile.clientServiceInstanceV1.ToList().Exists(ip => ip.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase))))
                            {
                                ClientServiceInstanceV1 SpringServiceInstance = new ClientServiceInstanceV1();
                                SpringServiceInstance = clientProfile.clientServiceInstanceV1.ToList().Where(ip => ip.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                if (clientProfile.client.clientIdentity.ToList().Exists(ic => ic.managedIdentifierDomain.value.Equals("MOBILE_MSISDN", StringComparison.OrdinalIgnoreCase) && ic.value.Equals(msisdn, StringComparison.OrdinalIgnoreCase)))
                                {

                                    //string rvsid = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower() == "rvsid").FirstOrDefault().Value;
                                    if (SpringServiceInstance.clientServiceRole.ToList().Where(ic => ic.id.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase)).ToList().Count == 1)
                                    {
                                        ServiceInstanceToBeDeleted = true;
                                    }
                                    else
                                    {
                                        if (clientProfile.clientServiceInstanceV1 != null)
                                        {
                                            if (clientProfile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(RVSID, StringComparison.OrdinalIgnoreCase)))
                                                oldRvsIdSOurce = clientProfile.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(RVSID, StringComparison.OrdinalIgnoreCase) && !ci.value.Equals(rvsid, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                        }
                                    }
                                    foreach (ClientServiceRole role in clientProfile.clientServiceInstanceV1.Where(si => si.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.Ordinal)).FirstOrDefault().clientServiceRole)
                                    {
                                        if (role.clientIdentity != null && role.id.ToLower() == "default" && role.clientIdentity.ToList().Exists(ci => ci.value.Equals(rvsid, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            role_key_GSM = role.name;
                                            break;
                                        }
                                    }
                                }
                                if (clientProfile.client.clientIdentity.ToList().Exists(ic => ic.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                {
                                    var ci = clientProfile.client.clientIdentity.ToList().Where(ic => ic.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                    if (ci.clientIdentityValidation != null)
                                        isSourceAHT = ci.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));

                                }
                                foreach (ClientIdentity ids in clientProfile.client.clientIdentity)
                                {
                                    if (ids.clientIdentityValidation != null)
                                    {
                                        SharedPlanesClientIdentityValidation indiClientidentval = new SharedPlanesClientIdentityValidation();
                                        List<ClientIdentity> CharclientIdentityList = new List<ClientIdentity>();
                                        foreach (ClientIdentityValidation ivds in ids.clientIdentityValidation)
                                        {
                                            ClientIdentity objci = new ClientIdentity();
                                            objci.identityAlias = ivds.name;
                                            objci.value = ivds.value;
                                            CharclientIdentityList.Add(objci);
                                        }
                                        indiClientidentval.identifiervalue = ids.value;
                                        indiClientidentval.identifiername = ids.managedIdentifierDomain.value;
                                        indiClientidentval.clientIdentity = CharclientIdentityList.ToArray();
                                        lstCIvals.Add(indiClientidentval);
                                    }
                                }
                            }
                        }

                    }
                    else
                    {
                        notification.sendNotification(false, false, "001", "Ignored as there is no profile for given sourcebacID in DnP", ref e2eData,true);
                        Logger.Write(orderKey + "," + Ignored + "," + IgnoredNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                    }
                    GetBatchProfileV1Res ProfileResponseTarget = null;
                    ProfileResponseTarget = SpringDnpWrapper.GetServiceUserProfilesV1ForSpring(targetBAC, BACID_IDENTIFER_NAMEPACE,orderKey);
                    if (ProfileResponseTarget != null && ProfileResponseTarget.clientProfileV1 != null)
                    {

                        foreach (ClientProfileV1 clientProfile in ProfileResponseTarget.clientProfileV1)
                        {
                            if (clientProfile.clientServiceInstanceV1 != null && clientProfile.clientServiceInstanceV1.ToList().Exists(SI => SI.serviceIdentity.ToList().Exists(sID => sID.domain.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.OrdinalIgnoreCase) && sID.value.Equals(targetBAC, StringComparison.OrdinalIgnoreCase))))
                            {
                                if ((clientProfile.clientServiceInstanceV1 != null) && (clientProfile.clientServiceInstanceV1.ToList().Exists(ip => ip.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase))))
                                {
                                    isSpringsrvcExistsinTarget = true;
                                }

                                if (clientProfile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(targetBAC, StringComparison.OrdinalIgnoreCase)))
                                {
                                    isExistingAccountTarget = true;
                                    BT.SaaS.IspssAdapter.Dnp.ClientIdentity bacClientIdentity = clientProfile.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(targetBAC, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                    if (bacClientIdentity.clientIdentityValidation != null)
                                        isTargetAHT = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                                }

                                if (clientProfile.client.clientIdentity.ToList().Exists(city => city.managedIdentifierDomain.value.Equals(RVSID, StringComparison.OrdinalIgnoreCase)))
                                    targetRvsid = clientProfile.client.clientIdentity.ToList().Where(city => city.managedIdentifierDomain.value.Equals(RVSID, StringComparison.OrdinalIgnoreCase) && !city.value.Equals(rvsid, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                            }
                        }
                    }
                    else
                    {
                        GetClientProfileV1Res gcp_response = null;
                        gcp_response = SpringDnpWrapper.GetClientProfileV1ForSpring(targetBAC, BACID_IDENTIFER_NAMEPACE, orderKey, "create");
                        if (gcp_response != null && gcp_response.clientProfileV1 != null && gcp_response.clientProfileV1.client != null &&
                            gcp_response.clientProfileV1.client.clientIdentity != null)
                        {
                            if (gcp_response.clientProfileV1.client.clientIdentity.ToList().Exists(id => id.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                            {
                                isExistingAccountTarget = true;
                                var ci = gcp_response.clientProfileV1.client.clientIdentity.ToList().Where(id => id.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                if (ci.clientIdentityValidation != null)
                                    isTargetAHT = ci.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                            }
                        }
                    }
                    notification.sendNotification(true, true, string.Empty, string.Empty, ref e2eData,false);
                    Logger.Write(orderKey + "," + Accepted + "," + AcceptedNotificationSent, Logger.TypeEnum.SpringMessageTrace);

                    #region SHeader
                    BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
                    standardHeader.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
                    standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
                    standardHeader.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();
                    batchProfileRequest.manageBatchProfilesV1Request.standardHeader = standardHeader;
                    # endregion
                    //step:2 prepare ClientProfiles for source and target
                    List<ClientProfileV1> clientProfileList = new List<ClientProfileV1>();// list of client profiles -- each client profile consitst many client identites  like RVSID,IMSI,MSISDN
                    #region clientprofile - source
                    //step 2-A - populate ClientIdentity
                    List<ClientIdentity> clientIdentityList = new List<ClientIdentity>();
                    //pavan


                    ClientIdentity clientIdentity = null;

                    Client client = new Client();
                    client.action = ACTION_UPDATE;

                    clientIdentity = new ClientIdentity();
                    if (isSourceAHT)
                    {
                        clientIdentity.value = sourceBAC; // SOURCE BAC 
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientIdentity.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                    }
                    else
                    {
                        if (ServiceInstanceToBeDeleted)
                        {
                            client.action = ACTION_DELETE;
                            clientIdentity.value = msisdn;
                            clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                            clientIdentity.managedIdentifierDomain.value = MOBILE_MSISDN_NAMESPACE;
                        }
                        else
                        {
                            clientIdentity.value = oldRvsIdSOurce;
                            clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                            clientIdentity.managedIdentifierDomain.value = RVSID;
                        }
                    }

                    clientIdentity.action = ACTION_SEARCH;
                    clientIdentityList.Add(clientIdentity);

                    if (isSourceAHT || (!ServiceInstanceToBeDeleted && !isSourceAHT))
                    {
                        //client Identity - 2
                        clientIdentity = new ClientIdentity();
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        //logic to get Client validations and add it ti client identity
                        List<ClientIdentityValidation> objinsCV = new List<ClientIdentityValidation>();
                        if (lstCIvals.ToList().Exists(ic => ic.identifiername.Equals("imsi", StringComparison.OrdinalIgnoreCase) && ic.identifiervalue.Equals(imsi, StringComparison.OrdinalIgnoreCase)))
                        {
                            //List<SharedPlanesClientIdentityValidation> llnew = new List<SharedPlanesClientIdentityValidation>();
                            for (int i = 0; i < lstCIvals.Count(); i++)
                            {
                                if (lstCIvals[i].identifiervalue.Equals(imsi, StringComparison.OrdinalIgnoreCase))
                                {
                                    for (int s = 0; s < lstCIvals[i].clientIdentity.Count(); s++)
                                    {
                                        ClientIdentityValidation clientidentval4 = new ClientIdentityValidation();
                                        {
                                            clientidentval4.value = lstCIvals[i].clientIdentity[s].value;
                                            clientidentval4.name = lstCIvals[i].clientIdentity[s].identityAlias;
                                            clientidentval4.action = ACTION_DELETE;
                                            objinsCV.Add(clientidentval4);
                                            clientIdentity.clientIdentityValidation = objinsCV.ToArray();
                                        }
                                    }
                                }
                            }
                        }
                        clientIdentity.managedIdentifierDomain.value = IMSI;
                        clientIdentity.value = imsi;
                        clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                        clientIdentity.clientIdentityStatus.value = ACTIVE;
                        clientIdentity.action = ACTION_DELETE;
                        clientIdentityList.Add(clientIdentity);

                        //client Identity - 3
                        clientIdentity = new ClientIdentity();
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientIdentity.managedIdentifierDomain.value = RVSID;

                        //logic to get Client validations and add it to client identity
                        List<ClientIdentityValidation> objinsCV2 = new List<ClientIdentityValidation>();
                        if (lstCIvals.ToList().Exists(ic => ic.identifiername.Equals("rvsid", StringComparison.OrdinalIgnoreCase) && ic.identifiervalue.Equals(rvsid, StringComparison.OrdinalIgnoreCase)))
                        {
                            //List<SharedPlanesClientIdentityValidation> llnew = new List<SharedPlanesClientIdentityValidation>();
                            for (int i = 0; i < lstCIvals.Count(); i++)
                            {
                                if (lstCIvals[i].identifiervalue.Equals(rvsid, StringComparison.OrdinalIgnoreCase))
                                {
                                    for (int s = 0; s < lstCIvals[i].clientIdentity.Count(); s++)
                                    {
                                        ClientIdentityValidation clientidentval4 = new ClientIdentityValidation();
                                        {
                                            clientidentval4.value = lstCIvals[i].clientIdentity[s].value;
                                            clientidentval4.name = lstCIvals[i].clientIdentity[s].identityAlias;
                                            clientidentval4.action = ACTION_DELETE;
                                            objinsCV2.Add(clientidentval4);
                                            clientIdentity.clientIdentityValidation = objinsCV2.ToArray();
                                        }
                                    }
                                }
                            }
                        }

                        clientIdentity.value = rvsid;
                        clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                        clientIdentity.clientIdentityStatus.value = ACTIVE;
                        clientIdentity.action = ACTION_DELETE;
                        clientIdentityList.Add(clientIdentity);

                        clientIdentity = new ClientIdentity();
                        clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientIdentity.managedIdentifierDomain.value = MOBILE_MSISDN_NAMESPACE;

                        //logic to get Client validations and add it to client identity
                        List<ClientIdentityValidation> objinsCV3 = new List<ClientIdentityValidation>();
                        if (lstCIvals.ToList().Exists(ic => ic.identifiername.Equals(MOBILE_MSISDN_NAMESPACE, StringComparison.OrdinalIgnoreCase) && ic.identifiervalue.Equals(msisdn, StringComparison.OrdinalIgnoreCase)))
                        {
                            //List<SharedPlanesClientIdentityValidation> llnew = new List<SharedPlanesClientIdentityValidation>();
                            for (int i = 0; i < lstCIvals.Count(); i++)
                            {
                                if (lstCIvals[i].identifiervalue.Equals(msisdn, StringComparison.OrdinalIgnoreCase))
                                {
                                    for (int s = 0; s < lstCIvals[i].clientIdentity.Count(); s++)
                                    {
                                        ClientIdentityValidation clientidentval4 = new ClientIdentityValidation();
                                        {
                                            clientidentval4.value = lstCIvals[i].clientIdentity[s].value;
                                            clientidentval4.name = lstCIvals[i].clientIdentity[s].identityAlias;
                                            clientidentval4.action = ACTION_DELETE;
                                            objinsCV3.Add(clientidentval4);
                                            clientIdentity.clientIdentityValidation = objinsCV3.ToArray();
                                        }
                                    }
                                }
                            }
                        }

                        clientIdentity.value = msisdn;
                        clientIdentity.clientIdentityStatus = new ClientIdentityStatus();
                        clientIdentity.clientIdentityStatus.value = ACTIVE;
                        clientIdentity.action = ACTION_DELETE;
                        clientIdentityList.Add(clientIdentity);
                    }
                    ////////////////// Now add ClientIdentity list to Client object/////////////////                  

                    client.clientIdentity = clientIdentityList.ToArray();



                    //Step 2-B populate ServiceIdentity
                    ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                    serviceIdentity[0] = new ServiceIdentity();
                    serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                    serviceIdentity[0].value = sourceBAC;
                    serviceIdentity[0].action = ACTION_SEARCH;

                    //Step 2-C populate  Client service Instance
                    ClientServiceInstanceV1 clientServiceInstanceV1 = new ClientServiceInstanceV1();
                    clientServiceInstanceV1 = new ClientServiceInstanceV1();
                    clientServiceInstanceV1.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                    clientServiceInstanceV1.clientServiceInstanceIdentifier.value = "SPRING_GSM";
                    clientServiceInstanceV1.clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                    clientServiceInstanceV1.clientServiceInstanceStatus.value = ACTIVE;

                    clientServiceInstanceV1.serviceIdentity = serviceIdentity; // add serviceIdentity to ClientServiceInstance
                    clientServiceInstanceV1.action = ACTION_UPDATE;

                    //step 2-D populate client service roles.
                    List<ClientServiceRole> clientServiceRoleList = new List<ClientServiceRole>();
                    ClientServiceRole clientServiceRole = new ClientServiceRole();

                    clientServiceRole = new ClientServiceRole();
                    clientServiceRole.id = DEFAULT_ROLE;
                    clientServiceRole.name = role_key_GSM;
                    clientServiceRole.action = ACTION_DELETE;
                    clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                    clientServiceRole.clientServiceRoleStatus.value = ACTIVE;
                    clientServiceRoleList.Add(clientServiceRole);
                    if (ServiceInstanceToBeDeleted && isSourceAHT)
                    {
                        clientServiceRole = new ClientServiceRole();
                        clientServiceRole.id = ADMIN_ROLE;
                        clientServiceRole.clientServiceRoleStatus = new ClientServiceRoleStatus();
                        clientServiceRole.clientServiceRoleStatus.value = ACTIVE;
                        clientServiceRole.clientIdentity = new ClientIdentity[1];
                        clientServiceRole.clientIdentity[0] = new ClientIdentity();
                        clientServiceRole.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientServiceRole.clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                        clientServiceRole.clientIdentity[0].value = sourceBAC;
                        clientServiceRole.clientIdentity[0].action = ACTION_SEARCH;

                        clientServiceRole.action = ACTION_DELETE;
                        clientServiceRoleList.Add(clientServiceRole);
                    }



                    clientServiceInstanceV1.clientServiceRole = new ClientServiceRole[clientServiceRoleList.Count];
                    clientServiceInstanceV1.clientServiceRole = clientServiceRoleList.ToArray(); // add ClientServiceroles to ClientServiceInstance
                    clientserviceInstanceList.Add(clientServiceInstanceV1);

                    inviteroles = MSEOSaaSMapper.GetInviterolesList("GetinviterolesbyUser", msisdn, "MOBILE_MSISDN", string.Empty);
                    if (inviteroles != null && inviteroles.Count > 0)
                    {
                        string[] rolekeys = inviteroles.ToArray();
                        foreach (string rolekey in rolekeys)
                        {
                            string[] rolekeyList = rolekey.ToString().Split(':');
                            clientServiceInstanceV1 = new ClientServiceInstanceV1();
                            clientServiceInstanceV1 = CreateinviteServiceinstanceobject(ACTION_DELETE, rolekeyList[0].ToString(), rolekeyList[1].ToString());
                            clientserviceInstanceList.Add(clientServiceInstanceV1);
                        }
                    }

                    //Now add ClientServiceInstance and Client to Client-profile
                    ClientProfileV1 clntprof = new ClientProfileV1();
                    clntprof.client = client;
                    clntprof.clientServiceInstanceV1 = clientserviceInstanceList.ToArray();
                    clientProfileList.Add(clntprof);


                    //---------Murali-delegaterole delete--------
                    //BTR-82064 deleting delegate role againtst RVSID-----------

                    gsiResponse = DnpWrapper.getServiceInstanceV1(sourceBAC, BACID_IDENTIFER_NAMEPACE, string.Empty);
                    MSEOSaaSMapper.GetDelegaterolelist(gsiResponse, ref delegateSpring, rvsid, ref delegateSport, ref delegateWifi);


                    if (!string.IsNullOrEmpty(delegateSpring))
                    {
                        string[] delegaterole = delegateSpring.ToString().Split(',');
                        foreach (string delegate1 in delegaterole)
                        {
                            if (!string.IsNullOrEmpty(delegate1))
                            {
                                string[] delegarelist = delegate1.ToString().Split(';');
                                CIDelegateList = new List<ClientIdentity>();
                                CSIDelegateList = new List<ClientServiceInstanceV1>();

                                ciDelegate = new ClientIdentity();
                                ciDelegate.managedIdentifierDomain = new ManagedIdentifierDomain();
                                ciDelegate.managedIdentifierDomain.value = "BTCOM";
                                ciDelegate.value = delegarelist[2].ToString();
                                ciDelegate.action = ACTION_SEARCH;
                                CIDelegateList.Add(ciDelegate);

                                csiDelegate = new ClientServiceInstanceV1();

                                csiDelegate = CreatedelegateServiceinstanceobject(ACTION_DELETE, delegarelist[0].ToString(), delegarelist[1].ToString(), "SPRING_GSM", sourceBAC);

                                CSIDelegateList.Add(csiDelegate);


                                clinetprofile = new ClientProfileV1();
                                clinetprofile.client = new Client();
                                clinetprofile.client.action = ACTION_UPDATE;
                                clinetprofile.client.clientIdentity = CIDelegateList.ToArray();
                                clinetprofile.clientServiceInstanceV1 = CSIDelegateList.ToArray();
                                clientProfileList.Add(clinetprofile);
                            }

                        }
                    }
                    if (!string.IsNullOrEmpty(delegateSport))
                    {
                        string[] delegaterole = delegateSport.ToString().Split(',');
                        foreach (string delegate1 in delegaterole)
                        {
                            if (!string.IsNullOrEmpty(delegate1))
                            {
                                string[] delegarelist = delegate1.ToString().Split(';');
                                CIDelegateList = new List<ClientIdentity>();
                                CSIDelegateList = new List<ClientServiceInstanceV1>();

                                ciDelegate = new ClientIdentity();
                                ciDelegate.managedIdentifierDomain = new ManagedIdentifierDomain();
                                ciDelegate.managedIdentifierDomain.value = "BTCOM";
                                ciDelegate.value = delegarelist[2].ToString();
                                ciDelegate.action = ACTION_SEARCH;
                                CIDelegateList.Add(ciDelegate);

                                csiDelegate = new ClientServiceInstanceV1();

                                csiDelegate = CreatedelegateServiceinstanceobject(ACTION_DELETE, delegarelist[0].ToString(), delegarelist[1].ToString(), "BTSPORT:DIGITAL", sourceBAC);

                                CSIDelegateList.Add(csiDelegate);


                                clinetprofile = new ClientProfileV1();
                                clinetprofile.client = new Client();
                                clinetprofile.client.action = ACTION_UPDATE;
                                clinetprofile.client.clientIdentity = CIDelegateList.ToArray();
                                clinetprofile.clientServiceInstanceV1 = CSIDelegateList.ToArray();
                                clientProfileList.Add(clinetprofile);
                            }

                        }
                    }
                    if (!string.IsNullOrEmpty(delegateWifi))
                    {
                        string[] delegaterole = delegateWifi.ToString().Split(',');
                        foreach (string delegate1 in delegaterole)
                        {
                            if (!string.IsNullOrEmpty(delegate1))
                            {
                                string[] delegarelist = delegate1.ToString().Split(';');
                                CIDelegateList = new List<ClientIdentity>();
                                CSIDelegateList = new List<ClientServiceInstanceV1>();

                                ciDelegate = new ClientIdentity();
                                ciDelegate.managedIdentifierDomain = new ManagedIdentifierDomain();
                                ciDelegate.managedIdentifierDomain.value = "BTCOM";
                                ciDelegate.value = delegarelist[2].ToString();
                                ciDelegate.action = ACTION_SEARCH;
                                CIDelegateList.Add(ciDelegate);

                                csiDelegate = new ClientServiceInstanceV1();

                                csiDelegate = CreatedelegateServiceinstanceobject(ACTION_DELETE, delegarelist[0].ToString(), delegarelist[1].ToString(), "BTWIFI:DEFAULT", sourceBAC);

                                CSIDelegateList.Add(csiDelegate);


                                clinetprofile = new ClientProfileV1();
                                clinetprofile.client = new Client();
                                clinetprofile.client.action = ACTION_UPDATE;
                                clinetprofile.client.clientIdentity = CIDelegateList.ToArray();
                                clinetprofile.clientServiceInstanceV1 = CSIDelegateList.ToArray();
                                clientProfileList.Add(clinetprofile);
                            }

                        }
                    }
                    //----------------------

                    # endregion clientprofile - source

                    #region clientprofile - target
                    //step 2-A - populate ClientIdentity
                    List<ClientIdentity> clientIdentityList2 = new List<ClientIdentity>();

                    ClientIdentity clientIdentity2 = null;


                    //add the imsi,rvsid,msisdn of source to target
                    clientIdentity2 = new ClientIdentity();
                    clientIdentity2.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity2.managedIdentifierDomain.value = IMSI;

                    List<ClientIdentityValidation> targetobjinsCV1 = new List<ClientIdentityValidation>();
                    if (lstCIvals.ToList().Exists(ic => ic.identifiername.Equals("imsi", StringComparison.OrdinalIgnoreCase) && ic.identifiervalue.Equals(imsi, StringComparison.OrdinalIgnoreCase)))
                    {
                        //List<SharedPlanesClientIdentityValidation> llnew = new List<SharedPlanesClientIdentityValidation>();
                        for (int i = 0; i < lstCIvals.Count(); i++)
                        {
                            if (lstCIvals[i].identifiervalue.Equals(imsi, StringComparison.OrdinalIgnoreCase))
                            {
                                for (int s = 0; s < lstCIvals[i].clientIdentity.Count(); s++)
                                {
                                    ClientIdentityValidation clientidentval4 = new ClientIdentityValidation();
                                    {
                                        clientidentval4.value = lstCIvals[i].clientIdentity[s].value;
                                        clientidentval4.name = lstCIvals[i].clientIdentity[s].identityAlias;
                                        clientidentval4.action = ACTION_INSERT;
                                        targetobjinsCV1.Add(clientidentval4);
                                        clientIdentity2.clientIdentityValidation = targetobjinsCV1.ToArray();
                                    }
                                }
                            }
                        }
                    }
                    clientIdentity2.value = imsi;
                    clientIdentity2.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity2.clientIdentityStatus.value = ACTIVE;
                    clientIdentity2.action = ACTION_INSERT;
                    clientIdentityList2.Add(clientIdentity2);

                    clientIdentity2 = new ClientIdentity();
                    clientIdentity2.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity2.managedIdentifierDomain.value = RVSID;
                    //logic to get Client validations and add it t0 client identity
                    List<ClientIdentityValidation> targetobjinsCV2 = new List<ClientIdentityValidation>();
                    if (lstCIvals.ToList().Exists(ic => ic.identifiername.Equals("rvsid", StringComparison.OrdinalIgnoreCase) && ic.identifiervalue.Equals(rvsid, StringComparison.OrdinalIgnoreCase)))
                    {
                        //List<SharedPlanesClientIdentityValidation> llnew = new List<SharedPlanesClientIdentityValidation>();
                        for (int i = 0; i < lstCIvals.Count(); i++)
                        {
                            if (lstCIvals[i].identifiervalue.Equals(rvsid, StringComparison.OrdinalIgnoreCase))
                            {
                                for (int s = 0; s < lstCIvals[i].clientIdentity.Count(); s++)
                                {
                                    ClientIdentityValidation clientidentval4 = new ClientIdentityValidation();
                                    {
                                        clientidentval4.value = lstCIvals[i].clientIdentity[s].value;
                                        clientidentval4.name = lstCIvals[i].clientIdentity[s].identityAlias;
                                        clientidentval4.action = ACTION_INSERT;
                                        targetobjinsCV2.Add(clientidentval4);
                                        clientIdentity2.clientIdentityValidation = targetobjinsCV2.ToArray();
                                    }
                                }
                            }
                        }
                    }

                    clientIdentity2.value = rvsid;
                    clientIdentity2.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity2.clientIdentityStatus.value = ACTIVE;
                    clientIdentity2.action = ACTION_INSERT;
                    clientIdentityList2.Add(clientIdentity2);

                    clientIdentity2 = new ClientIdentity();
                    clientIdentity2.managedIdentifierDomain = new ManagedIdentifierDomain();
                    clientIdentity2.managedIdentifierDomain.value = MOBILE_MSISDN_NAMESPACE;
                    //logic to get Client validations and add it t0 client identity
                    List<ClientIdentityValidation> targetobjinsCV3 = new List<ClientIdentityValidation>();
                    if (lstCIvals.ToList().Exists(ic => ic.identifiername.Equals(MOBILE_MSISDN_NAMESPACE, StringComparison.OrdinalIgnoreCase) && ic.identifiervalue.Equals(msisdn, StringComparison.OrdinalIgnoreCase)))
                    {
                        //List<SharedPlanesClientIdentityValidation> llnew = new List<SharedPlanesClientIdentityValidation>();
                        for (int i = 0; i < lstCIvals.Count(); i++)
                        {
                            if (lstCIvals[i].identifiervalue.Equals(msisdn, StringComparison.OrdinalIgnoreCase))
                            {
                                for (int s = 0; s < lstCIvals[i].clientIdentity.Count(); s++)
                                {
                                    ClientIdentityValidation clientidentval4 = new ClientIdentityValidation();
                                    {
                                        clientidentval4.value = lstCIvals[i].clientIdentity[s].value;
                                        clientidentval4.name = lstCIvals[i].clientIdentity[s].identityAlias;
                                        clientidentval4.action = ACTION_INSERT;
                                        targetobjinsCV3.Add(clientidentval4);
                                    }
                                }
                            }
                        }

                    }
                    if (!targetobjinsCV3.ToList().Exists(ci => ci.name.Equals("AUTH_PIN", StringComparison.OrdinalIgnoreCase)))
                    {
                        ClientIdentityValidation authPincIValidation = new ClientIdentityValidation();
                        authPincIValidation.name = "AUTH_PIN";
                        authPincIValidation.value = "000000";
                        authPincIValidation.action = ACTION_INSERT;
                        targetobjinsCV3.Add(authPincIValidation);
                    }
                    clientIdentity2.clientIdentityValidation = targetobjinsCV3.ToArray();
                    clientIdentity2.value = msisdn;
                    clientIdentity2.clientIdentityStatus = new ClientIdentityStatus();
                    clientIdentity2.clientIdentityStatus.value = ACTIVE;
                    clientIdentity2.action = ACTION_INSERT;
                    clientIdentityList2.Add(clientIdentity2);


                    ////////////////// Now add ClientIdentity list to Client object/////////////////
                    // batchProfileRequest.manageBatchProfilesV1Request.ManageBatchProfilesV1Req = new ClientProfileV1[0];
                    Client client2 = new Client();
                    ClientServiceInstanceV1 targetSrvcInstance = null;
                    List<ClientServiceInstanceV1> listTargetSrvcInstance = new List<ClientServiceInstanceV1>();
                    List<ClientServiceRole> clientServiceRoleList2 = new List<ClientServiceRole>();
                    ClientServiceRole clientServiceRole2 = null;

                    if (isSpringsrvcExistsinTarget)
                    {
                        client2.action = ACTION_UPDATE;

                        clientIdentity2 = new ClientIdentity();
                        clientIdentity2.value = targetRvsid;
                        clientIdentity2.managedIdentifierDomain = new ManagedIdentifierDomain();
                        clientIdentity2.managedIdentifierDomain.value = RVSID;
                        clientIdentity2.action = ACTION_SEARCH;
                        clientIdentityList2.Add(clientIdentity2);

                        //Step 2-B populate ServiceIdentity
                        ServiceIdentity[] serviceIdentity2 = new ServiceIdentity[1];
                        serviceIdentity2[0] = new ServiceIdentity();
                        serviceIdentity2[0].domain = BACID_IDENTIFER_NAMEPACE;
                        serviceIdentity2[0].value = targetBAC;
                        serviceIdentity2[0].action = ACTION_SEARCH;

                        //Step 2-C populate  Client service Instance
                        targetSrvcInstance = new ClientServiceInstanceV1();
                        targetSrvcInstance = new ClientServiceInstanceV1();
                        targetSrvcInstance.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                        targetSrvcInstance.clientServiceInstanceIdentifier.value = "SPRING_GSM";
                        targetSrvcInstance.clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                        targetSrvcInstance.clientServiceInstanceStatus.value = ACTIVE;

                        targetSrvcInstance.serviceIdentity = serviceIdentity2; // add serviceIdentity to ClientServiceInstance
                        targetSrvcInstance.action = ACTION_UPDATE;


                        //step 2-D populate client service roles.
                        clientServiceRole2 = new ClientServiceRole();
                        //step 2-e  populate clientidentities
                        List<ClientIdentity> sourceClientidenties = new List<ClientIdentity>();
                        clientServiceRole2 = new ClientServiceRole();
                        clientServiceRole2.id = DEFAULT_ROLE;
                        clientServiceRole2.clientIdentity = clientIdentityList2.ToArray();
                        clientServiceRole2.action = ACTION_INSERT;

                        clientServiceRole2.clientServiceRoleStatus = new ClientServiceRoleStatus();
                        clientServiceRole2.clientServiceRoleStatus.value = ACTIVE;

                        clientServiceRoleList2.Add(clientServiceRole2);
                        targetSrvcInstance.clientServiceRole = clientServiceRoleList2.ToArray(); // add ClientServiceroles to ClientServiceInstance
                        listTargetSrvcInstance.Add(targetSrvcInstance);
                    }
                    else
                    {
                        ServiceIdentity[] serviceIdentity3 = new ServiceIdentity[1];
                        serviceIdentity3[0] = new ServiceIdentity();
                        serviceIdentity3[0].domain = BACID_IDENTIFER_NAMEPACE;
                        serviceIdentity3[0].value = targetBAC;
                        serviceIdentity3[0].action = ACTION_LINK;

                        targetSrvcInstance = new ClientServiceInstanceV1();
                        targetSrvcInstance = new ClientServiceInstanceV1();
                        targetSrvcInstance.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                        targetSrvcInstance.clientServiceInstanceIdentifier.value = "SPRING_GSM";
                        targetSrvcInstance.clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                        targetSrvcInstance.clientServiceInstanceStatus.value = ACTIVE;
                        targetSrvcInstance.serviceIdentity = serviceIdentity3;
                        targetSrvcInstance.action = ACTION_INSERT;

                        if (isExistingAccountTarget)
                        {
                            client2.action = ACTION_UPDATE;
                            clientIdentity2 = new ClientIdentity();
                            clientIdentity2.managedIdentifierDomain = new ManagedIdentifierDomain();
                            clientIdentity2.managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                            clientIdentity2.value = targetBAC;
                            clientIdentity2.action = ACTION_SEARCH;

                            clientIdentityList2.Add(clientIdentity2);


                            if (isTargetAHT)
                            {
                                clientServiceRole2 = new ClientServiceRole();

                                clientServiceRole2 = new ClientServiceRole();
                                clientServiceRole2.id = ADMIN_ROLE;
                                clientServiceRole2.clientServiceRoleStatus = new ClientServiceRoleStatus();
                                clientServiceRole2.clientServiceRoleStatus.value = ACTIVE;
                                clientServiceRole2.clientIdentity = new ClientIdentity[1];
                                clientServiceRole2.clientIdentity[0] = new ClientIdentity();
                                clientServiceRole2.clientIdentity[0].managedIdentifierDomain = new ManagedIdentifierDomain();
                                clientServiceRole2.clientIdentity[0].managedIdentifierDomain.value = BACID_IDENTIFER_NAMEPACE;
                                clientServiceRole2.clientIdentity[0].value = targetBAC;
                                clientServiceRole2.clientIdentity[0].action = ACTION_INSERT;
                                clientServiceRole2.action = ACTION_INSERT;
                                clientServiceRoleList2.Add(clientServiceRole2);

                            }
                        }

                        else
                        {

                            client2.action = ACTION_INSERT;
                            client2.clientOrganisation = new ClientOrganisation();
                            client2.clientOrganisation.id = "BTRetailConsumer";
                            client2.type = "CUSTOMER";
                            client2.clientStatus = new ClientStatus();
                            client2.clientStatus.value = "ACTIVE";

                        }
                        //step 2-D populate client service roles.
                        clientServiceRole2 = new ClientServiceRole();
                        //step 2-e  populate clientidentities
                        List<ClientIdentity> sourceClientidenties = new List<ClientIdentity>();
                        clientServiceRole2 = new ClientServiceRole();
                        clientServiceRole2.id = DEFAULT_ROLE;
                        clientServiceRole2.clientIdentity = clientIdentityList2.ToArray();
                        clientServiceRole2.action = ACTION_INSERT;

                        clientServiceRole2.clientServiceRoleStatus = new ClientServiceRoleStatus();
                        clientServiceRole2.clientServiceRoleStatus.value = ACTIVE;

                        clientServiceRoleList2.Add(clientServiceRole2);
                        targetSrvcInstance.clientServiceRole = clientServiceRoleList2.ToArray(); // add ClientServiceroles to ClientServiceInstance
                        listTargetSrvcInstance.Add(targetSrvcInstance);

                    }

                    //Now add ClientServiceInstance and Client to Client-profile
                    client2.clientIdentity = clientIdentityList2.ToArray();
                    ClientProfileV1 clntprof2 = new ClientProfileV1();
                    clntprof2.client = client2;
                    clntprof2.clientServiceInstanceV1 = listTargetSrvcInstance.ToArray();
                    clientProfileList.Add(clntprof2);

                    # endregion clientprofile - source

                    //call dnp

                    batchProfileRequest.manageBatchProfilesV1Request.ManageBatchProfilesV1Req = clientProfileList.ToArray();


                    Logger.Write(orderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.SpringMessageTrace);
                    MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, orderKey, batchProfileRequest.SerializeObject(), dnpParameters.ProductCode);

                    batchResponse = SpringDnpWrapper.manageBatchProfilesV1(batchProfileRequest, orderKey);

                    Logger.Write(orderKey + "," + GotResponsefromDNP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.SpringMessageTrace);
                    MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, orderKey, batchResponse.SerializeObject(), dnpParameters.ProductCode);

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
                        if (ServiceInstanceToBeDeleted)
                        {
                            ManageServiceInstanceInDNP("SPRING_GSM", sourceBAC, dnpParameters, orderKey, notification, ref e2eData);
                        }
                        else
                        {
                            notification.sendNotification(true, false, "004", string.Empty, ref e2eData,true);
                            Logger.Write(orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                        }
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

                            if (errorMessage.Contains("Administrator"))
                            {
                                Logger.Write(orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.SpringExceptionTrace);
                                Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.SpringMessageTrace);
                            }
                            else
                            {
                                Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.SpringMessageTrace);
                            }

                            notification.sendNotification(false, false, "777", errorMessage, ref e2eData,true);
                            Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                        }
                        else
                        {
                            Logger.Write(orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.SpringMessageTrace);

                            e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");

                            if (batchResponse != null && batchResponse.manageBatchProfilesV1Response != null
                                && batchResponse.manageBatchProfilesV1Response.standardHeader != null
                                && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e != null
                                && batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA != null &&
                                !String.IsNullOrEmpty(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA))

                                e2eData.endOutboundCall(batchResponse.manageBatchProfilesV1Response.standardHeader.e2e.E2EDATA);
                            else
                                e2eData.endOutboundCall(e2eData.toString());
                            notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData,true);
                            Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                        }
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

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData,true);
                Logger.Write(orderKey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.SpringExceptionTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
            }
            finally
            {
                dnpParameters = null;
            }
        }

        /// <summary>
        /// deleting spring service instance in case of last sim cross account transfer.
        /// </summary>
        /// <param name="serviceCode"></param>
        /// <param name="SourceBAC"></param>
        /// <param name="dnpParameters"></param>
        /// <param name="orderkey"></param>
        /// <param name="notification"></param>
        /// <param name="e2eData"></param>
        public void ManageServiceInstanceInDNP(string serviceCode, string SourceBAC, SpringParameters dnpParameters, string orderkey, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {
            manageServiceInstanceV1Response1 profileResponse = null;
            manageServiceInstanceV1Request1 profileRequest = new manageServiceInstanceV1Request1();
            profileRequest.manageServiceInstanceV1Request = new ManageServiceInstanceV1Request();

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

            try
            {
                ManageServiceInstanceV1Req manageServiceInstanceV1Req1 = new ManageServiceInstanceV1Req();

                ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
                serviceIdentity[0] = new ServiceIdentity();
                serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
                serviceIdentity[0].value = SourceBAC;
                serviceIdentity[0].action = ACTION_SEARCH;

                ClientServiceInstanceV1 clientServiceInstanceV1 = new ClientServiceInstanceV1();

                clientServiceInstanceV1 = new ClientServiceInstanceV1();
                clientServiceInstanceV1.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                clientServiceInstanceV1.clientServiceInstanceIdentifier.value = serviceCode;
                clientServiceInstanceV1.serviceIdentity = serviceIdentity;
                clientServiceInstanceV1.action = ACTION_DELETE;

                manageServiceInstanceV1Req1.clientServiceInstanceV1 = clientServiceInstanceV1;

                profileRequest.manageServiceInstanceV1Request.manageServiceInstanceV1Req = manageServiceInstanceV1Req1;
                profileRequest.manageServiceInstanceV1Request.standardHeader = headerBlock;
                profileRequest.manageServiceInstanceV1Request.manageServiceInstanceV1Req = manageServiceInstanceV1Req1;

                Logger.Write(orderkey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.SpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, orderkey, profileRequest.SerializeObject(), dnpParameters.ProductCode);

                profileResponse = DnpWrapper.manageServiceInstanceV1Thomas(profileRequest, orderkey);

                Logger.Write(orderkey + "," + GotResponsefromDNP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.SpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, orderkey, profileResponse.SerializeObject(), dnpParameters.ProductCode);

                if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                    {
                        e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                    }
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData,true);
                    Logger.Write(orderkey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                }
                else
                {
                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null
                        && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res != null
                        && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages != null
                        && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0] != null
                        && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description;
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                        if (profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(orderkey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.SpringExceptionTrace);
                            Logger.Write(orderkey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.SpringMessageTrace);
                        }
                        else
                        {
                            Logger.Write(orderkey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.SpringMessageTrace);
                        }

                        notification.sendNotification(false, false, "777", errorMessage, ref e2eData,true);
                        Logger.Write(orderkey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                    }
                    else
                    {
                        Logger.Write(orderkey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.SpringMessageTrace);

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData,true);
                        Logger.Write(orderkey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);

                if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null
                            && profileResponse.manageServiceInstanceV1Response.standardHeader != null
                            && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null
                            && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null &&
                            !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))

                    e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData,true);
                Logger.Write(orderkey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.SpringExceptionTrace);
                Logger.Write(orderkey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
            }
            finally
            {
                dnpParameters = null;
            }
        }

        /// <summary>
        /// updating authpin and authleave 
        /// </summary>
        /// <returns></returns>
        public void UpdateAuthrizationStatus(string inviteeMSISDN, string reason, SpringParameters dnpParameters, string orderKey, MSEOOrderNotification notification, ref E2ETransaction e2eData)
        {

            manageClientProfileV1Response1 profileResponse = null;
            manageClientProfileV1Request1 profileRequest = new manageClientProfileV1Request1();
            profileRequest.manageClientProfileV1Request = new ManageClientProfileV1Request();

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

            ManageClientProfileV1Req manageClientProfileV1Req1 = new ManageClientProfileV1Req();
            manageClientProfileV1Req1.clientProfileV1 = new ClientProfileV1();

            List<ClientIdentity> clientIdentityList = new List<ClientIdentity>();

            ClientIdentity clientIdentity = null;

            try
            {

                clientIdentity = new ClientIdentity();
                clientIdentity.managedIdentifierDomain = new ManagedIdentifierDomain();
                clientIdentity.managedIdentifierDomain.value = MOBILE_MSISDN_NAMESPACE;
                clientIdentity.value = inviteeMSISDN;
                clientIdentity.action = ACTION_SEARCH;

                List<ClientIdentityValidation> clientIdentityValidationList = new List<ClientIdentityValidation>();
                ClientIdentityValidation clientIdentityValidation = null;
                if (!string.IsNullOrEmpty(reason) && reason.ToLower() == "authorise")
                {
                    clientIdentityValidation = new ClientIdentityValidation();
                    clientIdentityValidation.name = "AUTH_PIN";
                    clientIdentityValidation.value = GenerateAuthPin();
                    clientIdentityValidation.action = ACTION_FORCE_INSERT;
                    clientIdentityValidationList.Add(clientIdentityValidation);
                }
                else if (!string.IsNullOrEmpty(reason) && reason.ToLower() == "resetpin")
                {
                    clientIdentityValidation = new ClientIdentityValidation();
                    clientIdentityValidation.name = "AUTH_PIN";
                    clientIdentityValidation.value = "000000";
                    clientIdentityValidation.action = ACTION_FORCE_INSERT;
                    clientIdentityValidationList.Add(clientIdentityValidation);
                }
                clientIdentity.clientIdentityValidation = clientIdentityValidationList.ToArray();
                clientIdentityList.Add(clientIdentity);

                manageClientProfileV1Req1.clientProfileV1.client = new Client();
                manageClientProfileV1Req1.clientProfileV1.client.action = ACTION_UPDATE;
                manageClientProfileV1Req1.clientProfileV1.client.clientIdentity = clientIdentityList.ToArray();



                profileRequest.manageClientProfileV1Request.standardHeader = headerBlock;
                profileRequest.manageClientProfileV1Request.manageClientProfileV1Req = manageClientProfileV1Req1;

                e2eData.logMessage(StartedDnPCall, "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, SPRING_GSM_SERVICECODE_NAMEPACE);

                profileRequest.manageClientProfileV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                profileRequest.manageClientProfileV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                Logger.Write(orderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.SpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, orderKey, profileRequest.SerializeObject(), dnpParameters.ProductCode);

                profileResponse = SpringDnpWrapper.manageClientProfileV1ForSpring(profileRequest, orderKey);

                Logger.Write(orderKey + "," + GotResponsefromDNP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.SpringMessageTrace);
                MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, orderKey, profileResponse.SerializeObject(), dnpParameters.ProductCode);

                if (profileResponse != null && profileResponse.manageClientProfileV1Response != null
                    && profileResponse.manageClientProfileV1Response.standardHeader != null
                    && profileResponse.manageClientProfileV1Response.standardHeader.serviceState != null
                    && profileResponse.manageClientProfileV1Response.standardHeader.serviceState.stateCode != null
                    && profileResponse.manageClientProfileV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage(GotResponseFromDnP, "");
                    if (profileResponse.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                    {
                        e2eData.endOutboundCall(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                    }
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    notification.sendNotification(true, false, "004", string.Empty, ref e2eData,true);
                    Logger.Write(orderKey + "," + Completed + "," + CompletedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                }
                else
                {
                    if (profileResponse != null && profileResponse.manageClientProfileV1Response != null
                        && profileResponse.manageClientProfileV1Response.manageClientProfileV1Res != null
                        && profileResponse.manageClientProfileV1Response.manageClientProfileV1Res.messages != null
                        && profileResponse.manageClientProfileV1Response.manageClientProfileV1Res.messages[0] != null
                        && profileResponse.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description != null)
                    {
                        string errorMessage = profileResponse.manageClientProfileV1Response.manageClientProfileV1Res.messages[0].description;
                        e2eData.businessError(GotResponseFromDnPWithBusinessError, errorMessage);
                        if (profileResponse.manageClientProfileV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        if (errorMessage.Contains("Administrator"))
                        {
                            Logger.Write(orderKey + "," + Errored + "," + DnPAdminstratorFailedResponse + Process.GetCurrentProcess().ProcessName, Logger.TypeEnum.SpringExceptionTrace);
                            Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.SpringMessageTrace);
                        }
                        else
                        {
                            Logger.Write(orderKey + "," + Failed + "," + FailureResponseFromDnP + "," + errorMessage, Logger.TypeEnum.SpringMessageTrace);
                        }

                        notification.sendNotification(false, false, "777", errorMessage, ref e2eData,true);
                        Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                    }
                    else
                    {
                        Logger.Write(orderKey + "," + Failed + "," + NullResponseFromDnP, Logger.TypeEnum.SpringMessageTrace);

                        e2eData.businessError(GotResponseFromDnPWithBusinessError, "Response is null from DnP");
                        if (profileResponse != null && profileResponse.manageClientProfileV1Response != null && profileResponse.manageClientProfileV1Response.standardHeader != null && profileResponse.manageClientProfileV1Response.standardHeader.e2e != null && profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());

                        notification.sendNotification(false, false, "777", "Response is null from DnP", ref e2eData,true);
                        Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                e2eData.businessError(GotResponseFromDnPWithBusinessError, ex.Message);

                if (profileResponse != null && profileResponse.manageClientProfileV1Response != null
                            && profileResponse.manageClientProfileV1Response.standardHeader != null
                            && profileResponse.manageClientProfileV1Response.standardHeader.e2e != null
                            && profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA != null &&
                            !String.IsNullOrEmpty(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA))

                    e2eData.endOutboundCall(profileResponse.manageClientProfileV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                notification.sendNotification(false, false, "777", ex.Message, ref e2eData,true);
                Logger.Write(orderKey + "," + Errored + "," + FailedNotificationSent + "," + ex.Message, Logger.TypeEnum.SpringExceptionTrace);
                Logger.Write(orderKey + "," + Failed + "," + FailedNotificationSent, Logger.TypeEnum.SpringMessageTrace);
            }
        }

        /// <summary>
        /// BTRCE-111936 generating random Pin
        /// </summary>
        /// <returns></returns>
        public string GenerateAuthPin()
        {
            string authPin = string.Empty;
            Random generator = new Random();
            int number = generator.Next(000001, 999999);
            authPin = number.ToString();
            return authPin;
        }
        /// <summary>
        /// need to remove the spring service if spring service doesn't have any default roles
        /// </summary>
        /// <param name="serviceCode">Spring_GSM</param>
        /// <param name="e2edataforMSI">E2Edata</param>
        /// <returns></returns>
        public bool MSIForSpringDeletion(string serviceCode, string bac, string orderKey, ref E2ETransaction e2eData, ref string downStreamError)
        {
            manageServiceInstanceV1Response1 profileResponse;
            manageServiceInstanceV1Request1 profileRequest = new manageServiceInstanceV1Request1();
            profileRequest.manageServiceInstanceV1Request = new ManageServiceInstanceV1Request();

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

            ManageServiceInstanceV1Req manageServiceInstanceV1Req1 = new ManageServiceInstanceV1Req();

            ServiceIdentity[] serviceIdentity = new ServiceIdentity[1];
            serviceIdentity[0] = new ServiceIdentity();
            serviceIdentity[0].domain = BACID_IDENTIFER_NAMEPACE;
            serviceIdentity[0].value = bac;
            serviceIdentity[0].action = ACTION_SEARCH;

            ClientServiceInstanceV1 clientServiceInstanceV1 = new ClientServiceInstanceV1();

            clientServiceInstanceV1 = new ClientServiceInstanceV1();
            clientServiceInstanceV1.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
            clientServiceInstanceV1.clientServiceInstanceIdentifier.value = serviceCode;
            clientServiceInstanceV1.serviceIdentity = serviceIdentity;
            clientServiceInstanceV1.action = ACTION_DELETE;

            manageServiceInstanceV1Req1.clientServiceInstanceV1 = clientServiceInstanceV1;

            profileRequest.manageServiceInstanceV1Request.manageServiceInstanceV1Req = manageServiceInstanceV1Req1;
            profileRequest.manageServiceInstanceV1Request.standardHeader = headerBlock;
            profileRequest.manageServiceInstanceV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
            profileRequest.manageServiceInstanceV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();
            profileRequest.manageServiceInstanceV1Request.manageServiceInstanceV1Req = manageServiceInstanceV1Req1;

            e2eData.logMessage(StartedDnPCall, "");

            e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, SPRING_GSM_SERVICECODE_NAMEPACE);

            Logger.Write(orderKey + "," + MakingDnPCall + "," + SendingRequestToDNP, Logger.TypeEnum.SpringMessageTrace);
            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_IN_LOG, orderKey, profileRequest.SerializeObject(), "S0316296");

            profileResponse = SpringDnpWrapper.manageServiceInstanceV1(profileRequest, orderKey);

            Logger.Write(orderKey + "," + GotResponsefromDNP + "," + ReceivedResponsefromDNP, Logger.TypeEnum.SpringMessageTrace);
            MSEOSaaSMapper.WriteToLog(MSEOSaaSMapper.TypeOfLog.DNP_OUT_LOG, orderKey, profileResponse.SerializeObject(), "S0316296");
            
            if (profileResponse != null
                && profileResponse.manageServiceInstanceV1Response != null
                && profileResponse.manageServiceInstanceV1Response.standardHeader != null
                && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState != null
                && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode != null
                && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode == "0")
            {
                e2eData.logMessage(GotResponseFromDnP, "");
                if (profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                {
                    e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                }
                else
                    e2eData.endOutboundCall(e2eData.toString());
                return true;
            }
            else
            {               
                if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description != null)
                    downStreamError = "DeleteClient for Spring Product failed in DnP!!" + profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description;
                else
                    downStreamError = "Response is null from DnP";

                if (profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && !string.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

               
                return false;
            }
        }
    }
    public class SpringParameters
    {
        private string creditLimit = " ";

        public string CreditLimit
        {
            get { return creditLimit; }
            set { creditLimit = value; }
        }

        private string customerId = string.Empty;

        public string CustomerId
        {
            get { return customerId; }
            set { customerId = value; }
        }

        private string dataRoamingLimit = " ";

        public string DataRoamingLimit
        {
            get { return dataRoamingLimit; }
            set { dataRoamingLimit = value; }
        }

        private string EUOPTIN1 = " ";

        public string EUOPTIN
        {
            get { return EUOPTIN1; }
            set { EUOPTIN1 = value; }
        }

        private string IMSI1 = string.Empty;

        public string IMSI
        {
            get { return IMSI1; }
            set { IMSI1 = value; }
        }

        private string international = " ";

        public string International
        {
            get { return international; }
            set { international = value; }
        }

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

        private string MSISDN1 = string.Empty;

        public string MSISDN
        {
            get { return MSISDN1; }
            set { MSISDN1 = value; }
        }

        private string numSimDispatched = string.Empty;

        public string NumSimDispatched
        {
            get { return numSimDispatched; }
            set { numSimDispatched = value; }
        }

        private string oldRVSID = string.Empty;

        public string OldRVSID
        {
            get { return oldRVSID; }
            set { oldRVSID = value; }
        }

        private string pcProfile = " ";

        public string PCProfile
        {
            get { return pcProfile; }
            set { pcProfile = value; }
        }

        private string newPendingOrderValue = " ";

        public string NewPendingOrderValue
        {
            get { return newPendingOrderValue; }
            set { newPendingOrderValue = value; }
        }
        private string dnpPendingOrderValue = " ";

        public string DnpPendingOrderValue
        {
            get { return dnpPendingOrderValue; }
            set { dnpPendingOrderValue = value; }
        }

        private string oldPendingOrderValue = string.Empty;

        public string OldPendingOrderValue
        {
            get { return oldPendingOrderValue; }
            set { oldPendingOrderValue = value; }
        }

        private string pendingOptIn = " ";

        public string PendingOptIn
        {
            get { return pendingOptIn; }
            set { pendingOptIn = value; }
        }

        private string premiumRateService = " ";

        public string PremiumRateService
        {
            get { return premiumRateService; }
            set { premiumRateService = value; }
        }

        private string productCode = "S0316296";

        public string ProductCode
        {
            get { return productCode; }
        }

        private string productFamily = "spring";

        public string ProductFamily
        {
            get { return productFamily; }
        }

        private string pukCode = string.Empty;

        public string PukCode
        {
            get { return pukCode; }
            set { pukCode = value; }
        }

        private string roaming = " ";

        public string Roaming
        {
            get { return roaming; }
            set { roaming = value; }
        }

        private string RVSID1 = string.Empty;

        public string RVSID
        {
            get { return RVSID1; }
            set { RVSID1 = value; }
        }

        private bool serviceInstanceExists = false;

        public bool ServiceInstanceExists
        {
            get { return serviceInstanceExists; }
            set { serviceInstanceExists = value; }
        }

        private string simCardType = string.Empty;

        public string SimCardType
        {
            get { return simCardType; }
            set { simCardType = value; }
        }

        private string SSN1 = string.Empty;

        public string SSN
        {
            get { return SSN1; }
            set { SSN1 = value; }
        }

        private string oldIMSIValue = string.Empty;

        public string oldIMSI
        {
            get { return oldIMSIValue; }
            set { oldIMSIValue = value; }
        }


        private string oldMSISDNValue = string.Empty;

        public string oldMSISDN
        {
            get { return oldMSISDNValue; }
            set { oldMSISDNValue = value; }
        }

        private string reason = string.Empty;

        public string Reason
        {
            get { return reason; }
            set { reason = value; }
        }

        private bool replacementReasonValue = false;

        public bool replacementReason
        {
            get { return replacementReasonValue; }
            set { replacementReasonValue = value; }
        }
        private string billingaccountnumber = string.Empty;

        public string Billingaccountnumber
        {
            get { return billingaccountnumber; }
            set { billingaccountnumber = value; }
        }
        private List<string> inviteroles;
        public List<string> Inviteroles
        {
            get { return inviteroles; }
            set { inviteroles = value; }
        }
    }

    //pavan    
    public partial class SharedPlanesClientIdentityValidation
    {

        //private string nameField;

        //private string valueField;
        private string identifiervalueField;
        private string identifiernameField;
        private ClientIdentity[] clientIdentityField;

        public ClientIdentity[] clientIdentity
        {
            get
            {
                return this.clientIdentityField;
            }
            set
            {
                this.clientIdentityField = value;
            }
        }


        public string identifiervalue
        {
            get
            {
                return this.identifiervalueField;
            }
            set
            {
                this.identifiervalueField = value;
            }
        }
        public string identifiername
        {
            get
            {
                return this.identifiernameField;
            }
            set
            {
                this.identifiernameField = value;
            }
        }
    }
}
