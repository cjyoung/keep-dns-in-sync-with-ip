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
        private bool DEBUG = true; //TODO

        private string USERNAME = "";
        private string PASSWORD = "";
        private string CPANEL_URL = "";
        private string DNS_ENTRY = "";

        private string _my_IP = "";

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
        }

        protected override void OnStart(string[] args)
        {
            _eventLog.WriteEntry("OnStart");

            //TODO: read ip address from domain and set as current

            //TODO - move values to settings

            if (!DEBUG)
            {
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

            string stored_ip = _my_IP;
            string current_ip = GetPublicIPAddress();

            if (stored_ip != current_ip)
            {
                //update local value
                _my_IP = current_ip;

                UpdateDNSRecord();
            }

        }

        private void UpdateDNSRecord()
        {
            _eventLog.WriteEntry("UpdateDNSRecord");

            //TODO

            throw new NotImplementedException();
        }

        protected string GetPublicIPAddress()
        {
            _eventLog.WriteEntry("GetPublicIPAddress");

            string ip_address = "";
            string text_to_parse = "";

            //checkip.dyndns.org
            //Current IP Address: 192.168.1.1
            //be kind and don't request more than once every 10 min

            if (!DEBUG)
            {
                //call dyndns's free service to get current IP address
                HttpWebRequest req = HttpWebRequest.CreateHttp("http://checkip.dyndns.org");
                WebResponse resp = req.GetResponse();
                Stream stream = resp.GetResponseStream();
                StreamReader sr = new StreamReader(stream);
                text_to_parse = sr.ReadToEnd();
            }
            else
            {
                text_to_parse = "Current IP Address: 192.168.1.1";
            }

            //extract IP address from response
            Regex re = new Regex(@"Current IP Address:[^\d]*(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})");
            MatchCollection matches = re.Matches(text_to_parse);
            if (matches.Count >= 1 && matches[0].Groups.Count > 1)
            {
                ip_address = matches[0].Groups[1].Value;
            }

            return ip_address;
        }
    }
}
