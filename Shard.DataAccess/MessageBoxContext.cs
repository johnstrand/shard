using System;
using System.Collections.Generic;
using System.IO;
using LiteDB;

namespace Shard.DataAccess
{
  public class MessageBoxContext
  {
    private readonly LiteDatabase db;
    private readonly ILiteCollection<TrackedMessage> messages;
    private readonly ILiteCollection<LogEntry> logs;
    public static MessageBoxContext Instance = new MessageBoxContext();
    private MessageBoxContext()
    {
      var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Shard");
      Directory.CreateDirectory(folder);
      db = new LiteDatabase(Path.Combine(folder, "tracking.db"));
      messages = db.GetCollection<TrackedMessage>("trackedMessage");
      logs = db.GetCollection<LogEntry>("logEntries");
    }

    public void Submit(string pipeline, Dictionary<string, string> properties, Stream data, Guid? originalId = null)
    {
      messages.Insert(new TrackedMessage
      {
        CreatedAt = DateTime.UtcNow,
        Id = Guid.NewGuid(),
        OriginalId = originalId,
        Pipeline = pipeline
      });
      //messages.Insert(message);
    }

    public void Suspend(Guid id)
    {
      var message = messages.FindOne(m => m.Id == id);
      message.State = State.Faulted;
      messages.Update(message);
    }

    public void Log(LogEntry entry)
    {
      logs.Insert(entry);
    }
  }

  public enum State
  {
    New,
    InProgress,
    Faulted,
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
    public State State { get; set; }
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