
using System.IO;


namespace Markdown.Domain.Exceptions
{
    /// <summary>
    /// 自定义标记异常类
    /// </summary>
    public class MarkException : Exception
    {
        /// <summary>
        /// 初始化 <see cref="MarkException"/> 类的新实例。
        /// </summary>
        public MarkException()
        {
        }

        /// <summary>
        /// 使用指定的错误消息初始化 <see cref="MarkException"/> 类的新实例。
        /// </summary>
        /// <param name="message">描述错误的消息。</param>
        public MarkException(string message) : base(message)
        {
        }

        /// <summary>
        /// 使用指定错误消息和对作为此异常原因的内部异常的引用来初始化 <see cref="MarkException"/> 类的新实例。
        /// </summary>
        /// <param name="message">解释异常原因的错误消息。</param>
        /// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则是一个 null 引用。</param>
        public MarkException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}


