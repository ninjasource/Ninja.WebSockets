using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ninja.WebSockets
{
    interface IBufferPool
    {
        MemoryStream GetBuffer();
    }
}
