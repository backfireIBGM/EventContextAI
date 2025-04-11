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

    ChatClient client = new(
        model: "gpt-4o", 
        apiKey: ""
    );

app.UseHttpsRedirection();

app.MapGet("/chat", async () => 
{
    ChatCompletion completion = client.CompleteChat("Say 'this is a test.'");
    return new { Response = completion.Content[0].Text };
})
.WithName("GetChatResponse");

app.Run();