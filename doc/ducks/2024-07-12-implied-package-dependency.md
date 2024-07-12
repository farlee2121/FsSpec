---
date: 2024-07-12
---

Previously I thought I needed FsSpec.FsCheck to reference FsSpec.Core as a package in order to publish correctly.
To accomplish this and still have a sane local build I set up a system where the project would output a package to a local dir on build
and I had that local dir set up as a nuget source so that referencing projects could update their referenced package version every time I built.

Turns out this was all unnecessary. Nuget is apparently smart enough to recognize that if a project references another project that is a package, it should
turn the project reference into a package reference on publish!


The package creation on build
```xml
  <PropertyGroup>
    <!-- https://docs.microsoft.com/en-us/visualstudio/msbuild/property-functions?view=vs-2022#calling-instance-methods-on-static-properties -->
    <TimestampNow>$([System.DateTime]::Now.ToString('yyyy-MM-dd-HH-mm-ss'))</TimestampNow>
  </PropertyGroup>
  <Target Name="BuildNuget" AfterTargets="AfterBuild">
    <Exec Command="dotnet pack --no-build --include-symbols -o ../../LocalNugetPackages --version-suffix $(TimestampNow) --configuration $(Configuration)"  />
  </Target>
```

the Nuget config
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <packageSources>
      <add key="Local" value="LocalNugetPackages" />
    </packageSources>
</configuration>
```

The pseudo project reference for intellisense
```xml
<!-- uses a flexible version so it'll pick up new package versions every time I build -->
<PackageReference Include="FsSpec" Version="0.2.0-alpha5*" />
<ProjectReference Include="..\FsSpec.Core\FsSpec.Core.fsproj">
    <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
</ProjectReference>
```
