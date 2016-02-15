using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConcurrentCollections
{
    public enum TriState
        :byte
    {
        True,
        False,
        Timeout,
    }
}
