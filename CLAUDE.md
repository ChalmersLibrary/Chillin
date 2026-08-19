# Umbraco-borttagning

Projektet håller på att ta bort Umbraco-beroenden, se [TODO-remove-umbraco.md](TODO-remove-umbraco.md).

Testtäckningen i [Chalmers.ILL.Tests](Chalmers.ILL.Tests) är bristfällig. Innan en punkt i TODO-listan
görs klar: lägg till eller verifiera ett characterization-test som täcker nuvarande beteende för den
berörda ytan (kontroller/flöde), om det saknas. Testet ska verifiera beteende (t.ex. via HTTP-anrop/output)
snarare än interna Umbraco-typer, så att det överlever omskrivningen.
