namespace Services;
using Helpers.ResponseHelper;
using Models.CommandInterface;



public static class RecordingCardService
{
    public static async Task PostRecording(CommandInterface callData, string bucketUrl)
    {
        if (callData.DealId == 0 && callData.ContactId == 0) return;

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {callData.ApiKey}");
        client.DefaultRequestHeaders.Add("User-Key", callData.ApiKey);

        var payload = new Dictionary<string, dynamic>
        {
            { "ContactId", callData.ContactId },
            { "Content", $"<div>Ligação realizada via <strong>Suncall</strong></div> " +
                            $"<audio controls style='width: 300px; height: 70px;'>" +
                            $"<source src='{bucketUrl}' type='audio/mpeg'>" +
                            "Seu navegador não suporta o elemento de áudio.</audio> " +
                            $"<img src='https://stgploomescrmprd01.blob.core.windows.net/crm-prd/A841002849BF/Images/0bf06332101544d28bedac7b827d272f.png'/>"},
            { "DealId", callData.DealId },
            { "TypeId", 1 }
        };

        var response = await client.PostAsJsonAsync("https://api2.ploomes.com/InteractionRecords", payload);

        if(response.IsSuccessStatusCode){
            ResponseHelper.ResponseStatus("The archive was post in Ploomes With Sucess",200);
        }        
        else{
            ResponseHelper.ResponseStatus("The archive wasn't post in Ploomes", response.StatusCode);
        }
        
        Console.WriteLine(response.IsSuccessStatusCode ? "Recording posted successfully." : "Failed to post recording.");
    }
}