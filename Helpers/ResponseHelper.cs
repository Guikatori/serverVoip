using System.Text;
using System.Security.Cryptography;

namespace Helpers.ResponseHelper;

public static class ResponseHelper
{
    public static IResult ResponseStatus(string message, dynamic statusCode)
    {
        if(statusCode != 200){
            return Results.Json(new { message = $"Error: {message}"}, statusCode: statusCode);
        }else{
            return Results.Json(new { message = $"Sucess: {message}"}, statusCode: statusCode);
        }
    }
}