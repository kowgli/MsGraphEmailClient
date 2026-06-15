# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A C# Windows Forms desktop email client built on the Microsoft Graph API, targeting .NET 10.0-windows. The project is in early development with a blank main form as the starting point.

## Commands

All commands should be run from `MsGraphEmailClient/` (the project directory, not the solution root):

```powershell
dotnet build                            # Build the project
dotnet run                              # Build and launch the app
dotnet publish -c Release -o ./publish  # Publish a release build
dotnet clean                            # Clean build artifacts
```

The solution file is `MsGraphEmailClient.slnx` (modern VS solution format). You can also open and build from Visual Studio.

## Architecture

### Project structure

```
MsGraphEmailClient/
  Program.cs          # Entry point — STA thread, Application.Run(new Form1())
  Form1.cs            # UI event handlers (Connect, folder select, message select)
  Form1.Designer.cs   # Hand-authored control layout — safe to edit manually
  GraphService.cs     # All Microsoft Graph API calls; no WinForms dependency
  MsGraphEmailClient.csproj
```

### UI layout (Form1.Designer.cs)

```
[pnlCredentials — Dock=Top, 40px]
  Tenant ID | Client ID | Secret (masked) | Mailbox email | [Connect] | status label
[scMain — SplitContainer Vertical, Dock=Fill]
  Panel1 (200px): lstFolders (ListBox)
  Panel2:
    [scContent — SplitContainer Horizontal]
      Panel1 (220px): lvMessages (ListView — From / Subject / Date columns)
      Panel2:         wbPreview  (WebBrowser)
```

`Controls.Add(scMain)` must precede `Controls.Add(pnlCredentials)` — WinForms docking processes the Controls collection in reverse; Fill must be lower z-index than Top.

### GraphService.cs

Authentication uses `ClientSecretCredential` (Azure.Identity) with scope `https://graph.microsoft.com/.default` (client credentials / app-only). All Graph calls go through `graphClient.Users[mailboxEmail].*`.

- `GetFoldersAsync()` — top-100 folders via `PageIterator`
- `GetMessagesAsync(folderId, top=50)` — message summaries ordered by `receivedDateTime desc`; body is NOT fetched here
- `GetMessageBodyAsync(messageId)` — fetches only `body`; returns HTML directly or plain-text wrapped in `<pre>` using `System.Net.WebUtility.HtmlEncode`

### Key facts

- **WinForms + .NET 10.0**: `ImplicitUsings` enabled — `System`, `System.Drawing`, `System.Windows.Forms`, etc. are global; no need to add them in source files.
- **NuGet packages**: `Microsoft.Graph 5.105.0` and `Azure.Identity 1.21.0`. The `Microsoft.Kiota.Abstractions` transitive dependency carries a known advisory warning (NU1903) — expected, not a build error.
- **Async event handlers**: all three event handlers (`BtnConnect_Click`, `LstFolders_SelectedIndexChanged`, `LvMessages_SelectedIndexChanged`) are `async void` with full try/catch — required because WinForms events cannot await Task.
- **STA requirement**: Windows Forms requires `[STAThread]` on `Main` in `Program.cs` — do not remove it.

## Development notes

- The project targets `net10.0-windows` — Windows only.
- There are no tests yet. When adding tests, use a separate xUnit project referenced in the solution.
- Azure AD app registration prerequisites: Application permission `Mail.Read` on Microsoft Graph (not Delegated), admin consent granted, target mailbox in the same tenant.
