<#
.SYNOPSIS
    Backs up the Generational Journal SQLite database and media files to a
    NAS share or external drive using robocopy.

.DESCRIPTION
    Copies the following to the destination:
      - data/familyjournal.db   (the SQLite database)
      - data/media/             (uploaded photos and videos)

    By default a timestamped folder is created under -Destination so each run
    keeps its own snapshot. Pass -Mirror to sync directly into the destination
    root instead (useful for a fixed NAS share that should mirror the live
    data and drop deleted files).

.PARAMETER Destination
    Root path to back up to. May be a UNC path (\\nas\backups) or a local
    external drive (D:\backups). Required.

.PARAMETER Mirror
    Mirror into the destination root directly instead of a timestamped
    subfolder. Robocopy /MIR deletes files in the destination that no longer
    exist in the source.

.PARAMETER RetainDays
    When using timestamped backups, deletes timestamped backup folders older
    than this many days. Defaults to 30. Set to 0 to keep everything.

.PARAMETER WhatIf
    Shows what would happen without copying anything.

.EXAMPLE
    .\scripts\backup.ps1 -Destination "\\nas\family-backups"

.EXAMPLE
    .\scripts\backup.ps1 -Destination "D:\backups\journal" -Mirror

.EXAMPLE
    .\scripts\backup.ps1 -Destination "E:\backups" -RetainDays 14
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true)]
    [string]$Destination,

    [switch]$Mirror,

    [int]$RetainDays = 30,

    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$dataDir = Join-Path $repoRoot 'data'
$dbFile = Join-Path $dataDir 'familyjournal.db'
$mediaDir = Join-Path $dataDir 'media'

if (-not (Test-Path -LiteralPath $dbFile)) {
    Write-Error "Database file not found: $dbFile"
    exit 1
}

if (-not (Test-Path -LiteralPath $mediaDir)) {
    Write-Error "Media directory not found: $mediaDir"
    exit 1
}

if ($Mirror) {
    $backupRoot = $Destination
}
else {
    $backupRoot = Join-Path $Destination ("journal-{0:yyyyMMdd-HHmmss}" -f (Get-Date))
}

# Map robocopy exit codes (0-7 are success) to a readable message and process
# exit code. Values 8 and above indicate a failure.
function Assert-RobocopySuccess {
    param([int]$ExitCode, [string]$Label)

    switch ($ExitCode) {
        { $_ -le 7 } {
            Write-Host "[OK] $Label (robocopy exit code $ExitCode)"
            return 0
        }
        default {
            Write-Error "[FAILED] $Label (robocopy exit code $ExitCode)"
            return 1
        }
    }
}

if ($WhatIf -or $PSCmdlet.ShouldProcess($backupRoot, "Back up database and media")) {
    $global:LASTEXITCODE = 0
    $failed = $false

    Write-Host "Source data dir : $dataDir"
    Write-Host "Backup root     : $backupRoot"
    Write-Host ""

    # 1) Database file(s). Copy the .db plus any -wal/-shm sidecar files so a
    #    running instance in WAL mode can be restored consistently.
    Write-Host "Backing up database..."
    $dbBackupDir = Join-Path $backupRoot 'database'
    robocopy $dataDir $dbBackupDir "familyjournal.db*" /R:3 /W:5 /NP
    if ($WhatIf) {
        Write-Host "[WHATIF] robocopy $dataDir $dbBackupDir familyjournal.db* /R:3 /W:5 /NP"
    }
    elseif ($global:LASTEXITCODE -ge 8) { $failed = $true }
    Write-Host ""

    # 2) Media directory. Mirror keeps the NAS in lockstep; default /E adds a
    #    cumulative copy into the timestamped snapshot.
    Write-Host "Backing up media..."
    $mediaBackupDir = Join-Path $backupRoot 'media'
    if ($Mirror) {
        robocopy $mediaDir $mediaBackupDir /MIR /R:3 /W:5 /NP
        if ($WhatIf) {
            Write-Host "[WHATIF] robocopy $mediaDir $mediaBackupDir /MIR /R:3 /W:5 /NP"
        }
    }
    else {
        robocopy $mediaDir $mediaBackupDir /E /R:3 /W:5 /NP
        if ($WhatIf) {
            Write-Host "[WHATIF] robocopy $mediaDir $mediaBackupDir /E /R:3 /W:5 /NP"
        }
    }
    if ($WhatIf) { }
    elseif ($global:LASTEXITCODE -ge 8) { $failed = $true }

    if ($failed) {
        Write-Error "Backup failed. Review the robocopy output above."
        exit 1
    }

    Write-Host ""
    Write-Host "Backup completed to: $backupRoot"
}

# Prune old timestamped snapshots (skipped in -Mirror mode, which has no
# timestamped subfolders to prune).
if (-not $Mirror -and $RetainDays -gt 0) {
    $cutoff = (Get-Date).AddDays(-$RetainDays)
    $folders = Get-ChildItem -LiteralPath $Destination -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like 'journal-*' -and $_.LastWriteTime -lt $cutoff }

    foreach ($folder in $folders) {
        if ($WhatIf -or $PSCmdlet.ShouldProcess($folder.FullName, "Remove expired backup")) {
            Remove-Item -LiteralPath $folder.FullName -Recurse -Force
            Write-Host "[Pruned] $($folder.FullName)"
        }
    }
}
