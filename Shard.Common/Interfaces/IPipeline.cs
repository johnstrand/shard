using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Shard.Common.Interfaces
{
    internal interface IPipeline
    {
        void Submit(Stream data, IDictionary<string, string> context);

        void Start();

        void Stop();

        void Suspend(Guid id);
    }
}