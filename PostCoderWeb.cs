using System;
using System.Globalization;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.ServiceModel.Web;
using System.Web;
using Rock;
using Rock.Attribute;
using Rock.Web.UI;
using Rock.Address;
using RestSharp;
using Newtonsoft.Json;

namespace Rock.Address
{
    /// <summary>
    /// The address lookup and geocoding service from <a href="https://www.alliescomputing.com/postcoder/address-lookup">Postcoder Web</a>
    /// </summary>
    [Description("An address verification and geocoding service from Postcoder Web")]
    [Export(typeof(VerificationComponent))]
    [ExportMetadata("ComponentName", "Postcoder Web")]
    [TextField("API Key", "Your Postcoder Web API Key", true, "", "", 2)]
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

        public override bool VerifyLocation( Rock.Model.Location location, bool reVerify, out string result )
        {
            bool verified = false;
            result = string.Empty;

            // Only verify if location is valid, has not been locked, and 
            // has either never been attempted or last attempt was in last 30 secs (prev active service failed) or reverifying
            if ( location != null &&
                !( location.IsGeoPointLocked ?? false ) &&
                (
                    !location.GeocodeAttemptedDateTime.HasValue ||
                    location.GeocodeAttemptedDateTime.Value.CompareTo(RockDateTime.Now.AddSeconds(-30)) > 0 ||
                    reVerify
                ) &&
                //Need to: Cross reference which countries should not be accepted. (location.Country != "GB" || "US") &&
                ( string.IsNullOrWhiteSpace(location.Street1) || string.IsNullOrWhiteSpace(location.Street2) ) )
            {

                string inputKey = GetAttributeValue("APIKey");
                //Create request that encodes correctly
                var addressParts = new string[] { location.Street1, location.Street2, location.City, location.PostalCode };

                string inputAddress = string.Join(" ", addressParts.Where(s => !string.IsNullOrEmpty(s)));

                //restsharp API request
                var client = new RestClient(String.Format("http://ws.postcoder.com/pcw/{0}/addressgeo/{1}/{2}",
                                            inputKey, location.country, inputAddress));
                var request = new RestRequest(Method.GET);
                request.RequestFormat = DataFormat.Json;
                request.AddParameter("lines", "2");
                request.AddHeader("accept", "application/json");
                var response = client.Execute(request);

                if ( response.StatusCode == HttpStatusCode.OK )
                {
                    var addressresponse = JsonConvert.DeserializeObject(response.Content, typeof(List<PostCoder>)) as List<PostCoder>;
                    if ( addressresponse.Any() )
                    {
                        var address = addressresponse.FirstOrDefault();
                        verified = true;
                        result = string.Format("UDPRN: {0}", address.uniquedeliverypointreferencenumber);
                        location.Street1 = address.addressline1;
                        location.Street2 = address.addressline2;
                        location.City = address.posttown;
                        location.State = address.county;
                        location.PostalCode = address.postcode;
                        location.StandardizedDateTime = RockDateTime.Now;
                        location.SetLocationPointFromLatLong(address.latitude, address.longitude);
                        location.GeocodedDateTime = RockDateTime.Now;
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

    }

#pragma warning disable
    public class PostCoder
    {
        public string addressline1 { get; set; }
        public string addressline2 { get; set; }
        public string summaryline { get; set; }
        public string organisation { get; set; }
        public string buildingname { get; set; }
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
