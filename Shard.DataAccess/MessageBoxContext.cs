using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;

namespace Shard.DataAccess
{
  public class MessageBoxContext
  {
    public class Instance
    {
      private readonly string pipeline;
      private readonly MessageBoxContext wrappedContext;
      internal Instance(string pipeline, MessageBoxContext wrappedContext)
      {
        this.pipeline = pipeline;
        this.wrappedContext = wrappedContext;
      }
      public void Submit(Stage stage, Dictionary<string, string> properties, Stream data, Guid? originalId = null)
      {
        wrappedContext.Submit(pipeline, stage, properties, data, originalId);
      }

      public Task<TrackedMessage> DequeueAsync()
      {
        return wrappedContext.DequeueAsync(pipeline);
      }

      public void Suspend(Guid id, string reason)
      {
        wrappedContext.Suspend(id, reason);
      }

      public void Finalize(Guid id, Finalize state)
      {
        wrappedContext.Finalize(id, state);
      }
    }

    private readonly LiteDatabase db;
    private readonly ILiteCollection<TrackedMessage> messages;
    private readonly ILiteCollection<LogEntry> logs;
    private readonly ILiteStorage<Guid> storage;
    private readonly Dictionary<string, BlockingCollection<Guid>> pending = new Dictionary<string, BlockingCollection<Guid>>();
    private CancellationToken token;
    public static MessageBoxContext Context { get; } = new MessageBoxContext();
    private MessageBoxContext()
    {
      var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Shard");
      Directory.CreateDirectory(folder);
      db = new LiteDatabase(Path.Combine(folder, "tracking.db"));
      messages = db.GetCollection<TrackedMessage>("trackedMessages");
      logs = db.GetCollection<LogEntry>("logEntries");
      storage = db.GetStorage<Guid>();
    }

    public static Instance GetInstance(string pipeline, CancellationToken token)
    {
      Context.pending.Add(pipeline, new BlockingCollection<Guid>());

      foreach (var message in Context.messages.Find(m => !m.Completed && !m.Suspended && m.Pipeline == pipeline))
      {
        Context.pending[message.Pipeline].Add(message.Id, token);
      }

      return new Instance(pipeline, Context);
    }

    private void Submit(string pipeline, Stage stage, Dictionary<string, string> properties, Stream data, Guid? originalId = null)
    {
      var id = Guid.NewGuid();
      messages.Insert(new TrackedMessage
      {
        CreatedAt = DateTime.UtcNow,
        Id = id,
        OriginalId = originalId,
        Pipeline = pipeline,
        Properties = properties,
        RetryCount = 0,
        Stage = stage,
        Suspended = false,
        Completed = false
      });

      storage.Upload(id, id.ToString(), data);

      pending[pipeline].Add(id, token);
    }

    private Task<TrackedMessage> DequeueAsync(string pipeline)
    {
      return new Task<TrackedMessage>(() =>
      {
        var id = pending[pipeline].Take(token);
        return messages.FindById(id);
      }, token);
    }

    private void Suspend(Guid id, string reason) => Update(id, message =>
    {
      message.RetryCount++;
      message.Suspended = message.RetryCount == 3;
      Task.Run(async () =>
      {
        await Task.Delay(new TimeSpan(0, 5, 0), token);
        pending[message.Pipeline].Add(id);
      }, token);
    });

    private void Finalize(Guid id, Finalize state) => Update(id, message =>
    {
      message.Completed = true;
      message.Suspended = state == Shard.DataAccess.Finalize.Terminated;
      storage.Delete(id);
    });

    private void Log(LogEntry entry)
    {
      logs.Insert(entry);
    }

    private void Update(Guid id, Action<TrackedMessage> callback)
    {
      var message = messages.FindById(id);
      callback(message);
      messages.Update(id, message);
    }
  }

  public enum Finalize
  {
    Completed,
    Terminated
  }

  public enum Stage
  {
    Producer,
    Consumer
  }

  public class TrackedMessage
  {
    public Guid Id { get; set; }
    public Guid? OriginalId { get; set; }
    public int RetryCount { get; set; }
    public bool Suspended { get; set; }
    public bool Completed { get; set; }
    public Stage Stage { get; set; }
    public string Pipeline { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, string> Properties { get; set; }
  }

  public class LogEntry
  {
    public Guid Id { get; set; }
    public Guid TrackedMessageId { get; set; }
    public bool Sent { get; set; }
    public string Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Message { get; set; }
  }
}