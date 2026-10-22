namespace KCViewer.Avalonia.Translation;

public static class DataConstants
{
	// Fork: the working folder instead of %LOCALAPPDATA%\KCViewer, so each profile keeps its own
	// data and translation cache and the whole folder stays portable.
	public static string AppDataFolder => Directory.GetCurrentDirectory();

	public static string WorkingFolder => Path.Join(AppDataFolder, "DataAndTranslations");

	public static string DataFolder => Path.Join(WorkingFolder, "Data");

	public static string TranslationFolder(string currentLanguage) => Path.Join(
		WorkingFolder,
		"Translations",
		currentLanguage);
}
