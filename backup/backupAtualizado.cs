/*

using System.Diagnostics;
using System.Management;
using System.Text;
using System.Text.Json;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using System.IO.Compression;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.Runtime.CompilerServices;
using System.Net.Http;
using System.Threading.Tasks;
{
    
}




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


app.MapGet("/", () =>
{
    bool status = true;
    return Results.Json(new { status });
});


app.MapPost("/call", (CommandInterface CallData) =>
{
    var processUtils = new ProcessUtilities();
    var executionContext = processUtils.MakeCall(CallData);
    return Results.Json(new { executionContext });
});

app.Run();
public class ProcessUtilities
{
    public int isRunning = 1;
    public string localPath = LocalPathFinder.MicrosipPath();
    public Microsoft.AspNetCore.Http.IResult MakeCall(CommandInterface CallData)
    {

        if (CallData.DealId == null && CallData.ContactId == null)
        {

            return Results.Json(new { message = "CallId or ContactId is required" });
        }

        try
        {
            var processCalled = Process.Start("cmd.exe", $"/c {localPath} {CallData.phone}");
            if (localPath != null)
            {
                MonitorProcess(CallData);
                return Results.Json(new { message = "Call made" });
            }
            else
            {
                return Results.Json(new { message = "The LocalPath is Null" });

            }
        }
        catch
        {
            return Results.Json(new { message = "Don't Have A Valid LocalPath" });

        }
    }

    private int GetMicrosipPid()
    {
        Process[] processes = Process.GetProcessesByName("microsip");
        bool processExist = processes.Length > 0;
        return processExist ? processes[0].Id : -1;
    }

    private void MonitorProcess(CommandInterface CallData)
    {
        int pid = GetMicrosipPid();
        if (pid == -1)
        {
            return;
        }
        string pidString = pid.ToString();

        ManagementEventWatcher watcher = new ManagementEventWatcher(
            new WqlEventQuery($"SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Thread' AND TargetInstance.ProcessHandle = '{pidString}'")
        );


        watcher.EventArrived += new EventArrivedEventHandler(async (sender, e) => await HandleEvent(sender, e, CallData));
        watcher.Start();
    }

    public async Task HandleEvent(object sender, EventArrivedEventArgs e, CommandInterface CallData)
    {
        isRunning++;
        if (isRunning == 2)
        {
            {
                await recordingFiles.Webhook();
                await recordingFiles.zipFiles(CallData);
            }
        }
    }
}




public class LocalPathFinder
{

    public static string MicrosipPath()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string targetPath = Path.Combine(appDataPath, "MicroSIP", "microsip.exe");

        if (File.Exists(targetPath))
        {
            Console.WriteLine("Caminho do arquivo encontrado: " + targetPath);
            return targetPath;
        }
        else
        {
            targetPath = SecondTypeOfPathVerification();
            return targetPath;
        }
    }

    public static string SecondTypeOfPathVerification()
    {
        string programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string program86x = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        List<string> morePaths = new List<string>{
        Path.Combine(programFilesPath, "Microsip", "microsip.exe"),
        Path.Combine(program86x, "Microsip", "microsip.exe"),
    };

        foreach (var Path in morePaths)
        {
            if (File.Exists(Path))
            {
                Console.WriteLine("Caminho do arquivo encontrado: " + Path);
                return Path;
            }
        }
        return string.Empty;
    }
}


public class recordingFiles
{
    public static async Task Webhook()
    {
        using HttpClient httpClient = new HttpClient();

        string url = "https://webhook.site/073ef1df-fdfa-47dc-a913-869940380d6b",
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


    public static string RecordingsPath()
    {

        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string recordingPath = Path.Combine(desktopPath, "Recordings");


        if (Directory.Exists(recordingPath))
        {
            Console.WriteLine("Recordings Paths is" + recordingPath);
        }
        else
        {
            string documentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            recordingPath = Path.Combine(documentPath, "Recordings");
            Console.WriteLine("Recordings Paths is" + documentPath);

        }
        return recordingPath;
    }


    public static FileInfo GetLastAudioRecFile(string recPath)
    {

        if (string.IsNullOrEmpty(recPath))
        {
            Console.WriteLine("Recording Path is Null");
            return null;
        }

        var file = Directory.GetFiles(recPath, "*.mp3")
                            .Select(f => new FileInfo(f))
                            .OrderByDescending(f => f.CreationTime)
                            .FirstOrDefault();

        if(file == null){
            return null;
        }
        return file;
    }


    public static async Task <string> zipFiles(CommandInterface CallData){
        string recPath = RecordingsPath();
        var file = GetLastAudioRecFile(recPath);
        string archivePath  = Path.Combine(recPath, file.Name);
        string zipFilePath = Path.Combine(recPath, $"{file.Name}.zip");

        try{
             using (FileStream zipFile = new FileStream(zipFilePath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(zipFile, ZipArchiveMode.Create)){
                    archive.CreateEntryFromFile(archivePath, file.Name);

            }
            Console.WriteLine("The archive zip is Finished.");
            await sendFiles.MainSendFile(zipFilePath, file.Name);
            await putRecordingCard.MakingPloomesPost(CallData, file.Name);
            return file.Name;
        }

        catch (Exception ex)
        {
            Console.WriteLine($"zip error: {ex.Message}");
            return string.Empty;
        }
        }
}

public class sendFiles{
public static String bucketName = "voipbucket";
private static RegionEndpoint  bucketRegion  = RegionEndpoint.SAEast1;
private static IAmazonS3? s3Client;


public static async Task MainSendFile(string filePath, string bucketKeyName){
    s3Client = new AmazonS3Client("Aws-Key", "Aws-Secret-Key", bucketRegion);

    try{
        Console.WriteLine("Making Upload");
        var response =  await UploadFileAsync(filePath, bucketKeyName);
        Console.WriteLine(response);
        Console.WriteLine("Upload Finished");
    }
    catch{
         Console.WriteLine("Error");
    }
}


private static async Task<string?> UploadFileAsync(string filePath, string bucketKeyName){


    try{
        var putRequest = new PutObjectRequest{
            BucketName = bucketName,
            Key = bucketKeyName,
            FilePath = filePath,
        };

        PutObjectResponse response = await s3Client.PutObjectAsync(putRequest);

        return response.VersionId;

    }catch{
        Console.WriteLine("Error");
        return null;
    }
}

}


public class putRecordingCard{

    private static  HttpClient client = new HttpClient();
    public static async Task MakingPloomesPost(CommandInterface CallData, string bucketKeyName){

        string fileUrl = $"https://voipbucket.s3.sa-east-1.amazonaws.com/{bucketKeyName}";
        string url = "ploomesssUrl";

        var payloadPloomes = new {
            ContactId = CallData.ContactId,
            Content = "<div>Ligação realizada via <strong>Suncall</strong></div>" +
                      $"<iframe frameborder='0' src='{fileUrl}' " +
                      "width='320px' height='70px'></iframe>" +
                      "<img src='https://stgploomescrmprd01.blob.core.windows.net/crm-prd/A841002849BF/Images/0bf06332101544d28bedac7b827d272f.png'/>",
            DealId = CallData.DealId,
            TypeId = 1
        };

        string payloadJson = System.Text.Json.JsonSerializer.Serialize(payloadPloomes);

        try{
            
            HttpContent content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {CallData.ApiKey}");
            client.DefaultRequestHeaders.Add("UserKey", CallData.ApiKey);
            HttpResponseMessage response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseBody);
        }catch(HttpRequestException e){
            Console.WriteLine("Request error:");

        }

    }


}



public class CommandInterface
{
    public required string ApiKey { get; set; }
    public required string phone { get; set; }
    public int? DealId { get; set; }
    public int? ContactId { get; set; }

}


*/