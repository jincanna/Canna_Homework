using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapGet("/Echo/{message}", (string message) => message);

app.MapGet("/GetMessage/{id}", (int id) =>
{
    var message = MessageStorage.GetMessageById(id);
    if (message != null)
    {
        return Results.Json(new { messageId = id, message = message });
    }
    return Results.NotFound(new { error = "Message not found" });
});

app.MapGet("/GetAllMessages", () =>
{
    var messages = MessageStorage.GetAllMessages();
    return Results.Json(messages);
});

app.MapPost("/SendMessage", async (HttpContext context) =>
{
    var requestBody = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(context.Request.Body);

    MessageStorage.AddMessage(requestBody["Message"]);
    int messageId = MessageStorage.GetMessageCount() - 1;

    var response = new { messageId = messageId };
    
    context.Response.ContentType = "application/json";
    await JsonSerializer.SerializeAsync(context.Response.Body, response);
});

app.MapGet("/GetMessagesMoreThanOrEqual/{id}", (int id) =>
{
    var messages = MessageStorage.GetMessagesMoreThanOrEqual(id);
    return Results.Json(messages);
});

app.MapGet("/GetLatestMessageId", () =>
{
    int latestMessageId = MessageStorage.GetMessageCount() - 1;
    return Results.Json(new { latestMessageId = latestMessageId });
});


app.MapRazorPages();

app.Run();

public static class MessageStorage
{
    private static List<string> messages = new List<string>();

    public static void AddMessage(string message)
    {
        messages.Add(message);
    }

    public static int GetMessageCount()
    {
        return messages.Count;
    }

    public static string GetMessageById(int id)
    {
        if (id >= 0 && id < messages.Count)
        {
            return messages[id];
        }
        return null;
    }

    public static List<string> GetAllMessages()
    {
        return messages;
    }

    public static Dictionary<int, string> GetMessagesMoreThanOrEqual(int id)
    {
        if (id < 0 || id >= messages.Count)
        {
            return null;
        }

        var result = new Dictionary<int, string>();
        for (int i = id; i < messages.Count; i++)
        {
            result.Add(i, messages[i]);
        }
        return result;
    }
}
