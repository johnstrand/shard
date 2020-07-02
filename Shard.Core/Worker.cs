using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shard.DataAccess;

namespace Shard.Core
{
  public class Worker : BackgroundService
  {
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
      _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _logger.LogInformation("Main worker thread starting");
      var file = new FileInfo(@"C:\temp\test\br101\arttot.xml_onix.xml.gz");
      var instance = MessageBoxContext.GetInstance("test", stoppingToken);
      _logger.LogInformation("Submitting to message box");
      using var data = file.OpenRead();
      instance.Submit(Stage.Producer, new { size = file.Length, name = file.Name }, data);
      _logger.LogInformation("Message submitted");
      await Task.CompletedTask;
    }
  }
}
