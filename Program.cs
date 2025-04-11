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

// Try to get the api_key property safely
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

app.UseHttpsRedirection();

app.MapGet("/chat", (string prompt = null) => 
{
    string userPrompt = string.IsNullOrEmpty(prompt) 
        ? "Say 'this is a ball.'" 
        : prompt;
        
    ChatCompletion completion = client.CompleteChat(userPrompt);
    return new { Response = completion.Content[0].Text };
})
.WithName("GetChatResponse");
// http://localhost:5248/chat?prompt=

app.Run();