namespace FileDev.Domain.Exception;

public class NotFileException : System.Exception
{
    public NotFileException() : base("文件操作发生异常。")
    {
    }

    public NotFileException(string message) : base(message)
    {
    }

    public NotFileException(string message, System.Exception innerException)
        : base(message, innerException)
    {
    }
}
