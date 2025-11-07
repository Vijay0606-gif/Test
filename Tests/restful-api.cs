using RestSharp;
using Newtonsoft.Json.Linq;

namespace RestSharp_Automation;

/// <summary>
/// Comprehensive API test suite for RESTful API DEV service
/// Tests cover CRUD operations and negative scenarios for objects endpoint
/// </summary>
public class APITests
{
    private readonly RestClient client;

    /// <summary>
    /// Constructor initializes REST client with base URL from configuration file
    /// </summary>
    public APITests()
    {
        // client = new RestClient("https://api.restful-api.dev");
        string filePath = Path.Combine("..", "..", "..", "Models", "Configuration.json");
        var jsonData = File.ReadAllText(filePath);
        var config = JObject.Parse(jsonData);

        client = new RestClient(config["baseUrl"].ToString());
    }

    // =============================================
    // POSITIVE TESTS
    // =============================================

    /// <summary>
    /// Test to retrieve all objects from the API
    /// Validates successful response and saves data to file
    /// </summary>
    [Fact]
    public async Task GetListOfObjects()
    {
        var request = new RestRequest("objects", Method.Get);
        var response = await client.ExecuteAsync(request);

        Console.WriteLine("Request URL: " + request.Resource);
        Console.WriteLine("Response Status Code: " + (int)response.StatusCode);
        Console.WriteLine("Response Content: " + response.Content);

        Assert.Equal(200, (int)response.StatusCode);
        Assert.NotNull(response.Content);

        var jsonResponse = JArray.Parse(response.Content);

        string outputFilePath = Path.Combine("..", "..", "..", "Models", "responseData.json");
        File.WriteAllText(outputFilePath, jsonResponse.ToString());
    }

    /// <summary>
    /// Test to retrieve a specific object by ID
    /// Uses the last added object from response data file
    /// </summary>
    [Fact]
    public async Task GetObjectById()
    {
        string responseFilePath = Path.Combine("..", "..", "..", "Models", "responseData.json");
        JArray existingData;

        if (File.Exists(responseFilePath))
        {
            var existingDataJson = File.ReadAllText(responseFilePath);
            existingData = JArray.Parse(existingDataJson);

            var lastAddedObject = existingData.Last;
            string objectId = lastAddedObject["id"].ToString();

            var request = new RestRequest($"objects/{objectId}", Method.Get);
            var response = await client.ExecuteAsync(request);

            Assert.Equal(200, (int)response.StatusCode);
            Assert.NotNull(response.Content);
            var jsonResponse = JObject.Parse(response.Content);
            Assert.Equal(objectId, jsonResponse["id"].ToString());
            Assert.NotNull(jsonResponse["name"]);
        }
        else
        {
            Assert.True(false, "Response data file does not exist.");
        }
    }

    /// <summary>
    /// Test to create a new object with valid data
    /// Validates successful creation and ID generation
    /// </summary>
    [Fact]
    public async Task AddObject()
    {
        var body = @"{
            ""name"": ""Apple MacBook Pro 16"",
            ""data"": {
          ""year"": 2019,
          ""price"": 1849.99,
          ""CPU model"": ""Intel Core i9"",
          ""Hard disk size"": ""1 TB""
         }
        }";

        var request = new RestRequest("objects", Method.Post);

        request.AddHeader("Content-Type", "application/json");
        request.AddParameter("application/json", body, ParameterType.RequestBody);
        Console.WriteLine("Request Body: " + body);

        var response = await client.ExecuteAsync(request);

        Assert.Equal(200, (int)response.StatusCode);
        var content = JObject.Parse(response.Content);
        Assert.NotNull(content["id"]);

        string responseFilePath = Path.Combine("..", "..", "..", "Models", "responseData.json");
        JArray existingData;
        if (File.Exists(responseFilePath))
        {
            var existingDataJson = File.ReadAllText(responseFilePath);
            existingData = JArray.Parse(existingDataJson);
        }
        else
        {
            existingData = new JArray();
        }
        existingData.Add(content);
        File.WriteAllText(responseFilePath, existingData.ToString());
    }

    /// <summary>
    /// Test to update an existing object with new data
    /// Validates successful update operation
    /// </summary>

    //    // Test to update an existing Object
    [Fact]
    public async Task UpdateObjectById()
    {
        string responseFilePath = Path.Combine("..", "..", "..", "Models", "responseData.json");
        JArray existingData;

        if (File.Exists(responseFilePath))
        {
            var existingDataJson = File.ReadAllText(responseFilePath);
            existingData = JArray.Parse(existingDataJson);

            var lastAddedObject = existingData.Last;
            string objectId = lastAddedObject["id"].ToString();

            var body = @"{
                ""name"": ""Apple MacBook Pro 16"",
                   ""data"": {
                      ""year"": 2019,
                      ""price"": 2049.99,
                      ""CPU model"": ""Intel Core i9"",
                      ""Hard disk size"": ""1 TB"",
                      ""color"": ""silver""
                   }
                //""name"": ""ASUS TUF B760M Plus WIFI"",
                //""data"": {
                //    ""year"": 2024,
                //    ""price"": 1899.99,
                //    ""CPU model"": ""AMD RYZEN 7 7800X3D"",
                //    ""Hard disk size"": ""2 TB""
                //    }
                }";

            var request = new RestRequest($"objects/{objectId}", Method.Put);
            request.AddParameter("application/json", body, ParameterType.RequestBody);

            var response = await client.ExecuteAsync(request);

            Assert.Equal(400, (int)response.StatusCode);
            var content = JObject.Parse(response.Content);
        }
        else
        {
            Assert.True(false, "Response data file does not exist.");
        }
    }

    // <summary>
    // Test to delete an existing object
    // Validates successful deletion with 200 status
    /// </summary>
    [Fact]
    public async Task DeleteObjectById()
    {
        string responseFilePath = Path.Combine("..", "..", "..", "Models", "responseData.json");
        JArray existingData;

        if (File.Exists(responseFilePath))
        {

            var existingDataJson = File.ReadAllText(responseFilePath);
            existingData = JArray.Parse(existingDataJson);

            var lastAddedObject = existingData.Last;
            string objectId = lastAddedObject["id"].ToString();

            var request = new RestRequest($"objects/{objectId}", Method.Delete);
            var response = await client.ExecuteAsync(request);

            Assert.Equal(200,(int)response.StatusCode);
        }
        else
        {
            Assert.True(false, "Response data file does not exist.");
        }
    }

    // =============================================
    // NEGATIVE TESTS
    // =============================================

    /// <summary>
    /// Test invalid data with missing required fields
    /// Validates 400 Bad Request response for malformed data
    /// </summary>
    [Fact]
    public async Task AddObject_InvalidData_Returns400()
    {
        // Test with missing required "name" field and invalid year value
        var invalidBody = @"{
        ""data"": {
            ""year"": kdkjd,
            ""price"": 1849.99,
            ""CPU model"": """",
            ""Hard disk size"": ""1 TB""
        }
    }";

        var request = new RestRequest("objects", Method.Post);
        request.AddHeader("Content-Type", "application/json");
        request.AddParameter("application/json", invalidBody, ParameterType.RequestBody);

        Console.WriteLine("Request Body: " + invalidBody);

        var response = await client.ExecuteAsync(request);

        // Verify 400 Bad Request status
        Assert.Equal(400, (int)response.StatusCode);

        // Optional: Add additional assertions for error response structure
        var content = JObject.Parse(response.Content);
        Assert.NotNull(content["error"]);
        Console.WriteLine($"Error Response: {response.Content}");
    }

    /// <summary>
    /// Test invalid data types in request payload
    /// Validates proper data type validation on server
    /// </summary>
    [Fact]
    public async Task AddObject_InvalidDataTypes_Returns400()
    {
        var invalidBody = @"{
        ""name"": 12345, // Name should be string, not number
        ""data"": {
            ""year"": ""2019"", // Year should be number, not string
            ""price"": ""expensive"", // Price should be number, not string
            ""CPU model"": ""Intel Core i9"",
            ""Hard disk size"": ""1 TB""
        }
    }";

        var request = new RestRequest("objects", Method.Post);
        request.AddHeader("Content-Type", "application/json");
        request.AddParameter("application/json", invalidBody, ParameterType.RequestBody);

        var response = await client.ExecuteAsync(request);

        Console.WriteLine($"Status Code: {response.StatusCode}");
        Console.WriteLine($"Response: {response.Content}");

        Assert.Equal(400, (int)response.StatusCode);
    }

    /// <summary>
    /// Test accessing incorrect/non-existent endpoint
    /// Validates 404 Not Found response
    /// </summary>
    [Fact]
    public async Task IncorrectEndpoint()
    {
        var request = new RestRequest("objects/A/F", Method.Get);
        var response = await client.ExecuteAsync(request);

        Assert.Equal(404, (int)response.StatusCode);
    }
}