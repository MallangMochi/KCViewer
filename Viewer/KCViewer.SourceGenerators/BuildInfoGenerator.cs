using System;
using System.Text;
using Microsoft.CodeAnalysis;

namespace KCViewer.SourceGenerators
{
	[Generator]
	public class BuildInfoGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			context.RegisterPostInitializationOutput(GenerateBuildInfo);
		}

		private void GenerateBuildInfo(IncrementalGeneratorPostInitializationContext context)
		{
			StringBuilder sourceBuilder = new(
				$$"""
					using System;

					namespace KCViewer.Generated
					{
						public static class BuildInfo
						{
							public static long TimeStamp => {{DateTime.UtcNow.Ticks}};
						}
					}
				""");

			context.AddSource("KCViewer.Generated", sourceBuilder.ToString());
		}
	}
}
