using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Configuration;
using System.IO;
using System.Xml.Serialization;
using BT.SaaS.Core.MDMAPI.Entities.Product;
using SAASPE = BT.SaaS.Core.Shared.Entities;
using BT.SaaS.IspssAdapter;
using com.bt.util.logging;
using System.Security.Cryptography.X509Certificates;

namespace BT.SaaS.MSEOAdapter
{
    public sealed class CmpsWrapper
    {
        public static CustomerAssetsResponse GetCustomerAssets(string billNumber,ref E2ETransaction e2eData)
        {
            const string StartedCMPSCall = "StartedCMPSCall";
            const string GotResponseFromCMPSWithBusinessError = "GotResponseFromCMPSWithBusinessError";
            const string GotResponseFromCMPS = "GotResponseFromCMPS";
            const string DownStreamError = "DownStreamError";
            Guid APIGWTracker = Guid.NewGuid();
            try
            {
                HttpWebRequest webrequest;
                CustomerAssetsResponse assetResponse;
                //string uri = ConfigurationManager.AppSettings["cmpsuri"].ToString() + "/" + billNumber + "/" + "assets.xml?journey=consumer&v=5.0";
               // string uri = (ConfigurationManager.AppSettings["CmpsHead"].ToString() +"&"+ ConfigurationManager.AppSettings["CmpsTail"].ToString()).Replace("{BillingAccountNumber}", billNumber);
                string uri = (ConfigurationManager.AppSettings["CmpsHead"].ToString() + "&" + ConfigurationManager.AppSettings["CmpsTail"].ToString()).Replace("{billingaccounts}", billNumber);

                string responseXml;
                webrequest = (HttpWebRequest)WebRequest.Create(uri);
                webrequest.Method = "GET";
                webrequest.ContentType = "application/xml ";
                webrequest.Accept = "application/xml ";
                webrequest.Headers.Add("e2eData", e2eData.toString());
                webrequest.Headers.Add("Channel", "MSEO");
                webrequest.Headers.Add("APIGW-Tracking-Header", APIGWTracker.ToString());
                webrequest.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["CMPSTimeOut"].ToString());

                var clientCertificatePath = ConfigurationManager.AppSettings["APIGWcertpath"].ToString();
                string password= ConfigurationManager.AppSettings["APIGWcertpassword"].ToString();
                X509Certificate2 certificate = new X509Certificate2(clientCertificatePath, password);
                webrequest.ClientCertificates.Add(certificate);

                e2eData.logMessage(StartedCMPSCall, "Started...CMPS GetCustomerAssets call");
                e2eData.startOutboundCall(ConfigurationManager.AppSettings["destinationCMPS"], E2ETransaction.vREQ,"CMPS");
                using (HttpWebResponse webResponse = (HttpWebResponse)webrequest.GetResponse())
                {
                    using (StreamReader readStream = new StreamReader(webResponse.GetResponseStream()))
                    {
                        responseXml = readStream.ReadToEnd();
                        LogHelper.LogActivityDetails(billNumber, APIGWTracker, "GetCustomerAssets Response:", TimeSpan.Zero, "ESBCallsTrace", uri + " : " + responseXml.ToString());
                        readStream.Close();
                    }
                    webResponse.Close();
                }

                using (StringReader reader = new StringReader(responseXml))
                {
                    XmlSerializer serailizer = new XmlSerializer(typeof(CustomerAssetsResponse));
                    assetResponse = (CustomerAssetsResponse)serailizer.Deserialize(reader);
                    reader.Close();
                }

                e2eData.logMessage(GotResponseFromCMPS,"");
                string E2EData = E2ETransaction.getSimulatedReply(e2eData.toString(), E2ETransaction.vRES, null);
                e2eData.endOutboundCall(E2EData);
                return assetResponse;
                
            }
            catch (WebException cmpsEx)
            {
                if (cmpsEx.Response != null)
                {
                    string statusCode = string.Empty;
                    string responseXml = string.Empty;
                    using (HttpWebResponse errorResponse = (HttpWebResponse)cmpsEx.Response)
                    {
                        statusCode = errorResponse.StatusCode.ToString();
                        using (StreamReader readStream = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            responseXml = readStream.ReadToEnd();
                            LogHelper.LogActivityDetails(billNumber, APIGWTracker, "GetCustomerAssets Response:", TimeSpan.Zero, "ESBCallsTrace", responseXml.ToString());
                            readStream.Close();
                        }
                        errorResponse.Close();
                    }
                    //e2eData.businessError(GotResponseFromCMPSWithBusinessError, "CMPSException: " + statusCode + " : " + responseXml);
                    e2eData.downstreamError(DownStreamError, "CMPSException: " + statusCode + " : " + responseXml);
                    string E2EData = E2ETransaction.getSimulatedReply(e2eData.toString(), E2ETransaction.vRES, null);
                    e2eData.endOutboundCall(E2EData);
                    throw new CMPSException("CMPSException: apigwtracker " + APIGWTracker + " " + statusCode + " : " + responseXml);
                }
                else
                {
                    //e2eData.businessError(GotResponseFromCMPSWithBusinessError, "CMPSException: " + cmpsEx.Message);
                    e2eData.downstreamError(DownStreamError, "CMPSException: " + cmpsEx.Message);
                    string E2EData = E2ETransaction.getSimulatedReply(e2eData.toString(), E2ETransaction.vRES, null);
                    e2eData.endOutboundCall(E2EData);
                    throw new CMPSException("CMPSException: apigwtracker " + APIGWTracker + " " + cmpsEx.Message);
                }
            }
            catch (Exception cmpsException)
            {
                LogHelper.LogActivityDetails(billNumber, APIGWTracker, "GetCustomerAssets Response:", TimeSpan.Zero, "ESBCallsTrace", cmpsException.ToString());
                e2eData.businessError(GotResponseFromCMPSWithBusinessError, "CMPSException: " + cmpsException.ToString());
                string E2EData = E2ETransaction.getSimulatedReply(e2eData.toString(), E2ETransaction.vRES, null);
                e2eData.endOutboundCall(E2EData);
                throw new CMPSException("CMPSException: apigwtracker " + APIGWTracker + " "+ cmpsException.Message.ToString());
            }

        }
        public static List<ProductVasClass> ProductVasClassList(CustomerAssetsResponse assetResponse, ref List<string> CMPSCodes)
        {
            List<ProductVasClass> prodVASClassList = new List<ProductVasClass>();
            List<ProductPromotionclass> prodPromotionList = new List<ProductPromotionclass>();
            List<string> scodes = new List<string>();
            List<string> basePromotionList = new List<string>();
            CMPSCodes = new List<string>();
            try
            {
                if (assetResponse != null && assetResponse.customerAccount != null && assetResponse.customerAccount.productInstanceList != null && assetResponse.customerAccount.productInstanceList.Count() > 0)
                {
                    assetResponse.customerAccount.productInstanceList.ToList().ForEach(x =>
                    {
                        if (x.productName.code != null)
                        {
                            scodes.Add(x.productName.code.ToString());
                        }
                    });
                }
                //This we need to take from prod F:\App_Logs\MSEOLogs\ESBLogs\APIGWMessage.csv based on BAc we need to take the scodes and debug
                //scodes.Add("S0349013");
                //scodes.Add("S0386648");
                //scodes.Add("S0468488");
                //scodes.Add("S0493303");
                //scodes.Add("S0386644");
                //scodes.Add("S0204592");
                //scodes.Add("S0493304");
                //scodes.Add("S0349013");
                //scodes.Add("S0204305");
                //scodes.Add("S0526484");
                //Null reference
                //scodes.Add("S0204592");
                //scodes.Add("S0204305");
                //scodes.Add("S0571661");
                //scodes.Add("S0571661");
                //scodes.Add("S0468476");
                //scodes.Add("S0442568");
                //scodes.Add("S0571663");
                //scodes.Add("S0143533");
                //Value can't be null
                //scodes.Add("S0539775");
                //scodes.Add("S0331992");
                //scodes.Add("S0393471");
                //scodes.Add("S0427454");
                //scodes.Add("S0374345");
                //scodes.Add("S0356313");
                //scodes.Add("S0204305");
                //scodes.Add("S0427457");
                //scodes.Add("S0386648");
                //scodes.Add("S0356305");
                //scodes.Add("S0386644");
                //scodes.Add("S0347205");
                //scodes.Add("S0143533");
                //scodes.Add("S0445191");
                //scodes.Add("S0529238");
                //scodes.Add("S0336359");
                //scodes.Add("S0442569");
                //scodes.Add("S0527430");
                //scodes.Add("S0527284");
                //scodes.Add("S0349020");
                //scodes.Add("S0331991");
                //scodes.Add("S0395364");
                //scodes.Add("S0318953");
                //scodes.Add("S0527465");
                //scodes.Add("S0360097");
                //scodes.Add("S0539725");
                //scodes.Add("S0200791");
                //scodes.Add("S0359801");
                //scodes.Add("S0527264");
                //scodes.Add("S0388105");
                //scodes.Add("S0349015");
                //scodes.Add("S0146404");
                //scodes.Add("S0143533");
                
                if (scodes.Count > 0)
                {

                    CMPSCodes.Add(string.Join(",", scodes.ToArray()));
                   
                    //ccp 78 - MD2
                    bool isNOFSCodeAvailable = isNOFScode(scodes);

                    if (!isNOFSCodeAvailable && Convert.ToBoolean(ConfigurationManager.AppSettings["IsTariffCodeRequired"]))
                    {
                        // MDM call with CMPS SCODES
                        prodPromotionList = MdmWrapper.GetPromotionsDetails(scodes).ToList();
                        if (prodPromotionList != null && prodPromotionList.Count() > 0)
                        {
                            foreach (ProductPromotionclass promotion in prodPromotionList)
                            {
                                if (!string.IsNullOrEmpty(promotion.TariffCode) && promotion.TariffCode.Equals("UN", StringComparison.OrdinalIgnoreCase))
                                    basePromotionList.Add(promotion.ProductCode);
                            }
                        }
                        else
                            throw new CMPSException("There are no definations in MDM for the follwing Scodes returned from CMPS " + string.Join(",", scodes.ToArray()));

                        if (basePromotionList != null && basePromotionList.Count() > 0)
                            prodVASClassList = MdmWrapper.GetVasProductsFromPromotions(basePromotionList).ToList();
                        else
                            throw new CMPSException("CMPS Exception: " + " no basePromotion scodes returned from CMPS asset response");
                    }
                    else
                    {
                        prodVASClassList = MdmWrapper.GetVasProductsFromPromotions(scodes).ToList();
                    }

                }
                else
                {
                    throw new CMPSException("CMPS Exception: " + " no scodes returned from CMPS asset response");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                scodes = null;
            }
            return prodVASClassList;
        }

        //ccp 78 - MD2
        public static bool isNOFScode(List<string> scodes)
        {

            var NOFScodedata = System.IO.File.ReadAllLines(System.Configuration.ConfigurationManager.AppSettings["CSVFilePathForNOFScode"]);
            bool NOFScode = false;
            foreach(string scode in scodes)
            {
                if(NOFScodedata.Contains(scode))
                {
                    NOFScode = true;
                    break;
                }
            }           
            return NOFScode;
        }
    }
}
