namespace FileDev.Domain.FileDevException
{
    public class NotFileDevException : Exception
    {
        // 使用空体构造函数的简化语法
        public NotFileDevException() { }

        // 使用空体构造函数的简化语法
        public NotFileDevException(string message) : base(message) { }

        // 使用空体构造函数的简化语法
        public NotFileDevException(string? message, Exception? innerException) : base(message, innerException) { }
    }
}
