using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDev.Domain.Options
{
    public class FileTypeConfigurationTable
    {
        public List<string> FileTypeWhiteTable { get; } = new List<string>();
        public List<string> FileTypeBlckTable { get; } =new List<string>();
    }
}
