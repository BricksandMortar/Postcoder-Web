using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Linq;
using System.Net;


using Newtonsoft.Json;
using RestSharp;

using Rock.Attribute;
using Rock.Address;
using Rock;

namespace com.BricksandMortar.Address
{
    /// <summary>
    /// The address lookup and geocoding service from <a href="https://www.alliescomputing.com/postcoder/address-lookup">Postcoder Web</a>
    /// </summary>
    [Description("An address verification and geocoding service from Postcoder Web")]
    [Export(typeof(VerificationComponent))]
    [ExportMetadata("ComponentName", "Postcoder Web")]
    [TextField("API Key", "Your Postcoder Web API Key", true, "", "", 2)]
    [TextField("Country Codes", "A list of countries that Postcoder Web should verify. Country code values separated by commas. (See https://developers.alliescomputing.com/postcoder-web-api/address-lookup/international-addresses)", true, "GB,US", "", 3)]
    public class PostcoderWeb : VerificationComponent
    {
        /// <summary>
        /// Standardizes and Geocodes an address using the Postcoder Web service
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="reVerify">Should location be reverified even if it has already been succesfully verified</param>
        /// <param name="result">The result code unique to the service.</param>
        /// <returns>
        /// True/False value of whether the verification was successful or not
        /// </returns>

        public override bool VerifyLocation(Rock.Model.Location location, bool reVerify, out string result)
        {
            bool verified = false;
            result = string.Empty;

            string validcountries = GetAttributeValue("CountryCodes");
            string[] validcountryarray = validcountries.Split(',');

            // Only verify if location is valid, has not been locked, and 
            // has either never been attempted or last attempt was in last 30 secs (prev active service failed) or reverifying
            if (VerifyCheck(location, reVerify))
            {

                string inputKey = GetAttributeValue("APIKey");
                //Create request that encodes correctly
                var addressParts = new string[] { location.Street1, location.Street2, location.City, location.State, location.PostalCode };
                string inputAddress = string.Join(" ", addressParts.Where(s => !string.IsNullOrEmpty(s)));

                //Create Identifier 
                string identifier = string.Empty;
                var version = new Version(Rock.VersionInfo.VersionInfo.GetRockSemanticVersionNumber());
                object catalog = string.Empty;
                System.Data.Odbc.OdbcConnectionStringBuilder builder = new System.Data.Odbc.OdbcConnectionStringBuilder(ConfigurationManager.ConnectionStrings["RockContext"].ConnectionString);
                if (builder.TryGetValue("initial catalog", out catalog))
                {
                    identifier = string.Format("Rock:{0} DB:{1}", version, catalog);
                }

                //Use correct endpoint
                string method = string.Empty;
                int lines = 0;
                string country = location.Country;
                if (location.Country == "GB")
                {
                    method = "addressgeo";
                    lines = 2;
                    country = "UK";
                }
                else
                {
                    method = "address";
                    lines = 4;
                }

                //Restsharp API request
                var client = new RestClient(String.Format("http://ws.postcoder.com/pcw/{0}/{1}/{2}/{3}",
                                            inputKey, method, country, inputAddress));
                var request = new RestRequest(Method.GET);
                request.RequestFormat = DataFormat.Json;
                request.AddParameter("lines", lines);
                request.AddParameter("identifier", identifier);
                request.AddHeader("accept", "application/json");
                var response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var addressresponse = JsonConvert.DeserializeObject(response.Content, typeof(List<PostCoder>)) as List<PostCoder>;
                    if (addressresponse.Any())
                    {
                        var address = addressresponse.FirstOrDefault();
                        verified = true;
                        result = string.Format("UDPRN: {0}", address.uniquedeliverypointreferencenumber);
                        if (location.Country == "GB")
                        {
                            location.Street1 = address.addressline1;
                            location.Street2 = address.addressline2;
                        }
                        else
                        {
                            var lineparts = new string[] { address.premise, address.street, address.pobox };
                            string inputline = string.Join(" ", lineparts.Where(s => !string.IsNullOrEmpty(s)));
                            if (!string.IsNullOrWhiteSpace(address.organisation))
                            {
                                location.Street1 = address.organisation;
                                location.Street2 = inputline;
                            }
                            else
                            {
                                location.Street1 = inputline;
                            }

                        }
                        location.City = address.posttown;
                        location.State = address.county;
                        location.PostalCode = address.postcode;
                        location.StandardizedDateTime = RockDateTime.Now;
                        if (method == "addressgeo")
                        {
                            location.SetLocationPointFromLatLong(address.latitude, address.longitude);
                            location.GeocodedDateTime = RockDateTime.Now;
                        }
                    }
                    else
                    {
                        result = "No match.";
                        verified = false;
                    }
                }
                else
                {
                    result = response.StatusDescription;
                }

            }

            location.StandardizeAttemptedServiceType = "PostcoderWeb";
            location.StandardizeAttemptedDateTime = RockDateTime.Now;

            location.GeocodeAttemptedServiceType = "PostcoderWeb";
            location.GeocodeAttemptedDateTime = RockDateTime.Now;

            return verified;
        }
        public bool VerifyCheck(Rock.Model.Location location, bool reVerify)
        {
            // Only verify if location is valid, has not been locked, and 
            // has either never been attempted or last attempt was in last 30 secs (prev active service failed) or reverifying
            if (location != null &&
                !(location.IsGeoPointLocked ?? false) &&
                (
                    !location.GeocodeAttemptedDateTime.HasValue ||
                    location.GeocodeAttemptedDateTime.Value.CompareTo(RockDateTime.Now.AddSeconds(-30)) > 0 ||
                    reVerify
                ) &&
                location.Country == "GB" &&
                (string.IsNullOrWhiteSpace(location.Street1) || string.IsNullOrWhiteSpace(location.Street2)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #pragma warning disable
    public class PostCoder
    {
        public string addressline1 { get; set; }
        public string addressline2 { get; set; }
        public string summaryline { get; set; }
        public string organisation { get; set; }
        public string buildingname { get; set; }
        public string pobox { get; set; }
        public string uniquedeliverypointreferencenumber { get; set; }
        public string premise { get; set; }
        public string street { get; set; }
        public string dependentlocality { get; set; }
        public string posttown { get; set; }
        public string county { get; set; }
        public string postcode { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string grideasting { get; set; }
        public string gridnorthing { get; set; }
        public string number { get; set; }
    }
#pragma warning restore

    }


}
