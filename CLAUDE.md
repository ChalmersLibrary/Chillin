# Umbraco-borttagning

Projektet håller på att ta bort Umbraco-beroenden, se [TODO-remove-umbraco.md](TODO-remove-umbraco.md).

Testtäckningen i [Chalmers.ILL.Tests](Chalmers.ILL.Tests) är bristfällig. Innan en punkt i TODO-listan
görs klar: lägg till eller verifiera ett characterization-test som täcker nuvarande beteende för den
berörda ytan (kontroller/flöde), om det saknas. Testet ska verifiera beteende (t.ex. via HTTP-anrop/output)
snarare än interna Umbraco-typer, så att det överlever omskrivningen.

## Testrutiner

Kör alltid testerna **innan** och **efter** kodändringar för att säkerställa att befintligt beteende
inte brutits. Kör bygge och tester i **ett enda PowerShell-anrop** (dessa exakta kommandon är
förhandsgodkända i `.claude/settings.local.json`):

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" "Chalmers.ILL.Tests\Chalmers.ILL.Tests.csproj" /p:Configuration=Debug /v:minimal; if ($?) { & "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" "Chalmers.ILL.Tests\bin\Debug\Chalmers.ILL.Tests.dll" }
```

Alla tester ska vara gröna innan arbetet rapporteras klart.

## TODO-lista

När en punkt i [TODO-remove-umbraco.md](TODO-remove-umbraco.md) är genomförd, kryssa i den (`[ ]` → `[x]`) direkt.
Committa aldrig kod eller ändringar utan att användaren explicit ber om det.
