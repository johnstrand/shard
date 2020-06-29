using System;
using System.Collections.Generic;
using System.Text;

namespace Shard.Common.Interfaces
{
    internal interface IPipelineHost
    {
        IPipeline GetPipeline(string name);

        void Start();

        void Stop();
    }
}