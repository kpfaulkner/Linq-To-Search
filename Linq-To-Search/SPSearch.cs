using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data;

namespace LinqTest
{
    class SearchResult
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string URL { get; set; }

        public string Query { get; set; }
    }

    class SPSearch
    {
        private string uid;
        private string pwd;
        private string dom;

        public SPSearch()
        {
            //Read Credentials from App.config
            uid = ConfigurationManager.AppSettings.Get("ServiceUID").ToString();
            pwd = ConfigurationManager.AppSettings.Get("ServicePWD").ToString();
            dom = ConfigurationManager.AppSettings.Get("ServiceDomain").ToString();


        }


        private IEnumerable< DynamicDictionary > GenerateResults(DataTable table)
        {
            var l = new List<DynamicDictionary>();

            foreach (DataRow row in table.Rows)
            {

                dynamic entry = new DynamicDictionary();
                
                // go through each element.
                foreach (var i in row.Table.Columns)
                {
                    // store into dictionary....
                    var colName = i.ToString();
                    entry[colName.ToLower() ] = row[colName];
                }

                l.Add( entry );

            }

            return l;
        }

        public IEnumerable<DynamicDictionary> PerformSearch(string queryRequest)
        {

            IEnumerable<DynamicDictionary> results = new List<DynamicDictionary>();

            var qs = new spsearch.QueryServiceSoapClient();

            qs.ClientCredentials.Windows.ClientCredential = new System.Net.NetworkCredential(uid, pwd, dom);

            //Enable ImpersonationLevel and ntlm
            qs.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
            qs.ClientCredentials.Windows.AllowNtlm = true;

            //Verify that the Search Server is up and running
            if (string.Compare(qs.Status(), "ONLINE", true) != 0)
            {
                throw new Exception("The Search Server is not available online.");

            }

            var queryResults = new System.Data.DataSet();
            //Call SharePoint Search Web Service method with XML Query
            try
            {
                queryResults = qs.QueryEx(queryRequest);

                var table = queryResults.Tables[0];


                results = GenerateResults(table);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error !! {0}", ex.Message);
            }

            return results;

        }

    }
}
