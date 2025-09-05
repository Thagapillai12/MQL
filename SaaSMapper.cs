using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Linq;
using MSEO = BT.SaaS.MSEOAdapter;
using SaaSNS = BT.SaaS.Core.Shared.Entities;
using BT.SaaS.IspssAdapter;
using BT.SaaS.Core.MDMAPI.Entities;
using BT.SaaS.Core.MDMAPI.Entities.Product;
using BT.SaaS.Core.Shared.Entities;
using System.Collections.Generic;
using BT.SaaS.IspssAdapter.Dnp;
using System.Collections.Specialized;
using System.IO;  

namespace BT.SaaS.MSEOAdapter
{
    public partial class MSEOSaaSMapper
    {
        #region prepareProductOrderItemsBBReactivation
        public static SaaSNS.ProductOrderItem prepareProductOrderItemsBBReactivation(OrderItem orditem, string SIStatus, SaaSNS.Order response, string productName, int productorderItemCount, string vasProductID, string srvcTier)
        {
            string emailName = string.Empty;
            string BACID_IDENTIFER_NAMEPACE = "VAS_BILLINGACCOUNT_ID";
            string bakId = string.Empty;
            string ServiceInstanceKey = string.Empty;
            bool isReserved = false;
            bool isAHTDone = false;
            string emailsupplier = "MX";
            List<string> ListOfEmails = new List<string>();
            GetClientProfileV1Res getprofileResponse1 = new GetClientProfileV1Res();
            SaaSNS.ProductOrderItem productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
            productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
            productOrderItem.Header.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.notDone;
            productOrderItem.Header.Quantity = "1";
            productOrderItem.XmlIdKey = "PO-" + productorderItemCount.ToString();
            try
            {
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

                    //GetClientProfileV1Res getprofileResponse1 = new GetClientProfileV1Res();
                   // BT.SaaS.IspssAdapter.Dnp.ClientIdentity emailClientIdentity = null;
                    if (!String.IsNullOrEmpty(bakId))
                    {
                        getprofileResponse1 = DnpWrapper.GetClientProfileV1(bakId, BACID_IDENTIFER_NAMEPACE);

                        if (getprofileResponse1 != null && getprofileResponse1.clientProfileV1 != null)
                        {
                            //isExistingAccount = true;
                            BT.SaaS.IspssAdapter.Dnp.ClientIdentity bacClientIdentity = getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Where(i => i.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase) && i.value.Equals(bakId, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                                      
                            if (bacClientIdentity.clientIdentityValidation != null)
                                isAHTDone = bacClientIdentity.clientIdentityValidation.ToList().Exists(i => i.name.Equals("ACCOUNTTRUSTMETHOD", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(i.value.ToLower()));

                        }
                    }

                    if (productOrderItem.Header.ProductCode.Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"], StringComparison.OrdinalIgnoreCase))
                    {
                        if (orditem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("emailname")) && !string.IsNullOrEmpty(orditem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailname")).FirstOrDefault().Value))
                        {
                            emailName = orditem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailname")).FirstOrDefault().Value;
                            getprofileResponse1 = DnpWrapper.GetClientProfileV1(emailName, "BTIEMAILID");
                            if (getprofileResponse1.clientProfileV1 != null && getprofileResponse1.clientProfileV1.client != null && getprofileResponse1.clientProfileV1.client.clientIdentity != null && getprofileResponse1.clientProfileV1.client.clientIdentity.Count() > 0)
                            {
                                if (getprofileResponse1.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTIEMAILID", StringComparison.OrdinalIgnoreCase) && ci.value.Equals(emailName, StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["BTIEmailIdentityStatus"].Split(',').Contains(ci.clientIdentityStatus.value.ToLower())))
                                {
                                    isReserved = true;
                                }
                            }

                            SaaSNS.Attribute is_reserved = new BT.SaaS.Core.Shared.Entities.Attribute();
                            is_reserved.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            is_reserved.Name = "ISRESERVED";
                            is_reserved.Value = isReserved.ToString();
                            roleInstance.Attributes.Add(is_reserved);

                            ServiceInstanceKey = emailName;
                        }
                        if (SIStatus.Equals("pending-cease", StringComparison.OrdinalIgnoreCase))
                        {
                            //using same method as of premium eamil account holder provision
                            ClientServiceInstanceV1 emailServiceInstance = null;
                            ListOfEmails = DanteRequestProcessor.GetListOfEmailsOnPremiumAccountHolder(bakId, ref emailServiceInstance);

                            if (ListOfEmails != null && ListOfEmails.Count > 0)
                            {
                                if (ListOfEmails[0] != null)
                                {
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ListOfEmails", Value = ListOfEmails[0] });
                                }
                                if (ListOfEmails[1] != null)
                                {
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "InactiveEmailList", Value = ListOfEmails[1] });
                                }
                            }
                            //email supplier

                            if (ConfigurationManager.AppSettings["VASMigrationSwitch"].Equals("OFF", StringComparison.OrdinalIgnoreCase))
                            {
                                DanteRequestProcessor.GetEmailSupplier(true,ref emailsupplier,emailServiceInstance);
                            
                            }
                            if (orditem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("emailsupplier", StringComparison.OrdinalIgnoreCase)))
                                roleInstance.Attributes.ToList().Where(roleInstAttrb => roleInstAttrb.Name.Equals("EMAILSUPPLIER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = emailsupplier;
                            else
                            {
                                SaaSNS.Attribute siEmailSupplier = new BT.SaaS.Core.Shared.Entities.Attribute();
                                siEmailSupplier.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                siEmailSupplier.Name = "emailsupplier";
                                siEmailSupplier.Value = emailsupplier;
                                roleInstance.Attributes.Add(siEmailSupplier);
                            }
                           
                        }

                        if (SIStatus.Equals("ceased", StringComparison.OrdinalIgnoreCase)) //emailsupplier BTRCE-108426/BTRCE-108471
                        {

                            if (ConfigurationManager.AppSettings["VASMigrationSwitch"].Equals("OFF", StringComparison.OrdinalIgnoreCase))
                            {
                                emailsupplier = ConfigurationManager.AppSettings["VASEmailSupplierSwitch"];
                            }
                            else
                                emailsupplier = "CriticalPath";

                            if (roleInstance.Attributes.Exists(ra => ra.Name.Equals("EmailSupplier", StringComparison.OrdinalIgnoreCase)))
                            {
                                roleInstance.Attributes.ToList().Where(roleInstAttrb => roleInstAttrb.Name.Equals("EMAILSUPPLIER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value = emailsupplier;
                            }
                            else
                            {
                                SaaSNS.Attribute siEmailSupplier = new BT.SaaS.Core.Shared.Entities.Attribute();
                                siEmailSupplier.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                siEmailSupplier.Name = "EmailSupplier";
                                siEmailSupplier.Value = emailsupplier;
                                roleInstance.Attributes.Add(siEmailSupplier);
                            }


                        }

                        SaaSNS.Attribute MAILCLASS = new BT.SaaS.Core.Shared.Entities.Attribute();
                        MAILCLASS.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        MAILCLASS.Name = "MAILCLASS";
                        if (!string.IsNullOrEmpty(emailsupplier) && (emailsupplier.Equals(ConfigurationManager.AppSettings["yahoo_Email_Supplier"], StringComparison.OrdinalIgnoreCase) || emailsupplier.Equals(ConfigurationManager.AppSettings["MX_Email_Supplier"], StringComparison.OrdinalIgnoreCase)))
                            MAILCLASS.Value = "ACTIVE";
                        else
                            MAILCLASS.Value = "ENABLED";
                        roleInstance.Attributes.Add(MAILCLASS);

                        SaaSNS.Attribute is_AHTClient = new BT.SaaS.Core.Shared.Entities.Attribute();
                        is_AHTClient.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        is_AHTClient.Name = "ISAHTCLIENT";
                        is_AHTClient.Value = isAHTDone.ToString();
                        roleInstance.Attributes.Add(is_AHTClient);

                      
                        SaaSNS.Attribute emailSIStatus = new BT.SaaS.Core.Shared.Entities.Attribute();
                        emailSIStatus.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        emailSIStatus.Name = "EmailSIStatus";
                        emailSIStatus.Value = SIStatus;
                        roleInstance.Attributes.Add(emailSIStatus);


                        SaaSNS.Attribute siBacNumber = new BT.SaaS.Core.Shared.Entities.Attribute();
                        siBacNumber.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        siBacNumber.Name = "SIBACNUMBER";
                        siBacNumber.Value = bakId;
                        roleInstance.Attributes.Add(siBacNumber);
                    }
                    else
                    {
                        if (srvcTier.Equals(ConfigurationManager.AppSettings["BBWIFIServiceTier"], StringComparison.OrdinalIgnoreCase))
                        {
                            SaaSNS.Attribute hubStatusAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            hubStatusAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            hubStatusAttribute.Name = "HUB_WIFI_STATUS";
                            hubStatusAttribute.Value = "OPTED_IN";
                            roleInstance.Attributes.Add(hubStatusAttribute);

                            SaaSNS.Attribute BBStatusAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            hubStatusAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            hubStatusAttribute.Name = "WIFI_BROADBAND_FLAG";
                            hubStatusAttribute.Value = "Y";
                            roleInstance.Attributes.Add(hubStatusAttribute);

                        }
                        else if (srvcTier.Equals(ConfigurationManager.AppSettings["SpringWIFIServiceTier"], StringComparison.OrdinalIgnoreCase))
                        {
                            SaaSNS.Attribute hubStatusAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            hubStatusAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            hubStatusAttribute.Name = "HUB_WIFI_STATUS";
                            hubStatusAttribute.Value = "OPTED_OUT";
                            roleInstance.Attributes.Add(hubStatusAttribute);
                        }

                        SaaSNS.Attribute srvcTierAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                        srvcTierAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                        srvcTierAttribute.Name = "WIFI_SERVICE_TIER";
                        srvcTierAttribute.Value = srvcTier.ToString();
                        roleInstance.Attributes.Add(srvcTierAttribute);
                    }

                    SaaSNS.Attribute VASProduct_ID = new BT.SaaS.Core.Shared.Entities.Attribute();
                    VASProduct_ID.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    VASProduct_ID.Name = "VASPRODUCTID";
                    VASProduct_ID.Value = vasProductID.ToString();
                    roleInstance.Attributes.Add(VASProduct_ID);

                    SaaSNS.Attribute IsBBReactivate = new BT.SaaS.Core.Shared.Entities.Attribute();
                    IsBBReactivate.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    IsBBReactivate.Name = "IsBBReactivate";
                    IsBBReactivate.Value = "true";
                    roleInstance.Attributes.Add(IsBBReactivate);

                    SaaSNS.Attribute SStatus = new BT.SaaS.Core.Shared.Entities.Attribute();
                    SStatus.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    SStatus.Name = "ServiceInstanceStatus";
                    SStatus.Value = SIStatus;
                    roleInstance.Attributes.Add(SStatus);

                    serviceInstance.ServiceRoles.Add(serviceRole);
                    productOrderItem.ServiceInstances.Add(serviceInstance);
                    productOrderItem.RoleInstances.Add(roleInstance);

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
                ListOfEmails = null;
            }
            return productOrderItem;
        }

        #endregion

        #region AcceptedEmailOrder

        public static string AcceptedEmailOrder(OrderItem ordrItem, string VasProductName)
        {
            string orderType = string.Empty;
            string reservationId = string.Empty;
            try
            {
                //if (VasProductName.Equals("premium email", StringComparison.OrdinalIgnoreCase))
                //{
                //   // ordrItem.Specification[0].Identifier.Value1 = "S0145869";
                //    orderType = "PremiumEmail";
                //}
                //else
                //{
                    if (ConfigurationManager.AppSettings["VASMigrationSwitch"].Equals("ON", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ordrItem.Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.Equals("ReservationID") && !string.IsNullOrEmpty(ic.Value)))
                        {
                            reservationId = ordrItem.Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.Equals("ReservationID")).FirstOrDefault().Value;

                            if (!string.IsNullOrEmpty(reservationId))
                            {
                                string dataStore = VASRequestValidator.validateReservationID(reservationId);
                                if (dataStore.Equals("D&P"))
                                {
                                    orderType = "Activation";
                                }
                                else
                                {
                                    orderType = "ReservationIDNotFormat";
                                }
                            }
                        }
                        else
                        {
                            orderType = "ReservationIDNotFormat";
                        }
                    }
                    else if (ConfigurationManager.AppSettings["VASMigrationSwitch"].Equals("OFF", StringComparison.OrdinalIgnoreCase))
                    {
                        orderType = "Activation";
                    }
                    else if (ConfigurationManager.AppSettings["VASMigrationSwitch"].Equals("LEGACY", StringComparison.OrdinalIgnoreCase))
                    {
                        orderType = "switchInLegacy";
                    }

                //}
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return orderType;
        }
        #endregion

        # region MapUpgradeRequest
        public static SaaSNS.Order MapUpgradeRequest(MSEO.OrderRequest request, Dictionary<ProductVasClass, string> upgradeVASDic, ClientServiceInstanceV1[] srvcs)
        {
            int productorderItemCount = 0;
            string subscriptionExternalReference = string.Empty;
            string previousSupplierCode = string.Empty;
            string customerContextID = string.Empty;
            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Mapping VAS Regrade request");
            SaaSNS.Order response = new BT.SaaS.Core.Shared.Entities.Order();
            System.DateTime orderDate = new System.DateTime();

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

                foreach (OrderItem oItem in request.Order.OrderItem)
                {
                    string vasClass = oItem.Instance[0].InstanceCharacteristic.ToList().Where(v => v.Name.Equals("vasclass", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                    foreach (ProductVasClass pvc in upgradeVASDic.Keys)
                    {
                        if (pvc.VasClass.Equals(vasClass))
                        {
                            SaaSNS.ProductOrderItem productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
                            productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
                            productOrderItem.Header.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.notDone;
                            productOrderItem.Header.Quantity = "1";
                            productOrderItem.XmlIdKey = "PO-" + productorderItemCount.ToString();

                            productOrderItem.Header.OrderItemKey = productorderItemCount.ToString();

                            productOrderItem.Header.ProductCode = pvc.VasProductFamily;
                            System.Collections.Generic.List<string> inputProductDefinitionList = new System.Collections.Generic.List<string>();
                            inputProductDefinitionList.Add(productOrderItem.Header.ProductCode);

                            System.Collections.Generic.List<ProductDefinition> productDefinition = MdmWrapper.getSaaSProductDefs(inputProductDefinitionList, Settings1.Default.ConsumerServiceProviderId);
                            if (productDefinition.Count == 0)
                            {
                                oItem.Status = Settings1.Default.IgnoredStatus;
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

                                roleInstance.InternalProvisioningAction = ProvisioningActionEnum.provide;
                                roleInstance.Action = BT.SaaS.Core.Shared.Entities.RoleInstanceActionEnum.add;

                                SaaSNS.ServiceInstance serviceInstance = new BT.SaaS.Core.Shared.Entities.ServiceInstance();
                                serviceInstance.ServiceRoles = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ServiceRole>();
                                SaaSNS.ServiceRole serviceRole = new BT.SaaS.Core.Shared.Entities.ServiceRole();
                                serviceRole.roleAttributes = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.Attribute>();

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
                                foreach (ClientServiceInstanceV1 srvcInstnc in srvcs)
                                {
                                    if (srvcInstnc.clientServiceInstanceIdentifier.value.Equals(pvc.VasProductFamily + ":" + pvc.vasSubType, StringComparison.OrdinalIgnoreCase))
                                    {
                                        customerContextID = srvcInstnc.name.ToString();
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
                                        }
                                    }
                                }

                                if (pvc.VasProductFamily.Equals(ConfigurationManager.AppSettings["VASContentAnyWhereProductFamily"], StringComparison.OrdinalIgnoreCase))
                                {
                                    SaaSNS.Attribute attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "SUBSCRIPTIONEXTERNALREFERENCE";
                                    attr.Value = subscriptionExternalReference;
                                    roleInstance.Attributes.Add(attr);

                                    SaaSNS.Attribute prvSupplierCodeattr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    prvSupplierCodeattr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    prvSupplierCodeattr.Name = "PREVIOUSSUPPLIERCODE";
                                    prvSupplierCodeattr.Value = previousSupplierCode;
                                    roleInstance.Attributes.Add(prvSupplierCodeattr);

                                    SaaSNS.Attribute VASProductIDattr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    VASProductIDattr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    VASProductIDattr.Name = "VASPRODUCTID";
                                    VASProductIDattr.Value = pvc.VasProductId;
                                    roleInstance.Attributes.Add(VASProductIDattr);

                                }
                                if (pvc.VasProductFamily.Equals("btwifi", StringComparison.OrdinalIgnoreCase))
                                {
                                    SaaSNS.Attribute serviceTierattr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    serviceTierattr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    serviceTierattr.Name = "WIFI_SERVICE_TIER";
                                    serviceTierattr.Value = pvc.VasServiceTier.ToString();
                                    roleInstance.Attributes.Add(serviceTierattr);

                                    SaaSNS.Attribute VASProductIDattr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    VASProductIDattr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    VASProductIDattr.Name = "VASPRODUCTID";
                                    VASProductIDattr.Value = pvc.VasProductId;
                                    roleInstance.Attributes.Add(VASProductIDattr);
                                }

                                if (pvc.VasProductFamily.Equals("advancedsecurity", StringComparison.OrdinalIgnoreCase))
                                {
                                    SaaSNS.Attribute VASProductIDattr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    VASProductIDattr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    VASProductIDattr.Name = "VASPRODUCTID";
                                    VASProductIDattr.Value = pvc.VasProductId;
                                    roleInstance.Attributes.Add(VASProductIDattr);

                                    SaaSNS.Attribute attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attr.Name = "LICENSEQTY";
                                    attr.Value = pvc.ActivationCardinality.ToString();
                                    roleInstance.Attributes.Add(attr);
                                }


                                SaaSNS.Attribute isVASBoltONattr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                isVASBoltONattr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                isVASBoltONattr.Name = "ISVASBOLTON";
                                isVASBoltONattr.Value = "true";
                                roleInstance.Attributes.Add(isVASBoltONattr);

                                SaaSNS.Attribute CCIDattr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                CCIDattr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                CCIDattr.Name = "CUSTOMERCONTEXTID";
                                CCIDattr.Value = customerContextID;
                                roleInstance.Attributes.Add(CCIDattr);

                                SaaSNS.Attribute subTypeattr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                subTypeattr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                subTypeattr.Name = "VASSUBTYPE";
                                subTypeattr.Value = pvc.vasSubType;
                                roleInstance.Attributes.Add(subTypeattr);

                                SaaSNS.Attribute supplierCodeattr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                supplierCodeattr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                supplierCodeattr.Name = "SUPPLIERCODE";
                                supplierCodeattr.Value = pvc.SupplierCode;
                                roleInstance.Attributes.Add(supplierCodeattr);



                                SaaSNS.Attribute isVASRegradeAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                isVASRegradeAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                isVASRegradeAttribute.Name = "ISVASREGRADE";
                                isVASRegradeAttribute.Value = "TRUE";
                                roleInstance.Attributes.Add(isVASRegradeAttribute);

                                serviceInstance.ServiceRoles.Add(serviceRole);
                                productOrderItem.ServiceInstances.Add(serviceInstance);
                                productOrderItem.RoleInstances.Add(roleInstance);

                                if (request.StandardHeader != null && request.StandardHeader.E2e != null && request.StandardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(request.StandardHeader.E2e.E2EDATA.ToString()))
                                    productOrderItem.RoleInstances[0].Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "E2EDATA", Value = request.StandardHeader.E2e.E2EDATA.ToString(), action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });
                                else
                                    productOrderItem.RoleInstances[0].Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { Name = "E2EDATA", Value = string.Empty, action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add });

                                if (!(response.ProductOrderItems.ToList().Exists(poi => poi.Header.ProductCode.ToString().Equals(productOrderItem.Header.ProductCode) && poi.RoleInstances.FirstOrDefault().InternalProvisioningAction.ToString().Equals(productOrderItem.RoleInstances.FirstOrDefault().InternalProvisioningAction.ToString()) && poi.RoleInstances.FirstOrDefault().Attributes.ToList().Exists(at => at.Name.Equals("VASSUBTYPE") && at.Value.Equals(pvc.vasSubType)))))
                                {
                                    productorderItemCount++;
                                    response.ProductOrderItems.Add(productOrderItem);
                                }
                            }
                        }
                    }
                }
            }

            return response;
        }

        #endregion


        #region
        public static SaaSNS.Order MapChangeRequest(MSEO.OrderRequest request, string productCode, string identity, string iden, string billActNum, string BTOneID)
        {
            int productorderItemCount = 0;
            string barredReason = string.Empty;
            string rbsid = string.Empty;
            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Mapping the request");

            SaaSNS.Order response = new BT.SaaS.Core.Shared.Entities.Order();
            try
            {
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
                if (productCode.Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"].ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    //user.Action = ClientActionEnum.deactivate;
                    //user.Status = DataStatusEnum.notDone;
                    //user.ClientStatus = ClientStatusEnum.active;
                    //user.IdentityCredential = new System.Collections.Generic.List<IdentityCredential>();
                    //IdentityCredential identityCredential = new IdentityCredential();
                    //identityCredential.Identity = new BT.SaaS.Core.Shared.Entities.ClientIdentity();
                    //identityCredential.Identity.identiferNamepace = "EMAILNAME";
                    //identityCredential.Identity.IdentityAlias = IdentifierAliasEnum.no;
                    //identityCredential.Identity.identifier = identity;
                    user.Status = DataStatusEnum.done;
                    response.Header.Action = OrderActionEnum.deactivate;
                }
                else if (productCode.Equals(ConfigurationManager.AppSettings["CFProdCode"].ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    user.Status = DataStatusEnum.done;

                    response.Header.Action = OrderActionEnum.provide;
                }

                foreach (MSEO.OrderItem orderItem in request.Order.OrderItem)
                {
                    SaaSNS.ProductOrderItem productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
                    productOrderItem = new BT.SaaS.Core.Shared.Entities.ProductOrderItem();
                    productOrderItem.Header.Status = BT.SaaS.Core.Shared.Entities.DataStatusEnum.notDone;
                    productOrderItem.Header.Quantity = "1";
                    productOrderItem.XmlIdKey = "PO-" + productorderItemCount.ToString();

                    productOrderItem.Header.OrderItemKey = orderItem.Identifier.Id;
                    productOrderItem.Header.ProductCode = productCode;

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
                        roleInstance.RoleType = "DEFAULT";
                        roleInstance.XmlIdKey = "RI-" + productorderItemCount.ToString();

                        SaaSNS.ServiceInstance serviceInstance = new BT.SaaS.Core.Shared.Entities.ServiceInstance();
                        serviceInstance.ServiceRoles = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ServiceRole>();
                        SaaSNS.ServiceRole serviceRole = new BT.SaaS.Core.Shared.Entities.ServiceRole();
                        serviceRole.roleAttributes = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.Attribute>();

                        foreach (MSEO.Instance instance in orderItem.Instance)
                        {
                            foreach (MSEO.InstanceCharacteristic instanceCharacteristic in instance.InstanceCharacteristic)
                            {
                                if (productCode.Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"].ToString(), StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!instanceCharacteristic.Name.Equals("reason", StringComparison.OrdinalIgnoreCase))
                                    {
                                        SaaSNS.Attribute attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attribute.Name = instanceCharacteristic.Name;
                                        attribute.Value = instanceCharacteristic.Value;
                                        roleInstance.Attributes.Add(attribute);
                                    }
                                    else
                                    {
                                        barredReason = instanceCharacteristic.Value;
                                    }
                                }
                                else
                                {
                                    SaaSNS.Attribute attribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    attribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    attribute.Name = instanceCharacteristic.Name;
                                    attribute.Value = instanceCharacteristic.Value;
                                    roleInstance.Attributes.Add(attribute);

                                    if (instanceCharacteristic.Name.Equals("RBSID", StringComparison.OrdinalIgnoreCase))
                                        rbsid = instanceCharacteristic.Value;
                                }
                            }
                        }
                        if (productCode.Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"].ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            SaaSNS.Attribute reasonAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            reasonAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            reasonAttribute.Name = "reason";
                            reasonAttribute.Value = "BarMailbox";
                            roleInstance.Attributes.Add(reasonAttribute);

                            SaaSNS.Attribute typeAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            typeAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            typeAttribute.Name = "type";
                            typeAttribute.Value = iden;
                            roleInstance.Attributes.Add(typeAttribute);

                            SaaSNS.Attribute orderTypeAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            orderTypeAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            orderTypeAttribute.Name = "OrderType";
                            orderTypeAttribute.Value = "CompromisedEmail";
                            roleInstance.Attributes.Add(orderTypeAttribute);

                            SaaSNS.Attribute barredReasonAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            barredReasonAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            barredReasonAttribute.Name = "Barred_Reason";
                            barredReasonAttribute.Value = barredReason;
                            roleInstance.Attributes.Add(barredReasonAttribute);

                            SaaSNS.Attribute billActNumAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            billActNumAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            billActNumAttribute.Name = "BillAccountNumber";
                            billActNumAttribute.Value = billActNum;
                            roleInstance.Attributes.Add(billActNumAttribute);

                            SaaSNS.Attribute BToneIDAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            BToneIDAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            BToneIDAttribute.Name = "BtOneId";
                            BToneIDAttribute.Value = BTOneID;
                            roleInstance.Attributes.Add(BToneIDAttribute);

                            SaaSNS.Attribute CompromiseUnmergeEmail = new BT.SaaS.Core.Shared.Entities.Attribute();
                            CompromiseUnmergeEmail.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            CompromiseUnmergeEmail.Name = "CompromiseUnmergeEmail";
                            if (iden.Equals("NoOneId", StringComparison.OrdinalIgnoreCase) && ConfigurationManager.AppSettings["CompromiseUnmergeEmailSwitch"].Equals("ON", StringComparison.OrdinalIgnoreCase))
                            {
                                CompromiseUnmergeEmail.Value = "True";
                            }
                            else
                            {
                                CompromiseUnmergeEmail.Value = "False";
                            }
                            roleInstance.Attributes.Add(CompromiseUnmergeEmail);

                        }
                        else if (productCode.Equals(ConfigurationManager.AppSettings["CFProdCode"].ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            SaaSNS.Attribute cfsidAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            cfsidAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            cfsidAttribute.Name = "CFSID";
                            cfsidAttribute.Value = identity;
                            roleInstance.Attributes.Add(cfsidAttribute);

                            SaaSNS.Attribute isofspcactivation = new BT.SaaS.Core.Shared.Entities.Attribute();
                            isofspcactivation.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            isofspcactivation.Name = "ISOFSPCACTIVATION";
                            isofspcactivation.Value = "true";
                            roleInstance.Attributes.Add(isofspcactivation);

                            if (NominumProductProcessor.IsRBSIDExistsinDNP(rbsid))
                            {
                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "isRBSIDExists", Value = true.ToString() });
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
                        if (!response.ProductOrderItems.ToList().Exists(poi => poi.Header.ProductCode.Equals(productOrderItem.Header.ProductCode)))
                        {
                            productorderItemCount++;
                            response.ProductOrderItems.Add(productOrderItem);
                        }
                    }

                }
            }
            catch (MdmException mdmExcep)
            {
                throw mdmExcep;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return response;
        }
        #endregion

        # region WriteToLog
        public static void WriteToLog(TypeOfLog typeOfLog, string orderKey, string message, string productCode)
        {
            string folderPath = string.Empty;
            if ((orderKey.StartsWith("EE", StringComparison.OrdinalIgnoreCase)) && productCode.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"], StringComparison.OrdinalIgnoreCase))
            {
                folderPath = System.Configuration.ConfigurationManager.AppSettings["EESpringLogFilePath"] + "/" + System.DateTime.Now.ToString("ddMMyyyy");
            }
            else if (productCode.Equals(ConfigurationManager.AppSettings["BTSpringProductCode"], StringComparison.OrdinalIgnoreCase))
            {
                folderPath = System.Configuration.ConfigurationManager.AppSettings["SpringLogFilePath"] + "/" + System.DateTime.Now.ToString("ddMMyyyy");
            }
            else if (productCode.Equals(ConfigurationManager.AppSettings["BTSportsProductCode"], StringComparison.OrdinalIgnoreCase))
            {
                folderPath = System.Configuration.ConfigurationManager.AppSettings["LogFilePath"] + "/" + System.DateTime.Now.ToString("ddMMyyyy");
            }
            //Pavan - product code
            else if (productCode.Equals("S0341469", StringComparison.OrdinalIgnoreCase))
            {
                folderPath = System.Configuration.ConfigurationManager.AppSettings["VoInfinityLogFilePath"] + "/" + System.DateTime.Now.ToString("ddMMyyyy");
            }
            else if (productCode.Equals(ConfigurationManager.AppSettings["HCSProductCode"], StringComparison.OrdinalIgnoreCase))
            {
                folderPath = System.Configuration.ConfigurationManager.AppSettings["HCSLogFilePath"] + "/" + System.DateTime.Now.ToString("ddMMyyyy");
            }
            else if (productCode.Equals(ConfigurationManager.AppSettings["BTPlusMarkerProductCode"], StringComparison.OrdinalIgnoreCase)||
                productCode.Equals(ConfigurationManager.AppSettings["AssociateBTPlusMarkerProductCode"], StringComparison.OrdinalIgnoreCase))
            {
                folderPath = System.Configuration.ConfigurationManager.AppSettings["BTPlusMarkerLogFilePath"] + "/" + System.DateTime.Now.ToString("ddMMyyyy");
            }
            else if (productCode.Equals("MQNotification",StringComparison.OrdinalIgnoreCase))
            {
                folderPath = ConfigurationManager.AppSettings["MQNotificationLogFilePath"] + "/" + System.DateTime.Now.ToString("ddMMyyyy");
            }
            else if (productCode.Equals("BTConsumerBroadBand", StringComparison.OrdinalIgnoreCase))
            {
                folderPath = System.Configuration.ConfigurationManager.AppSettings["BTConsumerBroadBandLogFilePath"] + "/" + System.DateTime.Now.ToString("ddMMyyyy");
            }
            string filePath = null;

            // Prepare file extensions
            if (typeOfLog == TypeOfLog.MQ_IN_LOG)
            {
                filePath = string.Concat(folderPath, "/MQ-In-", orderKey, ".xml");
            }
            else if (typeOfLog == TypeOfLog.DNP_IN_LOG)
            {
                filePath = string.Concat(folderPath, "/DnP-Request-", orderKey, ".xml");
            }
            else if (typeOfLog == TypeOfLog.DNP_OUT_LOG)
            {
                filePath = string.Concat(folderPath, "/DnP-Response-", orderKey, ".xml");
            }
            else if (typeOfLog == TypeOfLog.MQ_OUT_LOG)
            {
                filePath = string.Concat(folderPath, "/MQ-Out-", orderKey, ".xml");
            }

            try
            {
                //Write logic to add directory
                using (StreamWriter writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    writer.Write(message);
                }
            }
            catch (DirectoryNotFoundException)
            {
                //Create Directory and Add files
                Directory.CreateDirectory(folderPath);
                using (StreamWriter writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    writer.Write(message);
                }
            }
        }
        #endregion

        #region TypeOfLog
        public enum TypeOfLog
        {
            MQ_IN_LOG,
            DNP_IN_LOG,
            DNP_OUT_LOG,
            MQ_OUT_LOG
        }
        #endregion

    }
}
