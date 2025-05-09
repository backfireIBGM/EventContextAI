using System.Text.Json;
using OpenAI.Chat;


var builder = WebApplication.CreateBuilder(args);

//  Add services to the container.
//  Learn more about configuring OpenAPI at httpsaka.msaspnetopenapi
builder.Services.AddOpenApi();

var app = builder.Build();

 // Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string jsonContent = File.ReadAllText("D:\\WebDev\\EventContextAI\\secrets\\config.json");

var config = JsonSerializer.Deserialize<JsonDocument>(jsonContent);
string? apiKey = null;

// // Try to get the api_key property safely
if (config != null && config.RootElement.TryGetProperty("api_key", out JsonElement apiKeyElement) 
    && apiKeyElement.ValueKind != JsonValueKind.Null)
{
    apiKey = apiKeyElement.GetString();
}

// Create client with the API key
ChatClient client = new(
    model: "gpt-4o", 
    apiKey: apiKey
);

string rocketInfoFile = "D:\\WebDev\\EventContextAI\\rocket_launches.json";
string lastUpdateTimeFile = "D:\\WebDev\\EventContextAI\\last_update_time.txt";

app.MapGet("/analyze-rocket-data", async (string? question = null) => 
{
    try
    {
        DateTime currentDate = DateTime.UtcNow;
        bool justCreated = false;
                
        if (!File.Exists(lastUpdateTimeFile))
        {
            await File.WriteAllTextAsync(lastUpdateTimeFile, currentDate.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            justCreated = true;
        }

        DateTime lastUpdate = DateTime.Parse(await File.ReadAllTextAsync(lastUpdateTimeFile));

        if (lastUpdate.AddMinutes(15) < currentDate || justCreated)
        {
            await UpdateRocketInfoFileAsync(rocketInfoFile);
            await File.WriteAllTextAsync(lastUpdateTimeFile, currentDate.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }

        // Read the JSON content from the file
        string jsonContent = await File.ReadAllTextAsync(rocketInfoFile);
        
        string noQuestion = $"Analyze this JSON data and provide a summary of the upcoming rocket launches hype them up too!:\n{jsonContent}\n" +
            "Give credit to RocketLaunch.Live for the data, they are the source of the data. They do not live stream the launches, they are a data provider." +
            "Say something like 'go to https://fdo.rocketlaunch.live/ for the raw data.' " +
            "But don't list the data, just give a summary. " +
            "Don't mention that it's JSON data or put it in JSON format unless asked. Most users don't know what JSON is.";
        
        string questionPrompt = $"Based on this JSON data, please answer the following question: {question}\n\n" +
            $"JSON data:\n{jsonContent}.\n" +
            "Give credit to RocketLaunch.Live for the data, they are the source of the data. They do not live stream the launches, they are a data provider." +
            "Say something like 'go to https://fdo.rocketlaunch.live/ for the raw data.' " +
            "But don't list the data, just give a summary, usless the user asks for raw data or something like that. " +
            "Don't mention that it's JSON data or put it in JSON format unless asked. Most users don't know what JSON is.";

        string userPrompt = string.IsNullOrEmpty(question)
            ? noQuestion: questionPrompt;
            
        ChatCompletion completion = client.CompleteChat(userPrompt);
        return Results.Ok(new { Response = completion.Content[0].Text });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error analyzing JSON file: {ex.Message}");
    }
})
.WithName("AnalyzeRocketData");

// http://localhost:5248/analyze-rocket-data
// http://localhost:5248/analyze-rocket-data?question=Tell%20me%20about%20the%20next%20rocket%20launch.
// http://localhost:5248/analyze-rocket-data?question=I%20want%20to%20go%20see%20a%20rocket%20fly,%20how,%20whare,%20when,%20what%20will%20I%20see?
// http://localhost:5248/analyze-rocket-data?question=Tell%20me%20about%20the%20next%20human%20launch.

async Task UpdateRocketInfoFileAsync(string path)
{
    try
    {
        const string jsonRocketLaunches = "https://fdo.rocketlaunch.live/json/launches/next/5";
        
        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(jsonRocketLaunches);
            response.EnsureSuccessStatusCode(); // Throws if not 200-299
            
            string responseBody = await response.Content.ReadAsStringAsync();
            
            // Write the response directly to the file
            await File.WriteAllTextAsync(path, responseBody);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching or saving rocket launches: {ex.Message}");
    }
}

app.Run();

        // JsonDocument data = JsonDocument.Parse(Path.ReadAllText(rocketInfoFile));