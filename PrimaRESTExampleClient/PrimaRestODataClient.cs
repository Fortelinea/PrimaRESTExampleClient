using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Simple.OData.Client;

namespace PrimaRESTExampleClient
{
    public class PrimaRestODataClient
    {
        private readonly ODataClient _client;

        public PrimaRestODataClient(Uri serverBaseUri, string accessToken)
        {
            // Uses nuget package Simple.OData.Client
            // https://www.odata.org/blog/advanced-odata-tutorial-with-simple-odata-client/
            _client = new ODataClient(new ODataClientSettings
                                      {
                                          BaseUri = new Uri(serverBaseUri, "api/v1/"),
                                          BeforeRequest = message => message.Headers.Add("Authorization", "Bearer " + accessToken)
                                      });
        }

        public async Task<TrackableIdentifier> FindSlideByBarcodeContentAsync(string barcodeContent)
        {
            //var getSlideByBarcodeUri = new Uri(_uri, $"Slide?$filter=barcodeContent eq '{barcodeContent}'");

            Console.WriteLine($"Making a GET request for slide information for slides with barcode content {barcodeContent}");

            var result = await _client.For("Slide")
                                      .Filter($"barcodeContent eq '{barcodeContent}'")
                                      .FindEntriesAsync();
            var matchingSlides = result.Select(obj => new TrackableIdentifier
                                                      {
                                                          AlternateIdentifier = obj["alternateIdentifier"] as string ?? string.Empty,
                                                          BarcodeContent = obj["barcodeContent"] as string ?? string.Empty,
                                                          PrimaryIdentifier = obj["savedIdentifier"] as string ?? string.Empty
                                                      });
            return matchingSlides.FirstOrDefault();
        }

        public async Task<IEnumerable<TrackableIdentifier>> TranslateBarcodesToIdentifiersAsync(IEnumerable<string> barcodeContents)
        {
            Console.WriteLine($"Making a POST request for slide information for slides with barcode content {barcodeContents}");

            // Make the array named for JSON
            var namedArray = new Dictionary<string, object>
                             {
                                 {"barcodes", barcodeContents}
                             };
            var results = await _client.For("Slide")
                                       .Action("FindByBarcode")
                                       .Set(namedArray)
                                       .ExecuteAsEnumerableAsync();

            var slides = new List<TrackableIdentifier>();
            foreach (var obj in results)
                slides.Add(new TrackableIdentifier
                           {
                               AlternateIdentifier = obj["alternateIdentifier"] as string ?? string.Empty,
                               BarcodeContent = obj["barcodeContent"] as string ?? string.Empty,
                               PrimaryIdentifier = obj["savedIdentifier"] as string ?? string.Empty
                           });

            return slides;
        }
    }
}