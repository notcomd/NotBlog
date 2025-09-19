using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FileDev.Domain.FileDevException
{
    public  class NotFileDevException : Exception
    {
       public NotFileDevException() { }

        public NotFileDevException(string message) : base(message)
        {
        }

        public NotFileDevException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

       // public NotFileDevException() { }
    }
}
