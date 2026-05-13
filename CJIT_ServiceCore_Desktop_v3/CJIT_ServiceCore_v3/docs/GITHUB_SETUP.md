# CJIT ServiceCore - GitHub Setup

This project is ready to push to GitHub.

## Option 1 - Push from your Windows PC

1. Create a new empty GitHub repository named:

   `CJIT-ServiceCore`

2. Extract this ZIP somewhere like:

   `G:\pos\cjit\CJIT_ServiceCore_v3`

3. Open PowerShell in the project folder.

4. Run:

```powershell
git init
git branch -M main
git add .
git commit -m "Initial CJIT ServiceCore desktop v3"
git remote add origin https://github.com/YOUR_USERNAME/CJIT-ServiceCore.git
git push -u origin main
```

## Option 2 - Use the included script

Run:

```powershell
.\PUSH_TO_GITHUB.ps1 -RepoUrl "https://github.com/YOUR_USERNAME/CJIT-ServiceCore.git"
```

## GitHub Actions Build

This repository includes:

`.github/workflows/windows-dotnet-build.yml`

When pushed to GitHub, it will build the Windows desktop app using .NET 8 on `windows-latest` and upload a published build artifact.

## Required SDK locally

Install the .NET 8 SDK on your Windows machine before building locally.

Then run:

```powershell
dotnet restore
dotnet build -c Release
dotnet run -c Release
```
