# Ta bort Umbraco från lösningen

Projektet har redan påbörjat en migrering bort från Umbraco (EntityFramework/Elasticsearch/Azure Blob är på plats),
men följande beroenden kvarstår.

## Autentisering & Membership

- [x] Ersätt Umbraco-membership med standard ASP.NET-autentisering  
  `MemberInfoManager.cs` och `LoginSurfaceController.cs` använder `umbraco.cms.businesslogic.member.Member`
  för inloggning, sessionshantering och rollkontroll.

- [x] Ersätt kvarvarande direktanrop till `umbraco.cms.businesslogic.member.Member`  
  `GetMemberSurfaceController.cs` (`GetMemberNameById`) var dödkod utan anropare någonstans i
  kodbasen (varken server- eller klientsidan) — borttagen helt. `OrderItemSurfaceController.cs`s
  tre `new Member(memberId).Text`-uppslag ersattes med `_memberInfoManager.GetCurrentMemberText(...)`,
  samma mönster som redan användes av `GetLocksForCurrentMember` i samma fil (memberId var alltid
  det inloggade medlemmens id). `PasswordSurfaceController.cs`s lösenordsbyte skrevs om till att
  använda standard `System.Web.Security.Membership.ValidateUser`/`MembershipUser.ChangePassword`
  istället för `Member.GetMemberFromLoginNameAndPassword` + egen HMACSHA1-hash + `m.Save()` — samma
  väg som redan används för inloggning i `LoginSurfaceController`. Utöver dessa tre hittades och
  togs döda `using umbraco.cms.businesslogic.datatype`/`Umbraco.Core*`-rader bort i ytterligare
  13 filer (kvarlämningar från tidigare TODO-punkter). Karakteriseringstester tillkom för
  `PasswordSurfaceController` och för lås/upplås/ta över-flödena i `OrderItemSurfaceController`
  (de senare gick inte att testa innan ändringen eftersom de krävde en riktig Umbraco-databas).
  Inga `umbraco`/`Umbraco.Core`-referenser kvarstår i något `.cs`-fil under `Chalmers.ILL` — kvar
  är bara `Umbraco.Core.Models.IContent` i en oanvänd `StubNotifier`-metod i testprojektet, se
  anteckning under "Tester" nedan.

- [x] Byt `Web.config`s membership-/role-provider från Umbraco  
  Efter avstämning med användaren valdes fil-baserad medlemslagring istället för en ny databas
  (det finns färre än 20 konton totalt, och målet är att bli av med databasberoenden helt, inte
  bara flytta dem). `UmbracoMembershipProvider`/`UmbracoRoleProvider` ersattes med två nya,
  minimala providers — `Chalmers.ILL.Members.FileMembershipProvider`/`FileRoleProvider` — som
  läser konton från `Chalmers.ILL/Config/members.json` (gitignorad, samma mönster som
  `chillinPrevalues.json`; se `members.example.json` för formatet). Lösenord hashas med
  `System.Web.Helpers.Crypto.HashPassword`/`VerifyHashedPassword` (PBKDF2, redan en transitiv
  `Microsoft.AspNet.WebPages`-referens — inget nytt paket). `LoginSurfaceController.cs` och
  `PasswordSurfaceController.cs` är oförändrade eftersom de bara anropar de statiska
  `Membership`/`Roles`-fasaderna, som nu routas till de nya providrarna via Web.config.
  `umbracoDbDSN`-connection-stringen är borttagen.

  **OBS — krävs innan driftsättning:** de gamla Umbraco-lösenordshasharna kan inte migreras
  (envägshash i Umbracos eget format), så alla ~20 konton behöver nya lösenord. Skapa
  `Chalmers.ILL/Config/members.json` (finns inte ännu — utan den kan ingen logga in) enligt
  `members.example.json`. Generera varje lösenordshash t.ex. via PowerShell:
  ```powershell
  Add-Type -Path "Chalmers.ILL\bin\System.Web.Helpers.dll"
  [System.Web.Helpers.Crypto]::HashPassword("nya-losenordet-har")
  ```

- [ ] Bygg en SuperAdmin-sida för kontohantering  
  Filbaserad medlemslagring (`FileMembershipProvider`/`FileRoleProvider`/`MemberFileStore`, se
  punkten ovan) har idag ingen UI — konton skapas och lösenord byts genom att redigera
  `Chalmers.ILL/Config/members.json` för hand plus ett separat PowerShell-anrop för att hasha
  lösenordet. Bygg en admin-sida (skapa konto, sätta/byta lösenord, tilldela roller) som gör
  detta via `MemberFileStore.Load()`/`Save()` istället, motsvarande den access till Umbracos
  gamla backoffice/admingränssnitt som superanvändare hade. Gate:a sidan bakom en ny roll
  `SuperAdmin` (skild från `Administrator`, som redan används för annat i appen) så att bara
  de som tidigare hade Umbraco-adminåtkomst kan hantera konton.

## Controllers

- [x] Byt ut `SurfaceController` som basklass i alla ~40 controllers till standard MVC `Controller`  
  Alla controllers under `Controllers/SurfaceControllers/` ärver `Umbraco.Web.Mvc.SurfaceController`.

- [x] Byt ut `RenderMvcController` i sidkontrollanterna under `Controllers/SurfaceControllers/Page/`  
  `ChalmersILLController` m.fl. ärver `Umbraco.Web.Mvc.RenderMvcController` och arbetar mot `RenderModel`.

## Routing & Vyer

- [x] Ersätt Umbraco content-baserad routing med standard MVC-routing  
  Nuvarande URL-routing drivs av Umbracos innehållsträd; ersätt med konventionell `RouteConfig` och egna routes.

- [x] Migrera vyerna från Umbraco Razor-mallar till standard MVC-vyer  
  `Views/*.cshtml` och `Views/Partials/*.cshtml` använder Umbraco-specifika modeller (`RenderModel`) och `@Umbraco`-helper.

## IUmbracoWrapper

- [x] Ta bort `IUmbracoWrapper` / `UmbracoWrapper`  
  Wrappern hanterar Umbracos data types, dropdown-prevalue-listor, relationer och content-XPath-queries.
  Ersätt prevalue-listor med konfiguration/databas och ta bort relationslogiken.

- [x] Ersätt `UmbracoDropdownListNtextDataType`-modellen och `PopulateModelWithAvailableValues`  
  Typer, statuser och leveransbibliotek hämtas idag ur Umbracos data type prevalue-tabeller.

## Legacy-klasser (kan troligen tas bort nu)

- [x] Ta bort legacy `OrderItemManager` (Umbraco-baserad)  
  `OrderItemManager.cs` använder `IContentService` och Examine för att lagra orderobjekt som Umbraco content-noder.
  Ta bort klassen och `"Legacy"`-registreringen i `Bootstrapper.cs`.

- [x] Ta bort `UmbracoOrderItemSearcher`  
  Använder `ExamineManager` mot Umbracos sökindex. Klassen är redan utbytt mot `ElasticSearchOrderItemSearcher`
  men `"Legacy"`-registreringen finns kvar.

- [x] Ta bort `UmbracoMediaItemManager`  
  Använder `IMediaService` för att lagra bilagor som Umbraco media-objekt. Klassen är redan utbytt mot
  `BlobStorageMediaItemManager` men `"Legacy"`-registreringen finns kvar.

- [x] Ta bort `UmbracoOrderItemMigrationSurfaceController`  
  Migreringskontrollern är bara relevant medan det fortfarande finns data kvar i Umbraco.

## Loggning

- [x] Ersätt Umbraco `LogHelper` med standard loggning  
  `IUmbracoWrapper.LogError/LogWarn/LogInfo/LogDebug` och direkta anrop till `Umbraco.Core.Logging.LogHelper`
  används genomgående; byt till log4net direkt eller `ILogger`.

## Bootstrapper & Application startup

- [x] Uppdatera `Bootstrapper.cs`  
  Ta bort `ApplicationContext.Current.Services`, `UmbracoContext.Current` och Unity.Mvc4/Unity.WebApi;
  ersätt med standard DI-uppstart.

- [x] Ta bort Umbraco-koppling i `Global.asax` och pre-build-steget i `Chalmers.ILL.csproj`  
  Pre-build xcopy:ar UmbracoFiles/Content från packages-mappen in i projektet.

## Projekt att ta bort

- [x] Ta bort hela projektet `Chalmers.ILL.PackageActions`  
  `ChillinInitialConfiguration.cs` implementerar `IPackageAction` och sätter upp Umbraco content-träd,
  member groups, media-mappar och relationstyper.

## Konfiguration

- [x] Ta bort Umbraco-konfigurationsfiler utan levande kodberoenden  
  `umbracoSettings.config`, `Dashboard.config`, `trees.config`, `applications.config`,
  `ClientDependency.config` och `UrlRewriting.config` är borttagna (samt motsvarande `<Content Include>`
  i `Chalmers.ILL.csproj` och `urlrewritingnet`-sektionen/modulregistreringen i `Web.config`).

- [x] Ta bort `ExamineSettings.config` / `ExamineIndex.config`  
  `MaintenanceSurfaceController.optimizeIndexes` (optimerade bara det oanvända Umbraco-contentindexet)
  togs bort. `StatisticsSurfaceController.GetAvailableValues` söker nu i `IOrderItemSearcher`
  (ElasticSearch) istället för `ExamineManager.Instance`. Config-filerna och deras `configSource`-
  referenser i `Web.config`/`Chalmers.ILL.csproj` är borttagna.

## NuGet-paket

- [x] Ta bort NuGet-paket/referenser utan kvarvarande kodberoenden  
  `ClientDependency`, `ClientDependency-Mvc`, `CommonServiceLocator`, `Lucene.Net`, `MiniProfiler`,
  `SharpZipLib`, `xmlrpcnet` samt referensen till `Examine` (som levereras inuti `UmbracoCms.Core`)
  är borttagna ur `Chalmers.ILL.csproj`/`packages.config`, tillsammans med döda `using Examine;`-rader
  i 10 filer och motsvarande döda config i `Web.config` (`clientDependency`-sektionen, moduler,
  handlers, Razor-namnrymden `Examine`).

- [ ] Ta bort `UmbracoCms`/`UmbracoCms.Core`-paketen  
  Kräver att punkterna under "Autentisering & Membership" ovan är klara först — `umbraco.dll` används
  fortfarande direkt av tre controllers. När de är migrerade kan hela paketet samt referenserna
  `umbraco`, `Umbraco.Core`, `businesslogic`, `cms`, `interfaces`, `controls`, `umbraco.providers`,
  `umbraco.editorControls`, `umbraco.DataLayer`, `umbraco.XmlSerializers`, `umbraco.macroRenderings`,
  `umbraco.MacroEngines`, `Umbraco.Web.UI`, `UrlRewritingNet.UrlRewriter`, `SQLCE4Umbraco`, `TidyNet`,
  `Microsoft.ApplicationBlocks.Data`, `Microsoft.Web.Helpers`, `Our.Umbraco.uGoLive*` samt
  motsvarande config i `Web.config` (Umbraco-appSettings, `UmbracoModule`, Umbraco-handlers,
  `RazorBuildProvider`/`RazorUmbracoFactory`, `FileSystemProviders`/`BaseRestExtensions`-sektionerna)
  tas bort i ett svep.

## Tester

- [x] Rensa Umbraco/Examine-beroenden i `Chalmers.ILL.Tests`  
  Alla Examine/Umbraco-referenser i testerna var döda (kvarvarande `using`-rader och en oanvänd
  `GetFakeSearchCriteria()`-hjälpmetod) — inga testomskrivningar behövdes. Borttaget: `Examine`-
  referenserna och `Fakes\Examine.fakes`, döda `using Examine*` + hjälpmetoder i `StatisticsTest.cs`,
  `AutomaticMailSendingEngineTest.cs` och `BulkDataManagerTest.cs`, samt den oanvända `umbraco`-
  referensen (`umbraco.dll`). OBS: `Umbraco.Core`-referensen måste vara kvar tills vidare —
  `OrderItemSurfaceControllerTest.cs` använder `Umbraco.Core.Models.IContent` i en (för närvarande
  oanvänd) `StubNotifier`-metod.
