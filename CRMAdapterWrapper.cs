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
using BT.SaaS.IspssAdapter.CRM;
using System.ServiceModel;

namespace BT.SaaS.IspssAdapter
{
    public sealed class CRMAdapterWrapper
    {
        #region Call CRM
        public static getCustomerResponse1 getCustomerFromCRM(string customerKey)
        {
            getCustomerResponse1 response;

            try
            {
                string billingKey = string.Empty;
                getCustomer getcustomer = new getCustomer();
                getcustomer.getCustomerRequest = new GetCustomerRequest();
                getcustomer.getCustomerRequest.customerKey = customerKey;
                using (ChannelFactory<ManageCRMProviderPortType> crmFactory = new ChannelFactory<ManageCRMProviderPortType>(@"ManageCRMProviderPort"))
                {
                    ManageCRMProviderPortType svcChannel = crmFactory.CreateChannel();
                    response = svcChannel.getCustomer(getcustomer);
                }
            }
            catch (Exception e)
            {
                throw new CrmException("getCustomerFromCRM() failed: " + e.Message.ToString(), e);
            }

            // did we get a valid response
            if (response == null )
            {
                throw new CrmException("Call to CRM adapter for customerKey " + customerKey + " returned a null response");
            }
            else if( response.getCustomerResponse == null )
            {
                throw new CrmException("Call to CRM adapter for customerKey " + customerKey + " returned a null customerResponse");
            }

            // Check if we got an error back from CRM adapter
            if (response.getCustomerResponse.standardHeader != null &&
                response.getCustomerResponse.standardHeader.serviceState != null &&
                response.getCustomerResponse.standardHeader.serviceState.errorCode != null)
            {
                throw new CrmException("Call to CRM adapter returned failure. errorCode: " +
                    response.getCustomerResponse.standardHeader.serviceState.errorCode +
                    "errorDescription: " +
                    response.getCustomerResponse.standardHeader.serviceState.errorDesc);
            }

            if (response.getCustomerResponse.customer == null)
            {
                throw new CrmException("Call to CRM adapter for customerKey " + customerKey + " returned null customer");
            }

            return response;

        }
        #endregion
    }
}
