# Unsupported: Rock Postcoder Web Location Service

## As of 2016-09-30 this is an unsupported plugin

### Intro
This is a location service for [Rock](http://rockrms.com) that looks-up, standardises, and geocodes UK addresses and looks-up and standardises international addresses using the [Postcoder Web](https://www.alliescomputing.com/postcoder/address-lookup) API.

The repository includes the C# source for use with the [Rockit SDK](http://www.rockrms.com/Rock/Developer). 
To download the latest release of the plugin in .dll format click [here](https://github.com/arranf/https://github.com/arranf/PostcoderWebLocationService/releases/latest).

### A Quick Explanation
This location service checks the country of the address being verified. If it is a UK address it will pass the values (if any are present) of the address line 1, address line 2, city, state, and postal code fields from Rock to Postcoder Web. It will then ask for the best match and, if values are present in the response, it will then create a two line address and replace the values stored in Rock with the response values including longitude and latitude. 

If the address is not a UK address it will do the same as above but will not geocode the address.

### Supported Countries
To check if a country is supported by this plugin please check to see if the country is listed [here](https://developers.alliescomputing.com/postcoder-web-api/address-lookup/international-addresses).

### Closing
If anything looks broken, flag up an issue. 

This project is licensed under the [Apache 2.0 License](http://www.apache.org/licenses/LICENSE-2.0.html).
