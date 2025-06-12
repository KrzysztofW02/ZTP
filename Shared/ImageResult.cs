using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class ImageResult
    {
        public string FileName { get; set; } = default!;
        public string Backend { get; set; } = default!; 
        public long ElapsedMs { get; set; }
    }
}
