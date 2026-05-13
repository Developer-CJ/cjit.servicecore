param(
    [Parameter(Mandatory=$true)]
    [string]$RepoUrl
)

$ErrorActionPreference = "Stop"

Write-Host "CJIT ServiceCore GitHub Publisher" -ForegroundColor Cyan
Write-Host "Repository: $RepoUrl" -ForegroundColor Gray

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "Git is not installed or not available in PATH. Install Git for Windows first."
}

if (-not (Test-Path ".git")) {
    git init
}

git branch -M main

git add .

$hasChanges = git status --porcelain
if ($hasChanges) {
    git commit -m "Initial CJIT ServiceCore desktop v3"
} else {
    Write-Host "No new changes to commit." -ForegroundColor Yellow
}

$existingRemote = git remote get-url origin 2>$null
if ($LASTEXITCODE -eq 0 -and $existingRemote) {
    git remote set-url origin $RepoUrl
} else {
    git remote add origin $RepoUrl
}

git push -u origin main

Write-Host "Done. CJIT ServiceCore pushed to GitHub." -ForegroundColor Green
