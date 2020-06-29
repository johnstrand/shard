namespace Shard.Common.Interfaces
{
    internal interface IConsumer
    {
        void Execute(IPipelineHost host);
    }
}