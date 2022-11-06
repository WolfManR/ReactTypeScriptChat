namespace ChatApi.Results;

public record Result<T>(int Code, T? Data = default) : Result(Code)
{
    public static Result<T> Success(T data) => new(0, data);
    public new static Result<T> Failure(Error error) => new(error.Code) { Message = error.Message };
}

public record Result(int Code)
{
    public bool IsFailure => Code > 0;
    public string? Message { get; init; }

    public static Result Success() => new(0);
    public static Result Failure(Error error) => new(error.Code) { Message = error.Message };
}