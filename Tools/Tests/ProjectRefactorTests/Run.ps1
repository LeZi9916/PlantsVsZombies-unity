param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$toolDirectory = Join-Path $repositoryRoot 'Tools/CSharp/ProjectRefactor'
$fixtureDirectory = Join-Path $PSScriptRoot ('artifacts/' + [Guid]::NewGuid().ToString('N'))
$sourceDirectory = Join-Path $fixtureDirectory 'Assets/Scripts'
New-Item -ItemType Directory -Path $sourceDirectory -Force | Out-Null

$primary = @'
namespace PvZReplica
{
    public sealed partial class Primary
    {
        private int count;
        public int Value { get { return count; } }
        public struct Payload { public int X; }
        partial void Add(ref int value);
        public void Step()
        {
            int Value = 1;
            Add(ref Value);
            count += Value;
        }
    }
}
'@
$fragment = @'
namespace PvZReplica
{
    public sealed partial class Primary
    {
        partial void Add(ref int value) { value += 2; }
    }
}
'@
$client = @'
namespace PvZReplica
{
    public static class Client
    {
        public static Primary.Payload Read() { return new Primary.Payload { X = 3 }; }
    }
}
'@
$vendor = 'namespace Vendor { public static class External { public const int Value = 7; } }'
$primaryPath = Join-Path $sourceDirectory 'Primary.cs'
[IO.File]::WriteAllText($primaryPath, $primary)
[IO.File]::WriteAllText((Join-Path $sourceDirectory 'AFragment.cs'), $fragment)
[IO.File]::WriteAllText((Join-Path $sourceDirectory 'Client.cs'), $client)
[IO.File]::WriteAllText((Join-Path $sourceDirectory 'Vendor.cs'), $vendor)
[IO.File]::WriteAllText(($primaryPath + '.meta'), "fileFormatVersion: 2`r`nguid: 0123456789abcdef0123456789abcdef`r`n")
[IO.File]::WriteAllText((Join-Path $sourceDirectory 'AFragment.cs.meta'), "fileFormatVersion: 2`r`nguid: fedcba9876543210fedcba9876543210`r`n")

Push-Location $toolDirectory
try
{
    & dotnet run --project ProjectRefactor.csproj -- $fixtureDirectory
    if ($LASTEXITCODE -ne 0)
    {
        throw 'Migration dry run failed.'
    }
    if ([IO.File]::ReadAllText($primaryPath) -cne $primary)
    {
        throw 'Dry run modified the source.'
    }
    & dotnet run --project ProjectRefactor.csproj -- $fixtureDirectory --apply
    if ($LASTEXITCODE -ne 0)
    {
        throw 'Migration apply failed.'
    }
    $migratedPath = Join-Path $fixtureDirectory 'Assets/Scripts/Simulation/Primary.cs'
    $migrated = [IO.File]::ReadAllText($migratedPath)
    if ($migrated -notmatch 'private int _count;' -or $migrated -match '\bpartial\s+(class|void)\b')
    {
        throw 'Partial merge or symbol-aware field rename failed.'
    }
    if ([IO.File]::ReadAllText(($migratedPath + '.meta')) -notmatch '0123456789abcdef0123456789abcdef')
    {
        throw 'The primary script GUID was replaced by a fragment GUID.'
    }
    if (-not (Test-Path -LiteralPath (Join-Path $fixtureDirectory 'Assets/Scripts/Simulation/Payload.cs')))
    {
        throw 'The public nested struct was not extracted.'
    }
    if ([IO.File]::ReadAllText((Join-Path $sourceDirectory 'Vendor.cs')) -cne $vendor)
    {
        throw 'Unrelated source was modified.'
    }
    $program = @'
#nullable enable
using System;
using PlantsVsZombies.Simulation;

internal static class Program
{
    private static int Main()
    {
        var model = new Primary();
        model.Step();
        if (model.Value != 3 || Client.Read().X != 3 || Vendor.External.Value != 7)
        {
            throw new InvalidOperationException("Migration changed fixture behavior.");
        }
        Console.WriteLine("PASS: primary GUID, symbol renames, partial methods, nested extraction, foreign source and runtime behavior.");
        return 0;
    }
}
'@
    $project = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Assets/**/*.cs" />
    <Compile Include="Program.cs" />
  </ItemGroup>
</Project>
'@
    [IO.File]::WriteAllText((Join-Path $fixtureDirectory 'Program.cs'), $program)
    [IO.File]::WriteAllText((Join-Path $fixtureDirectory 'Fixture.csproj'), $project)
    & dotnet run --project (Join-Path $fixtureDirectory 'Fixture.csproj')
    if ($LASTEXITCODE -ne 0)
    {
        throw 'The migrated fixture failed to compile or execute.'
    }
    $cleanupDirectory = Join-Path $fixtureDirectory 'missing-metadata'
    $cleanupSources = Join-Path $cleanupDirectory 'Assets/Scripts'
    New-Item -ItemType Directory -Path $cleanupSources -Force | Out-Null
    $unresolvedSource = @'
using Missing.Graphics;
namespace PlantsVsZombies.Demo
{
    public sealed class Consumer
    {
        public Renderer? Renderer { get; set; }
    }
}
'@
    $unresolvedPath = Join-Path $cleanupSources 'Consumer.cs'
    [IO.File]::WriteAllText($unresolvedPath, $unresolvedSource)
    & dotnet run --project ProjectRefactor.csproj -- $cleanupDirectory --cleanup-imports --apply
    if ($LASTEXITCODE -ne 0 -or [IO.File]::ReadAllText($unresolvedPath) -notmatch 'using Missing.Graphics;')
    {
        throw 'Import cleanup removed an external namespace while its metadata was unavailable.'
    }
    Write-Output 'PASS: missing external metadata preserves unresolved imports.'
}
finally
{
    Pop-Location
}
