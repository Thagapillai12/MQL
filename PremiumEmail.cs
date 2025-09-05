using System;
using System.Configuration;
using System.Linq;
using MSEO = BT.SaaS.MSEOAdapter;
using SaaSNS = BT.SaaS.Core.Shared.Entities;
using BT.SaaS.IspssAdapter;
using BT.SaaS.Core.MDMAPI.Entities.Product;
using System.Collections.Generic;
using BT.SaaS.IspssAdapter.Dnp;
using System.Globalization;

namespace BT.SaaS.MSEOAdapter
{
    public class PremiumEmail
    {
        OrderRequest orderRequest;
        const string BACID_IDENTIFER_NAMEPACE = "VAS_BILLINGACCOUNT_ID";
        const string BTIEMAILID = "BTIEMAILID";


        public OrderRequest OrderRequest
        {
            get { return orderRequest; }
            set { orderRequest = value; }
        }

        public PremiumEmail(OrderRequest orderRequest)
        {
            OrderRequest = orderRequest;
        }

        internal bool IsPremiumEmailOrder(ref ProductVasClass premiumProductVasClass)
        {
            List<string> vasClassList = new List<string>();
            List<ProductVasClass> vasDefinitionList = new List<ProductVasClass>();
            if (OrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("vasclass")) && !string.IsNullOrEmpty(OrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("vasclass")).FirstOrDefault().Value))
            {
                vasClassList.Add(OrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("vasclass")).FirstOrDefault().Value);
            }
            try
            {
                if (vasClassList != null && vasClassList.Count > 0)
                {
                    vasDefinitionList = MdmWrapper.getSaaSVASDefs(vasClassList);
                    if (vasDefinitionList != null && vasDefinitionList.Count > 0)
                    {
                        if (vasDefinitionList.ToList().Exists(vp => vp.VasProductName.Equals("Premium Email", StringComparison.OrdinalIgnoreCase)))
                        {
                            premiumProductVasClass = vasDefinitionList.ToList().Where(vp => vp.VasProductName.Equals("Premium Email", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                            return true;
                        }
                    }
                }
            }
            catch (MdmException mdmException)
            {
                throw mdmException;
            }
            finally
            {
                vasClassList = null;
                vasDefinitionList = null;
            }
            return false;
        }

        internal bool IsBBPremiumEmailOrder()
        {
            if (OrderRequest != null && OrderRequest.Order != null && OrderRequest.Order.OrderItem != null
                && OrderRequest.Order.OrderItem[0] != null && OrderRequest.Order.OrderItem[0].Action != null && !String.IsNullOrEmpty(OrderRequest.Order.OrderItem[0].Action.Code)
                 && OrderRequest.Order.OrderItem[0].Specification != null && OrderRequest.Order.OrderItem[0].Specification[0] != null
                && OrderRequest.Order.OrderItem[0].Specification[0].Identifier != null && !string.IsNullOrEmpty(OrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1))
            {
                string productCode = OrderRequest.Order.OrderItem[0].Specification[0].Identifier.Value1;

                if (productCode.Equals(ConfigurationManager.AppSettings["BBPremiumEmailScode"].ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        internal bool IsBasicEmailOrder(ref ProductVasClass premiumProductVasClass)
        {
            List<string> vasClassList = new List<string>();
            List<ProductVasClass> vasDefinitionList = new List<ProductVasClass>();
            if (OrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("vasclass")) && !string.IsNullOrEmpty(OrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("vasclass")).FirstOrDefault().Value))
            {
                vasClassList.Add(OrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("vasclass")).FirstOrDefault().Value);
            }
            try
            {
                if (vasClassList != null && vasClassList.Count > 0)
                {
                    vasDefinitionList = MdmWrapper.getSaaSVASDefs(vasClassList);
                    if (vasDefinitionList != null && vasDefinitionList.Count > 0)
                    {
                        if (vasDefinitionList.ToList().Exists(vp => vp.VasProductName.Equals("BT Basic Mail", StringComparison.OrdinalIgnoreCase)))
                        {
                            premiumProductVasClass = vasDefinitionList.ToList().Where(vp => vp.VasProductName.Equals("BT Basic Mail", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                            return true;
                        }
                    }
                }
            }
            catch (MdmException mdmException)
            {
                throw mdmException;
            }
            finally
            {
                vasClassList = null;
                vasDefinitionList = null;
            }
            return false;
        }

        internal SaaSNS.Order MapPremiumEmailProvisionRequest(ProductVasClass premiumProdVasClass)
        {
            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Mapping Premium Email Provision request");
            SaaSNS.Order response = new SaaSNS.Order();
            System.DateTime orderDate = new System.DateTime();
            string firstEmail = string.Empty, EmailName = string.Empty, oldBakId = string.Empty, newBakId = string.Empty, emailDataStore = string.Empty, siForPAYG = string.Empty, siDomain = string.Empty;
            int productorderItemCount = 0;
            string emailSupplier = "MX";
            bool isAHTDone = false;
            bool isPaygCustomer = false; bool isPendingReinstateOrder = false;
            bool bacSIexists = true;
            bool CompletePremiumProvision = false;
            bool isprimaryaffiliate = false;
            bool isEmaillinkedtoSameBAC = false;
            bool isCeasedBBmailbox = false;
            string BtOneId = string.Empty;
            string rolestatus = string.Empty; string reason = string.Empty;
            response.Header.CeaseReason = OrderRequest.SerializeObject();

            List<string> emailList = new List<string>();

            response.Header.OrderKey = OrderRequest.Order.OrderIdentifier.Value;
            response.Header.ServiceProviderID = Settings1.Default.ConsumerServiceProviderId;
            if (OrderRequest.Order.OrderItem[0].RequiredDateTime != null && OrderRequest.Order.OrderItem[0].RequiredDateTime.DateTime1 != null)
            {
                if (MSEOSaaSMapper.convertDateTime(OrderRequest.Order.OrderItem[0].RequiredDateTime.DateTime1, ref orderDate))
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

            try
            {
                switch (OrderRequest.Order.OrderItem[0].Action.Code.ToLower())
                {
                    case ("create"):
                    case ("add"):
                        response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.provide;
                        break;
                    default:
                        throw new Exception("Action not supported");
                }

                response.ProductOrderItems = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ProductOrderItem>(OrderRequest.Order.OrderItem.Length);

                foreach (MSEO.OrderItem orderItem in OrderRequest.Order.OrderItem)
                {
                    SaaSNS.ProductOrderItem productOrderItem = new SaaSNS.ProductOrderItem();
                    productOrderItem.Header.Status = SaaSNS.DataStatusEnum.notDone;
                    productOrderItem.Header.Quantity = "1";
                    productOrderItem.XmlIdKey = "PO-" + productorderItemCount.ToString();

                    productOrderItem.Header.OrderItemKey = orderItem.Identifier.Id;
                    if (ConfigurationManager.AppSettings["EmailScodes"].Split(',').Contains(orderItem.Specification[0].Identifier.Value1) || ConfigurationManager.AppSettings["PremiumEmailScodes"].Split(',').Contains(orderItem.Specification[0].Identifier.Value1))
                    {
                        productOrderItem.Header.ProductCode = ConfigurationManager.AppSettings["DanteEMailProdCode"].ToString();
                    }
                    else
                    {
                        productOrderItem.Header.ProductCode = premiumProdVasClass.VasProductFamily;
                    }

                    System.Collections.Generic.List<string> inputProductDefinitionList = new System.Collections.Generic.List<string>();
                    inputProductDefinitionList.Add(productOrderItem.Header.ProductCode);

                    System.Collections.Generic.List<ProductDefinition> productDefinition = MdmWrapper.getSaaSProductDefs(inputProductDefinitionList, Settings1.Default.ConsumerServiceProviderId);

                    if (productDefinition.Count == 0)
                    {
                        orderItem.Status = Settings1.Default.IgnoredStatus;
                        Logger.Debug("Product Code not found in Order : " + OrderRequest.Order.OrderIdentifier.Value + " for order item " + orderItem.Identifier.Id);
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

                        if (productOrderItem.Header.ProductCode.Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"], StringComparison.OrdinalIgnoreCase))
                        {
                            GetClientProfileV1Res getprofileResponse = null;
                            ClientServiceInstanceV1 emailService = null;
                            ClientServiceInstanceV1 getServices = null;
                            GetClientProfileV1Res oldBACProfile = null;
                            GetBatchProfileV1Res oldBACSIList = null;

                            if (OrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("emailname")) && !string.IsNullOrEmpty(OrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailname")).FirstOrDefault().Value))
                            {
                                EmailName = OrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailname")).FirstOrDefault().Value;
                            }
                            if (OrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("billaccountnumber")) && !string.IsNullOrEmpty(OrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value))
                            {
                                newBakId = OrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value;
                            }
                            getprofileResponse = DnpWrapper.GetClientProfileV1(EmailName, BTIEMAILID);


                            if (getprofileResponse != null && getprofileResponse.clientProfileV1 != null && getprofileResponse.clientProfileV1.clientServiceInstanceV1 != null)
                            {
                                emailDataStore = DanteRequestProcessor.GenerateEmailDataStore(getprofileResponse.clientProfileV1, "premiumemail", EmailName, ref emailSupplier);
                                if (emailDataStore == "D&P")
                                {

                                    emailService = (from si in getprofileResponse.clientProfileV1.clientServiceInstanceV1
                                                    where si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"].ToString(), StringComparison.OrdinalIgnoreCase)
                                                    && si.clientServiceRole != null
                                                    && si.clientServiceRole.ToList().Exists(csr => csr.id.Equals("Default", StringComparison.OrdinalIgnoreCase) && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)))
                                                    select si).FirstOrDefault();

                                    // Check for PAYG Customer or delinked and set the SIFORPAYG attribute if the BAC is not present at Service Identity level.
                                    isPaygCustomer = DanteRequestProcessor.IsPAYGCustomer(getprofileResponse.clientProfileV1, EmailName);                                    
                                    isPendingReinstateOrder = DanteRequestProcessor.IsPendingReinstateOrder(getprofileResponse.clientProfileV1, EmailName,ref reason, ref isCeasedBBmailbox);

                                    if (emailService.serviceIdentity.ToList().Exists(sid => sid.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        oldBakId = emailService.serviceIdentity.ToList().Where(sid => sid.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        firstEmail = emailService.serviceIdentity.ToList().Where(sid => sid.domain.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        siForPAYG = oldBakId;
                                        siDomain = BACID_IDENTIFER_NAMEPACE;
                                    }
                                    // PAYG without BAC at Service Identity 
                                    // getting the BAC details from Admin role instead of client identities as there is a chance of affliate
                                    else if (isPaygCustomer && emailService.clientServiceRole.ToList().Exists(csr => csr.id.Equals("admin", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        bacSIexists = false;// BAC at service identity level
                                        var BakId = from si in emailService.clientServiceRole
                                                    where si.clientIdentity.ToList().Exists(csr => csr.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase))
                                                    select si.clientIdentity.ToList().Where(csr => csr.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        oldBakId = BakId.ToList().FirstOrDefault();
                                        firstEmail = emailService.serviceIdentity.ToList().Where(sid => sid.domain.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        siForPAYG = firstEmail;
                                        siDomain = BTIEMAILID;
                                    }
                                    // PAYG without BAC at Service Identity and No Admin role
                                    // Get the primary email from Service Identity level
                                    else if (isPaygCustomer && emailService.serviceIdentity.ToList().Exists(sid => sid.domain.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        bacSIexists = false;
                                        firstEmail = emailService.serviceIdentity.ToList().Where(sid => sid.domain.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        siForPAYG = firstEmail;
                                        siDomain = BTIEMAILID;
                                    }
                                    //for pending reinstatement scenarios..
                                    else if (isPendingReinstateOrder && emailService.serviceIdentity.ToList().Exists(sid => sid.domain.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        bacSIexists = false;
                                        firstEmail = emailService.serviceIdentity.ToList().Where(sid => sid.domain.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        siForPAYG = firstEmail;
                                        siDomain = BTIEMAILID;
                                    }

                                    if (!isPendingReinstateOrder)
                                    {
                                        if (getprofileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(firstEmail, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            CompletePremiumProvision = true;
                                        }

                                        if (!string.IsNullOrEmpty(oldBakId))
                                        {
                                            oldBACProfile = DnpWrapper.GetClientProfileV1(oldBakId, BACID_IDENTIFER_NAMEPACE);

                                            if (oldBakId.Equals(newBakId))
                                            {
                                                isEmaillinkedtoSameBAC = true;
                                            }
                                        }

                                        if (oldBACProfile != null && oldBACProfile.clientProfileV1 != null)
                                        {
                                            isAHTDone = true;
                                        }
                                        else
                                        {
                                            isAHTDone = false;
                                        }

                                    }
                                }
                                else
                                {
                                    throw new DnpException("Ignored the request as EmailDataStore is D&P");
                                }
                            }
                            else
                            {
                                throw new DnpException("Profile doesn't have services in DNP");
                            }
                            List<SaaSNS.Attribute> attrList = new List<SaaSNS.Attribute>();
                            if (!isPendingReinstateOrder)
                            {
                                #region premiumOldcode to be moved
                                if (isAHTDone)
                                {
                                    List<ClientServiceInstanceV1> emailServiceInstanceList = new List<ClientServiceInstanceV1>();

                                    if ((oldBACProfile != null && oldBACProfile.clientProfileV1 != null) || isPaygCustomer)
                                    {
                                        if (oldBACProfile.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase))
                                            || (!bacSIexists && getprofileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase))))
                                        {
                                            CompletePremiumProvision = true;

                                            oldBACSIList = DnpWrapper.GetServiceUserProfilesV1ForDante(siForPAYG, siDomain);

                                            if (oldBACSIList != null && oldBACSIList.clientProfileV1 != null && oldBACSIList.clientProfileV1.Count() > 0)
                                            {
                                                foreach (ClientProfileV1 profile in oldBACSIList.clientProfileV1)
                                                {

                                                    if (profile.clientServiceInstanceV1 != null && profile.clientServiceInstanceV1.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase)))
                                                    {
                                                        ClientServiceInstanceV1 emailServiceInstance = (from si in profile.clientServiceInstanceV1
                                                                                                        where si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"].ToString(), StringComparison.OrdinalIgnoreCase) && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(siDomain, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(siForPAYG, StringComparison.OrdinalIgnoreCase))
                                                                                                        select si).FirstOrDefault();
                                                        emailServiceInstanceList.Add(emailServiceInstance);
                                                    }
                                                }

                                                emailList = GetListOfEmails(emailServiceInstanceList);
                                            }
                                        }
                                        else
                                        {
                                            CompletePremiumProvision = false;
                                            emailServiceInstanceList.Add(emailService);
                                            emailList = GetListOfEmails(emailServiceInstanceList);
                                        }
                                    }
                                }
                                else
                                {
                                    List<ClientServiceInstanceV1> emailServiceInstanceList = new List<ClientServiceInstanceV1>();
                                    if (CompletePremiumProvision)
                                    {
                                        oldBACSIList = DnpWrapper.GetServiceUserProfilesV1ForDante(siForPAYG, siDomain);

                                        if (oldBACSIList != null && oldBACSIList.clientProfileV1 != null && oldBACSIList.clientProfileV1.Count() > 0)
                                        {
                                            foreach (ClientProfileV1 profile in oldBACSIList.clientProfileV1)
                                            {

                                                if (profile.clientServiceInstanceV1 != null && profile.clientServiceInstanceV1.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    ClientServiceInstanceV1 emailServiceInstance = (from si in profile.clientServiceInstanceV1
                                                                                                    where si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"].ToString(), StringComparison.OrdinalIgnoreCase) && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(siDomain, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(siForPAYG, StringComparison.OrdinalIgnoreCase))
                                                                                                    select si).FirstOrDefault();
                                                    emailServiceInstanceList.Add(emailServiceInstance);
                                                    emailServiceInstance = null;
                                                }
                                            }
                                        }
                                        emailList = GetListOfEmails(emailServiceInstanceList);

                                    }
                                    else
                                    {
                                        emailServiceInstanceList.Add(emailService);
                                        emailList = GetListOfEmails(emailServiceInstanceList);
                                    }
                                }

                                #region BTRCE-124539 for Primary Affiliate Premium Provision Scenario
                                oldBACSIList = DnpWrapper.GetServiceUserProfilesV1ForDante(oldBakId, BACID_IDENTIFER_NAMEPACE);
                                string primaryEmailname = string.Empty;
                                bool isMultiplemailbox = false;
                                bool ISTargetAHT = false;
                                string newprimarymail = string.Empty;
                                int btOneIdloopcount = 0;



                                if (oldBACSIList != null && oldBACSIList.clientProfileV1 != null && oldBACSIList.clientProfileV1.Count() > 0)
                                {
                                    foreach (ClientProfileV1 profile in oldBACSIList.clientProfileV1)
                                    {
                                        // primary mailbox 
                                        if (string.IsNullOrEmpty(primaryEmailname))
                                        {
                                            var SIName = from si in profile.clientServiceInstanceV1
                                                         where ((si.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase)) && si.serviceIdentity.ToList().Exists(csr => csr.value.Equals(oldBakId, StringComparison.OrdinalIgnoreCase)))
                                                         select si.serviceIdentity.ToList().Where(sri => sri.domain.Equals("BTIEMAILID")).FirstOrDefault().value;
                                            if (SIName.Any())
                                                primaryEmailname = SIName.ToList().FirstOrDefault();
                                            else
                                                continue;//As no email servioce is linked..
                                        }

                                        // primary affiliate check
                                        if (primaryEmailname.Equals(EmailName, StringComparison.OrdinalIgnoreCase) && !isprimaryaffiliate)
                                        {
                                            if (oldBACProfile != null && oldBACProfile.clientProfileV1 != null && oldBACProfile.clientProfileV1.client != null && oldBACProfile.clientProfileV1.client.clientIdentity != null)
                                            {
                                                if (oldBACProfile.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase))
                                                    || (!bacSIexists && getprofileResponse.clientProfileV1.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase))))
                                                {
                                                    // Do Nothing...
                                                }
                                                else
                                                    isprimaryaffiliate = true;
                                            }

                                        }

                                        getServices = (from si in profile.clientServiceInstanceV1
                                                       where ((si.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase)) && si.serviceIdentity.ToList().Exists(siv => siv.value.Equals(oldBakId, StringComparison.OrdinalIgnoreCase)))
                                                       select si).FirstOrDefault();
                                        if (getServices != null)
                                        {

                                            var defaulttroles = from si in profile.clientServiceInstanceV1
                                                                where (si.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase))
                                                                select si.clientServiceRole.ToList().Where(sri => sri.id.Equals("DEFAULT")).ToList();

                                            if (defaulttroles.Any() && defaulttroles.FirstOrDefault().Count > 0)
                                            {
                                                btOneIdloopcount++;

                                                if (btOneIdloopcount > 1)
                                                {
                                                    isMultiplemailbox = true;
                                                    break;
                                                }
                                                else
                                                {
                                                    isMultiplemailbox = false;
                                                }
                                            }
                                        }
                                    }

                                    #region ifPrimaryAffilate with Multiple mailboxes
                                    if (isprimaryaffiliate && isMultiplemailbox)
                                    {
                                        // getting new primary mailbox based on first created date

                                        System.DateTime newcreationDate = System.DateTime.MaxValue;

                                        foreach (ClientProfileV1 clp in oldBACSIList.clientProfileV1)
                                        {
                                            var defaulttroles = from si in clp.clientServiceInstanceV1
                                                                where (si.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase))
                                                                select si.clientServiceRole.ToList().Where(sri => sri.id.Equals("DEFAULT")).ToList();

                                            foreach (List<ClientServiceRole> csr in defaulttroles)
                                            {
                                                foreach (ClientServiceRole csri in csr)
                                                {
                                                    System.DateTime creationdate = System.DateTime.ParseExact(csri.createdDate, "ddMMyyyyHHmmss", CultureInfo.InvariantCulture);

                                                    if (!csri.clientIdentity[0].value.Equals(EmailName, StringComparison.InvariantCultureIgnoreCase))
                                                    {
                                                        if (creationdate < newcreationDate)
                                                        {
                                                            newcreationDate = creationdate;
                                                            newprimarymail = csri.clientIdentity[0].value;
                                                        }

                                                    }
                                                }
                                            }
                                        }
                                    }
                                    #endregion
                                }



                                GetClientProfileV1Res gcpresponse = DnpWrapper.GetClientProfileV1(newBakId, "VAS_BILLINGACCOUNT_ID");
                                if (gcpresponse != null && gcpresponse.clientProfileV1 != null && gcpresponse.clientProfileV1.client != null

                                    && gcpresponse.clientProfileV1.client.clientIdentity != null)
                                {
                                    var accounttrust = from si in gcpresponse.clientProfileV1.client.clientIdentity
                                                       where ((si.value.Equals(newBakId, StringComparison.OrdinalIgnoreCase)) && si.clientIdentityValidation != null)
                                                       select si.clientIdentityValidation.ToList().Where(ci => ci.name.Equals("ACCOUNTTRUSTMETHOD")).FirstOrDefault().value;

                                    if (accounttrust.Any())
                                    {
                                        if (ConfigurationManager.AppSettings["AccountTrustMethodStatus"].Split(',').Contains(accounttrust.ToList().FirstOrDefault().ToLower()))

                                            ISTargetAHT = true;
                                    }
                                }
                                else if(isCeasedBBmailbox)
                                {
                                    string btOneId = GetBtOneID(getprofileResponse.clientProfileV1);
                                    
                                    if (!string.IsNullOrEmpty(btOneId))
                                    {
                                        string e2eData1 = (OrderRequest.StandardHeader != null && OrderRequest.StandardHeader.E2e != null && OrderRequest.StandardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(OrderRequest.StandardHeader.E2e.E2EDATA.ToString())) ? OrderRequest.StandardHeader.E2e.E2EDATA.ToString() : string.Empty;
                                        if (AHTRequestProcessor.DoAHTforReinstatement(btOneId, newBakId, e2eData1))
                                        {
                                            ISTargetAHT = true;
                                        }
                                        SaaSNS.Attribute attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                        attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                        attr.Name = "BTONEID";
                                        attr.Value = btOneId;
                                        roleInstance.Attributes.Add(attr);
                                    }

                                }

                                if (isprimaryaffiliate)
                                {
                                    SaaSNS.Attribute IsPrimaryAffiliate = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    IsPrimaryAffiliate.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    IsPrimaryAffiliate.Name = "IsPrimaryAffiliatePremiumProvision";
                                    IsPrimaryAffiliate.Value = isprimaryaffiliate ? "true" : "false";
                                    roleInstance.Attributes.Add(IsPrimaryAffiliate);

                                    SaaSNS.Attribute newprimaryemail = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    newprimaryemail.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                    newprimaryemail.Name = "NewPrimaryEmail";
                                    newprimaryemail.Value = newprimarymail;
                                    roleInstance.Attributes.Add(newprimaryemail);
                                }

                                #endregion
                                if (btOneIdloopcount == 1)
                                    CompletePremiumProvision = true;
                                //CompletePremiumProvision
                                SaaSNS.Attribute isAffiliateCheck = new BT.SaaS.Core.Shared.Entities.Attribute();
                                isAffiliateCheck.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                isAffiliateCheck.Name = "CompletePremiumProvision";
                                isAffiliateCheck.Value = CompletePremiumProvision.ToString();
                                roleInstance.Attributes.Add(isAffiliateCheck);

                                SaaSNS.Attribute MultipleMails = new BT.SaaS.Core.Shared.Entities.Attribute();
                                MultipleMails.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                MultipleMails.Name = "IsMultipleMailsLinked";
                                MultipleMails.Value = isMultiplemailbox ? "true" : "false";
                                roleInstance.Attributes.Add(MultipleMails);

                                SaaSNS.Attribute targetAHTAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                targetAHTAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                targetAHTAttribute.Name = "IsTargetAHTDone";
                                targetAHTAttribute.Value = ISTargetAHT ? "true" : "false";
                                roleInstance.Attributes.Add(targetAHTAttribute);

                                //PrimaryEmail
                                SaaSNS.Attribute primaryEmailName = new BT.SaaS.Core.Shared.Entities.Attribute();
                                primaryEmailName.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                primaryEmailName.Name = "PRIMARYEMAIL";
                                primaryEmailName.Value = primaryEmailname;
                                roleInstance.Attributes.Add(primaryEmailName);

                                string SIBacNumber = oldBakId;
                                SaaSNS.Attribute siBacNumber = new BT.SaaS.Core.Shared.Entities.Attribute();
                                siBacNumber.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                siBacNumber.Name = "SIBACNUMBER";
                                siBacNumber.Value = SIBacNumber.ToString();
                                roleInstance.Attributes.Add(siBacNumber);

                                //isPaygCustomerk
                                SaaSNS.Attribute isPAYGCustomer = new BT.SaaS.Core.Shared.Entities.Attribute();
                                isPAYGCustomer.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                isPAYGCustomer.Name = "ISPAYGCUSTOMER";
                                isPAYGCustomer.Value = isPaygCustomer.ToString();
                                roleInstance.Attributes.Add(isPAYGCustomer);

                                //isBACSIExistsforPAYG
                                SaaSNS.Attribute isBACSIExists = new BT.SaaS.Core.Shared.Entities.Attribute();
                                isBACSIExists.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                isBACSIExists.Name = "ISBACSIExistsforPAYG";
                                isBACSIExists.Value = bacSIexists.ToString();
                                roleInstance.Attributes.Add(isBACSIExists);

                                //SIFORPAYG
                                SaaSNS.Attribute SIFORPAYG = new BT.SaaS.Core.Shared.Entities.Attribute();
                                SIFORPAYG.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                SIFORPAYG.Name = "SIFORPAYG";
                                SIFORPAYG.Value = siForPAYG;
                                roleInstance.Attributes.Add(SIFORPAYG);

                                //isEmaillinkedtoSameBAC
                                SaaSNS.Attribute isEmaillinkedtoSameBACAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
                                isEmaillinkedtoSameBACAttr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                isEmaillinkedtoSameBACAttr.Name = "IsEmaillinkedtoSameBAC";
                                isEmaillinkedtoSameBACAttr.Value = isEmaillinkedtoSameBAC ? "true" : "false";
                                roleInstance.Attributes.Add(isEmaillinkedtoSameBACAttr);

                                if (emailList != null && emailList.Count > 0)
                                {
                                    if (emailList[0] != null)
                                    {
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ListOfEmails", Value = emailList[0] });
                                    }
                                    if (emailList[1] != null)
                                    {
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "InactiveEmailList", Value = emailList[1] });
                                    }
                                    if (emailList[2] != null)
                                    {
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "Unresolvedemaillist", Value = emailList[2] });
                                    }
                                    if (emailList[3] != null)
                                    {
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "UnmergedEmaillist", Value = emailList[3] });
                                    }
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

                                SaaSNS.Attribute is_AHTClient = new BT.SaaS.Core.Shared.Entities.Attribute();
                                is_AHTClient.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                is_AHTClient.Name = "ISAHTCLIENT";
                                is_AHTClient.Value = isAHTDone.ToString();
                                roleInstance.Attributes.Add(is_AHTClient);

                                SaaSNS.Attribute VASProduct_ID = new BT.SaaS.Core.Shared.Entities.Attribute();
                                VASProduct_ID.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                                VASProduct_ID.Name = "VASPRODUCTID";
                                VASProduct_ID.Value = premiumProdVasClass.VasProductId;
                                roleInstance.Attributes.Add(VASProduct_ID);

                                if (response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.provide)
                                {
                                    if (premiumProdVasClass.VasProductId.Equals("VPEM000060", StringComparison.OrdinalIgnoreCase))
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "REASON", Value = isCeasedBBmailbox ? "bbbasicmailvasprovision": "BasicMailVASProvision" });
                                    else
                                        roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "REASON", Value = "PremiumMailVASProvision" });
                                }
                                #endregion
                            }
                            else
                            {
                                string e2eData = (OrderRequest.StandardHeader != null && OrderRequest.StandardHeader.E2e != null && OrderRequest.StandardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(OrderRequest.StandardHeader.E2e.E2EDATA.ToString())) ? OrderRequest.StandardHeader.E2e.E2EDATA.ToString() : string.Empty;
                                string BAC = roleInstance.Attributes.ToList().Where(attr => attr.Name.Equals("BillAccountNumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value;
                                MapReinstatementRequest(premiumProdVasClass, EmailName, getprofileResponse.clientProfileV1, emailService, siForPAYG, siDomain, BAC, reason, e2eData, ref roleInstance);
                            }
                            SaaSNS.Attribute BptmE2EDataAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            BptmE2EDataAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            BptmE2EDataAttribute.Name = "E2EDATA";
                            if (OrderRequest.StandardHeader != null && OrderRequest.StandardHeader.E2e != null && OrderRequest.StandardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(OrderRequest.StandardHeader.E2e.E2EDATA.ToString()))
                                BptmE2EDataAttribute.Value = OrderRequest.StandardHeader.E2e.E2EDATA.ToString();
                            else
                                BptmE2EDataAttribute.Value = string.Empty;
                            roleInstance.Attributes.Add(BptmE2EDataAttribute);

                            serviceInstance.ServiceRoles.Add(serviceRole);
                            productOrderItem.ServiceInstances.Add(serviceInstance);
                            productOrderItem.RoleInstances.Add(roleInstance);
                            productorderItemCount++;

                            response.ProductOrderItems.Add(productOrderItem);
                        }
                    }
                }
            }
            catch (DnpException dnpException)
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter Mapping Premium Email Exception : " + dnpException.ToString());
                throw dnpException;
            }
            catch (MdmException mdmException)
            {
                throw mdmException;
            }
            catch (Exception ex)
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter Mapping Premium Email Exception : " + ex.ToString());
                throw ex;
            }
            finally
            {
            }

            return response;
        }

        internal void MapReinstatementRequest(ProductVasClass premiumProdVasClass, string EmailName, ClientProfileV1 profile, ClientServiceInstanceV1 emailService, string siforPayG, string siDomain, string BAC, string reason, string e2eData, ref SaaSNS.RoleInstance roleInstance)
        {
            SaaSNS.Attribute attr = null; string siBAC = string.Empty; bool adminRoleExists = isAdminRoleExists(emailService); bool siExists = isBACSIExists(emailService, ref siBAC);
            string newPrimaryEmail = string.Empty; string btOneId = GetBtOneID(profile);
            string primaryEmail = getPrimaryEamil(emailService);

            if (!string.IsNullOrEmpty(btOneId))
            {
                AHTRequestProcessor.DoAHTforReinstatement(btOneId, BAC, e2eData);

                attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                attr.Name = "BTONEID";
                attr.Value = btOneId;
                roleInstance.Attributes.Add(attr);
            }

            attr = new BT.SaaS.Core.Shared.Entities.Attribute();
            attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
            attr.Name = "REASON";
            attr.Value = reason;
            roleInstance.Attributes.Add(attr);


            //Defaulting to basic as we support only Basic Email reinstatement
            attr = new BT.SaaS.Core.Shared.Entities.Attribute();
            attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
            attr.Name = "VASPRODUCTID";
            attr.Value = "VPEM000060";
            roleInstance.Attributes.Add(attr);

            //To check if AccountHolder mailbox reactivation 
            //Check if BAC exists at Admin and SI level. Unlink and link new BAC in case of last email
            attr = new BT.SaaS.Core.Shared.Entities.Attribute();
            attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
            attr.Name = "ISAHTCLIENT";
            attr.Value = adminRoleExists ? "True" : "False";
            roleInstance.Attributes.Add(attr);

            if (!roleInstance.Attributes.ToList().Exists(a => a.Name.Equals("EMAILSUPPLIER", StringComparison.OrdinalIgnoreCase)))
            {
                attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                attr.Name = "EMAILSUPPLIER";
                attr.Value = GetSupplierFromidentity(profile, EmailName);
                roleInstance.Attributes.Add(attr);
            }

            attr = new BT.SaaS.Core.Shared.Entities.Attribute();
            attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
            attr.Name = "ISPAYGCUSTOMER";//isBACSIexists
            attr.Value = siExists ? "True" : "False";
            roleInstance.Attributes.Add(attr);

            if (siExists)
            {
                attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                attr.Name = "sibacnumber";
                attr.Value = siBAC;
                roleInstance.Attributes.Add(attr);
            }

            //Search for linked mailboxes only in case BAC exists at SI level
            if (siExists || adminRoleExists)
            {
                attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                attr.Name = "IsMultipleMailsLinked";
                attr.Value = areMultipleEmailsLinked(siforPayG, siDomain, EmailName, ref newPrimaryEmail) ? "True" : "False";
                roleInstance.Attributes.Add(attr);

                bool isPrimaryEmail = false;
                bool isAffiliateEmail = false;
                bool IsLastEmailMove = false;
                int emailCount = 0;
                if (EmailName.Equals(primaryEmail))
                    isPrimaryEmail = true;

                //if (!string.IsNullOrEmpty(BAC))
                //    IsLastEmailMove = DanteRequestProcessor.isLastEmailmoved(BAC, BACID_IDENTIFER_NAMEPACE, EmailName, isPrimaryEmail, ref isAffiliateEmail, ref newPrimaryEmail, "reinstatebasic");
                //else 
                if (!string.IsNullOrEmpty(siBAC))
                    IsLastEmailMove = DanteRequestProcessor.isLastEmailmoved(siBAC, BACID_IDENTIFER_NAMEPACE, EmailName, isPrimaryEmail, ref isAffiliateEmail, ref newPrimaryEmail, "reinstatebasic", ref emailCount);
                else if (!string.IsNullOrEmpty(primaryEmail))
                    IsLastEmailMove = DanteRequestProcessor.isLastEmailmoved(primaryEmail, "BTIEMAILID", EmailName, isPrimaryEmail, ref isAffiliateEmail, ref newPrimaryEmail, "reinstatebasic", ref emailCount);


                if (!string.IsNullOrEmpty(newPrimaryEmail))
                {
                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    attr.Name = "NewPrimaryEmail";
                    attr.Value = newPrimaryEmail;
                    roleInstance.Attributes.Add(attr);
                }
                if (!string.IsNullOrEmpty(primaryEmail))
                {
                    attr = new BT.SaaS.Core.Shared.Entities.Attribute();
                    attr.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                    attr.Name = "SourcePrimayemailId";
                    attr.Value = primaryEmail;
                    roleInstance.Attributes.Add(attr);
                }
                SaaSNS.Attribute lastemailMove = new BT.SaaS.Core.Shared.Entities.Attribute();
                lastemailMove.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                lastemailMove.Name = "IsLastEmailMove";
                lastemailMove.Value = IsLastEmailMove ? "true" : "false";
                roleInstance.Attributes.Add(lastemailMove);

                SaaSNS.Attribute PrimaryEmailMove = new BT.SaaS.Core.Shared.Entities.Attribute();
                PrimaryEmailMove.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                PrimaryEmailMove.Name = "IsPrimaryEmailMove";
                PrimaryEmailMove.Value = isPrimaryEmail ? "true" : "false";
                roleInstance.Attributes.Add(PrimaryEmailMove);
            }
        }

        private string getPrimaryEamil(ClientServiceInstanceV1 emailService)
        {
            string primaryemail = string.Empty;
            if (emailService != null & emailService.serviceIdentity != null && emailService.serviceIdentity.Count() > 0 && emailService.serviceIdentity.ToList().Exists(si => si.domain.Equals("BTIEMAILID")))
            {
                primaryemail = emailService.serviceIdentity.ToList().Where(si => si.domain.Equals("BTIEMAILID")).FirstOrDefault().value;
            }
            return primaryemail;
        }

        internal string GetBtOneID(ClientProfileV1 profile)
        {
            //if (profile.client != null && profile.client.clientIdentity != null)
            //{
            //    if (profile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)))
            //    {
            //        if (!string.IsNullOrEmpty(profile.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value))
            //        {
            //            string BTCOM = profile.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
            //        }
            //    }
            //}

            return profile.client != null && profile.client.clientIdentity != null && profile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase))
                && !string.IsNullOrEmpty(profile.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value) ? profile.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("BTCOM", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value : string.Empty;
        }
        internal bool isAdminRoleExists(ClientServiceInstanceV1 emailService)
        {
            return emailService.clientServiceRole != null && emailService.clientServiceRole.ToList().Exists(csr => csr.id.Equals("Admin", StringComparison.OrdinalIgnoreCase) && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase))) ? true : false;
        }

        internal bool isBACSIExists(ClientServiceInstanceV1 emailService, ref string SIBac)
        {
            SIBac = emailService.serviceIdentity != null && emailService.serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE)) ? emailService.serviceIdentity.ToList().Where(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE)).FirstOrDefault().value:string.Empty;
            return emailService.serviceIdentity != null && emailService.serviceIdentity.ToList().Exists(si => si.domain.Equals(BACID_IDENTIFER_NAMEPACE)) ? true : false;
        }
        internal bool areMultipleEmailsLinked(string siForPAYG, string siDomain, string EmailName, ref string newPrimaryEmail)
        {
            #region BTRCE-124539 for Primary Affiliate Premium Provision Scenario
            GetBatchProfileV1Res oldBACSIList = null; ClientServiceInstanceV1 getServices = null;
            oldBACSIList = DnpWrapper.GetServiceUserProfilesV1ForDante(siForPAYG, siDomain);
            string primaryEmailname = string.Empty;
            bool isMultiplemailbox = false;
            bool isprimary = false;
            string newprimarymail = string.Empty;
            int btOneIdloopcount = 0;

            if (oldBACSIList != null && oldBACSIList.clientProfileV1 != null && oldBACSIList.clientProfileV1.Count() > 0)
            {
                foreach (ClientProfileV1 profile in oldBACSIList.clientProfileV1)
                {
                    // primary mailbox 
                    if (string.IsNullOrEmpty(primaryEmailname))
                    {
                        var SIName = from si in profile.clientServiceInstanceV1
                                     where ((si.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase)) && si.serviceIdentity.ToList().Exists(csr => csr.value.Equals(siForPAYG, StringComparison.OrdinalIgnoreCase)))
                                     select si.serviceIdentity.ToList().Where(sri => sri.domain.Equals("BTIEMAILID")).FirstOrDefault().value;
                        if (SIName.Any())
                            primaryEmailname = SIName.ToList().FirstOrDefault();
                        else
                            continue;//As no email servioce is linked..
                    }

                    // primary check
                    if (primaryEmailname.Equals(EmailName, StringComparison.OrdinalIgnoreCase) && !isprimary)
                    {
                        isprimary = true;
                    }

                    getServices = (from si in profile.clientServiceInstanceV1
                                   where ((si.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase)) && si.serviceIdentity.ToList().Exists(siv => siv.value.Equals(siForPAYG, StringComparison.OrdinalIgnoreCase)))
                                   select si).FirstOrDefault();
                    if (getServices != null)
                    {

                        var defaulttroles = from si in profile.clientServiceInstanceV1
                                            where (si.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase))
                                            select si.clientServiceRole.ToList().Where(sri => sri.id.Equals("DEFAULT")).ToList();

                        if (defaulttroles.Any() && defaulttroles.FirstOrDefault().Count > 0)
                        {
                            btOneIdloopcount++;

                            if (btOneIdloopcount > 1)
                            {
                                isMultiplemailbox = true;
                                break;
                            }
                            else
                            {
                                isMultiplemailbox = false;
                            }
                        }
                    }
                }

                #region ifPrimaryAffilate with Multiple mailboxes
                if (isprimary && isMultiplemailbox)
                {
                    // getting new primary mailbox based on first created date

                    System.DateTime newcreationDate = System.DateTime.MaxValue;

                    foreach (ClientProfileV1 clp in oldBACSIList.clientProfileV1)
                    {
                        var defaulttroles = from si in clp.clientServiceInstanceV1
                                            where (si.clientServiceInstanceIdentifier.value.Equals("BTIEMAIL:DEFAULT", StringComparison.OrdinalIgnoreCase))
                                            select si.clientServiceRole.ToList().Where(sri => sri.id.Equals("DEFAULT")).ToList();

                        foreach (List<ClientServiceRole> csr in defaulttroles)
                        {
                            foreach (ClientServiceRole csri in csr)
                            {
                                System.DateTime creationdate = System.DateTime.ParseExact(csri.createdDate, "ddMMyyyyHHmmss", CultureInfo.InvariantCulture);

                                if (!csri.clientIdentity[0].value.Equals(EmailName, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    if (creationdate < newcreationDate)
                                    {
                                        newcreationDate = creationdate;
                                        newprimarymail = csri.clientIdentity[0].value;
                                    }

                                }
                            }
                        }
                    }
                }
                #endregion
            }
            #endregion

            return isMultiplemailbox;
        }
        internal string GetSupplierFromidentity(ClientProfileV1 profile, string EmailName)
        {
            return profile.client != null && profile.client.clientIdentity != null && profile.client.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase))
                   && profile.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientIdentityValidation != null &&
                   profile.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientIdentityValidation.ToList().Exists(civ => civ.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase)) ?
                   profile.client.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().clientIdentityValidation.ToList().Where(civ => civ.name.Equals("EMAIL_SUPPLIER", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value : string.Empty;
        }

        internal SaaSNS.Order MapBBPremiumEmailProvisionRequest(ProductVasClass premiumProdVasClass)
        {
            BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("Mapping Premium Email Provision request");
            SaaSNS.Order response = new SaaSNS.Order();
            System.DateTime orderDate = new System.DateTime();
            string firstEmail = string.Empty, EmailName = string.Empty, oldBakId = string.Empty, bacID = string.Empty, emailDataStore = string.Empty, siForPAYG = string.Empty, siDomain = string.Empty;
            int productorderItemCount = 0;
            string emailSupplier = "MX";
            bool isAHTDone = false;
            bool isPaygCustomer = false;
            bool bacSIexists = true;
            bool CompletePremiumProvision = false;
            bool isprimaryaffiliate = false;
            string BtOneId = string.Empty;
            string rolestatus = string.Empty; string primaryEmailname = string.Empty;
            string newprimarymail = string.Empty;
            response.Header.CeaseReason = OrderRequest.SerializeObject();

            List<string> emailList = new List<string>();

            response.Header.OrderKey = OrderRequest.Order.OrderIdentifier.Value;
            response.Header.ServiceProviderID = Settings1.Default.ConsumerServiceProviderId;
            if (OrderRequest.Order.OrderItem[0].RequiredDateTime != null && OrderRequest.Order.OrderItem[0].RequiredDateTime.DateTime1 != null)
            {
                if (MSEOSaaSMapper.convertDateTime(OrderRequest.Order.OrderItem[0].RequiredDateTime.DateTime1, ref orderDate))
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

            try
            {
                switch (OrderRequest.Order.OrderItem[0].Action.Code.ToLower())
                {
                    case ("create"):
                    case ("add"):
                        response.Header.Action = BT.SaaS.Core.Shared.Entities.OrderActionEnum.provide;
                        break;
                    default:
                        throw new Exception("Action not supported");
                }

                response.ProductOrderItems = new System.Collections.Generic.List<BT.SaaS.Core.Shared.Entities.ProductOrderItem>(OrderRequest.Order.OrderItem.Length);

                foreach (MSEO.OrderItem orderItem in OrderRequest.Order.OrderItem)
                {
                    SaaSNS.ProductOrderItem productOrderItem = new SaaSNS.ProductOrderItem();
                    productOrderItem.Header.Status = SaaSNS.DataStatusEnum.notDone;
                    productOrderItem.Header.Quantity = "1";
                    productOrderItem.XmlIdKey = "PO-" + productorderItemCount.ToString();

                    productOrderItem.Header.OrderItemKey = orderItem.Identifier.Id;
                    if (ConfigurationManager.AppSettings["EmailScodes"].Split(',').Contains(orderItem.Specification[0].Identifier.Value1) || ConfigurationManager.AppSettings["PremiumEmailScodes"].Split(',').Contains(orderItem.Specification[0].Identifier.Value1)
                        || ConfigurationManager.AppSettings["BBPremiumEmailScode"].Split(',').Contains(orderItem.Specification[0].Identifier.Value1))
                    {
                        productOrderItem.Header.ProductCode = ConfigurationManager.AppSettings["DanteEMailProdCode"].ToString();
                    }
                    else
                    {
                        productOrderItem.Header.ProductCode = premiumProdVasClass.VasProductFamily;
                    }

                    System.Collections.Generic.List<string> inputProductDefinitionList = new System.Collections.Generic.List<string>();
                    inputProductDefinitionList.Add(productOrderItem.Header.ProductCode);

                    System.Collections.Generic.List<ProductDefinition> productDefinition = MdmWrapper.getSaaSProductDefs(inputProductDefinitionList, Settings1.Default.ConsumerServiceProviderId);

                    if (productDefinition.Count == 0)
                    {
                        orderItem.Status = Settings1.Default.IgnoredStatus;
                        Logger.Debug("Product Code not found in Order : " + OrderRequest.Order.OrderIdentifier.Value + " for order item " + orderItem.Identifier.Id);
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

                        if (productOrderItem.Header.ProductCode.Equals(ConfigurationManager.AppSettings["DanteEMailProdCode"], StringComparison.OrdinalIgnoreCase))
                        {
                            GetClientProfileV1Res getprofileResponse = null;
                            ClientServiceInstanceV1 emailService = null;
                            ClientServiceInstanceV1 getServices = null;
                            GetClientProfileV1Res oldBACProfile = null;
                            GetBatchProfileV1Res oldBACSIList = null;

                            if (OrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("emailname")) && !string.IsNullOrEmpty(OrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailname")).FirstOrDefault().Value))
                            {
                                EmailName = OrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("emailname")).FirstOrDefault().Value;
                            }
                            if (OrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToLower().Equals("billaccountnumber")) && !string.IsNullOrEmpty(OrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value))
                            {
                                bacID = OrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Where(ic => ic.Name.ToLower().Equals("billaccountnumber")).FirstOrDefault().Value;
                            }
                            getprofileResponse = DnpWrapper.GetClientProfileV1(EmailName, BTIEMAILID);


                            if (getprofileResponse != null && getprofileResponse.clientProfileV1 != null && getprofileResponse.clientProfileV1.clientServiceInstanceV1 != null)
                            {
                                emailDataStore = DanteRequestProcessor.GenerateEmailDataStore(getprofileResponse.clientProfileV1, "premiumemail", EmailName, ref emailSupplier);
                                if (emailDataStore == "D&P")
                                {
                                    emailService = (from si in getprofileResponse.clientProfileV1.clientServiceInstanceV1
                                                    where si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"].ToString(), StringComparison.OrdinalIgnoreCase)
                                                    && si.clientServiceRole != null
                                                    && si.clientServiceRole.ToList().Exists(csr => csr.id.Equals("Default", StringComparison.OrdinalIgnoreCase) && csr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase) && ci.value.Equals(EmailName, StringComparison.OrdinalIgnoreCase)))
                                                    select si).FirstOrDefault();

                                    // Check for PAYG Customer and set the SIFORPAYG attribute if the BAC is not present at Service Identity level.
                                    isPaygCustomer = DanteRequestProcessor.IsPAYGCustomer(getprofileResponse.clientProfileV1, EmailName);

                                    if (emailService.serviceIdentity.ToList().Exists(sid => sid.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        oldBakId = emailService.serviceIdentity.ToList().Where(sid => sid.domain.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        firstEmail = emailService.serviceIdentity.ToList().Where(sid => sid.domain.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        siForPAYG = oldBakId;
                                        siDomain = BACID_IDENTIFER_NAMEPACE;
                                    }
                                    // PAYG without BAC at Service Identity 
                                    // getting the BAC details from Admin role instead of client identities as there is a chance of affliate
                                    else if (isPaygCustomer && emailService.clientServiceRole.ToList().Exists(csr => csr.id.Equals("admin", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        bacSIexists = false;// BAC at service identity level
                                        var BakId = from si in emailService.clientServiceRole
                                                    where si.clientIdentity.ToList().Exists(csr => csr.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase))
                                                    select si.clientIdentity.ToList().Where(csr => csr.managedIdentifierDomain.value.Equals(BACID_IDENTIFER_NAMEPACE, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        oldBakId = BakId.ToList().FirstOrDefault();
                                        firstEmail = emailService.serviceIdentity.ToList().Where(sid => sid.domain.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        siForPAYG = firstEmail;
                                        siDomain = BTIEMAILID;
                                    }
                                    // PAYG without BAC at Service Identity and No Admin role
                                    // Get the primary email from Service Identity level
                                    else if (isPaygCustomer && emailService.serviceIdentity.ToList().Exists(sid => sid.domain.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        bacSIexists = false;
                                        firstEmail = emailService.serviceIdentity.ToList().Where(sid => sid.domain.Equals(BTIEMAILID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value;
                                        siForPAYG = firstEmail;
                                        siDomain = BTIEMAILID;
                                    }
                                }
                                else
                                {
                                    throw new DnpException("Ignored the request as EmailDataStore is D&P");
                                }
                            }
                            else
                            {
                                throw new DnpException("Profile doesn't have services in DNP");
                            }

                            List<ClientServiceInstanceV1> emailServiceInstanceList = new List<ClientServiceInstanceV1>();

                            oldBACSIList = DnpWrapper.GetServiceUserProfilesV1ForDante(siForPAYG, siDomain);

                            if (oldBACSIList != null && oldBACSIList.clientProfileV1 != null && oldBACSIList.clientProfileV1.Count() > 0)
                            {
                                foreach (ClientProfileV1 profile in oldBACSIList.clientProfileV1)
                                {

                                    if (profile.clientServiceInstanceV1 != null && profile.clientServiceInstanceV1.ToList().Exists(csi => csi.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"], StringComparison.OrdinalIgnoreCase)))
                                    {
                                        ClientServiceInstanceV1 emailServiceInstance = (from si in profile.clientServiceInstanceV1
                                                                                        where si.clientServiceInstanceIdentifier.value.Equals(ConfigurationManager.AppSettings["DanteIdentifier"].ToString(), StringComparison.OrdinalIgnoreCase) && si.serviceIdentity.ToList().Exists(sIden => sIden.domain.Equals(siDomain, StringComparison.OrdinalIgnoreCase) && sIden.value.Equals(siForPAYG, StringComparison.OrdinalIgnoreCase))
                                                                                        select si).FirstOrDefault();
                                        emailServiceInstanceList.Add(emailServiceInstance);
                                    }
                                }

                                emailList = GetListOfEmails(emailServiceInstanceList);
                            }
                            SaaSNS.Attribute isAffiliateCheck = new BT.SaaS.Core.Shared.Entities.Attribute();
                            isAffiliateCheck.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            isAffiliateCheck.Name = "IsBBPremiumEmailProvision";
                            isAffiliateCheck.Value = "true";
                            roleInstance.Attributes.Add(isAffiliateCheck);

                            //PrimaryEmail
                            SaaSNS.Attribute primaryEmailName = new BT.SaaS.Core.Shared.Entities.Attribute();
                            primaryEmailName.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            primaryEmailName.Name = "PRIMARYEMAIL";
                            primaryEmailName.Value = primaryEmailname;
                            roleInstance.Attributes.Add(primaryEmailName);

                            string SIBacNumber = oldBakId;
                            SaaSNS.Attribute siBacNumber = new BT.SaaS.Core.Shared.Entities.Attribute();
                            siBacNumber.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            siBacNumber.Name = "SIBACNUMBER";
                            siBacNumber.Value = SIBacNumber.ToString();
                            roleInstance.Attributes.Add(siBacNumber);

                            //isPaygCustomerk
                            SaaSNS.Attribute isPAYGCustomer = new BT.SaaS.Core.Shared.Entities.Attribute();
                            isPAYGCustomer.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            isPAYGCustomer.Name = "ISPAYGCUSTOMER";
                            isPAYGCustomer.Value = isPaygCustomer.ToString();
                            roleInstance.Attributes.Add(isPAYGCustomer);

                            //isBACSIExistsforPAYG
                            SaaSNS.Attribute isBACSIExists = new BT.SaaS.Core.Shared.Entities.Attribute();
                            isBACSIExists.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            isBACSIExists.Name = "ISBACSIExistsforPAYG";
                            isBACSIExists.Value = bacSIexists.ToString();
                            roleInstance.Attributes.Add(isBACSIExists);

                            //SIFORPAYG
                            SaaSNS.Attribute SIFORPAYG = new BT.SaaS.Core.Shared.Entities.Attribute();
                            SIFORPAYG.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            SIFORPAYG.Name = "SIFORPAYG";
                            SIFORPAYG.Value = siForPAYG;
                            roleInstance.Attributes.Add(SIFORPAYG);

                            if (emailList != null && emailList.Count > 0)
                            {
                                if (emailList[0] != null)
                                {
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "ListOfEmails", Value = emailList[0] });
                                }
                                if (emailList[1] != null)
                                {
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "InactiveEmailList", Value = emailList[1] });
                                }
                                if (emailList[2] != null)
                                {
                                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "Unresolvedemaillist", Value = emailList[2] });
                                }
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

                            SaaSNS.Attribute is_AHTClient = new BT.SaaS.Core.Shared.Entities.Attribute();
                            is_AHTClient.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            is_AHTClient.Name = "ISAHTCLIENT";
                            is_AHTClient.Value = isAHTDone.ToString();
                            roleInstance.Attributes.Add(is_AHTClient);

                            SaaSNS.Attribute VASProduct_ID = new BT.SaaS.Core.Shared.Entities.Attribute();
                            VASProduct_ID.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            VASProduct_ID.Name = "VASPRODUCTID";
                            VASProduct_ID.Value = "VPEM000030";
                            roleInstance.Attributes.Add(VASProduct_ID);

                            if (response.Header.Action == BT.SaaS.Core.Shared.Entities.OrderActionEnum.provide)
                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute { action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add, Name = "REASON", Value = "BBPremiumMailVASProvision" });

                            SaaSNS.Attribute BptmE2EDataAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                            BptmE2EDataAttribute.action = BT.SaaS.Core.Shared.Entities.DataActionEnum.add;
                            BptmE2EDataAttribute.Name = "E2EDATA";
                            if (OrderRequest.StandardHeader != null && OrderRequest.StandardHeader.E2e != null && OrderRequest.StandardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(OrderRequest.StandardHeader.E2e.E2EDATA.ToString()))
                                BptmE2EDataAttribute.Value = OrderRequest.StandardHeader.E2e.E2EDATA.ToString();
                            else
                                BptmE2EDataAttribute.Value = string.Empty;
                            roleInstance.Attributes.Add(BptmE2EDataAttribute);

                            serviceInstance.ServiceRoles.Add(serviceRole);
                            productOrderItem.ServiceInstances.Add(serviceInstance);
                            productOrderItem.RoleInstances.Add(roleInstance);
                            productorderItemCount++;

                            response.ProductOrderItems.Add(productOrderItem);
                        }
                    }
                }
            }
            catch (DnpException dnpException)
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter Mapping Premium Email Exception : " + dnpException.ToString());
                throw dnpException;
            }
            catch (MdmException mdmException)
            {
                throw mdmException;
            }
            catch (Exception ex)
            {
                BT.SaaS.Core.Shared.ErrorManagement.SaaSExceptionManager.OutputTrace("MSEOAdapter Mapping Premium Email Exception : " + ex.ToString());
                throw ex;
            }
            finally
            {
            }

            return response;
        }

        #region Gets the list of emails from default role identities
        public static List<string> GetListOfEmails(List<ClientServiceInstanceV1> emailServiceInstanceList)
        {
            List<string> ListOfEmails = new List<string>();
            List<string> pending_inactiveList = new List<string>();
            List<string> cease_inactiveList = new List<string>();
            List<string> PE_IN_EmailList = new List<string>();
            List<string> CE_IN_EmailList = new List<string>();
            List<string> unresolved__EmailList = new List<string>();
            List<string> Unresolved__EmailList = new List<string>();
            List<string> expired__EmailList = new List<string>();
            List<string> Expired__EmailList = new List<string>();

            try
            {
                for (int i = 0; i < emailServiceInstanceList.Count; i++)
                {
                    foreach (ClientServiceRole sr in emailServiceInstanceList[i].clientServiceRole.Where(sr => sr.id.ToLower() == "default"))
                    {
                        if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.ToLower() == "btiemailid" && (ci.clientIdentityStatus.value.ToLower() == "cease-inactive" || ci.clientIdentityStatus.value.ToLower() == "inactive" || ci.clientIdentityStatus.value.ToLower() == "disabled_maxloginfail_c")))
                        {
                            CE_IN_EmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.ToLower() == "btiemailid" && (ci.clientIdentityStatus.value.ToLower() == "cease-inactive" || ci.clientIdentityStatus.value.ToLower() == "inactive"|| ci.clientIdentityStatus.value.ToLower() == "disabled_maxloginfail_c")).FirstOrDefault().value);
                        }
                        if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.ToLower() == "btiemailid" && (ci.clientIdentityStatus.value.ToLower() == "pending-inactive" || ci.clientIdentityStatus.value.ToLower() == "active" || ci.clientIdentityStatus.value.ToLower() == "marked-inactive" || ci.clientIdentityStatus.value.ToLower() == "disabled_maxloginfail_a")))
                        {
                            PE_IN_EmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.ToLower() == "btiemailid" && (ci.clientIdentityStatus.value.ToLower() == "pending-inactive" || ci.clientIdentityStatus.value.ToLower() == "active" || ci.clientIdentityStatus.value.ToLower() == "marked-inactive" || ci.clientIdentityStatus.value.ToLower() == "disabled_maxloginfail_a")).FirstOrDefault().value);
                        }
                        if (sr.clientServiceRoleStatus.value.Equals("UNRESOLVED") && sr.clientIdentity != null && sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.Equals("btiemailid", StringComparison.OrdinalIgnoreCase) && (ci.clientIdentityStatus.value.ToLower() != "expired")))
                        {
                            unresolved__EmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.Equals("btiemailid", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().value);
                        }
                        if (sr.clientIdentity.ToList().Exists(ci => ci.managedIdentifierDomain.value.ToLower() == "btiemailid" && (ci.clientIdentityStatus.value.ToLower() == "pending-inactive" || ci.clientIdentityStatus.value.ToLower() == "active" || ci.clientIdentityStatus.value.ToLower() == "marked-inactive" || ci.clientIdentityStatus.value.ToLower() == "disabled_maxloginfail_a")))
                        {
                            expired__EmailList.Add(sr.clientIdentity.ToList().Where(ci => ci.managedIdentifierDomain.value.ToLower() == "btiemailid" && (ci.clientIdentityStatus.value.ToLower() == "pending-inactive" || ci.clientIdentityStatus.value.ToLower() == "active" || ci.clientIdentityStatus.value.ToLower() == "marked-inactive" || ci.clientIdentityStatus.value.ToLower() == "disabled_maxloginfail_a")).FirstOrDefault().value);
                        }
                    }
                    if (PE_IN_EmailList.Count > 0)
                    {
                        pending_inactiveList.Add(PE_IN_EmailList.First() + ":" + string.Join(",", PE_IN_EmailList.ToArray()));
                    }
                    if (CE_IN_EmailList.Count > 0)
                    {
                        cease_inactiveList.Add(CE_IN_EmailList.First() + ":" + string.Join(",", CE_IN_EmailList.ToArray()));
                    }
                    if (unresolved__EmailList.Count > 0)
                    {
                        Unresolved__EmailList.Add(unresolved__EmailList.First() + ":" + string.Join(",", unresolved__EmailList.ToArray()));
                    }
                    if (expired__EmailList.Count > 0)
                    {
                        Expired__EmailList.Add(expired__EmailList.First() + ":" + string.Join(",", expired__EmailList.ToArray()));
                    }

                    if (i == emailServiceInstanceList.Count - 1)
                    {
                        ListOfEmails.Add(string.Join(";", pending_inactiveList.ToArray()));
                        ListOfEmails.Add(string.Join(";", cease_inactiveList.ToArray()));
                        ListOfEmails.Add(string.Join(";", Unresolved__EmailList.ToArray()));
                        ListOfEmails.Add(string.Join(";", Expired__EmailList.ToArray()));
                    }
                    PE_IN_EmailList.Clear();
                    CE_IN_EmailList.Clear();
                    unresolved__EmailList.Clear();
                    expired__EmailList.Clear();
                }
            }
            finally
            {
                pending_inactiveList = null;
                cease_inactiveList = null;
                Unresolved__EmailList = null;
                Expired__EmailList = null;
            }
            return ListOfEmails;
        }
        #endregion
    }
}
