/*
using System.Diagnostics;
using System.Management;
using System.Text;
using System.IO.Compression;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.Security.Cryptography;
using System.Text.Json;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using System.Runtime.CompilerServices;
using System.Net.Http;
using System.Threading.Tasks;

#pragma warning disable CA1416, CS8602,CS8603  

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
    bool applicationStatus = true;
    int statusCode = 200;
    return Results.Json(new { applicationStatus, statusCode });
});

app.MapPost("/call", (CommandInterface CallData) =>
{
    var processUtils = new ProcessUtilities();
    var executionContext = processUtils.MakeCall(CallData);

    Console.WriteLine(CallData.DealId);
    Console.WriteLine(CallData.ApiKey);
    Console.WriteLine(CallData.ContactId);
    Console.WriteLine(CallData.Phone);


    return executionContext;

});

app.Run();
public class ProcessUtilities
{
    public int isRunning = 1;
    public string localPath = LocalPathFinder.MicrosipPath();
    public Microsoft.AspNetCore.Http.IResult MakeCall(CommandInterface CallData)
    {
        if (string.IsNullOrEmpty(CallData.Phone)){
            return Results.Json(new { message = "Phone is required" }, statusCode: 400);
        }

        if (CallData.DealId == 0 && CallData.ContactId == 0)
        {

            return Results.Json(new { message = "CallId or ContactId is required"}, statusCode: 400);
        }

        try
        {
            var processCalled = Process.Start("cmd.exe", $"/c {localPath} {CallData.Phone}");
            if (localPath != null)
            {
                MonitorProcess(CallData);
                return Results.Json(new { message = "Call made" , phone = CallData.Phone}, statusCode: 200);
            }
            else
            {
                return Results.Json(new { message = "The LocalPath is Null" },statusCode: 400);

            }
        }
        catch(Exception ex)
        {
                return Results.Json(new { message = $"Don't Have A Valid LocalPath: {ex.Message}" }, statusCode: 400);
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

        if (file == null)
        {
            return null;
        }
        return file;
    }


    public static async Task<string> zipFiles(CommandInterface CallData)
    {
        string recPath = RecordingsPath();
        var file = GetLastAudioRecFile(recPath);
        string archivePath = Path.Combine(recPath, file.Name),
                zipFilePath = Path.Combine(recPath, $"{file.Name}.zip");
        try
        {
            
            using (FileStream zipFile = new FileStream(zipFilePath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(zipFile, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(archivePath, file.Name);

            }

            string keyNameHash = fileNameWithHash.FileNameFormated(file.Name),
                   stringKeyConcat = fileNameWithHash.FileNameFormated(file.Name),
                    bucketUrl = $"https://voipbucket.s3.sa-east-1.amazonaws.com/{stringKeyConcat}";

            await sendFiles.MainSendFile(archivePath, stringKeyConcat);
            await putRecordingCard.PostRecords(CallData, bucketUrl);
            return file.Name;
        }

        catch (Exception ex)
        {
            Console.WriteLine($"zip error: {ex.Message}");
            return string.Empty;
        }
    }
}

public class sendFiles
{
    public static String bucketName = "voipbucket";
    private static RegionEndpoint bucketRegion = RegionEndpoint.SAEast1;
    private static IAmazonS3? s3Client;


    public static async Task MainSendFile(string filePath, string stringKeyConcat)
    {
        s3Client = new AmazonS3Client("Aws-Key", "Aws-Secret-Key", bucketRegion);
        try
        {
            var response = await UploadFileAsync(filePath, stringKeyConcat);
        }
        catch
        {
            Console.WriteLine("Error");
        }
    }


    private static async Task<string?> UploadFileAsync(string filePath, string stringKeyConcat)
    {
        try
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = stringKeyConcat,
                FilePath = filePath,
                ContentType = "audio/mpeg"
            };

            PutObjectResponse response = await s3Client.PutObjectAsync(putRequest);
            Console.WriteLine($"https://voipbucket.s3.sa-east-1.amazonaws.com/{stringKeyConcat}");
            return response.VersionId;

        }
        catch
        {
            Console.WriteLine("Error");
            return null;
        }
    }

}


public class putRecordingCard
{
public static bool responseOk(int statusCode){

        return statusCode >= 200 && statusCode <= 299;
    }
    public static async Task PostRecords(CommandInterface CallData, String bucketURL)
    {

        if(CallData.ContactId == 0 && CallData.DealId == 0){
            return;
        }

        string url = "https://api2.ploomes.com/InteractionRecords";
        var client = new HttpClient();

        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + CallData.ApiKey);
        client.DefaultRequestHeaders.Add("User-Key", CallData.ApiKey);
        var payload = new Dictionary<string, dynamic> {

            {"ContactId" , CallData.ContactId},
            {"Content" ,    $"<div>Ligação realizada via <strong>Suncall</strong></div> " +
                            $"<audio controls style='width: 300px; height: 70px;'>" +
                            $"<source src='{bucketURL}' type='audio/mpeg'>" +
                            "Seu navegador não suporta o elemento de áudio.</audio> " +
                            $"<img src='https://stgploomescrmprd01.blob.core.windows.net/crm-prd/A841002849BF/Images/0bf06332101544d28bedac7b827d272f.png'/>"
                            },
            {"DealId" , CallData.DealId},
            {"TypeId" , 1}
        };

        var response = await client.PostAsJsonAsync(url, payload);            
        if(responseOk((int)response.StatusCode)){
            Console.WriteLine("Record posted successfully");
        }
        else{
            Console.WriteLine("Error posting record");
        }
    }
}


public class fileNameWithHash
{   
     public static String FileNameFormated(string fileName){
        
        string hash = GenerateMD5Hash(fileName),
               fileNameReplaced = fileName.Replace(".mp3", ""),
               stringKeyConcat = $"{fileNameReplaced}-{hash}";
        stringKeyConcat += ".mp3";
        return stringKeyConcat;

    }
    public static String GenerateMD5Hash(string fileName)
    {
        byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
        byte[] hashValue = SHA256.HashData(fileNameBytes);
        string hashString = Convert.ToHexString(hashValue);
        return hashString;
    }

}



public class CommandInterface
{
    public required string ApiKey { get; set; }
    public required string Phone { get; set; }
    public int? DealId { get; set; }
    public int? ContactId { get; set; }

}



*/