using System.Text.Json;
using OpenAI.Chat;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

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




app.MapGet("/chat", (string? prompt = null) => 
{
    string userPrompt = string.IsNullOrEmpty(prompt) 
        ? "Say 'this is a event.'" 
        : prompt;
        
    ChatCompletion completion = client.CompleteChat(userPrompt);
    return new { Response = completion.Content[0].Text };
})
.WithName("GetChatResponse");
// http://localhost:5248/chat?prompt=Talk%20about%20baseball.



// Define the file path as a constant
string fileName = "D:\\WebDev\\EventContextAI\\test.json";

app.MapGet("/api/json-file", async (HttpContext context) => 
{
    try
    {
        // Sanitize and validate the file path to prevent directory traversal attacks
        // Convert relative path to absolute path in a safe way
        string safePath = Path.GetFullPath(fileName);
        
        // Ensure the path ends with .json
        if (!safePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            safePath += ".json";
        }
        
        // Check if file exists
        if (!File.Exists(safePath))
        {
            return Results.NotFound($"File not found: {safePath}");
        }
        
        // Read the JSON file
        string jsonContent = await File.ReadAllTextAsync(safePath);
        
        // Set content type to application/json
        context.Response.ContentType = "application/json";
        
        // Write the JSON content to the response
        await context.Response.WriteAsync(jsonContent);
        
        return Results.Empty;
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error processing JSON file: {ex.Message}");
    }
})
.WithName("GetJsonFile");

app.MapGet("/csv-data", async () => 
{
    try
    {
        // Check if file exists
        if (!File.Exists(fileName))
        {
            return Results.NotFound($"File not found: {fileName}");
        }

        // Read the CSV file
        string[] csvLines = await File.ReadAllLinesAsync(fileName);
        
        if (csvLines.Length == 0)
        {
            return Results.Ok(new { Message = "CSV file is empty" });
        }

        // Parse header
        string[] headers = csvLines[0].Split(',');
        
        // Parse data rows
        var data = new List<Dictionary<string, string>>();
        for (int i = 1; i < csvLines.Length; i++)
        {
            var values = csvLines[i].Split(',');
            var row = new Dictionary<string, string>();
            
            for (int j = 0; j < Math.Min(headers.Length, values.Length); j++)
            {
                row[headers[j]] = values[j];
            }
            
            data.Add(row);
        }

        // Create prompt with CSV data context
        string csvContext = $"CSV data from {Path.GetFileName(fileName)}:\n";
        csvContext += string.Join("\n", csvLines.Take(Math.Min(10, csvLines.Length)));
        if (csvLines.Length > 10)
        {
            csvContext += "\n... [additional data rows omitted] ...";
        }
        
        return Results.Ok(new { 
            Headers = headers,
            Data = data,
            RowCount = csvLines.Length - 1
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error processing CSV file: {ex.Message}");
    }
})
.WithName("GetCSVData");
// http://localhost:5248/csv-data

// Add an endpoint to analyze the CSV data with OpenAI
app.MapGet("/analyze-csv", async (string? question = null) => 
{
    try
    {
        if (!File.Exists(fileName))
        {
            return Results.NotFound($"File not found: {fileName}");
        }

        // Read the CSV file
        string csvContent = await File.ReadAllTextAsync(fileName);
        
        // // Limit the size of the content to avoid exceeding token limits
        // if (csvContent.Length > 10000)
        // {
        //     csvContent = csvContent.Substring(0, 10000) + "\n[Content truncated due to size]";
        // }

        string userPrompt = string.IsNullOrEmpty(question)
            ? $"Analyze this CSV data and provide a summary:\n\n{csvContent}"
            : $"Based on this CSV data, please answer the following question: {question}\n\nCSV data:\n{csvContent}";
            
        ChatCompletion completion = client.CompleteChat(userPrompt);
        return Results.Ok(new { Response = completion.Content[0].Text });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error analyzing CSV file: {ex.Message}");
    }
})
.WithName("AnalyzeCSVData");
// http://localhost:5248/analyze-csv?question=Tell%20me%20about%20Moonlight%20Mirage%20Music%20Festival

app.Run();