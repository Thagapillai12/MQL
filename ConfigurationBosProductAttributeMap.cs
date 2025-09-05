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

namespace BT.SaaS.IspssAdapter
{
    public class ConfigurationBosProductAttributeMap : ConfigurationElement
    {
        [ConfigurationProperty("product", IsRequired = true)]
        public string Product
        {
            get
            {
                return this["product"] as string;
            }
        }
        [ConfigurationProperty("attributeMap", IsRequired = true)]
        public ConfigurationBosAttributeMapCollection AttributeMap 
        {
            get
            {
                return this["attributeMap"] as ConfigurationBosAttributeMapCollection;
            }
        }

    }
}
