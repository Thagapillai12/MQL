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
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using System.Globalization;

namespace BT.SaaS.IspssAdapter.BOS_API
{
    using System.Xml.Serialization;


    public partial class placeOrder
    {

        #region SerializeObject
        /// <summary>
        /// This method returns an xml representation of the object
        /// </summary>
        /// <returns>The xml resprestation as Srting</returns>
        public string SerializeObject()
        {
            StringBuilder serialBuilder;
            StringWriter serialWriter;

            XmlSerializer messageSerializer;
            try
            {
                serialBuilder = new StringBuilder(1000);

                serialWriter = new StringWriter(serialBuilder, CultureInfo.InvariantCulture);

                messageSerializer = new XmlSerializer(this.GetType());
                //serialize the object
                messageSerializer.Serialize(serialWriter, this);



                //return the string
                return serialWriter.ToString();
            }

            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                serialBuilder = null;
                serialWriter = null;
                messageSerializer = null;
            }
        }
        #endregion

        private bool existingCustomer = false;
        private string customerKey = null;
        private int customerId = -1;
        private bool isORTCheckRequired = false;
        public bool isExistingCustomer()
        {
            if (this.customerId == -1)
            {
                if (this.customerKey == null)
                    getCustomerKey();

                this.customerId = MdmWrapper.getCustomerIdByCustomerKey(customerKey);

                if (this.customerId == 0)
                {
                    this.existingCustomer = false;
                }
                else
                {
                    this.existingCustomer = true;
                }
            }
            return this.existingCustomer;
        }

        public string getCustomerKey()
        {
            if (this.customerKey == null)
            {
                if (this.AccountAndOrder == null)
                {
                    throw new MappingException("No AccountAndOrder section in BOS order");
                }
                if (this.AccountAndOrder.ISPAccountData == null)
                {
                    throw new MappingException("No AccountAndOrder.ISPAccountData section in BOS order");
                }

                this.customerKey = Mapper.value(this.AccountAndOrder.ISPAccountData.BESServiceAccountId);
                if (this.customerKey == null)
                {
                    throw new MappingException("No CustomerKey (BESServiceAccountId) in BOS order");
                }
            }
            return this.customerKey;
        }

        public int getCustomerId()
        {
            if (customerId == -1)
                isExistingCustomer();

            return (this.customerId );
        }

        public bool IsORTCheckRequired
        {
            get
            {
                return isORTCheckRequired;
            }
            set
            {
                isORTCheckRequired = value;
            }
        }
    }

}