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

// Parse the JSON to get the api_key value
var config = JsonSerializer.Deserialize<JsonDocument>(jsonContent);
string apiKey = config.RootElement.GetProperty("api_key").GetString();

// Create client with the API key
ChatClient client = new(
    model: "gpt-4o", 
    apiKey: apiKey
);

app.UseHttpsRedirection();

app.MapGet("/chat", async () => 
{
    ChatCompletion completion = client.CompleteChat("Say 'this is a ballssssssssssss.'");
    return new { Response = completion.Content[0].Text };
})
.WithName("GetChatResponse");

app.Run();