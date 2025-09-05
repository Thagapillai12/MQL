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
using BT.SaaS.IspssAdapter.MDMAPI.ProductService;
using BT.SaaS.IspssAdapter.MDMAPI.CustomerService;
using BT.SaaS.Core.MDMAPI.Entities.Product;
using Microsoft.Practices.EnterpriseLibrary.Caching;
using Microsoft.Practices.EnterpriseLibrary.Caching.Expirations;
using System.Collections.Generic;
using BT.SaaS.Core.MDMAPI.Entities.Customer;
using System.Globalization;
using BT.SaaS.Core.MDMAPI.Entities.ProductMapping;
using System.ServiceModel;
using BT.SaaS.Core.Shared.Entities;
using BT.SaaS.IspssAdapter.Dnp;
using System.Text;
using System.IO;


namespace BT.SaaS.IspssAdapter
{
    public sealed class MdmWrapper
    {
        private MdmWrapper()
        { }

        static public List<ProductDefinition> getSaaSProductDefs(List<string> inputProductCodeList, int serviceProviderId)
        {
            List<ProductDefinition> productData = new List<ProductDefinition>();
            CacheManager cacheManager;
            List<string> nonCachedProductCodeList = new List<string>();
            string[] productsToIgnore = ConfigurationManager.AppSettings["IgnoreProductList"].Split(new char[] { ';' });

            // Let's try and find as many as we can from the cache
            try
            {
                cacheManager = (CacheManager)CacheFactory.GetCacheManager();
                ProductDefinition productDefinition = null;
                foreach (string productCode in inputProductCodeList)
                {
                    if (!productsToIgnore.Contains(productCode))
                    {
                        productDefinition = (ProductDefinition)cacheManager.GetData(productCode);
                        if (productDefinition == null)
                        {
                            nonCachedProductCodeList.Add(productCode);
                        }
                        else
                        {
                            productData.Add(productDefinition);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new CacheException("getSaaSProductDefs() failed to read cache: " + e.Message.ToString(), e);
            }

            // Did we find everything?
            if (nonCachedProductCodeList.Count() > 0)
            {
                // We need to call MDM to get the missing product definitions
                ProductDefinition[] mdmProductDefinitions = getProductDefinitionsFromMDM(nonCachedProductCodeList, serviceProviderId);

                // Add to cache and return list
                int cacheRefresh = getCacheRefresh();
                foreach (ProductDefinition pd in mdmProductDefinitions)
                {
                    cacheManager.Add(pd.Code, pd, CacheItemPriority.High, null,
                       new AbsoluteTime(DateTime.Now.AddMinutes(cacheRefresh)));

                    productData.Add(pd);
                }
            }
            Logger.Debug("getSaaSProductDefs() returning " + productData.Count().ToString(CultureInfo.InvariantCulture) +
                         " Product Definitions");
            return productData;
        }

        static public List<ProductDefinition> getSaaSProductDefs(List<string> inputProductCodeList, int serviceProviderId, bool ORTCheckRequired)
        {
            List<ProductDefinition> productData = new List<ProductDefinition>();
            CacheManager cacheManager;
            List<string> nonCachedProductCodeList = new List<string>();
            string[] productsToIgnore;

            if (ORTCheckRequired)
            {
                productsToIgnore = ConfigurationManager.AppSettings["IgnoreORTProductList"].Split(new char[] { ';' });
            }
            else
            {
                productsToIgnore = ConfigurationManager.AppSettings["IgnoreProductList"].Split(new char[] { ';' });
            }

            // Let's try and find as many as we can from the cache
            try
            {
                cacheManager = (CacheManager)CacheFactory.GetCacheManager();
                ProductDefinition productDefinition = null;
                foreach (string productCode in inputProductCodeList)
                {
                    if (!productsToIgnore.Contains(productCode))
                    {
                        productDefinition = (ProductDefinition)cacheManager.GetData(productCode);
                        if (productDefinition == null)
                        {
                            nonCachedProductCodeList.Add(productCode);
                        }
                        else
                        {
                            productData.Add(productDefinition);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new CacheException("getSaaSProductDefs() failed to read cache: " + e.Message.ToString(), e);
            }

            // Did we find everything?
            if (nonCachedProductCodeList.Count() > 0)
            {
                // We need to call MDM to get the missing product definitions
                ProductDefinition[] mdmProductDefinitions = getProductDefinitionsFromMDM(nonCachedProductCodeList, serviceProviderId);

                // Add to cache and return list
                int cacheRefresh = getCacheRefresh();
                foreach (ProductDefinition pd in mdmProductDefinitions)
                {
                    cacheManager.Add(pd.Code, pd, CacheItemPriority.High, null,
                       new AbsoluteTime(DateTime.Now.AddMinutes(cacheRefresh)));

                    productData.Add(pd);
                }
            }
            Logger.Debug("getSaaSProductDefs() returning " + productData.Count().ToString(CultureInfo.InvariantCulture) +
                         " Product Definitions");
            return productData;
        }

        private static ProductDefinition[] getProductDefinitionsFromMDM(List<string> productCodeList, int serviceProviderId)
        {
            // Build the ProductType list
            List<ProductType> productTypeList = new List<ProductType>();
            string productCodeType = ConfigurationManager.AppSettings["productCodeType"];
            foreach (string saasProductCode in productCodeList)
            {
                productTypeList.Add(new ProductType()
                {
                    ProductCode = saasProductCode,
                    ProductCodeType = productCodeType
                });
            }

            // call MDM
            ProductDefinition[] mdmProductDefinitions = null;
            try
            {               
                ProductServiceClient client = new ProductServiceClient("BasicHttpBinding_IProductService");         
                mdmProductDefinitions = client.GetProductDefinitionByProductTypes(productTypeList.ToArray(), serviceProviderId);
            }
            catch (Exception exc)
            {
                MdmException mdmE = new MdmException("Failed to call MDMAPI GetProductDefinitionByProductTypes ", exc);
                throw mdmE;
            }

            // Add to cache
            try
            {
                int cacheRefresh = getCacheRefresh();
                CacheManager cacheManager = (CacheManager)CacheFactory.GetCacheManager();
                foreach (ProductDefinition pd in mdmProductDefinitions)
                {
                    cacheManager.Add(pd.Code, pd, CacheItemPriority.High, null,
                       new AbsoluteTime(DateTime.Now.AddMinutes(cacheRefresh)));
                }
            }
            catch (MappingException)
            {
                throw;
            }
            catch (Exception exc)
            {
                throw new CacheException("Failed to add to cache: " + exc.Message, exc);
            }

            return mdmProductDefinitions;
        }

        public static ProductDefinition getProductDefinitionByProductDefinitionId(int productDefinitionId)
        {
            int serviceProviderId = Mapper.getResellerId();

            // call MDM
            ProductDefinition[] mdmProductDefinitions = null;
            try
            {
                ProductServiceClient client = new ProductServiceClient("BasicHttpBinding_IProductService");
                mdmProductDefinitions = client.GetProductDefinitionsByProductDefinitionIds((new List<int>() { productDefinitionId }).ToArray(), serviceProviderId);
            }
            catch (Exception exc)
            {
                MdmException mdmE = new MdmException("Failed to call MDMAPI GetProductDefinitionByProductTypes ", exc);
                throw mdmE;
            }

            if (mdmProductDefinitions.Count() == 1) return mdmProductDefinitions[0];
            else return null;
        }

        static public List<string> getCustomerProductInstanceKeys(string customerKey)
        {
            ProductInstance[] productInstances = getCustomerProductInstances(customerKey);

            List<string> productInstanceKeys = new List<string>();
            foreach (ProductInstance productInstance in productInstances)
            {
                productInstanceKeys.Add(productInstance.ProductInstanceKey);
            }

            Logger.Debug("getCustomerProductInstanceKeys() returning " + productInstanceKeys.Count() + " productInstanceKeys");
            return productInstanceKeys;
        }

        public static ProductInstance[] getCustomerProductInstances(string customerKey)
        {
            int btRetailId = Mapper.getResellerId();

            ProductInstance[] productInstances = null;
            try
            {
                CustomerServiceClient client = new CustomerServiceClient("BasicHttpBinding_ICustomerService");
                productInstances = client.GetProductInstancesByCustomerKey(customerKey, btRetailId);
            }
            catch (Exception exc)
            {
                MdmException mdmE = new MdmException("Failed to call MDMAPI GetProductInstanceByCustomerKey ", exc);
                throw mdmE;
            }
            return productInstances;
        }

        public static List<ProductServiceInstanceEx> GetServiceInstancesByProductKey(string productKey)
        {

            int btRetailId = Mapper.getResellerId();

            ProductServiceInstanceEx[] serviceInstances = null;
            try
            {
                CustomerServiceClient client = new CustomerServiceClient("BasicHttpBinding_ICustomerService");
                serviceInstances = client.GetServiceInstancesByProductKey(productKey, btRetailId);
            }
            catch (Exception exc)
            {
                MdmException mdmE = new MdmException("Failed to call MDMAPI GetServiceInstancesByProductKey ", exc);
                throw mdmE;
            }

            return serviceInstances.ToList();
        }


        public static List<ProductInstance> GetProductInstancesByProductId(int productId, string customerKey)
        {
            int btRetailId = Mapper.getResellerId();

            ProductInstance[] productInstances = null;
            List<ProductInstance> selectedProductIdProductInstances = null;
            try
            {
                CustomerServiceClient client = new CustomerServiceClient("BasicHttpBinding_ICustomerService");
                productInstances = client.GetProductInstancesByCustomerKey(customerKey, btRetailId);
                selectedProductIdProductInstances = new List<ProductInstance>();

                foreach (ProductInstance productInstance in productInstances.Where(p => p.ProductDefinitionId == productId))
                {
                    selectedProductIdProductInstances.Add(productInstance);
                }
            }
            catch (Exception exc)
            {
                MdmException mdmE = new MdmException("Failed to call MDMAPI GetProductInstancesByProductId ", exc);
                throw mdmE;
            }

            return selectedProductIdProductInstances;
        }

        static public int getCustomerIdByCustomerKey(string customerKey)
        {
            int btRetailId = Mapper.getResellerId();

            int customerId = -1;
            try
            {

                CustomerServiceClient client = new CustomerServiceClient("BasicHttpBinding_ICustomerService");
                customerId = client.GetCustomerIdByCustomerKey(customerKey, btRetailId);
            }
            catch (Exception exc)
            {
                MdmException mdmE = new MdmException("Failed to call MDMAPI GetCustomerIdByCustomerKey ", exc);
                throw mdmE;
            }

            // NB a value of 0 means the customer was not found

            Logger.Debug("GetCustomerIdByCustomerKey() returning " + customerId.ToString());
            return customerId;
        }

        public static int GetProductFirstServiceLicenceQuantity(int productId, int mOSISpecificationId, string prodCode, out int serviceSpecificationId)
        {
            CacheManager cacheManager;
            ProductServiceMapping productServicemapping = null;
            try
            {
                cacheManager = (CacheManager)CacheFactory.GetCacheManager();

                productServicemapping = (ProductServiceMapping)cacheManager.GetData(productId.ToString());
            }
            catch (Exception e)
            {
                throw new CacheException("GetProductFirstServiceLicenceQuantity() failed to read cache: " + e.Message.ToString(), e);
            }

            if (productServicemapping == null)
            {
                productServicemapping = getProductServiceMappingbyProductId(productId, mOSISpecificationId, prodCode);
            }

            serviceSpecificationId = productServicemapping.ServiceSpecificationId;
            int licenceMultiplier = productServicemapping.LicenceQnatity;

            Logger.Debug("GetProductFirstServiceLicenceQuantity serviceSpecificationId="
                         + serviceSpecificationId + " licenceMultiplier=" + licenceMultiplier);
            return licenceMultiplier;
        }

        public static bool CheckEmailServiceIsFirstServiceInMDM(string ProdCode)
        {
            if (ConfigurationManager.AppSettings["ProductsWithFirstServiceAsHEorMOSI"] != null &&
                ConfigurationManager.AppSettings["ProductsWithFirstServiceAsHEorMOSI"].ToString().Contains(ProdCode))
            {
                return true;
            }
            return false;
        }

        private static ProductServiceMapping getProductServiceMappingbyProductId(int productId, int mOSISpecificationId, string pCode)
        {
            int btRetailId = Mapper.getResellerId();

            ProductServiceMapping selectedProductServiceMapping = null;

            // call MDM           
            try
            {
                BT.SaaS.Core.MDMAPI.ProductMappingService.IProductMappingService srv;
                List<ProductServiceMapping> allProductServiceMappings = null;

                using (ChannelFactory<BT.SaaS.Core.MDMAPI.ProductMappingService.IProductMappingService> factory = new ChannelFactory<BT.SaaS.Core.MDMAPI.ProductMappingService.IProductMappingService>("Primary_EndPoint_ProductServiceMapping"))
                {
                    srv = factory.CreateChannel();
                    allProductServiceMappings = srv.GetAllProductServiceMappings(btRetailId);
                }

                /// This is temp logic until we clean up HE Service code.
                if (mOSISpecificationId != 0 && CheckEmailServiceIsFirstServiceInMDM(pCode))
                {
                    selectedProductServiceMapping = allProductServiceMappings.Find(p => p.ProductId == productId && p.ServiceSpecificationId == mOSISpecificationId);
                }
                else
                {
                    foreach (ProductServiceMapping productServiceMapping in allProductServiceMappings.Where(p => p.ProductId == productId))
                    {
                        selectedProductServiceMapping = productServiceMapping;
                        break;
                    }
                }

            }
            catch (Exception exc)
            {
                MdmException mdmE = new MdmException("Failed to call MDMAPI GetAllProductServiceMappings ", exc);
                throw mdmE;
            }

            // Add to cache
            try
            {
                int cacheRefresh = getCacheRefresh();

                CacheManager cacheManager = (CacheManager)CacheFactory.GetCacheManager();
                cacheManager.Add(productId.ToString(), selectedProductServiceMapping, CacheItemPriority.High, null,
                       new AbsoluteTime(DateTime.Now.AddMinutes(cacheRefresh)));

            }
            catch (Exception exc)
            {
                throw new CacheException("Failed to add to cache: " + exc.Message, exc);
            }

            return selectedProductServiceMapping;
        }

        private static int getCacheRefresh()
        {
            string cacheRefreshString = ConfigurationManager.AppSettings["mdmCacheRefreshTimeMinutes"];
            int cacheRefresh;
            if (!int.TryParse(cacheRefreshString, out cacheRefresh))
            {
                MappingException mE = new MappingException("cacheRefresh '" + cacheRefreshString + "' is not an integer");
                throw mE;
            }
            return cacheRefresh;
        }

        /// <summary>
        /// Method to get Service Instances for a Product Instance Keys
        /// </summary>
        /// <param name="prodInstanceKey">Product Instance Key for which Service Instance keys has to be retreived</param>
        /// <param name="srvcProviderId">Service Provider Id</param>
        /// <returns>List of product Service Instance Mappings</returns>
        public static List<ProductServiceInstanceEx> GetServiceInstancesByProductKey(string prodInstanceKey, int srvcProviderId)
        {
            try
            {
                ProductServiceInstanceEx[] prodServiceInstancesArr = null;
                List<ProductServiceInstanceEx> prodServiceInstancesList = null;

                CustomerServiceClient client = new CustomerServiceClient("BasicHttpBinding_ICustomerService");
                prodServiceInstancesArr = client.GetServiceInstancesByProductKey(prodInstanceKey, srvcProviderId);
                if (prodServiceInstancesArr != null)
                {
                    prodServiceInstancesList = prodServiceInstancesArr.ToList();
                }

                return prodServiceInstancesList;
            }
            catch (Exception exc)
            {
                MdmException mdmE = new MdmException("Failed to call MDMAPI GetServiceInstancesByProductKey ", exc);
                throw mdmE;
            }

        }

        /// <summary>
        /// Method to get the Dependent Domain Products for a WH Products
        /// </summary>
        /// <param name="prodInstanceKey">Product Instance Key for which Domain PIK's has to be retreived</param>
        /// <param name="srvcProviderId">Service Provider Id</param>
        /// <returns>List of product Instance of the Domain Products</returns>
        public static List<ProductDefinationInstance> GetDependentDomainProductsFromPIK(string prodInstanceKey, int srvcProviderId)
        {

            try
            {
                ProductDefinationInstance[] prodDefinationArr = null;
                List<ProductDefinationInstance> prodDefinationList = null;
                CustomerServiceClient client = new CustomerServiceClient("BasicHttpBinding_ICustomerService");
                prodDefinationArr = client.GetProductInstanceDependenciesByProductInstanceKey(prodInstanceKey, srvcProviderId);

                if (prodDefinationArr != null)
                {
                    prodDefinationList = prodDefinationArr.ToList();
                }

                return prodDefinationList;
            }
            catch (Exception exc)
            {
                MdmException mdmE = new MdmException("Failed to call MDMAPI GetProductInstanceDependenciesByProductInstanceKey ", exc);
                throw mdmE;
            }
        }

        public static List<ProductVasClass> getSaaSVASDefs(List<string> vasClassIDList)
        {
            List<ProductVasClass> vasData = new List<ProductVasClass>();

            // We need to call MDM to get the vas definitions
            ProductVasClass[] mdmVASDefinitions = getVASDefinitionsFromMDM(vasClassIDList);

            if (mdmVASDefinitions != null && mdmVASDefinitions.Count() > 0)
            {
                foreach (ProductVasClass productVasClass in mdmVASDefinitions)
                {
                    if (ConfigurationManager.AppSettings.AllKeys.Contains("IsBTR126808enabled") && Convert.ToBoolean(ConfigurationManager.AppSettings["IsBTR126808enabled"]))
                    {
                        if (!(productVasClass.VasProductFamily.Equals("AdvancedSecurity", StringComparison.OrdinalIgnoreCase) || productVasClass.VasProductFamily.Equals("BasicSecurity", StringComparison.OrdinalIgnoreCase)))
                            vasData.Add(productVasClass);
                    }
                    else
                        vasData.Add(productVasClass);
                }
            }

            return vasData;
        }

        private static ProductVasClass[] getVASDefinitionsFromMDM(List<string> vasClassIDList)
        {
            //call to MDM
            ProductVasClass[] mdmVASDefinitions = null;
            try
            {
                ProductServiceClient client = new ProductServiceClient("BasicHttpBinding_IProductService");
                //mdmVASDefinitions = client.GetProductDetailsWithVasClass(vasClassIDList.ToArray());
                //Stuck order fix
                mdmVASDefinitions = client.GetProductDetailsWithVasClass(vasClassIDList.ToArray()).ToArray();
            }
            catch (Exception ex)
            {
                MdmException mdmE = new MdmException("Failed to call MDMAPI GetProductDetailsWithVasClass", ex);
                throw mdmE;
            }

            //Add to cache
            //try
            //{
            //    int cacheRefresh = getCacheRefresh();
            //    CacheManager cacheManager = (CacheManager)CacheFactory.GetCacheManager();
            //    foreach (ProductVasClass pv in mdmVASDefinitions)
            //    {
            //        cacheManager.Add(pv.VasClass, pv, CacheItemPriority.High, null,
            //            new AbsoluteTime(DateTime.Now.AddMinutes(cacheRefresh)));
            //    }
            //}
            //catch (MappingException)
            //{
            //    throw;
            //}
            //catch (Exception ex)
            //{
            //    throw new CacheException("Failed to add to cache" + ex.Message, ex);
            //}

            return mdmVASDefinitions;
        }

        public static ProductVasClass[] GetVasClassDetailsFromPromotions(List<string> sCodes)
        {
            //call to MDM
            ProductVasClass[] mdmVASDeatilsFromPromo = null;
            try
            {
             
                ProductServiceClient client = new ProductServiceClient("BasicHttpBinding_IProductService");
                //mdmVASDeatilsFromPromo = client.GetVasDetailsFromPromotions(sCodes.ToArray());
                //Stuck order fix
                mdmVASDeatilsFromPromo = client.GetVasDetailsFromPromotions(sCodes.ToArray()).ToArray();

            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("mdm..GetVasClassDetailsFromPromotions..exception");
                sb.Append(ex);
                sb.Append(ex.ToString());
                sb.Append(ex.StackTrace);
                sb.Append("Failed to call MDMAPI...exception");
                //File.AppendAllText(ConfigurationManager.AppSettings["MDMLogFilePath"] + "log1.txt", sb.ToString());
                MdmException mdmE = new MdmException("Failed to call MDMAPI GetVasClassDetailsFromPromotions - error " + ex.Message + ex.StackTrace, ex);
                throw mdmE;
            }

            ////Add to cache
            //try
            //{
            //    int cacheRefresh = getCacheRefresh();
            //    CacheManager cacheManager = (CacheManager)CacheFactory.GetCacheManager();
            //    foreach (ProductVasClass pv in mdmVASDeatilsFromPromo)
            //    {
            //        cacheManager.Add(pv.VasClass, pv, CacheItemPriority.High, null,
            //        new AbsoluteTime(DateTime.Now.AddMinutes(cacheRefresh)));
            //    }
            //}
            //catch (MappingException)
            //{
            //    throw;
            //}
            //catch (Exception ex)
            //{
            //    throw new CacheException("Failed to add to cache" + ex.Message, ex);
            //}
                  
           return mdmVASDeatilsFromPromo;
            
        }

        public static ProductPromotionclass[] GetPromotionsDetails(List<string> sCodes)
        {
            //call to MDM
            ProductPromotionclass[] mdmVASDeatilsFromPromo = null;

            try
            {
                //string sCode = string.Join(",", sCodes.ToArray());
                ProductServiceClient client = new ProductServiceClient("BasicHttpBinding_IProductService");
                //mdmVASDeatilsFromPromo = client.GetPromotionProductWithProdCode(sCodes.ToArray());
                //Stuck Order
                mdmVASDeatilsFromPromo = client.GetPromotionProductWithProdCode(sCodes.ToArray()).ToArray();
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("mdm..GetPromotionsDetails..exception");
                //File.AppendAllText(ConfigurationManager.AppSettings["MDMLogFilePath"] + "log1.txt", sb.ToString());
                MdmException mdmE = new MdmException("Failed to call MDMAPI GetVasClassDetailsFromPromotions", ex);
                throw mdmE;
            }

            ////Add to cache
            //try
            //{
            //    int cacheRefresh = getCacheRefresh();
            //    CacheManager cacheManager = (CacheManager)CacheFactory.GetCacheManager();
            //    foreach (ProductVasClass pv in mdmVASDeatilsFromPromo)
            //    {
            //        cacheManager.Add(pv.VasClass, pv, CacheItemPriority.High, null,
            //        new AbsoluteTime(DateTime.Now.AddMinutes(cacheRefresh)));
            //    }
            //}
            //catch (MappingException)
            //{
            //    throw;
            //}
            //catch (Exception ex)
            //{
            //    throw new CacheException("Failed to add to cache" + ex.Message, ex);
            //}

            return mdmVASDeatilsFromPromo;

        }

        public static List<ProductVasClass> GetVasProductsFromPromotions(List<string> scodes)
        {
            List<ProductVasClass> productVASClass = new List<ProductVasClass>();
            List<ProductVasClass> cmpsProductVasClassList = new List<ProductVasClass>();
            Dictionary<string, int> multipleScodes = null;
            try
            {
                productVASClass = MdmWrapper.GetVasClassDetailsFromPromotions(scodes.Distinct().ToList()).ToList();

                if (ConfigurationManager.AppSettings.AllKeys.Contains("IsBTR126808enabled") && Convert.ToBoolean(ConfigurationManager.AppSettings["IsBTR126808enabled"]))
                {
                    if (productVASClass != null && productVASClass.Count() > 0 && productVASClass.Exists(x => (x.VasProductFamily.Equals("AdvancedSecurity", StringComparison.OrdinalIgnoreCase) || x.VasProductFamily.Equals("BasicSecurity", StringComparison.OrdinalIgnoreCase))))
                    {
                        productVASClass.RemoveAll(prod => (prod.VasProductFamily.Equals("AdvancedSecurity", StringComparison.OrdinalIgnoreCase) || prod.VasProductFamily.Equals("BasicSecurity", StringComparison.OrdinalIgnoreCase)));
                    }
                }
                
                //find repeated scodes with count as spring customer can have same spring scode for each of 5 SIM's
                multipleScodes = scodes.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count()-1);

                if (multipleScodes.ToList().Where(d => d.Value > 0).ToList() != null)
                {
                    multipleScodes.ToList().Where(d => d.Value > 0).ToList().ForEach(
                    x =>
                    {
                        productVASClass.Where(y => !string.IsNullOrEmpty(y.PromotionScode) && y.PromotionScode.Equals(x.Key, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(
                            z =>
                            {
                                for (int i = 0; i < x.Value; i++)
                                {
                                    cmpsProductVasClassList.Add(z);
                                }
                            });
                    });
                }
                cmpsProductVasClassList.AddRange(productVASClass);

            }
            catch (MdmException exp)
            {
                throw exp;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                productVASClass = null;
                multipleScodes = null;
            }
            return cmpsProductVasClassList;
        }

    }
}
