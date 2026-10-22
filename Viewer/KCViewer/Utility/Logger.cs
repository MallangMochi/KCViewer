using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KCViewer.Core;

namespace KCViewer.Utility;

public delegate void LogAddedEventHandler(Logger.LogData data);


// Fork: partial for the daily cleanup of old entries (Fork/ForkLogCleanup.cs).
public sealed partial class Logger
{

	#region Singleton

	private static readonly Logger instance = new Logger();

	public static Logger Instance => instance;

	#endregion


	/// <summary>
	/// ログが追加された時に発生します。
	/// </summary>
	public event LogAddedEventHandler LogAdded = delegate { };


	public class LogData
	{

		/// <summary>
		/// 書き込み時刻
		/// </summary>
		public readonly DateTime Time;

		/// <summary>
		/// 優先度
		/// 基本的には 0=非表示(ログファイルにのみ記載, など), 1=内部情報(動作ログ, API受信情報など), 2=重要な情報(入渠完了など, ユーザーに表示する必要があるもの), 3=緊急の情報(エラー等)
		/// </summary>
		public readonly int Priority;

		/// <summary>
		/// ログ内容
		/// </summary>
		public readonly string Message;

		public LogData(DateTime time, int priority, string message)
		{
			Time = time;
			Priority = priority;
			Message = message;
		}


		public override string ToString() => $"[{DateTimeHelper.TimeToCSVString(Time)}][{Priority}] : {Message}";


	}



	private List<LogData> log;
	private bool toDebugConsole;
	private int lastSavedCount;


	private Logger()
	{
		log = new List<LogData>();
		toDebugConsole = true;
		lastSavedCount = 0;
	}


	public static IReadOnlyList<LogData> Log
	{
		get
		{
			lock (Instance)
			{
				return Instance.log.AsReadOnly();
			}
		}
	}


	/// <summary>
	/// ログを追加します。
	/// </summary>
	/// <param name="priority">優先度。</param>
	/// <param name="message">ログ内容。</param>
	public static void Add(int priority, string message)
	{

		LogData data = new LogData(DateTime.Now, priority, message);

		lock (Instance)
		{
			Instance.log.Add(data);
		}

		if (Configuration.Config.Log.SaveLogFlag && Configuration.Config.Log.SaveLogImmediately)
			Save();

		if (Configuration.Config.Log.LogLevel <= priority)
		{

			if (Instance.toDebugConsole)
			{
				System.Diagnostics.Debug.WriteLine(data.ToString());
			}


			try
			{
				Instance.LogAdded(data);

			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
			}

		}
	}

	public static void Add(int priority, string message, Exception exception)
	{
		Add(priority, $"{message} {exception.GetBaseException().Message} {exception.StackTrace}");
	}

	/// <summary>
	/// ログをすべて消去します。
	/// </summary>
	public static void Clear()
	{
		lock (Instance)
		{
			instance.log.Clear();
			instance.lastSavedCount = 0;
		}
	}



	public static readonly string DefaultPath = @"eolog.log";


	public static void Save()
	{
		Save(DefaultPath);
	}

	/// <summary>
	/// ログを保存します。
	/// </summary>
	/// <param name="path">保存先のファイル。</param>
	public static void Save(string path)
	{

		try
		{
			lock (Instance)
			{

				var log = instance;

				// Fork: the file is overwritten with every entry still kept (user decision 2026-10-01): the daily cleanup
				// writes the entries it drops the same way (Fork/ForkLogCleanup.cs), so the file holds the last write.
				using (StreamWriter sw = new StreamWriter(path, false, Configuration.Config.Log.FileEncoding))
				{

					int priority = Configuration.Config.Log.LogLevel;

					foreach (var l in log.log.Where(l => l.Priority >= priority))
					{
						sw.WriteLine(l.ToString());
					}

					log.lastSavedCount = log.log.Count;
				}
			}
		}
		catch (Exception)
		{

			// に ぎ り つ ぶ す
		}

	}

}
