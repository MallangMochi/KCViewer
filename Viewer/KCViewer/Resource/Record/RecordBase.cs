using System;
using System.IO;
using System.Windows.Forms;
using KCViewer.Core;
using KCViewer.Utility.Mathematics;

namespace KCViewer.Resource.Record;

/// <summary>
/// レコードの基底です。
/// </summary>
public abstract class RecordBase
{

	/// <summary>
	/// Fork: records are written in UTF-8 with a byte order mark whatever the log file encoding is (user's choice
	/// 2026-10-04: "새 기록부터 UTF-8로 저장"). With the default Shift-JIS, Korean ship and equipment names were saved
	/// as 「?」.
	/// </summary>
	private static System.Text.Encoding ForkRecordEncoding { get; } = new System.Text.UTF8Encoding(true);

	private static bool ForkIsUtf8File(string path)
	{
		try
		{
			using FileStream stream = File.OpenRead(path);
			Span<byte> head = stackalloc byte[3];
			return stream.Read(head) == 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF;
		}
		catch (IOException)
		{
			return true;
		}
	}


	/// <summary>
	/// レコードの要素の基底です。
	/// </summary>
	public abstract class RecordElementBase
	{

		public abstract void LoadLine(string line);
		public abstract string SaveLine();

	}



	/// <summary>
	/// ファイルからレコードを読み込みます。
	/// </summary>
	/// <param name="path">ファイルが存在するフォルダのパス。</param>
	public virtual bool Load(string path)
	{

		path = GetFilePath(path);

		try
		{
			bool hasError = false;

			// Fork: a file written in UTF-8 (ForkRecordEncoding) is read by its byte order mark, an older one in the
			// configured encoding
			using (StreamReader sr = new StreamReader(path, Utility.Configuration.Config.Log.FileEncoding, detectEncodingFromByteOrderMarks: true))
			{

				ClearRecord();

				bool ignoreError = false;

				string line;
				sr.ReadLine();          //ヘッダを読み飛ばす

				while ((line = sr.ReadLine()) != null)
				{
					try
					{
						LoadLine(line);

					}
					catch (Exception ex)
					{
						if (ignoreError)
							continue;

						hasError = true;
						Utility.ErrorReporter.SendErrorReport(ex, $"レコード {Path.GetFileName(path)} の破損を検出しました。");

						switch (MessageBox.Show($"レコード {Path.GetFileName(path)} で破損データを検出しました。\r\n\r\n[中止]: 読み込みを中止します。データを失う可能性があります。\r\n[再試行]: <推奨>読み込みを続行します。\r\n[無視]: 読み込みを続行します。(以降再確認しません。)",
							"レコード破損検出", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Error, MessageBoxDefaultButton.Button2))
						{
							case DialogResult.Abort:
								throw;

							case DialogResult.Retry:
								// do nothing
								break;

							case DialogResult.Ignore:
								ignoreError = true;
								break;
						}
					}
				}

			}

			if (hasError)
			{
				string backupDestination = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "_backup_" + DateTimeHelper.GetTimeStamp() + Path.GetExtension(path));
				File.Copy(path, backupDestination);
				Utility.Logger.Add(3, $"修復前のレコードを {backupDestination} に退避しました。復旧に失敗した場合はこのファイルから復元を試みてください。");

				SaveAll(RecordManager.Instance.MasterPath);
			}

			UpdateLastSavedIndex();
			return true;

		}
		catch (FileNotFoundException)
		{
			Utility.Logger.Add(1, string.Format(ResourceRecordResources.RecordBase_RecordDoesNotExist, path));

		}
		catch (Exception ex)
		{
			Utility.ErrorReporter.SendErrorReport(ex, string.Format(ResourceRecordResources.RecordBase_FailedToLoadRecord, path));
		}

		return false;
	}


	/// <summary>
	/// ファイルに全てのレコードを書き込みます。
	/// </summary>
	/// <param name="path">ファイルが存在するフォルダのパス。</param>
	public virtual bool SaveAll(string path)
	{

		path = GetFilePath(path);

		try
		{

			using (StreamWriter sw = new StreamWriter(path, false, ForkRecordEncoding))
			{

				sw.WriteLine(RecordHeader);
				sw.Write(SaveLinesAll());

			}

			UpdateLastSavedIndex();
			return true;

		}
		catch (Exception ex)
		{
			Utility.ErrorReporter.SendErrorReport(ex, string.Format(ResourceRecordResources.RecordBase_FailedToWriteRecord, path));
		}

		return false;
	}


	/// <summary>
	/// ファイルに前回からの差分を追記します。
	/// </summary>
	/// <param name="path">ファイルが存在するフォルダのパス。</param>
	public virtual bool SavePartial(string path)
	{

		if (!SupportsPartialSave)
			return false;

		// Fork: a file in an older encoding is written anew in UTF-8 first, so one file does not mix two encodings
		if (File.Exists(GetFilePath(path)) && !ForkIsUtf8File(GetFilePath(path)))
			return SaveAll(path);

		path = GetFilePath(path);
		bool exists = File.Exists(path);

		try
		{

			using (StreamWriter sw = new StreamWriter(path, true, ForkRecordEncoding))
			{

				if (!exists)
					sw.WriteLine(RecordHeader);

				sw.Write(SaveLinesPartial());
			}

			UpdateLastSavedIndex();
			return true;

		}
		catch (Exception ex)
		{
			Utility.ErrorReporter.SendErrorReport(ex, string.Format(ResourceRecordResources.RecordBase_FailedToWriteRecord, path));
		}

		return false;
	}


	protected string GetFilePath(string path)
	{
		return path.Trim(@" \\""".ToCharArray()) + "\\" + FileName;
	}


	/// <summary>
	/// ファイルから読み込んだデータを解析し、レコードに追加します。
	/// </summary>
	/// <param name="line">読み込んだ一行分のデータ。</param>
	protected abstract void LoadLine(string line);

	/// <summary>
	/// レコードのデータをファイルに書き込める文字列に変換します。
	/// </summary>
	protected abstract string SaveLinesAll();

	/// <summary>
	/// レコードの差分データをファイルに書き込める文字列に変換します。
	/// </summary>
	protected abstract string SaveLinesPartial();

	public abstract bool SupportsPartialSave { get; }


	/// <summary>
	/// レコードをクリアします。ロード直前に呼ばれます。
	/// </summary>
	protected abstract void ClearRecord();

	protected abstract void UpdateLastSavedIndex();

	public abstract bool NeedToSave { get; }


	public abstract void RegisterEvents();


	/// <summary>
	/// レコードのヘッダを取得します。
	/// </summary>
	public abstract string RecordHeader { get; }

	/// <summary>
	/// 保存するファイル名を取得します。
	/// </summary>
	public abstract string FileName { get; }

}
