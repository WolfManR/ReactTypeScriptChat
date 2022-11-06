namespace ChatApi.Results;

public record struct Error(int Code, string Message);