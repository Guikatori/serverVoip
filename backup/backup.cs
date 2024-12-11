/*

using System.Diagnostics;
using System.Management;
using System.Text;
using System.Text.Json;

#pragma warning disable CA1416 

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors("AllowAll");
app.MapGet("/", () => "Server no Ar");

app.MapPost("/data", (MyData myData) =>
{
    var processUtils = new ProcessUtilities();
    string resultado = processUtils.MakeCall(myData.Command);
    return Results.Ok("dados foram recebidos!");
});

app.Run();

public class ProcessUtilities
{
    private int counter = 0;
    public string MakeCall(string? command)
    {
        try
        {
            Process.Start("cmd.exe", $"/c {command}");
            MonitorProcess();
            return "Success";
        }
        catch
        {
            return "Error";
        }
    }

    private uint GetMicrosipPid()
    {
        Process[] processes = Process.GetProcessesByName("microsip");

        if (processes.Length > 0)
        {
            Process microsipProcess = processes[0];
            uint microsipPid = (uint)microsipProcess.Id;
            return microsipPid;
        }
        else
        {
            return 0;
        }
    }

    private void MonitorProcess()
    {
        uint pid = GetMicrosipPid();
        string pidString = pid.ToString();

        ManagementEventWatcher watcher = new ManagementEventWatcher(
            new WqlEventQuery($"SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Thread' AND TargetInstance.ProcessHandle = '{pidString}'")
        );
        watcher.EventArrived += new EventArrivedEventHandler(async (sender, e) => await HandleEvent(sender, e, pid));
        watcher.Start();
    }

    public async Task HandleEvent(object sender, EventArrivedEventArgs e, uint pid)
    {
        counter++;

        if (counter == 1)
        {
            await Webhook();
        }
    }

    private static async Task Webhook()
    {
        using HttpClient httpClient = new HttpClient();

        string url = "https://webhook.site/60d9b087-111c-42b0-ba51-fedfb24855af",
               message = "Chegou o post";

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(message),
            Encoding.UTF8,
            "application/json"
            );

        using HttpResponseMessage response = await httpClient.PostAsync(url, jsonContent);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"{jsonResponse}\n");
    }
}

public class MyData
{
    public string? Command { get; set; }
}


*/
