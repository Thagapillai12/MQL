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
    public class ConfigurationBosProductAttributeMapCollection : ConfigurationElementCollection
    {
        new public ConfigurationBosProductAttributeMap this[string key]
        {
            get
            {
                return base.BaseGet(key) as ConfigurationBosProductAttributeMap;
            }

        }

        public ConfigurationBosProductAttributeMap this[int index]
        {
            get
            {
                return base.BaseGet(index) as ConfigurationBosProductAttributeMap;
            }
            set
            {
                if (base.BaseGet(index) != null)
                {
                    base.BaseRemoveAt(index);
                }
                this.BaseAdd(index, value);
            }
        }
        protected override ConfigurationElement CreateNewElement()
        {
            return new ConfigurationBosProductAttributeMap();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ConfigurationBosProductAttributeMap)element).Product;
        }
    }
}
