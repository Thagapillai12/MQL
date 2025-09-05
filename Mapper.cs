using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using BT.SaaS.Core.MDMAPI.Entities.Product;
using BT.SaaS.Core.Shared.Entities;
using BT.SaaS.IspssAdapter.BOS_API;
using BT.SaaS.IspssAdapter.CRM;
using BT.SaaS.IspssAdapter.Dnp;
using BT.SaaS.Core.MDMAPI.Entities.Customer;

namespace BT.SaaS.IspssAdapter
{

    public sealed class Mapper
    {
        private const string BOOLEAN_TRUE = "true";
        private const string BOOLEAN_FALSE = "false";
        private const string AUTOACTIVATION = "autoactivation";
        private const string BOS_ACTION_ADD = "Add";
        private const string AUTOACTIVATION_VALID_QUANTITY = "1";
        private const string WEBHOSTING_SCODE = "S0001301";
        static bool isCreateDomainAutoActivation = false;
        static bool isAutoActivation = false;
        static bool isExternalDomainForActivation = false;
        static bool isExternalDomainForLicenseTypeActivation = false;
        static string domainNameForAutoActivation = string.Empty;
        static int p1ProductsCount = 0;
       public static string primaryDomainfromBos = "";
        //Assigning this value to Make AutoActivation false for External Domain Scenario for MOSI Customer
        static bool isNewOrMigratedCustomer = false;

        private Mapper()
        { }

        public static BT.SaaS.Core.Shared.Entities.Order MapOrder(BT.SaaS.IspssAdapter.BOS_API.placeOrder bosOrder)
        {

            // Let's do some up front checks on the main BOS objects
            validateBosXML(bosOrder);

            // Order

            BT.SaaS.Core.Shared.Entities.Order saasOrder = new BT.SaaS.Core.Shared.Entities.Order();

            // if an existing Customer look up MDM to see what productInstances this customer already has
            List<string> existingProductInstanceKeys;
            if (bosOrder.isExistingCustomer())
            {
                Logger.Debug("Existing customer");
                existingProductInstanceKeys = MdmWrapper.getCustomerProductInstanceKeys(bosOrder.getCustomerKey());

                // the preferred email flag isn't set on existing customer orders from BOS
                // this call fixes it by looking in D&P to find alias identites
                fixPreferredEmailFlag(bosOrder);
            }
            else
            {
                Logger.Debug("New customer");
                existingProductInstanceKeys = new List<string>();
            }

            // built the Product Code mapping table for the Order Items SaaS can process
            List<OrderItemProductMap> orderItemProductMapping = new List<OrderItemProductMap>();
            buildProductCodeTable(bosOrder, orderItemProductMapping);

            // Order.OrderHeader
            mapOrderHeader(saasOrder, bosOrder, orderItemProductMapping, existingProductInstanceKeys);

            // No creditCardAuthorisations
            saasOrder.CreditCardAuthorisations = null;

            // Order.OrderItems

            saasOrder.ProductOrderItems = mapOrderItems(bosOrder, saasOrder, orderItemProductMapping, existingProductInstanceKeys);

            // If we have no orderItems we still want to go ahead in order to create any customer/ user data
            if (saasOrder.ProductOrderItems.Count == 0)
            {
                if (bosOrder.isExistingCustomer())
                {
                    // for an existing customer with no product instances we don't need to do anything
                    // as you can't add new users in an existing customer order from BOS
                    // setting the order action to not defined will be used by the placeOrderServuice layer to ignore
                    saasOrder.Header.Action = OrderActionEnum.notDefined;
                }
                else
                {
                    // This is a bodge for the PE. It seems to need an empty orderItem if we are just creating customer/ users
                    ProductOrderItem emptyOrderItem = new ProductOrderItem();
                    emptyOrderItem.XmlIdKey = "OI-0";
                    emptyOrderItem.Header.OrderItemKey = "dummyOrderItem";
                    emptyOrderItem.Header.Status = DataStatusEnum.done;

                    saasOrder.ProductOrderItems = new List<ProductOrderItem>();
                    saasOrder.ProductOrderItems.Add(emptyOrderItem);

                    // This is an SDP only order so we need to remove any preferred email address identities 
                    // preferred email address alias ids should only be done for SaaS orders
                    bool removeNonSaaSAliasIdentities = false;
                    string removeNonSaaSAliasIdentitiesString = ConfigurationManager.AppSettings["removeNonSaaSAliasIdentities"];
                    if (removeNonSaaSAliasIdentitiesString.ToUpper(CultureInfo.InvariantCulture).Equals("TRUE"))
                        removeNonSaaSAliasIdentities = true;
                    if (removeNonSaaSAliasIdentities)
                        removeAliasIdentities(saasOrder);
                }
            }

            //Added When Customer with ExternalDomain Provison
            //Adding an RoleInstance Attribute for sending KCI's
            saasOrder = MapEmailActivity(saasOrder);

            // Remove any users that are not needed for this order
            removeUnusedUsers(saasOrder);

            // No serviceActionOrderItems
            saasOrder.ServiceActionOrderItems = null;


            ProductOrderItem mailStorageOrderItem = CreateMailStorageProductOrderItem(bosOrder, saasOrder);

            if (mailStorageOrderItem != null)
            {
                saasOrder.ProductOrderItems.Add(mailStorageOrderItem);
            }

            if (saasOrder.Header.Action == OrderActionEnum.provide)
            {
                // Verify and free domain product order item to SaaS Order.
                List<ProductOrderItem> domainOrderItems = CreateDomainProductOrderItem(bosOrder, saasOrder, orderItemProductMapping, existingProductInstanceKeys);

                if (domainOrderItems != null)
                {
                    saasOrder.ProductOrderItems.AddRange(domainOrderItems);
                }
            }

            //resetting the static variables as they should not effect subsequent orders
            isCreateDomainAutoActivation = false;
            isAutoActivation = false;
            isExternalDomainForActivation = false;
            isExternalDomainForLicenseTypeActivation = false;
            domainNameForAutoActivation = string.Empty;
            p1ProductsCount = 0;
            isNewOrMigratedCustomer = false;
            return saasOrder;
        }

        private static void fixPreferredEmailFlag(placeOrder bosOrder)
        {
            // for existing customers the preferred email flag isn't set
            // loop through the contacts and check if we have alias identity in D&P
            // if so set the flag

            if (bosOrder.AccountAndOrder.ISPAccountData.ListOfContact == null)
                return;

            foreach (BT.SaaS.IspssAdapter.BOS_API.Contact contact in bosOrder.AccountAndOrder.ISPAccountData.ListOfContact)
            {
                if (contact.EMailContact != null)
                {
                    string btconnectid = contact.UserId;

                    // Look up D&P with this btconnectId
                    List<string> aliases = DnpWrapper.GetAliasIdentities(btconnectid);

                    // check is the preferred contact is set or not
                    bool preferredSet = false;
                    foreach (EMailContact emailContact in contact.EMailContact)
                    {
                        if (emailContact.PreferredContact != null &&
                            emailContact.PreferredContact.ToUpper().Equals("Y"))
                        {
                            preferredSet = true;
                        }
                    }

                    if (!preferredSet)
                    {
                        // find the first matching email contact with a D&P alias and mark it as preferred
                        foreach (EMailContact emailContact in contact.EMailContact)
                        {
                            if (aliases.Contains(emailContact.EMailAddress))
                            {
                                Logger.Debug("Contact for " + contact.UserId + " setting preferred flag on " + emailContact.EMailAddress);
                                emailContact.PreferredContact = "Y";
                                break;
                            }
                        }
                    }
                }
            }

        }

        private static void removeAliasIdentities(BT.SaaS.Core.Shared.Entities.Order saasOrder)
        {
            // Is this is an SDP only order we need to remove any preferred email address identities 
            // this should be done for SaaS only orders
            if (saasOrder.Header != null && saasOrder.Header.Users != null)
            {
                foreach (User user in saasOrder.Header.Users)
                {
                    if (user.IdentityCredential != null)
                    {
                        List<BT.SaaS.Core.Shared.Entities.IdentityCredential> idsToRemove = new List<BT.SaaS.Core.Shared.Entities.IdentityCredential>();
                        foreach (BT.SaaS.Core.Shared.Entities.IdentityCredential id in user.IdentityCredential)
                        {
                            if (id.Identity.IdentityAlias.Equals(IdentifierAliasEnum.yes))
                            {
                                idsToRemove.Add(id);
                            }
                        }
                        foreach (BT.SaaS.Core.Shared.Entities.IdentityCredential idToRemove in idsToRemove)
                        {
                            Logger.Debug("SDP only order - removing alias identity created from preferred email address " + idToRemove.Identity.identifier);
                            user.IdentityCredential.Remove(idToRemove);
                        }
                    }
                }
            }
        }

        static private void removeUnusedUsers(BT.SaaS.Core.Shared.Entities.Order saasOrder)
        {
            List<User> usersReferencedByOrderItems = new List<User>();

            foreach (ProductOrderItem orderItem in saasOrder.ProductOrderItems)
            {
                foreach (BT.SaaS.Core.Shared.Entities.RoleInstance roleInstance in orderItem.RoleInstances)
                {
                    // Check added as there are occassions where RoleInstances are coming as null for existing users
                    if (roleInstance != null)
                    {
                        User user = findSaaSUser(saasOrder, roleInstance.UserXmlRef);
                        if (!usersReferencedByOrderItems.Contains(user))
                        {
                            usersReferencedByOrderItems.Add(user);
                        }
                    }
                }
            }

            // Now lets create the list of users to retain based on
            // if they are new users or if they are referenced by orderItems
            List<User> usersToRetain = new List<User>();

            foreach (User user in saasOrder.Header.Users)
            {
                if (!user.Action.Equals(ClientActionEnum.none) || usersReferencedByOrderItems.Contains(user))
                {
                    Logger.Debug("Retaining user " + user.XmlIdKey + " user action=" + user.Action +
                                 " referenced by orderItem=" + usersReferencedByOrderItems.Contains(user));
                    usersToRetain.Add(user);
                }
            }

            Logger.Debug("Started with " + saasOrder.Header.Users.Count + " users, removing " +
                (saasOrder.Header.Users.Count - usersToRetain.Count) + " of them as not used");

            saasOrder.Header.Users = usersToRetain;
        }

        static private List<ProductOrderItem> mapOrderItems(placeOrder bosOrder, BT.SaaS.Core.Shared.Entities.Order saasOrder, List<OrderItemProductMap> orderItemProductMapping, List<string> existingProductInstanceKeys)
        {
            List<ProductOrderItem> orderItems = new List<ProductOrderItem>();
            string errorContext;

            int orderItemNumber = 0;
            StringBuilder alertText = new StringBuilder();
            foreach (OrderItemProductMap orderItemProductMap in orderItemProductMapping)
            {
                BT.SaaS.IspssAdapter.BOS_API.OrderItem bosOrderItem = orderItemProductMap.BosOrderItem;
                Boolean existingProductInstance = isExistingProductInstance(existingProductInstanceKeys, bosOrderItem);

                alertText.Append("Mapping BOS OrderItem with productInstanceId: " + bosOrderItem.ProductInstanceId +
                                     ", productCode: " + orderItemProductMap.SaasProductCode + "\n");

                errorContext = " OrderItem productInstanceId: " + bosOrderItem.ProductInstanceId +
                             ", productCode: " + orderItemProductMap.SaasProductCode + " ";

                ProductOrderItem saasOrderItem = new ProductOrderItem();

                saasOrderItem.XmlIdKey = "OI-" + orderItemNumber++;

                // OrderItem.OrderItemHeader

                ProductOrderItemHeader orderItemHeader = saasOrderItem.Header;
                orderItemHeader.Status = DataStatusEnum.notDone;
                orderItemHeader.OrderItemKey = value(bosOrderItem.OrderItemId);
                orderItemHeader.OrderItemId = null;
                orderItemHeader.ProductCode = value(orderItemProductMap.SaasProductCode);
                orderItemHeader.ProductName = value(orderItemProductMap.ProductDefinition.Name);
                orderItemHeader.ProductInstanceKey = value(bosOrderItem.ProductInstanceId);
                orderItemHeader.ProductInstanceId = null;
                orderItemHeader.ParentProductInstanceKey = (orderItemProductMap.BosParentOrderItem != null) ?
                    value(orderItemProductMap.BosParentOrderItem.ProductInstanceId) : null;
                orderItemHeader.ParentProductOrderItemXmlRef = findParentOrderItemXmlKey(orderItems, orderItemHeader.ParentProductInstanceKey);
                // There is only ever one billingAccount in a BOS Order so link to it
                orderItemHeader.BillingAccountXmlRef = "BA-0";
                if (isExistingProductInstance(existingProductInstanceKeys, bosOrderItem))
                {
                    orderItemHeader.Quantity = value(bosOrderItem.Quantity);
                }
                else
                {
                    orderItemHeader.Quantity = value(bosOrderItem.Quantity);
                }

                orderItemHeader.HoldToTerm = getHoldToTerm(bosOrderItem);
                orderItemHeader.DeliveryContactXmlRef = null;


                //checking if the order is for autoactivation
                string webProds = getWebHostingAndExternalDomainProducts(bosOrder);

                // OrderItem.Roles

                saasOrderItem.RoleInstances = mapRoles(orderItemProductMap, orderItemNumber - 1, bosOrder, saasOrder, existingProductInstance, orderItems, errorContext);

                if (CheckIsBulkRegradeOrder(orderItemProductMap.BosOrderItem))
                {
                    if (saasOrderItem.RoleInstances != null && saasOrderItem.RoleInstances.Count > 0)
                    {
                        saasOrderItem.RoleInstances[0].Attributes = new List<BT.SaaS.Core.Shared.Entities.Attribute>();
                        saasOrderItem.RoleInstances[0].Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute() { action = DataActionEnum.add, Name = "sendKCI", Value="false" });
                    }
                }

                saasOrderItem.SequencedServiceActions = null;
                saasOrderItem.ServiceInstances = null;

                orderItems.Add(saasOrderItem);

            }

            if (alertText.Length > 0)
            {
                alertText.Append("Mapped " + orderItems.Count.ToString(CultureInfo.InvariantCulture) + " out of " + orderItemProductMapping.Count.ToString(CultureInfo.InvariantCulture) + " order items");
                Logger.Debug(alertText.ToString());
            }
            return orderItems;
        }

        static private string findParentOrderItemXmlKey(List<ProductOrderItem> saasOrderItems, string parentProductInstanceKey)
        {
            foreach (ProductOrderItem orderItem in saasOrderItems)
            {
                if (orderItem.Header != null && orderItem.Header.ProductInstanceKey != null)
                {
                    if (orderItem.Header.ProductInstanceKey.Equals(parentProductInstanceKey))
                        return (orderItem.XmlIdKey);
                }
            }
            return null;
        }

        static private int getLicenceQuantity(BT.SaaS.IspssAdapter.BOS_API.OrderItem oi)
        {
            int result = 0;
            if (oi.ListOfOrderItemAttribute != null)
            {
                foreach (BT.SaaS.IspssAdapter.BOS_API.ItemAttribute attr in oi.ListOfOrderItemAttribute)
                {
                    // make sure we ignore attributes with action None!
                    if (attr.Name != null && attr.Name.ToUpper(CultureInfo.InvariantCulture).Equals("LICENCES") &&
                        !attr.Action.Equals(BT.SaaS.IspssAdapter.BOS_API.Action.None))
                    {
                        if (!int.TryParse(attr.NewValue, out result))
                        {
                            throw new MappingException("Could not get an integer value from Licences attribute");
                        }
                    }
                }
            }
            return result;
        }
        static private Boolean getOrderType(BT.SaaS.IspssAdapter.BOS_API.OrderItem oi)
        {
            if (oi.ListOfOrderItemAttribute != null)
            {
                foreach (BT.SaaS.IspssAdapter.BOS_API.ItemAttribute attr in oi.ListOfOrderItemAttribute)
                {
                    if (attr.Name != null && attr.Name.ToUpper(CultureInfo.InvariantCulture).Equals("ORDERTYPE"))
                    {
                        if (attr.NewValue != null && attr.NewValue.ToUpper(CultureInfo.InvariantCulture).Equals("RESIGN") && attr.Action.ToString().Equals("add", StringComparison.OrdinalIgnoreCase))
                            return (true);
                    }
                }
            }
            return false;
        }

        static private Boolean CheckIsBulkRegradeOrder(BT.SaaS.IspssAdapter.BOS_API.OrderItem oi)
        {
            if (oi.ListOfOrderItemAttribute != null)
            {
                foreach (BT.SaaS.IspssAdapter.BOS_API.ItemAttribute attr in oi.ListOfOrderItemAttribute)
                {
                    if (attr.Name != null && attr.Name.ToUpper(CultureInfo.InvariantCulture).Equals("ORDERTYPE"))
                    {
                        if (attr.NewValue != null && attr.NewValue.ToUpper(CultureInfo.InvariantCulture).Equals("BULK VAS REGRADE HE TO MS") && attr.Action.ToString().Equals("add", StringComparison.OrdinalIgnoreCase))
                            return (true);
                    }
                }
            }
            return false;
        }

        static private Boolean getHoldToTerm(BT.SaaS.IspssAdapter.BOS_API.OrderItem oi)
        {
            if (oi.ListOfOrderItemAttribute != null)
            {
                foreach (BT.SaaS.IspssAdapter.BOS_API.ItemAttribute attr in oi.ListOfOrderItemAttribute)
                {
                    if (attr.Name != null && attr.Name.ToUpper(CultureInfo.InvariantCulture).Equals("HOLDTOTERM"))
                    {
                        if (attr.NewValue != null && attr.NewValue.ToUpper(CultureInfo.InvariantCulture).Equals("Y"))
                            return (true);
                    }
                }
            }
            return false;
        }

        static private List<BT.SaaS.Core.Shared.Entities.RoleInstance> mapRoles(OrderItemProductMap item, int orderItemIndex, placeOrder bosOrder, BT.SaaS.Core.Shared.Entities.Order saasOrder, Boolean existingProductInstance, List<ProductOrderItem> orderItems, string errorContext)
        {
            List<BT.SaaS.Core.Shared.Entities.RoleInstance> roles = null;

            ProductDefinition prodDef;

            // If it's a regrade make sure to use the production defintion we are regrading from not to
            if (item.BosOrderItem.Action == BT.SaaS.IspssAdapter.BOS_API.Action.Modify)
            {
                prodDef = item.OldProductDefinition;
            }
            else
            {
                prodDef = item.ProductDefinition;
            }

            if (prodDef.ProductDefinitionRoleList != null)
            {
                int roleInstanceCount = 0;
                roles = new List<BT.SaaS.Core.Shared.Entities.RoleInstance>();
                BT.SaaS.IspssAdapter.BOS_API.Contact bosPrimaryUser = getPrimaryBosUser(bosOrder);

                #region Manage Role Mapping
                ProductDefinitionRole manageRoleDefinition =
                    prodDef.ProductDefinitionRoleList.FirstOrDefault
                        (p => p.ProductRoleName.Equals(ConfigurationManager.AppSettings["manageUserRoleType"],
                         StringComparison.InvariantCultureIgnoreCase));

                if (manageRoleDefinition == null)
                {
                    //throw new MappingException("No manage role in this product definition. " + errorContext);
                }
                else
                {
                    BT.SaaS.Core.Shared.Entities.RoleInstance manageRoleInstance = mapRoleInstance(saasOrder, bosOrder, item, orderItemIndex, roleInstanceCount++, manageRoleDefinition, bosPrimaryUser, existingProductInstance, orderItems, errorContext);
                    roles.Add(manageRoleInstance);
                }
                #endregion

                #region Admin Role Mapping
                bool isAdminRoleIgnored = false;

                if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["ProductListToIgnoreAdminRole"]))
                {
                    List<string> ProductListToIgnoreAdminRole = ConfigurationManager.AppSettings["ProductListToIgnoreAdminRole"].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

                    isAdminRoleIgnored = ProductListToIgnoreAdminRole.Contains(item.SaasProductCode);
                }

                if (!isAdminRoleIgnored)
                {
                    // Get the Primary User Role definition
                    string primaryUserRoleType = ConfigurationManager.AppSettings["primaryUserRoleType"];

                    ProductDefinitionRole primaryUserRoleDefinition =
                        prodDef.ProductDefinitionRoleList.FirstOrDefault
                            (p => p.ProductRoleName.Equals(primaryUserRoleType,
                             StringComparison.InvariantCultureIgnoreCase));

                    if (primaryUserRoleDefinition == null)
                    {
                        throw new MappingException("No primary role in this product definition. " + errorContext);
                    }

                    // This modifyService Check is required as admin service role should not be created for modifyService.
                    if (saasOrder.Header.Action != OrderActionEnum.modifyService)
                    {

                        BT.SaaS.Core.Shared.Entities.RoleInstance primaryRoleInstance = mapRoleInstance(saasOrder, bosOrder, item, orderItemIndex, roleInstanceCount++, primaryUserRoleDefinition, bosPrimaryUser, existingProductInstance, orderItems, errorContext);
                        roles.Add(primaryRoleInstance);
                    }
                }
                #endregion

                #region Secondary Users - Role Mapping

                //to skip secondary user creation incase of external domains
                if (!(isExternalDomainForActivation || isAutoActivation))
                {
                    // Do we have secondary users?
                    List<BT.SaaS.IspssAdapter.BOS_API.Contact> bosSecondaryUsers = getSecondaryBosUsers(bosOrder, item.BosOrderItem);

                    if (bosSecondaryUsers.Count > 0)
                    {
                        string secondaryUserRoleType = ConfigurationManager.AppSettings["secondaryUserRoleType"];

                        ProductDefinitionRole secondaryUserRoleDefinition =
                            prodDef.ProductDefinitionRoleList.FirstOrDefault
                                (p => p.ProductRoleName.Equals(secondaryUserRoleType,
                                 StringComparison.InvariantCultureIgnoreCase));

                        if (secondaryUserRoleDefinition != null)
                        {
                            foreach (BT.SaaS.IspssAdapter.BOS_API.Contact bosUser in bosSecondaryUsers)
                            {
                                BT.SaaS.Core.Shared.Entities.RoleInstance secondaryRoleInstance = mapRoleInstance(saasOrder, bosOrder, item, orderItemIndex, roleInstanceCount++, secondaryUserRoleDefinition, bosUser, existingProductInstance, orderItems, errorContext);
                                if (secondaryRoleInstance != null)
                                    roles.Add(secondaryRoleInstance);
                            }
                        }
                        else
                        {
                            //we take this as secondary users will not be provided rather than error - Nitin
                            //PC Security also contains the user attribute which was causing the problem
                            // throw new MappingException("OrderItem has secondary users but no secondaryUserRoleDefinition can be found in MDM. " + errorContext);
                        }
                    }
                }
                #endregion
            }
            return roles;
        }

        static private User findSaaSUser(BT.SaaS.Core.Shared.Entities.Order saasOrder, string userXmlIdRef)
        {
            // find the matching user

            User retUser =
                (from user in saasOrder.Header.Users
                 where user.XmlIdKey.Equals(userXmlIdRef)
                 select user).First();

            return retUser;
        }

        static private User findSaaSUser(BT.SaaS.Core.Shared.Entities.Order saasOrder, BT.SaaS.IspssAdapter.BOS_API.Contact bosContact)
        {
            User user = null;

            // First find the SaaS contact XmlIdKey

            BT.SaaS.Core.Shared.Entities.Contact contact =
                (from saasContact in saasOrder.Header.Customer.Contacts
                 where saasContact.ContactKey.Equals(userFromEmailAddress(bosContact.UserId))
                 select saasContact).First();

            // Using the XmlIdKey find the User that is linked to this contact

            if (contact != null)
            {
                user =
                    (from saasUser in saasOrder.Header.Users
                     where saasUser.ContactXmlRef.Equals(contact.XmlIdKey)
                     select saasUser).First();
            }
            return user;
        }

        private static BT.SaaS.Core.Shared.Entities.RoleInstance mapRoleInstance(BT.SaaS.Core.Shared.Entities.Order saasOrder, placeOrder bosOrder, OrderItemProductMap item, int orderItemIndex, int roleInstanceCount, ProductDefinitionRole roleDef, BT.SaaS.IspssAdapter.BOS_API.Contact bosUser, Boolean existingProductInstance, List<ProductOrderItem> orderItems, string errorContext)
        {

            BT.SaaS.Core.Shared.Entities.RoleInstance roleInstance = new BT.SaaS.Core.Shared.Entities.RoleInstance()
            {
                XmlIdKey = "RI-" + orderItemIndex + "-" + roleInstanceCount,
                Status = DataStatusEnum.notDone,
                RoleType = roleDef.ProductRoleName,
                Attributes = null,
                UserIdentityCredentialXmlRef = null,
                LicenceKey = null,
                LicenceExpiry = new DateTime(),
                ReceivingProductInstanceId = null
            };

            Logger.Debug("888888888888888888888888888888888888: before action switch ");
            // Work out the action
            switch (item.BosOrderItem.Action)
            {
                case BT.SaaS.IspssAdapter.BOS_API.Action.Add:
                    roleInstance.Action = RoleInstanceActionEnum.add;
                    break;
                case BT.SaaS.IspssAdapter.BOS_API.Action.Delete:
                    roleInstance.Action = RoleInstanceActionEnum.delete;
                    break;
                case BT.SaaS.IspssAdapter.BOS_API.Action.Modify:
                    //ItemAttribute tariffId = null;
                    //if(item.BosOrderItem.ListOfOrderItemAttribute != null && item.BosOrderItem.ListOfOrderItemAttribute.Count() > 0)
                    //{
                    //    tariffId = item.BosOrderItem.ListOfOrderItemAttribute.Where(itemAttr => itemAttr.Name.ToUpper().Equals("TARIFFID")).FirstOrDefault();
                    //}
                    //if (tariffId != null && !String.IsNullOrEmpty(tariffId.OldValue) && (tariffId.OldValue == "TA0000000004103" || tariffId.OldValue == "TA0000000004113"))
                    //{
                    //    roleInstance.Action = RoleInstanceActionEnum.add;
                    //}
                    //else
                    //{
                    Logger.Debug("888888888888888888888888888888888888: Came into Modify role Instance");
                    roleInstance.Action = RoleInstanceActionEnum.change;
                    //}
                    break;
                default:
                    throw new MappingException("Don't know how to map BOS orderItem " +
                                               " with action: " + item.BosOrderItem.Action +
                                               " into a roleInstance action. " + errorContext);
            }

            Logger.Debug("888888888888888888888888888888888888: after action switch ");

            Logger.Debug("888888888888888888888888888888888888: " + item.BosOrderItem.Action.ToString());

            // Work out the OSSAction by looking up the MDM product definition
            // searching based on order action and role action
            IEnumerable<RoleActionOSSMapping> roleActionOSSMappingResult =
                (from roleActionOSSMapping in roleDef.RoleActionOSSMappingList
                 where ((roleActionOSSMapping.OrderAction == saasOrder.Header.Action) &&
                        (roleActionOSSMapping.RoleAction == roleInstance.Action))
                 select roleActionOSSMapping);

            if (roleActionOSSMappingResult.Count() == 0)
            {
                string removedProductList;
                if (ConfigurationManager.AppSettings["RemovedProductList"] != null)
                {
                    removedProductList = ConfigurationManager.AppSettings["RemovedProductList"].ToString();

                    if (removedProductList.Length > 0)
                    {
                        if (removedProductList.Contains(item.SaasProductCode) && item.BosOrderItem.Action == BT.SaaS.IspssAdapter.BOS_API.Action.Add)
                        {
                            throw new MappingException("This order type is not supported for this product. No RoleActionOSSMapping for role " + roleInstance.RoleType +
                                                       " saasOrder.Header.Action=" + saasOrder.Header.Action.ToString() +
                                                       " roleInstance.Action=" + roleInstance.Action.ToString() + " " +
                                                       errorContext);
                        }

                    }
                }
                ///for ex - For PC security there is no "default" role for provide action but there is default role for activate action                
                return null; //we will ignore the role instance if no data load found in mdm. data loads are expected to be correct - Nitin

                /*
                throw new MappingException("This order type is not supported for this product. No RoleActionOSSMapping for role " + roleInstance.RoleType +
                                           " saasOrder.Header.Action=" + saasOrder.Header.Action.ToString() +
                                           " roleInstance.Action=" + roleInstance.Action.ToString() + " " +
                                           errorContext);
                 */
            }
            if (roleActionOSSMappingResult.Count() > 1)
            {
                throw new MappingException("Internal error. More than one RoleActionOSSMapping found for role " + roleInstance.RoleType +
                                          " saasOrder.Header.Action=" + saasOrder.Header.Action.ToString() +
                                           " roleInstance.Action=" + roleInstance.Action.ToString() + " " +
                                           errorContext);
            }

            // set the OSSAction
            roleInstance.InternalProvisioningAction = roleActionOSSMappingResult.First().OSSAction;

            /*
             * NEED TO REVISIT THIS ISECP SEEMS TO WORK A LITTLE DIFFERENTLY TO EVERYTHING ELSE
             * 
            // If we have a licence quantity >0 this means this is an orderItem increasing the quantity of an existing ProductInstance
             * 
            int licenceQuantity = getLicenceQuantity(item.BosOrderItem);
            if (licenceQuantity > 0)
            {
                Logger.Debug("OrderItem with productInstanceId: " + item.BosOrderItem.ProductInstanceId + 
                        " has Licence attribute\n");
                roleInstance.QuantityToAddRemove = licenceQuantity;
            }
            */
            int quantity;
            if (int.TryParse(item.BosOrderItem.Quantity, out quantity))
            {
                roleInstance.QuantityToAddRemove = quantity;
            }
            else
            {
                throw new MappingException("Quantity doesn't contain a valid integer: " + item.BosOrderItem.Quantity +
                                           errorContext);
            }

            List<BT.SaaS.Core.Shared.Entities.Attribute> roleAttrs = new List<BT.SaaS.Core.Shared.Entities.Attribute>();

            foreach (ProductDefinitionAttribute attr in roleDef.ProductDefinitionAttributeList)
            {
                bool isUserInputAllowed = attr.IsUserInputAllowed;

                if (errorContext.StartsWith("DummyDomain productCode:") && (attr.Name.ToUpper().Equals("LICENSETYPE") || attr.Name.ToUpper().Equals("TYPE") || attr.Name.ToUpper().Equals("HELICENSETYPE")))
                {
                    isUserInputAllowed = false;
                }

                if (attr.OSSAction.Equals(roleInstance.InternalProvisioningAction) && isUserInputAllowed)
                {
                    string value = getValueFromAttribute(item.SaasProductCode, attr.Name, bosOrder, orderItems, item.BosOrderItem, bosUser, saasOrder.Header.Customer.CompanyName, attr.IsOptional);

                    // If we can't map a mandatory field throw an exception
                    if (value == null && !attr.IsOptional)
                    {
                        throw new MappingException("Failed to map user mandatory attribute: " + attr.Name + " from BOS XML");
                    }

                    //Set the value of ISADMIN
                    if (attr.Name.ToUpper() == "ISADMIN")
                    {
                        value = (value.ToUpper() == "P") ? BOOLEAN_TRUE : BOOLEAN_FALSE;
                    }

                    if (attr.Name.ToUpper() == "SECONDARYUSER")
                    {
                        string contactRef = value;
                        BT.SaaS.IspssAdapter.BOS_API.Contact secondaryContact = findBosContact(bosOrder, contactRef);

                        if (secondaryContact != null && secondaryContact.UserType.ToUpper() == "S")
                        {
                            value = secondaryContact.UserId;
                        }
                        else
                        {
                            value = String.Empty;
                        }
                    }

                    if (attr.Name.ToUpper() == "ENABLELCS")
                    {
                        if (item.BosOrderItem.ProductCode.Equals("S0001451") && item.BosOrderItem.Action == BT.SaaS.IspssAdapter.BOS_API.Action.Delete)
                        {
                            List<BT.SaaS.IspssAdapter.BOS_API.OrderItem> premiumOrders = bosOrder.AccountAndOrder.Order.ListOfOrderItem[0].OrderItem1.Where(oitem => oitem.ProductCode.Equals("S0001451")).ToList<BT.SaaS.IspssAdapter.BOS_API.OrderItem>();

                            if (premiumOrders.Count == 1)
                            {
                                value = "False";
                            }

                            if (premiumOrders.Count >= 2)
                            {
                                int ceaseOrderCount = 0;
                                foreach (BT.SaaS.IspssAdapter.BOS_API.OrderItem premiumItem in premiumOrders)
                                {
                                    if (premiumItem.Action == BT.SaaS.IspssAdapter.BOS_API.Action.Delete)
                                    {
                                        ceaseOrderCount += 1;
                                    }
                                }

                                if (ceaseOrderCount < premiumOrders.Count)
                                {
                                    value = "True";
                                }

                                if (ceaseOrderCount == premiumOrders.Count)
                                {
                                    value = "False";
                                }
                            }
                        }
                    }


                    //CODE SNIPPET FOR AUTOACTIVATION OF SMART HOSTING(EXTERNAL DOMAINS)
                    // First look up the S-code for Web hosting products in the config file
                    string webHostingSCode = ConfigurationManager.AppSettings["webHostingSCode"];


                    // look up the S-code for domain
                    string externalDomainSCode = ConfigurationManager.AppSettings["domainProductSCode"];
                    string licenseTypeForDomain = string.Empty;

                    if (webHostingSCode == null)
                    {
                        throw new MappingException("No definition for webHostingSCode in Web.config");
                    }

                    if (externalDomainSCode == null)
                    {
                        throw new MappingException("No definition for externalDomainSCode in Web.config");
                    }


                    //We need to allow autoactivation only incase of atleast one webhosting product and external domain               
                    if (attr.Name.ToUpper() == "DOMAINTYPE" && isAutoActivation && item.BosOrderItem.ProductCode.Equals(webHostingSCode))
                    {
                        value = AUTOACTIVATION;

                    }
                    //Domain name should not be read from Xpath in config incase of other scenarios than autoactivation    
                    if (attr.Name.ToUpper() == "DOMAINNAME" && isAutoActivation && item.BosOrderItem.ProductCode.Equals(webHostingSCode))
                    {
                        //As the Product code during create domain corresponds to web hosting product;we need to clear it
                        if (!isCreateDomainAutoActivation)
                            value = domainNameForAutoActivation;
                        else
                        {
                            value = "";
                            isCreateDomainAutoActivation = false;
                        }

                    }

                    //Domain name should not be sent in the IBP domain orderItem as it will activate the associated entitlement
                    if (attr.Name.ToUpper() == "DOMAINNAME" && isAutoActivation && item.BosOrderItem.ProductCode.Equals(externalDomainSCode))
                    {
                        if (isExternalDomainForActivation)
                        {
                            value = domainNameForAutoActivation;
                            //resetting the flag as it should not effect other domain products
                            isExternalDomainForActivation = false;
                        }
                        else
                            value = "";

                        if (attr.Name.ToUpper() == "DOMAININFOPASSWORD")
                        {
                             if(!string.IsNullOrEmpty(value))
                             value = BlowfishEncryptor.Encrypt(value);
                        }

                    }
                   
                    //LicenseType should not be set as Autoactivation for the IBP domain orderItem 
                    if (attr.Name.ToUpper() == "LICENSETYPE" && isAutoActivation && item.BosOrderItem.ProductCode.Equals(externalDomainSCode))
                    {
                        if (isExternalDomainForLicenseTypeActivation)
                        {
                            value = AUTOACTIVATION;
                            //resetting the flag as it should not effect other domain products
                            isExternalDomainForLicenseTypeActivation = false;
                        }
                        else
                            value = "";


                    }

                    //Passing the blank value, in DNP default values for the existing users will be " " after migration from HE to MOSI
                    if ((attr.Name.ToUpper() == "SKUTYPE" || attr.Name.ToUpper() == "DNS_VERIFICATION_CHECK_STATUS") && item.BosOrderItem.ProductCode.Contains(ConfigurationManager.AppSettings["domainProductSCode"]))
                    {
                        if (string.IsNullOrEmpty(value))
                        {
                            value = " ";
                        }
                        else
                        {
                            value = value.ToUpper();
                        }
                    }

                    // The Provisioning Engine can't cope with attributes being left out
                    // If there is no value a blank optional attribute should be passed down
                    roleAttrs.Add(new BT.SaaS.Core.Shared.Entities.Attribute()
                    {
                        action = DataActionEnum.add,
                        Name = attr.Name,
                        Value = value
                    });
                }

            }

            if (roleAttrs.Count > 0)
            {
                roleInstance.Attributes = roleAttrs;
            }
            else
            {
                roleInstance.Attributes = null;
            }

            User saasUser = findSaaSUser(saasOrder, bosUser);
            roleInstance.UserXmlRef = saasUser.XmlIdKey;

            // Which identity should we map the roleInstance to? If there is a preferred email use it
            if (saasUser.IdentityCredential.Count > 1)
            {
                // If there are multiple identities need to check the 2nd one is a preferred email 
                // and not a mailboxAlias  -- this is Not Valid now (Nitin)
                string preferredEmail = getPreferredEmailAddress(bosUser);

                //Changed the logic - Always use btconnect id - if 0 is the preferred email then use 1
                if (saasUser.IdentityCredential[0].Identity.identifier.Equals(preferredEmail))
                {
                    roleInstance.UserIdentityCredentialXmlRef = saasUser.IdentityCredential[1].XmlIdKey;
                }
                else
                    roleInstance.UserIdentityCredentialXmlRef = saasUser.IdentityCredential[0].XmlIdKey;
            }
            else
            {
                roleInstance.UserIdentityCredentialXmlRef = saasUser.IdentityCredential[0].XmlIdKey;
            }

            return roleInstance;
        }

        private static string getValueFromAttribute(string productCode, string attributeName, placeOrder bosOrder, List<ProductOrderItem> orderItems, BT.SaaS.IspssAdapter.BOS_API.OrderItem bosOrderItem, BT.SaaS.IspssAdapter.BOS_API.Contact bosUser, string companyName, bool isAttribOptional)
        {
            // Look up the attribute name in the config to find where to get it
            ConfigurationBosAttributeMapSection bosAttributeMapSection = (ConfigurationBosAttributeMapSection)ConfigurationManager.GetSection("bosAttributeMap");
            if (bosAttributeMapSection != null)
            {
                ConfigurationBosAttributeMap bosAttributeMap = null;

                // get the bosAttributeMap from config searching by productCode first section
                ConfigurationBosProductAttributeMap bosProductAttributeMap = bosAttributeMapSection.BosAttributeMap[productCode];
                if (bosProductAttributeMap != null)
                {
                    bosAttributeMap = bosProductAttributeMap.AttributeMap[attributeName];
                }
                if (bosAttributeMap == null)
                {
                    bosProductAttributeMap = bosAttributeMapSection.BosAttributeMap["all"];
                    if (bosProductAttributeMap != null)
                    {
                        bosAttributeMap = bosProductAttributeMap.AttributeMap[attributeName];
                    }
                }

                if (bosAttributeMap == null)
                {
                    if (isAttribOptional)
                    {
                        return String.Empty;
                    }
                    else if ((attributeName.ToUpper() == "DNS_VERIFICATION_CHECK_STATUS") || (attributeName.ToUpper() == "SKUTYPE" && productCode.StartsWith(ConfigurationManager.AppSettings["webHostingSCode"])))
                    {
                        return string.Empty;
                    }
                    else
                    {
                        throw new MappingException("Could not find a mapping for attribute " + attributeName);
                    }
                }

                if (!bosAttributeMap.AttributeMappingType.Equals(AttributeMappingTypeValue.SugarInstanceName) &&
                    !bosAttributeMap.AttributeMappingType.Equals(AttributeMappingTypeValue.WebHostingProductNames) &&
                    !bosAttributeMap.AttributeMappingType.Equals(AttributeMappingTypeValue.WebConsultAndBuildOrderItemCount) &&
                    !bosAttributeMap.AttributeMappingType.Equals(AttributeMappingTypeValue.WebConsultAndBuildSalesPrice) &&
                    !bosAttributeMap.AttributeMappingType.Equals(AttributeMappingTypeValue.BuildingNumber) &&
                    !bosAttributeMap.AttributeMappingType.Equals(AttributeMappingTypeValue.PostCodeName) &&
                    !bosAttributeMap.AttributeMappingType.Equals(AttributeMappingTypeValue.P1ProductProvisionStatus)
                   )
                {
                    if (String.IsNullOrEmpty(bosAttributeMap.Value))
                    {
                        throw new MappingException("No Value field for attributeMap :" + attributeName);
                    }
                }

                string xmlData = null;
                string value = null;
                BOS_API.Contact bosContact;

                switch (bosAttributeMap.AttributeMappingType)
                {
                    case AttributeMappingTypeValue.FixedString:
                        value = bosAttributeMap.Value;
                        break;
                    case AttributeMappingTypeValue.XPath:

                        switch (bosAttributeMap.Source)
                        {
                            case AttributeMappingSourceValue.BosOrderItem:
                                xmlData = bosOrderItem.SerializeObject();
                                break;
                            case AttributeMappingSourceValue.BosUser:
                                xmlData = bosUser.SerializeObject();
                                break;
                            case AttributeMappingSourceValue.BosOrder:
                                xmlData = bosOrder.SerializeObject();
                                break;
                        }
                        value = runXPath(xmlData, bosAttributeMap.Value);
                        break;
                    case AttributeMappingTypeValue.Linq:
                        bosContact = GetContact(bosOrder, bosOrderItem);
                        xmlData = bosContact.SerializeObject();
                        value = runXPath(xmlData, bosAttributeMap.Value);
                        break;
                    case AttributeMappingTypeValue.SugarInstanceName:
                        value = getSugarInstanceName(orderItems, companyName, attributeName);
                        break;
                    case AttributeMappingTypeValue.WebHostingProductNames:
                        value = getWebHostingProducts(bosOrder);
                        break;
                    case AttributeMappingTypeValue.WebConsultAndBuildOrderItemCount:
                        value = getWebConsultAndBuildOrderItemCount(bosOrder);
                        break;
                    case AttributeMappingTypeValue.WebConsultAndBuildSalesPrice:
                        value = getWebConsultAndBuildSalesPrice(bosOrder);
                        break;
                    case AttributeMappingTypeValue.PostCodeName:
                        bosContact = GetContact(bosOrder, bosOrderItem);
                        value = bosContact.Address.PostalOutcode + ((bosContact.Address.PostalIncode) != null ? " " + bosContact.Address.PostalIncode : string.Empty);
                        break;
                    case AttributeMappingTypeValue.BuildingNumber:
                        bosContact = GetContact(bosOrder, bosOrderItem);
                        if (bosContact.Address.BuildingNumber != null && bosContact.Address.BuildingName != null)
                        {
                            value = bosContact.Address.BuildingNumber + " " + bosContact.Address.BuildingName;
                        }
                        else if (bosContact.Address.BuildingNumber == null && bosContact.Address.BuildingName != null)
                        {
                            value = bosContact.Address.BuildingName;
                        }
                        else if (bosContact.Address.BuildingNumber != null && bosContact.Address.BuildingName == null)
                        {
                            value = bosContact.Address.BuildingNumber;
                        }
                        value = value.Trim();
                        break;
                    case AttributeMappingTypeValue.P1ProductProvisionStatus:
                        value = getP1ProductProvisionStatus(bosOrder);
                        break;
                }

                if (!String.IsNullOrEmpty(value))
                {
                    switch (bosAttributeMap.PostProcessing)
                    {
                        case AttributeMappingPostProcessingValue.ToUpper:
                            value = value.ToUpper(CultureInfo.InvariantCulture);
                            break;
                        case AttributeMappingPostProcessingValue.ToLower:
                            value = value.ToLower(CultureInfo.InvariantCulture);
                            break;
                        case AttributeMappingPostProcessingValue.BlowfishDecrypt:
                            //Sashi032
                            if (ConfigurationManager.AppSettings["IsAESEncryptionEnabled"] != null 
                                && Convert.ToBoolean(ConfigurationManager.AppSettings["IsAESEncryptionEnabled"]))
                            {
                                value = BT.SaaS.Core.Shared.Utils.DataHelper.DecryptFromAES(value);
                            }
                            else
                            {
                                value = BlowfishEncryptor.Decrypt(value);
                            }
                            break;
                        default:
                            break;
                    }
                }
                return value;
            }
            return null;
        }



        private static string getValueFromAttributeForProduct(string productCode, string attributeName, BT.SaaS.IspssAdapter.BOS_API.OrderItem bosOrderItem)
        {
            // Look up the attribute name in the config to find where to get it
            ConfigurationBosAttributeMapSection bosAttributeMapSection = (ConfigurationBosAttributeMapSection)ConfigurationManager.GetSection("bosAttributeMap");
            if (bosAttributeMapSection != null)
            {
                ConfigurationBosAttributeMap bosAttributeMap = null;

                // get the bosAttributeMap from config searching by productCode first section
                ConfigurationBosProductAttributeMap bosProductAttributeMap = bosAttributeMapSection.BosAttributeMap[productCode];
                if (bosProductAttributeMap != null)
                {
                    bosAttributeMap = bosProductAttributeMap.AttributeMap[attributeName];
                }
                if (bosAttributeMap == null)
                {
                    bosProductAttributeMap = bosAttributeMapSection.BosAttributeMap["all"];
                    if (bosProductAttributeMap != null)
                    {
                        bosAttributeMap = bosProductAttributeMap.AttributeMap[attributeName];
                    }
                }


                string xmlData = null;
                string value = null;

                switch (bosAttributeMap.AttributeMappingType)
                {

                    case AttributeMappingTypeValue.XPath:

                        switch (bosAttributeMap.Source)
                        {
                            case AttributeMappingSourceValue.BosOrderItem:
                                xmlData = bosOrderItem.SerializeObject();
                                break;
                        }
                        value = runXPath(xmlData, bosAttributeMap.Value);
                        break;

                }


                return value;
            }
            return null;
        }



        private static BOS_API.Contact GetContact(placeOrder bosOrder, BT.SaaS.IspssAdapter.BOS_API.OrderItem bosOrderItem)
        {
            string contactId = string.Empty;
            contactId = bosOrderItem.ListOfOrderItemAttribute.Where(attribute => attribute.Name.Equals("Customer Site Address", StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault().NewValue;

            BOS_API.Contact bosContact = bosOrder.AccountAndOrder.Order.ListOfContact.Where(contact => contact.ContactId.Equals(contactId, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault();
            return bosContact;
        }

        private static string runXPath(string xmlDoc, string xPath)
        {
            string value = null;
            try
            {

                StringReader reader = new StringReader(xmlDoc);
                XPathDocument docNav = new XPathDocument(reader);
                XPathNavigator nav = docNav.CreateNavigator();
                System.Xml.XmlNamespaceManager xmlnsManager = new XmlNamespaceManager(nav.NameTable);
                xmlnsManager.AddNamespace("ns1", "http://iat.intra.btexact.com/sysdesign/R7.1/ISPSchema");
                xmlnsManager.AddNamespace("ns2", "http://wls.bfsec.bt.co.uk/schema");

                Logger.Debug("**************** XPATH START *************************");
                Logger.Debug("Input xml:" + xmlDoc);
                Logger.Debug("XPath:" + xPath);
                XPathNodeIterator NodeIter = nav.Select(xPath, xmlnsManager);
                while (NodeIter.MoveNext())
                {
                    value = NodeIter.Current.Value;
                    Logger.Debug("value=" + value);
                }
                Logger.Debug("**************** XPATH END   *************************");
            }
            catch (Exception exc)
            {
                throw new MappingException("Failed to run XPATH :" + exc.Message, exc);
            }
            return (value);
        }

        private static List<BT.SaaS.IspssAdapter.BOS_API.Contact> getSecondaryBosUsers(placeOrder bosOrder, BT.SaaS.IspssAdapter.BOS_API.OrderItem bosOrderItem)
        {
            // Get the Secondary users
            List<BT.SaaS.IspssAdapter.BOS_API.Contact> secondaryUsers = new List<BT.SaaS.IspssAdapter.BOS_API.Contact>();

            if (bosOrderItem.ListOfOrderItemAttribute != null)
            {
                foreach (BT.SaaS.IspssAdapter.BOS_API.ItemAttribute attr in bosOrderItem.ListOfOrderItemAttribute)
                {
                    if (attr.Name.ToUpper(CultureInfo.InvariantCulture).Equals("SECONDARYUSER") ||
                        (attr.Name.ToUpper(CultureInfo.InvariantCulture).Equals("USER") &&
                          attr.Action.ToString().Equals("add", StringComparison.OrdinalIgnoreCase)))
                    {
                        BT.SaaS.IspssAdapter.BOS_API.Contact secondaryContact = findBosContact(bosOrder, attr.NewValue);
                        if (secondaryContact != null)
                        {
                            secondaryUsers.Add(secondaryContact);
                        }
                    }
                    else if (attr.Name.Equals("DomainName", StringComparison.OrdinalIgnoreCase))
                    {
                        //creating alias as secondary users (even admin will be sent as default role)
                        //HE will be called will modifyClient
                        foreach (BT.SaaS.IspssAdapter.BOS_API.Contact bosContact in bosOrder.AccountAndOrder.ISPAccountData.ListOfContact)
                        {
                            if (bosContact != null && !string.IsNullOrEmpty(bosContact.MailboxAlias) && bosContact.MailboxAlias.EndsWith(attr.NewValue, StringComparison.OrdinalIgnoreCase))
                            {
                                secondaryUsers.Add(findBosContact(bosOrder, bosContact.ContactId));
                            }
                        }

                    }
                }
            }
            return secondaryUsers;
        }

        private static BT.SaaS.IspssAdapter.BOS_API.Contact getPrimaryBosUser(placeOrder bosOrder)
        {
            BT.SaaS.IspssAdapter.BOS_API.Contact primaryContact = null;

            if (bosOrder.AccountAndOrder.ISPAccountData.ListOfContact != null)
            {
                primaryContact =
                    (from contact in bosOrder.AccountAndOrder.ISPAccountData.ListOfContact
                     where ((contact.UserType != null) && contact.UserType.ToUpper(CultureInfo.InvariantCulture).Equals("P"))
                     select contact).First();
            }
            return primaryContact;
        }

        static private void buildProductCodeTable(placeOrder bosOrder, List<OrderItemProductMap> orderItemProductMapping)
        {
            // List to hold the product codes that need looked up in MDM
            List<string> saasProductCodeTable = new List<string>();

            // Find the root ISP Control OrderItem in the BOS Order - all VAS orderItems are children of this orderItem
            BT.SaaS.IspssAdapter.BOS_API.OrderItem ispControl = getIspControl(bosOrder);

            // Loop through the VAS OrderItems
            foreach (BT.SaaS.IspssAdapter.BOS_API.OrderItem bosOrderItem in ispControl.OrderItem1)
            {
                analyseBosOrderItem(orderItemProductMapping, saasProductCodeTable, bosOrderItem, null);

                // Now do the same for any child OrderItems (i.e. bolt-ons)
                if (bosOrderItem.OrderItem1 != null)
                {
                    foreach (BT.SaaS.IspssAdapter.BOS_API.OrderItem bosChildOrderItem in bosOrderItem.OrderItem1)
                    {
                        analyseBosOrderItem(orderItemProductMapping, saasProductCodeTable, bosChildOrderItem, bosOrderItem);
                    }
                }
            }

            // Get the MDM product definitions for the products we have found in our order
            List<ProductDefinition> productData = MdmWrapper.getSaaSProductDefs(saasProductCodeTable, getResellerId(), bosOrder.IsORTCheckRequired);

            // Loop through our table of saasProduct codes to find matches
            // working out how many products are known about in the MDM
            if (productData != null &&
                productData.Count > 0)
            {
                foreach (OrderItemProductMap item in orderItemProductMapping)
                {
                    if (item.SaasProductCode != null)
                    {
                        foreach (ProductDefinition prodDef in productData)
                        {
                            if (item.SaasProductCode.Equals(prodDef.Code))
                            {
                                item.ProductDefinition = prodDef;
                            }
                            if (item.SaasOldProductCode != null && item.SaasOldProductCode.Equals(prodDef.Code))
                            {
                                item.OldProductDefinition = prodDef;
                            }
                        }
                    }
                }
            }

            // Loop through the items and remove any that cannot be processed by SaaS
            List<OrderItemProductMap> itemsToRemove = new List<OrderItemProductMap>();
            foreach (OrderItemProductMap item in orderItemProductMapping)
            {
                // No mapping between BOS product codes and SaaS product code
                if (item.SaasProductCode == null)
                {
                    Logger.Debug("Removing BOS OrderItem with productInstanceId: " + item.BosOrderItem.ProductInstanceId +
                                     " - no equivalent saasProductCode\n");
                    itemsToRemove.Add(item);
                    continue;
                }

                // No MDM product Definition for this product
                if (item.ProductDefinition == null)
                {
                    Logger.Debug("Removing BOS OrderItem with productInstanceId: " + item.BosOrderItem.ProductInstanceId +
                                     ", productCode: " + item.SaasProductCode + " - no MDM product definition\n");
                    itemsToRemove.Add(item);
                    continue;
                }

                // Is the product active?
                if (item.ProductDefinition.Status.Equals(ProductDefinitionStatus.Inactive))
                {
                    Logger.Debug("Removing BOS OrderItem with productInstanceId: " + item.BosOrderItem.ProductInstanceId +
                                     ", productCode: " + item.SaasProductCode +
                                     " - product not active in MDM, status: " + item.ProductDefinition.Status.ToString() + "\n");
                    itemsToRemove.Add(item);
                    continue;
                }

                // BOS OrderItem with action None i.e. nothing needs done
                if (item.BosOrderItem.Action == BT.SaaS.IspssAdapter.BOS_API.Action.None)
                {
                    Logger.Debug("Removing BOS OrderItem with productInstanceId: " + item.BosOrderItem.ProductInstanceId +
                                     " - BOS action is None\n");
                    itemsToRemove.Add(item);
                    continue;
                }
            }

            foreach (OrderItemProductMap item in itemsToRemove)
            {
                orderItemProductMapping.Remove(item);
            }

            Logger.Debug("Returning " + orderItemProductMapping.Count + " orderItemProductMappings");
        }

        static private void analyseBosOrderItem(List<OrderItemProductMap> orderItemProductMapping, List<string> saasProductCodeTable, BT.SaaS.IspssAdapter.BOS_API.OrderItem bosOrderItem, BT.SaaS.IspssAdapter.BOS_API.OrderItem parentBosOrderItem)
        {
            string saasProductCode;
            string saasOldProductCode;

            // Look up the SaaS productCode
            if (bosOrderItem.Action == BT.SaaS.IspssAdapter.BOS_API.Action.Modify)
            {
                // if its a regrade then we want the productCode we are regrading from
                saasOldProductCode = getOldSAASProductCode(bosOrderItem);
            }
            else
            {
                saasOldProductCode = null;
            }
            saasProductCode = getSAASProductCode(bosOrderItem);

            orderItemProductMapping.Add(new OrderItemProductMap()
            {
                BosOrderItem = bosOrderItem,
                SaasProductCode = saasProductCode,
                SaasOldProductCode = saasOldProductCode,
                BosParentOrderItem = parentBosOrderItem
            });

            if (saasProductCode != null)
            {
                // Store the productCode (if not already stored) in our table for sending to MDM
                if (!saasProductCodeTable.Contains(saasProductCode))
                    saasProductCodeTable.Add(saasProductCode);

            }

            if (saasOldProductCode != null)
            {
                // Store the productCode (if not already stored) in our table for sending to MDM
                if (!saasProductCodeTable.Contains(saasOldProductCode))
                    saasProductCodeTable.Add(saasOldProductCode);
            }
        }

        static public int getResellerId()
        {
            int resellerId;
            string btRetailId = ConfigurationManager.AppSettings["btRetailServiceProviderId"].ToString();

            if (btRetailId == null)
            {
                throw new MappingException("No Reseller Id for BT Retail defined in Web.config/appSettings");
            }

            try
            {
                resellerId = int.Parse(btRetailId, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                throw new MappingException("Invalid Reseller Id " + btRetailId + "for BT Retail defined in Web.config/appSettings");
            }
            return resellerId;
        }

        static public void validateBosXML(placeOrder placeOrder)
        {
            if (placeOrder == null)
            {
                throw new MappingException("No input XML");
            }

            if (placeOrder.AccountAndOrder == null)
            {
                throw new MappingException("No AccountAndOrder in the input XML");
            }

            if (placeOrder.AccountAndOrder.ISPAccountData == null)
            {
                throw new MappingException("No AccountAndOrder.ISPAccountData in the input XML");
            }

            if (placeOrder.AccountAndOrder.Order == null)
            {
                throw new MappingException("No AccountAndOrder.Order in the input XML");
            }

            if (placeOrder.AccountAndOrder.Order.ListOfOrderItem == null)
            {
                throw new MappingException("No AccountAndOrder.Order.ListOfOrderItem in the input XML");
            }

            // How many ISP Control orderItems are there?

            // First look up the S-code in the config file
            string ispControlSCode = ConfigurationManager.AppSettings["ispControlSCode"];
            if (ispControlSCode == null)
            {
                throw new MappingException("No definition for ispControlSCode in Web.config");
            }

            IEnumerable<BT.SaaS.IspssAdapter.BOS_API.OrderItem> ispControl =
                from orderItem in placeOrder.AccountAndOrder.Order.ListOfOrderItem
                where orderItem.ProductCode.Equals(ispControlSCode)
                select orderItem;

            if (ispControl.Count() > 1)
            {
                throw new MappingException(
                    "There not be more than one \"ISP Control\" OrderItem in the input XML. This input has "
                    + ispControl.Count());
            }
        }

        static private BT.SaaS.Core.Shared.Entities.OrderHeader mapOrderHeader(BT.SaaS.Core.Shared.Entities.Order saasOrder, placeOrder bosOrder, List<OrderItemProductMap> orderItemProductMapping, List<string> existingProductInstanceKeys)
        {
            // OrderHeader
            BT.SaaS.Core.Shared.Entities.OrderHeader orderHeader = saasOrder.Header;

            // We have a major problem here if BOS can send orders with mixed orderItem types!!!
            // We do not support and will throw an Exception
            orderHeader.Action = calculateOrderType(bosOrder, orderItemProductMapping, existingProductInstanceKeys);

            orderHeader.Status = OrderStatusEnum.@new;
            orderHeader.OrderKey = bosOrder.AccountAndOrder.Order.OrderNumber;
            orderHeader.OrderID = Guid.Empty;
            orderHeader.ServiceProviderID = getResellerId();

            // Only map required by date if it's a cease order
            if (orderHeader.Action == OrderActionEnum.cease || orderHeader.Action == OrderActionEnum.modifyService)
            {
                orderHeader.EffectiveDateTime = mapBosDate(bosOrder.AccountAndOrder.Order.RequiredByDate);

            }
            else
            {
                orderHeader.EffectiveDateTime = System.DateTime.Now;
            }
            orderHeader.OrderDateTime = System.DateTime.Now;

            // OrderHeader.Customer

            orderHeader.Customer = mapCustomer(bosOrder);

            //Check for New/Migrated Customers
            isNewOrMigratedCustomer = IsNewOrMigratedCustomer(orderHeader.Customer);

            // OrderHeader.Customer.Users

            // NB Users in a BOS new customer provide orders are always new. 
            // Users in all other order types are always existing

            orderHeader.Users = mapUsers(bosOrder, saasOrder);

            // OrderHeader.Customer.UserPlacingOrder

            //orderHeader.IdentityPlacingOrderXmlRef = findIdentityPlacingOrder(orderHeader.Users, placeOrder);
            orderHeader.IdentityPlacingOrder = new BT.SaaS.Core.Shared.Entities.Identity()
            {
                identifier = bosOrder.AccountAndOrder.Order.EmployeeId,
                identiferNamepace = "BOS"
            };

            return orderHeader;
        }

        static private DateTime mapBosDate(string bosDate)
        {
            if (String.IsNullOrEmpty(bosDate))
            {
                return DateTime.Now;
            }
            else
            {
                try
                {
                    string[] formats = new string[] { "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy" };
                    return DateTime.ParseExact(bosDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                }
                catch (Exception e)
                {
                    throw new MappingException("Bad date " + bosDate + " " + e.Message);
                }
            }
        }

        static public BT.SaaS.IspssAdapter.BOS_API.OrderItem getIspControl(placeOrder bosOrder)
        {
            BT.SaaS.IspssAdapter.BOS_API.OrderItem ispControl;

            // First look up the S-code int he config file
            string ispControlSCode = ConfigurationManager.AppSettings["ispControlSCode"];
            if (ispControlSCode == null)
            {
                throw new MappingException("No definition for ispControlSCode in Web.config");
            }

            // Search the root orderItems for one with the right S-Code
            IEnumerable<BT.SaaS.IspssAdapter.BOS_API.OrderItem> ispControlResult =
                    (from orderItem in bosOrder.AccountAndOrder.Order.ListOfOrderItem
                     where orderItem.ProductCode.Equals(ispControlSCode)
                     select orderItem);

            if (ispControlResult.Count() == 0)
            {
                return null;
            }

            if (ispControlResult.Count() > 1)
            {
                throw new MappingException("There should be only 1 ISP Control Order Item in this order not " + ispControlResult.Count());
            }

            ispControl = ispControlResult.First();

            return ispControl;
        }

        static private OrderActionEnum calculateOrderType(placeOrder bosOrder, List<OrderItemProductMap> orderItemProductMapping, List<string> existingProductInstanceKeys)
        {
            OrderActionEnum orderAction;

            // Do we have any orderItems to map?
            if (orderItemProductMapping.Count() == 0)
            {
                if (bosOrder.isExistingCustomer())
                {
                    // Existing customer - no orderItems - Nothing to do
                    Logger.Debug("Existing customer and no orderItems for SaaS products in this order. Nothing to do.");
                    orderAction = OrderActionEnum.notDefined;
                }
                else
                {
                    // New customer no orderItems
                    Logger.Debug("New customer and no orderItems for SaaS products in this order. Customer & User(s) still should be created.");
                    orderAction = OrderActionEnum.provide;
                }
            }
            else
            {
                orderAction = findOrderAction(orderItemProductMapping, existingProductInstanceKeys);
            }

            Logger.Debug("orderAction set to " + orderAction.ToString());
            return orderAction;
        }

        private static OrderActionEnum findOrderAction(List<OrderItemProductMap> orderItemProductMapping, List<string> existingProductInstanceKeys)
        {
            // Allowed structure of Orderitems in BOS Order
            //
            // orderItem type                   ISPControl         VAS           Bolt-on
            //
            // placeOrder.AccountAndOrder.Order.ListOfOrderItem[0].OrderItem1[n].OrderItem1[m]
            //               
            // orderItem action                      
            //                                  Add                Add[n]        Add[m]
            // ---------------------------------------------------------------------------------------
            //                                  None               None[a]       Add[e], Modify[f], Delete[g] e+f+g=m
            //                                                     Add[b]        Add[m]
            //                                                     Modify[c]     Add[e], Modify[f], Delete[g] e+f+g=m
            //                                                     Delete[d]     Delete[m]
            //                                                     a+b+c+d=n

            // We will look at the VAS orderItems actions
            // If the VAS orderItem has action None we look at the bolt-ons actions
            // If all the actions are the same we return that - otherwise we throw an exception
            // as it's a mixed order

            OrderActionEnum orderAction = OrderActionEnum.notDefined;
            bool isBulkVasRegrade = false;

            // Go through the list of orderItems SaaS knows about
            BT.SaaS.Core.Shared.Entities.OrderActionEnum previousAction = OrderActionEnum.notDefined;
            foreach (OrderItemProductMap orderItemMapping in orderItemProductMapping)
            {
                BT.SaaS.IspssAdapter.BOS_API.OrderItem bosOrderItem = orderItemMapping.BosOrderItem;
                string productInstanceId = bosOrderItem.ProductInstanceId;
                BT.SaaS.IspssAdapter.BOS_API.Action bosAction = bosOrderItem.Action;

                // Is this a base product or a bolt-on?
                bool boltOn = orderItemMapping.BosParentOrderItem == null ? false : true;
                Logger.Debug("orderItem productInstanceId=" + productInstanceId + " bolt-on:" + boltOn.ToString());

                // Check Whether Order is Bulk Vas Regarde Order, then directly make the OrderAction as "resign"
                isBulkVasRegrade = CheckIsBulkRegradeOrder(bosOrderItem);

                // We only really need to look at bolt-ons if the parent action is None
                if (boltOn && orderItemMapping.BosParentOrderItem.Action != BT.SaaS.IspssAdapter.BOS_API.Action.None)
                    continue;

                // If the productInstance exists get the existing quantity
                if (isExistingProductInstance(existingProductInstanceKeys, orderItemMapping.BosOrderItem))
                {
                    if (!isBulkVasRegrade)
                    {
                        // Get the quantity on the productInstance
                        int quantity, freeQuantity;
                        // Check if this is a regrade or not for whether we pass the oldProductDefinition or new one
                        if (orderItemMapping.OldProductDefinition == null)
                        {
                            GetVasUsageImpl.getProductInstanceUsage(out quantity, out freeQuantity, productInstanceId, orderItemMapping.ProductDefinition);
                        }
                        else
                        {
                            GetVasUsageImpl.getProductInstanceUsage(out quantity, out freeQuantity, productInstanceId, orderItemMapping.OldProductDefinition);
                        }
                        orderItemMapping.Quantity = quantity;
                    }
                }
                else
                {
                    orderItemMapping.Quantity = -1;
                }

                switch (bosAction)
                {
                    case BT.SaaS.IspssAdapter.BOS_API.Action.Add:

                        // If the productInstance exists then we are increasing the quantity
                        if (isExistingProductInstance(existingProductInstanceKeys, orderItemMapping.BosOrderItem))
                        {
                            orderAction = OrderActionEnum.modifyService;
                        }
                        else
                        {
                            orderAction = OrderActionEnum.provide;
                        }
                        break;

                    case BT.SaaS.IspssAdapter.BOS_API.Action.Delete:

                        // Can only have a Delete if there is a productInstance in SaaS
                        if (!isExistingProductInstance(existingProductInstanceKeys, orderItemMapping.BosOrderItem))
                        {
                            throw new MappingException("BOS orderItem productInstanceId=" + productInstanceId + " with action Delete but no productInstance in SaaS MDM");
                        }

                        // Get the quantity on the productInstance
                        int orderQuantity;
                        if (!int.TryParse(bosOrderItem.Quantity, out orderQuantity))
                        {
                            throw new MappingException("Bad Quantity " + bosOrderItem.Quantity + " in orderItem. ProductInstanceId=" + productInstanceId);
                        }
                        // If the quantity in the order is less than the quantity on the product instance then
                        // we are reducing the quantity so it's a modifyService otherwise we are ceasing
                        if (orderQuantity < orderItemMapping.Quantity)
                            orderAction = OrderActionEnum.modifyService;
                        else
                            orderAction = OrderActionEnum.cease;
                        break;

                    case BT.SaaS.IspssAdapter.BOS_API.Action.Modify:

                        // Can only have a Modify if there is a productInstance in SaaS
                        if (!isExistingProductInstance(existingProductInstanceKeys, orderItemMapping.BosOrderItem))
                        {
                            throw new MappingException("BOS orderItem productInstanceId=" + productInstanceId + "  with action Modify but no productInstance in SaaS MDM");
                        }
                        bool isResign = getOrderType(bosOrderItem);

                        if (isResign || isBulkVasRegrade)
                            orderAction = OrderActionEnum.resign;
                        else
                            orderAction = OrderActionEnum.regrade;

                        break;

                    default:

                        throw new MappingException("BOS orderItem productInstanceId=" + productInstanceId + " with unsupported Action type " + bosAction.ToString());
                }

                // If not the first time through the loop then check for mixed orderItems in the order
                if (!(previousAction == OrderActionEnum.notDefined))
                {
                    if (orderAction != previousAction)
                    {
                        throw new MappingException("Mixed orderItem types are not supported");
                    }
                }
                previousAction = orderAction;
            }
            return orderAction;
        }

        public static Boolean isExistingProductInstance(List<string> existingProductInstanceKeys, BT.SaaS.IspssAdapter.BOS_API.OrderItem orderItem)
        {
            // find out if this orderItem is for a productInstance that already exists or not
            Boolean existingProductInstance = false;
            if (existingProductInstanceKeys.Contains(orderItem.ProductInstanceId))
                existingProductInstance = true;
            else
                existingProductInstance = false;

            return existingProductInstance;
        }

        static public string value(string input)
        {
            if (String.IsNullOrEmpty(input))
                return null;
            else
                return input;
        }

        static private BT.SaaS.Core.Shared.Entities.Customer mapCustomer(placeOrder bosOrder)
        {
            BT.SaaS.Core.Shared.Entities.Customer customer = new BT.SaaS.Core.Shared.Entities.Customer();

            // Customer 
            customer.Action = bosOrder.isExistingCustomer() ? CustomerActionEnum.none : CustomerActionEnum.add;
            customer.Status = bosOrder.isExistingCustomer() ? DataStatusEnum.done : DataStatusEnum.notDone;
            customer.CustomerKey = bosOrder.getCustomerKey();

            // As an external system we probably should have to set the customerId but PE requires this
            // for existing customers
            if (bosOrder.isExistingCustomer())
            {
                customer.CustomerId = bosOrder.getCustomerId().ToString();
            }
            else
            {
                customer.CustomerId = null;
            }

            customer.CustomerStatus = null;

            // Customer.Contacts
            customer.Contacts = mapContacts(bosOrder);

            customer.CompanyName = value(bosOrder.AccountAndOrder.ISPAccountData.ServiceAccountName);
            // This is here because of a bug in the PE it should be removed!
            customer.TradingName = "";

            // Customer.BillAccounts
            customer.BillingAccounts = mapBillingAccounts(bosOrder);

            customer.Attributes = null;

            if (bosOrder.isExistingCustomer())
            {
                List<Dnp.ClientCharacteristic> clientChar = getCustomerCharacterstics(bosOrder.getCustomerKey());
                if (clientChar != null && clientChar.Count > 0)
                {
                    customer.Attributes = new List<BT.SaaS.Core.Shared.Entities.Attribute>();

                    foreach (Dnp.ClientCharacteristic attr in clientChar)
                    {
                        if (attr.name.Equals("IS_HE_COMPATIBLE", StringComparison.OrdinalIgnoreCase))
                        {
                            customer.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute()
                            {
                                action = DataActionEnum.add,
                                Name = "ISHECOMPATIBLE",
                                Value = attr.value
                            });
                        }
                        else if (attr.name.Equals("ORG_MIG_STATUS", StringComparison.OrdinalIgnoreCase))
                        {
                            customer.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute()
                            {
                                action = DataActionEnum.add,
                                Name = "ORG_MIG_STATUS",
                                Value = attr.value
                            });
                        }
                    }
                }
            }
            else
            { // New Customer So Intially mapping Org_Mig_Status as New at Customer Level Attributes
                customer.Attributes = new List<BT.SaaS.Core.Shared.Entities.Attribute>();
                customer.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute()
                {
                    action = DataActionEnum.add,
                    Name = "ORG_MIG_STATUS",
                    Value = "NEW"
                });
            }

            return customer;
        }

        static private List<BT.SaaS.Core.Shared.Entities.Contact> mapContacts(placeOrder bosOrder)
        {
            List<BT.SaaS.Core.Shared.Entities.Contact> contacts = new List<BT.SaaS.Core.Shared.Entities.Contact>();

            AccountAndOrder accountAndOrder = bosOrder.AccountAndOrder;
            ISPAccountData ispAccountData = accountAndOrder.ISPAccountData;
            BT.SaaS.Core.Shared.Entities.Address saasPrimaryAddress = null;
            if (ispAccountData.ListOfContact != null)
            {
                for (int c = 0; c < ispAccountData.ListOfContact.Length; c++)
                {
                    BT.SaaS.IspssAdapter.BOS_API.Contact contact = ispAccountData.ListOfContact[c];

                    if (contact.UserId == null)
                    {
                        throw new MappingException("No UserId in BOS Contact");
                    }

                    BT.SaaS.Core.Shared.Entities.Contact saasContact = null;

                    // Check this contact doesn't already exist 
                    foreach (BT.SaaS.Core.Shared.Entities.Contact contactFromList in contacts)
                    {
                        if (contactFromList.ContactKey.Equals(userFromEmailAddress(contact.UserId)))
                        {
                            saasContact = contactFromList;
                            break;
                        }
                    }
                    // If the contact already exists we might want to add more mailbox aliases as email contacts
                    if (saasContact != null)
                    {
                        // If it's not already there add  mailboxAlias as an email contact
                        if (!String.IsNullOrEmpty(contact.MailboxAlias))
                        {
                            bool mailboxAliasIsEmailContact = false;
                            foreach (BT.SaaS.Core.Shared.Entities.EmailContact emailContactInList in saasContact.EmailAddresses)
                            {
                                if (emailContactInList.EmailAddress.Equals(contact.MailboxAlias))
                                {
                                    mailboxAliasIsEmailContact = true;
                                }
                            }
                            if (!mailboxAliasIsEmailContact)
                            {
                                saasContact.EmailAddresses.Add(new BT.SaaS.Core.Shared.Entities.EmailContact()
                                {
                                    Type = null,
                                    EmailAddress = contact.MailboxAlias,
                                    // If no other email address is set as preferred make this one preferred
                                    IsPreferred = false,
                                    SuppressAll3rdPartyEmails = false,
                                    SuppressAllBTEmails = false,
                                    SuppressAllVendorEmails = false
                                });
                            }
                        }
                        continue;
                    }

                    // Let's create a new contact
                    saasContact = new BT.SaaS.Core.Shared.Entities.Contact();
                    saasContact.XmlIdKey = "CO-" + c.ToString(CultureInfo.InvariantCulture);

                    // In a BOS Order contacts are only new if it's for a new Customer
                    saasContact.Action = (bosOrder.isExistingCustomer()) ? DataActionEnum.none : DataActionEnum.add;

                    saasContact.ContactKey = userFromEmailAddress(contact.UserId);
                    // not done as PE still needs to update D&P
                    saasContact.Status = bosOrder.isExistingCustomer() ? DataStatusEnum.done : DataStatusEnum.notDone;

                    saasContact.ContactType = value(contact.UserType);

                    if (contact.Person != null)
                    {
                        saasContact.JobTitle = value(contact.Person.JobTitle);
                        saasContact.Title = value(contact.Person.Title);
                        saasContact.FirstName = value(contact.Person.Forename);
                        saasContact.MiddleName = value(contact.Person.Initial);
                        saasContact.LastName = value(contact.Person.Surname);
                        saasContact.Honours = value(contact.Person.Honours);
                        saasContact.PreferredName = null;
                    }
                    if (contact.Address != null)
                    {
                        saasContact.Address = mapAddress(contact.Address);
                        if (String.IsNullOrEmpty(saasContact.Address.UkAddress.OrganisationName))
                        {
                            saasContact.Address.UkAddress.OrganisationName = value(bosOrder.AccountAndOrder.ISPAccountData.ServiceAccountName);
                        }
                        saasPrimaryAddress = saasContact.Address;
                    }
                    else
                    {
                        saasContact.Address = null;
                    }

                    saasContact.EmailAddresses = new List<BT.SaaS.Core.Shared.Entities.EmailContact>();
                    Boolean isPreferredSet = false;
                    if (contact.EMailContact != null)
                    {
                        foreach (EMailContact bosEmailContact in contact.EMailContact)
                        {
                            BT.SaaS.Core.Shared.Entities.EmailContact emailContact = new BT.SaaS.Core.Shared.Entities.EmailContact();
                            emailContact.Type = null;
                            emailContact.EmailAddress = value(bosEmailContact.EMailAddress);
                            //emailContact.IsPreferred = bosEmailContact.PreferredContact != null && bosEmailContact.PreferredContact.ToUpper(CultureInfo.InvariantCulture).Equals("Y");
                            //emailContact.IsPreferred = true;
                            emailContact.IsPreferred = bosEmailContact.PreferredContact != null && ((bosEmailContact.PreferredContact.ToUpper(CultureInfo.InvariantCulture).Equals("Y")) || (bosEmailContact.PreferredContact.ToUpper(CultureInfo.InvariantCulture).Equals("N")));
                            if (emailContact.IsPreferred)
                            {
                                isPreferredSet = true;
                            }
                            emailContact.SuppressAll3rdPartyEmails = bosEmailContact.SuppressAll3rdPartyEMails != null && bosEmailContact.SuppressAll3rdPartyEMails.ToUpper(CultureInfo.InvariantCulture).Equals("Y");
                            emailContact.SuppressAllBTEmails = bosEmailContact.SuppressAllBTEMails != null && bosEmailContact.SuppressAllBTEMails.ToUpper(CultureInfo.InvariantCulture).Equals("Y");
                            emailContact.SuppressAllVendorEmails = bosEmailContact.SuppressAllVendorEMails != null && bosEmailContact.SuppressAllVendorEMails.ToUpper(CultureInfo.InvariantCulture).Equals("Y");
                            if (emailContact.EmailAddress != null) saasContact.EmailAddresses.Add(emailContact);
                        }
                    }

                    // If it's not already there add the btconnect id for the user as an email contact
                    bool btconnectIdIsEmailContact = false;
                    foreach (BT.SaaS.Core.Shared.Entities.EmailContact emailContactInList in saasContact.EmailAddresses)
                    {
                        if (emailContactInList.EmailAddress.Equals(contact.UserId))
                        {
                            btconnectIdIsEmailContact = true;
                            // if the btconnect id was there and preferred email is not set, then set it 
                            if (!isPreferredSet)
                            {
                                isPreferredSet = true;
                                emailContactInList.IsPreferred = true;
                            }
                        }
                    }

                    if (!btconnectIdIsEmailContact)
                    {
                        // If no other email address is set as preferred make this one preferred
                        isPreferredSet = !isPreferredSet;
                        saasContact.EmailAddresses.Add(new BT.SaaS.Core.Shared.Entities.EmailContact()
                        {
                            Type = null,
                            EmailAddress = contact.UserId,
                            IsPreferred = isPreferredSet,
                            SuppressAll3rdPartyEmails = false,
                            SuppressAllBTEmails = false,
                            SuppressAllVendorEmails = false
                        });
                    }

                    // If it's not already there add  mailboxAlias as an email contact
                    if (!String.IsNullOrEmpty(contact.MailboxAlias))
                    {
                        bool mailboxAliasIsEmailContact = false;
                        foreach (BT.SaaS.Core.Shared.Entities.EmailContact emailContactInList in saasContact.EmailAddresses)
                        {
                            if (emailContactInList.EmailAddress.Equals(contact.MailboxAlias))
                            {
                                mailboxAliasIsEmailContact = true;
                            }
                        }
                        if (!mailboxAliasIsEmailContact)
                        {
                            saasContact.EmailAddresses.Add(new BT.SaaS.Core.Shared.Entities.EmailContact()
                            {
                                Type = null,
                                EmailAddress = contact.MailboxAlias,
                                // If no other email address is set as preferred make this one preferred
                                IsPreferred = false,
                                SuppressAll3rdPartyEmails = false,
                                SuppressAllBTEmails = false,
                                SuppressAllVendorEmails = false
                            });
                        }
                    }

                    // Telephone contacts
                    if (contact.TelephoneContact != null)
                    {
                        int numTelContacts = contact.TelephoneContact.Length;
                        saasContact.TelephoneNumbers = new List<BT.SaaS.Core.Shared.Entities.TelephoneContact>();
                        Boolean preferredSet = false;
                        for (int n = 0; n < numTelContacts; n++)
                        {
                            BT.SaaS.Core.Shared.Entities.TelephoneContact telephoneContact = new BT.SaaS.Core.Shared.Entities.TelephoneContact();
                            telephoneContact.Number = value(contact.TelephoneContact[n].Number);
                            telephoneContact.Extension = null;
                            telephoneContact.Type = value(contact.TelephoneContact[n].Type);
                            telephoneContact.IsPreferred = contact.TelephoneContact[n].PreferredContact != null && contact.TelephoneContact[n].PreferredContact.ToUpper(CultureInfo.InvariantCulture).Equals("Y");
                            if (telephoneContact.Number != null)
                            {
                                saasContact.TelephoneNumbers.Add(telephoneContact);
                                if (telephoneContact.IsPreferred) preferredSet = true;
                            }
                        }

                        if (saasContact.TelephoneNumbers.Count == 0) saasContact.TelephoneNumbers = null;
                        else
                        {
                            // If none of the numbers are set as preferred, set the first one
                            if (!preferredSet)
                            {
                                saasContact.TelephoneNumbers[0].IsPreferred = true;
                            }
                        }
                    }
                    else
                    {
                        saasContact.TelephoneNumbers = null;
                    }
                    saasContact.dateOfBirth = mapBosDate(contact.Person.DateOfBirth);
                    saasContact.Gender = value(contact.Person.Gender);

                    contacts.Add(saasContact);
                }
            }

            #region Copy Primary Users Address to Secondary Users if Null
            if (saasPrimaryAddress != null && contacts != null && contacts.Count > 0)
            {
                foreach (BT.SaaS.Core.Shared.Entities.Contact saasContact in contacts)
                {
                    if (saasContact.Address == null) //For secondary users this will be null from BOS
                        saasContact.Address = saasPrimaryAddress;
                }
            }
            #endregion

            return contacts;
        }

        private static string userFromEmailAddress(string emailAddress)
        {
            //char[] splitter = { '@' };
            //string[] parsedUserId = emailAddress.Split(splitter);
            //return parsedUserId[0];
            return emailAddress;

        }

        private static BT.SaaS.Core.Shared.Entities.Address mapAddress(BT.SaaS.IspssAdapter.BOS_API.Address bosAddress)
        {
            if (bosAddress == null)
            {
                return null;
            }

            BT.SaaS.Core.Shared.Entities.Address saasAddress = new BT.SaaS.Core.Shared.Entities.Address();

            saasAddress.IntlAddress = null;
            saasAddress.UkAddress = new BT.SaaS.Core.Shared.Entities.UKAddress();
            saasAddress.UkAddress.DepartmentName = value(bosAddress.DepartmentName);
            saasAddress.UkAddress.BuildingName = value(bosAddress.BuildingName);
            saasAddress.UkAddress.BuildingNumber = value(bosAddress.BuildingNumber);


            saasAddress.UkAddress.DependentLocality = value(bosAddress.DependentLocality);
            saasAddress.UkAddress.DependentThoroughfareName = value(bosAddress.DependentThoroughfare);
            saasAddress.UkAddress.DoubleDependentLocality = value(bosAddress.DoubleDependentLocality);
            saasAddress.UkAddress.OrganisationName = value(bosAddress.OrganisationName);
            saasAddress.UkAddress.POBox = value(bosAddress.POBox);
            if (String.IsNullOrEmpty(bosAddress.PostalOutcode))
            {
                saasAddress.UkAddress.Postcode = null;
            }
            else
            {
                saasAddress.UkAddress.Postcode = bosAddress.PostalOutcode +
                    ((value(bosAddress.PostalIncode) != null) ? " " + value(bosAddress.PostalIncode) : string.Empty);
            }
            saasAddress.UkAddress.PostTown = value(bosAddress.PostTown);
            saasAddress.UkAddress.SubBuildingName = value(bosAddress.SubBuildingName);
            saasAddress.UkAddress.ThoroughfareName = value(bosAddress.Thoroughfare);

            return saasAddress;
        }

        static private List<User> mapUsers(placeOrder bosOrder, BT.SaaS.Core.Shared.Entities.Order saasOrder)
        {
            List<User> users = new List<User>();

            AccountAndOrder accountAndOrder = bosOrder.AccountAndOrder;
            ISPAccountData ispAccountData = accountAndOrder.ISPAccountData;
            bool preferredEmailAsIdentity = false, mailboxAliasAsIdentity = false;

            string preferredEmailAsIdentityString = ConfigurationManager.AppSettings["preferredEmailAsIdentity"];
            if (preferredEmailAsIdentityString.ToUpper(CultureInfo.InvariantCulture).Equals("TRUE"))
                preferredEmailAsIdentity = true;

            string mailboxAliasAsIdentityString = ConfigurationManager.AppSettings["mailboxAliasAsIdentity"];
            if (mailboxAliasAsIdentityString.ToUpper(CultureInfo.InvariantCulture).Equals("TRUE"))
                mailboxAliasAsIdentity = true;

            if (ispAccountData.ListOfContact != null)
            {
                // These are used for XmlIdKey values
                int userNumber = 0, identityNumber = 0;

                foreach (SaaS.IspssAdapter.BOS_API.Contact contact in ispAccountData.ListOfContact)
                {
                    //Alias Fix
                    if (!string.IsNullOrEmpty(contact.MailboxAlias))
                        mailboxAliasAsIdentity = true;
                    else
                        mailboxAliasAsIdentity = false;

                    // Does this contact have a userId?
                    if (String.IsNullOrEmpty(contact.UserId))
                        continue;

                    SaaS.Core.Shared.Entities.User user = null;

                    // Has this user already been mapped?
                    foreach (User userAlreadyMapped in users)
                    {
                        if (userAlreadyMapped.IdentityCredential[0].Identity.identifier.Equals(contact.UserId))
                        {
                            user = userAlreadyMapped;
                            break;
                        }
                    }

                    // User has already been mapped - in this case we can add more aliases
                    if (user != null)
                    {
                        if (mailboxAliasAsIdentity)
                        {
                            if (contact.MailboxAlias != null)
                            {
                                BT.SaaS.Core.Shared.Entities.IdentityCredential mailboxAliasIdentity;
                                if (isPrimaryUser(contact))
                                {
                                    mailboxAliasIdentity = mapIdentityCredential(identityNumber, contact.MailboxAlias, true, contact.Password, ispAccountData.SecretQuestion, ispAccountData.SecretAnswer);
                                }
                                else
                                {
                                    mailboxAliasIdentity = mapIdentityCredential(identityNumber, contact.MailboxAlias, true, contact.Password, null, null);
                                }
                                identityNumber++;
                                user.IdentityCredential.Add(mailboxAliasIdentity);
                            }
                        }
                    }
                    // New user
                    else
                    {
                        user = new SaaS.Core.Shared.Entities.User();

                        user.XmlIdKey = "CL-" + userNumber.ToString(CultureInfo.InvariantCulture);
                        // Can only have new users in a new Customer order
                        user.Action = bosOrder.isExistingCustomer() ? ClientActionEnum.none : ClientActionEnum.add;
                        user.Status = bosOrder.isExistingCustomer() ? DataStatusEnum.done : DataStatusEnum.notDone;
                        user.ClientStatus = ClientStatusEnum.active;
                        user.ClientId = null;
                        user.Type = (contact.UserType.ToUpper(CultureInfo.InvariantCulture).Equals("P")) ? "ADMIN" : "DEFAULT";

                        user.IdentityCredential = new List<BT.SaaS.Core.Shared.Entities.IdentityCredential>();

                        // Map first identity
                        BT.SaaS.Core.Shared.Entities.IdentityCredential primaryId;
                        if (isPrimaryUser(contact))
                        {
                            primaryId = mapIdentityCredential(identityNumber, contact.UserId, false, contact.Password, ispAccountData.SecretQuestion, ispAccountData.SecretAnswer);
                        }
                        else
                        {
                            primaryId = mapIdentityCredential(identityNumber, contact.UserId, false, contact.Password, null, null);
                        }

                        identityNumber++;
                        user.IdentityCredential.Add(primaryId);

                        // Have we a mailboxAlias?
                        if (mailboxAliasAsIdentity)
                        {
                            if (contact.MailboxAlias != null)
                            {
                                BT.SaaS.Core.Shared.Entities.IdentityCredential mailboxAliasIdentity;
                                if (isPrimaryUser(contact))
                                {
                                    mailboxAliasIdentity = mapIdentityCredential(identityNumber, contact.MailboxAlias, true, contact.Password, ispAccountData.SecretQuestion, ispAccountData.SecretAnswer);
                                }
                                else
                                {
                                    mailboxAliasIdentity = mapIdentityCredential(identityNumber, contact.MailboxAlias, true, contact.Password, null, null);
                                }
                                identityNumber++;
                                user.IdentityCredential.Add(mailboxAliasIdentity);
                            }
                        }

                        // Check if we need to map the preferred email as another identity                            

                        if (preferredEmailAsIdentity)
                        {
                            string preferredEmail = getPreferredEmailAddress(contact);
                            // don't do this for btconnect.com emails as they will already be identities in D&P
                            if (preferredEmail != null && !preferredEmail.Contains("@btconnect.com"))
                            {
                                BT.SaaS.Core.Shared.Entities.IdentityCredential perferredEmailAliasIdentity;
                                if (isPrimaryUser(contact))
                                {
                                    perferredEmailAliasIdentity = mapIdentityCredential(identityNumber, preferredEmail, true, contact.Password, ispAccountData.SecretQuestion, ispAccountData.SecretAnswer);
                                }
                                else
                                {
                                    perferredEmailAliasIdentity = mapIdentityCredential(identityNumber, preferredEmail, true, contact.Password, null, null);
                                }
                                identityNumber++;
                                user.IdentityCredential.Add(perferredEmailAliasIdentity);
                            }
                        }

                        // Add in the attributes that are used for setting up the roleInstance that gives the user
                        // the ability to log into the SaaS Portal
                        user.Attributes = new List<BT.SaaS.Core.Shared.Entities.Attribute>();
                        string portalServiceInstanceIdentifier = ConfigurationManager.AppSettings["portalServiceInstanceIdentifier"];
                        string portalServiceIntanceName = ConfigurationManager.AppSettings["portalServiceIntanceName"];
                        user.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute()
                        {
                            action = bosOrder.isExistingCustomer() ? DataActionEnum.none : DataActionEnum.add,
                            Name = "PORTALSERVICEINSTANCEIDENTIFIER",
                            Value = portalServiceInstanceIdentifier
                        });
                        user.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute()
                        {
                            action = bosOrder.isExistingCustomer() ? DataActionEnum.none : DataActionEnum.add,
                            Name = "PORTALSERVICEINSTANCENAME",
                            Value = portalServiceIntanceName
                        });

                        BT.SaaS.Core.Shared.Entities.Contact saasUserContact = findSaaSContact(saasOrder, userFromEmailAddress(contact.UserId));
                        if (saasUserContact == null)
                        {
                            throw new MappingException("Could not find a saas contact matching user " + contact.UserId + "when mapping user");
                        }
                        user.ContactXmlRef = saasUserContact.XmlIdKey;

                        userNumber++;
                        users.Add(user);
                    }
                }
            }
            return users;
        }

        static private BT.SaaS.Core.Shared.Entities.Contact findSaaSContact(BT.SaaS.Core.Shared.Entities.Order saasOrder, string contactKey)
        {
            foreach (BT.SaaS.Core.Shared.Entities.Contact contact in saasOrder.Header.Customer.Contacts)
            {
                if (contact.ContactKey.Equals(contactKey))
                    return contact;
            }
            return null;
        }

        static private bool isPrimaryUser(BT.SaaS.IspssAdapter.BOS_API.Contact contact)
        {
            bool result = false;

            if ((contact.UserType != null) && contact.UserType.ToUpper(CultureInfo.InvariantCulture).Equals("P"))
                result = true;

            return result;
        }

        static private BT.SaaS.Core.Shared.Entities.IdentityCredential mapIdentityCredential(int identityNumber, string userId, bool alias, string password, string secretQuestion, string secretAnswer)
        {
            BT.SaaS.Core.Shared.Entities.IdentityCredential id = new BT.SaaS.Core.Shared.Entities.IdentityCredential();

            id.XmlIdKey = "ID-" + identityNumber.ToString(CultureInfo.InvariantCulture);

            id.Identity = new BT.SaaS.Core.Shared.Entities.ClientIdentity();
            id.Identity.IdentityAlias = alias ? IdentifierAliasEnum.yes : IdentifierAliasEnum.no;
            id.Identity.identifier = userId;
            string userIdentityNamespace = ConfigurationManager.AppSettings["userIdentityNamespace"];
            id.Identity.identiferNamepace = userIdentityNamespace;

            if (!String.IsNullOrEmpty(password))
            {
                id.Credential = new BT.SaaS.Core.Shared.Entities.ClientCredential();

                if (ConfigurationManager.AppSettings["IsAESEncryptionEnabled"] != null
                              && Convert.ToBoolean(ConfigurationManager.AppSettings["IsAESEncryptionEnabled"]))
                {
                    id.Credential.Credential = BT.SaaS.Core.Shared.Utils.DataHelper.DecryptFromAES(password);
                }
                else
                {
                    id.Credential.Credential = BlowfishEncryptor.Decrypt(password);
                }
                id.Credential.CredentialType = "ENCRYPTEDPASSWORD";


                if (!String.IsNullOrEmpty(secretAnswer))
                {
                    if (ConfigurationManager.AppSettings["IsAESEncryptionEnabled"] != null
                              && Convert.ToBoolean(ConfigurationManager.AppSettings["IsAESEncryptionEnabled"]))
                    {
                        id.Credential.SecretAnswer = BT.SaaS.Core.Shared.Utils.DataHelper.DecryptFromAES(secretAnswer);
                    }
                    else
                    {
                        id.Credential.SecretAnswer = BlowfishEncryptor.Decrypt(secretAnswer);
                    }
                }
                else
                {
                    id.Credential.SecretAnswer = null;
                }

                if (!String.IsNullOrEmpty(secretQuestion))
                {
                    if (ConfigurationManager.AppSettings["IsAESEncryptionEnabled"] != null
                              && Convert.ToBoolean(ConfigurationManager.AppSettings["IsAESEncryptionEnabled"]))
                    {
                        id.Credential.SecretQuestion = BT.SaaS.Core.Shared.Utils.DataHelper.DecryptFromAES(secretQuestion);
                    }
                    else
                    {
                        id.Credential.SecretQuestion = BlowfishEncryptor.Decrypt(secretQuestion);
                    }
                }
                else
                {
                    id.Credential.SecretQuestion = null;
                }
            }
            else
            {
                // TEMPORARY FUDGE for Java/ .Net serialisation problem
                id.Credential = new BT.SaaS.Core.Shared.Entities.ClientCredential();
            }

            // THIS NEEDS REMOVED - TEMPORARY FUDGE FOR PE
            id.NewCredential = new BT.SaaS.Core.Shared.Entities.ClientCredential();

            return id;
        }

        static private string getPreferredEmailAddress(BT.SaaS.IspssAdapter.BOS_API.Contact contact)
        {
            if (contact.EMailContact != null)
            {
                foreach (EMailContact emailContact in contact.EMailContact)
                {
                    if (emailContact.PreferredContact != null &&
                        emailContact.PreferredContact.ToUpper(CultureInfo.InvariantCulture).Equals("Y"))
                    {
                        return value(emailContact.EMailAddress);
                    }
                }
            }
            return null;
        }

        static private List<SaaS.Core.Shared.Entities.BillingAccount> mapBillingAccounts(placeOrder bosOrder)
        {
            List<BT.SaaS.Core.Shared.Entities.BillingAccount> billingAccounts = null;
            if (bosOrder.isExistingCustomer())
            {
                string customerKey = bosOrder.getCustomerKey();

                Logger.Debug("Going to call CRM. Customer Key : " + customerKey);
                getCustomerResponse1 crmCustomerResponse = CRMAdapterWrapper.getCustomerFromCRM(customerKey);

                if (crmCustomerResponse.getCustomerResponse.customer.billingAccounts == null)
                {
                    throw new MappingException("Existing customer with customerKey=" + customerKey + " has no billingAccounts");
                }

                BT.SaaS.IspssAdapter.CRM.BillingAccount crmBillingAccount = crmCustomerResponse.getCustomerResponse.customer.billingAccounts[0];
                Logger.Debug("Got a successful response from CRM");

                billingAccounts = new List<SaaS.Core.Shared.Entities.BillingAccount>();
                SaaS.Core.Shared.Entities.BillingAccount billingAccount = new SaaS.Core.Shared.Entities.BillingAccount();

                billingAccount.Action = DataActionEnum.none;
                billingAccount.BankAccountDetails = new BT.SaaS.Core.Shared.Entities.BankAccountDetails();
                billingAccount.BillingAccountKey = crmBillingAccount.billingAccountKey;
                billingAccount.BillingAccountType = crmBillingAccount.billingAccountType;
                billingAccount.PaymentCardDetails = null;
                billingAccount.PayPalDetails = null;
                billingAccount.Status = DataStatusEnum.done;
                billingAccount.XmlIdKey = "BA-0";

                // contact
                if (crmBillingAccount.billingContact != null)
                {
                    billingAccount.BillingContact = new BT.SaaS.Core.Shared.Entities.Contact();
                    billingAccount.BillingContact.Action = DataActionEnum.none;

                    // address
                    if (crmBillingAccount.billingContact.address != null)
                    {
                        billingAccount.BillingContact.Address = new BT.SaaS.Core.Shared.Entities.Address();
                        billingAccount.BillingContact.Address.UkAddress = new BT.SaaS.Core.Shared.Entities.UKAddress();
                        billingAccount.BillingContact.Address.IntlAddress = null;

                        billingAccount.BillingContact.Address.UkAddress.BuildingName = crmBillingAccount.billingContact.address.ukAddress.buildingName;
                        billingAccount.BillingContact.Address.UkAddress.BuildingNumber = crmBillingAccount.billingContact.address.ukAddress.buildingNumber;
                        billingAccount.BillingContact.Address.UkAddress.DepartmentName = crmBillingAccount.billingContact.address.ukAddress.departmentName;
                        billingAccount.BillingContact.Address.UkAddress.DependendentThoroughfareDescriptor = crmBillingAccount.billingContact.address.ukAddress.dependendentThoroughfareDescriptor;
                        billingAccount.BillingContact.Address.UkAddress.DependentLocality = crmBillingAccount.billingContact.address.ukAddress.dependentLocality;
                        billingAccount.BillingContact.Address.UkAddress.DependentThoroughfareName = crmBillingAccount.billingContact.address.ukAddress.dependentThoroughfareName;
                        billingAccount.BillingContact.Address.UkAddress.DoubleDependentLocality = crmBillingAccount.billingContact.address.ukAddress.doubleDependentLocality;
                        billingAccount.BillingContact.Address.UkAddress.OrganisationName = crmBillingAccount.billingContact.address.ukAddress.organisationName;
                        billingAccount.BillingContact.Address.UkAddress.POBox = crmBillingAccount.billingContact.address.ukAddress.POBox;
                        billingAccount.BillingContact.Address.UkAddress.Postcode = crmBillingAccount.billingContact.address.ukAddress.postcode;
                        billingAccount.BillingContact.Address.UkAddress.PostTown = crmBillingAccount.billingContact.address.ukAddress.postTown;
                        billingAccount.BillingContact.Address.UkAddress.SubBuildingName = crmBillingAccount.billingContact.address.ukAddress.subBuildingName;
                        billingAccount.BillingContact.Address.UkAddress.ThoroughfareDescriptor = crmBillingAccount.billingContact.address.ukAddress.thoroughfareDescriptor;
                        billingAccount.BillingContact.Address.UkAddress.ThoroughfareName = crmBillingAccount.billingContact.address.ukAddress.thoroughfareName;

                        billingAccount.BillingContact.ContactKey = crmBillingAccount.billingContact.contactKey;
                        billingAccount.BillingContact.ContactType = crmBillingAccount.billingContact.contactType.ToString();
                        Logger.Debug("Mapped Billing Contact Address" + billingAccount.BillingContact.Address.UkAddress.Postcode);
                        if (crmBillingAccount.billingContact.dateOfBirth != null)
                        {
                            DateTime dob;
                            if (!DateTime.TryParse(crmBillingAccount.billingContact.dateOfBirth, out dob))
                            {
                            }
                            billingAccount.BillingContact.dateOfBirth = dob;
                        }
                        else
                        {
                            billingAccount.BillingContact.dateOfBirth = new DateTime();
                        }

                        // billingAccount.BillingContact.EmailAddresses
                        if (crmBillingAccount.billingContact.emailAddresses != null)
                        {
                            billingAccount.BillingContact.EmailAddresses = new List<BT.SaaS.Core.Shared.Entities.EmailContact>();

                            foreach (BT.SaaS.IspssAdapter.CRM.EmailContact crmEmailContact in crmBillingAccount.billingContact.emailAddresses)
                            {
                                BT.SaaS.Core.Shared.Entities.EmailContact emailContact = new BT.SaaS.Core.Shared.Entities.EmailContact();

                                emailContact.EmailAddress = crmEmailContact.emailAddress;
                                emailContact.IsPreferred = crmEmailContact.isPreferred;
                                emailContact.SuppressAll3rdPartyEmails = crmEmailContact.suppressAll3rdPartyEmails;
                                emailContact.SuppressAllBTEmails = crmEmailContact.suppressAllBTEmails;
                                emailContact.SuppressAllVendorEmails = crmEmailContact.suppressAllVendorEmails;
                                emailContact.Type = crmEmailContact.type;

                                billingAccount.BillingContact.EmailAddresses.Add(emailContact);
                            }
                        }

                        billingAccount.BillingContact.FirstName = crmBillingAccount.billingContact.firstName;
                        billingAccount.BillingContact.Gender = crmBillingAccount.billingContact.gender;
                        billingAccount.BillingContact.Honours = crmBillingAccount.billingContact.honours;
                        billingAccount.BillingContact.JobTitle = crmBillingAccount.billingContact.jobTitle;
                        billingAccount.BillingContact.LastName = crmBillingAccount.billingContact.lastName;
                        billingAccount.BillingContact.MiddleName = crmBillingAccount.billingContact.middleName;
                        billingAccount.BillingContact.PreferredName = crmBillingAccount.billingContact.preferredName;
                        billingAccount.BillingContact.Status = DataStatusEnum.done;

                        // billingAccount.BillingContact.TelephoneNumbers;
                        if (crmBillingAccount.billingContact.telephoneNumbers != null)
                        {
                            billingAccount.BillingContact.TelephoneNumbers = new List<BT.SaaS.Core.Shared.Entities.TelephoneContact>();

                            foreach (BT.SaaS.IspssAdapter.CRM.TelephoneContact crmTelephoneContact in crmBillingAccount.billingContact.telephoneNumbers)
                            {
                                BT.SaaS.Core.Shared.Entities.TelephoneContact telephoneContact = new BT.SaaS.Core.Shared.Entities.TelephoneContact();

                                telephoneContact.Extension = crmTelephoneContact.extension;
                                telephoneContact.IsPreferred = crmTelephoneContact.isPreferred;
                                telephoneContact.Number = crmTelephoneContact.number;
                                telephoneContact.Type = crmTelephoneContact.type;

                                billingAccount.BillingContact.TelephoneNumbers.Add(telephoneContact);
                            }
                        }

                        billingAccount.BillingContact.Title = crmBillingAccount.billingContact.title;
                        billingAccount.BillingContact.XmlIdKey = "BCO-01";
                    }
                    else
                    {
                        billingAccount.BillingContact.Address = null;
                    }

                }
                else
                {
                    billingAccount.BillingContact = null;
                }

                // address
                billingAccount.BillingAddress = new BT.SaaS.Core.Shared.Entities.Address();
                billingAccount.BillingAddress.UkAddress = new BT.SaaS.Core.Shared.Entities.UKAddress();
                billingAccount.BillingAddress.IntlAddress = null;

                // Need to change this logic
                // billingAccount.BillingAddress.UkAddress = billingAccount.BillingContact.Address.UkAddress;
                // Logger.Debug("Mapped Billing Address" + billingAccount.BillingAddress.UkAddress.BuildingNumber + billingAccount.BillingAddress.UkAddress.Postcode);
                if (crmBillingAccount.billingAddress != null)
                {
                    billingAccount.BillingAddress.UkAddress.BuildingName = crmBillingAccount.billingAddress.ukAddress.buildingName;
                    billingAccount.BillingAddress.UkAddress.BuildingNumber = crmBillingAccount.billingAddress.ukAddress.buildingNumber;
                    billingAccount.BillingAddress.UkAddress.DepartmentName = crmBillingAccount.billingAddress.ukAddress.departmentName;
                    billingAccount.BillingAddress.UkAddress.DependendentThoroughfareDescriptor = crmBillingAccount.billingAddress.ukAddress.dependendentThoroughfareDescriptor;
                    billingAccount.BillingAddress.UkAddress.DependentLocality = crmBillingAccount.billingAddress.ukAddress.dependentLocality;
                    billingAccount.BillingAddress.UkAddress.DependentThoroughfareName = crmBillingAccount.billingAddress.ukAddress.dependentThoroughfareName;
                    billingAccount.BillingAddress.UkAddress.DoubleDependentLocality = crmBillingAccount.billingAddress.ukAddress.doubleDependentLocality;
                    billingAccount.BillingAddress.UkAddress.OrganisationName = crmBillingAccount.billingAddress.ukAddress.organisationName;
                    billingAccount.BillingAddress.UkAddress.POBox = crmBillingAccount.billingAddress.ukAddress.POBox;
                    billingAccount.BillingAddress.UkAddress.Postcode = crmBillingAccount.billingAddress.ukAddress.postcode;
                    billingAccount.BillingAddress.UkAddress.PostTown = crmBillingAccount.billingAddress.ukAddress.postTown;
                    billingAccount.BillingAddress.UkAddress.SubBuildingName = crmBillingAccount.billingAddress.ukAddress.subBuildingName;
                    billingAccount.BillingAddress.UkAddress.ThoroughfareDescriptor = crmBillingAccount.billingAddress.ukAddress.thoroughfareDescriptor;
                    billingAccount.BillingAddress.UkAddress.ThoroughfareName = crmBillingAccount.billingAddress.ukAddress.thoroughfareName;
                }

                billingAccounts.Add(billingAccount);
            }
            else
            {
                if (bosOrder.AccountAndOrder.Order.ListOfBillAccount != null && bosOrder.AccountAndOrder.Order.ListOfBillAccount.Length > 0)
                {
                    billingAccounts = new List<SaaS.Core.Shared.Entities.BillingAccount>();
                    SaaS.Core.Shared.Entities.BillingAccount billingAccount = new SaaS.Core.Shared.Entities.BillingAccount();
                    billingAccount.XmlIdKey = "BA-0";
                    billingAccount.Action = bosOrder.isExistingCustomer() ? DataActionEnum.none : DataActionEnum.add;
                    billingAccount.Status = DataStatusEnum.notDone;
                    billingAccount.BillingAccountKey = value(bosOrder.AccountAndOrder.Order.ListOfBillAccount[0].BESBillAccountId);
                    billingAccount.BillingAccountType = "CSS";
                    if (bosOrder.AccountAndOrder.CSSCustAccData != null &&
                        bosOrder.AccountAndOrder.CSSCustAccData.InstallationDetails != null)
                    {
                        if (bosOrder.AccountAndOrder.CSSCustAccData.InstallationDetails.Item.GetType().Equals(typeof(BT.SaaS.IspssAdapter.BOS_API.Person)))
                        {
                            BT.SaaS.IspssAdapter.BOS_API.Person bosPerson = (BT.SaaS.IspssAdapter.BOS_API.Person)bosOrder.AccountAndOrder.CSSCustAccData.InstallationDetails.Item;

                            BT.SaaS.Core.Shared.Entities.Contact billingContact = new BT.SaaS.Core.Shared.Entities.Contact();
                            billingAccount.BillingContact = billingContact;

                            billingContact.Action = bosOrder.isExistingCustomer() ? DataActionEnum.none : DataActionEnum.add;
                            billingContact.Address = null;
                            billingContact.ContactKey = null;
                            billingContact.ContactType = "Billing";
                            billingContact.dateOfBirth = new DateTime();
                            billingContact.EmailAddresses = null;
                            billingContact.Gender = null;

                            billingContact.JobTitle = value(bosPerson.JobTitle);
                            billingContact.Title = value(bosPerson.Title);
                            billingContact.FirstName = value(bosPerson.Forename);
                            billingContact.MiddleName = value(bosPerson.Initial);
                            billingContact.LastName = value(bosPerson.Surname);
                            billingContact.Honours = value(bosPerson.Honours);
                            billingContact.PreferredName = null;

                            billingContact.Status = DataStatusEnum.notDone;
                            billingContact.TelephoneNumbers = null;
                            billingContact.XmlIdKey = "BCO-01";

                            if (bosOrder.AccountAndOrder.CSSCustAccData.InstallationDetails.Address != null)
                            {
                                billingAccount.BillingAddress = mapAddress(bosOrder.AccountAndOrder.CSSCustAccData.InstallationDetails.Address);
                            }
                            else
                            {
                                billingAccount.BillingAddress = null;
                            }
                        }
                        else
                        {
                            billingAccount.BillingContact = null;
                        }
                    }

                    // Normally we wouldn't send down an empty object but the PE can't cope with xs:choice very well and seems 
                    // to need an empty bankAccountDetails. We'll map it if we can otherwise it will be empty
                    BT.SaaS.Core.Shared.Entities.BankAccountDetails bankAccountDetails = new BT.SaaS.Core.Shared.Entities.BankAccountDetails();
                    if (bosOrder.AccountAndOrder.CSSCustAccData != null)
                    {
                        DirectDebitDetails bosDirectDebitDetails = bosOrder.AccountAndOrder.CSSCustAccData.DirectDebitDetails;
                        if (bosDirectDebitDetails != null)
                        {
                            bankAccountDetails.AccountHolderName = value(bosDirectDebitDetails.BankAccountName);
                            bankAccountDetails.AccountNumber = value(bosDirectDebitDetails.BankAccountNumber);
                            bankAccountDetails.BankName = null;
                            bankAccountDetails.SortCode = value(bosDirectDebitDetails.BankSortCode);
                        }
                    }
                    billingAccount.BankAccountDetails = bankAccountDetails;
                    billingAccount.PaymentCardDetails = null;
                    billingAccount.PayPalDetails = null;

                    billingAccounts.Add(billingAccount);
                }
            }
            return billingAccounts;
        }

        private static BT.SaaS.IspssAdapter.BOS_API.Contact findBosContact(placeOrder placeOrder, string contactRef)
        {
            if (placeOrder.AccountAndOrder != null && placeOrder.AccountAndOrder.ISPAccountData != null &&
                placeOrder.AccountAndOrder.ISPAccountData.ListOfContact != null)
            {
                int numContacts = placeOrder.AccountAndOrder.ISPAccountData.ListOfContact.Length;
                for (int n = 0; n < numContacts; n++)
                {
                    if (placeOrder.AccountAndOrder.ISPAccountData.ListOfContact[n].ContactId == contactRef)
                    {
                        return (placeOrder.AccountAndOrder.ISPAccountData.ListOfContact[n]);
                    }
                }
            }
            return null;
        }

        static private string getSAASProductCode(BT.SaaS.IspssAdapter.BOS_API.OrderItem orderItem)
        {

            string productCode = null;
            string tierProductId = null;
            string tarrifId = null;
            //string bosConsolidateProductCode = null;
            string saasProductId = null;

            productCode = orderItem.ProductCode;
            if (productCode == null) productCode = "";

            if (orderItem.ListOfOrderItemAttribute != null)
            {
                int noOfAttributes = orderItem.ListOfOrderItemAttribute.Length;
                for (int j = 0; j < noOfAttributes; j++)
                {
                    string attributeName = orderItem.ListOfOrderItemAttribute[j].Name;
                    if (attributeName.ToUpper(CultureInfo.InvariantCulture).Equals("TIERPRODUCTID"))
                    {
                        tierProductId = orderItem.ListOfOrderItemAttribute[j].NewValue;
                        if (tierProductId == null) tierProductId = "";
                    }
                    if (attributeName.ToUpper(CultureInfo.InvariantCulture).Equals("TARIFFID"))
                    {
                        tarrifId = orderItem.ListOfOrderItemAttribute[j].NewValue;
                        if (tarrifId == null) tarrifId = "";
                    }
                }

                //Create the consolidate string
                saasProductId = productCode + "-" + tierProductId + "-" + tarrifId;
            }
            return saasProductId;
        }

        // In regrade orders the old product code is given by the S-code and old values for the TierProductId and TariffId attributes
        static private string getOldSAASProductCode(BT.SaaS.IspssAdapter.BOS_API.OrderItem orderItem)
        {

            string productCode = null;
            string tierProductId = null;
            string tarrifId = null;
            string saasProductId = null;

            productCode = orderItem.ProductCode;
            if (productCode == null) productCode = "";

            if (orderItem.ListOfOrderItemAttribute != null)
            {
                int noOfAttributes = orderItem.ListOfOrderItemAttribute.Length;
                for (int j = 0; j < noOfAttributes; j++)
                {
                    string attributeName = orderItem.ListOfOrderItemAttribute[j].Name;
                    //bool isBulkRegrade = CheckIsBulkRegradeOrder(orderItem);

                    if (attributeName.ToUpper(CultureInfo.InvariantCulture).Equals("TIERPRODUCTID"))
                    {
                        tierProductId = orderItem.ListOfOrderItemAttribute[j].OldValue;
                        bool isResign = getOrderType(orderItem);
                        if (isResign && (orderItem.ListOfOrderItemAttribute[j].NewValue != null))
                        {
                             tierProductId = orderItem.ListOfOrderItemAttribute[j].NewValue;
                        }                       
                        if(tierProductId == null)
                        {
                             tierProductId = "";
                        }
                    }

                   
                    if (attributeName.ToUpper(CultureInfo.InvariantCulture).Equals("TARIFFID"))
                    {
                        tarrifId = orderItem.ListOfOrderItemAttribute[j].OldValue;
                        
                        if (tarrifId == null) tarrifId = "";
                    }
                }

                //Create the consolidate string
                saasProductId = productCode + "-" + tierProductId + "-" + tarrifId;
            }
            return saasProductId;
        }

        private static string getSugarInstanceName(List<ProductOrderItem> orderItems, string companyName, string instanceAttributeName)
        {
            if (companyName == null)
            {
                throw new MappingException("getSugarInstance() null company name");
            }

            // First look up the S-code for Web hosting products in the config file
            string sugarSCode = ConfigurationManager.AppSettings["sugarSCode"];
            if (sugarSCode == null)
            {
                throw new MappingException("No definition for sugarSCode in Web.config");
            }

            List<string> existingInstanceNames = new List<string>();
            // Do we already have any other OrderItems in this order with a Sugar Instance name?
            foreach (ProductOrderItem orderItem in orderItems)
            {
                if (orderItem.Header.ProductCode.StartsWith(sugarSCode))
                {
                    // This check is important as not all Sugar product actually have attributes
                    if (orderItem.RoleInstances != null && orderItem.RoleInstances[0].Attributes != null)
                    {
                        foreach (BT.SaaS.Core.Shared.Entities.Attribute attribute in orderItem.RoleInstances[0].Attributes)
                        {
                            if (attribute.Name.Equals(instanceAttributeName))
                            {
                                existingInstanceNames.Add(attribute.Value);
                            }
                        }
                    }
                }
            }

            companyName = companyName.ToLower();

            StringBuilder newCompanyName = new StringBuilder();

            // just take alpha numeric characters
            foreach (char character in companyName.ToCharArray())
            {
                if ((character >= '0' && character <= '9') || (character >= 'a' && character <= 'z'))
                    newCompanyName.Append(character);
            }

            string instanceName = newCompanyName.ToString();

            // Find out how many times we should check for a free name against Sugar
            string sugarCounterString = ConfigurationManager.AppSettings["checkSugarInstanceNameCount"];
            int sugarCounter;
            if (!int.TryParse(sugarCounterString, out sugarCounter))
            {
                MappingException mE = new MappingException("sugarCounter '" + sugarCounterString + "' is not an integer");
                throw mE;
            }

            // Go through the loop checking for a free name
            try
            {
                for (int counter = 0; counter < sugarCounter; counter++)
                {
                    Logger.Debug("Looking up Sugar Instance name: " + instanceName);

                    // If its in our list of existing instance names in the XML skip on and try again
                    Boolean found = false;
                    foreach (string existingInstanceName in existingInstanceNames)
                    {
                        if (existingInstanceName.Equals(instanceName))
                        {
                            Logger.Debug("Sugar Instance Name of " + instanceName + " is used already in this order");
                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        // this doesn't count in our number of calls to Sugar
                        sugarCounter++;
                    }
                    else
                    {

                        if (!SugarWrapper.checkInstanceName(instanceName))
                        {
                            Logger.Debug("Found a free Sugar Instance Name of " + instanceName);
                            return instanceName;
                        }
                        Logger.Debug("Sugar Instance name: " + instanceName + " is not free in SugarCRM");
                    }
                    instanceName = newCompanyName.ToString() + Convert.ToString(counter);
                }
            }
            catch (SugarException se)
            {
                // On exception publish the error but carry on
                Logger.Publish(se);
            }

            // oh dear we didn't find a free name - let's make up a random one and carry on with that
            // it might still fail but that will be picked up by the ASGs

            //instanceName = "inst" + RandomString(5, true);

            Random randomNumber = new Random();
            instanceName = newCompanyName + randomNumber.Next(99999).ToString("#####");

            Logger.Debug("Returning generated instance name: " + instanceName);
            return instanceName;

        }

        private static string getWebHostingProducts(placeOrder bosOrder)
        {
            StringBuilder wHProducts = new StringBuilder("");

            // First look up the S-code for Web hosting products in the config file
            string webHostingSCode = ConfigurationManager.AppSettings["webHostingSCode"];
            if (webHostingSCode == null)
            {
                throw new MappingException("No definition for webHostingSCode in Web.config");
            }

            BT.SaaS.IspssAdapter.BOS_API.OrderItem ispControl = getIspControl(bosOrder);

            // Build a list of the productCodes of web hosting products in the BOS order
            List<string> webHostingProducts = new List<string>();
            foreach (BT.SaaS.IspssAdapter.BOS_API.OrderItem vasOrderItem in ispControl.OrderItem1)
            {
                if (vasOrderItem.ProductCode.Equals(webHostingSCode))
                {
                    string productCode = getSAASProductCode(vasOrderItem);
                    if (!webHostingProducts.Contains(productCode))
                    {
                        webHostingProducts.Add(getSAASProductCode(vasOrderItem));
                    }
                }
            }

            // Look up the product defs in MDM
            List<ProductDefinition> productDefinitions = MdmWrapper.getSaaSProductDefs(webHostingProducts, getResellerId(), bosOrder.IsORTCheckRequired);

            // Build the string of product names
            Boolean first = true;
            foreach (ProductDefinition productDefinition in productDefinitions)
            {
                if (!first) wHProducts.Append(" ,");
                wHProducts.Append(productDefinition.Name);
                first = false;
            }

            return wHProducts.ToString();
        }

        private static string getWebHostingAndExternalDomainProducts(placeOrder bosOrder)
        {
            StringBuilder wHProducts = new StringBuilder("");

            // First look up the S-code for Web hosting products in the config file
            string webHostingSCode = ConfigurationManager.AppSettings["webHostingSCode"];


            // look up the S-code for domain
            string externalDomainSCode = ConfigurationManager.AppSettings["domainProductSCode"];
            string licenseTypeForDomain = string.Empty;

            if (webHostingSCode == null)
            {
                throw new MappingException("No definition for webHostingSCode in Web.config");
            }

            if (externalDomainSCode == null)
            {
                throw new MappingException("No definition for externalDomainSCode in Web.config");
            }
            BT.SaaS.IspssAdapter.BOS_API.OrderItem ispControl = getIspControl(bosOrder);

            // Build a list of the productCodes of web hosting products in the BOS order
            List<string> webHostingProducts = new List<string>();
            foreach (BT.SaaS.IspssAdapter.BOS_API.OrderItem vasOrderItem in ispControl.OrderItem1)
            {
                if (vasOrderItem.ProductCode.Equals(webHostingSCode) && (!string.IsNullOrEmpty(vasOrderItem.Action.ToString())) && vasOrderItem.Action.ToString().Equals(BOS_ACTION_ADD, StringComparison.InvariantCultureIgnoreCase))
                {
                    string productCode = getSAASProductCode(vasOrderItem);

                    if (productCode.Contains(WEBHOSTING_SCODE))
                    {
                        //do not add web hosting product if there are multiple quantities of it
                        if (vasOrderItem.Quantity == AUTOACTIVATION_VALID_QUANTITY)
                            webHostingProducts.Add(getSAASProductCode(vasOrderItem));
                    }
                }
                if (vasOrderItem.ProductCode.Equals(externalDomainSCode) && (!string.IsNullOrEmpty(vasOrderItem.Action.ToString())) && vasOrderItem.Action.ToString().Equals(BOS_ACTION_ADD, StringComparison.InvariantCultureIgnoreCase))
                {
                    string productCode = getSAASProductCode(vasOrderItem);
                    licenseTypeForDomain = getValueFromAttributeForProduct(productCode, "TYPE", vasOrderItem);
                    domainNameForAutoActivation = getValueFromAttributeForProduct(productCode, "DOMAINNAME", vasOrderItem);
                }

            }

            //AutoActivation is valid only for smart hosting with one web hosting product and one external domain in BOS Xml

            if (string.Compare(licenseTypeForDomain, "external", true).Equals(0))
            {

                //capturing external domain type in the flag
                isExternalDomainForActivation = true;
                isExternalDomainForLicenseTypeActivation = true;
                if (webHostingProducts.Count == 1 && !isNewOrMigratedCustomer)
                {

                    isAutoActivation = true;
                }
            }

            return wHProducts.ToString();
        }

        private static string getWebConsultAndBuildOrderItemCount(placeOrder bosOrder)
        {
            int rstoOrderItems = 0;

            // First look up the S-code for RSTO products in the config file
            string webConsultAndBuildSCode = ConfigurationManager.AppSettings["webConsultAndBuildSCode"];
            if (webConsultAndBuildSCode == null)
            {
                throw new MappingException("No definition for webConsultAndBuildSCode in Web.config");
            }

            BT.SaaS.IspssAdapter.BOS_API.OrderItem ispControl = getIspControl(bosOrder);

            foreach (BT.SaaS.IspssAdapter.BOS_API.OrderItem vasOrderItem in ispControl.OrderItem1)
            {
                if (vasOrderItem.ProductCode.Equals(webConsultAndBuildSCode))
                {
                    rstoOrderItems++;
                }
            }

            return rstoOrderItems.ToString();
        }

        private static string getWebConsultAndBuildSalesPrice(placeOrder bosOrder)
        {
            float salesPrice = 0;

            // First look up the S-code for RSTO products in the config file
            string webConsultAndBuildSCode = ConfigurationManager.AppSettings["webConsultAndBuildSCode"];
            if (webConsultAndBuildSCode == null)
            {
                throw new MappingException("No definition for webConsultAndBuildSCode in Web.config");
            }

            BT.SaaS.IspssAdapter.BOS_API.OrderItem ispControl = getIspControl(bosOrder);

            foreach (BT.SaaS.IspssAdapter.BOS_API.OrderItem vasOrderItem in ispControl.OrderItem1)
            {
                if (vasOrderItem.ProductCode.Equals(webConsultAndBuildSCode))
                {
                    float orderItemPrice;
                    if (!float.TryParse(vasOrderItem.ListPrice, out orderItemPrice))
                    {
                        throw new MappingException("Failed to convert orderitem ListPrice to float value=" + vasOrderItem.ListPrice);
                    }
                    salesPrice += orderItemPrice;
                }
            }

            return salesPrice.ToString();

        }
        private static string getP1ProductProvisionStatus(placeOrder bosOrder)
        {
            string p1ProductCode = ConfigurationManager.AppSettings["P1PRODUCTCODE"];
            bool result = false;
            bool isP1ProductFound = false;
            if (bosOrder.isExistingCustomer())
            {
                string userId = string.Empty;
                List<ClientServiceInstance> clientSrvcInstances;
                userId = bosOrder.AccountAndOrder.ISPAccountData.ListOfContact.ToList().Find(u => u.UserType.ToUpper() == "P").UserId;
                clientSrvcInstances = DnpWrapper.GetClientServiceInstances(userId, "BTCOM").ToList();
                foreach (ClientServiceInstance CSI in clientSrvcInstances)
                {
                    if (CSI.clientServiceInstanceIdentifier.value.Equals("MOSI", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (ClientServiceRole CSR in CSI.clientServiceRole)
                        {
                            if (CSR.id.ToUpper() == "SERVICEMANAGE")
                            {
                                ClientServiceRoleCharacteristic licenseTypeChar = CSR.clientServiceRoleCharacteristic.Where(CSRC => CSRC.name.ToUpper() == "LICENSETYPE").SingleOrDefault();
                                if (licenseTypeChar.value.ToUpper() == "PREMIUM" || licenseTypeChar.value.ToUpper() == "PREMIUM2")
                                {
                                    isP1ProductFound = true;
                                }
                            }
                            if (isP1ProductFound)
                            {
                                p1ProductsCount = ++p1ProductsCount;
                            }
                        }
                    }
                }
            }
            if (p1ProductCode == null)
            {
                throw new MappingException("No definition for p1ProductSCode in Web.config");
            }
            BT.SaaS.IspssAdapter.BOS_API.OrderItem ispControl = getIspControl(bosOrder);
            foreach (BT.SaaS.IspssAdapter.BOS_API.OrderItem vasOrderItem in ispControl.OrderItem1)
            {
                if (vasOrderItem.ProductCode.Equals(p1ProductCode))
                {
                    result = (p1ProductsCount == 0) ? false : true;
                    isP1ProductFound = true;
                }
            }
            p1ProductsCount = (isP1ProductFound) ? ++p1ProductsCount : 0;
            return result.ToString();
        }

        /// <summary>
        /// Generates a random string with the given length
        /// </summary>
        /// <param name="size">Size of the string</param>
        /// <param name="lowerCase">If true, generate lowercase string</param>
        /// <returns>Random string</returns>
        static private string RandomString(int size, bool lowerCase)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            if (lowerCase)
                return builder.ToString().ToLower();
            return builder.ToString();
        }

        /// <summary>
        /// Create a new Order Item for mail storage Product.
        /// </summary>
        /// <param name="bosOrder">Order which came from the BOSS</param>
        /// <param name="saasOrder">SaaS order object</param>
        /// <returns>Returns the Order Item for Mail Storage Prouct.</returns>
        private static ProductOrderItem CreateMailStorageProductOrderItem(placeOrder bosOrder, BT.SaaS.Core.Shared.Entities.Order saasOrder)
        {
            // Creates a new order item for mail storage for HE & WH products as per the configuration in LicenseStorageProducts.
            string licenseStorageProducts = ConfigurationManager.AppSettings["LicenseStorageProducts"];
            string quantity;
            int count = 0;
            List<ProductOrderItem> storageProducts = null;
            ProductOrderItem saasOrderItem = new ProductOrderItem();

            // check if there are any License Storage Products are defined.
            if (!String.IsNullOrEmpty(licenseStorageProducts))
            {
                storageProducts = (from poi in saasOrder.ProductOrderItems
                                   where poi.Header != null && poi.Header.ProductCode != null && !String.IsNullOrEmpty(poi.Header.Quantity)
                                         && licenseStorageProducts.Contains(poi.Header.ProductCode) == true
                                         && ConfigurationManager.AppSettings["Quantity_" + poi.Header.ProductCode] != null
                                   select poi).ToList<ProductOrderItem>();
            }


            if (storageProducts != null && storageProducts.Count > 0)
            {
                string mailStorage = ConfigurationManager.AppSettings["MailStorage"];
                bool isExistingCustomer = bosOrder.isExistingCustomer();
                string productInstanceKey = string.Empty;
                string orderItemKey = "dummyOrderItemkey";

                if (String.IsNullOrEmpty(mailStorage))
                {
                    throw new MappingException("No definition for Mail Storage in Web.config");
                }

                // Get the Product defination for the mail storage.
                List<ProductDefinition> productDefination = MdmWrapper.getSaaSProductDefs(new List<string> { mailStorage }, getResellerId(), bosOrder.IsORTCheckRequired);
                ProductDefinition mailStorageProduct = productDefination[0];

                // if the customer is new get the order details from Business Essentials product.
                if (!isExistingCustomer)
                {
                    string BusinessEssentials = ConfigurationManager.AppSettings["BusinessEssentials"];
                    if (String.IsNullOrEmpty(BusinessEssentials))
                    {
                        throw new MappingException("No definition for Business Essentials in Web.config");
                    }

                    ProductOrderItem BEOrderItem = saasOrder.ProductOrderItems.Find(p => p.Header.ProductCode == BusinessEssentials);
                    if (BEOrderItem != null && BEOrderItem.Header != null)
                    {
                        if (!String.IsNullOrEmpty(BEOrderItem.Header.ProductInstanceKey))
                        {
                            productInstanceKey = BEOrderItem.Header.ProductInstanceKey + "-MailStorage";
                        }

                        if (!String.IsNullOrEmpty(BEOrderItem.Header.OrderItemKey))
                        {
                            orderItemKey = BEOrderItem.Header.OrderItemKey;
                        }
                    }

                    count = saasOrder.ProductOrderItems.Count;
                }
                else
                {
                    // Get product instances with this product code and customer key
                    List<BT.SaaS.Core.MDMAPI.Entities.Customer.ProductInstance> productInstances = MdmWrapper.GetProductInstancesByProductId(mailStorageProduct.ProductDefinitionId, saasOrder.Header.Customer.CustomerKey);

                    if (productInstances != null && productInstances.Count > 0)
                    {
                        productInstanceKey = productInstances[0].ProductInstanceKey;
                    }
                }

                quantity = storageProducts.Sum(x => int.Parse(x.Header.Quantity) * int.Parse(ConfigurationManager.AppSettings["Quantity_" + x.Header.ProductCode])).ToString();

                saasOrderItem.XmlIdKey = "OI-" + count;
                saasOrderItem.Header.Status = DataStatusEnum.notDone;
                saasOrderItem.Header.OrderItemKey = orderItemKey;
                saasOrderItem.Header.OrderItemId = null;
                saasOrderItem.Header.ProductCode = mailStorageProduct.Code;
                saasOrderItem.Header.ProductName = mailStorageProduct.Name;
                saasOrderItem.Header.ProductInstanceKey = productInstanceKey;
                saasOrderItem.Header.ProductInstanceId = null;
                saasOrderItem.Header.Quantity = quantity;

                // There is only ever one billingAccount in a BOS Order so link to it
                saasOrderItem.Header.BillingAccountXmlRef = "BA-0";


                if (mailStorageProduct.ProductDefinitionRoleList != null)
                {
                    // Create the Primary User Role Instance
                    BT.SaaS.IspssAdapter.BOS_API.Contact bosPrimaryUser = getPrimaryBosUser(bosOrder);
                    User saasUser = findSaaSUser(saasOrder, bosPrimaryUser);

                    // Get the manage User Role definition
                    string manageRoleType = ConfigurationManager.AppSettings["manageUserRoleType"];

                    ProductDefinitionRole manageRoleDefinition =
                        mailStorageProduct.ProductDefinitionRoleList.FirstOrDefault
                            (p => p.ProductRoleName.Equals(manageRoleType,
                             StringComparison.InvariantCultureIgnoreCase));

                    if (manageRoleDefinition == null)
                    {
                        throw new MappingException("No manage role in this product definition. SaaS Product code" + mailStorageProduct.Code);
                    }

                    BT.SaaS.Core.Shared.Entities.RoleInstance manageRoleInstance = MapRoleInstanceForMailStorage(saasUser, bosPrimaryUser, manageRoleDefinition.ProductRoleName, storageProducts[0].RoleInstances[0].Action, isExistingCustomer, quantity, count, 1);
                    saasOrderItem.RoleInstances.Add(manageRoleInstance);

                }

                saasOrderItem.SequencedServiceActions = null;
                saasOrderItem.ServiceInstances = null;
            }
            else
            {
                saasOrderItem = null;
            }

            return saasOrderItem;
        }

        /// <summary>
        /// Method to create a role instance for a mails storage order item.
        /// </summary>
        /// <param name="saasUser">saas user</param>
        /// <param name="bosPrimaryUser">bos primary user</param>
        /// <param name="RoleName">name of role</param>
        /// <param name="Action">action</param>
        /// <param name="isExistingCustomer">Holds new or existing customer.</param>
        /// <param name="quantity">number of the quantity</param>
        /// <param name="orderItemIndex">indix for role identity</param>
        /// <param name="roleInstanceCount">index for role identity</param>
        /// <returns></returns>
        private static BT.SaaS.Core.Shared.Entities.RoleInstance MapRoleInstanceForMailStorage(User saasUser, BT.SaaS.IspssAdapter.BOS_API.Contact bosPrimaryUser, string RoleName, RoleInstanceActionEnum Action, bool isExistingCustomer, string quantity, int orderItemIndex, int roleInstanceCount)
        {

            BT.SaaS.Core.Shared.Entities.RoleInstance roleInstance = new BT.SaaS.Core.Shared.Entities.RoleInstance()
            {
                XmlIdKey = "RI-" + orderItemIndex + "-" + roleInstanceCount,
                Action = Action,
                Status = DataStatusEnum.notDone,
                InternalProvisioningAction = (isExistingCustomer == false) ? ProvisioningActionEnum.provide : ProvisioningActionEnum.modifyService,
                QuantityToAddRemove = !String.IsNullOrEmpty(quantity) ? Int32.Parse(quantity) : 0,
                RoleType = RoleName,
                UserXmlRef = saasUser.XmlIdKey,
                Attributes = null,
                UserIdentityCredentialXmlRef = null,
                LicenceKey = null,
                LicenceExpiry = new DateTime(),
                ReceivingProductInstanceId = null
            };


            // Which identity should we map the roleInstance to? If there is a preferred email use it
            if (saasUser.IdentityCredential.Count > 1)
            {
                // If there are multiple identities need to check the 2nd one is a preferred email 
                // and not a mailboxAlias
                string preferredEmail = getPreferredEmailAddress(bosPrimaryUser);
                if (saasUser.IdentityCredential[1].Identity.identifier.Equals(preferredEmail))
                {
                    roleInstance.UserIdentityCredentialXmlRef = saasUser.IdentityCredential[1].XmlIdKey;
                }
                else
                    roleInstance.UserIdentityCredentialXmlRef = saasUser.IdentityCredential[0].XmlIdKey;
            }
            else
            {
                roleInstance.UserIdentityCredentialXmlRef = saasUser.IdentityCredential[0].XmlIdKey;
            }

            return roleInstance;
        }

        /// <summary>
        /// Create a new Order Item for domain product.
        /// </summary>
        /// <param name="bosOrder">Order which came from the BOSS</param>
        /// <param name="saasOrder">SaaS order object</param>
        /// <returns>Returns the Order Item for domain Prouct.</returns>
        private static List<ProductOrderItem> CreateDomainProductOrderItem(placeOrder bosOrder, BT.SaaS.Core.Shared.Entities.Order saasOrder, List<OrderItemProductMap> orderItemProductMapping, List<string> existingProductInstanceKeys)
        {
            // get the list of Products defined in the config to add Domain Order Item
            string productsList = ConfigurationManager.AppSettings["ProductsToIncludeFreeDomain"];
            List<ProductOrderItem> domainSaaSOrderItems = null;

            // check if there are any domain Products are defined in the cofing.
            if (!String.IsNullOrEmpty(productsList))
            {
                List<ProductOrderItem> productsForDomainOrderItem = null;
                int ordersCount = saasOrder.ProductOrderItems.Count;

                productsForDomainOrderItem = (from poi in saasOrder.ProductOrderItems
                                              where poi.Header != null && poi.Header.ProductCode != null
                                              && productsList.Contains(poi.Header.ProductCode) == true
                                              && ConfigurationManager.AppSettings["Domains_" + poi.Header.ProductCode] != null
                                              && Int32.Parse(ConfigurationManager.AppSettings["Domains_" + poi.Header.ProductCode]) > 0
                                              select poi).ToList<ProductOrderItem>();

                if (productsForDomainOrderItem != null && productsForDomainOrderItem.Count > 0)
                {
                    string freeDomainProductCode = ConfigurationManager.AppSettings["FreeDomainProductCode"];

                    if (String.IsNullOrEmpty(freeDomainProductCode))
                    {
                        throw new MappingException("No product code defined for free doamin product in Web.config");
                    }

                    List<ProductOrderItem> freeDomainOrderitems = saasOrder.ProductOrderItems.FindAll(poi => poi.Header != null && poi.Header.ProductCode != null && poi.Header.ProductCode == freeDomainProductCode);

                    freeDomainOrderitems = (from oi in freeDomainOrderitems
                                            from ri in oi.RoleInstances
                                            where ri.RoleType.ToUpper() == "ADMIN"
                                            from a in ri.Attributes
                                            where ri.Attributes != null
                                            where a.Name.ToUpper() == "TYPE" && (a.Value.ToUpper() == "NEW" || a.Value.ToUpper() == "TRANSFER-IN")
                                            select oi).ToList<ProductOrderItem>();


                    int listOfFreeDomainsInSaaSOrder = freeDomainOrderitems.Count;
                    int totalDomainOrderItemsTobeCreatedForOrdersInBOS = productsForDomainOrderItem.Sum(poi => Int32.Parse(ConfigurationManager.AppSettings["Domains_" + poi.Header.ProductCode]));
                    int totalDomainOrderItemsTobeCreated = totalDomainOrderItemsTobeCreatedForOrdersInBOS - listOfFreeDomainsInSaaSOrder;
                    int keyCount = 0;

                    foreach (ProductOrderItem freeDomain in freeDomainOrderitems)
                    {
                        foreach (ProductOrderItem orderItem in productsForDomainOrderItem)
                        {
                            int freeDomainsCount = Int32.Parse(ConfigurationManager.AppSettings["Domains_" + orderItem.Header.ProductCode]);
                            for (int i = 0; i < freeDomainsCount; i++)
                            {
                                keyCount = saasOrder.ProductOrderItems.FindAll(poi => poi.Header.ProductCode == freeDomain.Header.ProductCode && string.IsNullOrEmpty(poi.Header.ParentProductInstanceId)).Count;

                                if (keyCount == 0)
                                {
                                    break;
                                }
                                saasOrder.ProductOrderItems.Find(poi => poi.Header.ProductCode == freeDomain.Header.ProductCode && string.IsNullOrEmpty(poi.Header.ParentProductInstanceId)).Header.ParentProductInstanceId = orderItem.Header.OrderItemKey;

                            }
                        }

                        foreach (BT.SaaS.Core.Shared.Entities.RoleInstance role in freeDomain.RoleInstances)
                        {
                            if (role.Attributes != null)
                            {
                                BT.SaaS.Core.Shared.Entities.Attribute DomainName = role.Attributes.Find(r => r.Name.ToUpper().Equals("DOMAINNAME", StringComparison.InvariantCultureIgnoreCase));

                                if (DomainName != null && !String.IsNullOrEmpty(DomainName.Value))
                                {
                                    BT.SaaS.Core.Shared.Entities.Attribute LicenseType = role.Attributes.Find(a => a.Name.ToUpper() == "LICENSETYPE");

                                    if (LicenseType != null)
                                    {
                                        if (!LicenseType.Value.Equals("TRANSFER-IN", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            bool isCCTLD = DomainName.Value.ToUpper().EndsWith("CO.UK", StringComparison.InvariantCultureIgnoreCase) || DomainName.Value.ToUpper().EndsWith("ORG.UK", StringComparison.InvariantCultureIgnoreCase);
                                            LicenseType.Value = isCCTLD ? "CCTLD" : "GTLD";
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (totalDomainOrderItemsTobeCreated > 0)
                    {
                        ProductOrderItem saasOrderItem;
                        bool isExistingCustomer = bosOrder.isExistingCustomer();
                        string productInstanceKey = string.Empty;

                        // Get the Product defination for free domain.
                        List<ProductDefinition> productDefination = MdmWrapper.getSaaSProductDefs(new List<string> { freeDomainProductCode }, getResellerId(), bosOrder.IsORTCheckRequired);
                        ProductDefinition domainProductDefination = productDefination[0];

                        string errorContext = "DummyDomain productCode: " + domainProductDefination.Code + " ";
                        int newcount = 0;

                        foreach (ProductOrderItem orderItem in productsForDomainOrderItem)
                        {

                            int freeDomainsCount = Int32.Parse(ConfigurationManager.AppSettings["Domains_" + orderItem.Header.ProductCode]);
                            int totalCount = 0;

                            if (freeDomainOrderitems != null & freeDomainOrderitems.Count > 0)
                            {
                                totalCount = freeDomainOrderitems.FindAll(poi => poi.Header.ParentProductInstanceId == orderItem.Header.OrderItemKey).Count;
                            }

                            totalCount = totalCount == 0 ? freeDomainsCount : totalCount;

                            for (int counter = 0; counter < totalCount; counter++)
                            {
                                saasOrderItem = new ProductOrderItem();
                                saasOrderItem.XmlIdKey = "OI-" + (ordersCount + newcount++);
                                saasOrderItem.Header.Status = DataStatusEnum.notDone;
                                saasOrderItem.Header.OrderItemKey = "PI-RCOM-Dummy-" + orderItem.Header.OrderItemKey + "-" + counter;
                                saasOrderItem.Header.OrderItemId = null;
                                saasOrderItem.Header.ProductCode = domainProductDefination.Code;
                                saasOrderItem.Header.ProductName = domainProductDefination.Name;
                                saasOrderItem.Header.Quantity = "1";
                                productInstanceKey = "PI-RCOM-Dummy-" + orderItem.Header.ProductInstanceKey + counter;

                                saasOrderItem.Header.ProductInstanceKey = productInstanceKey;
                                saasOrderItem.Header.ProductInstanceId = null;
                                saasOrderItem.Header.ParentProductInstanceId = orderItem.Header.OrderItemKey;

                                //setting flag for autoactivation
                                if (isAutoActivation)
                                    isCreateDomainAutoActivation = true;

                                // There is only ever one billingAccount in a BOS Order so link to it
                                saasOrderItem.Header.BillingAccountXmlRef = "BA-0";
                                OrderItemProductMap item = orderItemProductMapping.Find(p => p.SaasProductCode == orderItem.Header.ProductCode);
                                List<BT.SaaS.Core.Shared.Entities.RoleInstance> roles = null;

                                if (domainProductDefination.ProductDefinitionRoleList != null)
                                {
                                    int roleInstanceCount = 0;
                                    roles = new List<BT.SaaS.Core.Shared.Entities.RoleInstance>();

                                    // Get the Primary User Role definition
                                    string primaryUserRoleType = ConfigurationManager.AppSettings["primaryUserRoleType"];

                                    ProductDefinitionRole primaryUserRoleDefinition =
                                        domainProductDefination.ProductDefinitionRoleList.FirstOrDefault
                                            (p => p.ProductRoleName.Equals(primaryUserRoleType,
                                             StringComparison.InvariantCultureIgnoreCase));

                                    if (primaryUserRoleDefinition == null)
                                    {
                                        throw new MappingException("No primary role in this product definition. " + errorContext);
                                    }

                                    // Create the Primary User Role Instance
                                    BT.SaaS.IspssAdapter.BOS_API.Contact bosPrimaryUser = getPrimaryBosUser(bosOrder);
                                    BT.SaaS.Core.Shared.Entities.RoleInstance primaryRoleInstance = mapRoleInstance(saasOrder, bosOrder, item, counter, roleInstanceCount++, primaryUserRoleDefinition, bosPrimaryUser, false, domainSaaSOrderItems, errorContext);

                                    // Need to add one parameter called ignoreFlag so that C2B ignores the creation of PIK in C2B
                                    // Also this is to stop billing in CSS.
                                    BT.SaaS.Core.Shared.Entities.Attribute ignoreAttribute = new BT.SaaS.Core.Shared.Entities.Attribute();
                                    ignoreAttribute.action = DataActionEnum.add;
                                    ignoreAttribute.Name = "ignoreFlag";
                                    ignoreAttribute.Value = "Y";
                                    if (primaryRoleInstance.Attributes == null && primaryRoleInstance.Attributes.Count <= 0)
                                    {
                                        primaryRoleInstance.Attributes = new List<BT.SaaS.Core.Shared.Entities.Attribute>();
                                    }

                                    primaryRoleInstance.Attributes.Add(ignoreAttribute);

                                    roles.Add(primaryRoleInstance);


                                    // Get the Manage User Role definition
                                    string manageRoleType = ConfigurationManager.AppSettings["manageUserRoleType"];

                                    ProductDefinitionRole manageRoleDefinition =
                                        domainProductDefination.ProductDefinitionRoleList.FirstOrDefault
                                            (p => p.ProductRoleName.Equals(manageRoleType,
                                             StringComparison.InvariantCultureIgnoreCase));

                                    if (manageRoleDefinition == null)
                                    {
                                        throw new MappingException("No manage role in this product definition. " + errorContext);
                                    }

                                    BT.SaaS.Core.Shared.Entities.RoleInstance manageRoleInstance = mapRoleInstance(saasOrder, bosOrder, item, counter, roleInstanceCount++, manageRoleDefinition, bosPrimaryUser, false, domainSaaSOrderItems, errorContext);
                                    roles.Add(manageRoleInstance);
                                }

                                string type = "CCTLD";

                                ProductOrderItem freeDomainOrderWithLicenceType = freeDomainOrderitems.Find(r => r.Header.ParentProductInstanceId == orderItem.Header.OrderItemKey);

                                if (freeDomainOrderWithLicenceType == null)
                                {
                                    if (domainSaaSOrderItems != null && domainSaaSOrderItems.Count > 0)
                                    {
                                        freeDomainOrderWithLicenceType = domainSaaSOrderItems.Find(r => r.Header.ParentProductInstanceId == orderItem.Header.OrderItemKey);
                                    }
                                }

                                if (freeDomainOrderWithLicenceType != null)
                                {
                                    BT.SaaS.Core.Shared.Entities.Attribute LicenseTypeInFreeDomain = freeDomainOrderWithLicenceType.RoleInstances.Find(r =>
                                        r.RoleType.Equals("Admin", StringComparison.InvariantCultureIgnoreCase)).Attributes.Find(a =>
                                            a.Name.Equals("LicenseType", StringComparison.InvariantCultureIgnoreCase));

                                    if (LicenseTypeInFreeDomain != null)
                                    {
                                        if (LicenseTypeInFreeDomain.Value.Equals("TRANSFER-IN", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            BT.SaaS.Core.Shared.Entities.Attribute TransferInDomain = freeDomainOrderWithLicenceType.RoleInstances.Find(r =>
                                                    r.RoleType.Equals("Admin", StringComparison.InvariantCultureIgnoreCase)).Attributes.Find(a =>
                                                    a.Name.Equals("DOMAINNAME", StringComparison.InvariantCultureIgnoreCase));

                                            bool isCCTLD = TransferInDomain.Value.ToUpper().EndsWith("CO.UK", StringComparison.InvariantCultureIgnoreCase)
                                                || TransferInDomain.Value.ToUpper().EndsWith("ORG.UK", StringComparison.InvariantCultureIgnoreCase);
                                            type = isCCTLD ? "GTLD" : "CCTLD";
                                        }
                                        else
                                        {
                                            type = LicenseTypeInFreeDomain.Value == "CCTLD" ? "GTLD" : "CCTLD";
                                        }
                                    }
                                }

                                BT.SaaS.Core.Shared.Entities.Attribute LicenseType;
                                BT.SaaS.Core.Shared.Entities.Attribute Type;
                                LicenseType = new BT.SaaS.Core.Shared.Entities.Attribute();
                                LicenseType.Name = "LicenseType";
                                LicenseType.Value = type;
                                Type = new BT.SaaS.Core.Shared.Entities.Attribute();
                                Type.Name = "Type";
                                Type.Value = type;


                                foreach (BT.SaaS.Core.Shared.Entities.RoleInstance role in roles)
                                {
                                    if (role.Attributes != null)
                                    {
                                        role.Attributes.Add(LicenseType);
                                        role.Attributes.Add(Type);
                                    }
                                }

                                saasOrderItem.RoleInstances = roles;
                                saasOrderItem.SequencedServiceActions = null;
                                saasOrderItem.ServiceInstances = null;

                                if (domainSaaSOrderItems == null)
                                {
                                    domainSaaSOrderItems = new List<ProductOrderItem>();
                                }

                                domainSaaSOrderItems.Add(saasOrderItem);
                            }

                        }
                    }

                }
            }


            return domainSaaSOrderItems;
        }

        internal static ProductOrderItem CreateDomainProductOrderItemForRegrade(BT.SaaS.Core.Shared.Entities.Order saasOrder)
        {
            long whProductInstanceId = 0;
            ProductInstance[] custProductInstnace = null;

            try
            {

                ProductOrderItem WHPOI = null;
                // check for the WH product in the order
                foreach (ProductOrderItem POI in saasOrder.ProductOrderItems)
                {
                    if (POI.Header.ProductCode.StartsWith(ConfigurationManager.AppSettings["webHostingSCode"]))
                    {
                        WHPOI = POI;
                    }
                }

                // Get all Product Instances for the customer.
                custProductInstnace = MdmWrapper.getCustomerProductInstances(saasOrder.Header.Customer.CustomerKey);

                foreach (ProductInstance PI in custProductInstnace)
                {
                    if (PI.ProductInstanceKey == WHPOI.Header.ProductInstanceKey)
                    {
                        whProductInstanceId = PI.ProductInstanceId;
                    }
                }


                ProductOrderItem domainPOI = new ProductOrderItem();

                domainPOI.XmlIdKey = "OI-1";
                domainPOI.Header.Status = DataStatusEnum.notDone;
                domainPOI.Header.OrderItemKey = "PI-RCOM-Dummy-1";
                domainPOI.Header.OrderItemId = null;
                domainPOI.Header.ProductCode = "S0001331-PR0000000004008-";
                domainPOI.Header.ProductName = "New Domain_Free";
                domainPOI.Header.Quantity = "1";

                domainPOI.Header.ProductInstanceKey = "PI-RCOM-Dummy-" + WHPOI.Header.ProductInstanceKey;
                domainPOI.Header.ProductInstanceId = null;
                domainPOI.Header.ParentProductInstanceId = whProductInstanceId.ToString();
                domainPOI.Header.ParentProductInstanceKey = WHPOI.Header.ProductInstanceKey;


                //domainPOI.RoleInstances = WHPOI.RoleInstances;
                domainPOI.RoleInstances = new List<BT.SaaS.Core.Shared.Entities.RoleInstance>();

                foreach (BT.SaaS.Core.Shared.Entities.RoleInstance ri in WHPOI.RoleInstances)
                {
                    BT.SaaS.Core.Shared.Entities.RoleInstance domainRI = new BT.SaaS.Core.Shared.Entities.RoleInstance();
                    domainRI.Action = RoleInstanceActionEnum.add;
                    domainRI.XmlIdKey = ri.XmlIdKey;
                    domainRI.InternalProvisioningAction = ri.InternalProvisioningAction;
                    domainRI.Status = ri.Status;
                    domainRI.QuantityToAddRemove = ri.QuantityToAddRemove;
                    domainRI.RoleType = ri.RoleType;
                    domainRI.UserXmlRef = ri.UserXmlRef;
                    domainRI.Attributes = ri.Attributes;
                    domainRI.UserIdentityCredentialXmlRef = ri.UserIdentityCredentialXmlRef;
                    domainPOI.RoleInstances.Add(domainRI);
                }


                //foreach (BT.SaaS.Core.Shared.Entities.RoleInstance RI in domainPOI.RoleInstances)
                //{
                //    RI.Action = RoleInstanceActionEnum.add;
                //}

                return domainPOI;
            }
            catch (System.Exception ex)
            {
                throw new MappingException(ex.Message);
            }
        }

        internal static BT.SaaS.Core.Shared.Entities.Order createDeactivateRequest(BT.SaaS.Core.Shared.Entities.Order saasOrder)
        {
            BT.SaaS.Core.Shared.Entities.Order deactivateOrder = new BT.SaaS.Core.Shared.Entities.Order();

            deactivateOrder = BT.SaaS.Core.Shared.Entities.Order.FromString(saasOrder.ToString());

            deactivateOrder.Header.EffectiveDateTime = DateTime.Now;
            deactivateOrder.Header.Action = OrderActionEnum.deactivateProduct;
            deactivateOrder.Header.OrderKey = "DP-" + deactivateOrder.Header.OrderKey;



            return deactivateOrder;
        }

        internal static BT.SaaS.Core.Shared.Entities.Order createRegisterDomainRequest(BT.SaaS.Core.Shared.Entities.Order saasOrder)
        {
            BT.SaaS.Core.Shared.Entities.Order registerDomainRequest = new BT.SaaS.Core.Shared.Entities.Order();

            registerDomainRequest = BT.SaaS.Core.Shared.Entities.Order.FromString(saasOrder.ToString());

            // Processing Data set to future 
            registerDomainRequest.Header.EffectiveDateTime = DateTime.Now.AddDays(365);
            registerDomainRequest.Header.Action = OrderActionEnum.registerDomain;
            registerDomainRequest.Header.OrderKey = "RD-RCOM-" + registerDomainRequest.Header.OrderKey;


            return registerDomainRequest;
        }

        /// <summary>
        /// get all the domain product instances for the WH product instances
        /// get the service instances for the domain product instances. 
        /// get the servce characterstics of the domain service instance. 
        /// serach for the service instance & service role & service role characterstics for an attribut LicenseType & its value GTLD
        /// </summary>
        /// <param name="saasOrder"></param>
        /// <returns>Returns Order with Domain Product Orer Item</returns>
        internal static ProductOrderItem CreateProductOrderItemForCCDomain(BT.SaaS.Core.Shared.Entities.Order saasOrder)
        {
            try
            {

                bool foundCCServiceInstance = false;

                string whProdctInstnaceKey = string.Empty;
                string domainProdctInstnaceKey = string.Empty;
                string ccDomainProdInstanceKey = string.Empty;

                User userData;
                BT.SaaS.Core.Shared.Entities.RoleInstance adminRoleInstnace = null;
                BT.SaaS.Core.Shared.Entities.IdentityCredential identityCredential;
                ProductOrderItem whProductOrderItem = null;
                ProductOrderItem ccDomainProductOrderItem = null;
                ProductServiceInstanceEx ccDomainProdSrvcInstance = null;
                ProductDefinationInstance ccDomainProdDefinationInstance = null;
                ClientServiceInstance[] clientSrvcInstances = null;
                List<ProductDefinationInstance> domainProductInstances = null;
                List<ProductServiceInstanceEx> domainProductSrvcMapping = null;
                ClientServiceRoleCharacteristic domainNameChar = null;

                // Get the WH product Instance Key
                foreach (ProductOrderItem POI in saasOrder.ProductOrderItems)
                {
                    if (POI.Header.ProductCode.StartsWith(ConfigurationManager.AppSettings["webHostingSCode"]))
                    {
                        whProdctInstnaceKey = POI.Header.ProductInstanceKey;
                        whProductOrderItem = POI;
                        break;
                    }
                }

                // Get User data from the xml 
                adminRoleInstnace = whProductOrderItem.RoleInstances.Where(RI => RI.RoleType.ToUpper() == "ADMIN").SingleOrDefault();
                userData = saasOrder.Header.Users.Where(user => user.XmlIdKey == adminRoleInstnace.UserXmlRef).SingleOrDefault();
                identityCredential = userData.IdentityCredential.Where(IC => IC.XmlIdKey == adminRoleInstnace.UserIdentityCredentialXmlRef).SingleOrDefault();

                // Get Service instances for the users from DnP
                clientSrvcInstances = DnpWrapper.GetClientServiceInstances(identityCredential.Identity.identifier, identityCredential.Identity.identiferNamepace);

                // Get the Domain Product Instance Key's which are dependent on WH Product Instance Key
                domainProductInstances = MdmWrapper.GetDependentDomainProductsFromPIK(whProdctInstnaceKey, saasOrder.Header.ServiceProviderID);

                // Loop in the Domain Product Instance Keys
                if (domainProductInstances != null)
                {
                    foreach (ProductDefinationInstance PDI in domainProductInstances)
                    {
                        domainProdctInstnaceKey = PDI.ProductInstanceKey;

                        // Get the Service Instances for the domain product Instance Keys
                        domainProductSrvcMapping = MdmWrapper.GetServiceInstancesByProductKey(domainProdctInstnaceKey, saasOrder.Header.ServiceProviderID);

                        if (domainProductSrvcMapping != null)
                        {

                            foreach (ProductServiceInstanceEx PSI in domainProductSrvcMapping)
                            {
                                foreach (ClientServiceInstance CSI in clientSrvcInstances)
                                {
                                    if (CSI.clientServiceInstanceIdentifier.value.Equals("REGISTER_COM", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        if (CSI.name == PSI.ServiceInstanceKey)
                                        {
                                            foreach (ClientServiceRole CSR in CSI.clientServiceRole)
                                            {
                                                if (CSR.id.ToUpper() == "ADMIN")
                                                {
                                                    ClientServiceRoleCharacteristic licenseTypeChar = CSR.clientServiceRoleCharacteristic.Where(CSRC => CSRC.name.ToUpper() == "LICENSETYPE").SingleOrDefault();

                                                    domainNameChar = CSR.clientServiceRoleCharacteristic.Where(CSRC => CSRC.name.ToUpper() == "DOMAINNAME").SingleOrDefault();

                                                    if (licenseTypeChar.value.ToUpper() == "GTLD")
                                                    {
                                                        foundCCServiceInstance = true;
                                                    }
                                                }

                                                if (foundCCServiceInstance)
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    if (foundCCServiceInstance)
                                    {
                                        ccDomainProdSrvcInstance = PSI;
                                        break;
                                    }
                                }

                                if (foundCCServiceInstance)
                                {
                                    ccDomainProdDefinationInstance = PDI;
                                    break;
                                }
                            }
                        }

                        if (foundCCServiceInstance)
                        {
                            break;
                        }
                    }
                }

                ccDomainProductOrderItem = new ProductOrderItem();
                ccDomainProductOrderItem.Header.ProductCode = ccDomainProdDefinationInstance.Code;
                ccDomainProductOrderItem.Header.ProductInstanceKey = ccDomainProdDefinationInstance.ProductInstanceKey;
                ccDomainProductOrderItem.Header.ProductName = ccDomainProdDefinationInstance.Name;
                ccDomainProductOrderItem.RoleInstances = whProductOrderItem.RoleInstances;

                foreach (BT.SaaS.Core.Shared.Entities.RoleInstance RI in ccDomainProductOrderItem.RoleInstances)
                {
                    RI.Attributes = new List<BT.SaaS.Core.Shared.Entities.Attribute>();

                    BT.SaaS.Core.Shared.Entities.Attribute domainNameAttr = new BT.SaaS.Core.Shared.Entities.Attribute();
                    domainNameAttr.action = DataActionEnum.delete;
                    domainNameAttr.Name = "DOMAINNAME";
                    domainNameAttr.Value = domainNameChar.value;
                    RI.Attributes.Add(domainNameAttr);
                }

                return ccDomainProductOrderItem;
            }
            catch (System.Exception ex)
            {
                throw new MappingException(ex.Message);
            }
        }

        public static bool isFutureEmailCeaseOrder(BT.SaaS.Core.Shared.Entities.Order saasOrder)
        {
            bool returnFlag = false;
            if (saasOrder.Header.EffectiveDateTime > saasOrder.Header.OrderDateTime)
            {
                string[] emailProducts = ConfigurationManager.AppSettings["EmailProductCodes"].Split(new char[] { ',' });
                foreach (ProductOrderItem poi in saasOrder.ProductOrderItems)
                {
                    if (emailProducts.Contains(poi.Header.ProductCode))
                    {
                        foreach (BT.SaaS.Core.Shared.Entities.RoleInstance ri in poi.RoleInstances)
                        {
                            if (ri.Action == RoleInstanceActionEnum.delete)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return returnFlag;
        }

        public static bool IsModifyServiceFromCeaseOrder(BT.SaaS.IspssAdapter.BOS_API.OrderItem orderItem)
        {
            bool isModifyService = false;
            string productCode = getSAASProductCode(orderItem);
            string productInstanceId = orderItem.ProductInstanceId;
            int orderQuantity;
            List<ProductDefinition> productDefinitions = MdmWrapper.getSaaSProductDefs(new List<string> { productCode }, Mapper.getResellerId());
            int quantity, freeQuantity;
            
            GetVasUsageImpl.getProductInstanceUsage(out quantity, out freeQuantity, productInstanceId, productDefinitions[0]);

            if (!int.TryParse(orderItem.Quantity, out orderQuantity))
            {
                throw new MappingException("Bad Quantity " + orderItem.Quantity + " in orderItem. ProductInstanceId=" + productInstanceId);
            }

            // If the quantity in the order is less than the quantity on the product instance then
            // we are reducing the quantity so it's a modifyService otherwise we are ceasing
            if (orderQuantity < quantity)
            {
                isModifyService = true;
            }

            return isModifyService;
        }


        internal static BT.SaaS.Core.Shared.Entities.Order modifyFutureCeaseOrderforEmailProducts(BT.SaaS.Core.Shared.Entities.Order saasOrder)
        {
            BT.SaaS.Core.Shared.Entities.Order modifyServiceAddOrder =
                BT.SaaS.Core.Shared.Entities.Order.FromString(saasOrder.ToString());

            modifyServiceAddOrder.Header.OrderKey = "DP" + modifyServiceAddOrder.Header.OrderKey;

            foreach (ProductOrderItem poi in modifyServiceAddOrder.ProductOrderItems)
            {
                string[] emailProducts = ConfigurationManager.AppSettings["EmailProductCodes"].Split(new char[] { ',' });
                if (emailProducts.Contains(poi.Header.ProductCode))
                {
                    poi.Header.Status = DataStatusEnum.done;
                    foreach (BT.SaaS.Core.Shared.Entities.RoleInstance ri in poi.RoleInstances)
                    {
                        ri.Action = RoleInstanceActionEnum.add;
                    }
                }
            }
            return modifyServiceAddOrder;
        }

        #region Cease Website Orders
        public static BT.SaaS.Core.Shared.Entities.Order[] GetCeaseWebsiteOrders(placeOrder bosOrder, BT.SaaS.Core.Shared.Entities.Order ceaseWHOrder)
        {

            List<BT.SaaS.Core.Shared.Entities.Order> lstDeleteWebsiteOrders = new List<BT.SaaS.Core.Shared.Entities.Order>();
            BT.SaaS.Core.Shared.Entities.Order ceaseWebsiteOrder = null;

            BT.SaaS.IspssAdapter.BOS_API.OrderItem ispOI = Mapper.getIspControl(bosOrder);
            foreach (BT.SaaS.IspssAdapter.BOS_API.OrderItem bosOI in ispOI.OrderItem1)
            {
                if (!(bosOI.Action == BT.SaaS.IspssAdapter.BOS_API.Action.Delete &&  //only cease
                    bosOI.ProductCode.Contains(ConfigurationManager.AppSettings["webHostingSCode"]))) //only IBP/IBP+
                    continue;

                string primaryDomain = GetPrimaryDomain(bosOI);
                primaryDomainfromBos = primaryDomain;

                if (string.IsNullOrEmpty(primaryDomain))
                    continue; //website is not activated for this product

                if (ceaseWebsiteOrder == null) //first time clone from cease hosting order
                {
                    ceaseWebsiteOrder = BT.SaaS.Core.Shared.Entities.Order.FromString(ceaseWHOrder.ToString());
                    ProductOrderItem ceasePOI = ceaseWebsiteOrder.ProductOrderItems[0];
                    ceaseWebsiteOrder.Header.Action = OrderActionEnum.cease;
                    ceaseWebsiteOrder.Header.EffectiveDateTime = ceaseWebsiteOrder.Header.EffectiveDateTime.AddDays(30);
                    ceaseWebsiteOrder.ProductOrderItems.Clear();  //keep only first order item in case of many
                    ceaseWebsiteOrder.ProductOrderItems.Add(ceasePOI);
                    ceasePOI.Header.ParentProductInstanceId = bosOI.ProductInstanceId;
                    ceasePOI.Header.ProductCode = ConfigurationManager.AppSettings["DummyWebsiteProductCode"];
                    BT.SaaS.Core.Shared.Entities.RoleInstance roleInstance = ceasePOI.RoleInstances[0];
                    roleInstance.Action = RoleInstanceActionEnum.delete;
                    roleInstance.InternalProvisioningAction = ProvisioningActionEnum.cease;
                    roleInstance.RoleType = ConfigurationManager.AppSettings["primaryUserRoleType"];
                    roleInstance.Attributes = new List<BT.SaaS.Core.Shared.Entities.Attribute>();
                    ceasePOI.RoleInstances.Clear();
                    ceasePOI.RoleInstances.Add(roleInstance);
                    roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute());
                    roleInstance.Attributes[0].Name = "primarydomainname";
                    roleInstance.Attributes[0].action = DataActionEnum.delete;
                }
                else
                {
                    //clone the cease website order from existing simillar order
                    ceaseWebsiteOrder = BT.SaaS.Core.Shared.Entities.Order.FromString(ceaseWebsiteOrder.ToString());
                }
                ceaseWebsiteOrder.Header.OrderKey = ceaseWebsiteOrder.Header.OrderKey + "-DW" + lstDeleteWebsiteOrders.Count.ToString();
                ceaseWebsiteOrder.ProductOrderItems[0].RoleInstances[0].Attributes[0].Value = primaryDomain;
                lstDeleteWebsiteOrders.Add(ceaseWebsiteOrder);
            }

            return lstDeleteWebsiteOrders.ToArray();
        }
        public static System.DateTime getDomainAddTime(string domainname)
        {
            //need to call store proc for getting domain added time
            System.DateTime domainAddTime;          
             domainAddTime = VASAdapterDBDAL.getDomainCreatedTime(domainname);
            return domainAddTime;
        }

        public static BT.SaaS.Core.Shared.Entities.Order updateEffectiveDateorNot(BT.SaaS.Core.Shared.Entities.Order saasorder)
        {
            //Checking weather to change effective datetime or not.
            if (!string.IsNullOrEmpty(primaryDomainfromBos) && primaryDomainfromBos != "" && (saasorder.Header.Action == OrderActionEnum.ceaseWH || saasorder.Header.Action == OrderActionEnum.cease))
            {
                // Need to add extra condition here - If domain purchased time is less than 5 days we cant allow the product to cease
                // First need to get the domain purchased time - then compare the time with current time - Then add required days here.
                
                System.DateTime domainAddTime = getDomainAddTime(primaryDomainfromBos);
                System.TimeSpan numberOfDaysGap;
                if (!domainAddTime.Equals(System.DateTime.MaxValue))
                {
                    numberOfDaysGap = saasorder.Header.EffectiveDateTime - domainAddTime;
                    if (numberOfDaysGap.Days < 5 && numberOfDaysGap.Days > 0)
                    {
                        DateTime effectivtime = saasorder.Header.EffectiveDateTime;
                        DateTime modifiedeffectivedatetime = effectivtime.AddDays((5 - numberOfDaysGap.Days));
                        saasorder.Header.EffectiveDateTime = modifiedeffectivedatetime;
                    }
                    if (numberOfDaysGap.Days == 0)
                    {
                        DateTime effectivtime2 = saasorder.Header.EffectiveDateTime;
                        DateTime modifiedeffectivedatetime2 = effectivtime2.AddDays(6);
                        saasorder.Header.EffectiveDateTime = modifiedeffectivedatetime2;
                    }
                }
            }
            return saasorder;
        }

        private static string GetPrimaryDomain(BT.SaaS.IspssAdapter.BOS_API.OrderItem ibpOI)
        {
            string primaryDomain = string.Empty;
            foreach (BT.SaaS.IspssAdapter.BOS_API.ItemAttribute ibpOIA in ibpOI.ListOfOrderItemAttribute)
            {
                if (ibpOIA.Name.Equals("PrimaryDomain", StringComparison.OrdinalIgnoreCase))
                {
                    primaryDomain = ibpOIA.NewValue;
                    break;
                }
            }

            return primaryDomain;
        }

        public static BT.SaaS.Core.Shared.Entities.Order RegradeDomainName(BT.SaaS.Core.Shared.Entities.Order saasorder, string domainname)
        {
            primaryDomainfromBos = domainname;
            BT.SaaS.Core.Shared.Entities.Order domainOrder;
            domainOrder = Mapper.updateEffectiveDateorNot(saasorder);
            return domainOrder;
        }
        #endregion

        /// <summary>
        /// To get Customer Client level Characterstics...
        /// </summary>
        /// <param name="customerKey"></param>
        /// <returns>List of ClientCharactertics</returns>
        private static List<Dnp.ClientCharacteristic> getCustomerCharacterstics(string customerKey)
        {
            List<Dnp.ClientCharacteristic> clientChar = new List<Dnp.ClientCharacteristic>();
            clientChar = DnpWrapper.GetClientCharacterstics(customerKey);
            return clientChar;
        }

        /// <summary>
        /// Method to add the Product Role Attribute for External domains && MOSI Users
        /// </summary>
        /// <param name="order">SaaS order</param>
        /// <returns>order</returns>
        private static BT.SaaS.Core.Shared.Entities.Order SetEmailActivity(BT.SaaS.Core.Shared.Entities.Order order)
        {
            if (order.ProductOrderItems != null && order.ProductOrderItems.Count > 0)
            {
                List<ProductOrderItem> productOrderItemsList = (order.ProductOrderItems.Exists(poi => poi.Header.ProductCode.StartsWith(ConfigurationManager.AppSettings["domainProductSCode"]))) ? order.ProductOrderItems.Where(poi => poi.Header.ProductCode.StartsWith(ConfigurationManager.AppSettings["domainProductSCode"])).ToList() : null;
                string type = string.Empty;

                if (productOrderItemsList != null)
                {
                    foreach (ProductOrderItem poi in productOrderItemsList)
                    {
                        if (poi.RoleInstances != null && poi.RoleInstances.Exists(ri => ri.RoleType.Equals("ADMIN", StringComparison.OrdinalIgnoreCase)))
                        {
                            BT.SaaS.Core.Shared.Entities.RoleInstance roleInstance = poi.RoleInstances.Find(ri => ri.RoleType.Equals("ADMIN", StringComparison.OrdinalIgnoreCase));
                            type = roleInstance.Attributes.Exists(attr => attr.Name.Equals("TYPE", StringComparison.OrdinalIgnoreCase)) ? roleInstance.Attributes.Find(attr => attr.Name.Equals("TYPE", StringComparison.OrdinalIgnoreCase)).Value : string.Empty;
                            if (!string.IsNullOrEmpty(type) && type.Equals("EXTERNAL", StringComparison.OrdinalIgnoreCase))
                            {
                                roleInstance.Attributes.Add(new BT.SaaS.Core.Shared.Entities.Attribute()
                                {
                                    action = DataActionEnum.add,
                                    Name = "EMAIL_ACTIVITY_KEY",
                                    Value = "MOSI"
                                });
                            }
                        }
                    }
                }
            }

            return order;
        }


        /// <summary>
        /// To map the EmailActivity attribute while External Domain Provision
        /// </summary>
        /// <param name="order"></param>
        /// <returns>order</returns>
        private static BT.SaaS.Core.Shared.Entities.Order MapEmailActivity(BT.SaaS.Core.Shared.Entities.Order order)
        {
            //For Existing Customers - having "ORG_MIG_STATUS" flag as "NEW" or "MS_MIGRATION_FULLY_COMPLETED"
            if (isNewOrMigratedCustomer)
            {
                SetEmailActivity(order);
            }
            return order;
        }

        public static bool IsNewOrMigratedCustomer(BT.SaaS.Core.Shared.Entities.Customer customerDetails)
        {
            bool isNewOrMig = false;
            string orgStatus = string.Empty;
            if (customerDetails != null && customerDetails.Attributes != null && customerDetails.Attributes.Count > 0)
            {
                orgStatus = customerDetails.Attributes.Exists(a => a.Name.ToUpper() == "ORG_MIG_STATUS") ? customerDetails.Attributes.Find(a => a.Name.ToUpper() == "ORG_MIG_STATUS").Value : string.Empty;
                if (!string.IsNullOrEmpty(orgStatus) && ConfigurationManager.AppSettings["CustomerMigrationStatus"].Split(',').Contains(orgStatus.ToUpper()))
                {
                    isNewOrMig = true;
                }
            }
            return isNewOrMig;
        }
    }


    public static class DebugHelper
    {
        public static string GetDebugInfo(this IEnumerable collection)
        {
            List<string> retval = new List<string>();

            if (collection != null)
            {
                foreach (object obj in collection)
                {
                    if (obj != null)
                    {
                        retval.Add(obj.ToString());
                    }
                }
            }

            return string.Join(Environment.NewLine, retval.ToArray());
        }
    }
}
