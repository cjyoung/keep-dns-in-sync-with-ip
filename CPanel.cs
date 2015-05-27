using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Collections;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace keep_dns_in_sync_with_ip
{
    class CPanel
    {
        protected enum Module //case sensitive
        {
            DomainLookup,
            ZoneEdit
        };

        protected enum Function //case sensitive
        {
            getmaindomain,
            fetchzone,
            edit_zone_record
        };

        //TODO - move these to settings
        private string USERNAME = "";
        private string PASSWORD = "";
        private string CPANEL_URL = "";

        public CPanel()
        {

        }

        protected string ExecuteQuery(Module module, Function function, Dictionary<string, string> addl_params = null)
        {
            string parameters = "";
            if (null != addl_params)
            {
                foreach (string key in addl_params.Keys)
                {
                    parameters += string.Format("{0}={1}&", WebUtility.UrlEncode(key), WebUtility.UrlEncode(addl_params[key]));
                }
            }

            HttpWebRequest req = HttpWebRequest.CreateHttp(string.Format("{0}/json-api/cpanel?cpanel_jsonapi_user=user&cpanel_jsonapi_apiversion=2&cpanel_jsonapi_module={1}&cpanel_jsonapi_func={2}&{3}",
                CPANEL_URL,
                System.Enum.GetName(typeof(Module), module),
                System.Enum.GetName(typeof(Function), function),
                parameters
                ));

            // !!! Warning - use SSL if possible to encrypt since Basic auth is used !!!
            string auth = "Basic " + Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(USERNAME + ":" + PASSWORD));
            req.PreAuthenticate = true;
            req.AuthenticationLevel = System.Net.Security.AuthenticationLevel.MutualAuthRequested;
            req.Headers.Add("Authorization", auth);

            WebResponse r = req.GetResponse();
            Stream stream = r.GetResponseStream();
            StreamReader sr = new StreamReader(stream);
            string response = sr.ReadToEnd();

            return response;
        }

        public DNSEntry GetDNSEntry(string domain, string dns_entry)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("domain", domain);

            string response = ExecuteQuery(Module.ZoneEdit, Function.fetchzone, parameters);

            dynamic data = JsonConvert.DeserializeObject<dynamic>(response);

            var result = from i in (IEnumerable<dynamic>)data.cpanelresult.data[0].record
                         where i.name == (dns_entry + ".")
                         select i;

            DataContractJsonSerializer sr = new DataContractJsonSerializer(typeof(DNSEntry));
            MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(result.FirstOrDefault().ToString()));
            return (DNSEntry)sr.ReadObject(ms);
        }

        internal void UpdateDNSEntry(string domain, DNSEntry dns_entry)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("domain", domain);
            parameters.Add("line", dns_entry.line);
            parameters.Add("class", dns_entry.class_type);
            parameters.Add("type", dns_entry.type);
            parameters.Add("name", dns_entry.name);
            parameters.Add("ttl", dns_entry.ttl);
            parameters.Add("address", dns_entry.address);

            ExecuteQuery(Module.ZoneEdit, Function.edit_zone_record, parameters);
        }

        [DataContract]
        public class DNSEntry
        {
            [DataMember(Name = "record")]
            public string record;
            [DataMember(Name = "type")]
            public string type;
            [DataMember(Name = "address")]
            public string address;
            [DataMember(Name = "ttl")]
            public string ttl;
            [DataMember(Name = "name")]
            public string name;
            [DataMember(Name = "line")]
            public string line;
            [DataMember(Name = "Line")]
            public string Line;
            [DataMember(Name = "class")]
            public string class_type; 
        }
    }
}
