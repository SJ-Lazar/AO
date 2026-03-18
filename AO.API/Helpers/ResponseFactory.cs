using AO.Core.Shared.ApiResponses;

namespace AO.API.Helpers;

public static class ResponseFactory
{
    public static Response<T> Success<T>(T data, string message, int statusCode)
    {
        return new Response<T>
        {
            Sucesss = true,
            Data = data,
            Message = message,
            StatusCode = statusCode
        };
    }

    public static Response<object?> Failure(string message, int statusCode)
    {
        return new Response<object?>
        {
            Sucesss = false,
            Message = message,
            StatusCode = statusCode,
            Data = null
        };
    }
}
