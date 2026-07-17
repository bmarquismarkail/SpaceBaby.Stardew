using System.IO.Compression;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpaceBaby.PartOfTheCommunity.Framework;

namespace SV_PotC.Tests;

internal sealed class ReleasePackageTests
{
    private static readonly string[] RequiredEntries =
    {
        "PartOfTheCommunity/manifest.json",
        "PartOfTheCommunity/PartOfTheCommunity.dll",
        "PartOfTheCommunity/Data/default_characters.json",
        "PartOfTheCommunity/API_README.md",
        "PartOfTheCommunity/docs/example_character_pack.json",
        "PartOfTheCommunity/docs/content-pack-example/manifest.json",
        "PartOfTheCommunity/docs/content-pack-example/content.json"
    };

    public void RunAll(string packagePath)
    {
        Run(nameof(ReleasePackage_ContainsRuntimeDataAndApiDocumentation), () => this.ReleasePackage_ContainsRuntimeDataAndApiDocumentation(packagePath));
        Run(nameof(ReleasePackage_DefaultCharacterDataIsReadable), () => this.ReleasePackage_DefaultCharacterDataIsReadable(packagePath));
        Run(nameof(ReleasePackage_ContentPackExampleIsReadable), () => this.ReleasePackage_ContentPackExampleIsReadable(packagePath));
        Run(nameof(ReleasePackage_DoesNotLoadExampleAsRuntimeData), () => this.ReleasePackage_DoesNotLoadExampleAsRuntimeData(packagePath));
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }

    private void ReleasePackage_ContainsRuntimeDataAndApiDocumentation(string packagePath)
    {
        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        HashSet<string> entries = archive.Entries.Select(p => p.FullName).ToHashSet(StringComparer.Ordinal);

        foreach (string requiredEntry in RequiredEntries)
            Assert.True(entries.Contains(requiredEntry), $"Release package is missing required entry '{requiredEntry}'.");
    }

    private void ReleasePackage_DefaultCharacterDataIsReadable(string packagePath)
    {
        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        ZipArchiveEntry entry = archive.GetEntry("PartOfTheCommunity/Data/default_characters.json")
            ?? throw new InvalidOperationException("Release package has no default character data.");
        using StreamReader reader = new(entry.Open());
        CharacterPackFlat? pack = JsonConvert.DeserializeObject<CharacterPackFlat>(reader.ReadToEnd());

        Assert.True(pack?.Characters.Count > 0, "Packaged default character data should deserialize into at least one character.");
    }

    private void ReleasePackage_DoesNotLoadExampleAsRuntimeData(string packagePath)
    {
        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        Assert.True(
            archive.GetEntry("PartOfTheCommunity/Data/example_character_pack.json") == null,
            "The example character pack must stay under docs so CharacterManager doesn't load it as live data."
        );
    }

    private void ReleasePackage_ContentPackExampleIsReadable(string packagePath)
    {
        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        ZipArchiveEntry contentEntry = archive.GetEntry("PartOfTheCommunity/docs/content-pack-example/content.json")
            ?? throw new InvalidOperationException("Release package has no JSON content-pack example.");
        using StreamReader contentReader = new(contentEntry.Open());
        CharacterPackFlat? pack = JsonConvert.DeserializeObject<CharacterPackFlat>(contentReader.ReadToEnd());

        Assert.True(pack?.Characters.Count > 0, "Packaged content-pack example should deserialize into at least one character.");

        ZipArchiveEntry manifestEntry = archive.GetEntry("PartOfTheCommunity/docs/content-pack-example/manifest.json")
            ?? throw new InvalidOperationException("Release package has no content-pack example manifest.");
        using StreamReader manifestReader = new(manifestEntry.Open());
        JObject manifest = JObject.Parse(manifestReader.ReadToEnd());

        Assert.Equal("SpaceBaby.PartOfTheCommunity", manifest["ContentPackFor"]?["UniqueID"]?.Value<string>(), "The example manifest should target PotC.");
        Assert.Equal("1.4.0", manifest["ContentPackFor"]?["MinimumVersion"]?.Value<string>(), "The example manifest should require the first PotC version with content-pack support.");
    }
}
