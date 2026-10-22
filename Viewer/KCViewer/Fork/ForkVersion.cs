using System.Linq;
using System.Reflection;

namespace KCViewer.Fork;

/// <summary>
/// Fork: the fork's version for the version window (help menu, "버전 정보는 우리 버전으로 변경", 2026-10-05): the commit
/// it was built from, the commit's date and whether the working tree had changes. Written into the assembly at build
/// time (KCViewer.csproj, target ForkCommitInfo).
/// </summary>
public static class ForkVersion
{
	public static string ProductName => "KCViewer";

	/// <summary>A release's version ("1.0.0": a tag on the commit or fork-portable.ps1 -Version), null for a development build.</summary>
	public static string? ReleaseVersion => Metadata("ForkReleaseVersion");

	public static string? Commit => Metadata("ForkCommit");
	public static string? CommitDate => Metadata("ForkCommitDate");
	public static bool Dirty => Metadata("ForkDirty") is "True";

	/// <summary>The program update's public key (base64 SubjectPublicKeyInfo), null when the build has none (Update/ForkProgramUpdate.cs).</summary>
	public static string? UpdatePublicKey => Metadata("ForkUpdatePublicKey");

	/// <summary>"963beae9a", "963beae9a-dirty" (built with uncommitted changes), or null without git.</summary>
	public static string? CommitText => Commit is { } commit ? Dirty ? $"{commit}-dirty" : commit : null;

	private static string? Metadata(string key) => typeof(ForkVersion).Assembly
		.GetCustomAttributes<AssemblyMetadataAttribute>()
		.FirstOrDefault(a => a.Key == key)?.Value is { Length: > 0 } value ? value : null;
}
