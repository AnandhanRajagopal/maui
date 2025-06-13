﻿using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Maui.IntegrationTests;

[Trait(Categories.TraitKey, Categories.MultiProject)]
public class MultiProjectTemplateTest : BaseTemplateTests
{
	public MultiProjectTemplateTest(ITestOutputHelper output) : base()
	{
		_output = output;
		TestContext.Configure(output);
	}

	[Theory]
	[InlineData("Debug", "simplemulti")]
	[InlineData("Release", "simplemulti")]
	[InlineData("Debug", "MultiProject@Symbol & More")]
	[InlineData("Release", "MultiProject@Symbol & More")]
	public void BuildMultiProject(string config, string projectName)
	{
		var projectDir = Path.Combine(TestDirectory, projectName);
		var name = Path.GetFileName(projectDir);
		var solutionFile = Path.Combine(projectDir, $"{name}.sln");

		Assert.True(DotnetInternal.New("maui-multiproject", projectDir, DotNetCurrent),
			$"Unable to create template maui-multiproject. Check test output for errors.");

		// Always remove WinUI project if the project name contains special characters that cause WinRT source generator issues
		// See: https://github.com/microsoft/CsWinRT/issues/1809 (under "Special characters in assembly name" section)
		bool containsSpecialChars = projectName.IndexOfAny(new[] { '@', '&', '+', '%', '!', '#', '$', '^', '*', ' ', '-' }) >= 0;

		if (!TestEnvironment.IsWindows || containsSpecialChars)
		{
			Assert.True(DotnetInternal.Run("sln", $"\"{solutionFile}\" remove \"{projectDir}/{name}.WinUI/{name}.WinUI.csproj\""),
				$"Unable to remove WinUI project from solution. Check test output for errors.");
		}

		// TODO, we should not need this but hitting: https://github.com/dotnet/maui/issues/19840
		var buildProps = BuildProps;
		buildProps.Add("ResizetizerErrorOnDuplicateOutputFilename=false");

		Assert.True(DotnetInternal.Build(solutionFile, config, properties: buildProps, msbuildWarningsAsErrors: true),
			$"Solution {name} failed to build. Check test output/attachments for errors.");
	}

	[Theory]
	[InlineData("Debug", "--android")]
	[InlineData("Debug", "--ios")]
	[InlineData("Debug", "--windows")]
	[InlineData("Debug", "--macos")]
	public void BuildMultiProjectSinglePlatform(string config, string platformArg)
	{
		var projectDir = TestDirectory;
		var name = Path.GetFileName(projectDir);
		var solutionFile = Path.Combine(projectDir, $"{name}.sln");

		Assert.True(DotnetInternal.New($"maui-multiproject {platformArg}", projectDir, DotNetCurrent),
			$"Unable to create template maui-multiproject. Check test output for errors.");

		if (!TestEnvironment.IsWindows)
		{
			Assert.True(DotnetInternal.Run("sln", $"{solutionFile} remove {projectDir}/{name}.WinUI/{name}.WinUI.csproj"),
				$"Unable to remove WinUI project from solution. Check test output for errors.");
		}

		Assert.True(DotnetInternal.Build(solutionFile, config, properties: BuildProps, msbuildWarningsAsErrors: true),
			$"Solution {name} failed to build. Check test output/attachments for errors.");
	}

	[Theory]
	[InlineData("--android")]
	[InlineData("--ios")]
	[InlineData("--windows")]
	[InlineData("--macos")]
	[InlineData("")] // no platform arg means all platforms
				   // https://github.com/dotnet/maui/issues/28695
	public void VerifyIncludedPlatformsInSln(string platformArg)
	{
		var projectDir = TestDirectory;
		var name = Path.GetFileName(projectDir);
		var solutionFile = Path.Combine(projectDir, $"{name}.sln");

		Assert.True(DotnetInternal.New($"maui-multiproject {platformArg}", projectDir, DotNetCurrent),
			$"Unable to create template maui-multiproject. Check test output for errors.");

		var slnListOutput = DotnetInternal.RunForOutput("sln", $"{solutionFile} list", out int exitCode);

		// Asserts the process completed successfully
		Assert.Equal(0, exitCode);

		// Asserts if the shared project is included in the solution, this should always be the case
		Assert.Contains($"{name}.csproj", slnListOutput, StringComparison.OrdinalIgnoreCase);

		var expectedCsprojFiles = new List<string> { "Droid.csproj", "iOS.csproj", "Mac.csproj", "WinUI.csproj" };

		switch (platformArg)
		{
			case "--android":
				expectedCsprojFiles.Remove("iOS.csproj");
				expectedCsprojFiles.Remove("WinUI.csproj");
				expectedCsprojFiles.Remove("Mac.csproj");
				break;
			case "--ios":
				expectedCsprojFiles.Remove("Droid.csproj");
				expectedCsprojFiles.Remove("WinUI.csproj");
				expectedCsprojFiles.Remove("Mac.csproj");
				break;
			case "--windows":
				expectedCsprojFiles.Remove("Droid.csproj");
				expectedCsprojFiles.Remove("iOS.csproj");
				expectedCsprojFiles.Remove("Mac.csproj");
				break;
			case "--macos":
				expectedCsprojFiles.Remove("Droid.csproj");
				expectedCsprojFiles.Remove("iOS.csproj");
				expectedCsprojFiles.Remove("WinUI.csproj");
				break;
		}

		// Depending on the platform argument, we assert if the expected projects are included in the solution
		foreach (var platformCsproj in expectedCsprojFiles)
		{
			Assert.Contains(platformCsproj, slnListOutput, StringComparison.Ordinal);
		}
	}
}
