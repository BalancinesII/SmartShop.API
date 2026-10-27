using System.Text.Json;

namespace SmartShop.IntegrationTests.Infrastructure;

public record ServerSentEvent(string Event, JsonElement Data);

// Lector mínimo de text/event-stream para los tests: separa los eventos por
// línea en blanco y lee los campos "event:" y "data:" (JSON).
public static class ServerSentEventsReader
{
    public static async Task<List<ServerSentEvent>> ReadAllAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var events = new List<ServerSentEvent>();

        foreach (var block in body.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var eventName = "message";
            var data = new List<string>();

            foreach (var line in block.Split('\n'))
            {
                if (line.StartsWith("event:"))
                    eventName = line["event:".Length..].Trim();
                else if (line.StartsWith("data:"))
                    data.Add(line["data:".Length..].TrimStart());
            }

            if (data.Count > 0)
                events.Add(new ServerSentEvent(eventName, JsonDocument.Parse(string.Join("\n", data)).RootElement));
        }

        return events;
    }
}
