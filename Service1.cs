using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace keep_dns_in_sync_with_ip
{
    public partial class DNS_IP_Sync_Service : ServiceBase
    {
        private bool DEBUG = false;

        //TODO - move these to settings
        private string DOMAIN = "";
        private List<string> DNS_NAMES = new List<string>() { "" };

        private CPanel _cPanel;

        System.Timers.Timer _timer = new System.Timers.Timer();
        private System.Diagnostics.EventLog _eventLog;

        public DNS_IP_Sync_Service()
        {
            InitializeComponent();

            _eventLog = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("MySource"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "MySource", "MyNewLog");
            }
            _eventLog.Source = "MySource";
            _eventLog.Log = "MyNewLog";

            //instanciate cpanel object
            _cPanel = new CPanel();
        }

        protected override void OnStart(string[] args)
        {
            _eventLog.WriteEntry("OnStart");

            if (!DEBUG)
            {
                //TODO - move values to settings
                int poll_frequency_in_min = 15; //15 minutes
                int poll_frequency = poll_frequency_in_min * 60 * 1000;

                _timer.Interval = poll_frequency;
                _timer.Elapsed += new System.Timers.ElapsedEventHandler(this.CheckForIPUpdate);
                _timer.Start();
            }
            else
            {
                CheckForIPUpdate(this, null);
            }
        }

        protected override void OnStop()
        {
            _eventLog.WriteEntry("OnStop");

            _timer.Stop();
        }

        private void CheckForIPUpdate(object sender, System.Timers.ElapsedEventArgs args)
        {
            _eventLog.WriteEntry("CheckForIPUpdate");

            IPAddress server_ip = GetPublicIPAddress();

            foreach (string dns_name in DNS_NAMES)
            {
                CPanel.DNSEntry dns_entry = _cPanel.GetDNSEntry(DOMAIN, dns_name);
                IPAddress dns_ip;
                bool valid_dns_ip = IPAddress.TryParse(dns_entry.address, out dns_ip);

                if ((valid_dns_ip) && (dns_ip.ToString() != server_ip.ToString()))
                {
                    UpdateDNSRecord(dns_entry, server_ip);
                }
            }
        }

        private void UpdateDNSRecord(CPanel.DNSEntry dns_entry, IPAddress new_ip)
        {
            _eventLog.WriteEntry("UpdateDNSRecord");

            dns_entry.address = new_ip.ToString();

            _cPanel.UpdateDNSEntry(DOMAIN, dns_entry);
        }

        /// <summary>
        /// Calls out to DynDNS's public IP service that echos your public IP address
        /// </summary>
        /// <returns>The public IP address of the system</returns>
        protected IPAddress GetPublicIPAddress()
        {
            _eventLog.WriteEntry("GetPublicIPAddress");

            IPAddress ip_address = IPAddress.Parse("127.0.0.1");
            string text_to_parse = "";

            //checkip.dyndns.org
            //Current IP Address: 192.168.1.1
            //be kind and don't request more than once every 10 min

            //if (!DEBUG)
            //{

            //call dyndns's free service to get current IP address
            HttpWebRequest req = HttpWebRequest.CreateHttp("http://checkip.dyndns.org");
            WebResponse resp = req.GetResponse();
            Stream stream = resp.GetResponseStream();
            StreamReader sr = new StreamReader(stream);
            text_to_parse = sr.ReadToEnd();

            //}
            //else
            //{
            //    text_to_parse = "Current IP Address: 192.168.1.1";
            //}

            //extract IP address from response
            Regex re = new Regex(@"Current IP Address:[^\d]*(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})");
            MatchCollection matches = re.Matches(text_to_parse);
            if (matches.Count >= 1 && matches[0].Groups.Count > 1)
            {
                if (!IPAddress.TryParse(matches[0].Groups[1].Value, out ip_address))
                {
                    _eventLog.WriteEntry(string.Format("Error: could not parse IP address ({0})", matches[0].Groups[1].Value));
                }
            }
            else
            {
                _eventLog.WriteEntry(string.Format("Error: could not find IP address in response ({0})", text_to_parse));
            }

            return ip_address;
        }
    }
}
