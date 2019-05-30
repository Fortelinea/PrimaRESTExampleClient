# PrimaRESTExampleClient
Example client for connecting to Prima REST Service, authenticating, and retrieving data.

## Prima REST Service
Allows authorized external applications to query and modify Prima data. Each installation of Prima at client sites/laboratories will include an instance of this Prima REST Service that is only accessible to applications at those client sites/laboratories.

## Getting Started
The examples below and in this repository's source code show a good way to get started using the Prima REST Service, but more information can be found about using the service on the Swagger UI page and on the installed service running at the client site.

## Authentication/Authorization
The Prima REST Service implements the [OAuth2](https://oauth.net/2/) and [OpenId](https://openid.net/) protocols for authorization and authentication.

### Example authentication
Below is example code (also present in the example client) for using the OAuth2 Resource Owner Password flow to fetch an access token and a refresh token. Other OAuth2 flows are also available, depending on client application's use case. Other examples are also present in the example client source code.

```c#
var tokenRequestParameters = new Dictionary<string, string>
                             {
                                 {"grant_type", "password"},
                                 {"scope", "api_read_all offline_access"},
                                 {"client_id", "<clientId>"},
                                 {"client_secret", "<secret>"},
                                 {"username", "<username>"},
                                 {"password", "<password>"}
                             };

var client = new HttpClient();
var content = new FormUrlEncodedContent(tokenRequestParameters);
var authenticationUri = new Uri("https://<your-prima-rest-service-hostname-here>/connect/token");
var response = await client.PostAsync(authenticationUrl, content);
//Expected response: {"access_token":"<token>","expires_in":3600,"token_type":"Bearer","refresh_token":"<refresh_token>"}
var tokenResponse = await response.Content.ReadAsStringAsync();
```

In this example the scopes "api_read_all" and "offline_access" are requested. "api_read_all" allows authorized clients to make GET calls to the Prima REST Service but not write (POST/PUT/PATCH/DELETE) any data. "offline_access" requests that a refresh token be included in the token response.

Subsequent calls to the API can use the access token and token type from this response in the HTTP Request Authorization header like so:

```c#
var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "<token>");
```
Depending on configuration, the access token may also expire after a certain period of time. If refresh tokens are used, the refresh token can be used in lieu of user credentials to get an updated access token like so:

```c#
var tokenRequestParameters = new Dictionary<string, string>
			 {
			     {"grant_type", "refresh_token"},
			     {"client_id", clientId},
			     {"client_secret", clientSecret},
			     {"refresh_token", refreshToken}
			 };

var client = new HttpClient();
var content = new FormUrlEncodedContent(tokenRequestParameters);
var authenticationUri = new Uri("https://<your-prima-rest-service-hostname-here>/connect/token");
var response = await client.PostAsync(authenticationUrl, content);
//Expected response: {"access_token":"<token>","expires_in":3600,"token_type":"Bearer"}
var tokenResponse = await response.Content.ReadAsStringAsync();
```

## Reading data
The Prima REST Service uses the [OData](https://www.odata.org/) Protocol to allow client applications to easily query and filter data. Currently the API does not support use of the OData select/expand functionality.

### Example reading data
Below is example code (also present in the example client) for fetching information about a slide with specific encoded barcode data.

```c#
var apiUri = new Uri("https://<your-prima-rest-service-hostname-here>/api/v1");
var barcodeContent = "5*2U*UM9*1"; //example barcode content for a given slide
var getSlideByBarcodeUri = new Uri(apiUri, $"Slide?$filter=barcodeContent eq '{barcodeContent}'");

var client = new HttpClient();

//include bearer token in headers
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "<token>");

//sends a GET request to https://<your-prima-rest-service-hostname-here>/api/v1/Slide?$filter=barcodeContent eq '5*2U*UM9*1'
var response = await client.GetAsync(getSlideByBarcodeUri);
var responseString = await response.Content.ReadAsStringAsync();
```

The slide response should look like:
```json
{
	"@odata.context": "https://<your-prima-rest-service-hostname-here>/api/v1/$metadata#Slide",
	"value": [{
		"@odata.type": "#Fortelinea.Prima.Services.REST.V1.Models.CytoSlide",
		"alternateIdentifier": "",
		"barcodeContent": "5*2U*UM9*1",
		"caseBaseId": 5573,
		"currentBatchId": 300,
		"printStatus": "Verified",
		"isRadioActive": false,
		"orderedStatus": "None",
		"priorityLevelId": 1,
		"qcStatus": "NoIssues",
		"savedIdentifier": "S19-1256:D-C10",
		"surgicalSerialPart": 10,
		"id": 39680,
		"dilutionFactor": null,
		"handStain": false,
		"isCharged": true,
		"stainTestId": 474,
		"cytoSpecimenId": 6258,
		"protocolCytoSlideId": null,
		"currentLocation": null,
		"nextLocation": null
	}]
}
```

We can see plenty of data about this slide - for example, it has surgical number ```"savedIdentifier": "S19-1256:D-C10",```. We can also see some foreign key relationships, like ```"stainTestId": 474```. We could look up the title for this stain by hitting the StainTests endpoint. A list of all of the endpoints is available on the Swagger UI page (or Swagger Json document) which can be found at ```https://<your-prima-rest-service-hostname-here>/swagger/index.html```.

We can also see from the field ```"@odata.type": "#Fortelinea.Prima.Services.REST.V1.Models.CytoSlide"``` that this slide is a CytoSlide (a slide from a cytology/fluid specimen.) If we want to specifically query CytoSlides or another specific slide type (and properly search/filter using type-specific properties) we can use the specific CytoSlides REST endpoint. 

Examples can be found in the source code for grabbing case and study data from a slide. Similar steps can be taken for other barcoded items like cassettes/blocks and specimens. For more information about different types of barcoded items tracked by Prima, visit the installed client's Prima web site.
