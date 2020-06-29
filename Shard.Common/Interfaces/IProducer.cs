using System;
using System.Collections.Generic;
using System.Text;

namespace Shard.Common.Interfaces
{
    internal interface IProducer
    {
        void Start(IPipeline pipeline);

        void Stop();
    }
}