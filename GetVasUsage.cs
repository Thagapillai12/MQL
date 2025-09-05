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
using System.Xml.Linq;
using BT.SaaS.IspssAdapter.GetVasUsage;
using BT.SaaS.Core.MDMAPI.Entities.Product;
using System.Collections.Generic;
using BT.SaaS.Core.MDMAPI.Entities.Customer;

namespace BT.SaaS.IspssAdapter
{
    public class GetVasUsageImpl
    {

        public static GetVASUsageResponseType getUsage(GetVASUsageType getVASUsageType, GetVASUsageResponseType response, string productInstanceKey, string productCode, string customerKey)
        {
            bool onSaaS = false;
            string errorType = null;

            // If they supply a productInstanceKey in the input use it
            if( productInstanceKey != null )
            {
                errorType = "productInstanceId " + productInstanceKey;
                onSaaS = SetTotalAndFreeQuantityByProductInstanceKey(productInstanceKey, productCode, customerKey, response);
            }
            // If they only supply a product code in the input
            else
            {
                errorType = "productCode " + productCode;
                onSaaS = SetTotalAndFreeQuantityByProductCode(productCode, customerKey, response);
            }

            if( !onSaaS )
            {
                // Shall we try the SDP?
                if (ConfigurationManager.AppSettings["GetVasUsageSDPSwitch"].ToString().Equals("ON", StringComparison.InvariantCultureIgnoreCase))
                {
                    //Call SDP
                    Logger.Debug("GetVasUsageImpl:getUsage() Calling the SDP");
                    SdpGetVasUsageService sdpService = new SdpGetVasUsageService(ConfigurationManager.AppSettings["GetVasUsageSDPURL"].ToString());
                    response = sdpService.getVasUsage(getVASUsageType);
                }
                else
                {
                    throw new GetVasUsageException(errorType+" not found on SaaS");
                }
            }
 
            return response;
        }

        private static bool SetTotalAndFreeQuantityByProductInstanceKey(string productInstanceKey, string productCode, string customerKey, GetVASUsageResponseType response)
        {
            // Get product instances belonging to this customer
            ProductInstance[] customerProductInstances = MdmWrapper.getCustomerProductInstances(customerKey);
            ProductInstance productInstance = null;

            // find the product instance we need
            foreach( ProductInstance pI in customerProductInstances)
            {
 
                if( pI.ProductInstanceKey.Equals(productInstanceKey) )
                    productInstance=pI;
            }

            // Did we find a product instance in SaaS?
            if (productInstance!=null)
            {
                if (productInstance.Status == ProductInstanceStatus.Ceased)
                {
                    throw new GetVasUsageException("productInstanceId " + productInstanceKey + " has already been ceased");
                }

                // Found it in SaaS
                
                // Now we need to get the product definition
                ProductDefinition productDefinition = MdmWrapper.getProductDefinitionByProductDefinitionId(productInstance.ProductDefinitionId);

                // If a product code and product instance id are supplied check they match up
                if (productCode != null && !productDefinition.Code.Equals(productCode))
                {
                    throw new GetVasUsageException("productInstanceId " + productInstanceKey + 
                                                   " has productCode " + productDefinition.Code + 
                                                   " in SaaS MDM. This doesn't match the product code " + productCode + 
                                                   " supplied in the request");
                }

                bool isLicenceStorageProduct = false;
                int quantity, freeQuantity, freeMailStorage;                

                if (ConfigurationManager.AppSettings["LicenseStorageProducts"].Contains(productCode))
                {
                    isLicenceStorageProduct = true;
                }

                // Get Licence Usage
                getProductInstanceUsage(out quantity, out freeQuantity, productInstanceKey, productDefinition);

                // Get Mail Usage
                if (isLicenceStorageProduct)
                {
                    var dnpResponse = DnpWrapper.GetClientCharacterstics(customerKey);
                    bool requiredMailStorageCall = true;
                    if (dnpResponse != null && dnpResponse.Count > 0)
                    {
                        dnpResponse.ForEach(a =>
                        {
                            if (a.name.ToUpper() == "IS_HE_COMPATIBLE" && a.value.ToUpper() == "NO")
                                requiredMailStorageCall = false;
                        });
                    }
                    if (requiredMailStorageCall || !MdmWrapper.CheckEmailServiceIsFirstServiceInMDM(productCode))
                    {
                        // Get Mail storage Product defination
                        List<ProductDefinition> mailStorageProdDef = MdmWrapper.getSaaSProductDefs(new List<string> { ConfigurationManager.AppSettings["MailStorage"].ToString() }, Mapper.getResellerId());
                        getMailStorageUsage(out freeMailStorage, productCode, customerKey, productInstanceKey, mailStorageProdDef[0]);

                        if (Convert.ToInt32(freeMailStorage) >= Convert.ToInt32(ConfigurationManager.AppSettings["Quantity_" + productCode]))
                        {

                            response.FreeQuantity = freeQuantity.ToString();
                            response.TotalQuantity = quantity.ToString();
                            Logger.Debug(string.Format("Total Mail storage is greater then the storage required to cease the Product. PROD_CODE : {0}", productCode));
                            Logger.Debug("SetTotalAndFreeQuantityByProductInstanceKey() returning quantity=" + quantity + " freeQuantity=" + freeQuantity);

                        }
                        else
                        {
                            // If the Mail Service Instance doesn't have enough quantity to cease, 
                            // then return Total quantity as 1 & Free Quantity as 0.
                            response.FreeQuantity = "0";
                            response.TotalQuantity = "1";
                            Logger.Debug(string.Format("Total Mail storage is less then the storage required to cease the Product. PROD_CODE : {0}, Free Mail Storage Space : {1}", productCode, freeMailStorage));
                        }
                    }
                    else
                    {
                        Logger.Debug("License Based Product only, But MailStorage Check is Not needed");
                        response.FreeQuantity = freeQuantity.ToString();
                        response.TotalQuantity = quantity.ToString();                        
                    }
                }
                else
                {
                    // Get Licence Usage
                    getProductInstanceUsage(out quantity, out freeQuantity, productInstanceKey, productDefinition);
                    response.FreeQuantity = freeQuantity.ToString();
                    response.TotalQuantity = quantity.ToString();
                    Logger.Debug("Product is not a Mail Storaged Product");
                    Logger.Debug("SetTotalAndFreeQuantityByProductInstanceKey() returning quantity=" + quantity + " freeQuantity=" + freeQuantity);

                }

                
                

                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool SetTotalAndFreeQuantityByProductCode(string productCode, string customerKey, GetVASUsageResponseType response)
        {
            int totalQuantity = 0;
            int totalFreeQuantity = 0;

            // Look up product definition in SaaS
            List<ProductDefinition> productDefinitions = MdmWrapper.getSaaSProductDefs(new List<string> { productCode }, Mapper.getResellerId());

            // Does SaaS handle this product?
            if (productDefinitions.Count > 0 && productDefinitions[0].Status != ProductDefinitionStatus.Inactive)
            {
                ProductDefinition productDefinition = productDefinitions[0];

                // Get product instances with this product code
                List<ProductInstance> productInstances = MdmWrapper.GetProductInstancesByProductId(productDefinition.ProductDefinitionId, customerKey);

                // Loop through each of the product instances and add up the quantities
                foreach (ProductInstance productInstance in productInstances)
                {
                    if (productInstance.Status != ProductInstanceStatus.Ceased)
                    {
                        int quantity, freeQuantity;
                        getProductInstanceUsage(out quantity, out freeQuantity, productInstance.ProductInstanceKey, productDefinition);
                        totalQuantity += quantity;
                        totalFreeQuantity += freeQuantity;
                    }
                }

                response.FreeQuantity = totalFreeQuantity.ToString();
                response.TotalQuantity = totalQuantity.ToString();

                Logger.Debug("SetTotalAndFreeQuantityFromSaas() returning totalQuantity=" + totalQuantity + " totalFreeQuantity=" + totalFreeQuantity);
                return true;
            }
            else
            {
                return false;
            }
        }        
        
        public static void getProductInstanceUsage(out int quantity, out int freeQuantity, string productInstanceKey, ProductDefinition productDefinition)
        {
            quantity = 0;
            freeQuantity = 0;

            if (productDefinition.ProductDefinitionId > 0)
            {
                int licenceMultiplier, serviceInstanceTotalQuantity, serviceInstanceFreeQuantity;
                List<ProductServiceInstanceEx> serviceInstances = MdmWrapper.GetServiceInstancesByProductKey(productInstanceKey);

                List<ProductServiceInstanceEx> EmailserviceInstances = serviceInstances.Where(p=>p.ServiceCode.Contains(ConfigurationManager.AppSettings["MOSIServiceCode"].ToString())).ToList();
                int serviceSpecificationId;
                if (EmailserviceInstances != null && EmailserviceInstances.Count == 1 && productDefinition.Code != "S0001452-PR0000000005200-TA0000000005200")
                {
                    licenceMultiplier = MdmWrapper.GetProductFirstServiceLicenceQuantity(productDefinition.ProductDefinitionId, EmailserviceInstances[0].ServiceSpecificationId, productDefinition.Code, out serviceSpecificationId);
                }
                else
                {
                    licenceMultiplier = MdmWrapper.GetProductFirstServiceLicenceQuantity(productDefinition.ProductDefinitionId, 0, productDefinition.Code, out serviceSpecificationId);
                }

                // we only want to check the service instance we got the licence multiplier for
                // so let's loop through the service instances and find the one of the right type
                ProductServiceInstanceEx serviceInstanceToCheck = null;
                foreach (ProductServiceInstanceEx serviceInstance in serviceInstances)
                {
                    if (serviceInstance.ServiceSpecificationId == serviceSpecificationId)
                    {
                        serviceInstanceToCheck = serviceInstance;
                        break;
                    }
                    else
                    {
                        //Assuming only 1 service instance will come in MDM response, as only 1 service is mapped to 100 MB product in MDM.
                        //Else it is an DI issue. :)
                        if (productDefinition.Code == "S0001452-PR0000000005200-TA0000000005200" && serviceInstances != null && serviceInstances.Count == 1 && serviceInstances[0].ServiceCode == "MOSI")
                        {
                            serviceInstanceToCheck = serviceInstance;
                            break;
                        }
                    }
                }
                if (serviceInstanceToCheck == null)
                {
                    throw new GetVasUsageException("Failed to find serviceInstance of the same type as licenceQuantity check was run against");
                }

                DnpWrapper.GetVasUsage(new List<ProductServiceInstanceEx>() { serviceInstanceToCheck }, out serviceInstanceTotalQuantity, out serviceInstanceFreeQuantity);            
                
                // If this is a base product and only 1 licence allocated then this is the admin
                // If the product is HE product, then the below logic should not be applied.
                if (productDefinition.LicenseType != null && ((serviceInstanceTotalQuantity - serviceInstanceFreeQuantity) == 1) && !ConfigurationManager.AppSettings["LicenseTypeExceptionProducts"].Contains(productDefinition.Code))
                {
                    Logger.Debug("getProductInstanceUsage() This is a base product with just the admin account allocated. Increasing the licenceFree count to 100%");
                    serviceInstanceFreeQuantity = serviceInstanceTotalQuantity;
                }

                quantity = serviceInstanceTotalQuantity / licenceMultiplier;
                freeQuantity = serviceInstanceFreeQuantity / licenceMultiplier;

                Logger.Debug("getProductInstanceUsage() returning quantity=" + quantity + " freeQuantity=" + freeQuantity);
            }
        }

        public static void getMailStorageUsage(out int freeMailStorage, string productCode, string customerKey, string productInstanceKey, ProductDefinition mailStorageproductDefinition)
        {
            freeMailStorage = 0;
            int mailStorageTotalQuantity;
            string mailStoragePIK = string.Empty;
            string mailStorageSrvcInstKey = string.Empty;
            List<ProductServiceInstanceEx> mailStoragePIKSrvcMapping;

            // Get all the product instance for the customer
            ProductInstance[] customerPIs = MdmWrapper.getCustomerProductInstances(customerKey);

            // Looping all the product instances to get the PIK for MailStorage product.
            foreach (ProductInstance PI in customerPIs)
            {
                if (mailStorageproductDefinition.ProductDefinitionId == PI.ProductDefinitionId)
                {
                    mailStoragePIK = PI.ProductInstanceKey;
                    break;
                }
            }

            // Get Service Instance Mapped to PIK for MailStorage product.
            if (!string.IsNullOrEmpty(mailStoragePIK))
            {
                mailStoragePIKSrvcMapping = MdmWrapper.GetServiceInstancesByProductKey(mailStoragePIK);
                DnpWrapper.GetVasUsage(mailStoragePIKSrvcMapping, out mailStorageTotalQuantity, out freeMailStorage);
            }
            else
            {
                // throw exception if mailStoragePIK is null or empty.
                throw new MdmException(string.Format("ProductInstanceKey for MailStorage for Customer : {0} cannot be null or empty", customerKey));
            }            
        }
    }
}
