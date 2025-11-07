using RestSharp;
using Newtonsoft.Json.Linq;

namespace RestSharp_Automation;

/// <summary>
/// Comprehensive API test suite for RESTful API DEV service
/// Tests cover CRUD operations and negative scenarios for objects endpoint
/// </summary>
public class APITests
{
    /// <summary>
    /// REST client used for requests to the API; initialized from configuration.
    /// </summary>
    private readonly RestClient client;

    /// <summary>
    /// Constructor initializes REST client with base URL from configuration file
    /// </summary>
    public APITests()
    {
        // Read configuration file path used for tests
        string filePath = Path.Combine("..", "..", "..", "Models", "Configuration.json");
        // Read entire JSON configuration content from disk
        var jsonData = File.ReadAllText(filePath);
        // Parse the configuration JSON into a JObject for access
        var config = JObject.Parse(jsonData);

        // Initialize RestClient with baseUrl from configuration
        client = new RestClient(config["baseUrl"].ToString());
    }

    /// <summary>
    /// Test to retrieve all objects from the API.
    /// Validates successful response and saves data to file.
    /// </summary>
    [Fact]
    public async Task GetListOfObjects()
    {
        // Create a GET request for the "objects" endpoint
        var request = new RestRequest("objects", Method.Get);
        // Execute the request asynchronously and capture the response
        var response = await client.ExecuteAsync(request);

        // Log request resource for troubleshooting
        Console.WriteLine("Request URL: " + request.Resource);
        // Log numeric status code returned by the server
        Console.WriteLine("Response Status Code: " + (int)response.StatusCode);
        // Log response body content for debugging
        Console.WriteLine("Response Content: " + response.Content);

        // Assert that the server returned HTTP200 OK
        Assert.Equal(200, (int)response.StatusCode);
        // Assert that response content is not null
        Assert.NotNull(response.Content);

        // Parse response content as JArray since endpoint returns list
        var jsonResponse = JArray.Parse(response.Content);

        // Build output file path to persist the response data
        string outputFilePath = Path.Combine("..", "..", "..", "Models", "responseData.json");
        // Write the JSON array to file for later test usage
        File.WriteAllText(outputFilePath, jsonResponse.ToString());
    }

    /// <summary>
    /// Test to retrieve a specific object by ID.
    /// Uses the last added object from response data file.
    /// </summary>
    [Fact]
    public async Task GetObjectById()
    {
        // Build path to persisted response data file
        string responseFilePath = Path.Combine("..", "..", "..", "Models", "responseData.json");
        JArray existingData;

        // Ensure the persisted file exists before attempting to read
        if (File.Exists(responseFilePath))
        {
            // Read the existing JSON array from disk
            var existingDataJson = File.ReadAllText(responseFilePath);
            // Parse the JSON array into a JArray for manipulation
            existingData = JArray.Parse(existingDataJson);

            // Get the last item in the array (most recently created)
            var lastAddedObject = existingData.Last;
            // Extract id property from the stored object
            string objectId = lastAddedObject["id"].ToString();

            // Create a GET request for the object by id
            var request = new RestRequest($"objects/{objectId}", Method.Get);
            // Execute request
            var response = await client.ExecuteAsync(request);

            // Assert successful retrieval
            Assert.Equal(200, (int)response.StatusCode);
            // Assert response has content
            Assert.NotNull(response.Content);
            // Parse the response into a JObject
            var jsonResponse = JObject.Parse(response.Content);
            // Ensure the returned id matches the requested id
            Assert.Equal(objectId, jsonResponse["id"].ToString());
            // Ensure the object has a name field
            Assert.NotNull(jsonResponse["name"]);
        }
        else
        {
            // Fail the test explicitly if persisted data is missing
            Assert.True(false, "Response data file does not exist.");
        }
    }

    /// <summary>
    /// Test to create a new object with valid data.
    /// Validates successful creation and ID generation.
    /// </summary>
    [Fact]
    public async Task AddObject()
    {
        // Build payload as a typed object to avoid malformed JSON
        var payload = new
        {
            name = "Apple MacBook Pro16",
            data = new
            {
                year = 2019,
                price = 1849.99m,
                cpuModel = "Intel Core i9",
                hardDiskSize = "1 TB"
            }
        };

        // Create POST request and add JSON body (RestSharp serializes the object)
        var request = new RestRequest("objects", Method.Post).AddJsonBody(payload);

        // Execute request and capture response
        var response = await client.ExecuteAsync(request);

        // Assert server returned HTTP200 OK
        Assert.Equal(200, (int)response.StatusCode);
        // Parse response content into JObject (safe null-coalescing)
        var content = JObject.Parse(response.Content ?? "{}");
        // Ensure returned object contains an id
        Assert.NotNull(content["id"]);

        // Save to response file for later tests
        // Prefer creating and deleting resources inside same test instead of sharing files
        var responseFilePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "Models", "responseData.json");

        JArray existingData;
        // Append new content to existing data file if present
        if (File.Exists(responseFilePath))
        {
            // Read existing file content
            var existingDataJson = File.ReadAllText(responseFilePath);
            // Parse into a mutable JArray
            existingData = JArray.Parse(existingDataJson);
        }
        else
        {
            // Initialize a new JArray if file does not exist yet
            existingData = new JArray();
        }

        // Add returned content to the JArray
        existingData.Add(content);
        // Persist updated array back to disk
        File.WriteAllText(responseFilePath, existingData.ToString());
    }

    /// <summary>
    /// Test to update an existing object with new data.
    /// Validates expected server behavior for update operation.
    /// </summary>
    [Fact]
    public async Task UpdateObjectById()
    {
        // Build path to persisted response data file
        string responseFilePath = Path.Combine("..", "..", "..", "Models", "responseData.json");
        JArray existingData;

        // Ensure the persisted file exists before attempting update
        if (File.Exists(responseFilePath))
        {
            // Read existing persisted JSON
            var existingDataJson = File.ReadAllText(responseFilePath);
            // Parse persisted JSON into JArray
            existingData = JArray.Parse(existingDataJson);

            // Select the last added object to update
            var lastAddedObject = existingData.Last;
            // Get the object's id value
            string objectId = lastAddedObject["id"].ToString();

            // Build request body as raw JSON (this string intentionally contains commented-out alternatives)
            var body = @"{
                ""name"": ""Apple MacBook Pro16"",
                   ""data"": {
                      ""year"":2019,
                      ""price"":2049.99,
                      ""CPU model"": ""Intel Core i9"",
                      ""Hard disk size"": ""1 TB"",
                      ""color"": ""silver""
                   }
                }";

            // Create PUT request for the specific object
            var request = new RestRequest($"objects/{objectId}", Method.Put);
            // Add body as request payload (application/json)
            request.AddParameter("application/json", body, ParameterType.RequestBody);

            // Send request and capture response
            var response = await client.ExecuteAsync(request);

            // Assert expected HTTP status code (test expects400 based on API behavior)
            Assert.Equal(400, (int)response.StatusCode);
            // Parse server response content for additional checks
            var content = JObject.Parse(response.Content);
        }
        else
        {
            // Fail if required persisted data file is missing
            Assert.True(false, "Response data file does not exist.");
        }
    }

    /// <summary>
    /// Test to delete an existing object. Validates successful deletion with200 status.
    /// </summary>
    [Fact]
    public async Task DeleteObjectById()
    {
        // Build path to persisted response data file
        string responseFilePath = Path.Combine("..", "..", "..", "Models", "responseData.json");
        JArray existingData;

        // Ensure response file exists
        if (File.Exists(responseFilePath))
        {
            // Read file content and parse into JArray
            var existingDataJson = File.ReadAllText(responseFilePath);
            existingData = JArray.Parse(existingDataJson);

            // Get the last added object
            var lastAddedObject = existingData.Last;
            // Extract its id for deletion
            string objectId = lastAddedObject["id"].ToString();

            // Create DELETE request for the object id
            var request = new RestRequest($"objects/{objectId}", Method.Delete);
            // Execute request
            var response = await client.ExecuteAsync(request);

            // Assert deletion returned HTTP200 OK
            Assert.Equal(200, (int)response.StatusCode);
        }
        else
        {
            // Fail test if prerequisites are not met
            Assert.True(false, "Response data file does not exist.");
        }
    }

    // =============================================
    // NEGATIVE TESTS
    // =============================================

    /// <summary>
    /// Test invalid data with missing required fields.
    /// Validates400 Bad Request response for malformed data.
    /// </summary>
    [Fact]
    public async Task AddObject_InvalidData_Returns400()
    {
        // Test with missing required "name" field and invalid year value (malformed JSON intentionally)
        var invalidBody = @"{
        ""data"": {
            ""year"": kdkjd,
            ""price"":1849.99,
            ""CPU model"": """",
            ""Hard disk size"": ""1 TB""
        }
    }";

        // Create POST request and add malformed body
        var request = new RestRequest("objects", Method.Post);
        request.AddHeader("Content-Type", "application/json");
        request.AddParameter("application/json", invalidBody, ParameterType.RequestBody);

        // Log request body for debugging
        Console.WriteLine("Request Body: " + invalidBody);

        // Execute request against API
        var response = await client.ExecuteAsync(request);

        // Verify server returns HTTP400 Bad Request for malformed payload
        Assert.Equal(400, (int)response.StatusCode);

        // Parse response content expecting an error structure
        var content = JObject.Parse(response.Content);
        // Ensure error property is present in server response
        Assert.NotNull(content["error"]);
        // Log error response
        Console.WriteLine($"Error Response: {response.Content}");
    }

    /// <summary>
    /// Test invalid data types in request payload.
    /// Validates proper data type validation on server.
    /// </summary>
    [Fact]
    public async Task AddObject_InvalidDataTypes_Returns400()
    {
        // Build invalid JSON string where types are incorrect (intentionally invalid for server validation)
        var invalidBody = @"{
        ""name"":12345, // Name should be string, not number
        ""data"": {
            ""year"": ""2019"", // Year should be number, not string
            ""price"": ""expensive"", // Price should be number, not string
            ""CPU model"": ""Intel Core i9"",
            ""Hard disk size"": ""1 TB""
        }
    }";

        // Create POST request with invalid payload
        var request = new RestRequest("objects", Method.Post);
        request.AddHeader("Content-Type", "application/json");
        request.AddParameter("application/json", invalidBody, ParameterType.RequestBody);

        // Execute request
        var response = await client.ExecuteAsync(request);

        // Log response status and body for diagnostic purposes
        Console.WriteLine($"Status Code: {response.StatusCode}");
        Console.WriteLine($"Response: {response.Content}");

        // Assert server validates types and responds with HTTP400
        Assert.Equal(400, (int)response.StatusCode);
    }

    /// <summary>
    /// Test accessing incorrect/non-existent endpoint.
    /// Validates404 Not Found response.
    /// </summary>
    [Fact]
    public async Task IncorrectEndpoint()
    {
        // Create a request to an invalid endpoint path
        var request = new RestRequest("objects/A/F", Method.Get);
        // Execute request
        var response = await client.ExecuteAsync(request);

        // Assert server returns HTTP404 Not Found
        Assert.Equal(404, (int)response.StatusCode);
    }
}