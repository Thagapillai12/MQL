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
    public class ConfigurationBosAttributeMap : ConfigurationElement
    {
        [ConfigurationProperty("attribute", IsRequired = true)]
        public string Attribute
        {
            get
            {
                return this["attribute"] as string;
            }
        }
        [ConfigurationProperty("source", DefaultValue = AttributeMappingSourceValue.None, IsRequired = false)]
        public AttributeMappingSourceValue Source
        {
            get
            {
                return (AttributeMappingSourceValue)this["source"];
            }
        }
        [ConfigurationProperty("type", IsRequired = true)]
        public AttributeMappingTypeValue AttributeMappingType
        {
            get
            {
                return (AttributeMappingTypeValue)this["type"];
            }
        }
        [ConfigurationProperty("value", IsRequired = false)]
        public string Value
        {
            get
            {
                return this["value"] as string;
            }
        }

        [ConfigurationProperty("postProcessing", DefaultValue = AttributeMappingPostProcessingValue.None, IsRequired = false)]
        public AttributeMappingPostProcessingValue PostProcessing
        {
            get
            {
                return (AttributeMappingPostProcessingValue)this["postProcessing"];
            }
        }
    }
}
