namespace ServiceLib.Models;

public class RetResult
{
    public bool IsSuccess { get; set; }
    public string? Msg { get; set; }
    public object? Data { get; set; }

    public RetResult(bool success = false)
    {
        IsSuccess = success;
    }

    public RetResult(bool success, string? msg)
    {
        IsSuccess = success;
        Msg = msg;
    }

    public RetResult(bool success, string? msg, object? data)
    {
        IsSuccess = success;
        Msg = msg;
        Data = data;
    }
}
