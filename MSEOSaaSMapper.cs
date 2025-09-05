using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using MSEO = BT.SaaS.MSEOAdapter;
using SaaSNS = BT.SaaS.Core.Shared.Entities;
using BT.SaaS.IspssAdapter;
using BT.SaaS.Core.MDMAPI.Entities;
using BT.SaaS.Core.MDMAPI.Entities.Product;
using BT.SaaS.Core.Shared.Entities;
using System.Collections.Generic;
using BT.SaaS.IspssAdapter.Dnp;
using BT.SaaS.IspssAdapter.PE;
using com.bt.util.logging;
using System.Globalization;
using BT.ESB.RoBT.ManageCustomer;

namespace BT.SaaS.MSEOAdapter
{
    public partial class MSEOSaaSMapper
    {
        private const string PremiumEmailProductCode = "1430";
        private const string BasicEmailProductCode = "1460";
        private const string ACTION_SEARCH = "SEARCH";
        private const string ACTION_UPDATE = "UPDATE";
        private const string ACTION_FORCE_INS = "FORCE_INS";

        public MSEOSaaSMapper()
        {

        }


        #region Submit PE Order

        public static void SubmitOrder(SaaSNS.Order order, OrderRequest requestOrderRequest, ref E2ETransaction Mye2etxn)
        {
            try
            {
                bool isAck = true;
                MSEOOrderNotification notification = new MSEOOrderNotification(requestOrderRequest);
                SaaSNS.StandardHeader standardHeader = new BT.SaaS.Core.Shared.Entities.StandardHeader();

                standardHeader.serviceAddressing = new BT.SaaS.Core.Shared.Entities.ServiceAddressing();
                standardHeader.serviceAddressing.from = requestOrderRequest.StandardHeader.ServiceAddressing.From;

                if (order.ProductOrderItems.Count > 0)
                {
                    using (OrderClient orderClient = new OrderClient())
                    {
                        SaaSNS.OrderResponse response = orderClient.SubmitOrder(standardHeader, order);

                        if (response == null)
                        {
                            throw new Exception("Provisioning Engine returned a null response");
                        }
                        else
                        {
                            notification.sendNotification(response.result.result, isAck, string.Empty, string.Empty, ref Mye2etxn);
                            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("The Order " + response.order.Header.OrderID + " was placed successfully");
                        }
                    }
                }
                else
                {
                    if (order.Header != null && order.Header.Customer != null && order.Header.Customer.Attributes != null
                    && order.Header.Customer.Attributes.ToList().Exists(a => a.Name.Equals("skipcacease", StringComparison.OrdinalIgnoreCase) && a.Value.Equals("true", StringComparison.OrdinalIgnoreCase)))
                    {
                        //Sending Accepted acknowledgement
                        notification.sendNotification(true, isAck, string.Empty, string.Empty, ref Mye2etxn);
                        //Completed response
                        notification.sendNotification(true, false, string.Empty, string.Empty, ref Mye2etxn);
                        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter : The Order " + order.Header.OrderKey + " has skipped successfully");
                    }
                    else
                    {
                        InboundQueueDAL.UpdateQueueRawXML(Settings1.Default.IgnoredStatus, "Product's in the request are Ignored", requestOrderRequest.Order.OrderIdentifier.Value);

                        foreach (OrderItem orderItem in requestOrderRequest.Order.OrderItem)
                        {
                            orderItem.Status = "Ignored";
                        }

                        notification.sendNotification(false, isAck, Settings1.Default.InvalideRequestCode, "The Product's in the request are Ignored because there is no defination for this product in SaaS", ref Mye2etxn);
                        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("The Order " + requestOrderRequest.Order.OrderIdentifier.Value + " do not have SaaS products");
                    }
                }
            }
            catch (System.ServiceModel.FaultException)
            {
                throw new Exception("***SaaSGlobalErrorHandler*** : Check event viewer logs with Event ID " + Settings1.Default.PeEventIdRef + " for more information");
            }
            catch (Exception ex)
            {
                throw new Exception("***SaaSGlobalErrorHandler*** : Check event viewer logs with Event ID " + Settings1.Default.PeEventIdRef + " for more information" + ex.Message);
            }
        }

        #endregion


        public static SaaSNS.Order MapRequest(MSEO.OrderRequest request)
        {
            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Mapping the request");
            SaaSNS.Order response = new BT.SaaS.Core.Shared.Entities.Order();
            System.DateTime orderDate = new System.DateTime();
            bool isOrderingRequired = false;
            string orderingProductCode = string.Empty;

            try
            {
                #region variables
                int productorderItemCount = 0;
                bool IsExistingCustomer = false;
                // Introduced to check for existingcustomer  - Praveen Kumar : 20 Aug 2010
                // Uncommented after the P2 issue.- FEb-11-2011.
                bool isVASReactivate = false;
                bool isContentAnyWhere = false;
                bool isContentFilter = false;
                string bacID = string.Empty;
                string msisdnValue = string.Empty;
                string oldMsisdnValue = string.Empty;
                string imsiValue = string.Empty;
                string oldImsiValue = string.Empty;
                string rvsidValue = string.Empty;
                string customerContextID = string.Empty;
                string subscriptionExternalReference = string.Empty;
                string previousSupplierCode = string.Empty;
                string numSimDispatched = string.Empty;
                bool IsExsitingBacID = false;
                bool ServiceInstanceExists = false;
                bool ServiceInstanceToBeDeleted = false;
                bool isSpringServiceExist = false;
                bool IsLastEmailId = false;
                string ServiceInstanceKey = string.Empty;
                string BtOneId = string.Empty;
                string BakId = string.Empty;
                string EmailName = string.Empty;
                string identity = string.Empty;
                string identityDomain = string.Empty;
                string ISP_Service_Class = string.Empty;
                bool IsOFSDanteProvide = false;
                string ReservationID = string.Empty;
                string cakID = string.Empty;
                bool isAHTDone = false;
                const string BACID_IDENTIFER_NAMEPACE = "VAS_BILLINGACCOUNT_ID";
                const string BTSKY_SERVICECODE_NAMEPACE = "BTSPORT:DIGITAL";
                const string BTEMAIL_SERVICECODE_NAMEPACE = "BTIEMAIL:DEFAULT";
                const string SPRING_GSM_SERVICECODE_NAMEPACE = "SPRING_GSM";
                const string RVSID_IDENTIFER_NAMEPACE = "RVSID";
                const string EE_MSISDN_NAMESPACE = "EE_MOBILE_MSISDN";
                string old_rvsid = string.Empty;
                string old_msisdn = string.Empty;
                string SIBacNumber = string.Empty;
                string primaryEmail = string.Empty;
                string targetVasprodId = string.Empty;
                string vasProductID = string.Empty;
                string status = string.Empty;
                string thomasCeaseReason = string.Empty;
                string serviceInstanceStatus = string.Empty;
                string emailDataStore = string.Empty;
                string role_key_GSM = string.Empty;

                string delegateSpring = string.Empty;//------Murali
                string delegateSport = string.Empty;//------Murali
                string delegateWifi = string.Empty;//------Murali


                string role_key_WIFI = string.Empty;
                string deviceID = string.Empty;
                string spring_wisprUserName = string.Empty;
                bool isDisabledProfile = false;
                bool isEESpringService = false;
                string wifiVasProductCode = string.Empty;
                const string OTADEVICEID_IDENTIFER_NAMEPACE = "OTA_DEVICE_ID";
                bool AdminRoleExists = false;
                bool TOTGExists = false;
                bool isBTTVModify = false;
                string VSIDoldValue = string.Empty;
                string cfsid = string.Empty;
                string serviceAddressingFrom = string.Empty;
                bool isScodeExist = false;
                string Sim_invite_roles = string.Empty;
                string Bac_invite_roles = string.Empty;
                string spownerBAcID = string.Empty;
                string InviteeBAC = string.Empty;
                string InviteID = string.Empty;
                string InviteeaccountKey = string.Empty;
                string rolestatus = string.Empty;
                bool IsHDRDelete = false;
                bool IsNayanWifi = false;
                //NAYANAGL - 23775
                bool IsWifiReactivate = false;
                bool IsBBFlagExist = false;
                bool IsWifiRoleInactive = false;
                #endregion

                response.Header.CeaseReason = request.SerializeObject();

                response.Header.OrderKey = request.Order.OrderIdentifier.Value;
                response.Header.ServiceProviderID = Settings1.Default.ConsumerServiceProviderId;
                

                if (request.Order.OrderItem[0].RequiredDateTime != null && request.Order.OrderItem[0].RequiredDateTime.DateTime1 != null)
                {
                    if (convertDateTime(request.Order.OrderItem[0].RequiredDateTime.DateTime1, ref orderDate))
                    {
                        response.Header.EffectiveDateTime = orderDate;
                    }
                    else
                    {
                        response.Header.EffectiveDateTime = System.DateTime.Now;
                    }
                }
                else
                {
                    response.Header.EffectiveDateTime = System.DateTime.Now;
                }
                response.Header.OrderDateTime = System.DateTime.Now;

                response.Header.Status = BT.SaaS.Core.Shared.Entities.OrderStatusEnum.@new;

                response.Header.Customer = new BT.SaaS.Core.Shared.Entities.Customer();
                response.Header.Customer.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.done;

                response.Header.Users = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.User>(1);
                SaaSNS.User user = new BT.SaaS.Core.Shared.Entities.User();
                user.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.done;
                response.Header.Users.Add(user);

                //// Check if the customer already exists - Praveen Kumar : 24 Aug 2010
                // Uncommented after the P2 issue.- FEb-11-2011.
                foreach (InstanceCharacteristic instancecharacterstic in request.Order.OrderItem[0].Instance[0].InstanceCharacteristic)
                {
                    if (instancecharacterstic.Name.ToLower() == "existingaccount")
                    {
                        if (instancecharacterstic.Value.ToLower() == "true")
                        {
                            IsExistingCustomer = true;
                            break;
                        }
                    }
                }

                switch (request.Order.OrderItem[0].Action.Code.ToLower())
                {
                    case ("create"):
                    case ("add"):
                        response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.provide;
                        // BTRCE-111936 in create invite to use modify service attribute
                        if (request.Order.OrderItem[0].Action.Reason != null && request.Order.OrderItem[0].Action.Reason.ToUpper() == "CREATEINVITE")
                        {
                            response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.modifyService;
                        }
                        break;
                    case ("cancel"):
                    case ("cease"):
                        if (request.StandardHeader.ServiceAddressing.From == Settings1.Default.BTComstring && request.Order.OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["VASContentAnyWhereProductFamily"], StringComparison.OrdinalIgnoreCase))
                            response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.modifyService;
                        else if (request.Order.OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["WifiProdCode"], StringComparison.OrdinalIgnoreCase) && request.Order.OrderItem[0].Action.Reason.Equals("opt-out", StringComparison.OrdinalIgnoreCase))
                        {
                            response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.modifyService;
                        }
                        else
                            response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.cease;
                        break;
                    case ("amend"):
                        response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.amend;
                        IsExistingCustomer = true;
                        break;
                    case ("regrade"):
                    case ("upgrade"):
                    case ("modify"):
                        if (request.StandardHeader.ServiceAddressing.From.Equals(ConfigurationManager.AppSettings["BBMTServiceAddress"]))
                        {
                            if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("status")) && !string.IsNullOrEmpty(request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("status")).FirstOrDefault().Value))
                            {
                                status = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("status")).FirstOrDefault().Value;

                                if (ConfigurationManager.AppSettings["MSEOSupportedBBMTModifyStatuses"].Split(',').Contains(status, StringComparer.OrdinalIgnoreCase))
                                {
                                    response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.cease;
                                }
                                else
                                {
                                    throw new Exception("Given Status in request from BBMT: " + status.ToUpper() + " is not supported by MSEO");
                                }
                            }
                        }
                        else if (request.Order.OrderItem.ToList().Exists(oi => oi.Specification.ToList().Exists(s => s.Identifier.Value1.Equals((ConfigurationManager.AppSettings["BTTVProdCode"]).ToString(), StringComparison.OrdinalIgnoreCase))))
                        {
                            response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.provide;
                        }
                        else if ((ConfigurationManager.AppSettings["EmailScodes"].Split(',').Contains(request.Order.OrderItem[0].Specification[0].Identifier.Value1) || ConfigurationManager.AppSettings["PremiumEmailScodes"].Split(',').Contains(request.Order.OrderItem[0].Specification[0].Identifier.Value1)))                        
                        {
                            //invoking cease workflow instead of modify workflow in case of OCB cease.
                            if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(inst => inst.Name.ToLower().Equals("suspendtype")))
                            {
                                if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(inst => inst.Name.ToLower().Equals("suspendaction")).FirstOrDefault().Value.Equals("Add", StringComparison.OrdinalIgnoreCase))
                                {
                                    thomasCeaseReason = "OCBEmailCease";
                                    response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.cease;
                                }
                                else
                                {
                                    thomasCeaseReason = "OCBUndobar";
                                    response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.vasReactivate;
                                }
                            }
                            else
                            {
                                response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.modifyService;
                            }
                        }
                        else
                        {
                            response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.modifyService;
                        }
                        IsExistingCustomer = true;
                        break;
                    case ("reactivate"):
                        if (request.Order.OrderItem[0].Action.Reason != null)
                        {
                            if (request.Order.OrderItem[0].Action.Reason.ToLower().Equals("undocompromise", StringComparison.OrdinalIgnoreCase) || request.Order.OrderItem[0].Action.Reason.ToLower().Equals("undobar", StringComparison.OrdinalIgnoreCase)
                                || (request.Order.OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"], StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(request.Order.OrderItem[0].Action.Reason))
                                || request.Order.OrderItem[0].Action.Reason.Equals("reinstate", StringComparison.OrdinalIgnoreCase))
                            {
                                response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.reActivate;
                            }
                            else if (((request.Order.OrderItem[0].Specification[0].Identifier.Value1.Equals("contentfiltering", StringComparison.OrdinalIgnoreCase) || request.Order.OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["WifiProdCode"], StringComparison.OrdinalIgnoreCase)) && request.Order.OrderItem[0].Action.Reason.Equals("opt-in", StringComparison.OrdinalIgnoreCase))
                                || (request.Order.OrderItem[0].Specification[0].Identifier.Value1.Equals("S0309824", StringComparison.OrdinalIgnoreCase) && request.Order.OrderItem[0].Action.Reason.Equals("debtresume", StringComparison.OrdinalIgnoreCase)))
                            {
                                response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.reActivate;
                            }
                            else
                            {
                                response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.vasReactivate;
                                isVASReactivate = true;
                                IsExistingCustomer = true;
                            }
                        }
                        // invoke reactivate workflow for Eamil reactivation scenarios
                        else if (request.Order.OrderItem[0].Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"], StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(request.Order.OrderItem[0].Action.Reason))
                        {
                            response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.reActivate;
                        }
                        else
                        {
                            response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.vasReactivate;
                            isVASReactivate = true;
                            IsExistingCustomer = true;
                        }
                        break;
                    default:
                        throw new Exception("Action not supported");
                }
                List<string> OldrbsidList = new List<string>();
                if (response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.vasReactivate)
                {
                    if (request.Order.OrderItem.ToList().Exists(s => s.Specification.ToList().Exists(i => i.Identifier.Value1.Equals(ConfigurationManager.AppSettings["VASContentAnyWhereProductFamily"].ToString(), StringComparison.OrdinalIgnoreCase))))
                    {
                        foreach (MSEO.OrderItem orderitem in request.Order.OrderItem)
                        {
                            if (orderitem.Instance[0].InstanceCharacteristic.Length > 0)
                            {
                                bacID = orderitem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value;
                                ClientServiceInstanceV1[] serviceInstances = null;
                                serviceInstances = DnpWrapper.GetVASClientServiceInstances(bacID, BACID_IDENTIFER_NAMEPACE);
                                if (serviceInstances != null)
                                {
                                    if (serviceInstances.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CAService"].ToString(), StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceStatus.value != "PENDING"))
                                    {
                                        if (serviceInstances.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CAService"].ToString(), StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceCharacteristic.Length > 0))
                                        {
                                            isContentAnyWhere = true;
                                            ClientServiceInstanceV1 serviceInstance = serviceInstances.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CAService"].ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                            if (serviceInstance != null)
                                            {
                                                subscriptionExternalReference = serviceInstance.clientServiceInstanceCharacteristic.ToList().Where(ic => ic.name.Equals("SUBSCRPTION_EXTERNAL", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                previousSupplierCode = serviceInstance.clientServiceInstanceCharacteristic.ToList().Where(ic => ic.name.Equals("PREV_SUPPLIER_CODE", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        throw new DnpException("Ignored as ContentAnywhere is in pending state");
                                    }
                                }
                            }
                        }
                    }
                }

                #region THOMAS Journeys
                else if (request.Order.OrderItem.ToList().Exists(s => s.Specification.ToList().Exists(i => i.Identifier.Value1.Equals("S0309824", StringComparison.OrdinalIgnoreCase))))
                {
                    string Scode = string.Empty;
                    string serviceID = string.Empty;
                    string reason = string.Empty;
                    string Super = string.Empty;
                    foreach (InstanceCharacteristic inschar in request.Order.OrderItem[0].Instance[0].InstanceCharacteristic)
                    {
                        if ((inschar.Name.Equals("BillAccountNumber", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(inschar.Value)))
                        {
                            bacID = inschar.Value;
                        }
                        else if ((inschar.Name.Equals("CustomerId", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(inschar.Value)))
                        {
                            cakID = inschar.Value;
                        }
                        else if ((inschar.Name.Equals("Scode", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(inschar.Value)))
                        {
                            Scode = inschar.Value;
                        }
                        else if ((inschar.Name.Equals("serviceid", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(inschar.Value)))
                        {
                            serviceID = inschar.Value;
                        }
                        else if ((inschar.Name.Equals("HDR_Add", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(inschar.Value)) && inschar.Value.Equals("y", StringComparison.OrdinalIgnoreCase))
                        {
                            IsHDRDelete = false;
                        }
                        else if ((inschar.Name.Equals("HDR_Delete", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(inschar.Value)) && inschar.Value.Equals("y", StringComparison.OrdinalIgnoreCase))
                        {
                            IsHDRDelete = true;
                        }
                        else if ((inschar.Name.Equals("Super", StringComparison.OrdinalIgnoreCase)) && !(string.IsNullOrEmpty(inschar.Value)))
                        {
                            Super = inschar.Value;
                        }
                    }
                    if ((request.Order.OrderItem[0].Action.Reason != null) && !(string.IsNullOrEmpty(request.Order.OrderItem[0].Action.Reason.ToString())))
                    {
                        reason = request.Order.OrderItem[0].Action.Reason.ToString();
                    }
                    // For Provide Jounreys
                    if (response.Header.Action == OrderActionEnum.provide)
                    {
                        foreach (MSEO.OrderItem orderItem in request.Order.OrderItem)
                        {
                            GetClientProfileV1Res bacProfileResponse = null;
                            ClientServiceInstanceV1[] cakServiceResponse = null;
                            ClientServiceInstanceV1[] bacServiceResponse = null;

                            if (!String.IsNullOrEmpty(bacID))
                            {
                                bacProfileResponse = DnpWrapper.GetClientProfileV1(bacID, BACID_IDENTIFER_NAMEPACE);
                                cakServiceResponse = DnpWrapper.getServiceInstanceV1(cakID, "CAKID", BTSKY_SERVICECODE_NAMEPACE);
                                if (cakServiceResponse == null)
                                {
                                    bacServiceResponse = DnpWrapper.getServiceInstanceV1(bacID, BACID_IDENTIFER_NAMEPACE, BTSKY_SERVICECODE_NAMEPACE);
                                }

                                if (bacProfileResponse != null && bacProfileResponse.clientProfileV1 != null)
                                {
                                    IsExsitingBacID = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(bacID, StringComparison.OrdinalIgnoreCase));
                                    if (IsExsitingBacID)
                                    {
                                        BT.SaaS.IspssAdapter.Dnp.ClientIdentity bacClientIdentity = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(bacID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                        if (bacClientIdentity.clientIdentityValidation != null)
                                            isAHTDone = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
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
                                    ServiceInstanceExists = serviceIntances.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BTSKY_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase));
                                    ClientServiceInstanceV1 serviceInstance = serviceIntances.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(BTSKY_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                    if (serviceInstance != null)
                                    {
                                        if (serviceInstance.clientServiceInstanceCharacteristic != null)
                                            serviceIntanceChars = serviceInstance.clientServiceInstanceCharacteristic;

                                        if (serviceIntanceChars != null && serviceIntanceChars.ToList().Exists(sic => sic.name.Equals("scode", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(sic.value)))
                                            isScodeExist = true;

                                        if (serviceInstance.serviceIdentity.ToList().Exists(sID => sID.domain.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.OrdinalIgnoreCase) && sID.value.Equals(bacID, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            serviceInstanceStatus = serviceInstance.clientServiceInstanceStatus.value;

                                            if (serviceIntanceChars != null && serviceIntanceChars.Length > 0)
                                            {
                                                if (serviceInstanceStatus.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase)
                                                    && serviceIntanceChars.ToList().Exists(ic => ic.name.Equals("BTAPP_SERVICE_ID", StringComparison.OrdinalIgnoreCase) && ic.value.Equals(serviceID, StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    throw new DnpException("BTSPORT:DIGITAL service is already exists for " + bacID + " in DNP");
                                                }
                                                //checking switch state and scode in DnP for reactivate
                                                else if (serviceInstanceStatus.Equals("ceased", StringComparison.OrdinalIgnoreCase)
                                                    && ConfigurationManager.AppSettings["DNPThomasMigrationSwitch"].ToString().Equals("off", StringComparison.OrdinalIgnoreCase) && !isScodeExist)
                                                {
                                                    throw new DnpException("Ignoring Reactivate request as DnP migration switch is OFF and scode doesnot exist in DnP");
                                                }
                                                else
                                                {
                                                    ServiceInstanceExists = true;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            ServiceInstanceExists = true;
                                        }
                                    }
                                }
                                else
                                {
                                    ServiceInstanceExists = false;
                                }
                            }
                            else
                            {
                                ServiceInstanceExists = false;
                            }
                        }
                    }
                    //for Thomas Cease and Suspend Journey's
                    else if (response.Header.Action == OrderActionEnum.cease)
                    {

                        string dnpScode = string.Empty;

                        ClientServiceInstanceV1[] clientServiceInstnce = DnpWrapper.getServiceInstanceV1(bacID, BACID_IDENTIFER_NAMEPACE, BTSKY_SERVICECODE_NAMEPACE);

                        if (clientServiceInstnce != null && clientServiceInstnce.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BTSKY_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                        {
                            ClientServiceInstanceV1 clientService = clientServiceInstnce.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(BTSKY_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                            if (reason != null && reason.Equals("suspend", StringComparison.OrdinalIgnoreCase))
                            {
                                if (clientService.clientServiceInstanceStatus.value.Equals("suspended", StringComparison.OrdinalIgnoreCase))
                                {
                                    throw new DnpException("BT Sports Serivce is in suspended state with the Bill Account Number" + bacID);
                                }
                                else
                                {
                                    thomasCeaseReason = reason;
                                }
                            }
                            else if (!clientService.clientServiceInstanceStatus.value.Equals("ceased", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(thomasCeaseReason))
                            {
                                if (clientService.clientServiceInstanceCharacteristic != null && clientService.clientServiceInstanceCharacteristic.ToList().Exists(si => si.name.Equals("SCODE", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(si.value)))
                                {
                                    dnpScode = clientService.clientServiceInstanceCharacteristic.ToList().Where(si => si.name.Equals("SCODE", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                }
                                if (string.IsNullOrEmpty(dnpScode) || Scode.Equals(dnpScode, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (clientService.clientServiceRole != null && clientService.clientServiceRole.Count() > 0)
                                    {
                                        if (clientService.clientServiceRole.ToList().Exists(sr => sr.id.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase)))
                                            thomasCeaseReason = "onbillaccount-yes";
                                        else
                                            thomasCeaseReason = "onbillaccount-delinked";
                                    }
                                    else
                                    {
                                        thomasCeaseReason = "onbillaccount-no";
                                    }
                                }
                                else
                                {
                                    throw new DnpException("Unable to find BT Sport service with specified Scode " + Scode + "for BAC " + bacID + " at DNP end");
                                }
                            }
                            else
                            {
                                throw new DnpException("BTSPORT service for the given BAC is in " + clientService.clientServiceInstanceStatus.value + " state.");
                            }
                        }
                        else if (string.IsNullOrEmpty(thomasCeaseReason))
                        {
                            ClientServiceInstanceV1[] clientServiceInstnceWithCAK = DnpWrapper.getServiceInstanceV1(cakID, "CAKID", BTSKY_SERVICECODE_NAMEPACE);

                            if (clientServiceInstnceWithCAK != null && clientServiceInstnceWithCAK.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BTSKY_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                            {
                                ClientServiceInstanceV1 clientService = clientServiceInstnceWithCAK.ToList().Where(si => si.clientServiceInstanceIdentifier.value.Equals(BTSKY_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                if (clientService.clientServiceInstanceCharacteristic != null && clientService.clientServiceInstanceCharacteristic.ToList().Exists(si => si.name.Equals("SCODE", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(si.value)))
                                {
                                    dnpScode = clientService.clientServiceInstanceCharacteristic.ToList().Where(si => si.name.Equals("SCODE", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                }
                                if (string.IsNullOrEmpty(dnpScode) || Scode.Equals(dnpScode, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (clientService.clientServiceInstanceStatus.value.ToLower() == "active")
                                    {
                                        thomasCeaseReason = "oncakid";
                                    }
                                    else
                                    {
                                        throw new DnpException("NO Any BTSPORT is in ACTIVE state with CAKID");
                                    }
                                }
                                else
                                {
                                    throw new DnpException("Unable to find BT Sport service with specified scode" + Scode + "for CAKID " + cakID + " at DNP end");
                                }
                            }
                            else
                            {
                                throw new DnpException("No BTSPORT Service in the DnP with BillAccount and Customer ID");
                            }
                        }
                        else throw new DnpException("No BT Sports Service in the DnP with the Bill Account Number" + bacID + " at DNP end");
                    }
                    //for Thomas Reacitvate Scenario
                    else if (response.Header.Action == OrderActionEnum.reActivate)
                    {
                        ClientServiceInstanceV1[] clientServiceInstnce = DnpWrapper.getServiceInstanceV1(bacID, BACID_IDENTIFER_NAMEPACE, BTSKY_SERVICECODE_NAMEPACE);

                        if (reason != null && reason.Equals("debtresume", StringComparison.OrdinalIgnoreCase))
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
                                    thomasCeaseReason = reason;
                                }
                            }
                            else
                            {
                                throw new DnpException("No BT Sports Service in the DnP with Bill Account Number");
                            }
                        }
                    }
                }
                #endregion

                #region WifiJourneys
                else if (request.Order.OrderItem.ToList().Exists(s => s.Specification.ToList().Exists(i => i.Identifier.Value1.Equals((ConfigurationManager.AppSettings["WifiProdCode"]), StringComparison.OrdinalIgnoreCase)))
                        && response.Header.Action == OrderActionEnum.provide)
                {
                    if (request.StandardHeader.ServiceAddressing.From.StartsWith(Settings1.Default.MQstring.ToString()))
                    {
                        foreach (MSEO.OrderItem orderItem in request.Order.OrderItem)
                        {
                            GetClientProfileV1Res bacProfileResponse = null;
                            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)))
                                bacID = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

                            if (!String.IsNullOrEmpty(bacID))
                            {
                                bacProfileResponse = DnpWrapper.GetClientProfileV1(bacID, BACID_IDENTIFER_NAMEPACE);

                                if (bacProfileResponse != null && bacProfileResponse.clientProfileV1 != null)
                                {
                                    BT.SaaS.IspssAdapter.Dnp.ClientIdentity bacClientIdentity = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(bacID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                    BtOneId = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                    if (bacClientIdentity.clientIdentityValidation != null)
                                        isAHTDone = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                                }

                            }
                        }
                    }
                    else
                    {
                        isAHTDone = true;
                    }

                    bacID = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                    ClientServiceInstanceV1[] serviceInstances = null;
                    serviceInstances = DnpWrapper.getServiceInstanceV1(bacID, "VAS_BILLINGACCOUNT_ID", String.Empty);
                    if (serviceInstances != null && serviceInstances.Count() > 0)
                    {
                        //NAYANAGL-61637
                        if (serviceInstances.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals("MANAGE_ACCOUNT", StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceCharacteristic != null && si.clientServiceInstanceCharacteristic.ToList().Exists(cschar => cschar.name.Equals("BACORGANISATIONNAME", StringComparison.OrdinalIgnoreCase) && cschar.value.StartsWith("EE", StringComparison.OrdinalIgnoreCase))))
                        {
                            IsNayanWifi = true;
                        }
                    }
                    //NAYANAGL - 23775
                    var service = (from si in serviceInstances where (si.clientServiceInstanceIdentifier.value.Equals("BTWIFI:DEFAULT", StringComparison.OrdinalIgnoreCase)) select si).FirstOrDefault();
                    if (service != null)
                    {                        
                            IsWifiReactivate = true;

                            if (service.clientServiceInstanceCharacteristic.ToList().Exists(csi => csi.name.Equals("WIFI_BROADBAND_FLAG", StringComparison.OrdinalIgnoreCase) && csi.value.Equals("N", StringComparison.OrdinalIgnoreCase)))
                            {
                                IsBBFlagExist = true;
                            }

                            if (service.clientServiceRole != null)
                            {
                                foreach (ClientServiceRole role in service.clientServiceRole)
                                {
                                    if (role.clientServiceRoleStatus.value.Equals("INACTIVE"))
                                    {
                                        IsWifiRoleInactive = true;
                                        break;
                                    }
                                }
                            }
                    }
                }

                #endregion

                #region EESpring Journeys
                else if (request.Order.OrderItem.ToList().Exists(s => s.Specification.ToList().Exists(i => i.Identifier.Value1.Equals("S0316296", StringComparison.OrdinalIgnoreCase)))
                        && request.Order.OrderIdentifier.Value.StartsWith("EE", StringComparison.OrdinalIgnoreCase))
                {
                    if (response.Header.Action == OrderActionEnum.cease)
                    {
                        //isEESpringService = true;
                        foreach (MSEO.OrderItem orderItem in request.Order.OrderItem)
                        {
                            GetBatchProfileV1Res ProfileResponse = null;

                            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)))
                                bacID = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("EEMSISDN", StringComparison.OrdinalIgnoreCase)))
                                msisdnValue = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("EEMSISDN", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

                            if (!String.IsNullOrEmpty(bacID))
                            {

                                ProfileResponse = DnpWrapper.GetServiceUserProfilesV1ForDante(bacID, BACID_IDENTIFER_NAMEPACE);

                                if (ProfileResponse != null && ProfileResponse.clientProfileV1 != null)
                                {
                                    // BTRCE-111909 shared plan promotion cease
                                    if (!string.IsNullOrEmpty(orderItem.Action.Reason) && orderItem.Action.Reason.Equals("Last_Sim_cease", StringComparison.OrdinalIgnoreCase))
                                    {
                                        foreach (ClientProfileV1 clientProfile in ProfileResponse.clientProfileV1)
                                        {
                                            if (clientProfile.clientServiceInstanceV1 != null)
                                            {
                                                if (clientProfile.clientServiceInstanceV1.ToList().Exists(ip => ip.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    isEESpringService = true;
                                                    break;
                                                }
                                            }
                                        }

                                        // get the invite roles of the profile in case of shred plan promotion cease                                   
                                        if (!isEESpringService)
                                        {
                                            throw new DnpException("Ignored as there is no SPRING service for the given BillaccountNumber");
                                        }
                                    }
                                    else
                                    {
                                        foreach (ClientProfileV1 clientProfile in ProfileResponse.clientProfileV1)
                                        {
                                            if (clientProfile.clientServiceInstanceV1 != null && clientProfile.clientServiceInstanceV1.ToList().Exists(ip => ip.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase) &&
                                                    ip.serviceIdentity != null && ip.serviceIdentity.ToList().Exists(rl => rl.domain.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.OrdinalIgnoreCase) && rl.value.Equals(bacID, StringComparison.OrdinalIgnoreCase)) &&
                                                    !ip.clientServiceRole.ToList().Exists(csr => csr.id.Equals("SERVICE_USER", StringComparison.OrdinalIgnoreCase) || csr.id.Equals("SERVICE_MANAGER", StringComparison.OrdinalIgnoreCase))))
                                            {
                                                ClientServiceInstanceV1 SpringServiceInstance = new ClientServiceInstanceV1();
                                                SpringServiceInstance = clientProfile.clientServiceInstanceV1.ToList().Where(ip => ip.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                                                isEESpringService = true;

                                                if (clientProfile.client.clientIdentity.ToList().Exists(ic => ic.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    var ci = clientProfile.client.clientIdentity.ToList().Where(ic => ic.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                                    if (ci.clientIdentityValidation != null)
                                                        isAHTDone = ci.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));

                                                }

                                                if (clientProfile.client.clientIdentity.ToList().Exists(ic => ic.managedIdentifierDomain.value.Equals(EE_MSISDN_NAMESPACE, StringComparison.OrdinalIgnoreCase) && ic.value.Equals(msisdnValue, StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    string msisdn = string.Empty;
                                                    if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("EEMSISDN", StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        msisdn = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("EEMSISDN", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                                    }
                                                    else
                                                        throw new DnpException("Ignored the request as MSISDN attribute is missing in the request");
                                                    if (SpringServiceInstance.clientServiceRole.ToList().Where(ic => ic.id.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase)).ToList().Count == 1)
                                                    {
                                                        ServiceInstanceToBeDeleted = true;
                                                    }
                                                    else
                                                    {
                                                        if (clientProfile.clientServiceInstanceV1 != null)
                                                        {
                                                            if (clientProfile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(EE_MSISDN_NAMESPACE, StringComparison.OrdinalIgnoreCase)))
                                                                old_msisdn = clientProfile.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(EE_MSISDN_NAMESPACE, StringComparison.OrdinalIgnoreCase) && !ci.value.Equals(msisdn, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                                        }
                                                    }
                                                    foreach (ClientServiceRole role in clientProfile.clientServiceInstanceV1.Where(si => si.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.Ordinal)).FirstOrDefault().clientServiceRole)
                                                    {
                                                        if (role.clientIdentity != null && role.id.ToLower() == "default" && role.clientIdentity.ToList().Exists(ci => ci.value.Equals(msisdn, StringComparison.OrdinalIgnoreCase)))
                                                        {
                                                            role_key_GSM = role.name;
                                                            if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(OTADEVICEID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                                            {
                                                                deviceID = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(OTADEVICEID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                            }
                                                            break;
                                                        }
                                                    }

                                                }
                                                else
                                                    throw new DnpException("Ignored the request as MSISDN value in the request is not matched with dnp existing value");
                                            }
                                        }
                                        if (!isEESpringService)
                                        {
                                            throw new DnpException("Ignored as there is no SPRING service for the given BillaccountNumber");
                                        }

                                    }
                                }
                                else
                                {
                                    throw new DnpException("Ignored the request as there is no profile with the BACID");
                                }
                            }
                        }
                    }
                }

                #endregion

                #region Spring Journeys
                else if (request.Order.OrderItem.ToList().Exists(s => s.Specification.ToList().Exists(i => i.Identifier.Value1.Equals("S0316296", StringComparison.OrdinalIgnoreCase)))
                        && !(request.Order.OrderIdentifier.Value.StartsWith("EE", StringComparison.OrdinalIgnoreCase)))
                {
                    if (response.Header.Action == OrderActionEnum.cease)
                    {
                        foreach (MSEO.OrderItem orderItem in request.Order.OrderItem)
                        {
                            GetBatchProfileV1Res ProfileResponse = null;

                            //-----Murali----------
                            ClientServiceInstanceV1[] gsiResponse = null;
                            string gsi_rvsid = string.Empty;
                            //------Murali------

                            List<string> inviteroleslist = new List<string>();
                            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)))
                                bacID = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("MSISDN", StringComparison.OrdinalIgnoreCase)))
                                msisdnValue = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("MSISDN", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

                            if (!String.IsNullOrEmpty(bacID))
                            {
                                ProfileResponse = DnpWrapper.GetServiceUserProfilesV1ForDante(bacID, BACID_IDENTIFER_NAMEPACE);
                                if (ProfileResponse != null && ProfileResponse.clientProfileV1 != null)
                                {
                                    // BTRCE-111909 shared plan promotion cease
                                    if (!string.IsNullOrEmpty(orderItem.Action.Reason) && orderItem.Action.Reason.Equals("Last_Sim_cease", StringComparison.OrdinalIgnoreCase))
                                    {
                                        foreach (ClientProfileV1 clientProfile in ProfileResponse.clientProfileV1)
                                        {
                                            if (clientProfile.clientServiceInstanceV1 != null)
                                            {
                                                if (clientProfile.clientServiceInstanceV1.ToList().Exists(ip => ip.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    isSpringServiceExist = true;
                                                    break;
                                                }
                                            }

                                        }

                                        // get the invite roles of the profile in case of shred plan promotion cease                                   
                                        if (isSpringServiceExist)
                                        {
                                            inviteroleslist = GetInviterolesList("GetinviterolesSentByProfile", bacID, "VAS_BILLINGACCOUNT_ID", "last_sim");
                                            if (inviteroleslist.Count == 0)
                                            {  // ignore the order in case of no invite roles sent by the profile.
                                                throw new DnpException("Ignored the request as there is no invite roles  sent by the Profile");
                                            }
                                            Bac_invite_roles = GetinviteRoleandBAClist(inviteroleslist, Bac_invite_roles);
                                        }
                                        else
                                        {
                                            // igonre the order that there is not spring service.
                                            throw new DnpException("Ignored as there is no SPRING service for the given BillaccountNumber");
                                        }
                                    }
                                    else
                                    {
                                        foreach (ClientProfileV1 clientProfile in ProfileResponse.clientProfileV1)
                                        {
                                            if (clientProfile.clientServiceInstanceV1 != null && clientProfile.clientServiceInstanceV1.ToList().Exists(ip => ip.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase) &&
                                                    ip.serviceIdentity != null && ip.serviceIdentity.ToList().Exists(rl => rl.domain.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.OrdinalIgnoreCase) && rl.value.Equals(bacID, StringComparison.OrdinalIgnoreCase)) &&
                                                    !ip.clientServiceRole.ToList().Exists(csr => csr.id.Equals("SERVICE_USER", StringComparison.OrdinalIgnoreCase) || csr.id.Equals("SERVICE_MANAGER", StringComparison.OrdinalIgnoreCase))))
                                            {
                                                ClientServiceInstanceV1 SpringServiceInstance = new ClientServiceInstanceV1();
                                                SpringServiceInstance = clientProfile.clientServiceInstanceV1.ToList().Where(ip => ip.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                                                isSpringServiceExist = true;

                                                if (clientProfile.client.clientIdentity.ToList().Exists(ic => ic.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    var ci = clientProfile.client.clientIdentity.ToList().Where(ic => ic.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                                    if (ci.clientIdentityValidation != null)
                                                        isAHTDone = ci.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));

                                                }
                                                //ccp 78 - MD2
                                                if (clientProfile.client.clientIdentity.ToList().Exists(ic => ic.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && ic.value.Equals(bacID, StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    IsExistingCustomer = true;
                                                }                                                
                                                if (clientProfile.client.clientIdentity.ToList().Exists(ic => ic.managedIdentifierDomain.value.Equals("MOBILE_MSISDN", StringComparison.OrdinalIgnoreCase) && ic.value.Equals(msisdnValue, StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    string rvsid = string.Empty;
                                                    if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("RVSID", StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        rvsid = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower() == "rvsid").FirstOrDefault().Value;
                                                    }
                                                    else
                                                    {
                                                        throw new DnpException("Ignored the request as RVSID value is missing in the request");
                                                    }
                                                    if (SpringServiceInstance.clientServiceRole.ToList().Where(ic => ic.id.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase)).ToList().Count == 1)
                                                    {
                                                        ServiceInstanceToBeDeleted = true;
                                                    }
                                                    else
                                                    {
                                                        if (clientProfile.clientServiceInstanceV1 != null)
                                                        {
                                                            if (clientProfile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(RVSID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && !ci.value.Equals(rvsid, StringComparison.OrdinalIgnoreCase)))
                                                                old_rvsid = clientProfile.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(RVSID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && !ci.value.Equals(rvsid, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                            else
                                                                ServiceInstanceToBeDeleted = true;
                                                        }
                                                    }
                                                    foreach (ClientServiceRole role in clientProfile.clientServiceInstanceV1.Where(si => si.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.Ordinal)).FirstOrDefault().clientServiceRole)
                                                    {
                                                        if (role.clientIdentity != null && role.id.ToLower() == "default" && role.clientIdentity.ToList().Exists(ci => ci.value.Equals(rvsid, StringComparison.OrdinalIgnoreCase)))
                                                        {
                                                            role_key_GSM = role.name;
                                                            if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(OTADEVICEID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                                            {
                                                                deviceID = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(OTADEVICEID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                            }
                                                            break;
                                                        }
                                                    }

                                                }
                                                else
                                                    throw new DnpException("Ignored the request as MSISDN value in the request is not matched with dnp existing value");
                                            }
                                        }
                                        if (!isSpringServiceExist)
                                        {
                                            throw new DnpException("Ignored as there is no SPRING service for the given BillaccountNumber");
                                        }
                                        // BTRCE- 111909 get the invite roles for the msisdn in case of sim cease,                                                                                    
                                        inviteroleslist = GetInviterolesList("GetinviterolesbyUser", msisdnValue, "MOBILE_MSISDN", string.Empty);
                                        if (inviteroleslist.Count() > 0)
                                            Sim_invite_roles = GetinviteRoleandBAClist(inviteroleslist, Sim_invite_roles);
                                    }
                                }
                                else
                                {
                                    throw new DnpException("Ignored the request as there is no profile with the BACID");
                                }

                                //-----Murali-------                                
                                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("RVSID", StringComparison.OrdinalIgnoreCase)))
                                    gsi_rvsid = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower() == "rvsid").FirstOrDefault().Value;
                                else
                                {
                                    throw new DnpException("Ignored the request as RVSID value is missing in the request");
                                }

                                gsiResponse = DnpWrapper.getServiceInstanceV1(bacID, BACID_IDENTIFER_NAMEPACE, string.Empty);
                                GetDelegaterolelist(gsiResponse, ref delegateSpring, gsi_rvsid, ref delegateSport, ref delegateWifi);
                            }
                        }
                    }
                    else if (response.Header.Action == OrderActionEnum.provide)
                    {
                        foreach (MSEO.OrderItem orderItem in request.Order.OrderItem)
                        {
                            GetBatchProfileV1Res bacProfileResponse = null;
                            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)))
                                bacID = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

                            if (!String.IsNullOrEmpty(bacID))
                            {
                                bacProfileResponse = DnpWrapper.GetServiceUserProfilesV1(bacID, BACID_IDENTIFER_NAMEPACE);

                                if (bacProfileResponse != null && bacProfileResponse.clientProfileV1 != null)
                                {
                                    foreach (ClientProfileV1 clientProfile in bacProfileResponse.clientProfileV1)
                                    {
                                        if (clientProfile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(bacID, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            IsExistingCustomer = true;
                                            BT.SaaS.IspssAdapter.Dnp.ClientIdentity bacClientIdentity = clientProfile.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(bacID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                            if (bacClientIdentity.clientIdentityValidation != null)
                                                isAHTDone = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                                        }
                                        if (clientProfile.clientServiceInstanceV1 != null && clientProfile.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            ServiceInstanceExists = clientProfile.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase));

                                            if (clientProfile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(RVSID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                                old_rvsid = clientProfile.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(RVSID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                        }

                                    }
                                }
                                else
                                {
                                    GetClientProfileV1Res gcp_response = DnpWrapper.GetClientProfileV1(bacID, BACID_IDENTIFER_NAMEPACE);
                                    if (gcp_response != null && gcp_response.clientProfileV1 != null && gcp_response.clientProfileV1.client != null &&
                                        gcp_response.clientProfileV1.client.clientIdentity != null)
                                    {
                                        if (gcp_response.clientProfileV1.client.clientIdentity.ToList().Exists(id => id.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            IsExistingCustomer = true;
                                            var ci = gcp_response.clientProfileV1.client.clientIdentity.ToList().Where(id => id.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                            if (ci.clientIdentityValidation != null)
                                                isAHTDone = ci.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));

                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (response.Header.Action == OrderActionEnum.modifyService)
                    {
                        foreach (MSEO.OrderItem orderItem in request.Order.OrderItem)
                        {
                            if (!orderItem.Action.Reason.Equals("portin", StringComparison.OrdinalIgnoreCase))
                            {
                                GetBatchProfileV1Res ProfileResponse = null;
                                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)))
                                    bacID = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

                                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("RVSID", StringComparison.OrdinalIgnoreCase)))
                                    rvsidValue = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("RVSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

                                if (!String.IsNullOrEmpty(bacID))
                                {
                                    ProfileResponse = DnpWrapper.GetServiceUserProfilesV1ForDante(bacID, BACID_IDENTIFER_NAMEPACE);
                                    if (ProfileResponse != null && ProfileResponse.clientProfileV1 != null)
                                    {
                                        foreach (ClientProfileV1 clientProfile in ProfileResponse.clientProfileV1)
                                        {
                                            if (clientProfile.clientServiceInstanceV1.ToList().Exists(ip => ip.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                            {
                                                isSpringServiceExist = true;
                                                if (clientProfile.client.clientIdentity.ToList().Exists(ic => ic.managedIdentifierDomain.value.Equals("RVSID", StringComparison.OrdinalIgnoreCase) && ic.value.Equals(rvsidValue, StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    BT.SaaS.IspssAdapter.Dnp.ClientIdentity rvsidClientIdentity = clientProfile.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals("RVSID", StringComparison.OrdinalIgnoreCase) && i.value.Equals(rvsidValue, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                                    if (rvsidClientIdentity.clientIdentityValidation != null)
                                                    {
                                                        if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("imsi", StringComparison.OrdinalIgnoreCase)))
                                                        {
                                                            imsiValue = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("imsi", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                                            oldImsiValue = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("imsi", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().PreviousValue;
                                                        }
                                                        if (rvsidClientIdentity.clientIdentityValidation.ToList().Exists(civ => civ.name.Equals("num_sim_dispatched", StringComparison.OrdinalIgnoreCase)))
                                                            numSimDispatched = rvsidClientIdentity.clientIdentityValidation.ToList().Where(civ => civ.name.Equals("num_sim_dispatched", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                    }
                                                    else
                                                    {
                                                        throw new DnpException("Ignored as RVSID in the request is not having client identity validation in DnP");
                                                    }
                                                }
                                                else
                                                {
                                                    throw new DnpException("Ignored as RVSID in the request is not mapped to bacid in DnP");
                                                }
                                            }
                                        }
                                        if (!isSpringServiceExist)
                                        {
                                            throw new DnpException("Ignored as there is no SPRING service instance mapped to given bacID in DnP");
                                        }

                                    }
                                    else
                                    {
                                        throw new DnpException("Ignored as there is no profile for given bacID in DnP");
                                    }
                                }
                                else if (InviteRoleJoureny(request))
                                {  // BTRCE-111936 invite roles 
                                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("SPOwnerBAC", StringComparison.OrdinalIgnoreCase)))
                                        spownerBAcID = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("SPOwnerBAC", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

                                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("InviteeBAC", StringComparison.OrdinalIgnoreCase)))
                                        InviteeBAC = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("InviteeBAC", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("InviteID", StringComparison.OrdinalIgnoreCase)))
                                        InviteID = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("InviteID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

                                    if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("InviteeMSISDN", StringComparison.OrdinalIgnoreCase)))
                                        msisdnValue = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("InviteeMSISDN", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                    if (string.IsNullOrEmpty(spownerBAcID))
                                    {
                                        // in accept and decline invite SPowner BAC will not come.
                                        spownerBAcID = GetSPownerBAC("GetinviterolesbyUser", msisdnValue, InviteID);
                                    }

                                    if (!string.IsNullOrEmpty(spownerBAcID) && !string.IsNullOrEmpty(InviteeBAC))
                                    {
                                        if (GetSpringServiceVAlidation(spownerBAcID, BACID_IDENTIFER_NAMEPACE, SPRING_GSM_SERVICECODE_NAMEPACE) &&
                                            GetSpringServiceVAlidation(InviteeBAC, BACID_IDENTIFER_NAMEPACE, SPRING_GSM_SERVICECODE_NAMEPACE))
                                        {
                                            if (!string.IsNullOrEmpty(orderItem.Action.Reason) && orderItem.Action.Reason.ToString().ToLower() == "createinvite")
                                            {
                                                if (InviteroleExists("GetinviterolesbyUser", msisdnValue, spownerBAcID))
                                                {
                                                    throw new DnpException("Ignored as the invite role exists for the given Inviter BAC and Invitee BAC");
                                                }
                                            }
                                            else if (!string.IsNullOrEmpty(orderItem.Action.Reason))
                                            {
                                                if (string.IsNullOrEmpty(InviteID))
                                                {
                                                    throw new DnpException("Ignored as the invite id is not send in Request");
                                                }
                                                string statustobtcom = GetinviteroleValidation(orderItem.Action.Reason, InviteID, spownerBAcID);
                                                if (!string.IsNullOrEmpty(statustobtcom) && statustobtcom.ToString().ToUpper() == "ACCEPTED")
                                                {
                                                    // will send accept notification after submiting to PE.
                                                }
                                                else if (!string.IsNullOrEmpty(statustobtcom) && statustobtcom.ToString().ToUpper() == "REJECTED")
                                                {
                                                    // send reject notification
                                                    throw new DnpException("Rejected as the validation of invite status for the given InviteID in DnP");
                                                }
                                                else if (!string.IsNullOrEmpty(statustobtcom) && statustobtcom.ToString().ToUpper() == "IGNORED")
                                                {
                                                    // send ignore notification
                                                    throw new DnpException("Ignored as the validation of invite status for the given InviteID in DnP");
                                                }
                                                else if (!string.IsNullOrEmpty(statustobtcom) && statustobtcom.ToString().ToUpper() == "NOINVITEROLES")
                                                    throw new DnpException("Rejected as there no invite roles for the given inviteID");
                                            }

                                        }
                                        else
                                        {
                                            // throw the spring service not exists error reject notification
                                            throw new DnpException("Rejected as the status is ignored for given InviteID in DnP");
                                        }

                                    }
                                    else if (!string.IsNullOrEmpty(orderItem.Action.Reason) &&
                                        (orderItem.Action.Reason.ToString().ToLower() == "authorise" || orderItem.Action.Reason.ToString().ToLower() == "cancelauthorisation"))
                                    {
                                        if (!string.IsNullOrEmpty(spownerBAcID))
                                        {
                                            ProfileResponse = DnpWrapper.GetServiceUserProfilesV1ForDante(spownerBAcID, BACID_IDENTIFER_NAMEPACE);
                                            if (ProfileResponse != null && ProfileResponse.clientProfileV1 != null)
                                            {
                                                foreach (ClientProfileV1 clientProfile in ProfileResponse.clientProfileV1)
                                                {
                                                    if (clientProfile.clientServiceInstanceV1 != null && clientProfile.clientServiceInstanceV1.ToList().Exists(ip => ip.clientServiceInstanceIdentifier.value.Equals(SPRING_GSM_SERVICECODE_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        if (clientProfile.client.clientIdentity.ToList().Exists(ic => ic.managedIdentifierDomain.value.Equals("MOBILE_MSISDN", StringComparison.OrdinalIgnoreCase) && ic.value.Equals(msisdnValue, StringComparison.OrdinalIgnoreCase)))
                                                        {
                                                            // accept notification will be send.
                                                        }
                                                        else
                                                            throw new DnpException("Ignored the request as MSISDN value in the request is not matched with dnp existing value");
                                                    }
                                                }
                                            }
                                        }
                                        else
                                            throw new DnpException("Ignored the request as SPOwner BAcID value in the request is not present");
                                    }
                                    else
                                    {
                                        // send ignore notification
                                        throw new DnpException("Ignored as the input data is not correct for invite role journey");
                                    }
                                }

                            }
                            else
                            {
                                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("msisdn", StringComparison.OrdinalIgnoreCase)))
                                {
                                    msisdnValue = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("msisdn", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                    oldMsisdnValue = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("msisdn", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().PreviousValue;
                                }
                            }
                        }

                    }
                }

                #endregion

                #region BTTV
                else if (request.Order.OrderItem.ToList().Exists(oi => oi.Specification.ToList().Exists(s => s.Identifier.Value1.Equals((ConfigurationManager.AppSettings["BTTVProdCode"]).ToString(), StringComparison.OrdinalIgnoreCase))))
                {
                    GetClientProfileV1Res bacProfileResponse = null;
                    ClientServiceInstanceV1[] gsiResponse = null;
                    ClientServiceInstanceV1[] gsiResponsewithVSID = null;

                    string vsID = string.Empty;

                    if (request.Order.OrderItem.ToList().Exists(oi => oi.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase))))
                    {
                        bacID = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                    }
                    if (request.Order.OrderItem.ToList().Exists(oi => oi.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("VSID", StringComparison.OrdinalIgnoreCase))))
                    {
                        vsID = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("VSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                    }

                    gsiResponse = DnpWrapper.getServiceInstanceV1(bacID, BACID_IDENTIFER_NAMEPACE, ConfigurationManager.AppSettings["BTTVServiceCode"].ToString());

                    gsiResponsewithVSID = DnpWrapper.getServiceInstanceV1(vsID, "VSID", ConfigurationManager.AppSettings["BTTVServiceCode"].ToString());

                    if (response.Header.Action == OrderActionEnum.provide)
                    {
                        if (!string.IsNullOrEmpty(bacID))
                        {
                            //bacProfileResponse = DnpWrapper.GetClientProfileV1(bacID, BACID_IDENTIFER_NAMEPACE);

                            string url = ConfigurationManager.AppSettings["VoiceGetSIPurl"].Replace("{identifier_Value}", bacID).ToString();
                            url = url.Replace("SIP_DEVICE", BACID_IDENTIFER_NAMEPACE);
                            BT.DNP.Rest.getClientProfileV1Res getBACResponse = new BT.DNP.Rest.getClientProfileV1Res();

                            VoiceInfinityRequestProcessor reqProcessor = new VoiceInfinityRequestProcessor();
                            getBACResponse = reqProcessor.GetSIPDetailsRestCall(url);

                            if (getBACResponse != null && getBACResponse.clientProfileV1 != null && getBACResponse.clientProfileV1[0].client != null && getBACResponse.clientProfileV1[0].client.clientIdentity != null)
                            {
                                IsExistingCustomer = true;
                                if (getBACResponse.clientProfileV1[0].client.clientIdentity.ToList().Exists(i => i.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(bacID, StringComparison.OrdinalIgnoreCase))
                                    && getBACResponse.clientProfileV1[0].client.clientIdentity[0].characteristic != null && getBACResponse.clientProfileV1[0].client.clientIdentity[0].characteristic.Count() > 0
                                    && getBACResponse.clientProfileV1[0].client.clientIdentity[0].characteristic.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower())))
                                {
                                    isAHTDone = true;
                                }
                                //if (bacClientIdentity..clientIdentityValidation != null)
                                //    isAHTDone = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                            }
                            if (gsiResponse != null && gsiResponse.Length > 0)
                            {
                                ServiceInstanceExists = true;
                                if (gsiResponse[0].clientServiceInstanceStatus.value.Equals("Active", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!request.Order.OrderItem[0].Action.Code.Equals("modify", StringComparison.OrdinalIgnoreCase))
                                    {
                                        throw new DnpException("BT TV Service for bill account is in Active state");
                                    }
                                }

                                if (ServiceInstanceExists)
                                {
                                    if (gsiResponse[0].clientServiceRole != null && gsiResponse[0].clientServiceRole.ToList().Exists(csr => csr.id.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        AdminRoleExists = true;
                                    }
                                    if (gsiResponse[0].serviceIdentity != null && gsiResponse[0].serviceIdentity.ToList().Exists(sId => sId.domain.Equals("VSID", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        for (int i = 0; i < gsiResponse[0].serviceIdentity.Length; i++)
                                        {
                                            if (gsiResponse[0].serviceIdentity[i].domain.Equals("VSID", StringComparison.OrdinalIgnoreCase))
                                            {
                                                VSIDoldValue = gsiResponse[0].serviceIdentity[i].value;
                                            }
                                        }
                                    }
                                    if (gsiResponse[0].clientServiceInstanceCharacteristic != null && gsiResponse[0].clientServiceInstanceCharacteristic.ToList().Exists(csIC => csIC.name.Equals("TOTG", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        TOTGExists = true;
                                    }
                                }
                                if (request.Order.OrderItem[0].Action.Code.Equals("modify", StringComparison.OrdinalIgnoreCase))
                                {
                                    isBTTVModify = true;
                                    if (!gsiResponse[0].clientServiceInstanceStatus.value.Equals("Active", StringComparison.OrdinalIgnoreCase))
                                    {
                                        throw new DnpException("BT TV Service is Not in Active state in the DnP ");
                                    }
                                    else if (TOTGExists)
                                    {
                                        throw new DnpException("BT TV Service for bill account is in Active state and has TOTG value");
                                    }
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(vsID))
                        {
                            //gsiResponsewithVSID = DnpWrapper.getServiceInstanceV1(vsID, "VSID", ConfigurationManager.AppSettings["BTTVServiceCode"].ToString());
                            if (gsiResponsewithVSID != null && gsiResponsewithVSID.Length > 0)
                            {
                                string orderKey = request.Order.OrderIdentifier.Value;

                                if (gsiResponsewithVSID[0].clientServiceInstanceStatus.value.Equals("Active", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (request.Order.OrderItem[0].Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase))
                                    {
                                        throw new DnpException("BT TV Service for VSID is in Active state");
                                    }
                                }
                                //calling a function to delink the vsid from profile if bac is not linked with vsid.
                                if (gsiResponsewithVSID[0].clientServiceInstanceStatus.value.Equals("CEASED", StringComparison.OrdinalIgnoreCase)
                                    && !gsiResponsewithVSID[0].serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && si.value.Equals(bacID, StringComparison.OrdinalIgnoreCase)))
                                {
                                    string oldBAC = string.Empty;

                                    if (gsiResponsewithVSID[0].serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(si.value)))
                                        oldBAC = gsiResponsewithVSID[0].serviceIdentity.ToList().Where(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                    UnlinkVSIDfromBAC(vsID, oldBAC, orderKey);
                                }
                            }
                        }
                    }
                    else if (response.Header.Action == OrderActionEnum.cease)
                    {
                        if (!string.IsNullOrEmpty(bacID))
                        {
                            if (gsiResponse == null)
                            {
                                throw new DnpException("No BT TV Service exist in the DnP ");
                            }
                            else if (!request.Order.OrderItem.ToList().Exists(oi => oi.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("TOTG", StringComparison.OrdinalIgnoreCase))))
                            {
                                if (gsiResponse[0].clientServiceRole != null && gsiResponse[0].clientServiceRole.ToList().Exists(csr => csr.id.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
                                {
                                    AdminRoleExists = true;
                                }
                            }
                            if (gsiResponse[0].clientServiceInstanceCharacteristic != null && gsiResponse[0].clientServiceInstanceCharacteristic.ToList().Exists(csIC => csIC.name.Equals("TOTG", StringComparison.OrdinalIgnoreCase)))
                            {
                                TOTGExists = true;
                            }
                        }
                    }
                }
                #endregion

                response.ProductOrderItems = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ProductOrderItem>(request.Order.OrderItem.Length);

                foreach (MSEO.OrderItem orderItem in request.Order.OrderItem)
                {
                    string[] productsWithServiceName = ConfigurationManager.AppSettings["ProductsWithServiceName"].Split(new char[] { ';' });

                    SaaSNS.ProductOrderItem productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
                    productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
                    productOrderItem.Header.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.notDone;
                    productOrderItem.Header.Quantity = "1";
                    productOrderItem.XmlIdKey = "PO-" + productorderItemCount.ToString();

                    productOrderItem.Header.OrderItemKey = orderItem.Identifier.Id;
                    if (productsWithServiceName.Contains(orderItem.Specification[0].Identifier.Value1))
                    {
                        productOrderItem.Header.ProductCode = orderItem.Specification[0].Identifier.Value1 + "-" + orderItem.Instance[0].InstanceCharacteristic.Where(ic => ic.Name.Equals("ServiceName", StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault().Value;
                    }
                    else if (ConfigurationManager.AppSettings["EmailScodes"].Split(',').Contains(orderItem.Specification[0].Identifier.Value1) || ConfigurationManager.AppSettings["PremiumEmailScodes"].Split(',').Contains(orderItem.Specification[0].Identifier.Value1))                    
                    {
                        productOrderItem.Header.ProductCode = ConfigurationManager.AppSettings["DanteEMailProdCode"].ToString();
                    }
                    //nayan email changes
                    else if (ConfigurationManager.AppSettings["NayanEmailScodes"].Split(',').Contains(orderItem.Specification[0].Identifier.Value1))
                    {
                        productOrderItem.Header.ProductCode = ConfigurationManager.AppSettings["DanteEMailProdCode"].ToString();
                    }
                    //ccp 78 - MD2
                    else if (ConfigurationManager.AppSettings["CloudScode"].Split(',').Contains(orderItem.Specification[0].Identifier.Value1))
                    {
                        productOrderItem.Header.ProductCode = ConfigurationManager.AppSettings["VASContentAnyWhereProductFamily"].ToString();
                    }
                    else
                    {
                        if (orderItem.Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["WifiProdCode"].ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            productOrderItem.Header.ProductCode = ConfigurationManager.AppSettings["WifiProdCode"].ToString();
                        }
                        //ccp 78
                        if (orderItem.Specification[0].Identifier.Value1.Equals("SafeSearch", StringComparison.OrdinalIgnoreCase))
                        {
                            productOrderItem.Header.ProductCode = "ContentFiltering";
                        }
                        else
                        {
                            productOrderItem.Header.ProductCode = orderItem.Specification[0].Identifier.Value1;
                        }
                    }

                    System.Collections.Generic.List<string> inputProductDefinitionList = new System.Collections.Generic.List<string>();
                    inputProductDefinitionList.Add(productOrderItem.Header.ProductCode);

                    System.Collections.Generic.List<ProductDefinition> productDefinition = MdmWrapper.getSaaSProductDefs(inputProductDefinitionList, Settings1.Default.ConsumerServiceProviderId);

                    if (productDefinition.Count == 0)
                    {
                        orderItem.Status = Settings1.Default.IgnoredStatus;
                        Logger.Debug("Product Code not found in Order : " + request.Order.OrderIdentifier.Value + " for order item " + orderItem.Identifier.Id);
                    }
                    else
                    {
                        productOrderItem.RoleInstances = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.RoleInstance>(1);
                        productOrderItem.ServiceInstances = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ServiceInstance>();
                        SaaSNS.RoleInstance roleInstance = new BT.SaaS.Core.Shared.Entities.RoleInstance();

                        roleInstance = new BT.SaaS.Core.Shared.Entities.RoleInstance();
                        roleInstance.Action = BT.SaaS.Core.Shared.Entities.RoleInstanceActionEnum.add;

                        // BTRCE - 111936 for invite role role type is Invite to call DNP and KCIM
                        if (InviteRoleJoureny(request))
                            roleInstance.RoleType = "INVITE";
                        else
                            roleInstance.RoleType = "ADMIN";

                        roleInstance.XmlIdKey = "RI-" + productorderItemCount.ToString();
                        SaaSNS.ServiceInstance serviceInstance = new BT.SaaS.Core.Shared.Entities.ServiceInstance();
                        serviceInstance.ServiceRoles = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ServiceRole>();
                        SaaSNS.ServiceRole serviceRole = new BT.SaaS.Core.Shared.Entities.ServiceRole();
                        serviceRole.roleAttributes = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.Attribute>();

                        foreach (MSEO.Instance instance in orderItem.Instance)
                        {
                            foreach (MSEO.InstanceCharacteristic instanceCharacteristic in instance.InstanceCharacteristic)
                            {
                                SaaSNS.Attribute attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                attribute.Name = instanceCharacteristic.Name;
                                attribute.Value = instanceCharacteristic.Value;
                                roleInstance.Attributes.Add(attribute);
                            }
                        }

                        SaaSNS.Attribute isExistingAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                        isExistingAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        isExistingAttribute.Name = "ISEXISTINGACCOUNT";

                        if (ConfigurationManager.AppSettings["NominumServices"].ToString().Split(',').Contains(orderItem.Specification[0].Identifier.Value1, StringComparer.OrdinalIgnoreCase))
                        {
                            NominumProductProcessor nominumProductProcessor = new NominumProductProcessor();
                            string action = response.Header.Action.ToString();
                            nominumProductProcessor.MapNominumProduct(orderItem, response.Header.Action, ref roleInstance, orderItem.Specification[0].Identifier.Value1, request.StandardHeader.ServiceAddressing.From.ToString(), ref action);
                            response.Header.Action = (OrderActionEnum)Enum.Parse(typeof(OrderActionEnum), action);
                        }

                        if (ConfigurationManager.AppSettings["McAfeeServices"].ToString().Split(',').Contains(orderItem.Specification[0].Identifier.Value1, StringComparer.OrdinalIgnoreCase))
                        {
                            string action = response.Header.Action.ToString();
                            string vasSubType = string.Empty;
                            if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("vassubtype", StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrEmpty(orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("vassubtype", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
                            {
                                vasSubType = roleInstance.Attributes.ToList().Where(roleInstAttrb => roleInstAttrb.Name.Equals("vassubtype", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                            }
                            if (!string.IsNullOrEmpty(vasSubType) && (vasSubType.Equals("Common", StringComparison.OrdinalIgnoreCase) || (vasSubType.Equals("windows", StringComparison.OrdinalIgnoreCase))))
                            {
                                McAfeeProcessor mcAfeeProcessor = new McAfeeProcessor();
                                mcAfeeProcessor.MapSecurityProduct(orderItem, response.Header.Action, ref roleInstance, vasSubType);
                            }
                        }

                        #region DANTE Journeys
                        //nayan email changes
                        if (ConfigurationManager.AppSettings["NayanEmailScodes"].Split(',').Contains(orderItem.Specification[0].Identifier.Value1) && productOrderItem.Header.ProductCode.Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"], StringComparison.OrdinalIgnoreCase))
                        {
                            string emailSupplier = "MX";
                            if (response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.modifyService && request.Order.OrderItem[0].Action.Reason.Equals("vasregrade", StringComparison.OrdinalIgnoreCase))
                            {
                                SaaSNS.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                reasonAttirbute.Name = "REASON";
                                reasonAttirbute.Value = orderItem.Action.Reason.ToString();
                                roleInstance.Attributes.Add(reasonAttirbute);

                                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("emailtype")) && !string.IsNullOrEmpty(orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailtype")).FirstOrDefault().Value))
                                {
                                    string typeOfEmail = string.Empty;
                                    typeOfEmail = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailtype")).FirstOrDefault().Value;

                                    if (typeOfEmail.ToLower().Equals("premium"))
                                    {
                                        SaaSNS.Attribute emailTypeAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        emailTypeAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        emailTypeAttribute.Name = "ISPSERVICECLASS";
                                        emailTypeAttribute.Value = "ISP_SC_PM";
                                        roleInstance.Attributes.Add(emailTypeAttribute);
                                    }
                                    else if (typeOfEmail.ToLower().Equals("basic"))
                                    {
                                        SaaSNS.Attribute emailTypeAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        emailTypeAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        emailTypeAttribute.Name = "ISPSERVICECLASS";
                                        emailTypeAttribute.Value = "ISP_SC_BM";
                                        roleInstance.Attributes.Add(emailTypeAttribute);
                                    }
                                    else
                                    {
                                        SaaSNS.Attribute emailTypeAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        emailTypeAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        emailTypeAttribute.Name = "ISPSERVICECLASS";
                                        emailTypeAttribute.Value = "ISP_SC_INFINITY_2";
                                        roleInstance.Attributes.Add(emailTypeAttribute);
                                    }
                                }

                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "isnayanemail", Value = true.ToString() });
                            }
                            else if (response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.cease)
                            {
                                DanteRequestProcessor.MapNayanEmailCeaseRequest(orderItem, ref roleInstance, ref emailSupplier);
                            }

                            if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("EMAILSUPPLIER", StringComparison.OrdinalIgnoreCase)))
                            {
                                SaaSNS.Attribute emailSupplierAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                emailSupplierAttr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                emailSupplierAttr.Name = "EMAILSUPPLIER";
                                emailSupplierAttr.Value = emailSupplier;
                                roleInstance.Attributes.Add(emailSupplierAttr);
                            }
                            else
                            {
                                roleInstance.Attributes.ToList().Where(roleInstAttrb => roleInstAttrb.Name.Equals("EMAILSUPPLIER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = emailSupplier;
                            }

                            if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("rolestatus", StringComparison.OrdinalIgnoreCase)))
                            {
                                SaaSNS.Attribute rolestatusvalue = new BT.SaaS.Core.Shared.Entities.Attribute();
                                rolestatusvalue.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                rolestatusvalue.Name = "rolestatus";
                                rolestatusvalue.Value = rolestatus;
                                roleInstance.Attributes.Add(rolestatusvalue);
                            }
                        }
                        else if (productOrderItem.Header.ProductCode.Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"], StringComparison.OrdinalIgnoreCase))
                        {
                            string emailSupplier = "MX";
                            string targetBAC = string.Empty;
                            bool isbtoneidexists = false;
                            string AcchldrBtOneId = string.Empty;
                            serviceAddressingFrom = request.StandardHeader.ServiceAddressing.From.ToString();
                            string messageId = request.StandardHeader.ServiceAddressing != null && request.StandardHeader.ServiceAddressing.MessageId != null ? request.StandardHeader.ServiceAddressing.MessageId.ToString() : string.Empty;
                            //provision orders || first email creation for Affliate || proposed mailbox
                            if ((response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.provide || (response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.modifyService && string.IsNullOrEmpty(request.Order.OrderItem[0].Action.Reason)) ||
                             (response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.modifyService && !string.IsNullOrEmpty(request.Order.OrderItem[0].Action.Reason) && request.Order.OrderItem[0].Action.Reason.Equals("First_email", StringComparison.OrdinalIgnoreCase)))
                                && !request.Order.OrderItem[0].Action.Reason.Equals("OCBUndobar", StringComparison.OrdinalIgnoreCase))
                            {
                                DanteRequestProcessor.MapDanteProvisionRequest(orderItem, ref roleInstance, serviceAddressingFrom, ref emailSupplier);
                            }
                            // BBMT cease is processed based on VASproductID returned from Email service instancel)
                            else if (string.IsNullOrEmpty(status) && response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.cease &&
                                serviceAddressingFrom.Equals(ConfigurationManager.AppSettings["BBMTServiceAddress"]))
                            {
                                DanteRequestProcessor.MapBBMTCeaseRequest(orderItem, ref roleInstance, ref emailSupplier);
                            }
                            // ISP leg cease is processed based on ISP_SERVICE_CLASS passed in the request
                            else if (response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.cease &&
                                serviceAddressingFrom.Contains(Settings1.Default.MQstring))
                            {
                                if (!string.IsNullOrEmpty(thomasCeaseReason) && thomasCeaseReason.Equals("OCBEmailCease", StringComparison.OrdinalIgnoreCase))
                                {
                                    SaaSNS.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    reasonAttirbute.Name = "REASON";
                                    reasonAttirbute.Value = thomasCeaseReason;
                                    roleInstance.Attributes.Add(reasonAttirbute);
                                }
                                else
                                    DanteRequestProcessor.MapISPEmailCeaseRequest(orderItem, ref roleInstance, ref emailSupplier);
                            }
                            //Adding undobarred reason to productorderitem                            
                            // ISP leg BB Regrade
                            else if (response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.modifyService && request.Order.OrderItem[0].Action.Reason.Equals("vasregrade", StringComparison.OrdinalIgnoreCase))
                            {
                                SaaSNS.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                reasonAttirbute.Name = "REASON";
                                reasonAttirbute.Value = orderItem.Action.Reason.ToString();
                                roleInstance.Attributes.Add(reasonAttirbute);
                                //targetvasprodictid
                                //ccp 83 MD2
                                string ispServiceClass = string.Empty;
                                ispServiceClass = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("ispserviceclass")).FirstOrDefault().Value;
                                if (ispServiceClass.Equals("ISP_SC_PM", StringComparison.OrdinalIgnoreCase))
                                {
                                    SaaSNS.Attribute emailTypeAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    emailTypeAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    emailTypeAttribute.Name = "targetvasproductid";
                                    emailTypeAttribute.Value = "VPEM000030";
                                    roleInstance.Attributes.Add(emailTypeAttribute);
                                }
                                else if (ispServiceClass.Equals("ISP_SC_BM", StringComparison.OrdinalIgnoreCase))
                                {
                                    SaaSNS.Attribute emailTypeAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    emailTypeAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    emailTypeAttribute.Name = "targetvasproductid";
                                    emailTypeAttribute.Value = "VPEM000060";
                                    roleInstance.Attributes.Add(emailTypeAttribute);
                                }
                                else
                                {
                                    SaaSNS.Attribute emailTypeAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    emailTypeAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    emailTypeAttribute.Name = "targetvasproductid";
                                    emailTypeAttribute.Value = "VPEM000010";
                                    roleInstance.Attributes.Add(emailTypeAttribute);
                                }

                                string bacId = string.Empty;
                                bacId = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value;

                                List<string> activeEmailList = new List<string>();
                                Dictionary<string, string> activeDictionary = new Dictionary<string, string>();
                                List<string> activeList = new List<string>();
                                List<string> ListOfEmails = new List<string>();
                                string emailsList = string.Empty;
                                GetBatchProfileV1Res response1 = null;

                                response1 = DnpWrapper.GetServiceUserProfilesV1ForDante(bacId, "VAS_BILLINGACCOUNT_ID");

                                if (response1 != null && response1.clientProfileV1 != null && response1.clientProfileV1.Count() > 0)
                                {
                                    foreach (ClientProfileV1 profile in response1.clientProfileV1)
                                    {
                                        if (profile.clientServiceInstanceV1 != null && profile.clientServiceInstanceV1.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && csi.clientServiceInstanceStatus.value.ToLower() == "active"))
                                        {
                                            var emailServiceInstance = (from si in profile.clientServiceInstanceV1
                                                                        where si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceStatus.value.ToLower() == "active" && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(bacId, StringComparison.OrdinalIgnoreCase))
                                                                        select si).FirstOrDefault();
                                           

                                            foreach (ClientServiceRole sr in emailServiceInstance.clientServiceRole.Where(sr => sr.id.ToLower() == "default"))
                                            {
                                                if (sr.clientIdentity != null && sr.clientIdentity.Any())
                                                {
                                                    if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "active" || ci.clientIdentityStatus.value.ToLower() == "transferring")))
                                                    {
                                                        activeEmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() == "active" || ci.clientIdentityStatus.value.ToLower() == "transferring")).FirstOrDefault().value);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                //stuck order fix
                                else
                                {
                                    throw new DnpException("No active profile found for this billingaccountID in DnP");

                                }

                                if (activeEmailList != null && activeEmailList.Count() > 0)
                                {
                                    activeDictionary.Add(activeEmailList.First(), string.Join(",", activeEmailList.ToArray()));
                                }

                                foreach (string key in activeDictionary.Keys)
                                {
                                    string abc = key + ":" + activeDictionary[key];
                                    activeList.Add(abc);
                                    emailsList = emailsList + ";" + abc;
                                }

                                if (activeList != null)
                                {
                                    ListOfEmails.Add(string.Join(";", activeList.ToArray()));
                                }

                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ListOfEmails", Value = ListOfEmails[0] });
                                //mxmailbox code
                                Dictionary<string, string> listofSupplierMailboxes = new Dictionary<string, string>();
                                string emailName = string.Empty;
                                emailName = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailname")).FirstOrDefault().Value;
                                listofSupplierMailboxes = DanteRequestProcessor.GetListOfSuplierEmails(bacId, emailName, "EMAILREGRADE", response1, emailsList);

                                if (listofSupplierMailboxes != null && listofSupplierMailboxes.Count() > 0)
                                {
                                    if (listofSupplierMailboxes.ContainsKey("MXMailboxes") && !string.IsNullOrEmpty(listofSupplierMailboxes["MXMailboxes"]))
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "MXMailboxes", Value = listofSupplierMailboxes["MXMailboxes"] });
                                }

                            }
                            else if (!string.IsNullOrEmpty(thomasCeaseReason) && thomasCeaseReason.Equals("OCBUndobar", StringComparison.OrdinalIgnoreCase))
                            {
                                SaaSNS.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                reasonAttirbute.Name = "REASON";
                                reasonAttirbute.Value = thomasCeaseReason.Equals("OCBUndobar", StringComparison.OrdinalIgnoreCase) ? "OCBUndobar" : orderItem.Action.Reason.ToString();
                                roleInstance.Attributes.Add(reasonAttirbute);
                            }
                            else
                            {
                                GetClientProfileV1Res getprofileResponse1 = new GetClientProfileV1Res();

                                if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("emailname")) && !string.IsNullOrEmpty(request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailname")).FirstOrDefault().Value))
                                {
                                    EmailName = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailname")).FirstOrDefault().Value;
                                }
                                if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("billaccountnumber")) && !string.IsNullOrEmpty(request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value))
                                {
                                    BakId = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value;
                                }
                                if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("btoneid")) && !string.IsNullOrEmpty(request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("btoneid")).FirstOrDefault().Value))
                                {
                                    BtOneId = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("btoneid")).FirstOrDefault().Value;
                                }
                                else if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("btid")) && !string.IsNullOrEmpty(request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("btid")).FirstOrDefault().Value))
                                {
                                    BtOneId = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("btid")).FirstOrDefault().Value;
                                }
                                if (response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.reActivate
                                    || response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.vasReactivate
                                    || response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.cease)
                                {
                                    identityDomain = "BTIEMAILID";
                                    identity = EmailName;
                                }
                                else if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower() == "btoneid" && (ic.Value != null)) && !IsOFSDanteProvide)
                                {
                                    IsOFSDanteProvide = false;
                                    BtOneId = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("btoneid")).FirstOrDefault().Value;
                                    identityDomain = "BTCOM";
                                    identity = BtOneId;
                                }
                                else if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower() == "btid" && (ic.Value != null)) && !IsOFSDanteProvide)
                                {
                                    IsOFSDanteProvide = false;
                                    BtOneId = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("btid")).FirstOrDefault().Value;
                                    identityDomain = "BTCOM";
                                    identity = BtOneId;

                                    AddorUpdateProductAttributes("BtOneId", BtOneId, ref roleInstance, BT.SaaS.Core.Shared.Entities.DataActionEnum.change);
                                }

                                if (response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.cease
                                    || response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.vasReactivate
                                    || response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.reActivate)
                                {
                                    if ((!String.IsNullOrEmpty(orderItem.Action.Reason) && (orderItem.Action.Reason.Equals("delink", StringComparison.OrdinalIgnoreCase) || orderItem.Action.Reason.Equals("hard_delete", StringComparison.OrdinalIgnoreCase))))
                                    {
                                        if (!Convert.ToBoolean(ConfigurationManager.AppSettings["IsBTRCE191515enabled"]))
                                            throw new DnpException("Delink switch disbaled");
                                    }
                                    if ((!String.IsNullOrEmpty(orderItem.Action.Reason) && (orderItem.Action.Reason.Equals("reinstate", StringComparison.OrdinalIgnoreCase))))
                                    {
                                        if (!Convert.ToBoolean(ConfigurationManager.AppSettings["IsBTRCE191525enabled"]))
                                            throw new DnpException("Reinstate switch disbaled");
                                    }

                                    if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("TargetBillAccountNumber")) && !string.IsNullOrEmpty(request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("TargetBillAccountNumber")).FirstOrDefault().Value))
                                    {
                                        targetBAC = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("TargetBillAccountNumber")).FirstOrDefault().Value;
                                    }

                                    if (response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.reActivate
                                        && (string.IsNullOrEmpty(request.Order.OrderItem[0].Action.Reason) || (!string.IsNullOrEmpty(request.Order.OrderItem[0].Action.Reason) && request.Order.OrderItem[0].Action.Reason.Equals("Reinstate", StringComparison.OrdinalIgnoreCase))))
                                    {
                                        bool isStaretegicProductModelLaunched = false; string BtOneID = string.Empty;
                                        isStaretegicProductModelLaunched = bool.Parse(ConfigurationManager.AppSettings["isStaretegicProductModelLaunched"]);

                                        //DanteRequestProcessor.ReactivateReinstateMapper(BakId, targetBAC, EmailName, request.Order.OrderItem[0].Action.Reason, serviceAddressingFrom, ref roleInstance, ref emailSupplier);

                                        if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("BtOneId")) && !string.IsNullOrEmpty(request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("BtOneId")).FirstOrDefault().Value))
                                            BtOneID = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("BtOneId")).FirstOrDefault().Value;
                                        else if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("BTID")) && !string.IsNullOrEmpty(request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("BTID")).FirstOrDefault().Value))
                                            BtOneID = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("BTID")).FirstOrDefault().Value;

                                        if (!string.IsNullOrEmpty(BakId))
                                        {
                                            if (!string.IsNullOrEmpty(request.Order.OrderItem[0].Action.Reason) && request.Order.OrderItem[0].Action.Reason.Equals("Reinstate", StringComparison.OrdinalIgnoreCase))
                                            {
                                                DanteRequestProcessor.ReinstateonSameBAC(BakId, EmailName, ref roleInstance, ref emailSupplier);
                                            }
                                            else
                                                DanteRequestProcessor.ReactivateonSameBAC(BakId, EmailName, ref roleInstance, ref emailSupplier);
                                        }
                                        else if (!string.IsNullOrEmpty(targetBAC))
                                            DanteRequestProcessor.ReactivateonTargetBAC(targetBAC, EmailName, ref roleInstance, ref emailSupplier);
                                        else if (serviceAddressingFrom.Equals(ConfigurationManager.AppSettings["BBMTServiceAddress"], StringComparison.OrdinalIgnoreCase)) // as no BAC would be passed from Resolve for Reactivate orders
                                            DanteRequestProcessor.ReactivateonSameBAC(BakId, EmailName, ref roleInstance, ref emailSupplier);
                                        else if (isStaretegicProductModelLaunched)
                                        {
                                            DanteRequestProcessor.ValidateReinstateOrder(BtOneID, EmailName, ref emailSupplier);
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        getprofileResponse1 = DnpWrapper.GetClientProfileV1(identity, identityDomain);
                                        if (getprofileResponse1 != null && getprofileResponse1.clientProfileV1 != null
                                            && getprofileResponse1.clientProfileV1.clientServiceInstanceV1 != null && getprofileResponse1.clientProfileV1.clientServiceInstanceV1.Count() > 0)
                                        {
                                            if (getprofileResponse1.clientProfileV1.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase)))
                                            {
                                                if (getprofileResponse1.clientProfileV1.client != null && getprofileResponse1.clientProfileV1.client.clientIdentity != null && getprofileResponse1.clientProfileV1.client.clientIdentity.Count() > 0)
                                                {
                                                    //to populate profiletype in kcim request.
                                                    if (getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        isbtoneidexists = true;
                                                    }
                                                    if (request.Order.OrderItem[0].Action.Reason != null && request.Order.OrderItem[0].Action.Reason.Equals("hard_delete", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(BakId))
                                                    {
                                                        if (getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(BtOneId, StringComparison.OrdinalIgnoreCase))
                                                            && getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(BakId, StringComparison.OrdinalIgnoreCase)))
                                                        {
                                                            AcchldrBtOneId = BtOneId;
                                                        }
                                                        else
                                                        {
                                                            GetClientProfileV1Res getResponse1 = DnpWrapper.GetClientProfileV1(BakId, "VAS_BILLINGACCOUNT_ID");
                                                            if (getprofileResponse1 != null && getprofileResponse1.clientProfileV1 != null && getprofileResponse1.clientProfileV1.client != null && getprofileResponse1.clientProfileV1.client.clientIdentity != null && getprofileResponse1.clientProfileV1.client.clientIdentity.Count() > 0
                                                                && getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                                                            {
                                                                AcchldrBtOneId = getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value.ToString();
                                                            }
                                                        }
                                                        AddorUpdateProductAttributes("AccountholderBtOneId", AcchldrBtOneId, ref roleInstance, BT.SaaS.Core.Shared.Entities.DataActionEnum.change);

                                                    }
                                                    if (getprofileResponse1.clientProfileV1.client.clientCharacteristic != null && getprofileResponse1.clientProfileV1.client.clientCharacteristic.Count() > 0
                                                        && getprofileResponse1.clientProfileV1.client.clientCharacteristic.ToList().Exists(chr => !string.IsNullOrEmpty(chr.name) && chr.name.Equals("PRIMARY_EMAIL", StringComparison.OrdinalIgnoreCase))
                                                        && !string.IsNullOrEmpty(getprofileResponse1.clientProfileV1.client.clientCharacteristic.ToList().Where(chr => chr.name.Equals("PRIMARY_EMAIL", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value))
                                                    {
                                                        SaaSNS.Attribute conkid = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                        conkid.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                        conkid.Name = "PrimaryDetails";
                                                        conkid.Value = getprofileResponse1.clientProfileV1.client.clientCharacteristic.ToList().Where(chr => chr.name.Equals("PRIMARY_EMAIL", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value.ToString();
                                                        roleInstance.Attributes.Add(conkid);
                                                    }
                                                    if (getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("CONKID", StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        SaaSNS.Attribute conkid = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                        conkid.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                        conkid.Name = "CONKID";
                                                        conkid.Value = getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("CONKID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                        roleInstance.Attributes.Add(conkid);
                                                    }
                                                    if (getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName)))
                                                    {
                                                        BT.SaaS.IspssAdapter.Dnp.ClientIdentity clientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                                                        clientIdentity = getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName)).FirstOrDefault();

                                                        string domain = string.Empty;
                                                        if (clientIdentity.clientIdentityValidation.ToList().Exists(civ => civ.name.Equals("DOMAIN", StringComparison.OrdinalIgnoreCase)))
                                                        {
                                                            domain = clientIdentity.clientIdentityValidation.ToList().Where(civ => civ.name.Equals("DOMAIN", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                                            SaaSNS.Attribute Domainattr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                            Domainattr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                            Domainattr.Name = "DOMAIN";
                                                            Domainattr.Value = domain;
                                                            roleInstance.Attributes.Add(Domainattr);
                                                        }
                                                    }
                                                }

                                                var serviceInst = (from si in getprofileResponse1.clientProfileV1.clientServiceInstanceV1
                                                                   where (si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null && si.clientServiceRole.ToList().Exists(sr => sr.clientIdentity != null && sr.clientIdentity.ToList().Exists(sri => sri.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase))))
                                                                   select si).FirstOrDefault();
                                                serviceInstanceStatus = serviceInst.clientServiceInstanceStatus.value.ToString();
                                                ServiceInstanceExists = true;
                                                if (serviceInst.serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                                    SIBacNumber = serviceInst.serviceIdentity.ToList().Where(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                if (serviceInst.serviceIdentity.ToList().Exists(si => si.domain.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase)))
                                                    primaryEmail = serviceInst.serviceIdentity.ToList().Where(si => si.domain.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                if (serviceInst.clientServiceInstanceCharacteristic != null &&
                                                    serviceInst.clientServiceInstanceCharacteristic.ToList().Exists(sic => sic.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase)))
                                                    targetVasprodId = serviceInst.clientServiceInstanceCharacteristic.ToList().Where(sic => sic.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                                //to populate profiletype in kcim request in softdelete scenario.
                                                if (!String.IsNullOrEmpty(orderItem.Action.Reason) && orderItem.Action.Reason.Equals("soft_delete", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    bool IsLastEmailcheck = false;
                                                    bool isAffilicateacc = false;
                                                    string newmail = string.Empty;
                                                    int emailCount = 0;

                                                    if (!string.IsNullOrEmpty(BakId))
                                                        IsLastEmailcheck = DanteRequestProcessor.isLastEmailmoved(BakId, BACID_IDENTIFER_NAMEPACE, EmailName, false, ref isAffilicateacc, ref newmail, orderItem.Action.Reason, ref emailCount);
                                                    else if (!string.IsNullOrEmpty(SIBacNumber))
                                                        IsLastEmailcheck = DanteRequestProcessor.isLastEmailmoved(SIBacNumber, BACID_IDENTIFER_NAMEPACE, EmailName, false, ref isAffilicateacc, ref newmail, orderItem.Action.Reason, ref emailCount);
                                                    else if (!string.IsNullOrEmpty(primaryEmail))
                                                        IsLastEmailcheck = DanteRequestProcessor.isLastEmailmoved(primaryEmail, "BTIEMAILID", EmailName, false, ref isAffilicateacc, ref newmail, orderItem.Action.Reason, ref emailCount);

                                                    SaaSNS.Attribute btoneid = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                    btoneid.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                    btoneid.Name = "Profiletype";
                                                    btoneid.Value = isbtoneidexists ? isAffilicateacc ? "Affiliate" : "AccountHolder" : "NoOneId";
                                                    roleInstance.Attributes.Add(btoneid);
                                                }


                                                if (!String.IsNullOrEmpty(status) && status.ToUpper() == "INACTIVE")
                                                {
                                                    if (!string.IsNullOrEmpty(serviceInstanceStatus) && !(new string[] { "PENDING-CEASE", "ACTIVE" }.Contains(serviceInstanceStatus, StringComparer.OrdinalIgnoreCase)))
                                                    {
                                                        throw new DnpException("Email Service inst status is not as expected for BBMT modify INACTIVE status");
                                                    }
                                                }
                                                else if ((!string.IsNullOrEmpty(serviceInstanceStatus) && serviceInstanceStatus.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
                                                    || (!String.IsNullOrEmpty(orderItem.Action.Reason) && orderItem.Action.Reason.ToLower() == "compromise"))
                                                {
                                                    if (!String.IsNullOrEmpty(orderItem.Action.Reason) && orderItem.Action.Reason.ToLower() == "compromise")
                                                    {
                                                        string billAccountNumber = string.Empty;
                                                        if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("BillAccountNumber", StringComparison.OrdinalIgnoreCase)))
                                                        {
                                                            billAccountNumber = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("BillAccountNumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                                        }
                                                        List<string> mailBoxList = new List<string>();
                                                        foreach (ClientServiceRole sr in serviceInst.clientServiceRole.Where(sr => sr.id.ToLower() == "default"))
                                                        {
                                                            if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.ToLower() == "btiemailid" && ConfigurationManager.AppSettings["EmailIdentityStatus"].Split(',').Contains(ci.clientIdentityStatus.value.ToLower())))
                                                            {
                                                                mailBoxList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.ToLower() == "btiemailid" && ConfigurationManager.AppSettings["EmailIdentityStatus"].Split(',').Contains(ci.clientIdentityStatus.value.ToLower())).FirstOrDefault().value);
                                                            }
                                                        }

                                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "ListOfEmails", Value = string.Join(";", mailBoxList.ToArray()), action = DataActionEnum.add });
                                                        // checking the BT_PROMPT_FLAG value in the request 
                                                        if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("BT_PROMPT_FLAG", StringComparison.OrdinalIgnoreCase)))
                                                        {
                                                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "BT_PROMPT_FLAG", Value = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("BT_PROMPT_FLAG", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value, action = DataActionEnum.add });
                                                        }
                                                    }

                                                }
                                                //INC1970404
                                                //else if ((!string.IsNullOrEmpty(serviceInstanceStatus) && serviceInstanceStatus.Equals("PENDING-CEASE", StringComparison.OrdinalIgnoreCase)) && ((!String.IsNullOrEmpty(orderItem.Action.Reason) && (orderItem.Action.Reason.ToLower() == "undobar" || orderItem.Action.Reason.Equals("soft_delete", StringComparison.OrdinalIgnoreCase) || orderItem.Action.Reason.Equals("delink", StringComparison.OrdinalIgnoreCase) || orderItem.Action.Reason.Equals("hard_delete", StringComparison.OrdinalIgnoreCase))) || orderItem.Action.Code.Equals("reactivate", StringComparison.OrdinalIgnoreCase)))
                                                else if (((!string.IsNullOrEmpty(serviceInstanceStatus) && serviceInstanceStatus.Equals("PENDING-CEASE", StringComparison.OrdinalIgnoreCase)) && ((!String.IsNullOrEmpty(orderItem.Action.Reason) && (orderItem.Action.Reason.Equals("soft_delete", StringComparison.OrdinalIgnoreCase) || orderItem.Action.Reason.Equals("delink", StringComparison.OrdinalIgnoreCase) || orderItem.Action.Reason.Equals("hard_delete", StringComparison.OrdinalIgnoreCase))) || orderItem.Action.Code.Equals("reactivate", StringComparison.OrdinalIgnoreCase))) || (!String.IsNullOrEmpty(orderItem.Action.Reason) && orderItem.Action.Reason.Equals("undobar", StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    // Do Nothing
                                                    // Don't ignore the order in case of compromise recovery when email service is in pending cease state.
                                                }
                                                else
                                                {
                                                    throw new DnpException("Error in getting Email service: No active Email service instance is mapped to given Emailname");
                                                }

                                                SaaSNS.Attribute siBacNumber = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                siBacNumber.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                siBacNumber.Name = "SIBACNUMBER";
                                                siBacNumber.Value = SIBacNumber.ToString();
                                                roleInstance.Attributes.Add(siBacNumber);

                                                SaaSNS.Attribute primaryEmailSI = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                primaryEmailSI.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                if (request.Order.OrderItem[0].Action.Reason != null && !string.IsNullOrEmpty(request.Order.OrderItem[0].Action.Reason) && request.Order.OrderItem[0].Action.Reason.Equals("delink", StringComparison.OrdinalIgnoreCase))
                                                    primaryEmailSI.Name = "SourcePrimayemailId";
                                                else
                                                    primaryEmailSI.Name = "PrimaryEmail";
                                                primaryEmailSI.Value = primaryEmail.ToString();
                                                roleInstance.Attributes.Add(primaryEmailSI);
                                                //Changes for Qc 121261(BTR - 106921)
                                                //Pick the Email_Supplier value from Email Client Identity chars for Bar/Undobar/Soft_ Mailbox/Hard_Delete scenarios
                                                if (request.Order.OrderItem[0].Action.Reason != null && request.Order.OrderItem[0].Action.Reason.Equals("BreachOfTermsAndConditions", StringComparison.OrdinalIgnoreCase) ||
                                                    request.Order.OrderItem[0].Action.Reason != null && request.Order.OrderItem[0].Action.Reason.Equals("soft_delete", StringComparison.OrdinalIgnoreCase) ||
                                                    (request.Order.OrderItem[0].Action.Reason != null && request.Order.OrderItem[0].Action.Reason.Equals("UNDOBAR", StringComparison.OrdinalIgnoreCase))
                                                    || (request.Order.OrderItem[0].Action.Reason != null && request.Order.OrderItem[0].Action.Reason.Equals("hard_delete", StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    if (getprofileResponse1.clientProfileV1 != null && getprofileResponse1.clientProfileV1.client != null && getprofileResponse1.clientProfileV1.client.clientIdentity != null)
                                                    {
                                                        var emailIdentity = getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Where(
                                                                            ci => ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase) && ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase));

                                                        if (emailIdentity.Any())
                                                        {
                                                            var emailSupplierChar = string.Empty;
                                                            if (emailIdentity.FirstOrDefault().clientIdentityValidation.ToList().Exists(civ => civ.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase)))
                                                            {
                                                                emailSupplierChar = emailIdentity.FirstOrDefault().clientIdentityValidation.ToList().Where(civ => civ.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                                emailSupplier = emailSupplierChar;
                                                            }
                                                            //If no EmailIdentityChar found continue with the existing logic to determine the Supplier value based on Email service instance char value(may cause a prablem for Cross provisioned accounts)
                                                            if (string.IsNullOrEmpty(emailSupplierChar))
                                                                DanteRequestProcessor.GetEmailSupplier(true, ref emailSupplier, serviceInst);
                                                        }
                                                        //If no EmailIdentity found continue with the existing logic to determine the Supplier value based on Email service instance char value(may cause a prablem for Cross provisioned accounts)
                                                        else
                                                        {
                                                            DanteRequestProcessor.GetEmailSupplier(true, ref emailSupplier, serviceInst);
                                                        }
                                                    }
                                                    //Continue with the default logic if no Identities are returned in GCP call
                                                    else
                                                    {
                                                        DanteRequestProcessor.GetEmailSupplier(true, ref emailSupplier, serviceInst);
                                                    }
                                                }
                                                //Email Supplier: Added for BTR BTR-74854
                                                else
                                                    DanteRequestProcessor.GetEmailSupplier(true, ref emailSupplier, serviceInst);

                                                if (getprofileResponse1 != null && getprofileResponse1.clientProfileV1 != null && getprofileResponse1.clientProfileV1.client != null && getprofileResponse1.clientProfileV1.client.clientIdentity != null &&
                                                    getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    string AHTBAC = getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                    AddorUpdateProductAttributes("AHTBAC", AHTBAC, ref roleInstance, BT.SaaS.Core.Shared.Entities.DataActionEnum.add);
                                                }

                                                // To check Delink scenario.
                                                #region Delink Journey
                                                if (request.Order.OrderItem[0].Action.Reason != null && (request.Order.OrderItem[0].Action.Reason.Equals("delink", StringComparison.OrdinalIgnoreCase) || request.Order.OrderItem[0].Action.Reason.Equals("hard_delete", StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    bool IsLastEmailMove = false; bool isAffiliateEmail = false; bool isPrimaryEmail = false;
                                                    string newprimarymail = string.Empty;
                                                    int emailCount = 0;

                                                    if (!string.IsNullOrEmpty(EmailName))
                                                    {
                                                        if (EmailName.Equals(primaryEmail))
                                                            isPrimaryEmail = true;

                                                        if (!string.IsNullOrEmpty(BakId))
                                                            IsLastEmailMove = DanteRequestProcessor.isLastEmailmoved(BakId, BACID_IDENTIFER_NAMEPACE, EmailName, isPrimaryEmail, ref isAffiliateEmail, ref newprimarymail, request.Order.OrderItem[0].Action.Reason, ref emailCount);
                                                        else if (!string.IsNullOrEmpty(SIBacNumber))
                                                            IsLastEmailMove = DanteRequestProcessor.isLastEmailmoved(SIBacNumber, BACID_IDENTIFER_NAMEPACE, EmailName, isPrimaryEmail, ref isAffiliateEmail, ref newprimarymail, request.Order.OrderItem[0].Action.Reason, ref emailCount);
                                                        else if (!string.IsNullOrEmpty(primaryEmail))
                                                            IsLastEmailMove = DanteRequestProcessor.isLastEmailmoved(primaryEmail, "BTIEMAILID", EmailName, isPrimaryEmail, ref isAffiliateEmail, ref newprimarymail, request.Order.OrderItem[0].Action.Reason, ref emailCount);
                                                        //else if (!string.IsNullOrEmpty(SIBacNumber) && request.Order.OrderItem[0].Action.Reason.Equals("hard_delete", StringComparison.OrdinalIgnoreCase))
                                                        //    IsLastEmailMove = DanteRequestProcessor.isLastEmailmoved(SIBacNumber, EmailName, isPrimaryEmail, ref isAffiliateEmail, ref newprimarymail);
                                                        if (!request.Order.OrderItem[0].Action.Reason.Equals("hard_delete", StringComparison.OrdinalIgnoreCase))
                                                        {
                                                            if (isAffiliateEmail)
                                                            {
                                                                SaaSNS.Attribute btoneid = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                                btoneid.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                                btoneid.Name = "Profiletype";
                                                                btoneid.Value = "Affiliate";
                                                                roleInstance.Attributes.Add(btoneid);
                                                            }
                                                            else
                                                            {
                                                                throw new DnpException("The received email is not affiliate email");
                                                            }
                                                            if (getprofileResponse1 != null && getprofileResponse1.clientProfileV1 != null && getprofileResponse1.clientProfileV1.client != null && getprofileResponse1.clientProfileV1.client.clientIdentity != null && getprofileResponse1.clientProfileV1.client.clientIdentity.Count() > 0)
                                                            {
                                                                if (getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                                                                {
                                                                    string btoneid1 = getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                                    AddorUpdateProductAttributes("BtOneId", btoneid1, ref roleInstance, BT.SaaS.Core.Shared.Entities.DataActionEnum.change);
                                                                }
                                                                else
                                                                {
                                                                    throw new DnpException("Email hasn't linked with any btoneid");
                                                                }
                                                                if (!string.IsNullOrEmpty(targetVasprodId))
                                                                {
                                                                    AddorUpdateProductAttributes("VasProductId", targetVasprodId, ref roleInstance, BT.SaaS.Core.Shared.Entities.DataActionEnum.change);
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        throw new Exception("No Email received in request from OSCH");
                                                    }

                                                    SaaSNS.Attribute lastemailMove = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                    lastemailMove.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                    lastemailMove.Name = "IsLastEmailMove";
                                                    lastemailMove.Value = IsLastEmailMove ? "true" : "false";
                                                    roleInstance.Attributes.Add(lastemailMove);
                                                    if (IsLastEmailMove && !string.IsNullOrEmpty(BakId) && request.Order.OrderItem[0].Action.Reason.Equals("hard_delete", StringComparison.OrdinalIgnoreCase)
                                                        && !string.IsNullOrEmpty(targetVasprodId) && (targetVasprodId.Equals("VPEM000060", StringComparison.OrdinalIgnoreCase) || targetVasprodId.Equals("VPEM000030", StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        AddorUpdateProductAttributes("isBACCeaseRequired", "true", ref roleInstance, BT.SaaS.Core.Shared.Entities.DataActionEnum.change);
                                                        AddorUpdateProductAttributes("VasProductId", targetVasprodId, ref roleInstance, BT.SaaS.Core.Shared.Entities.DataActionEnum.change);
                                                    }

                                                    string reason = string.Empty;
                                                    if (isPrimaryEmail)
                                                    {
                                                        if (IsLastEmailMove)
                                                        {
                                                            reason = request.Order.OrderItem[0].Action.Reason + ":Primaryandlast";
                                                        }
                                                        else
                                                            reason = request.Order.OrderItem[0].Action.Reason + ":Primaryandnotlast";

                                                        SaaSNS.Attribute newprimaryemial = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                        newprimaryemial.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                        newprimaryemial.Name = "NewPrimaryEmail";
                                                        newprimaryemial.Value = newprimarymail;
                                                        roleInstance.Attributes.Add(newprimaryemial);
                                                    }
                                                    else
                                                    {
                                                        if (IsLastEmailMove)
                                                        {
                                                            reason = request.Order.OrderItem[0].Action.Reason + ":secondaryandlast";
                                                        }
                                                        else
                                                            reason = request.Order.OrderItem[0].Action.Reason + ":secondary";
                                                    }
                                                    if (!request.Order.OrderItem[0].Action.Reason.Equals("hard_delete", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        SaaSNS.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                        reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                        reasonAttirbute.Name = "REASON";
                                                        reasonAttirbute.Value = reason;
                                                        roleInstance.Attributes.Add(reasonAttirbute);
                                                    }

                                                }
                                                #endregion
                                            }
                                            else
                                            {
                                                throw new DnpException("Error in getting Email Service: No Email service is found with given EmailName in DnP");
                                            }
                                            // sreedhar
                                            var emailSrvcRole = (from si in getprofileResponse1.clientProfileV1.clientServiceInstanceV1
                                                                 where (si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null && si.clientServiceRole.ToList().Exists(sr => sr.clientIdentity != null && sr.clientIdentity.ToList().Exists(sri => sri.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase))))
                                                                 select si.clientServiceRole.ToList().Where(sri => sri.id.Equals("DEFAULT")).ToList());

                                            foreach (List<ClientServiceRole> csr in emailSrvcRole)
                                            {
                                                foreach (ClientServiceRole csri in csr)
                                                {
                                                    if (csri.clientIdentity != null &&
                                                        csri.clientIdentity.ToList().Exists(a => a.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && a.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        //rolestatus = csri.clientIdentity.ToList().Where(a => a.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && a.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)).Select(a => a.clientIdentityStatus).FirstOrDefault().value;
                                                        if (!string.IsNullOrEmpty(csri.clientServiceRoleStatus.value))
                                                            rolestatus = csri.clientServiceRoleStatus.value;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            throw new DnpException("Error in getting Email Service: No Profile is found with given EmailName in DnP");
                                        }
                                    }
                                }
                                #region Movemailbox
                                else if (response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.modifyService && !string.IsNullOrEmpty(request.Order.OrderItem[0].Action.Reason) && request.Order.OrderItem[0].Action.Reason.Equals("MailboxMove", StringComparison.OrdinalIgnoreCase))
                                {
                                    //getting batch response for source bac id and target bac 
                                    GetBatchProfileV1Response Batchprofileresponse = new GetBatchProfileV1Response();
                                    GetBatchProfileV1Res responsegsup = new GetBatchProfileV1Res();

                                    string identity2 = string.Empty; string email = string.Empty;
                                    string TarvasproID = string.Empty; string sourceBillAccountID = string.Empty;
                                    string sourceserviceprovider = string.Empty; string sourceVas_Product_Id = string.Empty;
                                    string SourcePrimayemailId = string.Empty;
                                    string btcom = string.Empty;
                                    bool ISTargetAHT = false;
                                    bool IsEmailSrvcExistInTarget = false;
                                    bool isprimaryemailmove = false;
                                    bool islastemailMove = false;
                                    bool isMoveClientIdentity = false;
                                    bool isUnmeregeProfile = false;
                                    bool isUnresolvedemail = false;
                                    bool moveAllMailstoTarget = false;
                                    //string sourceserviceprovider = string.Empty;
                                    string targetserviceprovider = string.Empty;
                                    int assignedemails = 0;
                                    string SIemailname = string.Empty;
                                    List<string> ListOfEmails = new List<string>();
                                    // bool isMoveMailboxPremiummailBarred = false;
                                    string[] allowImportMailboxes = ConfigurationManager.AppSettings["ImportAllowedStatus"].Split(',');
                                    string[] allowunmeregedImportMailboxes = ConfigurationManager.AppSettings["ImportUnmergedAllowedStatus"].Split(',');

                                    if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("sourcebillaccountnumber")))
                                        sourceBillAccountID = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("sourcebillaccountnumber")).FirstOrDefault().Value;
                                    if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("targetbillaccountnumber")))
                                        identity2 = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("targetbillaccountnumber")).FirstOrDefault().Value;

                                    if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("targetvasproductid")))
                                        TarvasproID = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("targetvasproductid")).FirstOrDefault().Value;
                                    else if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("vasproductid")))
                                        TarvasproID = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("vasproductid")).FirstOrDefault().Value;


                                    if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("emailname")))
                                        email = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailname")).FirstOrDefault().Value;

                                    string[] emailList = email.Split(',');

                                    GetClientProfileV1Res gcpResposne = new GetClientProfileV1Res();
                                    gcpResposne = DnpWrapper.GetClientProfileV1(emailList[0], "BTIEMAILID");


                                    if (gcpResposne != null && gcpResposne.clientProfileV1 != null && gcpResposne.clientProfileV1.clientServiceInstanceV1 != null &&
                                            gcpResposne.clientProfileV1.clientServiceInstanceV1.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals(BTEMAIL_SERVICECODE_NAMEPACE)))
                                    {
                                        var emailSrvc = gcpResposne.clientProfileV1.clientServiceInstanceV1.ToList().Where
                                                          (si => si.clientServiceInstanceIdentifier.value.Equals(BTEMAIL_SERVICECODE_NAMEPACE) &&
                                                           si.clientServiceRole.ToList().Exists(sr => sr.clientIdentity != null && sr.clientIdentity.Count() > 0 && sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID") && ci.value.Equals(emailList[0], StringComparison.OrdinalIgnoreCase))));
                                        #region EamilService Check
                                        if (emailSrvc.Any())
                                        {
                                            ClientServiceInstanceV1 srvcIntance = emailSrvc.FirstOrDefault();

                                            #region PMDebtBarred Scenarios
                                            if (srvcIntance.clientServiceInstanceCharacteristic.ToList().Exists(sic => sic.name.Equals("ISP_SERVICE_CLASS", StringComparison.OrdinalIgnoreCase) && sic.value.Equals("ISP_SC_PM")))
                                            {
                                                if (gcpResposne.clientProfileV1.client != null && gcpResposne.clientProfileV1.client.clientIdentity != null && gcpResposne.clientProfileV1.client.clientIdentity.Count() > 0
                                                    && gcpResposne.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(emailList[0]))
                                                    && gcpResposne.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(emailList[0]) && ci.clientIdentityStatus.value.Equals("BARRED-ABUSE", StringComparison.OrdinalIgnoreCase) && ci.clientIdentityValidation.ToList().Exists(CIVald => CIVald.name.Equals("BARRED_REASON", StringComparison.OrdinalIgnoreCase) && CIVald.value.Equals("PM_DebtBarred", StringComparison.OrdinalIgnoreCase))))
                                                {
                                                    //isMoveMailboxPremiummailBarred = true;

                                                    //SaaSNS.Attribute isMoveMailboxPremiummailBarredAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                    //isMoveMailboxPremiummailBarredAttr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                    //isMoveMailboxPremiummailBarredAttr.Name = "isMoveMailboxPremiummailBarred";
                                                    //isMoveMailboxPremiummailBarredAttr.Value = isMoveMailboxPremiummailBarred.ToString();
                                                    //roleInstance.Attributes.Add(isMoveMailboxPremiummailBarredAttr);

                                                    //to reactivate the premium email from barred abuse changing the action from modify to Reactivate                                                    
                                                    //SaaSNS.Attribute BarredAbuseEmailList = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                    //BarredAbuseEmailList.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                    //BarredAbuseEmailList.Name = "InactiveEmailList";
                                                    //BarredAbuseEmailList.Value = emailId;
                                                    //roleInstance.Attributes.Add(BarredAbuseEmailList);
                                                }
                                            }
                                            #endregion

                                            //fetch the sourcebillaccountnumber from emailid using GCP call to DnP as part of BTR-106921
                                            if (string.IsNullOrEmpty(sourceBillAccountID))
                                            {
                                                if (srvcIntance.serviceIdentity != null &&
                                                    srvcIntance.serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    sourceBillAccountID = srvcIntance.serviceIdentity.ToList().Where(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                                    SaaSNS.Attribute sourceBillAccountno = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                    sourceBillAccountno.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                    sourceBillAccountno.Name = "sourcebillaccountnumber";
                                                    sourceBillAccountno.Value = sourceBillAccountID;
                                                    roleInstance.Attributes.Add(sourceBillAccountno);
                                                }
                                            }

                                            if (srvcIntance.serviceIdentity != null &&
                                                srvcIntance.serviceIdentity.ToList().Exists(si => si.domain.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                SourcePrimayemailId = srvcIntance.serviceIdentity.ToList().Where(si => si.domain.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                                SaaSNS.Attribute SourcePrimayemailIdAtrr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                SourcePrimayemailIdAtrr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                SourcePrimayemailIdAtrr.Name = "SourcePrimayemailId";
                                                SourcePrimayemailIdAtrr.Value = SourcePrimayemailId;
                                                roleInstance.Attributes.Add(SourcePrimayemailIdAtrr);
                                            }
                                            //Todo : Need to update to fetch the EmailSupplier from client identity validations.
                                            if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsdefaultemailsupplierMX"]))
                                            {
                                                sourceserviceprovider = "MX";
                                            }
                                            else
                                            {
                                                if (string.IsNullOrEmpty(sourceserviceprovider) && gcpResposne.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(emailList[0], StringComparison.OrdinalIgnoreCase) && ci.clientIdentityValidation != null && ci.clientIdentityValidation.ToList().Exists(civ => civ.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase))))
                                                {
                                                    var servicecharstarget = from si in gcpResposne.clientProfileV1.client.clientIdentity
                                                                             where ((si.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && si.value.Equals(emailList[0], StringComparison.OrdinalIgnoreCase)) && si.clientIdentityValidation != null)
                                                                             select si.clientIdentityValidation.ToList().Where(ci => ci.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                                    if (servicecharstarget.Any())
                                                    {
                                                        sourceserviceprovider = servicecharstarget.ToList().FirstOrDefault();
                                                    }
                                                }
                                            }
                                            //Get the source EmailSupplier

                                            sourceVas_Product_Id = srvcIntance.clientServiceInstanceCharacteristic.ToList().Where(
                                                            x => x.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                            //Commenting as email supplier check if no loger needed.Email supplier value is populated at client identity level from CCP 53
                                            //Getting Service provider Id of Source Email Service 
                                            //if (string.IsNullOrEmpty(sourceserviceprovider))
                                            //{
                                            //    var servicecharstarget = from si in clp.clientServiceInstanceV1
                                            //                             where (si.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase))
                                            //                             select si.clientServiceInstanceCharacteristic.ToList().Where(csi => csi.name.Equals("EMAIL_SUPPLIER")).FirstOrDefault().value;
                                            //    if (servicecharstarget.Any())
                                            //    {
                                            //        sourceserviceprovider = servicecharstarget.ToList().FirstOrDefault();
                                            //    }
                                            //}
                                        }
                                        else
                                        {
                                            throw new DnpException("Error in getting Email Service: No Profile is found with given EmailName in DnP");
                                        }
                                        #endregion

                                        if (emailList.Count() == 1 && gcpResposne != null && gcpResposne.clientProfileV1 != null && gcpResposne.clientProfileV1.client != null && gcpResposne.clientProfileV1.client.clientIdentity != null && gcpResposne.clientProfileV1.client.clientIdentity.Count() > 0
                                            && gcpResposne.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(emailList[0], StringComparison.OrdinalIgnoreCase)))
                                        {
                                            var moveEmailIdentity = gcpResposne.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(emailList[0], StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                                            if (!gcpResposne.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                if (!(moveEmailIdentity != null && allowunmeregedImportMailboxes.Contains(moveEmailIdentity.clientIdentityStatus.value)))
                                                    throw new DnpException("Mailbox status is not in a allowed ImportMailbox statuses");
                                            }
                                            else if(!(moveEmailIdentity!=null&&allowImportMailboxes.Contains(moveEmailIdentity.clientIdentityStatus.value)))
                                                throw new DnpException("Mailbox status is not in a allowed ImportMailbox statuses");

                                        }
                                    }
                                    else
                                    {
                                        throw new DnpException("Error in getting Email Service: No Profile is found with given EmailName in DnP");
                                    }

                                    SaaSNS.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    reasonAttirbute.Name = "REASON";
                                    reasonAttirbute.Value = orderItem.Action.Reason.ToString();
                                    roleInstance.Attributes.Add(reasonAttirbute);                                    
                                                                      
                                    // GSUP using Source BAC
                                    if (string.IsNullOrEmpty(sourceBillAccountID))
                                        responsegsup = DnpWrapper.GetServiceUserProfilesV1ForDante(SourcePrimayemailId, "BTIEMAILID");
                                    else
                                        responsegsup = DnpWrapper.GetServiceUserProfilesV1ForDante(sourceBillAccountID, BACID_IDENTIFER_NAMEPACE);

                                    //ccp 79
                                    bool isSourceProfileNayan = false;
                                    //isSourceProfileNayan = IsNayanProfile(sourceBillAccountID);
                                    string targetValue = string.Empty;
                                    isSourceProfileNayan = IsNayanProfile(sourceBillAccountID, ref targetValue);

                                    //Code to be removed 
                                    if (!string.IsNullOrEmpty(sourceBillAccountID))
                                    {
                                        //ccp 79
                                        //to check for Nayan profile as per CCP79
                                        SaaSNS.Attribute IsSourceNayanProfile = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        IsSourceNayanProfile.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        IsSourceNayanProfile.Name = "IsSourceNayanProfile";
                                        IsSourceNayanProfile.Value = isSourceProfileNayan.ToString();
                                        roleInstance.Attributes.Add(IsSourceNayanProfile);

                                        GetClientProfileV1Res primarygcpResposne = new GetClientProfileV1Res();
                                        primarygcpResposne = DnpWrapper.GetClientProfileV1(sourceBillAccountID, BACID_IDENTIFER_NAMEPACE);

                                        if (primarygcpResposne != null && primarygcpResposne.clientProfileV1 != null && primarygcpResposne.clientProfileV1.client != null &&
                                            primarygcpResposne.clientProfileV1.client.clientCharacteristic != null && primarygcpResposne.clientProfileV1.client.clientCharacteristic.ToList().Exists(ch => ch.name.Equals("DEFAULT_MAILBOX", StringComparison.OrdinalIgnoreCase)))
                                        {
                                            SaaSNS.Attribute IsFavouriteMailbox = new BT.SaaS.Core.Shared.Entities.Attribute();
                                            IsFavouriteMailbox.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                            IsFavouriteMailbox.Name = "IsFavouriteMailbox";
                                            if (emailList.Contains(primarygcpResposne.clientProfileV1.client.clientCharacteristic.ToList().Where(ch => ch.name.Equals("DEFAULT_MAILBOX", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value))
                                                IsFavouriteMailbox.Value = primarygcpResposne.clientProfileV1.client.clientCharacteristic.ToList().Where(ch => ch.name.Equals("DEFAULT_MAILBOX", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                            else
                                                IsFavouriteMailbox.Value = "false";
                                                                                        
                                            roleInstance.Attributes.Add(IsFavouriteMailbox);
                                        }
                                    }

                                    if (responsegsup != null && responsegsup.clientProfileV1 != null && responsegsup.clientProfileV1.Count() > 0)
                                    {
                                        //Check for Multiple mailbox move or single mailbox move
                                        if (emailList.Length > 1)
                                        {
                                            foreach (ClientProfileV1 clp in responsegsup.clientProfileV1)
                                            {
                                                //Todo : Move the if conditions to lamdba
                                                if (clp.clientServiceInstanceV1 != null)
                                                {
                                                    var defaulttroles = from si in clp.clientServiceInstanceV1
                                                                        where (si.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase) &&
                                                                               si.serviceIdentity.ToList().Exists(sri => sri.domain != null && sri.domain.Equals("BTIEMAILID") && sri.value.Equals(SourcePrimayemailId, StringComparison.OrdinalIgnoreCase)))
                                                                        select si.clientServiceRole.ToList().Where(sri => sri.id.Equals("DEFAULT")).ToList();
                                                    if (defaulttroles.Any())
                                                    {
                                                        assignedemails = assignedemails + defaulttroles.ToList().FirstOrDefault().Count();
                                                    }
                                                }
                                            }
                                            moveAllMailstoTarget = assignedemails.Equals(emailList.Length);

                                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "IsMultipleMaiboxMove", Value = "true" });
                                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "MoveallEmailstoTarget", Value = moveAllMailstoTarget ? "true" : "false" });

                                            if (!moveAllMailstoTarget)
                                            {
                                                if (emailList.Contains(SourcePrimayemailId, StringComparer.OrdinalIgnoreCase))
                                                {
                                                    isprimaryemailmove = true;
                                                    SaaSNS.Attribute newprimaryemial = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                    newprimaryemial.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                    newprimaryemial.Name = "NewPrimaryEmail";
                                                    newprimaryemial.Value = DanteRequestProcessor.GetNewPrimaryEmailforMailboxMove(responsegsup, emailList);
                                                    roleInstance.Attributes.Add(newprimaryemial);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            #region Primary and LastEmail Checks
                                            //Checking Primary and last email move conditions.... 

                                            if (!string.IsNullOrEmpty(SourcePrimayemailId) && SourcePrimayemailId.Equals(emailList[0], StringComparison.OrdinalIgnoreCase))
                                            {
                                                isprimaryemailmove = true;
                                            }

                                            foreach (ClientProfileV1 clp in responsegsup.clientProfileV1)
                                            {

                                                var defaulttroles = from si in clp.clientServiceInstanceV1
                                                                    where (si.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase))
                                                                    select si.clientServiceRole.ToList().Where(sri => sri.id.Equals("DEFAULT")).ToList();
                                                if (defaulttroles.Any())
                                                {
                                                    assignedemails = assignedemails + defaulttroles.ToList().FirstOrDefault().Count();
                                                }
                                            }

                                            if (isprimaryemailmove && !islastemailMove)
                                            {
                                                SaaSNS.Attribute newprimaryemial = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                newprimaryemial.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                newprimaryemial.Name = "NewPrimaryEmail";
                                                newprimaryemial.Value = DanteRequestProcessor.GetNewPrimaryEmailforMailboxMove(responsegsup, emailList);
                                                roleInstance.Attributes.Add(newprimaryemial);
                                            }
                                            else if (!isprimaryemailmove && islastemailMove)
                                            {
                                                SaaSNS.Attribute newprimaryemial = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                newprimaryemial.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                newprimaryemial.Name = "NewPrimaryEmail";
                                                newprimaryemial.Value = SIemailname;
                                                roleInstance.Attributes.Add(newprimaryemial);
                                            }
                                            #endregion
                                        }
                                    }
                                    
                                    //ccp 79
                                    ListOfEmails = DanteRequestProcessor.GetListOfMailboxestoMove(responsegsup, emailList, sourceBillAccountID, SourcePrimayemailId, isSourceProfileNayan);

                                    if (ListOfEmails != null && ListOfEmails.Count > 0)
                                    {
                                        if (ListOfEmails[0] != null)
                                        {
                                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ListOfEmailsToMove", Value = ListOfEmails[0] });
                                        }
                                        if (ListOfEmails[1] != null)
                                        {
                                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "UnresolvedEmailsList", Value = ListOfEmails[1] });
                                        }
                                        if (ListOfEmails[2] != null)
                                        {
                                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "AffiliateEmailsList", Value = ListOfEmails[2] });
                                        }
                                        if (ListOfEmails[3] != null)
                                        {
                                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "MarkedInactiveMailboxList", Value = ListOfEmails[3] });
                                        }
                                        if (ListOfEmails[4] != null)
                                        {
                                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "PremiumEmailDebtBarredList", Value = ListOfEmails[4] });
                                        }
                                        if (ListOfEmails[5] != null)
                                        {
                                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ListOfActiveEmails", Value = ListOfEmails[5] });
                                        }
                                        if (ListOfEmails[6] != null)
                                        {
                                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ListOfInActiveEmails", Value = ListOfEmails[6] });
                                        }
                                        if (ListOfEmails[7] != null)
                                        {
                                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "UnmergedEmaillist", Value = ListOfEmails[7] });
                                        }
                                    }

                                    SaaSNS.Attribute IsPrimaryEmailMove = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    IsPrimaryEmailMove.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    IsPrimaryEmailMove.Name = "IsPrimaryEmailMove";
                                    IsPrimaryEmailMove.Value = isprimaryemailmove ? "true" : "false";
                                    roleInstance.Attributes.Add(IsPrimaryEmailMove);

                                    SaaSNS.Attribute lastemailMove = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    lastemailMove.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    lastemailMove.Name = "IsLastEmailMove";
                                    lastemailMove.Value = assignedemails == 1 ? "true" : "false";
                                    roleInstance.Attributes.Add(lastemailMove);
                                    //islastemailMove = assignedemails == 1 ? true : false;

                                    SaaSNS.Attribute moveAllMails = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    moveAllMails.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    moveAllMails.Name = "IsmoveAllMailstoTarget";
                                    moveAllMails.Value = moveAllMailstoTarget ? "true" : "false";
                                    roleInstance.Attributes.Add(moveAllMails);                                    

                                    SaaSNS.Attribute isMoveClientIdentityAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    isMoveClientIdentityAttr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    isMoveClientIdentityAttr.Name = "isMoveClientIdentity";
                                    isMoveClientIdentityAttr.Value = isMoveClientIdentity ? "true" : "false";
                                    roleInstance.Attributes.Add(isMoveClientIdentityAttr);

                                    SaaSNS.Attribute isUnresolvedemailAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    isUnresolvedemailAttr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    isUnresolvedemailAttr.Name = "isUnresolvedemail";
                                    isUnresolvedemailAttr.Value = isUnresolvedemail ? "true" : "false";
                                    roleInstance.Attributes.Add(isUnresolvedemailAttr);

                                    #region TargetBAC Checks

                                    //check whether the target BAC is AHT or not and has Email service    

                                    GetClientProfileV1Res gcpresponse = DnpWrapper.GetClientProfileV1(identity2, "VAS_BILLINGACCOUNT_ID");

                                    //ccp 79
                                    string nayanBTId = string.Empty;
                                    bool isTargetNayan = IsNayanProfile(identity2,ref nayanBTId);

                                    SaaSNS.Attribute IsTargetNayanProfile = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    IsTargetNayanProfile.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    IsTargetNayanProfile.Name = "IsTargetNayanProfile";
                                    IsTargetNayanProfile.Value = isTargetNayan.ToString();
                                    roleInstance.Attributes.Add(IsTargetNayanProfile);

                                    if (gcpresponse != null && gcpresponse.clientProfileV1 != null && gcpresponse.clientProfileV1.client != null
                                        && gcpresponse.clientProfileV1.client.clientIdentity != null)
                                    {
                                        var accounttrust = from si in gcpresponse.clientProfileV1.client.clientIdentity
                                                           where ((si.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && si.value.Equals(identity2, StringComparison.OrdinalIgnoreCase)) && si.clientIdentityValidation != null)
                                                           select si.clientIdentityValidation.ToList().Where(ci => ci.name.Equals("ACCOUNTTRUSTMETHOD")).FirstOrDefault().value;

                                        if (accounttrust.Any())
                                        {
                                            if (gcpresponse.clientProfileV1.client.clientIdentity.Count() > 0 && gcpresponse.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                btcom = gcpresponse.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                                // adding btcom attribute if not exists else change the btcom value from source to target to move CI directly using btoneid.
                                                if (roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("BTONEID", StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("BTONEID", StringComparison.OrdinalIgnoreCase) && attr.Value.Equals(btcom, StringComparison.OrdinalIgnoreCase)))
                                                        roleInstance.Attributes.ToList().Where(attr => attr.Name.Equals("BTONEID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = btcom;
                                                }
                                                else
                                                {
                                                    SaaSNS.Attribute Tragetbtcomattr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                    Tragetbtcomattr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                    Tragetbtcomattr.Name = "BTONEID";
                                                    Tragetbtcomattr.Value = btcom;
                                                    roleInstance.Attributes.Add(Tragetbtcomattr);
                                                }
                                            }

                                            if (ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(accounttrust.ToList().FirstOrDefault().ToLower()))
                                                ISTargetAHT = true;
                                        }

                                        //ccp 79
                                        if(isTargetNayan&&!string.IsNullOrEmpty(nayanBTId))
                                        {                                            
                                            if (roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("BTONEID", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("BTONEID", StringComparison.OrdinalIgnoreCase) && attr.Value.Equals(nayanBTId, StringComparison.OrdinalIgnoreCase)))
                                                    roleInstance.Attributes.ToList().Where(attr => attr.Name.Equals("BTONEID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = nayanBTId;
                                            }
                                            else
                                            {
                                                SaaSNS.Attribute Tragetbtcomattr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                Tragetbtcomattr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                Tragetbtcomattr.Name = "BTONEID";
                                                Tragetbtcomattr.Value = nayanBTId;
                                                roleInstance.Attributes.Add(Tragetbtcomattr);
                                            }
                                        }
                                    
                                        //ccp 79 - adding condition check
                                        if (!IsEmailSrvcExistInTarget)
                                        {
                                            if (gcpresponse.clientProfileV1.clientServiceInstanceV1 != null && gcpresponse.clientProfileV1.clientServiceInstanceV1.Count() > 0 &&
                                                gcpresponse.clientProfileV1.clientServiceInstanceV1.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase)
                                                && csi.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(BACID_IDENTIFER_NAMEPACE) && sIden.value.Equals(identity2))))
                                            {
                                                IsEmailSrvcExistInTarget = true;

                                                ClientServiceInstanceV1 targetEmailSrvc = gcpresponse.clientProfileV1.clientServiceInstanceV1.ToList().Where(csi => csi.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase)
                                                && csi.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(BACID_IDENTIFER_NAMEPACE) && sIden.value.Equals(identity2))).FirstOrDefault();

                                                if (targetEmailSrvc!=null&& targetEmailSrvc.clientServiceInstanceCharacteristic!=null)
                                                {
                                                    //ClientServiceInstanceV1 targetSrvcIntance = targetEmailSrvc.FirstOrDefault();

                                                    TarvasproID = targetEmailSrvc.clientServiceInstanceCharacteristic.ToList().Where(
                                                            x => x.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                }
                                            }
                                        }
                                        //ccp 79
                                        if(!IsEmailSrvcExistInTarget && isTargetNayan)
                                            throw new DnpException("Target Nayan Profile doesn't have email service");
                                    }
                                    else
                                    {
                                        throw new DnpException("No Profile is found for the given TargetBAC in DnP");
                                    }

                                    //added this condition to add the target vasproductid into vasproduct id. 
                                    if (roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("vasproductid", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        roleInstance.Attributes.ToList().Where(attr => attr.Name.Equals("vasproductid", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = TarvasproID;
                                    }
                                    else
                                    {
                                        SaaSNS.Attribute VasProductID = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        VasProductID.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        VasProductID.Name = "VASPRODUCTID";
                                        VasProductID.Value = TarvasproID;
                                        roleInstance.Attributes.Add(VasProductID);
                                    }

                                    SaaSNS.Attribute targetAHTAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    targetAHTAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    targetAHTAttirbute.Name = "IsTargetAHTDone";
                                    targetAHTAttirbute.Value = ISTargetAHT ? "true" : "false";
                                    roleInstance.Attributes.Add(targetAHTAttirbute);

                                    SaaSNS.Attribute emailserviceinTarget = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    emailserviceinTarget.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    emailserviceinTarget.Name = "IsEmailSrvcExistInTarget";
                                    emailserviceinTarget.Value = IsEmailSrvcExistInTarget ? "true" : "false";
                                    roleInstance.Attributes.Add(emailserviceinTarget);

                                    //To be removed once if no issues are observed
                                    SaaSNS.Attribute EmailSupplierForMailboxMove = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    EmailSupplierForMailboxMove.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    EmailSupplierForMailboxMove.Name = "EmailSupplierForMailboxMove";
                                    EmailSupplierForMailboxMove.Value = sourceserviceprovider;
                                    roleInstance.Attributes.Add(EmailSupplierForMailboxMove);

                                    //added this condition to add the target vasproductid into vasproduct id. 
                                    if (roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("EMAILSUPPLIER", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        roleInstance.Attributes.ToList().Where(attr => attr.Name.Equals("EMAILSUPPLIER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = sourceserviceprovider;
                                    }
                                    else
                                    {
                                        SaaSNS.Attribute EmailSupplier = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        EmailSupplier.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        EmailSupplier.Name = "EMAILSUPPLIER";
                                        EmailSupplier.Value = sourceserviceprovider;
                                        roleInstance.Attributes.Add(EmailSupplier);
                                    }

                                    //Ignoring the emailsupplier check at source & target profiles as part of BTR-106921 move mail box CCP53.

                                    //if (IsEmailSrvcExistInTarget)
                                    //{
                                    //    if (sourceserviceprovider != targetserviceprovider)
                                    //    {
                                    //        // need to reject the order if the source and target service providers are not equal.
                                    //        throw new DnpException("Rejected Mailbox move request as email service providers of source and target bac do not match");
                                    //    }
                                    //}
                                    #endregion

                                    bool isCossupplierChangeRequired = false; string targetvasproductid = string.Empty;
                                    if (!string.IsNullOrEmpty(sourceVas_Product_Id) && !string.IsNullOrEmpty(TarvasproID))
                                    {
                                        if (!string.Equals(sourceVas_Product_Id, TarvasproID, StringComparison.OrdinalIgnoreCase))
                                        {
                                            isCossupplierChangeRequired = true;
                                        }
                                    }

                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "isCossupplierChangeRequired", Value = isCossupplierChangeRequired ? "True" : "False" });

                                    AddorUpdateProductAttributes("targetvasproductid", TarvasproID, ref roleInstance, BT.SaaS.Core.Shared.Entities.DataActionEnum.change);

                                    //if (!roleInstance.Attributes.Exists(ri => ri.Name.Equals("targetvasproductid", StringComparison.OrdinalIgnoreCase)))
                                    //    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "targetvasproductid", Value = TarvasproID });
                                }
                                #endregion
                                if (response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.cease)
                                {
                                    if (getprofileResponse1.clientProfileV1.client != null && getprofileResponse1.clientProfileV1.client.clientIdentity != null && getprofileResponse1.clientProfileV1.client.clientIdentity.Count() > 0)
                                    {
                                        if (getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(BakId)))
                                        {
                                            IsExsitingBacID = true;
                                        }
                                    }
                                    //SaaSNS.Attribute isExistingAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    //isExistingAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    //isExistingAttribute.Name = "ISEXISTINGACCOUNT";
                                    isExistingAttribute.Value = IsExsitingBacID.ToString();
                                    roleInstance.Attributes.Add(isExistingAttribute);

                                    if (getprofileResponse1 != null && getprofileResponse1.clientProfileV1 != null
                                           && getprofileResponse1.clientProfileV1.client.clientIdentity != null && getprofileResponse1.clientProfileV1.client.clientIdentity.Count() > 0)
                                    {
                                        if (getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase)))
                                        {
                                            var EmailClientIdentitiesCount = from localclientIdentity in getprofileResponse1.clientProfileV1.client.clientIdentity
                                                                             where localclientIdentity.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && localclientIdentity.clientIdentityStatus.value.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase)
                                                                             select getprofileResponse1.clientProfileV1.client.clientIdentity.Count();
                                            if (EmailClientIdentitiesCount.Count() == 1)
                                            {
                                                IsLastEmailId = true;
                                            }
                                            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Entered Cancel flow : EmailClientIdentitiesCount:" + EmailClientIdentitiesCount.Count());
                                        }
                                    }

                                    SaaSNS.Attribute isLastBTEmailId = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    isLastBTEmailId.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    isLastBTEmailId.Name = "ISLASTEMAILID";
                                    isLastBTEmailId.Value = IsLastEmailId.ToString();
                                    roleInstance.Attributes.Add(isLastBTEmailId);
                                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Entered Cancel flow : IsLastEmailId:" + IsLastEmailId.ToString());

                                    if (!String.IsNullOrEmpty(orderItem.Action.Reason) && !orderItem.Action.Reason.Equals("delink", StringComparison.OrdinalIgnoreCase))
                                    {
                                        SaaSNS.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        reasonAttirbute.Name = "REASON";
                                        reasonAttirbute.Value = orderItem.Action.Reason.ToString();
                                        roleInstance.Attributes.Add(reasonAttirbute);
                                    }
                                }
                                else if (response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.reActivate && string.IsNullOrEmpty(request.Order.OrderItem[0].Action.Reason))
                                {
                                    isVASReactivate = false;

                                    if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("REASON", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        SaaSNS.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        reasonAttirbute.Name = "REASON";
                                        reasonAttirbute.Value = request.Order.OrderItem[0].Action.Code.ToLower();
                                        roleInstance.Attributes.Add(reasonAttirbute);
                                    }
                                }
                                else if (response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.reActivate || (response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.modifyService && !string.IsNullOrEmpty(request.Order.OrderItem[0].Action.Reason)) || (!string.IsNullOrEmpty(request.Order.OrderItem[0].Action.Reason) && request.Order.OrderItem[0].Action.Reason.Equals("AffiliateClaim", StringComparison.OrdinalIgnoreCase)))
                                {
                                    if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("REASON", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        SaaSNS.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        reasonAttirbute.Name = "REASON";
                                        reasonAttirbute.Value = orderItem.Action.Reason.ToString();
                                        roleInstance.Attributes.Add(reasonAttirbute);
                                    }
                                }

                                if (serviceAddressingFrom.Equals(ConfigurationManager.AppSettings["BBMTServiceAddress"]))
                                {
                                    SaaSNS.Attribute isResolveorder = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    isResolveorder.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    isResolveorder.Name = "ISRESOLVEORDER";
                                    isResolveorder.Value = "true";
                                    roleInstance.Attributes.Add(isResolveorder);
                                }

                                SaaSNS.Attribute SIKey = new BT.SaaS.Core.Shared.Entities.Attribute();
                                SIKey.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                SIKey.Name = "SERVICEINSTANCEKEY";
                                SIKey.Value = ServiceInstanceKey.ToString();
                                roleInstance.Attributes.Add(SIKey);
                            }
                            //Added for BTR BTR-74854
                            if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("EMAILSUPPLIER", StringComparison.OrdinalIgnoreCase)))
                            {
                                SaaSNS.Attribute emailSupplierAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                emailSupplierAttr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                emailSupplierAttr.Name = "EMAILSUPPLIER";
                                emailSupplierAttr.Value = emailSupplier;
                                roleInstance.Attributes.Add(emailSupplierAttr);
                            }
                            else
                            {
                                roleInstance.Attributes.ToList().Where(roleInstAttrb => roleInstAttrb.Name.Equals("EMAILSUPPLIER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = emailSupplier;
                            }
                            if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("rolestatus", StringComparison.OrdinalIgnoreCase)))
                            {
                                SaaSNS.Attribute rolestatusvalue = new BT.SaaS.Core.Shared.Entities.Attribute();
                                rolestatusvalue.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                rolestatusvalue.Name = "rolestatus";
                                rolestatusvalue.Value = rolestatus;
                                roleInstance.Attributes.Add(rolestatusvalue);
                            }

                        }
                        #endregion

                        //ccp 78 - MD2
                        #region Cloud Journey
                        if (ConfigurationManager.AppSettings["CloudScode"].Split(',').Contains(orderItem.Specification[0].Identifier.Value1) && ConfigurationManager.AppSettings["NayanCloudCease"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            if (response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.cease)
                            {
                                string orderType = String.Empty;
                                IsExistingCustomer = true;                                

                                if (request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("billaccountnumber")) && !string.IsNullOrEmpty(request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value))
                                {
                                    BakId = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value;
                                }
                                ClientServiceInstanceV1[] serviceInstances = DnpWrapper.getServiceInstanceV1(BakId, "VAS_BILLINGACCOUNT_ID", ConfigurationManager.AppSettings["CAService"]);
                                if (serviceInstances != null)
                                {
                                    if (serviceInstances[0].clientServiceInstanceStatus.value.Equals("active", StringComparison.OrdinalIgnoreCase))
                                    {
                                        foreach (ClientServiceInstanceV1 srvcInstnc in serviceInstances)
                                        {
                                            if (srvcInstnc.clientServiceInstanceCharacteristic != null)
                                            {
                                                if (srvcInstnc.clientServiceInstanceCharacteristic.ToList().Exists(insChar => insChar.name.Equals("subscrption_external", StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    subscriptionExternalReference = srvcInstnc.clientServiceInstanceCharacteristic.ToList().Where(insChar => insChar.name.Equals("subscrption_external", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                }
                                                if (srvcInstnc.clientServiceInstanceCharacteristic.ToList().Exists(insChar => insChar.name.Equals("prev_supplier_code", StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    previousSupplierCode = srvcInstnc.clientServiceInstanceCharacteristic.ToList().Where(insChar => insChar.name.Equals("prev_supplier_code", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                }
                                                //if (srvcInstnc.clientServiceInstanceCharacteristic.ToList().Exists(insChar => insChar.name.Equals("prev_supplier_code", StringComparison.OrdinalIgnoreCase)))
                                                //{
                                                //    customerContextID = srvcInstnc.serviceIdentity.ToList().Where(si => si.domain.Equals("CCID_CLOUD", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                //    //customerContextID = srvcInstnc.clientServiceInstanceCharacteristic.ToList().Where(insChar => insChar.name.Equals("customeridentifier", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                //}
                                                if(srvcInstnc.serviceIdentity.ToList().Exists(si => si.domain.Equals("CCID_CLOUD", StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    customerContextID = srvcInstnc.serviceIdentity.ToList().Where(si => si.domain.Equals("CCID_CLOUD", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                }

                                            }
                                        }

                                        SaaSNS.Attribute subscriptionExternalReferenceAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        subscriptionExternalReferenceAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        subscriptionExternalReferenceAttribute.Name = "SUBSCRIPTIONEXTERNALREFERENCE";
                                        subscriptionExternalReferenceAttribute.Value = subscriptionExternalReference.ToString();
                                        roleInstance.Attributes.Add(subscriptionExternalReferenceAttribute);

                                        SaaSNS.Attribute supplierCodeAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        supplierCodeAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        supplierCodeAttribute.Name = "PREVIOUSSUPPLIERCODE";
                                        supplierCodeAttribute.Value = previousSupplierCode.ToString();
                                        roleInstance.Attributes.Add(supplierCodeAttribute);

                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "isnayancloudcease", Value = true.ToString() });

                                        if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("vassubtype")))
                                        {
                                            SaaSNS.Attribute subTypeAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                            subTypeAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                            subTypeAttribute.Name = "vassubtype";
                                            subTypeAttribute.Value = "Default";
                                            roleInstance.Attributes.Add(subTypeAttribute);
                                        }

                                        if (roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("CCID")))
                                        {
                                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "CUSTOMERCONTEXTID", Value = roleInstance.Attributes.ToList().Where(attr => attr.Name.Equals("CCID")).FirstOrDefault().Value });
                                        }
                                        else
                                        {
                                            SaaSNS.Attribute customerContextIDAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                            customerContextIDAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                            customerContextIDAttribute.Name = "CUSTOMERCONTEXTID";
                                            customerContextIDAttribute.Value = customerContextID.ToString();
                                            roleInstance.Attributes.Add(customerContextIDAttribute);                                            
                                        }
                                        if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("ISEXISTINGACCOUNT", StringComparison.OrdinalIgnoreCase)))
                                        {
                                            isExistingAttribute.Value = IsExistingCustomer.ToString();
                                            roleInstance.Attributes.Add(isExistingAttribute);
                                        }
                                    }

                                    else
                                    {
                                        throw new DnpException("Profile does not have active Cloud service");
                                    }
                                }
                                else
                                {
                                    throw new DnpException("Profile does not have active Cloud service");
                                }
                            }
                        }
                        #endregion

                        else if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("ISEXISTINGACCOUNT", StringComparison.OrdinalIgnoreCase)))
                        {
                            //SaaSNS.Attribute isExistingAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            //isExistingAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            //isExistingAttribute.Name = "ISEXISTINGACCOUNT";
                            isExistingAttribute.Value = IsExistingCustomer.ToString();
                            roleInstance.Attributes.Add(isExistingAttribute);
                        }

                        if (orderItem.Specification[0].Identifier.Value1.Equals("S0220670", StringComparison.OrdinalIgnoreCase) && orderItem.Action.Reason != null && orderItem.Action.Reason.Equals("barred", StringComparison.OrdinalIgnoreCase))
                        {
                            SaaSNS.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            reasonAttirbute.Name = "REASON";
                            reasonAttirbute.Value = orderItem.Action.Reason.ToString();
                            roleInstance.Attributes.Add(reasonAttirbute);
                        }

                        if (orderItem.Specification[0].Identifier.Value1.Equals("S0309824", StringComparison.OrdinalIgnoreCase))
                        {
                            if (orderItem.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase))
                            {
                                SaaSNS.Attribute serviceAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                serviceAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                serviceAttirbute.Name = "SERVICEINSTANCEEXISTS";
                                serviceAttirbute.Value = ServiceInstanceExists.ToString();
                                roleInstance.Attributes.Add(serviceAttirbute);

                                SaaSNS.Attribute isAHTAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                isAHTAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                isAHTAttribute.Name = "ISAHTDONE";
                                isAHTAttribute.Value = isAHTDone.ToString();
                                roleInstance.Attributes.Add(isAHTAttribute);

                                SaaSNS.Attribute serviceStatusAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                serviceStatusAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                serviceStatusAttribute.Name = "SERVICEINSTANCESTATUS";
                                serviceStatusAttribute.Value = serviceInstanceStatus;
                                roleInstance.Attributes.Add(serviceStatusAttribute);


                                SaaSNS.Attribute isScodeExistAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                isScodeExistAttr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                isScodeExistAttr.Name = "ISSCODEEXIST";
                                isScodeExistAttr.Value = isScodeExist.ToString();
                                roleInstance.Attributes.Add(isScodeExistAttr);


                            }
                            else if (orderItem.Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                            {
                                //if (!IsHDRDelete && roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("HDR", StringComparison.OrdinalIgnoreCase)))
                                //{
                                //    roleInstance.Attributes.Remove(roleInstance.Attributes.ToList().Where(attr => attr.Name.Equals("HDR", StringComparison.OrdinalIgnoreCase)).FirstOrDefault());
                                //}                                
                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "REASON", Value = thomasCeaseReason, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                            }
                            else if (orderItem.Action.Code.Equals("reactivate", StringComparison.OrdinalIgnoreCase))
                            {
                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "REASON", Value = thomasCeaseReason, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                            }
                        }
                        //  spring 
                        if (orderItem.Specification[0].Identifier.Value1.Equals("S0316296", StringComparison.OrdinalIgnoreCase) &&
                            !(request.Order.OrderIdentifier.Value.StartsWith("EE", StringComparison.OrdinalIgnoreCase)))
                        {
                            if (orderItem.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase))
                            {
                                SaaSNS.Attribute isAHTAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                isAHTAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                isAHTAttribute.Name = "ISAHTDONE";
                                isAHTAttribute.Value = isAHTDone.ToString();
                                roleInstance.Attributes.Add(isAHTAttribute);

                                SaaSNS.Attribute serviceAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                serviceAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                serviceAttirbute.Name = "SERVICEINSTANCEEXISTS";
                                serviceAttirbute.Value = ServiceInstanceExists.ToString();
                                roleInstance.Attributes.Add(serviceAttirbute);

                                SaaSNS.Attribute oldRVSIDAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                oldRVSIDAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                oldRVSIDAttirbute.Name = "OLD_RVSID";
                                oldRVSIDAttirbute.Value = old_rvsid.ToString();
                                roleInstance.Attributes.Add(oldRVSIDAttirbute);

                                SaaSNS.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                reasonAttirbute.Name = "REASON";
                                reasonAttirbute.Value = orderItem.Action.Reason.ToString();
                                roleInstance.Attributes.Add(reasonAttirbute);
                            }
                            else if (orderItem.Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                            {
                                SaaSNS.Attribute isAHTAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                isAHTAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                isAHTAttribute.Name = "ISAHTDONE";
                                isAHTAttribute.Value = isAHTDone.ToString();
                                roleInstance.Attributes.Add(isAHTAttribute);                               

                                SaaSNS.Attribute serviceAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                serviceAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                serviceAttribute.Name = "ServiceInstanceToBeDeleted";
                                serviceAttribute.Value = ServiceInstanceToBeDeleted.ToString();
                                roleInstance.Attributes.Add(serviceAttribute);

                                SaaSNS.Attribute oldRVSIDAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                oldRVSIDAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                oldRVSIDAttribute.Name = "OLD_RVSID";
                                oldRVSIDAttribute.Value = old_rvsid.ToString();
                                roleInstance.Attributes.Add(oldRVSIDAttribute);

                                SaaSNS.Attribute roleKey_GSM_Attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                roleKey_GSM_Attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                roleKey_GSM_Attribute.Name = "Role_Key_GSM";
                                roleKey_GSM_Attribute.Value = role_key_GSM;
                                roleInstance.Attributes.Add(roleKey_GSM_Attribute);

                                SaaSNS.Attribute deviceID_Attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                deviceID_Attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                deviceID_Attribute.Name = "OTA_DEVICE_ID";
                                deviceID_Attribute.Value = deviceID;
                                roleInstance.Attributes.Add(deviceID_Attribute);

                                SaaSNS.Attribute Sim_invite_attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                Sim_invite_attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                Sim_invite_attribute.Name = "Sim_Invitee_roles";
                                Sim_invite_attribute.Value = Sim_invite_roles;
                                roleInstance.Attributes.Add(Sim_invite_attribute);

                                SaaSNS.Attribute BAC_invite_attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                BAC_invite_attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                BAC_invite_attribute.Name = "Bac_Invitee_roles";
                                BAC_invite_attribute.Value = Bac_invite_roles;
                                roleInstance.Attributes.Add(BAC_invite_attribute);


                                //------Murali-----------
                                SaaSNS.Attribute rolekey_Delegate_attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                rolekey_Delegate_attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                rolekey_Delegate_attribute.Name = "BT_Spring_DelegateroleId";
                                rolekey_Delegate_attribute.Value = delegateSpring;
                                roleInstance.Attributes.Add(rolekey_Delegate_attribute);

                                SaaSNS.Attribute rolekey_Delegatesport_attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                rolekey_Delegatesport_attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                rolekey_Delegatesport_attribute.Name = "BT_Sport_DelegateroleId";
                                rolekey_Delegatesport_attribute.Value = delegateSport;
                                roleInstance.Attributes.Add(rolekey_Delegatesport_attribute);

                                SaaSNS.Attribute rolekey_Delegatewifi_attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                rolekey_Delegatewifi_attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                rolekey_Delegatewifi_attribute.Name = "BT_WIFI_DelegateroleId";
                                rolekey_Delegatewifi_attribute.Value = delegateWifi;
                                roleInstance.Attributes.Add(rolekey_Delegatewifi_attribute);

                                //-----------------------                            
                            }
                            else if (orderItem.Action.Code.Equals("modify", StringComparison.OrdinalIgnoreCase))
                            {
                                if (orderItem.Instance[0].InstanceCharacteristic.ToList().Exists(i => i.Name.Equals("replacementreason", StringComparison.OrdinalIgnoreCase)))
                                {
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "Num_Sim_Dispatched", Value = numSimDispatched });
                                }
                                if (!String.IsNullOrEmpty(orderItem.Action.Reason))
                                {
                                    //SIM-Activation after Sim Replacemnt.
                                    if (orderItem.Action.Reason.Equals("sim_activation", StringComparison.OrdinalIgnoreCase))
                                    {
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "Reason", Value = orderItem.Action.Reason });
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "IMSI_OLD", Value = oldImsiValue });
                                    }
                                    //Creditlimit.
                                    else if (orderItem.Action.Reason.Equals("sim_creditlimit", StringComparison.OrdinalIgnoreCase))
                                    {
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "Reason", Value = orderItem.Action.Reason });
                                    }
                                    //portin
                                    else if (orderItem.Action.Reason.Equals("portin", StringComparison.OrdinalIgnoreCase))
                                    {
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "Reason", Value = orderItem.Action.Reason });
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "OLD_MSISDN", Value = oldMsisdnValue });
                                    }
                                    else if (orderItem.Action.Reason.Equals("acceptinvite", StringComparison.OrdinalIgnoreCase) || orderItem.Action.Reason.Equals("declineinvite", StringComparison.OrdinalIgnoreCase))
                                    {
                                        SaaSNS.Attribute spOwnerAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        spOwnerAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        spOwnerAttirbute.Name = "SPOwnerBAC";
                                        spOwnerAttirbute.Value = spownerBAcID;
                                        roleInstance.Attributes.Add(spOwnerAttirbute);
                                    }

                                    SaaSNS.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    reasonAttirbute.Name = "REASON";
                                    reasonAttirbute.Value = orderItem.Action.Reason.ToString();
                                    roleInstance.Attributes.Add(reasonAttirbute);
                                }
                            }
                        }
                        //EESpring attributes.
                        if (orderItem.Specification[0].Identifier.Value1.Equals("S0316296", StringComparison.OrdinalIgnoreCase)
                            && (request.Order.OrderIdentifier.Value.StartsWith("EE", StringComparison.OrdinalIgnoreCase)))
                        {

                            if (orderItem.Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                            {
                                SaaSNS.Attribute isAHTAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                isAHTAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                isAHTAttribute.Name = "ISAHTDONE";
                                isAHTAttribute.Value = isAHTDone.ToString();
                                roleInstance.Attributes.Add(isAHTAttribute);

                                SaaSNS.Attribute serviceAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                serviceAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                serviceAttribute.Name = "ServiceInstanceToBeDeleted";
                                serviceAttribute.Value = ServiceInstanceToBeDeleted.ToString();
                                roleInstance.Attributes.Add(serviceAttribute);

                                SaaSNS.Attribute oldMSISDNAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                oldMSISDNAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                oldMSISDNAttribute.Name = "OLD_MSISDN";
                                oldMSISDNAttribute.Value = old_msisdn.ToString();
                                roleInstance.Attributes.Add(oldMSISDNAttribute);

                                SaaSNS.Attribute MSISDNAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                MSISDNAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                MSISDNAttribute.Name = "MSISDN";
                                MSISDNAttribute.Value = msisdnValue.ToString();
                                roleInstance.Attributes.Add(MSISDNAttribute);

                                SaaSNS.Attribute roleKey_GSM_Attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                roleKey_GSM_Attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                roleKey_GSM_Attribute.Name = "Role_Key_GSM";
                                roleKey_GSM_Attribute.Value = role_key_GSM;
                                roleInstance.Attributes.Add(roleKey_GSM_Attribute);

                                SaaSNS.Attribute deviceID_Attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                deviceID_Attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                deviceID_Attribute.Name = "OTA_DEVICE_ID";
                                deviceID_Attribute.Value = deviceID;
                                roleInstance.Attributes.Add(deviceID_Attribute);

                                SaaSNS.Attribute isEESpringServiceAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                isEESpringServiceAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                isEESpringServiceAttribute.Name = "isEESpringService";
                                isEESpringServiceAttribute.Value = isEESpringService.ToString();
                                roleInstance.Attributes.Add(isEESpringServiceAttribute);
                            }
                        }
                        // BT TV provision
                        if (orderItem.Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["BTTVProdCode"].ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            if (response.Header.Action == OrderActionEnum.provide)
                            {
                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "isBTTVModify", Value = isBTTVModify.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "SERVICEINSTANCEEXISTS", Value = ServiceInstanceExists.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "IsAHTDone", Value = isAHTDone.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "AdminRoleExists", Value = AdminRoleExists.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "TOTGExists", Value = TOTGExists.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                                if (!String.IsNullOrEmpty(VSIDoldValue))
                                {
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "VSIDoldValue", Value = VSIDoldValue.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                                }
                            }
                            else if (response.Header.Action == OrderActionEnum.cease)
                            {
                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "AdminRoleExists", Value = AdminRoleExists.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "TOTGExists", Value = TOTGExists.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                                if (request.Order.OrderItem.ToList().Exists(oi => oi.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("totg", StringComparison.OrdinalIgnoreCase))))
                                {
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "isOnlyTOTGCease", Value = "true", action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                                }
                                else
                                {
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "isOnlyTOTGCease", Value = "false", action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                                }


                            }
                        }
                        if (orderItem.Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["WifiProdCode"], StringComparison.OrdinalIgnoreCase)
                            && response.Header.Action == OrderActionEnum.provide)
                        {
                            SaaSNS.Attribute isAHTAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            isAHTAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            isAHTAttribute.Name = "ISAHTDONE";
                            isAHTAttribute.Value = isAHTDone.ToString();
                            roleInstance.Attributes.Add(isAHTAttribute);

                            SaaSNS.Attribute ceaseReason = new BT.SaaS.Core.Shared.Entities.Attribute();
                            ceaseReason.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            ceaseReason.Name = "CEASEREASON";
                            ceaseReason.Value = " ";
                            roleInstance.Attributes.Add(ceaseReason);

                            SaaSNS.Attribute hubStatus = new BT.SaaS.Core.Shared.Entities.Attribute();
                            hubStatus.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            hubStatus.Name = "HUB_WIFI_STATUS";
                            hubStatus.Value = "OPTED_IN";
                            roleInstance.Attributes.Add(hubStatus);

                            wifiVasProductCode = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("VasProductId", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                            string[] BBwifiVasProductCodes = ConfigurationManager.AppSettings["BBWifiVasProductCode"].Split(new char[] { ',' });
                            string[] SpringWifiVasProductCodes = ConfigurationManager.AppSettings["SpringWifiVasProductCode"].Split(new char[] { ',' });

                            if (BBwifiVasProductCodes.Contains(wifiVasProductCode))
                            {
                                SaaSNS.Attribute wifitier = new BT.SaaS.Core.Shared.Entities.Attribute();
                                wifitier.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                wifitier.Name = "WIFI_SERVICE_TIER";
                                wifitier.Value = "10";
                                roleInstance.Attributes.Add(wifitier);
                            }
                            else if (SpringWifiVasProductCodes.Contains(wifiVasProductCode))
                            {
                                SaaSNS.Attribute wifitier = new BT.SaaS.Core.Shared.Entities.Attribute();
                                wifitier.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                wifitier.Name = "WIFI_SERVICE_TIER";
                                wifitier.Value = "20";
                                roleInstance.Attributes.Add(wifitier);
                            }
                            if (request.StandardHeader.ServiceAddressing.From.StartsWith(Settings1.Default.MQstring.ToString()))
                            {
                                SaaSNS.Attribute btOneIDAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                btOneIDAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                btOneIDAttribute.Name = "BtOneId";
                                btOneIDAttribute.Value = BtOneId.ToString();
                                roleInstance.Attributes.Add(btOneIDAttribute);
                            }

                            if (IsNayanWifi)
                            {
                                SaaSNS.Attribute nayanwifi = new BT.SaaS.Core.Shared.Entities.Attribute();
                                nayanwifi.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                nayanwifi.Name = "ISNAYANWIFI";
                                nayanwifi.Value = IsNayanWifi.ToString();
                                roleInstance.Attributes.Add(nayanwifi);
                            }
                            //NAYANAGL - 23775
                            if (IsWifiReactivate)
                            {
                                SaaSNS.Attribute wifiexist = new BT.SaaS.Core.Shared.Entities.Attribute();
                                wifiexist.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                wifiexist.Name = "ISWIFIREACTIVATE";
                                wifiexist.Value = IsWifiReactivate.ToString();
                                roleInstance.Attributes.Add(wifiexist);
                            }
                            if (IsBBFlagExist)
                            {
                                SaaSNS.Attribute BBflagexist = new BT.SaaS.Core.Shared.Entities.Attribute();
                                BBflagexist.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                BBflagexist.Name = "isBBFlagExists";
                                BBflagexist.Value = "Y";
                                roleInstance.Attributes.Add(BBflagexist);
                            }
                            if (IsWifiRoleInactive)
                            {
                                SaaSNS.Attribute wifiroleinactive = new BT.SaaS.Core.Shared.Entities.Attribute();
                                wifiroleinactive.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                wifiroleinactive.Name = "ISWIFIROLEINACTIVE";
                                wifiroleinactive.Value = IsWifiRoleInactive.ToString();
                                roleInstance.Attributes.Add(wifiroleinactive);
                            }

                        }
                        if (orderItem.Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["VASContentAnyWhereProductFamily"], StringComparison.OrdinalIgnoreCase) && response.Header.Action == OrderActionEnum.modifyService)
                        {
                            if (request.Order.OrderItem[0].Action.Code.ToLower() == "cease")
                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "REASON", Value = "CEASE_CA", action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                            else
                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "REASON", Value = orderItem.Action.Reason, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });

                            if (roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("CCID")))
                            {
                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "CUSTOMERCONTEXTID", Value = roleInstance.Attributes.ToList().Where(attr => attr.Name.Equals("CCID")).FirstOrDefault().Value });
                                roleInstance.Attributes.Remove(roleInstance.Attributes.ToList().Where(attr => attr.Name.Equals("CCID")).First());
                            }
                        }
                        if (isVASReactivate)
                        {
                            if (!roleInstance.Attributes.ToList().Exists(ri => ri.Name.Equals("ISVASREACTIVATE", StringComparison.OrdinalIgnoreCase)))
                            {
                                SaaSNS.Attribute isVASReactivateAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                isVASReactivateAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                isVASReactivateAttribute.Name = "ISVASREACTIVATE";
                                isVASReactivateAttribute.Value = isVASReactivate.ToString();
                                roleInstance.Attributes.Add(isVASReactivateAttribute);
                            }
                            if (isContentAnyWhere)
                            {
                                SaaSNS.Attribute subscriptionExternalReferenceAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                subscriptionExternalReferenceAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                subscriptionExternalReferenceAttribute.Name = "SUBSCRIPTIONEXTERNALREFERENCE";
                                subscriptionExternalReferenceAttribute.Value = subscriptionExternalReference.ToString();
                                roleInstance.Attributes.Add(subscriptionExternalReferenceAttribute);

                                SaaSNS.Attribute supplierCodeAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                supplierCodeAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                supplierCodeAttribute.Name = "PREVIOUSSUPPLIERCODE";
                                supplierCodeAttribute.Value = previousSupplierCode.ToString();
                                roleInstance.Attributes.Add(supplierCodeAttribute);

                                if (roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("CCID")))
                                {
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "CUSTOMERCONTEXTID", Value = roleInstance.Attributes.ToList().Where(attr => attr.Name.Equals("CCID")).FirstOrDefault().Value });
                                    roleInstance.Attributes.Remove(roleInstance.Attributes.ToList().Where(attr => attr.Name.Equals("CCID")).First());
                                }

                            }
                            //if (isContentFilter)
                            //{
                            //    SaaSNS.Attribute OldrbSidListAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
                            //    OldrbSidListAttr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            //    OldrbSidListAttr.Name = "OLDRBSIDLIST";
                            //    OldrbSidListAttr.Value = string.Join(",", OldrbsidList.ToArray());
                            //    roleInstance.Attributes.Add(OldrbSidListAttr);

                            //    SaaSNS.Attribute DisabledProfile = new BT.SaaS.Core.Shared.Entities.Attribute();
                            //    DisabledProfile.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            //    DisabledProfile.Name = "IsDisabledProfile";
                            //    DisabledProfile.Value = isDisabledProfile.ToString();
                            //    roleInstance.Attributes.Add(DisabledProfile);
                            //}
                        }

                        if ((response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.provide || response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.amend) && request.Order.OrderItem.ToList().Exists(s => s.Specification.ToList().Exists(i => i.Identifier.Value1.Equals(ConfigurationManager.AppSettings["VASContentAnyWhereProductFamily"].ToString(), StringComparison.OrdinalIgnoreCase))))
                        {
                            foreach (MSEO.OrderItem orderitem in request.Order.OrderItem)
                            {
                                SaaSNS.Attribute supplierCodeAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                supplierCodeAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                supplierCodeAttribute.Name = "PREVIOUSSUPPLIERCODE";
                                supplierCodeAttribute.Value = orderitem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("suppliercode")).FirstOrDefault().Value;
                                roleInstance.Attributes.Add(supplierCodeAttribute);
                            }
                        }

                        if (orderItem.Specification[0].Identifier.Value1.Equals("contentfiltering", StringComparison.OrdinalIgnoreCase) || orderItem.Specification[0].Identifier.Value1.Equals("btwifi", StringComparison.OrdinalIgnoreCase))
                        {
                            if (request.StandardHeader.ServiceAddressing.From.Equals(Settings1.Default.BTComstring, StringComparison.OrdinalIgnoreCase))
                            {
                                SaaSNS.Attribute orderFromAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                orderFromAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                orderFromAttirbute.Name = "ORDERFROM";
                                orderFromAttirbute.Value = Settings1.Default.BTComstring;
                                roleInstance.Attributes.Add(orderFromAttirbute);
                            }
                            if (response.Header.Action == OrderActionEnum.reActivate)
                            {
                                SaaSNS.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                reasonAttirbute.Name = "REASON";
                                reasonAttirbute.Value = OrderActionEnum.reActivate.ToString();
                                roleInstance.Attributes.Add(reasonAttirbute);
                            }
                            else if ((response.Header.Action == OrderActionEnum.cease || response.Header.Action == OrderActionEnum.modifyService) && !String.IsNullOrEmpty(orderItem.Action.Reason) && orderItem.Action.Reason.ToString().Equals("opt-out", StringComparison.OrdinalIgnoreCase))
                            {
                                SaaSNS.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                reasonAttirbute.Name = "REASON";
                                reasonAttirbute.Value = "suspend";
                                roleInstance.Attributes.Add(reasonAttirbute);
                            }
                        }
                        //ccp 78
                        if (orderItem.Specification[0].Identifier.Value1.Equals("safesearch", StringComparison.OrdinalIgnoreCase))
                        {
                            if (request.StandardHeader.ServiceAddressing.From.Equals(Settings1.Default.BTComstring, StringComparison.OrdinalIgnoreCase))
                            {
                                SaaSNS.Attribute orderFromAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                orderFromAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                orderFromAttirbute.Name = "ORDERFROM";
                                orderFromAttirbute.Value = Settings1.Default.BTComstring;
                                roleInstance.Attributes.Add(orderFromAttirbute);
                            }
                            
                            else if ((response.Header.Action == OrderActionEnum.cease || response.Header.Action == OrderActionEnum.modifyService) && !String.IsNullOrEmpty(orderItem.Action.Reason) && orderItem.Action.Reason.ToString().Equals("opt-out", StringComparison.OrdinalIgnoreCase))
                            {
                                SaaSNS.Attribute reasonAttirbute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                reasonAttirbute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                reasonAttirbute.Name = "REASON";
                                reasonAttirbute.Value = "suspend";
                                roleInstance.Attributes.Add(reasonAttirbute);
                            }
                        }
                        SaaSNS.Attribute BptmE2EDataAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                        BptmE2EDataAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        BptmE2EDataAttribute.Name = "E2EDATA";
                        if (request.StandardHeader != null && request.StandardHeader.E2e != null && request.StandardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(request.StandardHeader.E2e.E2EDATA.ToString()))
                            BptmE2EDataAttribute.Value = request.StandardHeader.E2e.E2EDATA.ToString();
                        else
                            BptmE2EDataAttribute.Value = string.Empty;
                        roleInstance.Attributes.Add(BptmE2EDataAttribute);

                        serviceInstance.ServiceRoles.Add(serviceRole);
                        productOrderItem.ServiceInstances.Add(serviceInstance);
                        productOrderItem.RoleInstances.Add(roleInstance);
                        productorderItemCount++;

                        if (response.Header.Action == OrderActionEnum.provide || response.Header.Action == OrderActionEnum.cease)
                        {
                            foreach (ProductDefinitionRole role in productDefinition[0].ProductDefinitionRoleList)
                            {
                                foreach (ProductDefinitionAttribute attribute in role.ProductDefinitionAttributeList)
                                {
                                    if (attribute.Name.Trim().Equals("SERVICETYPE", StringComparison.OrdinalIgnoreCase) &&
                                       !string.IsNullOrEmpty(attribute.Default) && attribute.Default.Trim().Equals("Chrysalis Subscription", StringComparison.OrdinalIgnoreCase))
                                    {
                                        isOrderingRequired = true;
                                        orderingProductCode = productOrderItem.Header.ProductCode;
                                        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("orderingProductCode :-" + orderingProductCode + "isOrderingRequired: " + isOrderingRequired.ToString());
                                        break;
                                    }
                                }
                            }
                        }

                        // This is only for BTCOM Gaurd Profiles,CA-VAS profiles. This logic will be removed once BT com sends the correct action.
                        if (response.Header.Action == OrderActionEnum.modifyService && productOrderItem.Header.ProductCode != ConfigurationManager.AppSettings["DanteEMailProdCode"].ToString())
                        {
                            string[] gaurdProducts = ConfigurationManager.AppSettings["GaurdProducts"].Split(new char[] { ';' });
                            if (gaurdProducts.Contains(productOrderItem.Header.ProductCode))
                            {
                                response.Header.Action = OrderActionEnum.provide;
                            }
                        }
                        response.ProductOrderItems.Add(productOrderItem);
                    }
                }

                if (isOrderingRequired && !String.IsNullOrEmpty(orderingProductCode))
                {
                    ProductOrderItem ChrysalisSubscription = response.ProductOrderItems.Find(p => p.Header.ProductCode.Equals(orderingProductCode, StringComparison.OrdinalIgnoreCase));

                    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("In condition : orderingProductCode :-" + orderingProductCode + "isOrderingRequired: " + isOrderingRequired.ToString());
                    if (ChrysalisSubscription != null)
                    {
                        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Entered into if loop (ChrysalisSubscription != null)");
                        if (response.Header.Action == OrderActionEnum.provide && !response.ProductOrderItems[0].Header.ProductCode.Equals(orderingProductCode, StringComparison.OrdinalIgnoreCase))
                        {
                            response.ProductOrderItems.Remove(ChrysalisSubscription);
                            response.ProductOrderItems.Insert(0, ChrysalisSubscription);
                            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Product Order Item Sequence check " + response.ProductOrderItems[0].Header.ProductCode);
                        }
                        else if (response.Header.Action == OrderActionEnum.cease)
                        {
                            response.ProductOrderItems.Remove(ChrysalisSubscription);
                            response.ProductOrderItems.Add(ChrysalisSubscription);
                            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Product Order Item Sequence check " + response.ProductOrderItems[response.ProductOrderItems.Count - 1].Header.ProductCode);
                        }
                    }
                }
            }

            catch (DnpException dnpException)
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter Mapping DnP Exception : " + dnpException.ToString());
                throw dnpException;
            }
            catch (Exception ex)
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter Mapping Exception : " + ex.ToString());
                throw ex;
            }
            return response;
        }

        public static void AddorUpdateProductAttributes(string attrID, string attrValue, ref SaaSNS.RoleInstance roleInstance, BT.SaaS.Core.Shared.Entities.DataActionEnum action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add)
        {
            if (action.Equals(BT.SaaS.Core.Shared.Entities.DataActionEnum.add))
            {
                SaaSNS.Attribute attr = new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = attrID, Value = attrValue };
                roleInstance.Attributes.Add(attr);
            }
            else
            {
                if (roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals(attrID, StringComparison.OrdinalIgnoreCase)))
                {   
                    roleInstance.Attributes.ToList().Where(attr => attr.Name.Equals(attrID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = attrValue;
                }
                else
                {
                    SaaSNS.Attribute attr = new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = attrID, Value = attrValue };
                    roleInstance.Attributes.Add(attr);
                }
            }
        }


        public static void MapResponse(SaaSNS.Order orderResponse, ref MSEO.OrderRequest orderNotification)
        {
            try
            {
                foreach (MSEO.OrderItem oNpoi in orderNotification.Order.OrderItem)
                {

                    ProductOrderItem ispresentpoi = orderResponse.ProductOrderItems.Where(oi => oi.Header.OrderItemKey.Equals(oNpoi.Identifier.Id, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault();

                    if (ispresentpoi == null)
                    {
                        oNpoi.Status = Settings1.Default.IgnoredStatus;
                    }
                }

                foreach (ProductOrderItem productOrderItem in orderResponse.ProductOrderItems)
                {

                    MSEO.OrderItem currentOrderItem = null;
                    currentOrderItem = orderNotification.Order.OrderItem.Where(oi => oi.Identifier.Id.Equals(productOrderItem.Header.OrderItemKey, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault();

                    currentOrderItem.HoldToTerm = null;
                    currentOrderItem.Status = (productOrderItem.Header.Status == DataStatusEnum.done ? Settings1.Default.CompleteStatus : Settings1.Default.FailedStatus);

                    currentOrderItem.Error = new Error();
                    currentOrderItem.Error.Code = (productOrderItem.Header.Status == DataStatusEnum.done ? "0" : "3");
                    currentOrderItem.Error.Description = (productOrderItem.Header.Status == DataStatusEnum.done ? "Complete" : "Failed");

                    if (productOrderItem.Header.ProductCode == "GuardProduct")
                    {
                        InstanceCharacteristic[] instanceChar = new InstanceCharacteristic[2];
                        for (int i = 0; i < 2; i++)
                        {
                            instanceChar[i] = null;
                        }
                        SaaSNS.ServiceActionOrderItem serviceOrderItem = findServiceOrderItem("DNP", productOrderItem, orderResponse, ServiceActionEnum.modifyClient);

                        if (serviceOrderItem != null)
                        {
                            foreach (SaaSNS.Attribute atrribute in serviceOrderItem.ServiceRoles[0].roleAttributes)
                            {
                                if (atrribute.Name.ToUpper() == "SUBSCRIPTIONID")
                                {
                                    instanceChar[0] = new InstanceCharacteristic();
                                    instanceChar[0].Name = "SubscriptionId";
                                    instanceChar[0].Value = atrribute.Value;
                                }
                                if (atrribute.Name.ToUpper() == "ACTIVATIONPIN")
                                {

                                    instanceChar[1] = new InstanceCharacteristic();
                                    instanceChar[1].Name = "ActivationPin";
                                    instanceChar[1].Value = atrribute.Value;
                                }
                            }
                        }
                        else
                        {
                            //Added for Guard Product Cease
                            serviceOrderItem = findServiceOrderItem("DNP", productOrderItem, orderResponse, ServiceActionEnum.deleteClient);
                            if (serviceOrderItem != null)
                            {
                                foreach (SaaSNS.Attribute atrribute in serviceOrderItem.ServiceRoles[0].roleAttributes)
                                {
                                    if (atrribute.Name.ToUpper() == "SUBSCRIPTIONIDSUCCESS")
                                    {
                                        instanceChar[0] = new InstanceCharacteristic();
                                        instanceChar[0].Name = "SubscriptionIdSuccess";
                                        instanceChar[0].Value = atrribute.Value;
                                    }
                                }
                            }
                        }

                        currentOrderItem.Instance[0].InstanceCharacteristic = instanceChar;
                    }

                    else if (productOrderItem.Header.ProductCode.Equals("NETPROTECT", StringComparison.InvariantCultureIgnoreCase)
                           || productOrderItem.Header.ProductCode.Equals("S0162974-NPPLUS1", StringComparison.InvariantCultureIgnoreCase)
                        )
                    {
                        InstanceCharacteristic[] instanceChar = new InstanceCharacteristic[3];
                        for (int i = 0; i < 3; i++)
                        {
                            instanceChar[i] = null;
                        }
                        SaaSNS.ServiceActionOrderItem serviceOrderItem = findServiceOrderItem("DNP", productOrderItem, orderResponse);
                        if (serviceOrderItem != null)
                        {
                            foreach (SaaSNS.Attribute atrribute in serviceOrderItem.ServiceRoles[0].roleAttributes)
                            {
                                if (atrribute.Name.ToUpper() == "SUBSCRIPTIONLIST")
                                {
                                    instanceChar[0] = new InstanceCharacteristic();
                                    instanceChar[0].Name = "SubscriptionId";
                                    instanceChar[0].Value = atrribute.Value;
                                }
                                if (atrribute.Name.ToUpper() == "NEWSUBSCRIPTIONLIST")
                                {
                                    instanceChar[1] = new InstanceCharacteristic();
                                    instanceChar[1].Name = "SubscriptionId";
                                    instanceChar[1].Value = atrribute.Value;
                                }
                                if (atrribute.Name.ToUpper() == "USERSUBSCRIPTIONPIN")
                                {

                                    instanceChar[2] = new InstanceCharacteristic();
                                    instanceChar[2].Name = "usersubscriptionpin";
                                    instanceChar[2].Value = atrribute.Value;
                                }


                            }
                        }
                        currentOrderItem.Instance[0].InstanceCharacteristic = instanceChar;

                    }

                    else if (productOrderItem.Header.ProductCode.Equals("DigitalVault", StringComparison.InvariantCultureIgnoreCase)
                        || productOrderItem.Header.ProductCode.Equals("S0162975", StringComparison.InvariantCultureIgnoreCase)
                        || productOrderItem.Header.ProductCode.Equals("S0162978", StringComparison.InvariantCultureIgnoreCase))
                    {
                        InstanceCharacteristic[] instanceChar = new InstanceCharacteristic[1];
                        instanceChar[0] = null;
                        SaaSNS.ServiceActionOrderItem serviceOrderItem = findServiceOrderItem("DNP", productOrderItem, orderResponse);
                        if (serviceOrderItem != null)
                        {
                            foreach (SaaSNS.Attribute atrribute in serviceOrderItem.ServiceRoles[0].roleAttributes)
                            {
                                if (atrribute.Name.ToUpper() == "DVUSERNAME")
                                {
                                    instanceChar[0] = new InstanceCharacteristic();
                                    instanceChar[0].Name = "ServiceId";
                                    instanceChar[0].Value = atrribute.Value;
                                }
                            }
                        }
                        currentOrderItem.Instance[0].InstanceCharacteristic = instanceChar;
                    }
                }

                //BPTM implementation(including THOMAS)
                if (orderResponse.ServiceActionOrderItems.ToList().Where(saoi => saoi.Header.Status.ToString().Equals("done", StringComparison.OrdinalIgnoreCase) || saoi.Header.Status.ToString().Equals("failed", StringComparison.OrdinalIgnoreCase)).LastOrDefault().ServiceRoles[0].roleAttributes.ToList().Exists(ra => ra.Name.Equals("E2EDATA", StringComparison.OrdinalIgnoreCase) && !String.IsNullOrEmpty(ra.Value)))
                {
                    orderNotification.StandardHeader.E2e.E2EDATA = orderResponse.ServiceActionOrderItems.ToList().Where(saoi => saoi.Header.Status.ToString().Equals("done", StringComparison.OrdinalIgnoreCase) || saoi.Header.Status.ToString().Equals("failed", StringComparison.OrdinalIgnoreCase)).LastOrDefault().ServiceRoles[0].roleAttributes.ToList().Where(ra => ra.Name.Equals("E2EDATA", StringComparison.OrdinalIgnoreCase) && !String.IsNullOrEmpty(ra.Value)).FirstOrDefault().Value;
                }
            }
            catch { }
        }

        private static ServiceActionOrderItem findServiceOrderItem(string serviceCode, ProductOrderItem productOrderItem, SaaSNS.Order order)
        {
            foreach (ServiceActionOrderItem saoi in order.ServiceActionOrderItems)
            {
                if (saoi.Header.ServiceCode.Equals(serviceCode, StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (SequencedServiceAction item in productOrderItem.SequencedServiceActions)
                    {
                        if (item.ServiceActionOrderItemXmlRef == saoi.XmlIdKey)
                        {
                            return saoi;
                        }
                    }
                }
            }
            return null;
        }

        internal static bool convertDateTime(string input, ref System.DateTime dateTime)
        {
            dateTime = System.DateTime.Now;
            try
            {
                System.DateTime validObj = new System.DateTime();
                try
                {
                    System.DateTime winEpoch = new System.DateTime(1970, 01, 01);
                    long epochTime = (Convert.ToInt64(input) * 10000000) + winEpoch.Ticks;
                    validObj = new System.DateTime(epochTime).ToLocalTime();
                    dateTime = validObj;
                }
                catch
                {
                    validObj = System.DateTime.Parse(input);
                    dateTime = validObj;
                }
                if (validObj != null)
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        private static ServiceActionOrderItem findServiceOrderItem(string serviceCode, ProductOrderItem productOrderItem, SaaSNS.Order order, ServiceActionEnum serviceAction)
        {
            foreach (ServiceActionOrderItem saoi in order.ServiceActionOrderItems)
            {
                if (saoi.Header.ServiceCode.Equals(serviceCode, StringComparison.InvariantCultureIgnoreCase) && saoi.Header.ServiceAction == serviceAction)
                {
                    foreach (SequencedServiceAction item in productOrderItem.SequencedServiceActions)
                    {
                        if (item.ServiceActionOrderItemXmlRef == saoi.XmlIdKey)
                        {
                            return saoi;
                        }
                    }
                }
            }
            return null;
        }


        public static SaaSNS.Order MapCeaseRequest(MSEO.OrderRequest request, ref E2ETransaction e2eData)
        {
            string emailDataStore = string.Empty;
            string emailSupplier = "MX"; string vasproductid = string.Empty;
            string orderType = string.Empty;
            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Mapping VAS Cease the request");
            SaaSNS.Order response = new BT.SaaS.Core.Shared.Entities.Order();
            System.DateTime orderDate = new System.DateTime();
            string orderingProductCode = string.Empty;

            List<string> vasClassList = new List<string>();
            ClientServiceInstanceV1[] serviceInstances = null;
            List<string> CMPSCodes = null;
            List<ProductVasClass> productVasClassList = null;
            List<string> ListOfEmails = null;
            Dictionary<string, string> supplierList = new Dictionary<string, string>();
            bool NominumSubscription = false, isMcAfeeCeaseModify = false, isEmailCeaseModify = false, isCloudCeaseModify = false;
            string UpdateCFSIDatRBSC = "false";
            try
            {
                int productorderItemCount = 0;
                response.Header.CeaseReason = request.SerializeObject();

                response.Header.OrderKey = request.Order.OrderIdentifier.Value;
                response.Header.ServiceProviderID = Settings1.Default.ConsumerServiceProviderId;

                if (request.Order.OrderItem[0].RequiredDateTime != null && request.Order.OrderItem[0].RequiredDateTime.DateTime1 != null)
                {
                    if (convertDateTime(request.Order.OrderItem[0].RequiredDateTime.DateTime1, ref orderDate))
                    {
                        response.Header.EffectiveDateTime = orderDate;
                    }
                    else
                    {
                        response.Header.EffectiveDateTime = System.DateTime.Now;
                    }
                }
                else
                {
                    response.Header.EffectiveDateTime = System.DateTime.Now;
                }
                response.Header.OrderDateTime = System.DateTime.Now;

                response.Header.Status = BT.SaaS.Core.Shared.Entities.OrderStatusEnum.@new;

                response.Header.Customer = new BT.SaaS.Core.Shared.Entities.Customer();
                response.Header.Customer.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.done;

                response.Header.Users = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.User>(1);
                SaaSNS.User user = new BT.SaaS.Core.Shared.Entities.User();
                user.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.done;
                response.Header.Users.Add(user);

                if (request.Order.OrderItem[0].Action.Code.ToLower().Equals("cancel", StringComparison.OrdinalIgnoreCase))
                {
                    response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.cease;
                }

                response.ProductOrderItems = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ProductOrderItem>(request.Order.OrderItem.Length);

                // Need to make a call to DnP for what are  all the products activated in the Dnp,
                // refine the products which are in active send those products into MSEO to prepare ProductOrderItem

                foreach (MSEO.OrderItem orderItem in request.Order.OrderItem)
                {
                    string billAccountID = string.Empty;
                    string vasClassId = string.Empty;

                    billAccountID = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(a => a.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                    vasClassId = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(a => a.Name.Equals("vasclass", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                    vasClassList.Add(vasClassId);

                    //ccp 78 - defect 281437
                    if (!string.IsNullOrEmpty(billAccountID) && (vasClassId.Equals("1430") || vasClassId.Equals("1460")) && !IsEmailCeaseRequired(billAccountID, vasClassId))
                        throw new DnpException("BAC doesn't have Premium or Basic Email service to cease it");

                    productVasClassList = MSEOSaaSMapper.FindAllActiveInactiveProductVasClass(vasClassList, billAccountID, "VAS_BILLINGACCOUNT_ID", "ACTIVE", ref serviceInstances, ref CMPSCodes, ref e2eData);
                    if (productVasClassList != null && productVasClassList.Count > 0)
                    {
                        //Descision to act on complete nominum subscription or not.
                        IEnumerable<ProductVasClass> nominumserviceslist =
                                   productVasClassList.Where(nomser => nomser.VasProductFamily.Equals(ConfigurationManager.AppSettings["CFProdCode"], StringComparison.OrdinalIgnoreCase)
                                   || nomser.VasProductFamily.Equals(ConfigurationManager.AppSettings["NetworkSecurityProdCode"], StringComparison.OrdinalIgnoreCase));
                        if (!nominumserviceslist.Any(nom => nom.Notes != null && nom.Notes.ToUpper().Equals("MODIFY", StringComparison.OrdinalIgnoreCase)))
                        {
                            NominumSubscription = true;
                            UpdateCFSIDatRBSC = "true";
                        }
                        foreach (ProductVasClass productVasClass in productVasClassList)
                        {
                            if (productVasClass.Notes != null && productVasClass.Notes.Equals("modify", StringComparison.OrdinalIgnoreCase))
                            {
                                //preparing product order items only for Wifi, Email and McAfee MMA in case of modification
                                if (ConfigurationManager.AppSettings["CeaseMofifyServicesList"].Split(',').Contains(productVasClass.VasProductFamily + ":" + productVasClass.vasSubType, StringComparer.OrdinalIgnoreCase))
                                {
                                    //Modify McAfee MMA service only when there is a change in Activation cardinality(check based on vas_product_id)
                                    if (ConfigurationManager.AppSettings["MMAService"].Equals(productVasClass.VasProductFamily + ":" + productVasClass.vasSubType, StringComparison.OrdinalIgnoreCase))
                                    {
                                        string vas_product_id = serviceInstances.ToList().Where(inst => inst.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MMAService"], StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceCharacteristic.ToList().Where(ins_char => ins_char.name.Equals("vas_product_id", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        if (vas_product_id.Equals(productVasClass.VasProductId, StringComparison.OrdinalIgnoreCase))
                                            continue;
                                        else
                                            isMcAfeeCeaseModify = true;
                                    }
                                    else if (ConfigurationManager.AppSettings["DanteIdentifierInMDM"].Equals(productVasClass.VasProductFamily + ":" + productVasClass.vasSubType, StringComparison.OrdinalIgnoreCase))
                                    {
                                        string vas_product_id = serviceInstances.ToList().Where(inst => inst.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceCharacteristic.ToList().Where(ins_char => ins_char.name.Equals("vas_product_id", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        if (vas_product_id.Equals(productVasClass.VasProductId, StringComparison.OrdinalIgnoreCase))
                                            continue;
                                        else
                                            isEmailCeaseModify = true; 
                                    }
                                    else if (ConfigurationManager.AppSettings["CAService"].Equals(productVasClass.VasProductFamily + ":" + productVasClass.vasSubType, StringComparison.OrdinalIgnoreCase))                                       
                                    {
                                        //Order stuck fix 12-09-2024
                                        if (serviceInstances.ToList().Where(inst => inst.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CAService"], StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceCharacteristic != null)
                                        {
                                            if (!serviceInstances.ToList().Where(inst => inst.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CAService"], StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceCharacteristic.ToList().Where(ins_char => ins_char.name.Equals("prev_supplier_code", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value.Equals(productVasClass.SupplierCode, StringComparison.OrdinalIgnoreCase))
                                                isCloudCeaseModify = true;
                                        }
                                        else
                                            continue;
                                    }
                                }
                                else if(ConfigurationManager.AppSettings["DanteIdentifierInMDM"].Equals(productVasClass.VasProductFamily + ":" + productVasClass.vasSubType, StringComparison.OrdinalIgnoreCase) && productVasClass.VasProductName.Equals("Standard Email", StringComparison.OrdinalIgnoreCase))
                                {
                                    string vas_product_id = serviceInstances.ToList().Where(inst => inst.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceCharacteristic.ToList().Where(ins_char => ins_char.name.Equals("vas_product_id", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                    if (vas_product_id.Equals(productVasClass.VasProductId, StringComparison.OrdinalIgnoreCase))
                                        continue;
                                    else
                                        isEmailCeaseModify = true;
                                }
                                // Do Nothing for the remaining services.
                                else
                                    continue;
                            }
                            if(productVasClass.VasProductFamily.Equals(ConfigurationManager.AppSettings["VASContentAnyWhereProductFamily"], StringComparison.OrdinalIgnoreCase) && !productVasClass.ProductType.Equals(ConfigurationManager.AppSettings["BundledProductType"], StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            else  if (productVasClass.VasProductFamily.Equals(ConfigurationManager.AppSettings["VASContentAnyWhereProductFamily"], StringComparison.OrdinalIgnoreCase) && !isCloudCeaseModify)
                            {
                                //Ignore CA cease if there's a diff in supplier quotas
                                //Stuck order fix

                                if (serviceInstances != null && serviceInstances.ToList().Exists(inst => inst.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CAService"], StringComparison.OrdinalIgnoreCase) && inst.clientServiceInstanceCharacteristic != null && inst.clientServiceInstanceCharacteristic.ToList().Exists(ins_char => ins_char.name.Equals("prev_supplier_code", StringComparison.OrdinalIgnoreCase))))
                                {

                                    string active_suppliercode = serviceInstances.ToList().Where(inst => inst.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CAService"], StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientServiceInstanceCharacteristic.ToList().Where(ins_char => ins_char.name.Equals("prev_supplier_code", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                    if (!(active_suppliercode.Equals(productVasClass.SupplierCode, StringComparison.OrdinalIgnoreCase)))
                                        continue;
                                }
                            }
                            else if (productVasClass.VasProductFamily.Equals(ConfigurationManager.AppSettings["WifiProdCode"], StringComparison.OrdinalIgnoreCase))
                            {
                                if (!ConfigurationManager.AppSettings["WifiSwitch"].Equals("ON", StringComparison.OrdinalIgnoreCase))
                                    continue;
                            }

                            SaaSNS.ProductOrderItem productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
                            productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
                            productOrderItem.Header.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.notDone;
                            productOrderItem.Header.Quantity = "1";
                            productOrderItem.XmlIdKey = "PO-" + productorderItemCount.ToString();

                            productOrderItem.Header.OrderItemKey = orderItem.Identifier.Id;
                            productOrderItem.Header.ProductCode = productVasClass.VasProductFamily;


                            System.Collections.Generic.List<string> inputProductDefinitionList = new System.Collections.Generic.List<string>();
                            inputProductDefinitionList.Add(productOrderItem.Header.ProductCode);

                            System.Collections.Generic.List<ProductDefinition> productDefinition = MdmWrapper.getSaaSProductDefs(inputProductDefinitionList, Settings1.Default.ConsumerServiceProviderId);

                            if (productDefinition.Count == 0)
                            {
                                orderItem.Status = Settings1.Default.IgnoredStatus;
                                Logger.Debug("Product Code not found in Order : " + request.Order.OrderIdentifier.Value + " for order item " + orderItem.Identifier.Id);
                            }
                            else
                            {
                                productOrderItem.RoleInstances = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.RoleInstance>(1);
                                productOrderItem.ServiceInstances = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ServiceInstance>();
                                SaaSNS.RoleInstance roleInstance = new BT.SaaS.Core.Shared.Entities.RoleInstance();

                                roleInstance = new BT.SaaS.Core.Shared.Entities.RoleInstance();
                                roleInstance.Action = BT.SaaS.Core.Shared.Entities.RoleInstanceActionEnum.add;
                                roleInstance.RoleType = "ADMIN";
                                roleInstance.XmlIdKey = "RI-" + productorderItemCount.ToString();


                                SaaSNS.ServiceInstance serviceInstance = new BT.SaaS.Core.Shared.Entities.ServiceInstance();
                                serviceInstance.ServiceRoles = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ServiceRole>();
                                SaaSNS.ServiceRole serviceRole = new BT.SaaS.Core.Shared.Entities.ServiceRole();
                                serviceRole.roleAttributes = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.Attribute>();

                                foreach (MSEO.Instance instance in orderItem.Instance)
                                {
                                    foreach (MSEO.InstanceCharacteristic instanceCharacteristic in instance.InstanceCharacteristic)
                                    {
                                        SaaSNS.Attribute attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attribute.Name = instanceCharacteristic.Name;
                                        attribute.Value = instanceCharacteristic.Value;
                                        roleInstance.Attributes.Add(attribute);
                                    }
                                }

                                string[] vasContentAnyWhereProductFamily = ConfigurationManager.AppSettings["VASContentAnyWhereProductFamily"].Split(new char[] { ';' });

                                SaaSNS.Attribute attr = null;
                                string customerContextID = string.Empty;
                                string subscriptionID = string.Empty;
                                string subscriptionExternalReference = string.Empty;
                                string rbsidList = string.Empty;
                                string cfsid = string.Empty;

                                if (serviceInstances != null)
                                {
                                    foreach (BT.SaaS.IspssAdapter.Dnp.ClientServiceInstanceV1 dnpserviceInstance in serviceInstances)
                                    {
                                        if (dnpserviceInstance.clientServiceInstanceStatus.value.Equals("active", StringComparison.OrdinalIgnoreCase) && dnpserviceInstance.clientServiceInstanceIdentifier.value.Equals(productVasClass.VasProductFamily + ":" + productVasClass.vasSubType, StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (!(new string[] { ConfigurationManager.AppSettings["DanteIdentifier"].ToString(), ConfigurationManager.AppSettings["WifiIdentifier"].ToString() }.Contains(dnpserviceInstance.clientServiceInstanceIdentifier.value.ToUpper())))
                                            {
                                                customerContextID = dnpserviceInstance.name.ToString();
                                                if (dnpserviceInstance.clientServiceInstanceCharacteristic != null)
                                                {
                                                    if (dnpserviceInstance.clientServiceInstanceCharacteristic.ToList().Exists(insChar => insChar.name.Equals("subscription_id", StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        subscriptionID = dnpserviceInstance.clientServiceInstanceCharacteristic.ToList().Where(insChar => insChar.name.Equals("subscription_id", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                    }
                                                    if (dnpserviceInstance.clientServiceInstanceCharacteristic.ToList().Exists(insChar => insChar.name.Equals("subscrption_external", StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        subscriptionExternalReference = dnpserviceInstance.clientServiceInstanceCharacteristic.ToList().Where(insChar => insChar.name.Equals("subscrption_external", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                    }
                                                    if (dnpserviceInstance.clientServiceInstanceCharacteristic.ToList().Exists(insChar => insChar.name.Equals("cfsid", StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        cfsid = dnpserviceInstance.clientServiceInstanceCharacteristic.ToList().Where(insChar => insChar.name.Equals("cfsid", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                    }
                                                }
                                                if (string.IsNullOrEmpty(cfsid) && dnpserviceInstance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"].ToString(), StringComparison.OrdinalIgnoreCase))
                                                {
                                                    cfsid = dnpserviceInstance.serviceIdentity.ToList().Where(si => si.domain.Equals("cfsid", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                }
                                                if (dnpserviceInstance.clientServiceInstanceStatus.value.Equals("active", StringComparison.OrdinalIgnoreCase) && dnpserviceInstance.clientServiceInstanceIdentifier.value.Equals("contentfiltering:default", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    if (dnpserviceInstance.clientServiceRole != null && dnpserviceInstance.clientServiceRole.Length > 0)
                                                    {
                                                        List<ClientServiceRole> rbsidRoles = dnpserviceInstance.clientServiceRole.ToList().Where(role => role.clientServiceRoleStatus.value.Equals("active", StringComparison.OrdinalIgnoreCase) && role.id.Equals("default", StringComparison.OrdinalIgnoreCase)).ToList();
                                                        if (rbsidRoles.Count > 0)
                                                        {
                                                            foreach (ClientServiceRole role in rbsidRoles)
                                                            {
                                                                if (String.IsNullOrEmpty(rbsidList))
                                                                    rbsidList = role.clientIdentity[0].value;
                                                                else
                                                                    rbsidList = rbsidList + "," + role.clientIdentity[0].value;
                                                            }
                                                        }
                                                        //Added RVSID attr to skip RBSC call in Spring PC cease.
                                                        else
                                                        {
                                                            attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                            attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                            attr.Name = "RVSID";
                                                            attr.Value = "dummyRvsid";
                                                            roleInstance.Attributes.Add(attr);
                                                        }
                                                    }
                                                }
                                                if (string.IsNullOrEmpty(rbsidList) && dnpserviceInstance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"].ToString(), StringComparison.OrdinalIgnoreCase))
                                                {
                                                    rbsidList = dnpserviceInstance.clientServiceInstanceCharacteristic.ToList().Where(csi => csi.name.Equals("RBSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                                }
                                            }
                                            if (ConfigurationManager.AppSettings["WifiIdentifier"].ToString().Equals(dnpserviceInstance.clientServiceInstanceIdentifier.value.ToUpper()))
                                            {
                                                if (dnpserviceInstance.clientServiceInstanceCharacteristic.ToList().Exists(insChar => insChar.name.Equals("WIFI_BROADBAND_FLAG", StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                    attr.Name = "isBBFlagExists";
                                                    attr.Value = "Y";
                                                    roleInstance.Attributes.Add(attr);
                                                }

                                                else
                                                {
                                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                    attr.Name = "isBBFlagExists";
                                                    attr.Value = "N";
                                                    roleInstance.Attributes.Add(attr);
                                                }

                                            }
                                        }
                                    }
                                }

                                if (productVasClass.Notes != null && productVasClass.Notes.Equals("disabled", StringComparison.OrdinalIgnoreCase))
                                {
                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "isDisabledProfile";
                                    attr.Value = "TRUE";
                                    roleInstance.Attributes.Add(attr);
                                }

                                attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                attr.Name = "CUSTOMERCONTEXTID";
                                attr.Value = customerContextID;
                                roleInstance.Attributes.Add(attr);

                                attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                attr.Name = "VASSUBTYPE";
                                attr.Value = productVasClass.vasSubType;
                                roleInstance.Attributes.Add(attr);

                                if (CMPSCodes.Count > 0)
                                {
                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "CMPSRESPONSE";
                                    attr.Value = CMPSCodes[0].ToString();
                                    roleInstance.Attributes.Add(attr);
                                }

                                if (productOrderItem.Header.ProductCode.Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"], StringComparison.OrdinalIgnoreCase))
                                {
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "SIBACNUMBER", Value = billAccountID });

                                    if (productVasClass.VasProductName.Equals("Premium Email", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (!isEmailCeaseModify)
                                            orderType = "PREMIUMEMAILCEASE";
                                        else
                                            orderType = "EMAILMODIFYCEASE";
                                        //roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "REASON", Value = "PREMIUMEMAILCEASE" });
                                    }
                                    else
                                    {
                                        if (!isEmailCeaseModify)
                                            orderType = "EMAILCEASE";
                                        else
                                            orderType = "EMAILMODIFYCEASE";

                                    }
                                    if (orderType == "EMAILMODIFYCEASE")
                                    {
                                        string ceasereason = string.Empty;
                                        if (productVasClass.VasClass.Equals("1430", StringComparison.OrdinalIgnoreCase))
                                            ceasereason = "bbtopremium";
                                        else if (productVasClass.VasClass.Equals("1460", StringComparison.OrdinalIgnoreCase))
                                            ceasereason = "bbtobasic";
                                        else
                                            ceasereason = "premiumtobb";
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ceasereason", Value = orderType.ToString() });
                                    }
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "REASON", Value = orderType.ToString() });
                                    ListOfEmails = DanteRequestProcessor.GetListOfEmailsOnCease(billAccountID, string.Empty, "vascease", orderType, ref emailDataStore, ref emailSupplier, ref vasproductid,ref supplierList);
                                    if (emailDataStore.Equals("CISP"))
                                        continue;
                                    if (ListOfEmails[0] != null)
                                    {
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ListOfEmails", Value = ListOfEmails[0] });
                                    }
                                    if (ListOfEmails[1] != null)
                                    {
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ProposedEmailList", Value = ListOfEmails[1] });
                                    }
                                    if (ListOfEmails[2] != null)
                                    {
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "InactiveEmailList", Value = ListOfEmails[2] });
                                    }
                                    if (ListOfEmails[4] != null)
                                    {
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "AffiliateEmailList", Value = ListOfEmails[4] });
                                    }
                                    // Added for BTRCE-108554
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "EMAILSUPPLIER", Value = emailSupplier });
                                    if (!roleInstance.Attributes.ToList().Exists(ic => ic.Name.Equals("VASPRODUCTID", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "VASPRODUCTID", Value = string.IsNullOrEmpty(vasproductid) ? "VPEM000010" : vasproductid });
                                    }
                                    if(supplierList != null && supplierList.Count() > 0)
                                    {
                                        if(supplierList.ContainsKey("MXMailboxes") && !string.IsNullOrEmpty(supplierList["MXMailboxes"]))
                                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "MXMailboxes", Value = supplierList["MXMailboxes"] });
                                        if (supplierList.ContainsKey("Yahoomailboxes") && !string.IsNullOrEmpty(supplierList["Yahoomailboxes"]))
                                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "Yahoomailboxes", Value = supplierList["Yahoomailboxes"] });
                                        if (supplierList.ContainsKey("CPMSMailboxes") && !string.IsNullOrEmpty(supplierList["CPMSMailboxes"]))
                                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "CPMSMailboxes", Value = supplierList["CPMSMailboxes"] });
                                    }
                                }
                                else if (productOrderItem.Header.ProductCode.Equals("contentfiltering", StringComparison.OrdinalIgnoreCase))
                                {
                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "RBSID";
                                    attr.Value = rbsidList;
                                    roleInstance.Attributes.Add(attr);
                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "CFSID";
                                    attr.Value = cfsid;
                                    roleInstance.Attributes.Add(attr);
                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "UpdateCFSIDAtRBSC";
                                    attr.Value = UpdateCFSIDatRBSC;
                                    roleInstance.Attributes.Add(attr);
                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "ActionOnCompleteNominumSubscription";
                                    attr.Value = NominumSubscription.ToString();
                                    roleInstance.Attributes.Add(attr);
                                }

                                else if (productOrderItem.Header.ProductCode.Equals("networksecurity", StringComparison.OrdinalIgnoreCase))
                                {
                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "RBSID";
                                    attr.Value = rbsidList;
                                    roleInstance.Attributes.Add(attr);
                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "CFSID";
                                    attr.Value = cfsid;
                                    roleInstance.Attributes.Add(attr);
                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "UpdateCFSIDAtRBSC";
                                    attr.Value = "False";
                                    roleInstance.Attributes.Add(attr);
                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "ActionOnCompleteNominumSubscription";
                                    attr.Value = "False";
                                    roleInstance.Attributes.Add(attr);
                                }

                                else if (productOrderItem.Header.ProductCode.Equals(ConfigurationManager.AppSettings["WifiProdCode"], StringComparison.OrdinalIgnoreCase))
                                {
                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "CEASEREASON";
                                    attr.Value = request.Order.OrderItem[0].Action.Reason.ToString();
                                    roleInstance.Attributes.Add(attr);

                                    if (productVasClass.Notes != null && productVasClass.Notes.Equals("modify", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (productVasClass.VasServiceTier.Equals(ConfigurationManager.AppSettings["BBWIFIServiceTier"], StringComparison.OrdinalIgnoreCase))
                                        {
                                            attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                            attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                            attr.Name = "VASPRODUCTID";
                                            attr.Value = productVasClass.VasProductId.ToString();
                                            roleInstance.Attributes.Add(attr);

                                            attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                            attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                            attr.Name = "WIFI_SERVICE_TIER";
                                            attr.Value = productVasClass.VasServiceTier.ToString();
                                            roleInstance.Attributes.Add(attr);

                                            attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                            attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                            attr.Name = "WIFI_BROADBAND_FLAG";
                                            attr.Value = "Y";
                                            roleInstance.Attributes.Add(attr);

                                            attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                            attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                            attr.Name = "isCeaseModify";
                                            attr.Value = "TRUE";
                                            roleInstance.Attributes.Add(attr);
                                        }
                                    }

                                    else if (productVasClass.Notes == null)
                                    {
                                        attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attr.Name = "WIFI_BROADBAND_FLAG";
                                        attr.Value = "N";
                                        roleInstance.Attributes.Add(attr);
                                    }

                                }
                                    //add bonded wifi also.

                                //Changes for McAfee MMA
                                else if (productOrderItem.Header.ProductCode.Equals(ConfigurationManager.AppSettings["NPPService"], StringComparison.OrdinalIgnoreCase))
                                {
                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "isCeaseModify";
                                    attr.Value = isMcAfeeCeaseModify.ToString();
                                    roleInstance.Attributes.Add(attr);

                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "LICENSEQTY";
                                    attr.Value = productVasClass.ActivationCardinality.ToString();
                                    roleInstance.Attributes.Add(attr);

                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "VASPRODUCTID";
                                    attr.Value = productVasClass.VasProductId.ToString();
                                    roleInstance.Attributes.Add(attr);
                                }

                                foreach (string contentAnyWhereFamiily in vasContentAnyWhereProductFamily)
                                {
                                    if (productVasClass.VasProductFamily.Equals(contentAnyWhereFamiily, StringComparison.OrdinalIgnoreCase))
                                    {
                                        attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attr.Name = "SUBSCRIPTIONEXTERNALREFERENCE";
                                        attr.Value = subscriptionExternalReference;
                                        roleInstance.Attributes.Add(attr);

                                        attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attr.Name = "PREVIOUSSUPPLIERCODE";
                                        attr.Value = productVasClass.SupplierCode;
                                        roleInstance.Attributes.Add(attr);

                                        attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attr.Name = "VASPRODUCTID";
                                        attr.Value = productVasClass.VasProductId;
                                        roleInstance.Attributes.Add(attr);

                                        attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attr.Name = "isCeaseModify";
                                        attr.Value = isCloudCeaseModify.ToString();
                                        roleInstance.Attributes.Add(attr);

                                    }
                                    else
                                    {
                                        attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attr.Name = "SUBSCRIPTIONID";
                                        attr.Value = subscriptionID;
                                        roleInstance.Attributes.Add(attr);

                                        attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attr.Name = "SUPPLIERCODE";
                                        attr.Value = productVasClass.SupplierCode;
                                        roleInstance.Attributes.Add(attr);
                                    }
                                }

                                serviceInstance.ServiceRoles.Add(serviceRole);
                                productOrderItem.ServiceInstances.Add(serviceInstance);
                                productOrderItem.RoleInstances.Add(roleInstance);
                                productOrderItem.RoleInstances[0].Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "E2EDATA", Value = request.StandardHeader.E2e.E2EDATA.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });

                                if (!(response.ProductOrderItems.ToList().Exists(poi => poi.Header.ProductCode.ToString().Equals(productOrderItem.Header.ProductCode) && poi.RoleInstances.FirstOrDefault().Attributes.ToList().Exists(at => at.Name.Equals("VASSUBTYPE") && at.Value.Equals(productVasClass.vasSubType)))))
                                {
                                    productorderItemCount++;
                                    response.ProductOrderItems.Add(productOrderItem);
                                }
                            }
                        }
                    }

                }
                if (productorderItemCount == 0)
                {
                    throw new DnpException("No services exist to cease for the given VAS cease order. SCodes returned from CMPS are " + string.Join(",", CMPSCodes.ToArray()));
                }
            }
            catch (DnpException DnPexception)
            {
                throw DnPexception;
            }
            catch (MdmException Mdmexception)
            {
                throw Mdmexception;
            }
            catch (Exception ex)
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter Mapping VAS Cease Exception : " + ex.ToString());
                throw ex;
            }
            finally
            {
                ListOfEmails = null;
                productVasClassList = null;
                serviceInstances = null;
                CMPSCodes = null;
                vasClassList = null;
            }

            return response;
        }

        //ccp 78 - defect 281437
        private static bool IsEmailCeaseRequired(string BAC, string vasClass)
        {
            string vasProductId = string.Empty;
            string ispServiceClass = string.Empty;
            bool isEmailService = false;

            ClientServiceInstanceV1[] serviceInstances = DnpWrapper.getServiceInstanceV1(BAC, "VAS_BILLINGACCOUNT_ID", ConfigurationManager.AppSettings["DanteIdentifier"]);
            if (serviceInstances != null)
            {
                if (serviceInstances[0].clientServiceInstanceStatus.value.Equals("active", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (ClientServiceInstanceV1 srvcInstnc in serviceInstances)
                    {
                        if (srvcInstnc.clientServiceInstanceCharacteristic != null)
                        {
                            if (srvcInstnc.clientServiceInstanceCharacteristic.ToList().Exists(insChar => insChar.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(insChar.value)))
                            {
                                vasProductId = srvcInstnc.clientServiceInstanceCharacteristic.ToList().Where(insChar => insChar.name.Equals("VAS_PRODUCT_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                            }
                            if (srvcInstnc.clientServiceInstanceCharacteristic.ToList().Exists(insChar => insChar.name.Equals("ISP_SERVICE_CLASS", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(insChar.value)))
                            {
                                ispServiceClass = srvcInstnc.clientServiceInstanceCharacteristic.ToList().Where(insChar => insChar.name.Equals("ISP_SERVICE_CLASS", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                            }

                            if (vasClass.Equals("1430") && (vasProductId.Equals("VPEM000030", StringComparison.OrdinalIgnoreCase) || ispServiceClass.Equals("ISP_SC_PM", StringComparison.OrdinalIgnoreCase)))
                                isEmailService = true;
                            else if (vasClass.Equals("1460") && (vasProductId.Equals("VPEM000060", StringComparison.OrdinalIgnoreCase) || ispServiceClass.Equals("ISP_SC_BM", StringComparison.OrdinalIgnoreCase)))
                                isEmailService = true;
                        }
                    }
                }
            }

            return isEmailService;
        }

        #region Regrade

        /// <summary>
        /// MapRegradeRequest: prepare the regrade request using the MSEO.OrderRequest 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// 
        public static SaaSNS.Order MapRegradeRequest(MSEO.OrderRequest request, ref E2ETransaction e2eData)
        {
            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Mapping VAS Regrade the request");
            SaaSNS.Order response = new BT.SaaS.Core.Shared.Entities.Order();
            System.DateTime orderDate = new System.DateTime();

            List<string> orderIdentifierID = new List<string>();
            List<List<string>> mainVasList = new List<List<string>>();
            List<string> CMPSCodes = null;
            ClientServiceInstanceV1[] userServices = null;

            try
            {
                response.Header.CeaseReason = request.SerializeObject();
                response.Header.OrderKey = request.Order.OrderIdentifier.Value;

                //Get the ConsumerServiceProviderId from config

                response.Header.ServiceProviderID = Settings1.Default.ConsumerServiceProviderId;
                //Check if the request is not null and contains atleast once orderitem

                if ((request.Order != null) && (request.Order.OrderItem.Length >= 0))
                {
                    if (request.Order.OrderItem[0].RequiredDateTime != null && request.Order.OrderItem[0].RequiredDateTime.DateTime1 != null)
                    {
                        if (convertDateTime(request.Order.OrderItem[0].RequiredDateTime.DateTime1, ref orderDate))
                        {
                            response.Header.EffectiveDateTime = orderDate;
                        }
                        else
                        {
                            response.Header.EffectiveDateTime = System.DateTime.Now;
                        }
                    }
                    else
                    {
                        response.Header.EffectiveDateTime = System.DateTime.Now;
                    }

                    response.Header.OrderDateTime = System.DateTime.Now;

                    response.Header.Status = BT.SaaS.Core.Shared.Entities.OrderStatusEnum.@new;

                    response.Header.Customer = new BT.SaaS.Core.Shared.Entities.Customer();
                    response.Header.Customer.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.done;

                    response.Header.Users = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.User>(1);
                    SaaSNS.User user = new BT.SaaS.Core.Shared.Entities.User();
                    user.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.done;
                    response.Header.Users.Add(user);

                    response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.vasRegrade;

                    response.ProductOrderItems = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ProductOrderItem>(request.Order.OrderItem.Length);


                    // Call the GetAllvasClassList: to get all the required lists used:
                    mainVasList = GetAllvasClassList(request);

                    if ((mainVasList != null) && (mainVasList.Count > 0))
                    {
                        if ((mainVasList[4] != null) && (mainVasList[4].Count > 0))
                        {
                            orderIdentifierID = mainVasList[4];
                        }
                    }

                    //call to get call the required action mapping on each Product
                    bool isCACeaseSkipped = false;
                    List<ProductVasClass> productVasClassList =
                                                    MSEOSaaSMapper.GetAction("VAS_BILLINGACCOUNT_ID",
                                                    "ACTIVE", request, mainVasList, ref userServices, ref CMPSCodes, ref e2eData, ref isCACeaseSkipped);


                    if ((productVasClassList != null) && (productVasClassList.Count > 0))
                    {
                        //BTR 80925 **Start** Added conditions to identify the type of request i.e either (premium to broadband or broadband to premium )
                        string premiumEmailRegrade = string.Empty;
                        bool isBBWifi = false;
                        if (productVasClassList.Exists(x => x.VasProductFamily.Equals("Email", StringComparison.OrdinalIgnoreCase)))
                        {
                            ProductVasClass emailVasClass = productVasClassList.Where(x => x.VasProductFamily.Equals("Email", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                            if (emailVasClass.VasClass.Equals("1430", StringComparison.OrdinalIgnoreCase))
                                premiumEmailRegrade = "bbtopremium";
                            else if (emailVasClass.VasClass.Equals("1460", StringComparison.OrdinalIgnoreCase))
                                premiumEmailRegrade = "bbtobasic";
                            else
                                premiumEmailRegrade = "premiumtobb";
                        }

                        //if ((mainVasList[1] != null) && (mainVasList[1].Count > 0) && (mainVasList[1].FirstOrDefault().ToString() == PremiumEmailProductCode))
                        //{//mainVasList[1] is for cancelList from GetAllvasClassList method  
                        //    premiumEmailRegrade = "premiumtobb";
                        //}
                        //else if ((mainVasList[2] != null) && (mainVasList[2].Count > 0) && (mainVasList[2].FirstOrDefault().ToString() == PremiumEmailProductCode))
                        //{//mainVasList[2] is for createList from GetAllvasClassList method
                        //    premiumEmailRegrade = "bbtopremium";
                        //}//**end**
                        //else if ((mainVasList[1] != null) && (mainVasList[1].Count > 0) && (mainVasList[1].FirstOrDefault().ToString() == BasicEmailProductCode))
                        //{//mainVasList[1] is for cancelList from GetAllvasClassList method  
                        //    premiumEmailRegrade = "basictobb";
                        //}
                        //else if ((mainVasList[2] != null) && (mainVasList[2].Count > 0) && (mainVasList[2].FirstOrDefault().ToString() == BasicEmailProductCode))
                        //{//mainVasList[2] is for createList from GetAllvasClassList method
                        //    premiumEmailRegrade = "bbtobasic";
                        //}//**end**

                        // Added 

                        if ((mainVasList[1] != null) && (mainVasList[1].Count > 0) && (mainVasList[2] != null) && (mainVasList[2].Count > 0))
                        {
                            foreach (var cancelvasclass in mainVasList[1])
                            {
                                if (ConfigurationManager.AppSettings["BBVasClassList"].Split(',').Contains(cancelvasclass, StringComparer.OrdinalIgnoreCase))
                                {
                                    foreach (var createvasclass in mainVasList[2])
                                    {
                                        if (ConfigurationManager.AppSettings["BBVasClassList"].Split(',').Contains(createvasclass, StringComparer.OrdinalIgnoreCase))
                                        {
                                            isBBWifi = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        response = MSEOSaaSMapper.PrepareProductOrderItems(productVasClassList, request, orderIdentifierID, response, userServices, CMPSCodes, premiumEmailRegrade, isBBWifi);
                    }
                    else
                    {
                        if (isCACeaseSkipped)
                        {
                            //Update the order header to send the response from Submit Order method
                            BT.SaaS.Core.Shared.Entities.Attribute attr = new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "skipcacease", Value = "true" };
                            response.Header.Customer.Attributes.Add(attr);
                        }
                        else
                            throw new MdmException("No Services exist to Upgrade/Downgrade for given VasClasse's");
                    }
                }
            }
            catch (DnpException DnPexception)
            {
                throw DnPexception;
            }
            catch (MdmException Mdmexception)
            {
                throw Mdmexception;
            }
            catch (Exception ex)
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter Mapping VAS Regrade Exception : " + ex.ToString());
                throw ex;
            }
            finally
            {
                //Dispose all the used list and other objects
                orderIdentifierID = null;
                mainVasList = null;
            }
            return response;
        }


        /// <summary>
        /// compare the OrderIdentifierID list and get the IdentifierID 
        /// </summary>
        /// <param name="OrderIdentifierID"></param>
        /// <param name="productVasClass"></param>
        /// <returns></returns>
        public static string GetOrderIdentifierID(List<string> orderIdentifierID, string productVasClass)
        {
            string result = string.Empty;
            string vasClassId = string.Empty;
            string identifierID = string.Empty;

            try
            {
                if ((orderIdentifierID != null) && (orderIdentifierID.Count >= 0))
                {
                    for (int i = 0; i < orderIdentifierID.Count; i++)
                    {
                        /*
                         * Get the vasClassId,IdentifierID from OrderIdentifierID list
                         */
                        vasClassId = orderIdentifierID[i].ToString().Split('_')[1];
                        identifierID = orderIdentifierID[i].ToString().Split('_')[0];

                        if (productVasClass.Equals(vasClassId))
                        {
                            result = identifierID;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter Mapping VAS Regrade Exception : " + ex.ToString());
                throw ex;
            }
            finally
            {
                //Dispose all the used objects
                orderIdentifierID = null;
            }

            return result;
        }

        /// <summary>
        /// Prepare the ProductOrderItems using 
        /// productVasClassList: set all the required parameters
        /// </summary>
        /// <param name="productVasClassList"></param>
        /// <param name="request"></param>
        /// <param name="OrderIdentifierID"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public static SaaSNS.Order PrepareProductOrderItems(List<ProductVasClass> productVasClassList,
                                                            MSEO.OrderRequest request, List<string> orderIdentifierID
                                                            , SaaSNS.Order response
                                                            , ClientServiceInstanceV1[] userServices, List<string> CMPSCodes, string strPremiumEmailRegrade, bool isBroadbandWifi)
        {
            int productorderItemCount = 0;
            string identifierId = string.Empty;
            string customerContextID = string.Empty;
            string billAccountID = string.Empty;
            string newRBSID = string.Empty;
            string oldRBSID = string.Empty;
            bool isNominumHomeMove = false;
            bool isAHTDone = false;
            string BtOneId = string.Empty;
            string emailDataStore = string.Empty;
            List<string> ListOfEmails = null;
            Dictionary<string, string> supplierList = new Dictionary<string, string>();

            MSEO.OrderItem[] orderItem = request.Order.OrderItem;

            billAccountID = orderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value;

            foreach (ProductVasClass productVasClass in productVasClassList)
            {
                if (productVasClass.Notes != null)// the action are set at Notes
                {
                    if (!productVasClass.Notes.Equals("provide") || productVasClass.VasProductFamily.Equals(ConfigurationManager.AppSettings["WifiProdCode"], StringComparison.OrdinalIgnoreCase )|| productVasClass.VasProductFamily.Equals(ConfigurationManager.AppSettings["BondedWifiProdCode"], StringComparison.OrdinalIgnoreCase))//avoid the provide action except Wifi/Seamless Wifi
                    {
                        if (productVasClass.VasProductFamily.Equals(ConfigurationManager.AppSettings["WifiProdCode"], StringComparison.OrdinalIgnoreCase))
                        {
                            if (!ConfigurationManager.AppSettings["WifiSwitch"].Equals("ON", StringComparison.OrdinalIgnoreCase))
                                continue;
                        }
                        //Get the IdentifierId from the list of OrderIdentifierID 
                        identifierId = MSEOSaaSMapper.GetOrderIdentifierID(orderIdentifierID, productVasClass.VasClass);

                        SaaSNS.ProductOrderItem productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
                        productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
                        productOrderItem.Header.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.notDone;
                        productOrderItem.Header.Quantity = "1";
                        productOrderItem.XmlIdKey = "PO-" + productorderItemCount.ToString();

                        productOrderItem.Header.OrderItemKey = identifierId;

                        productOrderItem.Header.ProductCode = productVasClass.VasProductFamily;


                        System.Collections.Generic.List<string> inputProductDefinitionList = new System.Collections.Generic.List<string>();
                        inputProductDefinitionList.Add(productOrderItem.Header.ProductCode);

                        System.Collections.Generic.List<ProductDefinition> productDefinition = MdmWrapper.getSaaSProductDefs(inputProductDefinitionList, Settings1.Default.ConsumerServiceProviderId);

                        //Get the current order item from the request object
                        OrderItem oItem = null;

                        var resultOrder = from order in request.Order.OrderItem
                                          where order.Identifier.Id.Equals(identifierId)
                                          select order;
                        if ((resultOrder != null) && (resultOrder.ToList().Count > 0))
                        {
                            oItem = resultOrder.ToList<OrderItem>().FirstOrDefault();
                        }
                        if (oItem == null)
                        {
                            // In case of email regrade if no customer eligible vas_classes are matching pick from first OI
                            oItem = request.Order.OrderItem.FirstOrDefault();
                        }

                        if (productDefinition.Count == 0)
                        {
                            oItem.Status = Settings1.Default.IgnoredStatus;
                            Logger.Debug("Product Code not found in Order : " + request.Order.OrderIdentifier.Value + " for order item " + identifierId);
                        }
                        else
                        {
                            productOrderItem.RoleInstances = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.RoleInstance>(1);
                            productOrderItem.ServiceInstances = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ServiceInstance>();
                            SaaSNS.RoleInstance roleInstance = new BT.SaaS.Core.Shared.Entities.RoleInstance();

                            roleInstance = new BT.SaaS.Core.Shared.Entities.RoleInstance();
                            roleInstance.RoleType = "ADMIN";
                            roleInstance.XmlIdKey = "RI-" + productorderItemCount.ToString();

                            switch (productVasClass.Notes)
                            {
                                //
                                case ("modify"):
                                case ("provide"):
                                    if (ConfigurationManager.AppSettings["NominumVasServices"].ToString().Split(',').Contains(productVasClass.VasProductFamily, StringComparer.OrdinalIgnoreCase)
                                        && productVasClass.Notes.Equals("modify", StringComparison.OrdinalIgnoreCase))
                                    {

                                        List<OrderItem> createOrderItemList = (from createOrderItem in request.Order.OrderItem
                                                                               where createOrderItem.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)
                                                                               select createOrderItem).ToList();

                                        List<OrderItem> cancelOrderItemList = (from cancelOrderItem in request.Order.OrderItem
                                                                               where cancelOrderItem.Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase)
                                                                               select cancelOrderItem).ToList();

                                        foreach (OrderItem createOrder in createOrderItemList)
                                        {
                                            if (createOrder.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("rbsid", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                newRBSID = createOrder.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("rbsid", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                                                break;
                                            }
                                        }
                                        foreach (OrderItem cancelOrder in cancelOrderItemList)
                                        {
                                            if (cancelOrder.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("rbsid", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                oldRBSID = cancelOrder.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("rbsid", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Value;
                                                break;
                                            }
                                        }

                                        if (newRBSID != oldRBSID)
                                        {
                                            roleInstance.InternalProvisioningAction = ProvisioningActionEnum.modifyService;
                                            roleInstance.Action = BT.SaaS.Core.Shared.Entities.RoleInstanceActionEnum.change;
                                            isNominumHomeMove = true;
                                        }
                                        else
                                        {
                                            roleInstance.InternalProvisioningAction = ProvisioningActionEnum.provide;
                                            roleInstance.Action = BT.SaaS.Core.Shared.Entities.RoleInstanceActionEnum.add;
                                        }
                                    }

                                    else
                                    {
                                        roleInstance.InternalProvisioningAction = ProvisioningActionEnum.provide;
                                        roleInstance.Action = BT.SaaS.Core.Shared.Entities.RoleInstanceActionEnum.add;
                                    }
                                    break;

                                case ("cease"):
                                    roleInstance.InternalProvisioningAction = ProvisioningActionEnum.cease;
                                    roleInstance.Action = BT.SaaS.Core.Shared.Entities.RoleInstanceActionEnum.delete;
                                    break;
                                case ("tradeup"):
                                    roleInstance.InternalProvisioningAction = ProvisioningActionEnum.provide;
                                    roleInstance.Action = BT.SaaS.Core.Shared.Entities.RoleInstanceActionEnum.add;
                                    break;
                                default:
                                    throw new Exception("Action not supported");
                            }


                            SaaSNS.ServiceInstance serviceInstance = new BT.SaaS.Core.Shared.Entities.ServiceInstance();
                            serviceInstance.ServiceRoles = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ServiceRole>();
                            SaaSNS.ServiceRole serviceRole = new BT.SaaS.Core.Shared.Entities.ServiceRole();
                            serviceRole.roleAttributes = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.Attribute>();

                            //oItem.Instance is the current order item instance
                            if (oItem != null)
                            {
                                foreach (MSEO.Instance instance in oItem.Instance)
                                {
                                    foreach (MSEO.InstanceCharacteristic instanceCharacteristic in instance.InstanceCharacteristic)
                                    {
                                        SaaSNS.Attribute attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attribute.Name = instanceCharacteristic.Name;
                                        attribute.Value = instanceCharacteristic.Value;
                                        roleInstance.Attributes.Add(attribute);
                                    }
                                }
                            }

                            string subscriptionID = string.Empty;
                            string subscriptionExternalReference = string.Empty;
                            string previousSupplierCode = string.Empty;
                            string cfsid = string.Empty;
                            string emailSupplier = "MX"; string vasProdId = string.Empty;
                            string[] vasContentAnyWhereProductFamily = ConfigurationManager.AppSettings["VASContentAnyWhereProductFamily"].Split(new char[] { ';' });
                            SaaSNS.Attribute attr = null;

                            if (userServices != null)
                            {
                                foreach (BT.SaaS.IspssAdapter.Dnp.ClientServiceInstanceV1 dnpserviceInstance in userServices)
                                {
                                    if (dnpserviceInstance.clientServiceInstanceStatus.value.Equals("active", StringComparison.OrdinalIgnoreCase) && dnpserviceInstance.clientServiceInstanceIdentifier.value.Equals(productVasClass.VasProductFamily + ":" + productVasClass.vasSubType, StringComparison.OrdinalIgnoreCase))
                                    {
                                        customerContextID = dnpserviceInstance.name.ToString();
                                        if (dnpserviceInstance.clientServiceInstanceCharacteristic != null)
                                        {
                                            if (dnpserviceInstance.clientServiceInstanceCharacteristic.ToList().Exists(insChar => insChar.name.Equals("subscription_id", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                subscriptionID = dnpserviceInstance.clientServiceInstanceCharacteristic.ToList().Where(insChar => insChar.name.Equals("subscription_id", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                            }
                                            if (dnpserviceInstance.clientServiceInstanceCharacteristic.ToList().Exists(insChar => insChar.name.Equals("subscrption_external", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                subscriptionExternalReference = dnpserviceInstance.clientServiceInstanceCharacteristic.ToList().Where(insChar => insChar.name.Equals("subscrption_external", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                            }
                                            if (dnpserviceInstance.clientServiceInstanceCharacteristic.ToList().Exists(insChar => insChar.name.Equals("prev_supplier_code", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                previousSupplierCode = dnpserviceInstance.clientServiceInstanceCharacteristic.ToList().Where(insChar => insChar.name.Equals("prev_supplier_code", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                            }
                                            if (dnpserviceInstance.clientServiceInstanceCharacteristic.ToList().Exists(insChar => insChar.name.Equals("cfsid", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                cfsid = dnpserviceInstance.clientServiceInstanceCharacteristic.ToList().Where(insChar => insChar.name.Equals("cfsid", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                            }
                                        }
                                        if (string.IsNullOrEmpty(cfsid) && dnpserviceInstance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"].ToString(), StringComparison.OrdinalIgnoreCase))
                                        {
                                            cfsid = dnpserviceInstance.serviceIdentity.ToList().Where(si => si.domain.Equals("cfsid", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        }
                                    }

                                    if (dnpserviceInstance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["WifiIdentifier"].ToString(), StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (dnpserviceInstance.clientServiceInstanceCharacteristic != null)
                                        {
                                            if (dnpserviceInstance.clientServiceInstanceCharacteristic.ToList().Exists(insChar => insChar.name.Equals("WIFI_BROADBAND_FLAG", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                attr.Name = "isBBFlagExists";
                                                attr.Value = "Y";
                                                roleInstance.Attributes.Add(attr);
                                            }
                                            else
                                            {
                                                attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                                attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                                attr.Name = "isBBFlagExists";
                                                attr.Value = "N";
                                                roleInstance.Attributes.Add(attr);
                                            }
                                        }
                                    }
                                }
                            }

                            attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                            attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            attr.Name = "VASPRODUCTID";
                            attr.Value = productVasClass.VasProductId;
                            roleInstance.Attributes.Add(attr);

                            foreach (string contentAnyWhereFamiily in vasContentAnyWhereProductFamily)
                            {
                                if (productVasClass.VasProductFamily.Equals(contentAnyWhereFamiily, StringComparison.OrdinalIgnoreCase))
                                {
                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "SUBSCRIPTIONEXTERNALREFERENCE";
                                    attr.Value = subscriptionExternalReference;
                                    roleInstance.Attributes.Add(attr);

                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "PREVIOUSSUPPLIERCODE";
                                    attr.Value = previousSupplierCode;
                                    roleInstance.Attributes.Add(attr);
                                }
                                else
                                {
                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "SUBSCRIPTIONID";
                                    attr.Value = subscriptionID;
                                    roleInstance.Attributes.Add(attr);
                                }
                            }
                            if (ConfigurationManager.AppSettings["NominumVasServices"].ToString().Split(',').Contains(productVasClass.VasProductFamily, StringComparer.OrdinalIgnoreCase))
                            {
                                bool UpdateCFSIDAtRBSC = false;
                                bool ActionOnCompleteNominumSubscription = false;
                                //get active nominum services from DNP.
                                var NominumVasServices = (from csi in userServices.ToList()
                                                          where (csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CFService"], StringComparison.OrdinalIgnoreCase)
                                                           || csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["NetworkSecurityService"], StringComparison.OrdinalIgnoreCase))
                                                           && csi.clientServiceInstanceStatus.value.Equals("Active", StringComparison.OrdinalIgnoreCase)
                                                          select csi).ToList();

                                if (NominumVasServices != null && NominumVasServices.Count > 1)
                                {
                                    //Get NominumProductVasClassList from productvasclass list
                                    var NominumProductVasClassList = productVasClassList.Where(pvc => ConfigurationManager.AppSettings["NominumVasServices"].ToString().Split(',').Contains(pvc.VasProductFamily, StringComparer.OrdinalIgnoreCase)).ToList();
                                    //if we have more than one nominum active service check for productvasclass.notes
                                    //if modify notes exist it implies not to cease nominum service.
                                    if (NominumProductVasClassList.Exists(pvc => pvc.Notes.Equals("modify", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        UpdateCFSIDAtRBSC = false;
                                        ActionOnCompleteNominumSubscription = false;
                                    }
                                    else
                                    {
                                        UpdateCFSIDAtRBSC = true;
                                        ActionOnCompleteNominumSubscription = true;
                                    }
                                }
                                else
                                {
                                    //if only  nominumactive service exist then cease if completly.
                                    UpdateCFSIDAtRBSC = true;
                                    ActionOnCompleteNominumSubscription = false;
                                }
                                if (productVasClass.Notes.Equals("modify", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (isNominumHomeMove)
                                    {
                                        attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attr.Name = "NEWRBSID";
                                        attr.Value = newRBSID;
                                        roleInstance.Attributes.Add(attr);

                                        attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attr.Name = "OLDRBSID";
                                        attr.Value = oldRBSID;
                                        roleInstance.Attributes.Add(attr);

                                        attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attr.Name = "ISPROMOTIONCHANGE";
                                        attr.Value = "true";
                                        roleInstance.Attributes.Add(attr);

                                        attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attr.Name = "UpdateCFSIDAtRBSC";
                                        attr.Value = "True";
                                        roleInstance.Attributes.Add(attr);

                                        if (NominumProductProcessor.IsRBSIDExistsinDNP(newRBSID))
                                        {
                                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "isRBSIDExists", Value = true.ToString() });
                                        }
                                    }
                                }
                                attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                attr.Name = "CFSID";
                                attr.Value = cfsid;
                                roleInstance.Attributes.Add(attr);
                                if (!isNominumHomeMove)
                                {
                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "UpdateCFSIDAtRBSC";
                                    attr.Value = UpdateCFSIDAtRBSC.ToString();
                                    roleInstance.Attributes.Add(attr);

                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "ActionOnCompleteNominumSubscription";
                                    attr.Value = ActionOnCompleteNominumSubscription.ToString();
                                    roleInstance.Attributes.Add(attr);
                                }
                            }
                            if (productVasClass.VasProductFamily.Equals(ConfigurationManager.AppSettings["WifiProdCode"], StringComparison.OrdinalIgnoreCase))
                            {
                                if (productVasClass.Notes.Equals("provide", StringComparison.OrdinalIgnoreCase))
                                {
                                    GetClientProfileV1Res gcpresponse = DnpWrapper.GetClientProfileV1(billAccountID, "VAS_BILLINGACCOUNT_ID");
                                    if (gcpresponse != null && gcpresponse.clientProfileV1 != null && gcpresponse.clientProfileV1.client != null
                                        && gcpresponse.clientProfileV1.client.clientIdentity != null)
                                    {
                                        BT.SaaS.IspssAdapter.Dnp.ClientIdentity bacClientIdentity = gcpresponse.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.OrdinalIgnoreCase) && i.value.Equals(billAccountID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                                        if (gcpresponse.clientProfileV1.client.clientIdentity.ToList().Exists(i => i.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                                            BtOneId = gcpresponse.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                        if (bacClientIdentity.clientIdentityValidation != null)
                                            isAHTDone = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                                    }
                                    if (productVasClass.VasServiceTier.Equals(ConfigurationManager.AppSettings["SpringWIFIServiceTier"], StringComparison.OrdinalIgnoreCase))
                                    {
                                        attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attr.Name = "HUB_WIFI_STATUS";
                                        attr.Value = "OPTED_OUT";
                                        roleInstance.Attributes.Add(attr);

                                        attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attr.Name = "WIFI_BROADBAND_FLAG";
                                        attr.Value = "N";
                                        roleInstance.Attributes.Add(attr);
                                    }
                                    else if (productVasClass.VasServiceTier.Equals(ConfigurationManager.AppSettings["BBWIFIServiceTier"], StringComparison.OrdinalIgnoreCase))
                                    {
                                        attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attr.Name = "HUB_WIFI_STATUS";
                                        attr.Value = "OPTED_IN";
                                        roleInstance.Attributes.Add(attr);

                                        attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attr.Name = "WIFI_BROADBAND_FLAG";
                                        attr.Value = "Y";
                                        roleInstance.Attributes.Add(attr);
                                    }

                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "WIFI_SERVICE_TIER";
                                    attr.Value = productVasClass.VasServiceTier.ToString();
                                    roleInstance.Attributes.Add(attr);

                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "ISAHTDONE";
                                    attr.Value = isAHTDone.ToString();
                                    roleInstance.Attributes.Add(attr);

                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "BtOneId";
                                    attr.Value = BtOneId;
                                    roleInstance.Attributes.Add(attr);

                                    //NAYANAGL-61637
                                    ClientServiceInstanceV1[] serviceInstances = null;
                                    serviceInstances = DnpWrapper.getServiceInstanceV1(billAccountID, "VAS_BILLINGACCOUNT_ID", String.Empty);
                                    if (serviceInstances != null && serviceInstances.Count() > 0)
                                    {
                                        if (serviceInstances.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals("MANAGE_ACCOUNT", StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceCharacteristic != null && si.clientServiceInstanceCharacteristic.ToList().Exists(cschar => cschar.name.Equals("BACORGANISATIONNAME", StringComparison.OrdinalIgnoreCase) && cschar.value.StartsWith("EE", StringComparison.OrdinalIgnoreCase))))
                                        {
                                            SaaSNS.Attribute nayanwififlag = new BT.SaaS.Core.Shared.Entities.Attribute();
                                            nayanwififlag.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                            nayanwififlag.Name = "ISNAYANWIFI";
                                            nayanwififlag.Value = true.ToString();
                                            roleInstance.Attributes.Add(nayanwififlag);
                                        }
                                    }
                                }
                                else if (productVasClass.Notes.Equals("cease", StringComparison.OrdinalIgnoreCase))
                                {
                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "WIFI_BROADBAND_FLAG";
                                    attr.Value = "N";
                                    roleInstance.Attributes.Add(attr);
                                }


                                else if (productVasClass.Notes.Equals("modify", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (isBroadbandWifi == true)
                                    {
                                        attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attr.Name = "WIFI_BROADBAND_FLAG";
                                        attr.Value = "Y";
                                        roleInstance.Attributes.Add(attr);
                                    }
                                    else
                                    {
                                        attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attr.Name = "WIFI_BROADBAND_FLAG";
                                        attr.Value = "N";
                                        roleInstance.Attributes.Add(attr);
                                    }

                                    //NAYANAGL-61637
                                    ClientServiceInstanceV1[] serviceInstances = null;
                                    serviceInstances = DnpWrapper.getServiceInstanceV1(billAccountID, "VAS_BILLINGACCOUNT_ID", String.Empty);
                                    if (serviceInstances != null && serviceInstances.Count() > 0)
                                    {
                                        //NAYANAGL-61637
                                        if (serviceInstances.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals("MANAGE_ACCOUNT", StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceCharacteristic != null && si.clientServiceInstanceCharacteristic.ToList().Exists(cschar => cschar.name.Equals("BACORGANISATIONNAME", StringComparison.OrdinalIgnoreCase) && cschar.value.StartsWith("EE", StringComparison.OrdinalIgnoreCase))))
                                        {
                                            SaaSNS.Attribute nayanwififlag = new BT.SaaS.Core.Shared.Entities.Attribute();
                                            nayanwififlag.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                            nayanwififlag.Name = "ISNAYANWIFI";
                                            nayanwififlag.Value = true.ToString();
                                            roleInstance.Attributes.Add(nayanwififlag);
                                        }
                                    }
                                }
                            }

                            if (productVasClass.VasProductFamily.Equals(ConfigurationManager.AppSettings["BondedWifiProdCode"], StringComparison.OrdinalIgnoreCase))
                            {
                                if (productVasClass.Notes.Equals("provide", StringComparison.OrdinalIgnoreCase))
                                {
                                    GetClientProfileV1Res gcpresponse = DnpWrapper.GetClientProfileV1(billAccountID, "VAS_BILLINGACCOUNT_ID");
                                    if (gcpresponse != null && gcpresponse.clientProfileV1 != null && gcpresponse.clientProfileV1.client != null
                                        && gcpresponse.clientProfileV1.client.clientIdentity != null)
                                    {
                                        BT.SaaS.IspssAdapter.Dnp.ClientIdentity bacClientIdentity = gcpresponse.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.OrdinalIgnoreCase) && i.value.Equals(billAccountID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                                        if (gcpresponse.clientProfileV1.client.clientIdentity.ToList().Exists(i => i.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                                            BtOneId = gcpresponse.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                                        if (bacClientIdentity.clientIdentityValidation != null)
                                            isAHTDone = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                                    }

                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "ISAHTDONE";
                                    attr.Value = isAHTDone.ToString();
                                    roleInstance.Attributes.Add(attr);

                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "BtOneId";
                                    attr.Value = BtOneId;
                                    roleInstance.Attributes.Add(attr);
                                }
                            }

                            //Changes for MMA
                            if (productOrderItem.Header.ProductCode.Equals(ConfigurationManager.AppSettings["NPPService"], StringComparison.OrdinalIgnoreCase))
                            {
                                if (productVasClass.Notes != null && productVasClass.Notes.Equals("modify", StringComparison.OrdinalIgnoreCase) && productVasClass.vasSubType.Equals("common", StringComparison.OrdinalIgnoreCase))
                                {
                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "LICENSEQTY";
                                    attr.Value = productVasClass.ActivationCardinality.ToString();
                                    roleInstance.Attributes.Add(attr);

                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "subtype";
                                    attr.Value = productVasClass.vasSubType;
                                    roleInstance.Attributes.Add(attr);
                                }
                                if (productVasClass.Notes != null && productVasClass.Notes.Equals("tradeup", StringComparison.OrdinalIgnoreCase))
                                {
                                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "REASON";
                                    attr.Value = "TradeUp";
                                    roleInstance.Attributes.Add(attr);
                                }
                            }
                            //BTRCE-108426/BTRCE-108471
                            if (productVasClass.VasProductFamily.Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"], StringComparison.OrdinalIgnoreCase))
                            {
                                if (productVasClass.Notes.Equals("cease", StringComparison.OrdinalIgnoreCase))
                                {
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "SIBACNUMBER", Value = billAccountID });

                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "REASON", Value = "EMAILCEASE" });
                                }

                                ListOfEmails = DanteRequestProcessor.GetListOfEmailsOnCease(billAccountID, string.Empty, "vascease", "EMAILREGRADE", ref emailDataStore, ref emailSupplier, ref vasProdId,ref supplierList);

                                if (supplierList != null && supplierList.Count() > 0)
                                {
                                    if (supplierList.ContainsKey("MXMailboxes") && !string.IsNullOrEmpty(supplierList["MXMailboxes"]))
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "MXMailboxes", Value = supplierList["MXMailboxes"] });
                                    if (supplierList.ContainsKey("Yahoomailboxes") && !string.IsNullOrEmpty(supplierList["Yahoomailboxes"]))
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "Yahoomailboxes", Value = supplierList["Yahoomailboxes"] });
                                    if (supplierList.ContainsKey("CPMSMailboxes") && !string.IsNullOrEmpty(supplierList["CPMSMailboxes"]))
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "CPMSMailboxes", Value = supplierList["CPMSMailboxes"] });
                                }

                                if (orderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("emailsupplier", StringComparison.OrdinalIgnoreCase)))
                                {
                                    roleInstance.Attributes.ToList().Where(roleInstAttrb => roleInstAttrb.Name.Equals("EMAILSUPPLIER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = emailSupplier;
                                }
                                else
                                {
                                    SaaSNS.Attribute isEmailSupplier = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    isEmailSupplier.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    isEmailSupplier.Name = "EmailSupplier";
                                    isEmailSupplier.Value = emailSupplier;
                                    roleInstance.Attributes.Add(isEmailSupplier);
                                }

                                if (!roleInstance.Attributes.ToList().Exists(ic => ic.Name.Equals("VASPRODUCTID", StringComparison.OrdinalIgnoreCase)))
                                {
                                    SaaSNS.Attribute ceaseReasonAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    ceaseReasonAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    ceaseReasonAttribute.Name = "VASPRODUCTID";
                                    ceaseReasonAttribute.Value = string.IsNullOrEmpty(vasProdId) ? "VPEM000010" : vasProdId;
                                    roleInstance.Attributes.Add(ceaseReasonAttribute);
                                }

                                if (emailDataStore.Equals("CISP"))
                                    continue;
                                if (ListOfEmails[0] != null)
                                {
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ListOfEmails", Value = ListOfEmails[0] });
                                }
                                if (ListOfEmails[1] != null)
                                {
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ProposedEmailList", Value = ListOfEmails[1] });
                                }
                                if (ListOfEmails[2] != null)
                                {
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "InactiveEmailList", Value = ListOfEmails[2] });
                                }
                                if (ListOfEmails[3] != null)
                                {
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "UnmergedEmailList", Value = ListOfEmails[3] });
                                }
                                //BTR 80925 **Start** Added two attributes to identify the type of request i.e either (premium to broadband or broadband to premium )
                                if ((!string.IsNullOrEmpty(strPremiumEmailRegrade)) && strPremiumEmailRegrade.Equals("premiumtobb", StringComparison.OrdinalIgnoreCase))
                                {
                                    SaaSNS.Attribute attrPremiumEmail = null;
                                    attrPremiumEmail = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attrPremiumEmail.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attrPremiumEmail.Name = "ispremiumtobb";
                                    attrPremiumEmail.Value = "true";
                                    roleInstance.Attributes.Add(attrPremiumEmail);
                                }
                                else if ((!string.IsNullOrEmpty(strPremiumEmailRegrade)) && strPremiumEmailRegrade.Equals("bbtopremium", StringComparison.OrdinalIgnoreCase))
                                {
                                    SaaSNS.Attribute attrPremiumEmail = null;
                                    attrPremiumEmail = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attrPremiumEmail.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attrPremiumEmail.Name = "isbbtopremium";
                                    attrPremiumEmail.Value = "true";
                                    roleInstance.Attributes.Add(attrPremiumEmail);
                                }//**end**
                                else if ((!string.IsNullOrEmpty(strPremiumEmailRegrade)) && strPremiumEmailRegrade.Equals("bbtobasic", StringComparison.OrdinalIgnoreCase))
                                {
                                    SaaSNS.Attribute attrPremiumEmail = null;
                                    attrPremiumEmail = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attrPremiumEmail.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attrPremiumEmail.Name = "isbbtobasic";
                                    attrPremiumEmail.Value = "true";
                                    roleInstance.Attributes.Add(attrPremiumEmail);
                                }//**end**
                                else if ((!string.IsNullOrEmpty(strPremiumEmailRegrade)) && strPremiumEmailRegrade.Equals("basictobb", StringComparison.OrdinalIgnoreCase))
                                {
                                    SaaSNS.Attribute attrPremiumEmail = null;
                                    attrPremiumEmail = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attrPremiumEmail.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attrPremiumEmail.Name = "isbasictobb";
                                    attrPremiumEmail.Value = "true";
                                    roleInstance.Attributes.Add(attrPremiumEmail);
                                }//**end**

                                SaaSNS.Attribute emailRegradeType = null;
                                emailRegradeType = new BT.SaaS.Core.Shared.Entities.Attribute();
                                emailRegradeType.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                emailRegradeType.Name = "emailregradetype";
                                emailRegradeType.Value = strPremiumEmailRegrade;
                                roleInstance.Attributes.Add(emailRegradeType);
                            }

                            attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                            attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            attr.Name = "CUSTOMERCONTEXTID";
                            attr.Value = customerContextID;
                            roleInstance.Attributes.Add(attr);

                            //Temp fix to pass customer context ID for vas regrade orders(to overcome stuck orders replay issue)
                            attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                            attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            attr.Name = "CUSTOMERCONTEXTIDRegrade";
                            attr.Value = customerContextID;
                            roleInstance.Attributes.Add(attr);

                            if (productVasClass.Notes != null && productVasClass.Notes.Equals("tradeup", StringComparison.OrdinalIgnoreCase))
                            {
                                attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                attr.Name = "VASSUBTYPE";
                                attr.Value = "Common";
                                roleInstance.Attributes.Add(attr);

                                attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                attr.Name = "SUPPLIERCODE";
                                attr.Value = ConfigurationManager.AppSettings["mmasuppliercode"].ToString();
                                roleInstance.Attributes.Add(attr);

                                attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                attr.Name = "SOURCESKU";
                                attr.Value = productVasClass.SupplierCode;
                                roleInstance.Attributes.Add(attr);
                            }
                            else
                            {
                                attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                attr.Name = "VASSUBTYPE";
                                attr.Value = productVasClass.vasSubType;
                                roleInstance.Attributes.Add(attr);

                                attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                attr.Name = "SUPPLIERCODE";
                                attr.Value = productVasClass.SupplierCode;
                                roleInstance.Attributes.Add(attr);
                            }

                            if (CMPSCodes.Count > 0)
                            {
                                attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                attr.Name = "CMPSRESPONSE";
                                attr.Value = CMPSCodes[0].ToString();
                                roleInstance.Attributes.Add(attr);
                            }

                            SaaSNS.Attribute isVASRegradeAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            isVASRegradeAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            isVASRegradeAttribute.Name = "ISVASREGRADE";
                            if (!productVasClass.Notes.Equals("provide"))
                            {
                                if (!productVasClass.Notes.Equals("tradeup"))
                                {
                                    isVASRegradeAttribute.Value = "TRUE";
                                }
                                else
                                    isVASRegradeAttribute.Value = "FALSE";//for McAfee TradeUp
                            }
                            else
                            {
                                isVASRegradeAttribute.Value = "FALSE";//for Wifi provide and McAfee TradeUp
                            }
                            roleInstance.Attributes.Add(isVASRegradeAttribute);

                            serviceInstance.ServiceRoles.Add(serviceRole);
                            productOrderItem.ServiceInstances.Add(serviceInstance);
                            productOrderItem.RoleInstances.Add(roleInstance);
                            productorderItemCount++;
                            if (request.StandardHeader != null && request.StandardHeader.E2e != null && request.StandardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(request.StandardHeader.E2e.E2EDATA.ToString()))
                                productOrderItem.RoleInstances[0].Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "E2EDATA", Value = request.StandardHeader.E2e.E2EDATA.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                            else
                                productOrderItem.RoleInstances[0].Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "E2EDATA", Value = string.Empty, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });

                            if (!(response.ProductOrderItems.ToList().Exists(poi => poi.Header.ProductCode.ToString().Equals(productOrderItem.Header.ProductCode) && poi.RoleInstances.FirstOrDefault().InternalProvisioningAction.ToString().Equals(productOrderItem.RoleInstances.FirstOrDefault().InternalProvisioningAction.ToString()) && poi.RoleInstances.FirstOrDefault().Attributes.ToList().Exists(at => at.Name.Equals("VASSUBTYPE") && at.Value.Equals(productVasClass.vasSubType)))))
                            {
                                response.ProductOrderItems.Add(productOrderItem);
                            }
                        }
                    }
                }
            }
            if (response.ProductOrderItems != null && response.ProductOrderItems.Count == 0)
            {
                throw new MdmException("No Services exist to Upgrade/Downgrade for given VasClasse's");
            }
            return response;
        }

        /// <summary>
        /// Get the required action for each Vasproduct
        /// Make call to MDM,CMPS and DnP to set the action 
        /// Get customer eligible list from cmps prod vasclass(es)+ create list prod prod vasclass(es) - cancel list prod vasclass(es) 
        /// Find list of cancel and create product items and set the action 
        /// as per the below rule
        /// 1. Modify the products for which customer is eligible an dpresent in either create or cancel vasclass(es) list
        /// 2. Cancel the products for which customer is not eligible and present in cancel cancel vasclass(es) list
        /// 3. Provide only early activation products(email,wifi) if present in customer is eligible and present in create vasclass(es) list
        /// 
        ///  The below is the sub list details from parameter mainVasList
        ///  
        /// vasClassList = mainVasList[0];
        /// vasClassListCancel = mainVasList[1];
        /// vasClassListCreate = mainVasList[2];
        /// billAccountNumber = mainVasList[3].FirstOrDefault();
        /// OrderIdentifierID = mainVasList[4];
        /// </summary>
        /// <param name="identityDomain"></param>
        /// <param name="serviceType"></param>
        /// <param name="request"></param>
        /// <param name="mainVasList"></param>
        /// <returns></returns>
        public static List<ProductVasClass> GetAction(
                               string identityDomain, string serviceType,
                               MSEO.OrderRequest request,
                                List<List<string>> mainVasList,
                                 ref ClientServiceInstanceV1[] userServices, ref List<string> CMPSCodes, ref E2ETransaction e2eData, ref bool caceaseskipped)
        {
            SaaSNS.ProductOrderItem productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();

            #region attribute List
            List<ProductVasClass> cmpsProdVasClassList = new List<ProductVasClass>();
            List<ProductVasClass> mdmProductVasClassList = new List<ProductVasClass>();
            List<ProductVasClass> mdmProductVasClassListcancel = new List<ProductVasClass>();
            List<ProductVasClass> mdmProductVasClassListcreate = new List<ProductVasClass>();
            List<ProductVasClass> activeProductVasClassList = new List<ProductVasClass>();
            List<string> dnpProductVasClassList = new List<string>();
            List<ProductVasClass> custEligibleVASProducts = new List<ProductVasClass>();
            List<ProductVasClass> unionList = null;
            ProductVasClass eligibleProductVasClass = null;
            bool isScodeExist = true;
            const string ScodesReturnedfromCMPS = "ScodesReturnedfromCMPS";

            #endregion

            ClientServiceInstanceV1[] serviceInstances = null;
            CustomerAssetsResponse assetResponse = null;
            string caSupplierCode = string.Empty;
            string acc_id = string.Empty;
            string email_Supplier = "MX";
            try
            {
                if (string.Equals(serviceType, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                {
                    // check if the mainVasList is not null and contains sub lists

                    if ((mainVasList != null) && (mainVasList.Count > 0))
                    {
                        // check if the mainVasList contains sub list at index 3.

                        if ((mainVasList[3] != null) && (mainVasList[3].Count > 0))
                        {
                            serviceInstances = DnpWrapper.getServiceInstanceV1(mainVasList[3].FirstOrDefault(), identityDomain, string.Empty);
                            //Get services linked to migrated BillAccountNumber
                            ClientServiceInstanceV1[] migratedServices = DnpWrapper.GetVASClientServiceInstances(mainVasList[3].FirstOrDefault(), "SECURITY_BAC");

                            if (serviceInstances != null)
                            {
                                if (migratedServices != null)
                                    serviceInstances = serviceInstances.Concat(migratedServices).ToArray();
                            }
                            else
                                serviceInstances = migratedServices;

                            userServices = serviceInstances;

                            assetResponse = CmpsWrapper.GetCustomerAssets(mainVasList[3].FirstOrDefault(), ref e2eData);
                            if (assetResponse != null)
                            {
                                cmpsProdVasClassList = CmpsWrapper.ProductVasClassList(assetResponse, ref CMPSCodes);
                                e2eData.logMessage(ScodesReturnedfromCMPS, string.Join(",", CMPSCodes.ToArray()));
                            }
                            else
                            {
                                e2eData.logMessage(ScodesReturnedfromCMPS, "No asset details retuned from CMPS");
                                throw new CMPSException("No asset details retuned from CMPS");
                            }
                        }
                        if (serviceInstances != null)// if null just avoid the calls to MDM
                        {
                            // check if the mainVasList contains sub list at index 0  
                            // and use the list to get all the product def from MDM

                            if ((mainVasList[0] != null) && (mainVasList[0].Count > 0))
                            {
                                mdmProductVasClassList = MdmWrapper.getSaaSVASDefs(mainVasList[0]);//get prd def for all vas
                            }

                            /*
                             * check if the mainVasList contains sub list at index 1
                             * and use the list to get the list of cancel prod def
                             * FirstOrDefault : there will be max of 1 cancel and 1 
                             * create orderitem and VasClassList
                             */

                            if ((mainVasList[1] != null) && (mainVasList[1].Count > 0))
                            {
                                foreach (var cancelvasclass in mainVasList[1])
                                {
                                    var resultcancel = from cancelVasList in mdmProductVasClassList
                                                       where cancelVasList.VasClass.Equals(cancelvasclass)
                                                       select cancelVasList;
                                    mdmProductVasClassListcancel.AddRange(resultcancel.ToList());
                                }
                            }

                            /*
                             * check if the mainVasList contains sub list at index 2
                             * and use the list to get the list of create prod def
                             * FirstOrDefault : there will be max of 1 cancel and 1 
                             * create orderitem and VasClassList
                             */
                            if ((mainVasList[2] != null) && (mainVasList[2].Count > 0))
                            {
                                List<ProductVasClass> vasDefinitionList = MdmWrapper.getSaaSVASDefs(mainVasList[2]);

                                string[] vasProductFamilyList = ConfigurationManager.AppSettings["VASProductFamily"].Split(new char[] { ';' });
                                string[] vasSubTypeList = ConfigurationManager.AppSettings["VASSubType"].Split(new char[] { ';' });
                                foreach (string vasProductFamily in vasProductFamilyList)
                                {
                                    foreach (string vasSubType in vasSubTypeList)
                                    {
                                        ProductVasClass maxPreferenceIndicatorProductVASClass = (from vasDefinition in vasDefinitionList
                                                                                                 where vasDefinition.VasProductFamily.Equals(vasProductFamily, StringComparison.OrdinalIgnoreCase)
                                                                                                 && vasDefinition.vasSubType.Equals(vasSubType, StringComparison.OrdinalIgnoreCase)
                                                                                                 orderby vasDefinition.PreferenceIndicator descending
                                                                                                 select vasDefinition).FirstOrDefault();
                                        if (maxPreferenceIndicatorProductVASClass != null)
                                            mdmProductVasClassListcreate.Add(maxPreferenceIndicatorProductVASClass);
                                    }
                                }
                            }

                            if (serviceInstances != null && serviceInstances.Length > 0)
                            {
                                foreach (ClientServiceInstanceV1 serviceIntance in serviceInstances)
                                {
                                    if (serviceIntance.clientServiceInstanceStatus.value.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (serviceIntance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"].ToString(), StringComparison.OrdinalIgnoreCase))
                                        {
                                            //checking if Email is managed by either CISP or D&P. 
                                            if (serviceIntance.clientServiceInstanceCharacteristic != null)
                                            //&& serviceIntance.clientServiceInstanceCharacteristic.ToList().Exists(sic => sic.name.Equals("ACC_ID", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                serviceIntance.clientServiceInstanceCharacteristic.ToList().ForEach(x =>
                                                {
                                                    if (x.name.Equals("ACC_ID", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        acc_id = x.value;
                                                    }
                                                    else if (x.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        email_Supplier = x.value;
                                                    }
                                                }
                                                               );
                                                //acc_id = serviceIntance.clientServiceInstanceCharacteristic.ToList().Where(sic => sic.name.Equals("ACC_ID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                            }
                                            if (!String.IsNullOrEmpty(acc_id.Trim()))
                                            {
                                                //Changes done for this BTRCE 108426/ BTRCE-108471/ BTRCE-108573
                                                if (ConfigurationManager.AppSettings["DnPManagedEmailSupplier"].Split(',').Contains(email_Supplier, StringComparer.OrdinalIgnoreCase))
                                                {
                                                    //Adding Dante Service Info as VASProduct Family and Subtype Format
                                                    dnpProductVasClassList.Add(ConfigurationManager.AppSettings["DanteIdentifierInMDM"]);
                                                }
                                            }
                                            else
                                            {
                                                dnpProductVasClassList.Add(ConfigurationManager.AppSettings["DanteIdentifierInMDM"]);
                                            }
                                        }
                                        else if (serviceIntance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["BondedWifiIdentifier"].ToString(), StringComparison.OrdinalIgnoreCase))
                                        {
                                            dnpProductVasClassList.Add(ConfigurationManager.AppSettings["BondedWifiIdentifierInMDM"]);
                                        }
                                        else
                                        {
                                            dnpProductVasClassList.Add(serviceIntance.clientServiceInstanceIdentifier.value.ToLower());

                                            //stuck order fix
                                            //if (serviceIntance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["CAService"].ToString(), StringComparison.OrdinalIgnoreCase))
                                            //{
                                            //    if (serviceIntance.clientServiceInstanceCharacteristic.ToList().Exists(insChar => insChar.name.Equals("prev_supplier_code", StringComparison.OrdinalIgnoreCase)))
                                            //    {
                                            //        caSupplierCode = serviceIntance.clientServiceInstanceCharacteristic.ToList().Where(insChar => insChar.name.Equals("prev_supplier_code", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                            //    }
                                            //}
                                        }
                                    }
                                }
                            }
                            /* Verify the mdmProductVasClassListcancel and mdmProductVasClassListcreate 
                               are not null and contain values
                             */

                            if ((mdmProductVasClassListcancel != null) && (mdmProductVasClassListcancel.Count > 0)
                                 && (mdmProductVasClassListcreate != null) && (mdmProductVasClassListcreate.Count > 0))
                            {
                                if (cmpsProdVasClassList.Count > 0)
                                {
                                    unionList = new List<ProductVasClass>();//Add CMPS list and Create List
                                    unionList.AddRange(cmpsProdVasClassList);
                                    unionList.AddRange(mdmProductVasClassListcreate);

                                    foreach (ProductVasClass mdmVasCls in mdmProductVasClassListcancel)
                                    {
                                        if ((mdmVasCls.VasProductFamily + ":" + mdmVasCls.vasSubType).Equals(ConfigurationManager.AppSettings["WifiIdentifier"], StringComparison.OrdinalIgnoreCase))
                                        {
                                            eligibleProductVasClass = unionList.Where(cmps => (cmps.VasProductFamily.Equals(mdmVasCls.VasProductFamily, StringComparison.OrdinalIgnoreCase) && cmps.vasSubType.Equals(mdmVasCls.vasSubType, StringComparison.OrdinalIgnoreCase))
                                                                                       && ((cmps.VasProductFamily + ":" + cmps.vasSubType).Equals(ConfigurationManager.AppSettings["WifiIdentifier"], StringComparison.OrdinalIgnoreCase)) && cmps.VasServiceTier.Equals(mdmVasCls.VasServiceTier, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                            unionList.Remove(eligibleProductVasClass);
                                        }
                                        else if ((mdmVasCls.VasProductFamily + ":" + mdmVasCls.vasSubType).Equals(ConfigurationManager.AppSettings["MMAService"], StringComparison.OrdinalIgnoreCase))
                                        {
                                            eligibleProductVasClass = unionList.Where(cmps => (cmps.VasProductFamily.Equals(mdmVasCls.VasProductFamily, StringComparison.OrdinalIgnoreCase) && cmps.vasSubType.Equals(mdmVasCls.vasSubType, StringComparison.OrdinalIgnoreCase))
                                                                                       && ((cmps.VasProductFamily + ":" + cmps.vasSubType).Equals(ConfigurationManager.AppSettings["MMAService"], StringComparison.OrdinalIgnoreCase)) && cmps.ActivationCardinality.Equals(mdmVasCls.ActivationCardinality)).FirstOrDefault();
                                            unionList.Remove(eligibleProductVasClass);
                                        }
                                        else if ((mdmVasCls.VasProductFamily + ":" + mdmVasCls.vasSubType).Equals(ConfigurationManager.AppSettings["DanteIdentifierInMDM"], StringComparison.OrdinalIgnoreCase))
                                        {
                                            eligibleProductVasClass = unionList.Where(cmps => (cmps.VasProductFamily.Equals(mdmVasCls.VasProductFamily, StringComparison.OrdinalIgnoreCase) && cmps.vasSubType.Equals(mdmVasCls.vasSubType, StringComparison.OrdinalIgnoreCase))
                                                                                       && ((cmps.VasProductFamily + ":" + cmps.vasSubType).Equals(ConfigurationManager.AppSettings["DanteIdentifierInMDM"], StringComparison.OrdinalIgnoreCase)) && cmps.PreferenceIndicator.Equals(mdmVasCls.PreferenceIndicator)).FirstOrDefault();
                                            unionList.Remove(eligibleProductVasClass);
                                        }
                                        else
                                        {
                                            eligibleProductVasClass = unionList.Where(cmps => cmps.VasProductFamily.Equals(mdmVasCls.VasProductFamily, StringComparison.OrdinalIgnoreCase) && cmps.vasSubType.Equals(mdmVasCls.vasSubType, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                            unionList.Remove(eligibleProductVasClass);
                                        }
                                    }
                                    //get the distinct productVasClassList
                                    custEligibleVASProducts = unionList.GroupBy(x => (x.VasProductFamily + x.vasSubType)).Select(x => x.First()).ToList();
                                    ProductVasClass maxActivationCardinalityProductVASClass = null;
                                    if (dnpProductVasClassList.Contains((ConfigurationManager.AppSettings["MMAService"]), StringComparer.OrdinalIgnoreCase) ||
                                        (dnpProductVasClassList.Contains((ConfigurationManager.AppSettings["NPPWindowsService"]), StringComparer.OrdinalIgnoreCase)))
                                    {
                                        maxActivationCardinalityProductVASClass = (from vasDefinition in unionList
                                                                                   where vasDefinition.VasProductFamily.Equals("ADVANCEDSECURITY", StringComparison.OrdinalIgnoreCase)
                                                                                   && vasDefinition.vasSubType.Equals("Common", StringComparison.OrdinalIgnoreCase)
                                                                                   orderby vasDefinition.ActivationCardinality descending
                                                                                   select vasDefinition).FirstOrDefault();
                                    }

                                    ProductVasClass maxPreferenceIndicatoeEmailVASProduct = null;
                                    if (dnpProductVasClassList.Contains((ConfigurationManager.AppSettings["DanteIdentifierInMDM"]), StringComparer.OrdinalIgnoreCase))
                                    {
                                        maxPreferenceIndicatoeEmailVASProduct = (from vasDefinition in unionList
                                                                                 where vasDefinition.VasProductFamily.Equals("EMAIL", StringComparison.OrdinalIgnoreCase)
                                                                                 orderby vasDefinition.PreferenceIndicator descending
                                                                                 select vasDefinition).FirstOrDefault();
                                    }

                                    foreach (string dnpServiceCode in dnpProductVasClassList)
                                    {
                                        //modify the service if customer is eliglible(based on CMPS assets) and present in either create or cancel vas class 
                                        if (custEligibleVASProducts.Exists(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)) &&
                                            (mdmProductVasClassListcreate.Exists(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)) || mdmProductVasClassListcancel.Exists(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase))))
                                        {
                                            if (mdmProductVasClassListcreate.Exists(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(x.Notes)))
                                            {
                                                mdmProductVasClassListcreate.Where(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(x.Notes)).FirstOrDefault().Notes = "modify";
                                                // Updating McAfee MMA Licensequantity to Eligible license quantity(Temporary fix need to be updatd in 45. or 46)
                                                if (dnpServiceCode.Equals(ConfigurationManager.AppSettings["MMAService"], StringComparison.OrdinalIgnoreCase))
                                                {
                                                    mdmProductVasClassListcreate.Where(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().ActivationCardinality = maxActivationCardinalityProductVASClass.ActivationCardinality;
                                                    mdmProductVasClassListcreate.Where(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().VasProductId = maxActivationCardinalityProductVASClass.VasProductId;
                                                }
                                                else if (dnpServiceCode.Equals(ConfigurationManager.AppSettings["DanteIdentifierInMDM"], StringComparison.OrdinalIgnoreCase))
                                                {
                                                    mdmProductVasClassListcreate.Where(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().VasClass = maxPreferenceIndicatoeEmailVASProduct.VasClass;
                                                    mdmProductVasClassListcreate.Where(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().VasProductId = maxPreferenceIndicatoeEmailVASProduct.VasProductId;
                                                }
                                            }
                                            else if (mdmProductVasClassListcancel.Exists(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(x.Notes)))
                                            {
                                                mdmProductVasClassListcancel.Where(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(x.Notes)).FirstOrDefault().Notes = "modify";
                                                if (dnpServiceCode.Equals(ConfigurationManager.AppSettings["MMAService"], StringComparison.OrdinalIgnoreCase))
                                                {
                                                    mdmProductVasClassListcancel.Where(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().ActivationCardinality = maxActivationCardinalityProductVASClass.ActivationCardinality;
                                                    mdmProductVasClassListcancel.Where(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().VasProductId = maxActivationCardinalityProductVASClass.VasProductId;
                                                }
                                                else if (dnpServiceCode.Equals(ConfigurationManager.AppSettings["DanteIdentifierInMDM"], StringComparison.OrdinalIgnoreCase))
                                                {
                                                    mdmProductVasClassListcancel.Where(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().VasClass = maxPreferenceIndicatoeEmailVASProduct.VasClass;
                                                    mdmProductVasClassListcancel.Where(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().VasProductId = maxPreferenceIndicatoeEmailVASProduct.VasProductId;
                                                }
                                            }
                                        }
                                        // cease if customer is not eligible(based on CMPS assets and present in cancel vas class 
                                        else if (!custEligibleVASProducts.Exists(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)) && mdmProductVasClassListcancel.Exists(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            if (dnpServiceCode.Equals(ConfigurationManager.AppSettings["CAService"].ToString(), StringComparison.OrdinalIgnoreCase))
                                            {
                                                //mdmProductVasClassListcancel.ToList().Where(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase) && x.SupplierCode.Equals(caSupplierCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Notes = "cease";
                                                mdmProductVasClassListcancel.ToList().Where(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Notes = "cease";
                                            }
                                            else if (dnpServiceCode.Equals(ConfigurationManager.AppSettings["NPPWindowsService"], StringComparison.OrdinalIgnoreCase))
                                            {
                                                if (custEligibleVASProducts.Exists(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(ConfigurationManager.AppSettings["MMAService"], StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    if (!dnpProductVasClassList.Contains(ConfigurationManager.AppSettings["MMAService"], StringComparer.OrdinalIgnoreCase))
                                                    {
                                                        mdmProductVasClassListcancel.ToList().Where(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Notes = "tradeup";

                                                        mdmProductVasClassListcancel.Where(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().ActivationCardinality = maxActivationCardinalityProductVASClass.ActivationCardinality;
                                                        mdmProductVasClassListcancel.Where(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().VasProductId = maxActivationCardinalityProductVASClass.VasProductId;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                mdmProductVasClassListcancel.ToList().Where(x => (x.VasProductFamily + ":" + x.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Notes = "cease";
                                            }
                                        }
                                    }

                                    /*
                                     Seting action:provide
                                      Verify with the ProvideList and also check the Notes value
                                     *provision the early activation products if customer is eligible,
                                      present in create list and not present in DnP */
                                    string[] earlyActivationProducts = ConfigurationManager.AppSettings["VASEarlyActivationProducts"].ToString().Split(';');
                                    mdmProductVasClassListcreate.ForEach(a =>
                                    {
                                        if ((custEligibleVASProducts.Exists(x => (x.VasProductFamily + ":" + x.vasSubType).Equals((a.VasProductFamily + ":" + a.vasSubType), StringComparison.OrdinalIgnoreCase)))
                                            && earlyActivationProducts.Contains(a.VasProductFamily, StringComparer.OrdinalIgnoreCase) && (string.IsNullOrEmpty(a.Notes)))
                                        {
                                            a.Notes = "provide";
                                        }
                                    });

                                    activeProductVasClassList = mdmProductVasClassListcreate.Where(x => !string.IsNullOrEmpty(x.Notes)).ToList();
                                    activeProductVasClassList.AddRange(mdmProductVasClassListcancel.Where(mdmcncl => !string.IsNullOrEmpty(mdmcncl.Notes)));

                                    // Temporary fix to skip CA during VAS Regrade(when the vas_Class details are not present in SaaS MDM)
                                    if (Convert.ToBoolean(ConfigurationManager.AppSettings["SkipCeaseforCA"]))
                                    {
                                        cmpsProdVasClassList.Select(x => x.VasClass).Distinct().ToList().ForEach(x =>
                                        {
                                            if (!mainVasList[2].Contains(x))
                                            {
                                                isScodeExist = false;
                                            }
                                        });

                                        if (!isScodeExist && activeProductVasClassList.Exists(x => x.VasProductFamily.Equals("ContentAnywhere", StringComparison.OrdinalIgnoreCase) && x.Notes.Equals("cease", StringComparison.OrdinalIgnoreCase)))
                                        {
                                            eligibleProductVasClass = activeProductVasClassList.Where(x => x.VasProductFamily.Equals("ContentAnywhere", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                            activeProductVasClassList.Remove(eligibleProductVasClass);
                                            caceaseskipped = true;
                                        }
                                    }
                                }
                                else
                                {
                                    //e2eData.logMessage(ScodesReturnedfromCMPS, string.Join(",", CMPSCodes.ToArray()));
                                    throw new CMPSException("There are no definations in MDM for the follwing Scodes returned from CMPS " + string.Join(",", CMPSCodes.ToArray()));
                                }
                            }
                            else
                            {
                                throw new MdmException("No products mapped for given VAS Class(es) in MDM");
                            }

                        }
                        else
                        {
                            throw new DnpException("No services mapped for given BAC in DnP");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //dispose all the used lists and objects
                mdmProductVasClassListcancel = null;
                mdmProductVasClassListcreate = null;
                dnpProductVasClassList = null;
                serviceInstances = null;
                custEligibleVASProducts = null;
                assetResponse = null;
                unionList = null;
                eligibleProductVasClass = null;
            }
            return activeProductVasClassList;
        }

        /// <summary>
        /// GetAllvasClassList is used to get 
        /// all the VAS Class and billAccountNumber 
        /// add them to the target list
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static List<List<string>> GetAllvasClassList(OrderRequest request)
        {
            #region ListItems
            List<List<string>> main = new List<List<string>>();
            List<string> vasClassList = new List<string>();
            List<string> vasClassListCancel = new List<string>();
            List<string> vasClassListCreate = new List<string>();
            List<string> billAccountNumberList = new List<string>();
            List<string> orderIdentifierID = new List<string>();
            //List<string> prodCodeList = new List<string>();
            #endregion

            try
            {
                if ((request != null) && (request.Order.OrderItem.Length >= 0))
                {

                    List<OrderItem> createOrderItemList = (from orderItem in request.Order.OrderItem
                                                           where orderItem.Action.Code.Equals("create", StringComparison.OrdinalIgnoreCase)
                                                           select orderItem).ToList();

                    List<OrderItem> cancelOrderItemList = (from orderItem in request.Order.OrderItem
                                                           where orderItem.Action.Code.Equals("cancel", StringComparison.OrdinalIgnoreCase)
                                                           select orderItem).ToList();

                    foreach (OrderItem orderItem in createOrderItemList)
                    {
                        string vasClassValue = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("vasclass", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                        string billAccountNumber = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

                        //if(orderItem.Specification != null && orderItem.Specification.Count() > 0 && orderItem.Specification[0].Identifier != null
                        //    && orderItem.Specification[0].Identifier.Value1 != null && !string.IsNullOrEmpty(orderItem.Specification[0].Identifier.Value1))
                        //    prodCodeList.Add(orderItem.Specification[0].Identifier.Value1);
                        billAccountNumberList.Add(billAccountNumber);
                        vasClassListCreate.Add(vasClassValue);
                        vasClassList.Add(vasClassValue);
                        orderIdentifierID.Add(orderItem.Identifier.Id + "_" + vasClassValue);
                    }

                    foreach (OrderItem orderItem in cancelOrderItemList)
                    {
                        string vasClassValue = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("vasclass", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                        string billAccountNumber = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                        //if (orderItem.Specification != null && orderItem.Specification.Count() > 0 && orderItem.Specification[0].Identifier != null
                        //    && orderItem.Specification[0].Identifier.Value1 != null && !string.IsNullOrEmpty(orderItem.Specification[0].Identifier.Value1))
                        //    prodCodeList.Add(orderItem.Specification[0].Identifier.Value1);
                        billAccountNumberList.Add(billAccountNumber);
                        vasClassListCancel.Add(vasClassValue);
                        vasClassList.Add(vasClassValue);
                        orderIdentifierID.Add(orderItem.Identifier.Id + "_" + vasClassValue);
                    }

                    main.Add(vasClassList);
                    main.Add(vasClassListCancel);
                    main.Add(vasClassListCreate);
                    main.Add(billAccountNumberList);
                    main.Add(orderIdentifierID);
                    //main.Add(prodCodeList);
                }
            }
            catch (Exception ex)
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter Mapping Exception : " + ex.ToString());
                throw ex;

            }
            finally
            {
                //Dispose all the used objects
                vasClassList = null;
                vasClassListCancel = null;
                vasClassListCreate = null;
                billAccountNumberList = null;
                orderIdentifierID = null;
            }
            return main;
        }
        #endregion

        public static List<ProductVasClass> FindAllActiveInactiveProductVasClass(List<string> vasClassList, string identity, string identityDomain, string serviceType, ref ClientServiceInstanceV1[] services, ref List<string> CMPSCodes, ref E2ETransaction e2eData)
        {
            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter FindAllActiveInactiveProductVasClass : " + "vasclass:" + vasClassList[0].ToString() + "identity:" + identity);
            #region attribute list

            List<ProductVasClass> response = new List<ProductVasClass>();
            List<ProductVasClass> mdmEligibleProdVasClassList = new List<ProductVasClass>();
            List<ProductVasClass> cmpsEligibleProdVasClassList = new List<ProductVasClass>();
            List<ProductVasClass> activeProductVasClassList = new List<ProductVasClass>();
            List<string> dnpProductVasClassList = new List<string>();
            List<string> disabledProductVasClassList = new List<string>();
            ProductVasClass eligibleProductVasClass = null;
            const string ScodesReturnedfromCMPS = "ScodesReturnedfromCMPS";
            string DnPServiceTier = null;
            #endregion
            CustomerAssetsResponse assetResponse = null;
            ClientServiceInstanceV1[] serviceInstances = null;
            try
            {
                if (string.Equals(serviceType, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                {
                    serviceInstances = DnpWrapper.getServiceInstanceV1(identity, identityDomain, string.Empty);
                    //Get services linked to migrated BillAccountNumber
                    ClientServiceInstanceV1[] migratedServices = DnpWrapper.GetVASClientServiceInstances(identity, "SECURITY_BAC");

                    if (serviceInstances != null)
                    {
                        if (migratedServices != null)
                            serviceInstances = serviceInstances.Concat(migratedServices).ToArray();
                    }
                    else
                        serviceInstances = migratedServices;

                    services = serviceInstances;

                    //Get DnP active services
                    if (serviceInstances != null)
                    {
                        foreach (ClientServiceInstanceV1 serviceIntance in serviceInstances)
                        {
                            if (serviceIntance.clientServiceInstanceStatus.value.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
                            {
                                if (serviceIntance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"].ToString(), StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!ConfigurationManager.AppSettings["TransitionSwitch"].ToString().Equals("TRANSITION-1", StringComparison.OrdinalIgnoreCase))
                                    {
                                        dnpProductVasClassList.Add(ConfigurationManager.AppSettings["DanteIdentifierInMDM"].ToString());
                                    }
                                }
                                else if (serviceIntance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["BondedWifiIdentifier"].ToString(), StringComparison.OrdinalIgnoreCase))
                                {
                                    dnpProductVasClassList.Add(ConfigurationManager.AppSettings["BondedWifiIdentifierInMDM"].ToString());
                                }
                                else
                                {
                                    if (serviceIntance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["WifiIdentifier"].ToString(), StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (serviceIntance.clientServiceInstanceCharacteristic != null && serviceIntance.clientServiceInstanceCharacteristic.ToList().Exists(sic => sic.name.Equals("WIFI_SERVICE_TIER", StringComparison.OrdinalIgnoreCase) && sic.value != null))
                                        {
                                            DnPServiceTier = serviceIntance.clientServiceInstanceCharacteristic.Where(x => (x.name.Equals("WIFI_SERVICE_TIER", StringComparison.OrdinalIgnoreCase))).FirstOrDefault().value;
                                        }
                                        else
                                            throw new DnpException("Wifi service tier does not exsist for the client" + identity + " in DnP.");
                                    }
                                    dnpProductVasClassList.Add(serviceIntance.clientServiceInstanceIdentifier.value);
                                }
                            }
                            else if (serviceIntance.clientServiceInstanceStatus.value.Equals("DISABLED", StringComparison.OrdinalIgnoreCase))
                            {
                                if (serviceIntance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["MMAService"].ToString(), StringComparison.OrdinalIgnoreCase))
                                    disabledProductVasClassList.Add(serviceIntance.clientServiceInstanceIdentifier.value);
                            }
                        }
                        assetResponse = CmpsWrapper.GetCustomerAssets(identity, ref e2eData);
                        if (assetResponse != null)
                        {

                            cmpsEligibleProdVasClassList = CmpsWrapper.ProductVasClassList(assetResponse, ref CMPSCodes);
                            if (cmpsEligibleProdVasClassList.Count > 0)
                            {
                                e2eData.logMessage(ScodesReturnedfromCMPS, string.Join(",", CMPSCodes.ToArray()));
                                //MDM call with request VAS Class
                                mdmEligibleProdVasClassList = MdmWrapper.getSaaSVASDefs(vasClassList);

                                // get customer eligible list
                                foreach (ProductVasClass mdmVasCls in mdmEligibleProdVasClassList)
                                {
                                    if ((mdmVasCls.VasProductFamily + ":" + mdmVasCls.vasSubType).Equals(ConfigurationManager.AppSettings["WifiIdentifier"], StringComparison.OrdinalIgnoreCase))
                                    {
                                        eligibleProductVasClass = cmpsEligibleProdVasClassList.Where(cmps => (cmps.VasProductFamily.Equals(mdmVasCls.VasProductFamily, StringComparison.OrdinalIgnoreCase) && cmps.vasSubType.Equals(mdmVasCls.vasSubType, StringComparison.OrdinalIgnoreCase))
                                                                               && ((cmps.VasProductFamily + ":" + cmps.vasSubType).Equals(ConfigurationManager.AppSettings["WifiIdentifier"], StringComparison.OrdinalIgnoreCase)) && cmps.VasServiceTier.Equals(mdmVasCls.VasServiceTier, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                        cmpsEligibleProdVasClassList.Remove(eligibleProductVasClass);
                                    }
                                    else if ((mdmVasCls.VasProductFamily + ":" + mdmVasCls.vasSubType).Equals(ConfigurationManager.AppSettings["MMAService"], StringComparison.OrdinalIgnoreCase))
                                    {
                                        eligibleProductVasClass = cmpsEligibleProdVasClassList.Where(cmps => (cmps.VasProductFamily.Equals(mdmVasCls.VasProductFamily, StringComparison.OrdinalIgnoreCase) && cmps.vasSubType.Equals(mdmVasCls.vasSubType, StringComparison.OrdinalIgnoreCase))
                                                                                   && ((cmps.VasProductFamily + ":" + cmps.vasSubType).Equals(ConfigurationManager.AppSettings["MMAService"], StringComparison.OrdinalIgnoreCase)) && cmps.ActivationCardinality.Equals(mdmVasCls.ActivationCardinality)).FirstOrDefault();
                                        cmpsEligibleProdVasClassList.Remove(eligibleProductVasClass);
                                    }
                                    else
                                    {
                                        eligibleProductVasClass = cmpsEligibleProdVasClassList.Where(cmps => cmps.VasProductFamily.Equals(mdmVasCls.VasProductFamily, StringComparison.OrdinalIgnoreCase) && cmps.vasSubType.Equals(mdmVasCls.vasSubType, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                        cmpsEligibleProdVasClassList.Remove(eligibleProductVasClass);
                                    }
                                }

                                foreach (string dnpServiceCode in dnpProductVasClassList)
                                {
                                    // modify list
                                    if (mdmEligibleProdVasClassList.Exists(mpvc => (mpvc.VasProductFamily + ":" + mpvc.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        if (cmpsEligibleProdVasClassList.ToList().Exists(pvc => (pvc.VasProductFamily + ":" + pvc.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            if (dnpServiceCode.Equals(ConfigurationManager.AppSettings["WifiIdentifier"], StringComparison.OrdinalIgnoreCase))
                                            {
                                                /*modify wifi service only when DnP service tier is not equal
                                                 * to eligible service tier */
                                                if (!(cmpsEligibleProdVasClassList.ToList().Where(mpvc => (mpvc.VasProductFamily + ":" + mpvc.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).ToList().
                                                            Any(y => y.VasServiceTier.Equals(DnPServiceTier, StringComparison.OrdinalIgnoreCase))))
                                                {
                                                    cmpsEligibleProdVasClassList.ToList().Where(pvc => (pvc.VasProductFamily + ":" + pvc.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Notes = "modify";
                                                    activeProductVasClassList.Add(cmpsEligibleProdVasClassList.ToList().Where(pvc => (pvc.VasProductFamily + ":" + pvc.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault());
                                                }
                                            }
                                            else if (dnpServiceCode.Equals(ConfigurationManager.AppSettings["CAService"], StringComparison.OrdinalIgnoreCase))
                                            {
                                                /*get highest eligible clod quota list for modify*/
                                                ProductVasClass maxActivationCardinalityProductVASClass = (from vasDefinition in cmpsEligibleProdVasClassList
                                                                                                           where (vasDefinition.VasProductFamily + ":" + vasDefinition.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)                                                                                                           
                                                                                                           orderby vasDefinition.ActivationCardinality descending
                                                                                                           select vasDefinition).FirstOrDefault();
                                                if (maxActivationCardinalityProductVASClass != null)
                                                {
                                                    maxActivationCardinalityProductVASClass.Notes = "modify";
                                                    activeProductVasClassList.Add(maxActivationCardinalityProductVASClass);
                                                }
                                                else
                                                {
                                                    cmpsEligibleProdVasClassList.ToList().Where(pvc => (pvc.VasProductFamily + ":" + pvc.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Notes = "modify";
                                                    activeProductVasClassList.Add(cmpsEligibleProdVasClassList.ToList().Where(pvc => (pvc.VasProductFamily + ":" + pvc.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault());
                                                }
                                            }
                                            else
                                            {
                                                cmpsEligibleProdVasClassList.ToList().Where(pvc => (pvc.VasProductFamily + ":" + pvc.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Notes = "modify";
                                                activeProductVasClassList.Add(cmpsEligibleProdVasClassList.ToList().Where(pvc => (pvc.VasProductFamily + ":" + pvc.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault());
                                            }
                                        }
                                        // cease list
                                        else if (!cmpsEligibleProdVasClassList.ToList().Exists(pvc => (pvc.VasProductFamily + ":" + pvc.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            activeProductVasClassList.Add(mdmEligibleProdVasClassList.ToList().Where(mpvc => (mpvc.VasProductFamily + ":" + mpvc.vasSubType).Equals(dnpServiceCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault());
                                        }
                                    }
                                    //    //added condition to except the Bonded wifi.
                                    //else if (mdmEligibleProdVasClassList.ToList().Exists(mgpv => mgpv.VasProductFamily.Equals("Bonded Wi-Fi", StringComparison.OrdinalIgnoreCase)) && dnpServiceCode.Equals("BONDED_SEAMLESS_WIFI", StringComparison.OrdinalIgnoreCase))
                                    //{
                                    //    cmpsEligibleProdVasClassList.ToList().Where(pvc => pvc.VasProductFamily.Equals("Bonded Wi-Fi", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Notes = "modify";
                                    //    activeProductVasClassList.Add(cmpsEligibleProdVasClassList.ToList().Where(pvc => pvc.VasProductFamily.Equals("Bonded Wi-Fi", StringComparison.OrdinalIgnoreCase)).FirstOrDefault());
                                    //}

                                }
                                foreach (string disabledservice in disabledProductVasClassList)
                                {
                                    if (!cmpsEligibleProdVasClassList.ToList().Exists(pvc => (pvc.VasProductFamily + ":" + pvc.vasSubType).Equals(disabledservice, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        if (mdmEligibleProdVasClassList.ToList().Exists(pvc => (pvc.VasProductFamily + ":" + pvc.vasSubType).Equals(disabledservice, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            mdmEligibleProdVasClassList.ToList().Where(pvc => (pvc.VasProductFamily + ":" + pvc.vasSubType).Equals(disabledservice, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Notes = "disabled";
                                            activeProductVasClassList.Add(mdmEligibleProdVasClassList.ToList().Where(mpvc => (mpvc.VasProductFamily + ":" + mpvc.vasSubType).Equals(disabledservice, StringComparison.OrdinalIgnoreCase)).FirstOrDefault());
                                        }
                                    }
                                }
                                if (string.Equals(serviceType, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                                {
                                    response = activeProductVasClassList;
                                }
                            }
                            else
                            {
                                e2eData.logMessage(ScodesReturnedfromCMPS, string.Join(",", CMPSCodes.ToArray()));
                                throw new CMPSException("There are no definations in MDM for the follwing Scodes returned from CMPS " + string.Join(",", CMPSCodes.ToArray()));
                            }
                    }
                    else
                    {
                        throw new CMPSException("No asset details returned from CMPS ");
                    }
                }
                    else
                    {
                        throw new DnpException("No services mapped against client in DnP");
                    }
                }
                else
                {
                    response = MdmWrapper.getSaaSVASDefs(vasClassList);
                }
            }
            catch (DnpException DnPexception)
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter FindAllActiveInactiveProductVasClass DNP Exception : " + DnPexception.ToString());
                throw DnPexception;
            }
            catch (Exception ex)
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter FindAllActiveInactiveProductVasClass Exception : " + ex.ToString());
                throw ex;
            }
            finally
            {
                activeProductVasClassList = null;
                mdmEligibleProdVasClassList = null;
                cmpsEligibleProdVasClassList = null;
                dnpProductVasClassList = null;
                assetResponse = null;
                eligibleProductVasClass = null;
            }

            return response;
        }

        #region Methods For Asset Transfer

        public static SaaSNS.Order PreparePEOrderForAssetTransfer(List<ProductVasClass> prodVasClassList, OrderRequest request, List<string> nonVASServicesList, Dictionary<string, string> springParameters, string RoleKeyList, Dictionary<string, string> productNameStatus, ref GetBatchProfileV1Response batchProfileResponse)
        {
            SaaSNS.Order response = new BT.SaaS.Core.Shared.Entities.Order();
            try
            {
                //-----Murali----------------------
                const string BACID_IDENTIFER_NAMEPACE = "VAS_BILLINGACCOUNT_ID";
                ClientServiceInstanceV1[] gsiResponse = null;
                string delegateSpring = string.Empty;
                string delegateSport = string.Empty;
                string delegateWifi = string.Empty;
                string rvsid = string.Empty;

                foreach (OrderItem item in request.Order.OrderItem)
                    if (item.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("RVSID", StringComparison.OrdinalIgnoreCase)))
                        rvsid = item.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("RVSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

                string sourceBillAccountId = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("SourceBillAccountNumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

                gsiResponse = DnpWrapper.getServiceInstanceV1(sourceBillAccountId, BACID_IDENTIFER_NAMEPACE, string.Empty);
                if (gsiResponse != null)
                    MSEOSaaSMapper.GetDelegaterolelist(gsiResponse, ref delegateSpring, rvsid, ref delegateSport, ref delegateWifi);

                //------------------------------------



                System.DateTime orderDate = new System.DateTime();
                response.Header.CeaseReason = request.SerializeObject();

                response.Header.OrderKey = request.Order.OrderIdentifier.Value;
                response.Header.ServiceProviderID = Settings1.Default.ConsumerServiceProviderId;
                if (request.Order.OrderItem[0].RequiredDateTime != null && request.Order.OrderItem[0].RequiredDateTime.DateTime1 != null)
                {
                    if (convertDateTime(request.Order.OrderItem[0].RequiredDateTime.DateTime1, ref orderDate))
                    {
                        response.Header.EffectiveDateTime = orderDate;
                    }
                    else
                    {
                        response.Header.EffectiveDateTime = System.DateTime.Now;
                    }
                }
                else
                {
                    response.Header.EffectiveDateTime = System.DateTime.Now;
                }
                response.Header.OrderDateTime = System.DateTime.Now;

                response.Header.Status = BT.SaaS.Core.Shared.Entities.OrderStatusEnum.@new;

                response.Header.Customer = new BT.SaaS.Core.Shared.Entities.Customer();
                response.Header.Customer.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.done;

                response.Header.Users = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.User>(1);
                SaaSNS.User user = new BT.SaaS.Core.Shared.Entities.User();
                user.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.done;
                response.Header.Users.Add(user);
                if (request.Order.OrderItem[0].Action.Code.ToLower() == "modify")
                    response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.modifyService;
                response.ProductOrderItems = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ProductOrderItem>();
                int productorderItemCount = 0;
                if (prodVasClassList != null)
                {
                    foreach (ProductVasClass productVasClass in prodVasClassList)
                    {
                        if (productVasClass.Notes != null)
                        {
                            SaaSNS.ProductOrderItem productOrderItem = PrepareProductOrderItems(productVasClass, productorderItemCount, request, springParameters, RoleKeyList, productNameStatus, ref batchProfileResponse);

                            if (productOrderItem != null)
                            {
                                productorderItemCount++;
                                response.ProductOrderItems.Add(productOrderItem);
                            }
                        }
                    }
                }
                if (nonVASServicesList.Contains(ConfigurationManager.AppSettings["HomeawayScode"].ToLower()))
                {
                    ProductVasClass productVasClass = new ProductVasClass();
                    productVasClass.VasProductFamily = ConfigurationManager.AppSettings["HomeawayScode"];
                    SaaSNS.ProductOrderItem productOrderItem = PrepareProductOrderItems(productVasClass, productorderItemCount, request, springParameters, RoleKeyList, productNameStatus, ref batchProfileResponse);
                    if (productOrderItem != null)
                    {
                        productorderItemCount++;
                        response.ProductOrderItems.Add(productOrderItem);
                    }
                }
                if (nonVASServicesList.Contains(ConfigurationManager.AppSettings["ThomasScode"].ToLower()))
                {
                    ProductVasClass productVasClass = new ProductVasClass();
                    productVasClass.VasProductFamily = ConfigurationManager.AppSettings["ThomasScode"].ToString();
                    SaaSNS.ProductOrderItem productOrderItem = PrepareProductOrderItems(productVasClass, productorderItemCount, request, springParameters, RoleKeyList, productNameStatus, ref batchProfileResponse);
                    if (productOrderItem != null)
                    {
                        productorderItemCount++;
                        response.ProductOrderItems.Add(productOrderItem);
                    }
                }
                if (nonVASServicesList.Contains(ConfigurationManager.AppSettings["SpringScode"].ToLower()))
                {
                    ProductVasClass productVasClass = new ProductVasClass();
                    productVasClass.VasProductFamily = ConfigurationManager.AppSettings["SpringScode"].ToString();
                    SaaSNS.ProductOrderItem productOrderItem = PrepareProductOrderItems(productVasClass, productorderItemCount, request, springParameters, RoleKeyList, productNameStatus, ref batchProfileResponse);

                    //--------adding delegate role attribute----Murali-------------

                    SaaSNS.Attribute rolekey_Delegate_attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                    rolekey_Delegate_attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    rolekey_Delegate_attribute.Name = "BT_Spring_DelegateroleId";
                    rolekey_Delegate_attribute.Value = delegateSpring;
                    productOrderItem.RoleInstances[0].Attributes.Add(rolekey_Delegate_attribute);

                    SaaSNS.Attribute rolekey_Delegatesport_attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                    rolekey_Delegatesport_attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    rolekey_Delegatesport_attribute.Name = "BT_Sport_DelegateroleId";
                    rolekey_Delegatesport_attribute.Value = delegateSport;
                    productOrderItem.RoleInstances[0].Attributes.Add(rolekey_Delegatesport_attribute);

                    SaaSNS.Attribute rolekey_Delegatewifi_attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                    rolekey_Delegatewifi_attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    rolekey_Delegatewifi_attribute.Name = "BT_WIFI_DelegateroleId";
                    rolekey_Delegatewifi_attribute.Value = delegateWifi;
                    productOrderItem.RoleInstances[0].Attributes.Add(rolekey_Delegatewifi_attribute);



                    //----------------------------------
                    if (productOrderItem != null)
                    {
                        productorderItemCount++;
                        response.ProductOrderItems.Add(productOrderItem);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                prodVasClassList = null;
                productNameStatus = null;
                nonVASServicesList = null;
                springParameters = null;
            }
            return response;
        }
        public static SaaSNS.ProductOrderItem PrepareProductOrderItems(ProductVasClass prodVasClass, int productorderItemCount, OrderRequest request, Dictionary<string, string> parameters, string RoleKeyList, Dictionary<string, string> productNameStatus, ref GetBatchProfileV1Response batchProfileResponse)
        {
            bool isTargetBacExist = false;
            bool isSameProfile = false;
            string targetbtoneid = string.Empty;
            string sourceBillAccountId = request.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("SourceBillAccountNumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
            string Status = string.Empty;

            SaaSNS.ProductOrderItem productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
            productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
            productOrderItem.Header.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.notDone;
            productOrderItem.Header.Quantity = "1";
            productOrderItem.XmlIdKey = "PO-" + productorderItemCount.ToString();

            productOrderItem.Header.OrderItemKey = productorderItemCount.ToString();
            productOrderItem.Header.OrderItemId = productorderItemCount.ToString();

            productOrderItem.Header.ProductCode = prodVasClass.VasProductFamily;

            if (productNameStatus.ContainsKey(productOrderItem.Header.ProductCode.ToLower()))
            {
                Status = productNameStatus[productOrderItem.Header.ProductCode.ToLower()];
            }

            System.Collections.Generic.List<string> inputProductDefinitionList = new System.Collections.Generic.List<string>();
            inputProductDefinitionList.Add(productOrderItem.Header.ProductCode);

            System.Collections.Generic.List<ProductDefinition> productDefinition = MdmWrapper.getSaaSProductDefs(inputProductDefinitionList, Settings1.Default.ConsumerServiceProviderId);

            if (productDefinition.Count == 0)
            {
                request.Order.OrderItem[0].Status = Settings1.Default.IgnoredStatus;
                Logger.Debug("Product Code not found in Order : " + request.Order.OrderIdentifier.Value);
            }
            else
            {
                productOrderItem.RoleInstances = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.RoleInstance>(1);
                productOrderItem.ServiceInstances = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ServiceInstance>();
                SaaSNS.RoleInstance roleInstance = new BT.SaaS.Core.Shared.Entities.RoleInstance();

                roleInstance = new BT.SaaS.Core.Shared.Entities.RoleInstance();
                roleInstance.RoleType = "ADMIN";
                roleInstance.XmlIdKey = "RI-" + productorderItemCount.ToString();
                roleInstance.Action = BT.SaaS.Core.Shared.Entities.RoleInstanceActionEnum.add;

                SaaSNS.ServiceInstance serviceInstance = new BT.SaaS.Core.Shared.Entities.ServiceInstance();
                serviceInstance.ServiceRoles = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ServiceRole>();
                SaaSNS.ServiceRole serviceRole = new BT.SaaS.Core.Shared.Entities.ServiceRole();
                serviceRole.roleAttributes = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.Attribute>();
                if (productOrderItem.Header.ProductCode.Equals(ConfigurationManager.AppSettings["SpringScode"], StringComparison.OrdinalIgnoreCase))
                {
                    foreach (MSEO.Instance instance in request.Order.OrderItem.ToList().Where(oi => oi.Specification[0].Identifier.Value1.Equals(ConfigurationManager.AppSettings["SpringScode"], StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Instance)
                    {
                        foreach (MSEO.InstanceCharacteristic instanceCharacteristic in instance.InstanceCharacteristic)
                        {
                            SaaSNS.Attribute attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            attribute.Name = instanceCharacteristic.Name;
                            attribute.Value = instanceCharacteristic.Value;
                            roleInstance.Attributes.Add(attribute);
                        }
                    }
                }
                else
                {
                    foreach (MSEO.Instance instance in request.Order.OrderItem[0].Instance)
                    {
                        foreach (MSEO.InstanceCharacteristic instanceCharacteristic in instance.InstanceCharacteristic)
                        {
                            SaaSNS.Attribute attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            attribute.Name = instanceCharacteristic.Name;
                            attribute.Value = instanceCharacteristic.Value;
                            roleInstance.Attributes.Add(attribute);
                        }
                    }
                }

                if (new string[] { ConfigurationManager.AppSettings["DanteEMailProdCode"], ConfigurationManager.AppSettings["WifiProdCode"], ConfigurationManager.AppSettings["SpringScode"] }.Contains(productOrderItem.Header.ProductCode.ToUpper()))
                {
                    if (batchProfileResponse.getBatchProfileV1Res.clientProfileV1[2] != null)
                    {
                        isTargetBacExist = true;
                        if (batchProfileResponse.getBatchProfileV1Res.clientProfileV1[2].client.clientIdentity != null && batchProfileResponse.getBatchProfileV1Res.clientProfileV1[2].client.clientIdentity.Count() > 0)
                        {
                            if (batchProfileResponse.getBatchProfileV1Res.clientProfileV1[2].client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(sourceBillAccountId, StringComparison.OrdinalIgnoreCase)))
                            {
                                isSameProfile = true;
                            }
                            if (batchProfileResponse.getBatchProfileV1Res.clientProfileV1[2].client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(ci.value)))
                            {
                                targetbtoneid = batchProfileResponse.getBatchProfileV1Res.clientProfileV1[2].client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                            }
                        }
                    }
                    //for Spring wifi
                    if (prodVasClass.VasServiceTier != null && prodVasClass.VasServiceTier.Equals(ConfigurationManager.AppSettings["SpringServiceTier"]))
                    {
                        request.Order.OrderItem.ToList().ForEach(x =>
                        {
                            x.Instance.ToList().ForEach(y =>
                            {
                                if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("SourceBACHasBroadBand", StringComparison.OrdinalIgnoreCase)))
                                {
                                    if (y.InstanceCharacteristic.ToList().Exists(z => z.Name.Equals("SourceBACHasBroadBand", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(z.Value)))
                                    {
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "SourceBACHasBroadBand", Value = y.InstanceCharacteristic.ToList().Where(z => z.Name.Equals("SourceBACHasBroadBand", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                                    }
                                }
                                if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("TargetBACHasBroadBand", StringComparison.OrdinalIgnoreCase)))
                                {
                                    if (y.InstanceCharacteristic.ToList().Exists(z => z.Name.Equals("TargetBACHasBroadBand", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(z.Value)))
                                    {
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "TargetBACHasBroadBand", Value = y.InstanceCharacteristic.ToList().Where(z => z.Name.Equals("TargetBACHasBroadBand", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                                    }
                                }
                                if (!roleInstance.Attributes.ToList().Exists(attr => attr.Name.Equals("PromotionIntegrationId", StringComparison.OrdinalIgnoreCase)))
                                {
                                    if (y.InstanceCharacteristic.ToList().Exists(z => z.Name.Equals("PromotionIntegrationId", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(z.Value)))
                                    {
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "PromotionIntegrationId", Value = y.InstanceCharacteristic.ToList().Where(z => z.Name.Equals("PromotionIntegrationId", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                                    }
                                }
                            });
                        });

                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "vasproductid", Value = prodVasClass.VasProductId, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "IsSpringAssetTransfer", Value = "TRUE", action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "isTargetBACWifiExist", Value = parameters.ToList().Where(x => x.Key.Equals("isTargetBACWifiExist", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "isMultiSim", Value = parameters.ToList().Where(x => x.Key.Equals("isMultiSim", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "TargetRoleKeyList", Value = parameters.ToList().Where(x => x.Key.Equals("TargetRoleKeyList", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                    }
                    if (prodVasClass.VasProductFamily.Equals(ConfigurationManager.AppSettings["SpringScode"], StringComparison.OrdinalIgnoreCase))
                    {
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "IsSpringAssetTransfer", Value = "TRUE", action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                        parameters.ToList().ForEach(x =>
                        {
                            roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = x.Key, Value = x.Value, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                        });
                    }
                    else
                    {
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "ISVASASSETTRANSFER", Value = "TRUE", action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "roleKeyList", Value = RoleKeyList.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "targetBTOneID", Value = targetbtoneid.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                    }
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "isTargetBacExist", Value = isTargetBacExist.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "isSameProfile", Value = isSameProfile.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                }
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "VASSUBTYPE", Value = prodVasClass.vasSubType, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "REASON", Value = "ASSETTRANSFER", action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "Status", Value = Status, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "E2EDATA", Value = request.StandardHeader.E2e.E2EDATA.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });

                serviceInstance.ServiceRoles.Add(serviceRole);
                productOrderItem.ServiceInstances.Add(serviceInstance);
                productOrderItem.RoleInstances.Add(roleInstance);
            }
            return productOrderItem;
        }
        #endregion

        //#region Methods for Home Move for Ref
        //public SaaSNS.Order MapHomeMoveRequest(MSEO.OrderRequest request,ref E2ETransaction e2eData)
        //{
        //    BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Mapping VAS Hoem Move the request");
        //    SaaSNS.Order response = new BT.SaaS.Core.Shared.Entities.Order();
        //    System.DateTime orderDate = new System.DateTime();
        //    try
        //    {
        //        int productorderItemCount = 0;
        //        response.Header.CeaseReason = request.SerializeObject();

        //        response.Header.OrderKey = request.Order.OrderIdentifier.Value;
        //        response.Header.ServiceProviderID = Settings1.Default.ConsumerServiceProviderId;

        //        if (request.Order.OrderItem[0].RequiredDateTime != null && request.Order.OrderItem[0].RequiredDateTime.DateTime1 != null)
        //        {
        //            if (convertDateTime(request.Order.OrderItem[0].RequiredDateTime.DateTime1, ref orderDate))
        //            {
        //                response.Header.EffectiveDateTime = orderDate;
        //            }
        //            else
        //            {
        //                response.Header.EffectiveDateTime = System.DateTime.Now;
        //            }
        //        }
        //        else
        //        {
        //            response.Header.EffectiveDateTime = System.DateTime.Now;
        //        }
        //        response.Header.OrderDateTime = System.DateTime.Now;

        //        response.Header.Status = BT.SaaS.Core.Shared.Entities.OrderStatusEnum.@new;

        //        response.Header.Customer = new BT.SaaS.Core.Shared.Entities.Customer();
        //        response.Header.Customer.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.done;

        //        response.Header.Users = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.User>(1);
        //        SaaSNS.User user = new BT.SaaS.Core.Shared.Entities.User();
        //        user.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.done;
        //        response.Header.Users.Add(user);

        //        if (request.Order.OrderItem[0].Action.Code.ToLower().Equals("modify", StringComparison.OrdinalIgnoreCase))
        //        {
        //            response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.modifyService;
        //        }

        //        response.ProductOrderItems = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ProductOrderItem>(request.Order.OrderItem.Length);

        //        foreach (MSEO.OrderItem orderItem in request.Order.OrderItem)
        //        {
        //            string billAccountID = string.Empty;
        //            string vasClassID = string.Empty;
        //            string cfsid = string.Empty;
        //            List<string> vasClassList = new List<string>();
        //            ClientServiceInstanceV1[] serviceInstances = null;
        //            List<string> CMPSCodes = null;

        //            billAccountID = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(a => a.Name.Equals("billaccountnumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
        //            vasClassID = orderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("vasclass", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
        //            vasClassList.Add(vasClassID);

        //            List<ProductVasClass> productVASClassList = MSEOSaaSMapper.FindAllActiveInactiveProductVasClass(vasClassList, billAccountID, "VAS_BILLINGACCOUNT_ID", string.Empty, ref serviceInstances, ref CMPSCodes,ref e2eData);

        //            foreach (ProductVasClass productVasClass in productVASClassList)
        //            {
        //                if (productVasClass.VasProductFamily.Equals("contentfiltering", StringComparison.OrdinalIgnoreCase))
        //                {
        //                    serviceInstances = DnpWrapper.getServiceInstanceV1(billAccountID, "VAS_BILLINGACCOUNT_ID", ConfigurationManager.AppSettings["CFService"].ToString());

        //                    if (serviceInstances != null && serviceInstances.Count() > 0)
        //                    {
        //                        if (serviceInstances[0].clientServiceInstanceStatus.value.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
        //                        {
        //                            SaaSNS.ProductOrderItem productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
        //                            productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
        //                            productOrderItem.Header.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.notDone;
        //                            productOrderItem.Header.Quantity = "1";
        //                            productOrderItem.XmlIdKey = "PO-" + productorderItemCount.ToString();

        //                            productOrderItem.Header.OrderItemKey = orderItem.Identifier.Id;
        //                            productOrderItem.Header.ProductCode = productVasClass.VasProductFamily;

        //                            System.Collections.Generic.List<string> inputProductDefinitionList = new System.Collections.Generic.List<string>();
        //                            inputProductDefinitionList.Add(productOrderItem.Header.ProductCode);

        //                            System.Collections.Generic.List<ProductDefinition> productDefinition = MdmWrapper.getSaaSProductDefs(inputProductDefinitionList, Settings1.Default.ConsumerServiceProviderId);

        //                            if (productDefinition.Count == 0)
        //                            {
        //                                orderItem.Status = Settings1.Default.IgnoredStatus;
        //                                Logger.Debug("Product Code not found in Order : " + request.Order.OrderIdentifier.Value + " for order item " + orderItem.Identifier.Id);
        //                            }
        //                            else
        //                            {
        //                                productOrderItem.RoleInstances = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.RoleInstance>(1);
        //                                productOrderItem.ServiceInstances = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ServiceInstance>();
        //                                SaaSNS.RoleInstance roleInstance = new BT.SaaS.Core.Shared.Entities.RoleInstance();

        //                                roleInstance = new BT.SaaS.Core.Shared.Entities.RoleInstance();
        //                                roleInstance.Action = BT.SaaS.Core.Shared.Entities.RoleInstanceActionEnum.add;
        //                                roleInstance.RoleType = "ADMIN";
        //                                roleInstance.XmlIdKey = "RI-" + productorderItemCount.ToString();


        //                                SaaSNS.ServiceInstance serviceInstance = new BT.SaaS.Core.Shared.Entities.ServiceInstance();
        //                                serviceInstance.ServiceRoles = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ServiceRole>();
        //                                SaaSNS.ServiceRole serviceRole = new BT.SaaS.Core.Shared.Entities.ServiceRole();
        //                                serviceRole.roleAttributes = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.Attribute>();

        //                                foreach (MSEO.Instance instance in orderItem.Instance)
        //                                {
        //                                    foreach (MSEO.InstanceCharacteristic instanceCharacteristic in instance.InstanceCharacteristic)
        //                                    {
        //                                        if (instanceCharacteristic.Name.Equals("rbsid", StringComparison.OrdinalIgnoreCase))
        //                                        {
        //                                            SaaSNS.Attribute newRBSIDAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
        //                                            newRBSIDAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
        //                                            newRBSIDAttribute.Name = "NEWRBSID";
        //                                            newRBSIDAttribute.Value = instanceCharacteristic.Value;
        //                                            roleInstance.Attributes.Add(newRBSIDAttribute);

        //                                            SaaSNS.Attribute oldRBSIDAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
        //                                            oldRBSIDAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
        //                                            oldRBSIDAttribute.Name = "OLDRBSID";
        //                                            oldRBSIDAttribute.Value = instanceCharacteristic.PreviousValue;
        //                                            roleInstance.Attributes.Add(oldRBSIDAttribute);

        //                                        }
        //                                        else
        //                                        {
        //                                            SaaSNS.Attribute attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
        //                                            attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
        //                                            attribute.Name = instanceCharacteristic.Name;
        //                                            attribute.Value = instanceCharacteristic.Value;
        //                                            roleInstance.Attributes.Add(attribute);
        //                                        }
        //                                    }
        //                                }

        //                                if (!roleInstance.Attributes.Exists(ri => ri.Name.Equals("VasSubType", StringComparison.OrdinalIgnoreCase)))
        //                                {
        //                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { Name = "VasSubType", Value = "Default", action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
        //                                }

        //                                SaaSNS.Attribute promotionChangeAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
        //                                promotionChangeAttr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
        //                                promotionChangeAttr.Name = "ISPROMOTIONCHANGE";
        //                                promotionChangeAttr.Value = "false";
        //                                roleInstance.Attributes.Add(promotionChangeAttr);


        //                                if (serviceInstances[0].clientServiceInstanceCharacteristic != null && serviceInstances[0].clientServiceInstanceCharacteristic.ToList().Exists(srin => srin.name.Equals("cfsid", StringComparison.OrdinalIgnoreCase)))
        //                                {
        //                                    cfsid = serviceInstances[0].clientServiceInstanceCharacteristic.ToList().Where(sic => sic.name.Equals("cfsid", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
        //                                }

        //                                SaaSNS.Attribute cfsidAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
        //                                cfsidAttr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
        //                                cfsidAttr.Name = "CFSID";
        //                                cfsidAttr.Value = cfsid.ToString();
        //                                roleInstance.Attributes.Add(cfsidAttr);

        //                                serviceInstance.ServiceRoles.Add(serviceRole);
        //                                productOrderItem.ServiceInstances.Add(serviceInstance);
        //                                productOrderItem.RoleInstances.Add(roleInstance);
        //                                productOrderItem.RoleInstances[0].Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "E2EDATA", Value = request.StandardHeader.E2e.E2EDATA.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
        //                                productorderItemCount++;
        //                                response.ProductOrderItems.Add(productOrderItem);
        //                            }
        //                        }
        //                        else
        //                        {
        //                            throw new DnpException("Ignored Home Move request as the CONTENTFILTERING service is in " + serviceInstances[0].clientServiceInstanceStatus.value + " state for BAC: " + billAccountID);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        throw new DnpException("Ignored Home Move request as no CONTENTFILTERING service is associated to BillAccount ID :" + billAccountID + " in ProfileStore");
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (MdmException Mdmexception)
        //    {
        //        throw Mdmexception;
        //    }
        //    catch (Exception ex)
        //    {
        //        BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter Mapping VAS Home Move Exception : " + ex.ToString());
        //        throw ex;
        //    }

        //    return response;
        //}

        //#endregion

        #region MapEarlyActivation
        public static SaaSNS.Order MapActivation(MSEO.OrderRequest request, Dictionary<ProductVasClass, string> provisionVASDic)
        {
            List<string> danteProvisionvasClassList = new List<string>();
            List<string> vasClassList = new List<string>();
            List<string> dantevasClassList = new List<string>();

            Dictionary<string, int> dantevasClassPrefDic = new Dictionary<string, int>();
            Dictionary<string, string> dantevasProductIDDic = new Dictionary<string, string>();
            Dictionary<string, string> dantevasProductNameDic = new Dictionary<string, string>();

            int prefInd = 0, productorderItemCount = 0;
            string wifiVASProductID = string.Empty;
            string danteVASProductID = string.Empty;
            string danteProvisionClass = string.Empty;
            string wifiProvisionVASClass = string.Empty;
            string wifiReactivationVASClass = string.Empty;
            string danteReactivationVASClass = string.Empty;
            string danteProductName = string.Empty;
            string bondedWifiProvisionVASClass = string.Empty;
            string bondedWifiVASProductID = string.Empty;
            string wifiSIStatus = string.Empty;
            string bondedwifiSIStatus = string.Empty;
            string danteSIStatus = string.Empty;
            string wifiserviceTier = string.Empty;
            string bondedwifiServiceTier = string.Empty;
            string vasClassID = string.Empty;

            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Mapping VAS request");
            SaaSNS.Order response = new BT.SaaS.Core.Shared.Entities.Order();
            System.DateTime orderDate = new System.DateTime();

            try
            {
                response.Header.CeaseReason = request.SerializeObject();
                response.Header.OrderKey = request.Order.OrderIdentifier.Value;

                //Get the ConsumerServiceProviderId from config
                response.Header.ServiceProviderID = Settings1.Default.ConsumerServiceProviderId;
                //Check if the request is not null and contains atleast once orderitem

                if ((request.Order != null) && (request.Order.OrderItem.Length >= 0))
                {
                    if (request.Order.OrderItem[0].RequiredDateTime != null && request.Order.OrderItem[0].RequiredDateTime.DateTime1 != null)
                    {
                        if (convertDateTime(request.Order.OrderItem[0].RequiredDateTime.DateTime1, ref orderDate))
                        {
                            response.Header.EffectiveDateTime = orderDate;
                        }
                        else
                        {
                            response.Header.EffectiveDateTime = System.DateTime.Now;
                        }
                    }
                    else
                    {
                        response.Header.EffectiveDateTime = System.DateTime.Now;
                    }

                    response.Header.OrderDateTime = System.DateTime.Now;

                    response.Header.Status = BT.SaaS.Core.Shared.Entities.OrderStatusEnum.@new;

                    response.Header.Customer = new BT.SaaS.Core.Shared.Entities.Customer();
                    response.Header.Customer.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.done;

                    response.Header.Users = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.User>(1);
                    SaaSNS.User user = new BT.SaaS.Core.Shared.Entities.User();
                    user.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.done;
                    response.Header.Users.Add(user);

                    response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.provide;

                    response.ProductOrderItems = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ProductOrderItem>(request.Order.OrderItem.Length);
                    SaaSNS.ProductOrderItem productOrderItem = null;


                    foreach (ProductVasClass pvcls in provisionVASDic.Keys)
                    {
                        if (pvcls.VasProductFamily.Equals(ConfigurationManager.AppSettings["WifiProdCode"], StringComparison.OrdinalIgnoreCase))
                        {
                            wifiserviceTier = pvcls.VasServiceTier;
                            wifiVASProductID = pvcls.VasProductId;

                            if ((string.IsNullOrEmpty(provisionVASDic[pvcls])) || (!string.IsNullOrEmpty(provisionVASDic[pvcls]) && provisionVASDic[pvcls].ToLower().Equals("active")))
                            {
                                wifiProvisionVASClass = pvcls.VasClass;
                                wifiSIStatus = provisionVASDic[pvcls];
                            }
                            else if (!string.IsNullOrEmpty(provisionVASDic[pvcls]) && provisionVASDic[pvcls].ToLower().Equals("ceased"))
                            {
                                wifiReactivationVASClass = pvcls.VasClass;
                                wifiSIStatus = provisionVASDic[pvcls];
                            }

                        }
                        else if (pvcls.VasProductFamily.Equals(ConfigurationManager.AppSettings["BondedWifiProdCode"], StringComparison.OrdinalIgnoreCase))
                        {
                            bondedwifiServiceTier = pvcls.VasServiceTier;
                            bondedWifiVASProductID = pvcls.VasProductId;

                            if ((string.IsNullOrEmpty(provisionVASDic[pvcls])) || (!string.IsNullOrEmpty(provisionVASDic[pvcls]) && provisionVASDic[pvcls].ToLower().Equals("active")))
                            {
                                bondedWifiProvisionVASClass = pvcls.VasClass;
                                bondedwifiSIStatus = provisionVASDic[pvcls];
                            }
                        }
                        else if (pvcls.VasProductFamily.Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"], StringComparison.OrdinalIgnoreCase))
                        {
                            if (string.IsNullOrEmpty(provisionVASDic[pvcls]) || (!string.IsNullOrEmpty(provisionVASDic[pvcls]) && provisionVASDic[pvcls].Equals("pending", StringComparison.OrdinalIgnoreCase)))
                            {
                                danteSIStatus = provisionVASDic[pvcls];
                                foreach (OrderItem vasOrderItem in request.Order.OrderItem)
                                {
                                    vasClassID = vasOrderItem.Instance[0].InstanceCharacteristic.ToList().Where(insChar => insChar.Name.Equals("vasclass", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                    vasClassList.Add(vasClassID);
                                }
                                if ((vasClassList != null) && (vasClassList.Count > 0))
                                {
                                    List<ProductVasClass> vasDefinitionList = MdmWrapper.getSaaSVASDefs(vasClassList);
                                    foreach (ProductVasClass pvc1 in vasDefinitionList)
                                    {
                                        if (pvc1.VasProductFamily.Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"], StringComparison.OrdinalIgnoreCase))
                                        {
                                            dantevasClassList.Add(pvc1.VasClass);
                                            dantevasClassPrefDic.Add(pvc1.VasClass, pvc1.PreferenceIndicator);
                                            dantevasProductIDDic.Add(pvc1.VasClass, pvc1.VasProductId);
                                        }
                                    }
                                }

                                foreach (OrderItem oi in request.Order.OrderItem)
                                {
                                    string vasClas = oi.Instance[0].InstanceCharacteristic.ToList().Where(v => v.Name.Equals("vasclass", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

                                    if (dantevasClassList.Contains(vasClas))
                                    {
                                        if (oi.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("emailname", StringComparison.OrdinalIgnoreCase)))
                                        {
                                            danteProvisionvasClassList.Add(vasClas);
                                        }
                                    }
                                }

                                foreach (string danteClass in danteProvisionvasClassList)
                                {
                                    if (dantevasClassPrefDic[danteClass] > prefInd)
                                    {
                                        prefInd = dantevasClassPrefDic[danteClass];
                                        danteProvisionClass = danteClass;
                                        danteVASProductID = dantevasProductIDDic[danteClass];
                                    }
                                }
                            }
                            else
                            {
                                danteReactivationVASClass = pvcls.VasClass;
                                danteProductName = pvcls.VasProductName;
                                danteSIStatus = provisionVASDic[pvcls];
                            }
                        }
                    }
                    foreach (OrderItem orditem in request.Order.OrderItem)
                    {
                        string vasClass = orditem.Instance[0].InstanceCharacteristic.ToList().Where(v => v.Name.Equals("vasclass", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;

                        if (vasClass.Equals(wifiProvisionVASClass) || vasClass.Equals(wifiReactivationVASClass))
                        {
                            if (ConfigurationManager.AppSettings["WifiSwitch"].Equals("ON", StringComparison.OrdinalIgnoreCase))
                            {
                                //wifi activation
                                if (vasClass.Equals(wifiProvisionVASClass))
                                {
                                    if ((response.ProductOrderItems.ToList().Count == 0) || (response.ProductOrderItems.ToList().Count > 0 && !response.ProductOrderItems.ToList().Exists(poi => poi.Header.ProductCode.ToString().Equals(ConfigurationManager.AppSettings["WifiProdCode"]))))
                                    {
                                        productOrderItem = prepareProductOrderItemsEarlyActivation(orditem, response, ConfigurationManager.AppSettings["WifiProdCode"], productorderItemCount, wifiVASProductID, wifiserviceTier, wifiSIStatus);
                                    }
                                }
                                else if (vasClass.Equals(wifiReactivationVASClass))
                                // wifi reactivation
                                {
                                    if ((response.ProductOrderItems.ToList().Count == 0) || (response.ProductOrderItems.ToList().Count > 0 && !response.ProductOrderItems.ToList().Exists(poi => poi.Header.ProductCode.ToString().Equals(ConfigurationManager.AppSettings["WifiProdCode"]))))
                                    {
                                        productOrderItem = prepareProductOrderItemsBBReactivation(orditem, wifiSIStatus, response, ConfigurationManager.AppSettings["WifiProdCode"], productorderItemCount, wifiVASProductID, wifiserviceTier);
                                    }
                                }
                                if (productOrderItem != null)
                                {
                                    productOrderItem.RoleInstances[0].Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "E2EDATA", Value = request.StandardHeader.E2e.E2EDATA.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                                    productorderItemCount++;
                                    response.ProductOrderItems.Add(productOrderItem);
                                    productOrderItem = null;
                                }
                            }
                        }
                        if (vasClass.Equals(bondedWifiProvisionVASClass))
                        {
                            //Bondedwifi activation
                            if (vasClass.Equals(bondedWifiProvisionVASClass))
                            {
                                if ((response.ProductOrderItems.ToList().Count == 0) || (response.ProductOrderItems.ToList().Count > 0 && !response.ProductOrderItems.ToList().Exists(poi => poi.Header.ProductCode.ToString().Equals(ConfigurationManager.AppSettings["BondedWifiProdCode"]))))
                                {
                                    productOrderItem = prepareProductOrderItemsEarlyActivation(orditem, response, ConfigurationManager.AppSettings["BondedWifiProdCode"], productorderItemCount, bondedWifiVASProductID, bondedwifiServiceTier, bondedwifiSIStatus);
                                }
                            }

                            if (productOrderItem != null)
                            {
                                productOrderItem.RoleInstances[0].Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "E2EDATA", Value = request.StandardHeader.E2e.E2EDATA.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                                productorderItemCount++;
                                response.ProductOrderItems.Add(productOrderItem);
                                productOrderItem = null;
                            }

                        }
                        if (vasClass.Equals(danteProvisionClass) || vasClass.Equals(danteReactivationVASClass))
                        {
                            string orderType = AcceptedEmailOrder(orditem, danteProductName);
                            if (!string.IsNullOrEmpty(orderType) && orderType.Equals("Activation", StringComparison.OrdinalIgnoreCase))
                            {
                                if (vasClass.Equals(danteProvisionClass))
                                {
                                    if ((response.ProductOrderItems.ToList().Count == 0) || (response.ProductOrderItems.ToList().Count > 0 && !response.ProductOrderItems.ToList().Exists(poi => poi.Header.ProductCode.ToString().Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"]))))
                                    {
                                        productOrderItem = prepareProductOrderItemsEarlyActivation(orditem, response, ConfigurationManager.AppSettings["DanteEMailProdCode"], productorderItemCount, danteVASProductID, string.Empty, danteSIStatus);
                                    }
                                }
                                else if (vasClass.Equals(danteReactivationVASClass))
                                {
                                    if ((response.ProductOrderItems.ToList().Count == 0) || (response.ProductOrderItems.ToList().Count > 0 && !response.ProductOrderItems.ToList().Exists(poi => poi.Header.ProductCode.ToString().Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"]))))
                                    {
                                        productOrderItem = prepareProductOrderItemsBBReactivation(orditem, danteSIStatus, response, ConfigurationManager.AppSettings["DanteEMailProdCode"], productorderItemCount, danteVASProductID, string.Empty);
                                    }
                                }
                                if (productOrderItem != null)
                                {
                                    productOrderItem.RoleInstances[0].Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "E2EDATA", Value = request.StandardHeader.E2e.E2EDATA.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                                    productorderItemCount++;
                                    response.ProductOrderItems.Add(productOrderItem);
                                    productOrderItem = null;
                                }
                            }
                        }
                    }
                }
            }
            catch (MdmException Mdmexception)
            {
                throw Mdmexception;
            }
            catch (DnpException DnPexception)
            {
                throw DnPexception;
            }
            finally
            {
                danteProvisionvasClassList = null;
                vasClassList = null;
                dantevasClassList = null;
                dantevasClassPrefDic.Clear();
                dantevasProductIDDic.Clear();
                dantevasProductNameDic.Clear();
            }

            return response;
        }
        #endregion

        #region prepareProductOrderItemsEarlyActivation
        public static SaaSNS.ProductOrderItem prepareProductOrderItemsEarlyActivation(OrderItem orditem, SaaSNS.Order response, string productName, int productorderItemCount, string vasProductID, string srvcTier, string SIStatus)
        {
            string BACID_IDENTIFER_NAMEPACE = "VAS_BILLINGACCOUNT_ID";
            string emailName = string.Empty;
            string bakId = string.Empty;
            string ServiceInstanceKey = string.Empty;
            bool isReserved = false;
            bool serviceInstanceExists = false;
            bool isOFSDanteProvide = true;
            bool isExistingAccount = false;
            bool isAHTDone = false;
            string BtOneId = string.Empty;
            string emailSupplier = "MX";
            ClientServiceInstanceV1 emailServiceInstance = null;
            //ClientIdentity emailClientIdentity = new ClientIdentity();
            SaaSNS.ProductOrderItem productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
            productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
            productOrderItem.Header.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.notDone;
            productOrderItem.Header.Quantity = "1";
            productOrderItem.XmlIdKey = "PO-" + productorderItemCount.ToString();

            productOrderItem.Header.OrderItemKey = productorderItemCount.ToString();
            productOrderItem.Header.ProductCode = productName;

            System.Collections.Generic.List<string> inputProductDefinitionList = new System.Collections.Generic.List<string>();
            inputProductDefinitionList.Add(productOrderItem.Header.ProductCode);

            System.Collections.Generic.List<ProductDefinition> productDefinition = MdmWrapper.getSaaSProductDefs(inputProductDefinitionList, Settings1.Default.ConsumerServiceProviderId);

            if (productDefinition.Count == 0)
            {
                orditem.Status = Settings1.Default.IgnoredStatus;
                //Logger.Debug("Product Code not found in Order : " + request.Order.OrderIdentifier.Value);
            }
            else
            {
                productOrderItem.RoleInstances = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.RoleInstance>(1);
                productOrderItem.ServiceInstances = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ServiceInstance>();
                SaaSNS.RoleInstance roleInstance = new BT.SaaS.Core.Shared.Entities.RoleInstance();

                roleInstance = new BT.SaaS.Core.Shared.Entities.RoleInstance();
                roleInstance.RoleType = "ADMIN";
                roleInstance.XmlIdKey = "RI-" + productorderItemCount.ToString();
                roleInstance.Action = BT.SaaS.Core.Shared.Entities.RoleInstanceActionEnum.add;

                SaaSNS.ServiceInstance serviceInstance = new BT.SaaS.Core.Shared.Entities.ServiceInstance();
                serviceInstance.ServiceRoles = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ServiceRole>();
                SaaSNS.ServiceRole serviceRole = new BT.SaaS.Core.Shared.Entities.ServiceRole();
                serviceRole.roleAttributes = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.Attribute>();

                foreach (MSEO.Instance instance in orditem.Instance)
                {
                    foreach (MSEO.InstanceCharacteristic instanceCharacteristic in instance.InstanceCharacteristic)
                    {
                        SaaSNS.Attribute attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                        attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        attribute.Name = instanceCharacteristic.Name;
                        attribute.Value = instanceCharacteristic.Value;
                        roleInstance.Attributes.Add(attribute);
                    }
                }

                if (orditem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("billaccountnumber")) && !string.IsNullOrEmpty(orditem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value))
                {
                    bakId = orditem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value;
                }

                GetClientProfileV1Res getprofileResponse1 = new GetClientProfileV1Res();
                BT.SaaS.IspssAdapter.Dnp.ClientIdentity emailClientIdentity = null;
                if (!String.IsNullOrEmpty(bakId))
                {
                    getprofileResponse1 = DnpWrapper.GetClientProfileV1(bakId, BACID_IDENTIFER_NAMEPACE);

                    if (getprofileResponse1 != null && getprofileResponse1.clientProfileV1 != null)
                    {
                        isExistingAccount = true;
                        BT.SaaS.IspssAdapter.Dnp.ClientIdentity bacClientIdentity = getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(bakId, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                        if (getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Exists(i => i.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                            BtOneId = getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;

                        if (bacClientIdentity.clientIdentityValidation != null)
                            isAHTDone = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));

                    }
                }
                if (productOrderItem.Header.ProductCode.Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"], StringComparison.OrdinalIgnoreCase))
                {
                    if (orditem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("emailname")) && !string.IsNullOrEmpty(orditem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailname")).FirstOrDefault().Value))
                    {
                        emailName = orditem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailname")).FirstOrDefault().Value;
                    }
                    if (string.IsNullOrEmpty(emailName))
                        return null;
                    getprofileResponse1 = DnpWrapper.GetClientProfileV1(emailName, "BTIEMAILID");
                    if (getprofileResponse1.clientProfileV1 != null && getprofileResponse1.clientProfileV1.client != null && getprofileResponse1.clientProfileV1.client.clientIdentity != null && getprofileResponse1.clientProfileV1.client.clientIdentity.Count() > 0)
                    {
                        if (getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(emailName, StringComparison.OrdinalIgnoreCase)))
                        {
                            emailClientIdentity = new BT.SaaS.IspssAdapter.Dnp.ClientIdentity();
                            emailClientIdentity = getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(emailName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault<BT.SaaS.IspssAdapter.Dnp.ClientIdentity>();
                            if (ConfigurationManager.AppSettings["BTIEmailIdentityStatus"].Split(',').Contains(emailClientIdentity.clientIdentityStatus.value.ToLower()))
                            {
                                isReserved = true;
                            }
                            if (getprofileResponse1.clientProfileV1.clientServiceInstanceV1 != null && getprofileResponse1.clientProfileV1.clientServiceInstanceV1.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && csi.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(bakId, StringComparison.OrdinalIgnoreCase))))
                            {
                                emailServiceInstance = (from si in getprofileResponse1.clientProfileV1.clientServiceInstanceV1
                                                        where si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(bakId, StringComparison.OrdinalIgnoreCase))
                                                        select si).FirstOrDefault();
                            }
                        }
                    }

                    SaaSNS.Attribute is_reserved = new BT.SaaS.Core.Shared.Entities.Attribute();
                    is_reserved.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    is_reserved.Name = "ISRESERVED";
                    is_reserved.Value = isReserved.ToString();
                    roleInstance.Attributes.Add(is_reserved);

                    SaaSNS.Attribute is_AHTClient = new BT.SaaS.Core.Shared.Entities.Attribute();
                    is_AHTClient.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    is_AHTClient.Name = "ISAHTCLIENT";
                    is_AHTClient.Value = isAHTDone.ToString();
                    roleInstance.Attributes.Add(is_AHTClient);

                    SaaSNS.Attribute is_OFSProvide = new BT.SaaS.Core.Shared.Entities.Attribute();
                    is_OFSProvide.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    is_OFSProvide.Name = "ISOFSPROVIDE";
                    is_OFSProvide.Value = isOFSDanteProvide.ToString();
                    roleInstance.Attributes.Add(is_OFSProvide);

                    SaaSNS.Attribute isExistingAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                    isExistingAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    isExistingAttribute.Name = "ISEXISTINGACCOUNT";
                    isExistingAttribute.Value = isExistingAccount.ToString();
                    roleInstance.Attributes.Add(isExistingAttribute);

                    SaaSNS.Attribute siBacNumber = new BT.SaaS.Core.Shared.Entities.Attribute();
                    siBacNumber.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    siBacNumber.Name = "SIBACNUMBER";
                    siBacNumber.Value = bakId;
                    roleInstance.Attributes.Add(siBacNumber);

                    SaaSNS.Attribute reason = new BT.SaaS.Core.Shared.Entities.Attribute();
                    reason.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    reason.Name = "REASON";
                    reason.Value = "MailVASProvision";
                    roleInstance.Attributes.Add(reason);

                    SaaSNS.Attribute isearlyactivation = new BT.SaaS.Core.Shared.Entities.Attribute();
                    isearlyactivation.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    isearlyactivation.Name = "isearlyactivation";
                    isearlyactivation.Value = "TRUE";
                    roleInstance.Attributes.Add(isearlyactivation);

                    if (SIStatus.Equals("pending", StringComparison.OrdinalIgnoreCase))
                    {
                        SaaSNS.Attribute isShowUserAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
                        isShowUserAttr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        isShowUserAttr.Name = "ISSHOWUSERNEED";
                        isShowUserAttr.Value = "true";
                        roleInstance.Attributes.Add(isShowUserAttr);

                        #region BTRCE-108426/BTRCE-108471
                        if (ConfigurationManager.AppSettings["VASMigrationSwitch"].Equals("OFF", StringComparison.OrdinalIgnoreCase))
                        {
                            if (emailServiceInstance != null)
                            {
                                DanteRequestProcessor.GetEmailSupplier(true, ref emailSupplier, emailServiceInstance);
                            }
                        }

                        if (roleInstance.Attributes.ToList().Exists(ic => ic.Name.Equals("emailsupplier", StringComparison.OrdinalIgnoreCase)))
                        {
                            roleInstance.Attributes.ToList().Where(roleInstAttrb => roleInstAttrb.Name.Equals("emailsupplier", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = emailSupplier;
                        }
                        else
                        {
                            SaaSNS.Attribute isEmailSupplier = new BT.SaaS.Core.Shared.Entities.Attribute();
                            isEmailSupplier.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            isEmailSupplier.Name = "EmailSupplier";
                            isEmailSupplier.Value = emailSupplier;
                            roleInstance.Attributes.Add(isEmailSupplier);
                        }


                        if (emailSupplier.Equals(ConfigurationManager.AppSettings["OWM_Email_Supplier"], StringComparison.OrdinalIgnoreCase))
                        {
                            if (roleInstance.Attributes.ToList().Exists(roleInstAttrb => roleInstAttrb.Name.Equals("PASSWORD", StringComparison.OrdinalIgnoreCase)))
                            {
                                roleInstance.Attributes.ToList().Where(roleInstAttrb => roleInstAttrb.Name.Equals("PASSWORD", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = emailClientIdentity.clientCredential[0].credentialValue;
                            }
                            else
                            {
                                SaaSNS.Attribute passwordAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                passwordAttr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                passwordAttr.Name = "PASSWORD";
                                passwordAttr.Value = emailClientIdentity.clientCredential[0].credentialValue;
                                roleInstance.Attributes.Add(passwordAttr);
                            }

                            SaaSNS.Attribute pwdHashSaltAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
                            pwdHashSaltAttr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            pwdHashSaltAttr.Name = "PWDHASHSALT";
                            pwdHashSaltAttr.Value = emailClientIdentity.clientIdentityValidation.ToList().Where(civ => civ.name.Equals("SALT_VALUE")).FirstOrDefault().value;
                            roleInstance.Attributes.Add(pwdHashSaltAttr);

                        }

                        #endregion
                    }
                    else
                    {
                        // code to check for emailsupplier BTRCE-108426/BTRCE-108471
                        if (ConfigurationManager.AppSettings["VASMigrationSwitch"].Equals("OFF", StringComparison.OrdinalIgnoreCase))
                        {
                            if (orditem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("emailsupplier", StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrEmpty(orditem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailsupplier", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value))
                            {
                                emailSupplier = roleInstance.Attributes.ToList().Where(roleInstAttrb => roleInstAttrb.Name.Equals("EMAILSUPPLIER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                            }
                            else
                            {
                                emailSupplier = ConfigurationManager.AppSettings["VASEmailSupplierSwitch"];
                                SaaSNS.Attribute isEmailSupplier = new BT.SaaS.Core.Shared.Entities.Attribute();
                                isEmailSupplier.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                isEmailSupplier.Name = "EmailSupplier";
                                isEmailSupplier.Value = emailSupplier;
                                roleInstance.Attributes.Add(isEmailSupplier);
                            }
                        }
                        else
                        {
                            if (orditem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("emailsupplier", StringComparison.OrdinalIgnoreCase)))
                                roleInstance.Attributes.ToList().Where(roleInstAttrb => roleInstAttrb.Name.Equals("EMAILSUPPLIER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = emailSupplier;
                            else
                            {
                                SaaSNS.Attribute isEmailSupplier = new BT.SaaS.Core.Shared.Entities.Attribute();
                                isEmailSupplier.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                isEmailSupplier.Name = "EmailSupplier";
                                isEmailSupplier.Value = emailSupplier;
                                roleInstance.Attributes.Add(isEmailSupplier);
                            }
                        }
                    }

                    SaaSNS.Attribute MAILCLASS = new BT.SaaS.Core.Shared.Entities.Attribute();
                    MAILCLASS.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    MAILCLASS.Name = "MAILCLASS";
                    if (emailSupplier.Equals(ConfigurationManager.AppSettings["OWM_Email_Supplier"], StringComparison.OrdinalIgnoreCase))
                        MAILCLASS.Value = "ENABLED";
                    else
                        MAILCLASS.Value = "ACTIVE";
                    roleInstance.Attributes.Add(MAILCLASS);

                    SaaSNS.Attribute isServiceInstanceExists = new BT.SaaS.Core.Shared.Entities.Attribute();
                    isServiceInstanceExists.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    isServiceInstanceExists.Name = "SERVICEINSTANCEEXISTS";
                    isServiceInstanceExists.Value = serviceInstanceExists.ToString();
                    roleInstance.Attributes.Add(isServiceInstanceExists);

                    ServiceInstanceKey = emailName;
                }
                else if (productOrderItem.Header.ProductCode.Equals(ConfigurationManager.AppSettings["WifiProdCode"], StringComparison.OrdinalIgnoreCase))
                {
                    ClientServiceInstanceV1[] res = DnpWrapper.getServiceInstanceV1(bakId, "VAS_BILLINGACCOUNT_ID", "");
                    if (res != null)
                    {
                        foreach (ClientServiceInstanceV1 srvcInstance in res)
                        {
                            if (srvcInstance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["WifiIdentifier"].ToString(), StringComparison.OrdinalIgnoreCase))
                            {
                                if (srvcInstance.clientServiceInstanceCharacteristic.ToList().Exists(sic => sic.name.Equals("WIFI_BROADBAND_FLAG", StringComparison.OrdinalIgnoreCase)))
                                {
                                    SaaSNS.Attribute attrBBflag = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attrBBflag.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attrBBflag.Name = "isBBFlagExists";
                                    attrBBflag.Value = "Y";
                                    roleInstance.Attributes.Add(attrBBflag);
                                }
                            }
                        }

                        if(orditem.Action.Code.Equals("Create",StringComparison.OrdinalIgnoreCase))
                        {
                            if (res.Count() > 0)
                            {
                                //NAYANAGL-61637
                                if (res.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals("MANAGE_ACCOUNT", StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceCharacteristic != null && si.clientServiceInstanceCharacteristic.ToList().Exists(cschar => cschar.name.Equals("BACORGANISATIONNAME", StringComparison.OrdinalIgnoreCase) && cschar.value.StartsWith("EE", StringComparison.OrdinalIgnoreCase))))
                                {
                                    SaaSNS.Attribute isnayanwififlag = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    isnayanwififlag.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    isnayanwififlag.Name = "ISNAYANWIFI";
                                    isnayanwififlag.Value = true.ToString();
                                    roleInstance.Attributes.Add(isnayanwififlag);
                                }
                            }
                        }
                    }

                    SaaSNS.Attribute ceaseReason = new BT.SaaS.Core.Shared.Entities.Attribute();
                    ceaseReason.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    ceaseReason.Name = "CEASEREASON";
                    ceaseReason.Value = " ";
                    roleInstance.Attributes.Add(ceaseReason);

                    SaaSNS.Attribute isAHTAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                    isAHTAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    isAHTAttribute.Name = "ISAHTDONE";
                    isAHTAttribute.Value = isAHTDone.ToString();
                    roleInstance.Attributes.Add(isAHTAttribute);

                    SaaSNS.Attribute btOneIDAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                    btOneIDAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    btOneIDAttribute.Name = "BtOneId";
                    btOneIDAttribute.Value = BtOneId.ToString();
                    roleInstance.Attributes.Add(btOneIDAttribute);

                    if (SIStatus.Equals("active", StringComparison.OrdinalIgnoreCase))
                    {
                        //BB provision after Spring VAS Provision
                        SaaSNS.Attribute hubStatusAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                        hubStatusAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        hubStatusAttribute.Name = "HUB_WIFI_STATUS";
                        hubStatusAttribute.Value = "OPTED_IN";
                        roleInstance.Attributes.Add(hubStatusAttribute);

                        SaaSNS.Attribute isWifiExistAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                        isWifiExistAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        isWifiExistAttribute.Name = "isWifiExist";
                        isWifiExistAttribute.Value = "TRUE";
                        roleInstance.Attributes.Add(isWifiExistAttribute);
                    }
                    else
                    {
                        //New BB provision or SPring VAS Provision
                        SaaSNS.Attribute srvcTierAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                        srvcTierAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        srvcTierAttribute.Name = "WIFI_SERVICE_TIER";
                        srvcTierAttribute.Value = srvcTier.ToString();
                        roleInstance.Attributes.Add(srvcTierAttribute);

                        if (srvcTier.Equals(ConfigurationManager.AppSettings["BBWIFIServiceTier"], StringComparison.OrdinalIgnoreCase))
                        {
                            SaaSNS.Attribute hubStatusAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            hubStatusAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            hubStatusAttribute.Name = "HUB_WIFI_STATUS";
                            hubStatusAttribute.Value = "OPTED_IN";
                            roleInstance.Attributes.Add(hubStatusAttribute);
                        }
                        else
                        {
                            SaaSNS.Attribute hubStatusAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            hubStatusAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            hubStatusAttribute.Name = "HUB_WIFI_STATUS";
                            hubStatusAttribute.Value = "OPTED_OUT";
                            roleInstance.Attributes.Add(hubStatusAttribute);
                        }
                    }
                    if (srvcTier.Equals(ConfigurationManager.AppSettings["BBWIFIServiceTier"], StringComparison.OrdinalIgnoreCase))
                    {
                        SaaSNS.Attribute isearlyactivation = new BT.SaaS.Core.Shared.Entities.Attribute();
                        isearlyactivation.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        isearlyactivation.Name = "isearlyactivation";
                        isearlyactivation.Value = "TRUE";
                        roleInstance.Attributes.Add(isearlyactivation);
                    }
                }

                else if (productOrderItem.Header.ProductCode.Equals(ConfigurationManager.AppSettings["BondedWifiProdCode"], StringComparison.OrdinalIgnoreCase))
                {
                    ClientServiceInstanceV1[] res = DnpWrapper.getServiceInstanceV1(bakId, "VAS_BILLINGACCOUNT_ID", "");
                    if (res != null)
                    {
                        foreach (ClientServiceInstanceV1 srvcInstance in res)
                        {
                            if (srvcInstance.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["WifiIdentifier"].ToString(), StringComparison.OrdinalIgnoreCase))
                            {
                                if (srvcInstance.clientServiceInstanceCharacteristic.ToList().Exists(sic => sic.name.Equals("WIFI_BROADBAND_FLAG", StringComparison.OrdinalIgnoreCase)))
                                {
                                    SaaSNS.Attribute attrBBflag = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attrBBflag.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attrBBflag.Name = "isBBFlagExists";
                                    attrBBflag.Value = "Y";
                                    roleInstance.Attributes.Add(attrBBflag);
                                }
                            }
                        }
                    }

                    SaaSNS.Attribute ceaseReason = new BT.SaaS.Core.Shared.Entities.Attribute();
                    ceaseReason.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    ceaseReason.Name = "CEASEREASON";
                    ceaseReason.Value = " ";
                    roleInstance.Attributes.Add(ceaseReason);

                    SaaSNS.Attribute isAHTAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                    isAHTAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    isAHTAttribute.Name = "ISAHTDONE";
                    isAHTAttribute.Value = isAHTDone.ToString();
                    roleInstance.Attributes.Add(isAHTAttribute);

                    SaaSNS.Attribute btOneIDAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                    btOneIDAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    btOneIDAttribute.Name = "BtOneId";
                    btOneIDAttribute.Value = BtOneId.ToString();
                    roleInstance.Attributes.Add(btOneIDAttribute);

                    if (SIStatus.Equals("active", StringComparison.OrdinalIgnoreCase))
                    {
                        //BB provision after Spring VAS Provision
                        SaaSNS.Attribute hubStatusAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                        hubStatusAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        hubStatusAttribute.Name = "HUB_WIFI_STATUS";
                        hubStatusAttribute.Value = "OPTED_IN";
                        roleInstance.Attributes.Add(hubStatusAttribute);

                        SaaSNS.Attribute isWifiExistAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                        isWifiExistAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        isWifiExistAttribute.Name = "isWifiExist";
                        isWifiExistAttribute.Value = "TRUE";
                        roleInstance.Attributes.Add(isWifiExistAttribute);
                    }
                    else
                    {
                        //New BB provision or SPring VAS Provision
                        SaaSNS.Attribute srvcTierAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                        srvcTierAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        srvcTierAttribute.Name = "WIFI_SERVICE_TIER";
                        srvcTierAttribute.Value = srvcTier.ToString();
                        roleInstance.Attributes.Add(srvcTierAttribute);

                        if (srvcTier.Equals(ConfigurationManager.AppSettings["BBWIFIServiceTier"], StringComparison.OrdinalIgnoreCase))
                        {
                            SaaSNS.Attribute hubStatusAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            hubStatusAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            hubStatusAttribute.Name = "HUB_WIFI_STATUS";
                            hubStatusAttribute.Value = "OPTED_IN";
                            roleInstance.Attributes.Add(hubStatusAttribute);
                        }
                        else
                        {
                            SaaSNS.Attribute hubStatusAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            hubStatusAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            hubStatusAttribute.Name = "HUB_WIFI_STATUS";
                            hubStatusAttribute.Value = "OPTED_OUT";
                            roleInstance.Attributes.Add(hubStatusAttribute);
                        }
                    }
                    if (srvcTier.Equals(ConfigurationManager.AppSettings["BBWIFIServiceTier"], StringComparison.OrdinalIgnoreCase))
                    {
                        SaaSNS.Attribute isearlyactivation = new BT.SaaS.Core.Shared.Entities.Attribute();
                        isearlyactivation.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        isearlyactivation.Name = "isearlyactivation";
                        isearlyactivation.Value = "TRUE";
                        roleInstance.Attributes.Add(isearlyactivation);
                    }
                }


                SaaSNS.Attribute VASProduct_ID = new BT.SaaS.Core.Shared.Entities.Attribute();
                VASProduct_ID.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                VASProduct_ID.Name = "VASPRODUCTID";
                VASProduct_ID.Value = vasProductID.ToString();
                roleInstance.Attributes.Add(VASProduct_ID);



                serviceInstance.ServiceRoles.Add(serviceRole);
                productOrderItem.ServiceInstances.Add(serviceInstance);
                productOrderItem.RoleInstances.Add(roleInstance);

            }
            return productOrderItem;
        }

        #endregion

        public static bool IsAHTDONE(GetClientProfileV1Res bacProfileResponse, string BakId)
        {
            bool isAHTDone = false;
            if (bacProfileResponse.clientProfileV1 != null && bacProfileResponse.clientProfileV1.client != null && bacProfileResponse.clientProfileV1.client.clientIdentity != null)
            {
                if (bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(i => i.managedIdentifierDomain.value.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.OrdinalIgnoreCase) && i.value.Equals(BakId, StringComparison.OrdinalIgnoreCase)))
                {
                    BT.SaaS.IspssAdapter.Dnp.ClientIdentity bacClientIdentity = bacProfileResponse.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.OrdinalIgnoreCase) && i.value.Equals(BakId, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (bacClientIdentity.clientIdentityValidation != null)
                        isAHTDone = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));
                }
            }
            return isAHTDone;
        }

        /// <summary>
        /// BTRCE-111909 Getting invite roles by using rest call
        /// </summary>
        /// <param name="urlKey"></param>
        /// <param name="BacID"></param>
        /// <param name="msisdn"></param>
        /// <param name="reason"></param>
        /// <returns></returns> 
        public static List<string> GetInviterolesList(string urlKey, string identity, string domain, string reason)
        {
            List<string> inviteroles = new List<string>();
            serviceResponse serviceresponse = new serviceResponse();

            string url = ConfigurationManager.AppSettings[urlKey].ToString();

            if (!string.IsNullOrEmpty(domain) && domain.Equals("VAS_BILLINGACCOUNT_ID", StringComparison.OrdinalIgnoreCase))
            {
                url = url + "VAS_BILLINGACCOUNT_ID/" + identity + "/services?fields=serviceIdentity,serviceRoleIdentity,createdDTime,updatedDTime&serviceCodes=spring_gsm&roleCodes=INVITE";
            }
            else
            {
                url = url + domain + "/" + identity + "/services?fields=serviceIdentity,serviceRoleIdentity,createdDTime,updatedDTime&serviceCodes=spring_gsm&roleCodes=INVITE&roleIdentity=" + identity + "%23" + domain;

            }
            serviceresponse = DnpRestCallWrapper.GetInviterolesfromDNP(url);
            if (serviceresponse != null)
            {
                foreach (serviceInstance srvinstc in serviceresponse.serviceInstance)
                {
                    foreach (serviceRole srvcrole in srvinstc.serviceRole)
                    {
                        if (srvcrole.roleCode == "INVITE")
                        {
                            if (reason == "last_sim")
                            {
                                // in case of SHared plan promotion cease need invitee BAC  
                                if (srvcrole.roleIdentity != null)
                                    inviteroles.Add(srvcrole.roleKey + ":" + srvcrole.roleIdentity.ToList().Find(ri => ri.identifierDomain == "VAS_BILLINGACCOUNT_ID").identifierValue);
                            }
                            else
                            { // in case of SHared plan SIM cease need Inviter BAC  
                                if (srvinstc.serviceIdentity[0].identifierDomain == "VAS_BILLINGACCOUNT_ID")
                                {
                                    inviteroles.Add(srvcrole.roleKey + ":" + srvinstc.serviceIdentity[0].identifierValue);
                                }
                            }
                        }
                    }
                }
            }
            return inviteroles;
        }

        /// <summary>
        /// BTRCE-111909 foramtiing in string with rolekey and BAC
        /// </summary>
        /// <param name="inviteroleslist"></param>
        /// <param name="role_key_invite"></param>
        /// <returns></returns> 
        public static string GetinviteRoleandBAClist(List<string> inviteroleslist, string role_key_invite)
        {
            if (inviteroleslist != null && inviteroleslist.Count() > 0)
            {
                foreach (string str in inviteroleslist)
                {
                    if (!string.IsNullOrEmpty(role_key_invite))
                    {
                        role_key_invite = role_key_invite + "," + str;
                    }
                    else
                    {
                        role_key_invite = str;
                    }
                }

            }
            return role_key_invite;
        }

        /// <summary>
        /// BTRCE -111936 Validation for the springservice is already is present for the profile
        /// </summary>
        /// <param name="identity1"></param>
        /// <param name="domain1"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public static bool GetSpringServiceVAlidation(string identity1, string domain1, string service)
        {
            GetBatchProfileV1Res ProfileResponse = null;
            ProfileResponse = DnpWrapper.GetServiceUserProfilesV1ForDante(identity1, domain1);
            bool isSpringServiceExist = false;
            if (ProfileResponse != null && ProfileResponse.clientProfileV1 != null)
            {
                foreach (ClientProfileV1 clientProfile in ProfileResponse.clientProfileV1)
                {
                    if (clientProfile.clientServiceInstanceV1 != null && clientProfile.clientServiceInstanceV1.Count() > 0)
                    {
                        if (clientProfile.clientServiceInstanceV1.ToList().Exists(ip => ip.clientServiceInstanceIdentifier.value.Equals(service, StringComparison.OrdinalIgnoreCase)))
                        {
                            isSpringServiceExist = true;
                        }
                    }
                }
            }
            return isSpringServiceExist;
        }

        /// <summary>
        /// BTRCE-11936 Validating the status of the invite role to update the status.
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="inviteID"></param>
        /// <param name="InviterBAC"></param>
        /// <returns></returns> 
        public static string GetinviteroleValidation(string reason, string inviteID, string InviterBAC)
        {
            string inviterolevalidation = string.Empty;
            serviceResponse serviceresponse = new serviceResponse();
            string url = ConfigurationManager.AppSettings["GetinviterolesSentByProfile"].ToString();
            url = url + "VAS_BILLINGACCOUNT_ID/" + InviterBAC + "/services?fields=serviceIdentity,serviceRoleIdentity,createdDTime,updatedDTime&serviceCodes=spring_gsm&roleCodes=INVITE&roleKeys=" + inviteID;
            serviceresponse = DnpRestCallWrapper.GetInviterolesfromDNP(url);
            if (serviceresponse != null && serviceresponse.serviceInstance.Count() > 0)
            {
                if (!string.IsNullOrEmpty(reason) && reason.ToString().ToUpper() == "REVOKEINVITE")
                {
                    if (serviceresponse.serviceInstance[0].serviceRole[0].status == "INVITE_ACCEPTED")
                    {
                        inviterolevalidation = "Ignored";
                    }
                    else if (serviceresponse.serviceInstance[0].serviceRole[0].status == "INVITE_CANCELLED")
                    {
                        inviterolevalidation = "Rejected";
                    }
                    else
                        inviterolevalidation = "Accepted";
                }
                if (!string.IsNullOrEmpty(reason) && reason.ToString().ToUpper() == "DELETEINVITE")
                {
                    if (serviceresponse.serviceInstance[0].serviceRole[0].status == "INVITE_ACCEPTED")
                    {
                        inviterolevalidation = "Ignored";
                    }
                    else if (serviceresponse.serviceInstance[0].serviceRole[0].status == "INVITE_SENT")
                    {
                        inviterolevalidation = "Rejected";
                    }
                    else
                        inviterolevalidation = "Accepted";
                }
                if (!string.IsNullOrEmpty(reason) && reason.ToString().ToUpper() == "DECLINEINVITE")
                {
                    if (serviceresponse.serviceInstance[0].serviceRole[0].status == "INVITE_ACCEPTED")
                    {
                        inviterolevalidation = "Ignored";
                    }
                    else if (serviceresponse.serviceInstance[0].serviceRole[0].status == "INVITE_CANCELLED")
                    {
                        inviterolevalidation = "Rejected";
                    }
                    else
                        inviterolevalidation = "Accepted";
                }
                if (!string.IsNullOrEmpty(reason) && reason.ToString().ToUpper() == "ACCEPTINVITE")
                {
                    if (serviceresponse.serviceInstance[0].serviceRole[0].status == "INVITE_ACCEPTED")
                    {
                        inviterolevalidation = "Ignored";
                    }
                    else
                        inviterolevalidation = "Accepted";
                }
            }
            else
            {
                inviterolevalidation = "noinviteroles";
            }

            return inviterolevalidation;
        }

        /// <summary>
        /// BTRCE-111936 Checking the invite role joureny by reason
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static bool InviteRoleJoureny(OrderRequest request)
        {
            bool invitejourney = false;
            if (!string.IsNullOrEmpty(request.Order.OrderItem[0].Action.Reason) &&
                (request.Order.OrderItem[0].Action.Reason.ToString().ToLower() == "createinvite" ||
                request.Order.OrderItem[0].Action.Reason.ToString().ToLower() == "revokeinvite" ||
                request.Order.OrderItem[0].Action.Reason.ToString().ToLower() == "deleteinvite" ||
                request.Order.OrderItem[0].Action.Reason.ToString().ToLower() == "acceptinvite" ||
                request.Order.OrderItem[0].Action.Reason.ToString().ToLower() == "declineinvite" ||
                request.Order.OrderItem[0].Action.Reason.ToString().ToLower() == "authorise" ||
                request.Order.OrderItem[0].Action.Reason.ToString().ToLower() == "cancelauthorisation"))
            {
                invitejourney = true;
            }
            return invitejourney;

        }

        /// <summary>
        /// BTRCE-111909 check the invite role is already exists for the sim from same BAC
        /// </summary>
        /// <returns></returns>
        public static bool InviteroleExists(string urlKey, string msisdn, string SPownerBAC)
        {
            bool inviteRoleExist = false;
            serviceResponse serviceresponse = new serviceResponse();

            string url = ConfigurationManager.AppSettings[urlKey].ToString();
            if (!string.IsNullOrEmpty(msisdn))
            {
                url = url + "MOBILE_MSISDN/" + msisdn + "/services?fields=serviceIdentity,serviceRoleIdentity,createdDTime,updatedDTime&serviceCodes=spring_gsm&roleCodes=INVITE&roleIdentity=" + msisdn + "%23MOBILE_MSISDN";
            }
            serviceresponse = DnpRestCallWrapper.GetInviterolesfromDNP(url);
            if (serviceresponse != null)
            {
                foreach (serviceInstance srvinstc in serviceresponse.serviceInstance)
                {
                    foreach (identityV1 srvcid in srvinstc.serviceIdentity)
                    {
                        if (srvcid.identifierDomain == "VAS_BILLINGACCOUNT_ID" && srvcid.identifierValue == SPownerBAC)
                        {
                            inviteRoleExist = true;
                        }
                    }
                }
            }
            return inviteRoleExist;
        }
        //ccp 79
        public static bool IsNayanProfile(string bac,ref string targetId)
        {
            bool isNayanProfile = false;
            ClientServiceInstanceV1[] serviceInstances = null;           
            serviceInstances = DnpWrapper.getServiceInstanceV1(bac, "VAS_BILLINGACCOUNT_ID", String.Empty);           
            if (serviceInstances != null && serviceInstances.Count() > 0)
            {                

                if (serviceInstances.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals("MANAGE_ACCOUNT", StringComparison.OrdinalIgnoreCase) && si.clientServiceInstanceCharacteristic != null && si.clientServiceInstanceCharacteristic.ToList().Exists(cschar => cschar.name.Equals("BACORGANISATIONNAME", StringComparison.OrdinalIgnoreCase) && cschar.value.Equals("EE", StringComparison.OrdinalIgnoreCase))))
                {
                    isNayanProfile = true;
                    if (serviceInstances.ToList().Exists(si => si.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null && si.clientServiceRole.Count() > 1 && si.clientServiceRole.ToList().Exists(csr => csr.id.Equals("SERVICE_MANAGER", StringComparison.OrdinalIgnoreCase))))
                    {
                        var serviceRole = (from si in serviceInstances
                                           where (si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase) && si.clientServiceRole != null && si.clientServiceRole.ToList().Exists(sr => sr.id.Equals("SERVICE_MANAGER", StringComparison.OrdinalIgnoreCase)))
                                           select si.clientServiceRole).FirstOrDefault();

                        if (serviceRole != null)
                        {
                            var clientidentitydetails = (from sr in serviceRole
                                                         where (sr.id.Equals("SERVICE_MANAGER", StringComparison.OrdinalIgnoreCase))
                                                         select sr.clientIdentity).FirstOrDefault();
                            if (clientidentitydetails != null)
                            {
                                if (clientidentitydetails.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
                                {
                                    targetId = clientidentitydetails.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                }
                            }                            
                        }
                    }                    
                }
            }
            return isNayanProfile;
        }
        public static string GetSPownerBAC(string urlKey, string msisdn, string inviteID)
        {
            string SPownerBAC = string.Empty;
            serviceResponse serviceresponse = new serviceResponse();

            string url = ConfigurationManager.AppSettings[urlKey].ToString();
            if (!string.IsNullOrEmpty(msisdn))
            {
                url = url + "MOBILE_MSISDN/" + msisdn + "/services?fields=serviceIdentity,serviceRoleIdentity,createdDTime,updatedDTime&serviceCodes=spring_gsm&roleCodes=INVITE&roleIdentity=" + msisdn + "%23MOBILE_MSISDN";
            }
            serviceresponse = DnpRestCallWrapper.GetInviterolesfromDNP(url);
            if (serviceresponse != null)
            {
                foreach (serviceInstance srvinstc in serviceresponse.serviceInstance)
                {
                    foreach (serviceRole srvcid in srvinstc.serviceRole)
                    {
                        if (srvcid.roleKey == inviteID)
                        {
                            if (srvinstc.serviceIdentity[0].identifierDomain == "VAS_BILLINGACCOUNT_ID")
                                SPownerBAC = srvinstc.serviceIdentity[0].identifierValue;
                        }
                    }
                }
            }
            return SPownerBAC;
        }

        //-----------Murali-------------
        public static void GetDelegaterolelist(ClientServiceInstanceV1[] gsiResponse, ref string delegateSpring, string RVSID, ref string delegateSport, ref string delegateWifi)
        {
            try
            {
                string servicename = string.Empty;

                if (gsiResponse != null)
                {
                    foreach (ClientServiceInstanceV1 serviceIntance in gsiResponse)
                    {
                        servicename = serviceIntance.clientServiceInstanceIdentifier.value;
                        if (serviceIntance.clientServiceRole != null && serviceIntance.clientServiceRole.Count() > 0)
                        {
                            foreach (ClientServiceRole role in serviceIntance.clientServiceRole.Where(csr => csr.id.Equals("SERVICE_USER", StringComparison.OrdinalIgnoreCase) || csr.id.Equals("SERVICE_MANAGER", StringComparison.OrdinalIgnoreCase)))
                            {
                                if (role != null && role.clientIdentity != null)
                                {

                                    if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("RVSID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(RVSID, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        if (servicename == "SPRING_GSM")
                                        {
                                            getDelegateRoleIdentity(role, ref delegateSpring);                                            
                                        }
                                        else if (servicename == "BTWIFI:DEFAULT")
                                        {
                                            getDelegateRoleIdentity(role, ref delegateWifi);
                                        }
                                        else if (servicename == "BTSPORT:DIGITAL")
                                        {
                                            getDelegateRoleIdentity(role, ref delegateSport);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// read the delegate role identities
        /// </summary>
        /// <param name="role"></param>
        /// <param name="delegateRoleDetails"></param>
        public static void getDelegateRoleIdentity(ClientServiceRole role,ref string delegateRoleDetails)
        {
            string rolename = role.id;
            string roleid = role.name;
            string roleIdentityvalue = string.Empty;
            if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
            {
                roleIdentityvalue = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                delegateRoleDetails += rolename + ";" + roleid + ";" + roleIdentityvalue + ",";
            }
            else if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("MOBILE_MSISDN", StringComparison.OrdinalIgnoreCase)))
            {
                roleIdentityvalue = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("MOBILE_MSISDN", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                delegateRoleDetails += rolename + ";" + roleid + ";" + roleIdentityvalue + ";" + "MOBILE_MSISDN" + ",";
            }
            else if (role.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("RVSID", StringComparison.OrdinalIgnoreCase)))
            {
                roleIdentityvalue = role.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("RVSID", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                delegateRoleDetails += rolename + ";" + roleid + ";" + roleIdentityvalue + ";" + "RVSID" + ",";
            }           
        }
        //-----------

        /// <summary>
        /// BTR-95909 MSI call to udpate the SICharacter to BTTV
        /// </summary>
        /// <param name="MSIRequest">MSIRequest</param>
        /// <param name="notification"></param>
        /// <param name="e2eData"></param>
        /// <returns>MSIResponse</returns>
        public static ManageDNPServiceInstanceResponse MSICalltoDnP(ManageDNPServiceInstanceRequest MSIRequest, ref E2ETransaction e2eData, string bptmTxnId, System.DateTime ActivityStartTime, Guid guid, string orderKey)
        {
            ManageDNPServiceInstanceResponse MSIResponse = new ManageDNPServiceInstanceResponse();
            manageServiceInstanceV1Request1 request = new manageServiceInstanceV1Request1();
            manageServiceInstanceV1Response1 profileResponse = null;

            request.manageServiceInstanceV1Request = new ManageServiceInstanceV1Request();
            request.manageServiceInstanceV1Request.standardHeader = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            request.manageServiceInstanceV1Request.standardHeader.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            request.manageServiceInstanceV1Request.standardHeader.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            request.manageServiceInstanceV1Request.standardHeader.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

            List<ClientServiceInstanceCharacteristic> listClientSrvcInstnceChar = new List<ClientServiceInstanceCharacteristic>();
            ClientServiceInstanceCharacteristic clientSrvcChar = null;

            try
            {
                ClientServiceInstanceV1[] serviceInstance = new ClientServiceInstanceV1[1];
                serviceInstance[0] = new ClientServiceInstanceV1();

                serviceInstance[0].clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
                if (MSIRequest != null && MSIRequest.srvcInstance != null && MSIRequest.srvcInstance.ServiceCode != null)
                    serviceInstance[0].clientServiceInstanceIdentifier.value = MSIRequest.srvcInstance.ServiceCode;
                //serviceInstance[0].clientServiceInstanceStatus = new ClientServiceInstanceStatus();
                //serviceInstance[0].clientServiceInstanceStatus.value = "ACTIVE";
                serviceInstance[0].action = ACTION_UPDATE;
                List<ServiceIdentity> listServiceIdentity = new List<ServiceIdentity>();
                ServiceIdentity serviceIdentity = new ServiceIdentity();
                serviceIdentity.domain = "VAS_BILLINGACCOUNT_ID";
                if (MSIRequest != null && MSIRequest.srvcInstance != null && MSIRequest.srvcInstance.BillAccountNumber != null)
                    serviceIdentity.value = MSIRequest.srvcInstance.BillAccountNumber;
                serviceIdentity.action = ACTION_SEARCH;

                listServiceIdentity.Add(serviceIdentity);

                serviceInstance[0].serviceIdentity = listServiceIdentity.ToArray();

                foreach (InstanceCharcteristic instanceChar in MSIRequest.srvcInstance.InstanceCharacteristics.InstanceCharacteristic)
                {
                    clientSrvcChar = new ClientServiceInstanceCharacteristic();
                    if (instanceChar != null && instanceChar.Name != null)
                        clientSrvcChar.name = instanceChar.Name;
                    if (instanceChar != null & instanceChar.Value != null)
                        clientSrvcChar.value = instanceChar.Value;
                    clientSrvcChar.action = ACTION_FORCE_INS;
                    listClientSrvcInstnceChar.Add(clientSrvcChar);
                }

                serviceInstance[0].clientServiceInstanceCharacteristic = listClientSrvcInstnceChar.ToArray();

                ManageServiceInstanceV1Req manageServiceInstanceV1Req1 = new ManageServiceInstanceV1Req();
                manageServiceInstanceV1Req1.clientServiceInstanceV1 = serviceInstance[0];

                request.manageServiceInstanceV1Request.manageServiceInstanceV1Req = manageServiceInstanceV1Req1;

                e2eData.logMessage("StartedDnPCall", "");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationDnP"].ToString(), E2ETransaction.vREQ, "BTTV:DEFAULT");

                request.manageServiceInstanceV1Request.standardHeader.e2e = new BT.SaaS.IspssAdapter.Dnp.E2E();
                request.manageServiceInstanceV1Request.standardHeader.e2e.E2EDATA = e2eData.toString();

                LogHelper.LogActivityDetails(bptmTxnId, guid, "MakingDnPCall", System.DateTime.Now - ActivityStartTime, "BTNetFlixTrace", orderKey, request.SerializeObject());

                profileResponse = DnpWrapper.manageServiceInstanceV1Thomas(request, MSIRequest.OrderNumber.ToString());

                LogHelper.LogActivityDetails(bptmTxnId, guid, "GotResponseFromDnP", System.DateTime.Now - ActivityStartTime, "BTNetFlixTrace", orderKey, profileResponse.SerializeObject());

                if (profileResponse != null
                    && profileResponse.manageServiceInstanceV1Response != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode != null
                    && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode == "0")
                {
                    e2eData.logMessage("GotResponseFromDnP", "");
                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                        e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                    else
                        e2eData.endOutboundCall(e2eData.toString());

                    MSIResponse.result = true;
                    MSIResponse.errorcode = "004";
                    MSIResponse.errorDescritpion = "Completed";
                    LogHelper.LogActivityDetails(bptmTxnId, guid, "Completed successfully and MSEOSendResponse to OSCH", System.DateTime.Now - ActivityStartTime, "BTNetFlixTrace", orderKey, MSIResponse.SerializeObject());
                }
                else
                {
                    string errorMessage = string.Empty;
                    if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages != null && profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description != null)
                    {
                        errorMessage = profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description;
                        e2eData.businessError("GotResponseFromDnPWithBusinessError", errorMessage);
                        if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                    }
                    else
                    {
                        e2eData.businessError("GotResponseFromDnPWithBusinessError", "Response is null from DnP");
                        if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                            e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                        else
                            e2eData.endOutboundCall(e2eData.toString());
                    }

                    MSIResponse.result = false;
                    MSIResponse.errorcode = "777";
                    MSIResponse.errorDescritpion = errorMessage;
                    LogHelper.LogActivityDetails(bptmTxnId, guid, "Failed and MSEOSendResponse to OSCH", System.DateTime.Now - ActivityStartTime, "BTNetFlixTrace", orderKey, MSIResponse.SerializeObject());
                }

            }
            catch (Exception ex)
            {
                e2eData.businessError("GotResponseFromDnPWithBusinessError", ex.Message);
                if (profileResponse != null && profileResponse.manageServiceInstanceV1Response != null && profileResponse.manageServiceInstanceV1Response.standardHeader != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e != null && profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA != null && !String.IsNullOrEmpty(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA))
                    e2eData.endOutboundCall(profileResponse.manageServiceInstanceV1Response.standardHeader.e2e.E2EDATA);
                else
                    e2eData.endOutboundCall(e2eData.toString());

                MSIResponse.result = false;
                MSIResponse.errorcode = "777";
                MSIResponse.errorDescritpion = ex.Message.ToString();
            }
            finally
            {
                listClientSrvcInstnceChar = null;
            }
            return MSIResponse;
        }
        /// <summary>
        /// using this call will unlink the vsid from bacid
        /// </summary>
        /// <param name="vsID"></param>
        /// <param name="bacId"></param>
        public static void UnlinkVSIDfromBAC(string vsID, string bacId, string orderKey)
        {
            manageServiceInstanceV1Response1 profileResponse;
            manageServiceInstanceV1Request1 profileRequest = new manageServiceInstanceV1Request1();
            profileRequest.manageServiceInstanceV1Request = new ManageServiceInstanceV1Request();
            ManageServiceInstanceV1Req manageServiceInsatnceV1Req = new ManageServiceInstanceV1Req();

            ClientServiceInstanceV1 serviceInstance = new ClientServiceInstanceV1();
            serviceInstance.clientServiceInstanceIdentifier = new ClientServiceInstanceIdentifier();
            serviceInstance.clientServiceInstanceIdentifier.value = "BTTV:DEFAULT";
            serviceInstance.action = ACTION_UPDATE;

            List<ServiceIdentity> serviceIdList = new List<ServiceIdentity>();
            ServiceIdentity serviceIdentity;
            if (!string.IsNullOrEmpty(vsID) && !string.IsNullOrEmpty(bacId))
            {
                serviceIdentity = new ServiceIdentity();
                serviceIdentity.domain = "VAS_BILLINGACCOUNT_ID";
                serviceIdentity.value = bacId;
                serviceIdentity.action = ACTION_SEARCH;
                serviceIdList.Add(serviceIdentity);

                serviceIdentity = new ServiceIdentity();
                serviceIdentity.domain = "VSID";
                serviceIdentity.value = vsID;
                serviceIdentity.action = "UNLINK";
                serviceIdList.Add(serviceIdentity);
            }
            serviceInstance.serviceIdentity = serviceIdList.ToArray();
            manageServiceInsatnceV1Req.clientServiceInstanceV1 = serviceInstance;

            BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock headerBlock = new BT.SaaS.IspssAdapter.Dnp.StandardHeaderBlock();
            headerBlock.serviceAddressing = new BT.SaaS.IspssAdapter.Dnp.ServiceAddressing();
            headerBlock.serviceAddressing.from = "http://www.profile.com?SAASMSEO";
            headerBlock.serviceState = new BT.SaaS.IspssAdapter.Dnp.ServiceState();

            manageServiceInsatnceV1Req.clientServiceInstanceV1 = serviceInstance;
            profileRequest.manageServiceInstanceV1Request.standardHeader = headerBlock;
            profileRequest.manageServiceInstanceV1Request.manageServiceInstanceV1Req = manageServiceInsatnceV1Req;

            #region Call Dnp
            profileResponse = DnpWrapper.manageServiceInstanceV1Thomas(profileRequest, orderKey);

            if (profileResponse != null
                && profileResponse.manageServiceInstanceV1Response != null
                && profileResponse.manageServiceInstanceV1Response.standardHeader != null
                && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState != null
                && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode != null
                && profileResponse.manageServiceInstanceV1Response.standardHeader.serviceState.stateCode == "0")
            {
                //success
            }
            else
            {
                throw new DnpException(profileResponse.manageServiceInstanceV1Response.manageServiceInstanceV1Res.messages[0].description);
            }
            #endregion
        }
    }
}
