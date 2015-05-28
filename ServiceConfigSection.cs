using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keep_dns_in_sync_with_ip
{
    public class ServiceConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("cPanel")]
        public CPanelElement CPanel
        {
            get { return (CPanelElement)this["cPanel"]; }
            set { this["cPanel"] = value; }
        }

        [ConfigurationProperty("pollFrequencyInMinutes", DefaultValue = 15)]
        public int PollFrequency
        {
            get { return (int)this["pollFrequencyInMinutes"]; }
            set { this["pollFrequencyInMinutes"] = value; }
        }

    }

    public class CPanelElement : ConfigurationElement
    {
        [ConfigurationProperty("domain")]
        public GenericConfigElement Domain
        {
            get { return (GenericConfigElement)this["domain"]; }
            set { this["domain"] = value; }
        }

        [ConfigurationProperty("username")]
        public GenericConfigElement Username
        {
            get { return (GenericConfigElement)this["username"]; }
            set { this["username"] = value; }
        }

        [ConfigurationProperty("password")]
        public GenericConfigElement Password
        {
            get { return (GenericConfigElement)this["password"]; }
            set { this["password"] = value; }
        }

        [ConfigurationProperty("cPanelURL")]
        public GenericConfigElement CPanelURL
        {
            get { return (GenericConfigElement)this["cPanelURL"]; }
            set { this["cPanelURL"] = value; }
        }

        [ConfigurationProperty("DNSNames")]
        [ConfigurationCollection(typeof(GenericConfigElement), AddItemName = "DNSName")]
        public DNSNames DNSNames
        {
            get { return (DNSNames)this["DNSNames"]; }
            set { this["DNSNames"] = value; }
        }
    }


    /// <summary>
    /// give an easy to use alias by inheriting
    /// </summary>
    public class DNSName : GenericConfigElement
    {

    }

    public class GenericConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("value", IsRequired = true)]
        public string Value
        {
            get { return (string)this["value"]; }
            set { this["value"] = value; }
        }
    }

    public class DNSNames : ConfigurationElementCollection
    {
        public DNSName this[int index]
        {
            get
            {
                return base.BaseGet(index) as DNSName;
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

        public new DNSName this[string responseString]
        {
            get { return (DNSName)BaseGet(responseString); }
            set
            {
                if (BaseGet(responseString) != null)
                {
                    BaseRemoveAt(BaseIndexOf(BaseGet(responseString)));
                }
                BaseAdd(value);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new DNSName();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((DNSName)element).Value;
        }
    }
}
