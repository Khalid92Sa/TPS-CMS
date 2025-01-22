namespace CMS.Application.Extensions;

public class Result<T>
{
    public bool IsSuccess { get; set; }
    public T Value { get; set; }
    public string Error { get; set; }
    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(T value, string error) => new() { IsSuccess = false, Value = value, Error = error };
}
