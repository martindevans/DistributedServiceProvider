using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Base;
using DistributedServiceProvider.MessageConsumers;

namespace Consumers.DataStorage
{
    public interface IDataStore
    {
        void Put(Identifier512 key, byte[] value);

        byte[] Get(Identifier512 key, int timeout);
    }
}
