# .NET9.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that an .NET9.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET9.0 upgrade.
3. Upgrade Components\Components.csproj
4. Upgrade BlazorApp1\Shared\BlazorApp1.Shared.csproj
5. Upgrade Components.BlazorDemo\Components.BlazorDemo.Client\Components.BlazorDemo.Client.csproj
6. Upgrade BlazorApp1\Client\BlazorApp1.Client.csproj
7. Upgrade Components.BlazorDemo\Components.BlazorDemo\Components.BlazorDemo.csproj
8. Upgrade BlazorApp1\Server\BlazorApp1.Server.csproj
9. Upgrade Components.WebAssmbly\Components.WebAssmbly.csproj
10. Upgrade ServerDemo\ServerDemo.csproj
11. Upgrade Demo\Demo.csproj

## Settings

This section contains settings and data used by execution steps.

### Excluded projects

Table below contains projects that do belong to the dependency graph for selected projects and should not be included in the upgrade.

| Project name | Description |
|:-----------------------------------------------|:---------------------------:|

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name | Current Version | New Version | Description |
|:---------------------------------------------|:---------------:|:-----------:|:----------------------------------------------|
| Microsoft.AspNetCore.Components.Web |8.0.7 |9.0.10 | Recommended for .NET9.0 |
| Microsoft.AspNetCore.Components.WebAssembly |8.0.7 |9.0.10 | Recommended for .NET9.0 |
| Microsoft.AspNetCore.Components.WebAssembly.DevServer |8.0.7 |9.0.10 | Recommended for .NET9.0 |
| Microsoft.AspNetCore.Components.WebAssembly.Server |8.0.4;8.0.7 |9.0.10 | Recommended for .NET9.0 |

### Project upgrade details
This section contains details about each project upgrade and modifications that need to be done in the project.

#### Components\Components.csproj modifications

Project properties changes:
 - Target framework should be changed from `net8.0` to `net9.0`

NuGet packages changes:
 - Microsoft.AspNetCore.Components.Web should be updated from `8.0.7` to `9.0.10` (recommended for .NET9.0)
 - Microsoft.AspNetCore.Components.WebAssembly should be updated from `8.0.7` to `9.0.10` (recommended for .NET9.0)

Other changes:
 - None

#### BlazorApp1\Shared\BlazorApp1.Shared.csproj modifications

Project properties changes:
 - Target framework should be changed from `net8.0` to `net9.0`

Other changes:
 - None

#### Components.BlazorDemo\Components.BlazorDemo.Client\Components.BlazorDemo.Client.csproj modifications

Project properties changes:
 - Target framework should be changed from `net8.0` to `net9.0`

NuGet packages changes:
 - Microsoft.AspNetCore.Components.WebAssembly should be updated from `8.0.7` to `9.0.10` (recommended for .NET9.0)

Other changes:
 - None

#### BlazorApp1\Client\BlazorApp1.Client.csproj modifications

Project properties changes:
 - Target framework should be changed from `net8.0` to `net9.0`

NuGet packages changes:
 - Microsoft.AspNetCore.Components.WebAssembly should be updated from `8.0.7` to `9.0.10` (recommended for .NET9.0)
 - Microsoft.AspNetCore.Components.WebAssembly.DevServer should be updated from `8.0.7` to `9.0.10` (recommended for .NET9.0)

Other changes:
 - None

#### Components.BlazorDemo\Components.BlazorDemo\Components.BlazorDemo.csproj modifications

Project properties changes:
 - Target framework should be changed from `net8.0` to `net9.0`

NuGet packages changes:
 - Microsoft.AspNetCore.Components.WebAssembly.Server should be updated from `8.0.7` to `9.0.10` (recommended for .NET9.0)

Other changes:
 - None

#### BlazorApp1\Server\BlazorApp1.Server.csproj modifications

Project properties changes:
 - Target framework should be changed from `net8.0` to `net9.0`

NuGet packages changes:
 - Microsoft.AspNetCore.Components.WebAssembly.Server should be updated from `8.0.4` to `9.0.10` (recommended for .NET9.0)

Other changes:
 - None

#### Components.WebAssmbly\Components.WebAssmbly.csproj modifications

Project properties changes:
 - Target framework should be changed from `net8.0` to `net9.0`

NuGet packages changes:
 - Microsoft.AspNetCore.Components.WebAssembly should be updated from `8.0.7` to `9.0.10` (recommended for .NET9.0)
 - Microsoft.AspNetCore.Components.WebAssembly.DevServer should be updated from `8.0.7` to `9.0.10` (recommended for .NET9.0)

Other changes:
 - None

#### ServerDemo\ServerDemo.csproj modifications

Project properties changes:
 - Target framework should be changed from `net8.0` to `net9.0`

Other changes:
 - None

#### Demo\Demo.csproj modifications

Project properties changes:
 - Target framework should be changed from `net8.0` to `net9.0`

NuGet packages changes:
 - Microsoft.AspNetCore.Components.WebAssembly.DevServer should be updated from `8.0.7` to `9.0.10` (recommended for .NET9.0)
 - Microsoft.AspNetCore.Components.WebAssembly should be updated from `8.0.7` to `9.0.10` (recommended for .NET9.0)

Other changes:
 - None
