# Umbraco-borttagning

Projektet håller på att ta bort Umbraco-beroenden, se [TODO-remove-umbraco.md](TODO-remove-umbraco.md).

Testtäckningen i [Chalmers.ILL.Tests](Chalmers.ILL.Tests) är bristfällig. Innan en punkt i TODO-listan
görs klar: lägg till eller verifiera ett characterization-test som täcker nuvarande beteende för den
berörda ytan (kontroller/flöde), om det saknas. Testet ska verifiera beteende (t.ex. via HTTP-anrop/output)
snarare än interna Umbraco-typer, så att det överlever omskrivningen.

## Arbetsrutiner

Föredra **Edit-verktyget** framför Bash/PowerShell-kommandon när båda kan lösa uppgiften, eftersom
edits är förhandsgodkända och inte kräver användarinteraktion. Använd scripts bara när det inte finns
ett Edit-alternativ (t.ex. ta bort filer, köra byggen/tester), eller när antalet edits skulle bli
oskäligt stort.

## Testrutiner

Kör alltid testerna **innan** och **efter** kodändringar för att säkerställa att befintligt beteende
inte brutits. Kör bygge och tester som **två separata PowerShell-anrop** — båda är förhandsgodkända
i `.claude/settings.local.json`:

**1. Bygg:**
```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" "Chalmers.ILL.Tests\Chalmers.ILL.Tests.csproj" /p:Configuration=Debug /v:minimal
```

**2. Kör tester (bara om bygget lyckades):**
```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" "Chalmers.ILL.Tests\bin\Debug\Chalmers.ILL.Tests.dll"
```

Alla tester ska vara gröna innan arbetet rapporteras klart.

## Arkitekturnoter

- `IUmbracoWrapper`/`UmbracoWrapper` är borttagna. Inga klasser använder längre detta interface.
- `ChillinOrderConfiguration`/`IChillinOrderConfiguration` ligger kvar i namnrymden `Chalmers.ILL.UmbracoApi`
  men är **inte** Umbraco-typer — de är appkonfiguration. `Bootstrapper.cs` importerar fortfarande
  `using Chalmers.ILL.UmbracoApi` av den anledningen.
- Den primära `IOrderItemManager` är `EntityFrameworkOrderItemManager`. Den äldre `OrderItemManager`
  (Umbraco/Examine-baserad) är fortfarande kompilerad och registrerad som `"Legacy"` — se TODO-listan.
- `INotifier.ReportNewOrderItemUpdate` har bara `OrderItemModel`-overloaden kvar; `IContent`-overloaden
  är borttagen.

## TODO-lista

När en punkt i [TODO-remove-umbraco.md](TODO-remove-umbraco.md) är genomförd, kryssa i den (`[ ]` → `[x]`) direkt.
Committa aldrig kod eller ändringar utan att användaren explicit ber om det.
