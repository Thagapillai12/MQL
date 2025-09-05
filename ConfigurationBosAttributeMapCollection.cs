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
    public class ConfigurationBosAttributeMapCollection : ConfigurationElementCollection
    {
        new public ConfigurationBosAttributeMap this[string key]
        {
            get
            {
                return base.BaseGet(key) as ConfigurationBosAttributeMap;
            }
        }

        public ConfigurationBosAttributeMap this[int index]
        {
            get
            {
                return base.BaseGet(index) as ConfigurationBosAttributeMap;
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
            return new ConfigurationBosAttributeMap();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ConfigurationBosAttributeMap)element).Attribute;
        }
    }
}
