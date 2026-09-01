<#
.SYNOPSIS
    Hittar och tar bort App_Browsers-filer (.browser) som registrerar ASP.NET
    control adapters mot Umbraco-typer som inte längre finns kvar i projektet.

.DESCRIPTION
    Efter att Umbraco-beroenden tagits bort kan gamla .browser-filer under
    App_Browsers bli kvar och peka på typer som t.ex.
    "umbraco.presentation.urlRewriter.FormRewriterControlAdapter". ASP.NET
    försöker ladda dessa typer vid appstart, vilket kraschar med:
        System.Web.HttpException: Could not load type '...'

    Scriptet skannar rekursivt efter *.browser-filer, letar efter adapterType/
    userControl/type-attribut som innehåller "umbraco" (case-insensitive) och
    listar dem. Med -Delete tas de matchande filerna bort.

    Body/output skrivs på svenska för att matcha projektets konventioner.

.PARAMETER Path
    Rotmapp att skanna. Standard: aktuell katalog.

.PARAMETER Delete
    Ta faktiskt bort de matchande filerna. Utan denna flagga körs scriptet
    bara i granskningsläge (dry run) och listar vad som skulle tagits bort.

.EXAMPLE
    .\Remove-DeadUmbracoBrowserAdapters.ps1 -Path "C:\inetpub\wwwroot\ChillinTest"

.EXAMPLE
    .\Remove-DeadUmbracoBrowserAdapters.ps1 -Path "C:\inetpub\wwwroot\ChillinTest" -Delete
#>
param(
    [string]$Path = ".",
    [switch]$Delete
)

$excludedDirs = @('packages', 'node_modules', 'bower_components', 'bin', 'obj', '.git')

$browserFiles = Get-ChildItem -Path $Path -Recurse -Filter *.browser -File -ErrorAction SilentlyContinue |
    Where-Object {
        $fullPath = $_.FullName
        -not ($excludedDirs | Where-Object { $fullPath -match "\\$_\\" })
    }

if (-not $browserFiles) {
    Write-Host "Inga .browser-filer hittades under '$Path'." -ForegroundColor Yellow
    exit 0
}

$matches = @()

foreach ($file in $browserFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    if ($content -match '(?i)umbraco') {
        $matches += $file
    }
}

if (-not $matches) {
    Write-Host "Hittade $($browserFiles.Count) .browser-fil(er) under '$Path', men inga refererade till Umbraco." -ForegroundColor Green
    exit 0
}

Write-Host "Hittade $($matches.Count) .browser-fil(er) med Umbraco-referenser:" -ForegroundColor Cyan
foreach ($file in $matches) {
    Write-Host "  $($file.FullName)"
}

if ($Delete) {
    foreach ($file in $matches) {
        Remove-Item -Path $file.FullName -Force
        Write-Host "Borttagen: $($file.FullName)" -ForegroundColor Red
    }
    Write-Host "`nKlart. $($matches.Count) fil(er) borttagna." -ForegroundColor Green
} else {
    Write-Host "`nDetta var en granskning (dry run) - inga filer togs bort." -ForegroundColor Yellow
    Write-Host "Kör med -Delete för att faktiskt ta bort filerna."
}
