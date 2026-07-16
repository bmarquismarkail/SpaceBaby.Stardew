using SV_PotC.Tests;

var tests = new MultiplayerFriendshipAwardTests();
tests.RunAll();

const string packageOption = "--package=";
string? packagePath = args.FirstOrDefault(p => p.StartsWith(packageOption, StringComparison.OrdinalIgnoreCase))?[packageOption.Length..];
if (!string.IsNullOrWhiteSpace(packagePath))
    new ReleasePackageTests().RunAll(packagePath);
