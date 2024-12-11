namespace Services;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Helpers;
using Helpers.ResponseHelper;

public static class SendFileService
{
    public static String bucketName = "voipbucket";
    private static RegionEndpoint bucketRegion = RegionEndpoint.SAEast1;
    private static IAmazonS3? s3Client;


    public static async Task MainSendFile(string filePath, string stringKeyConcat)
    {
        s3Client = new AmazonS3Client("Aws-Key", "Aws-Secret-Key", bucketRegion);

        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(stringKeyConcat))
        {
            ResponseHelper.ResponseStatus("FilePath or Key is missing", 400);
            return;
        }
        try
        {
            var response = await UploadFileAsync(filePath, stringKeyConcat);
            ResponseHelper.ResponseStatus("The File was Send", 200);
        }
        catch (AmazonS3Exception ex)
        {
            ResponseHelper.ResponseStatus($"AWS S3 Error: {ex.Message}", 400);
        }
        catch (Exception ex)
        {
            ResponseHelper.ResponseStatus($"Unexpected Error: {ex.Message}", 400);
        }
    }


    private static async Task<dynamic?> UploadFileAsync(string filePath, string stringKeyConcat)
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
            if (s3Client == null)
            {
                ResponseHelper.ResponseStatus("The S3Client is null", 400);
                return string.Empty;
            }
            PutObjectResponse response = await s3Client.PutObjectAsync(putRequest);
            ResponseHelper.ResponseStatus("Archive posted in Aws", response.HttpStatusCode);
            Console.WriteLine($"https://voipbucket.s3.sa-east-1.amazonaws.com/{stringKeyConcat}");

            return response;
        }
        catch
        {
            ResponseHelper.ResponseStatus("Is Not Possible to Put The Archive in Aws", 400);
            Console.WriteLine("Error");
            return null;
        }
    }
}