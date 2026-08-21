# Ta bort Umbraco från lösningen

Projektet har redan påbörjat en migrering bort från Umbraco (EntityFramework/Elasticsearch/Azure Blob är på plats),
men följande beroenden kvarstår.

## Autentisering & Membership

- [x] Ersätt Umbraco-membership med standard ASP.NET-autentisering  
  `MemberInfoManager.cs` och `LoginSurfaceController.cs` använder `umbraco.cms.businesslogic.member.Member`
  för inloggning, sessionshantering och rollkontroll.

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

- [ ] Ta bort `ExamineSettings.config` / `ExamineIndex.config`  
  Kan inte tas bort ännu: `MaintenanceSurfaceController.cs` och `StatisticsSurfaceController.cs`
  anropar fortfarande `ExamineManager.Instance` direkt, och båda filerna läses via `configSource`
  i `Web.config`. Förutsätter att den Examine-baserade koden i dessa controllers tas bort/ersätts först.

## NuGet-paket

- [ ] Ta bort Umbraco-specifika NuGet-paket från `packages.config` och `.csproj`  
  UmbracoCms 6.1.6, UmbracoCms.Core 6.1.6, ClientDependency, Examine, Lucene.Net, MiniProfiler,
  SharpZipLib, xmlrpcnet m.fl.

## Tester

- [ ] Uppdatera `Chalmers.ILL.Tests`  
  Testerna refererar `umbraco.dll` och Examine-fakes; rensa bort Umbraco-beroenden och uppdatera
  eventuella tester som mockar Umbraco-typer.
