using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using com.bt.util.logging;
using System.Configuration;

namespace BT.SaaS.MSEOAdapter
{
    /// <summary>
    /// This class would used to instanticate approriate mapper based on input - SERVICENAME : value
    /// </summary>

    public class ProductFactory
    {

        public E2ETransaction Mye2etxn = null;

        /// <summary>
        /// Factory method that will create object for appropriate mapper class and call the request mapper method
        /// </summary>
        /// <param name="requestOrderRequest"></param>
        public bool Mapperfactory(OrderRequest requestOrderRequest, ref E2ETransaction mye2etxn)
        {
            # region declare
            bool retout = false;
            MessagesFile mf = new MessagesFile(ConfigurationManager.AppSettings["logMessagesFilename"].ToString());
            if (requestOrderRequest.StandardHeader != null && requestOrderRequest.StandardHeader.E2e != null && requestOrderRequest.StandardHeader.E2e.E2EDATA != null && !String.IsNullOrEmpty(requestOrderRequest.StandardHeader.E2e.E2EDATA.ToString()))
                mye2etxn = new E2ETransaction(requestOrderRequest.StandardHeader.E2e.E2EDATA.ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), mf);
            else
                mye2etxn = new E2ETransaction(ConfigurationManager.AppSettings["E2EData"].ToString(), ConfigurationManager.AppSettings["ProjectID"].ToString(), ConfigurationManager.AppSettings["Component"].ToString(), mf);
            # endregion declare


            if (requestOrderRequest.Order.OrderItem[0].Instance[0].InstanceCharacteristic.ToList().Exists(ic => ic.Name.ToUpper() == "SERVICENAME" && (ic.Value.Equals("IP_Voice_Service", StringComparison.OrdinalIgnoreCase))))
            {
                InterfaceRequestProcessor objvoice = new VoiceInfinityRequestProcessor();
                retout = objvoice.RequestMapper(requestOrderRequest, ref mye2etxn);
                retout = true;
            }
            // if you have any new products/services create new class/functionality, create object and call ...
            return retout;
        }
    }
}
