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

- [x] Återinför inloggningsspärr på skyddade sidor  
  **Allvarligt fynd, upptäckt sent:** hela appen gick att nå utloggad, utan att någon kod
  klagade. Skyddet av sidorna (allt utom inloggningssidan) satt aldrig i den här kodbasen —
  det sköttes av Umbracos "Public Access"-nodskydd, konfigurerat i CMS-innehållsträdet, inte i
  kod. Det försvann tyst när Umbraco togs bort och syntes inte förrän `ChalmersILLController`
  (rotsidan `/`) slutade krascha (se punkten ovan om `RenderBody`) och den okrypterade
  startsidan blev synlig. `Web.config` hade dessutom `<authorization><allow users="?" />`
  (tillåter alla) och `<forms loginUrl="login.aspx">` som pekade på en aspx-sida som aldrig
  funnits i MVC-appen. Åtgärdat med en global `AuthorizeAttribute` registrerad via ny
  `App_Start/FilterConfig.cs` (kallad från `Global.asax.cs`), med `[AllowAnonymous]` bara på
  `ChalmersILLLoginPageController`, `LoginSurfaceController` (login-POST) och
  `OrderItemReceivedAtBranchSurfaceController` (QR-kod-flödet för redan utskrivna följesedlar,
  se `LegacyUmbracoSurfaceAlias`-kommentaren i `RouteConfig.cs`). `loginUrl` uppdaterad till
  `~/ChalmersILLLoginPage`. Karakteriseringstester i `AuthorizationTest.cs` verifierar både att
  filtret registreras och exakt vilka controllers som är undantagna.

- [x] Åtgärda mojibake (Ã¤/Ã¥/Ã¶) på sidor utan BOM  
  Efter att inloggningsspärren ovan gjorde inloggningssidan synlig för alla igen upptäcktes att
  åäö visades som `Ã¤`/`Ã¥`/`Ã¶` där. Orsak: `Web.config`s `<globalization>` deklarerade
  `requestEncoding`/`responseEncoding` men inte `fileEncoding`, så ASP.NET:s Razor-parser läser
  `.cshtml`-filer utan byte-order-mark (BOM) med systemets ANSI-kodsida istället för UTF-8 — 24
  av 54 vyer saknar BOM, inklusive huvudlayouten `ChalmersILL.cshtml` och `ChalmersILLLoginPage.cshtml`.
  **Rättelse:** detta är, till skillnad från vad som först antecknades här, en riktig regression
  från Umbraco-borttagningen, inte en obesläktad gammal bugg. `git show` på commit
  "Hello MVC views" (0d2279e, 2026-08-20) visar att raden som bytte ut Umbracos
  `@inherits Umbraco.Web.Mvc.UmbracoViewPage<...>` mot `@inherits System.Web.Mvc.WebViewPage<...>`
  hade BOM före ändringen (`-﻿@inherits ...` i diffen) men saknar den efter — samma mönster
  upprepas i minst `ChalmersILLLoginPage.cshtml`, `ChalmersILLDiskPage.cshtml` och
  `ChalmersILLLogoutPage.cshtml`. Verktyget/editorn som gjorde de raderna sparade filerna utan
  BOM under den migreringen, vilket gjort dem sårbara sedan augusti — det syntes bara inte
  förrän inloggningsspärren ovan gjorde att `ChalmersILLLoginPage.cshtml` faktiskt renderades.
  Första försöket: `fileEncoding="UTF-8"` tillagt i `<globalization>`. **Räckte inte** —
  verifierat i drift efteråt med kvarstående mojibake (`inkÃ¶psfÃ¶rslag` i
  `ChalmersILLOrderListPage.cshtml`), trots att filens bytes på disk fortfarande var giltig,
  enkelkodad UTF-8. Slutsats: `<globalization fileEncoding>` respekteras bara av det klassiska
  `.aspx`-sidparsersystemet, inte av Razor-build-providern som läser `.cshtml`. Den faktiska
  fixen är att lägga till en riktig UTF-8-BOM i varje `.cshtml`-fil som saknar en — det
  respekteras universellt oavsett build provider. Gjort med nytt script
  `scripts/Add-Utf8BomToCshtml.ps1` (rena byte-operationer, rör inte filernas innehåll) kört mot
  `Chalmers.ILL/Views` — 8 filer saknade fortfarande BOM där (`ChalmersILL.cshtml`,
  `ChalmersILLDiskPage.cshtml`, `ChalmersILLLoginPage.cshtml`, `ChalmersILLLogoutPage.cshtml`,
  `ChalmersILLOrderListPage.cshtml`, `ChalmersILLSettingsPage.cshtml`, `ChalmersILLStartPage.cshtml`,
  `Partials/Settings/MemberAdmin.cshtml`); resten av de tidigare räknade 24 filerna utan BOM
  ligger under `Chalmers.ILL/Umbraco/Scripting/templates/cshtml/` — oanvänd Umbraco-scaffolding,
  inte riktiga appvyer, lämnade orörda. `fileEncoding="UTF-8"` i `Web.config` behålls ändå som
  extra skydd (ofarligt, kan skydda `.aspx`-relaterat innehåll om sådant tillkommer).

- [x] Fixa trasiga omdirigerings-URL:er efter inloggning  
  Upptäckt när ett konto faktiskt loggades in för första gången sedan inloggningsspärren ovan
  återinfördes: `LoginSurfaceController.HandleLogin` omdirigerar till
  `ConfigurationManager.AppSettings["orderListPageUrl"]` (icke-Desk-konton) eller hårdkodat
  `/disk/` (Desk-konton) — men `orderListPageUrl` stod till `/lista` i `Web.config`, en gammal
  slug som aldrig haft en matchande route, och `/disk/` saknade route helt. Ingen av URL:erna
  märktes tidigare eftersom ingen inloggning någonsin behövde slutföras (se punkten om
  inloggningsspärr ovan). `orderListPageUrl` ändrat till `/bestaellningar` (samma kanoniska slug
  som redan återinfördes i `RouteConfig.cs`, se punkten om routing ovan). Ny
  `LegacyDiskSlugAlias`-route tillagd i `RouteConfig.cs` (samma mönster som
  `LegacyBestaellningarSlugAlias`) så `/disk/` faktiskt resolvar mot
  `ChalmersILLDiskPageController`. Nya routingtester i `RoutingTest.cs`.

- [x] Bygg en SuperAdmin-sida för kontohantering  
  Ny flik "Konton" i Inställningar-sidan (`MemberAdminSurfaceController` +
  `Views/Partials/Settings/MemberAdmin.cshtml`), synlig bara om
  `Roles.IsUserInRole(CurrentMemberLoginName, "SuperAdmin")` (samma mönster som
  `ChalmersILL.cshtml` redan använder för `Administrator`). Kan skapa konto, byta lösenord,
  sätta roller (kommaseparerad textruta) och ta bort konto — allt via en ny
  `IMemberAdminService`/`MemberAdminService` som läser/skriver samma
  `Chalmers.ILL/Config/members.json` som `FileMembershipProvider`/`FileRoleProvider` (se punkten
  ovan), så ändringar via sidan gäller direkt utan omstart. Registrerad i `Bootstrapper.cs`.
  `SuperAdmin` kräver ingen kodändring i providrarna — rollnamn är redan fritextsträngar i
  `members.json`, sätts bara på det första kontot manuellt (eller via sidan när minst ett konto
  redan har `SuperAdmin`).

  **Sidoupptäckt, inte åtgärdad:** två vyer (`ChangePassword.cshtml` x2) använder fortfarande en
  riktig Umbraco-helper (`Html.BeginUmbracoForm<T>()`), och en tredje (`EditTemplates.cshtml`)
  anropar den gamla `/umbraco/surface/...`-routen i sin inbäddade JS. Se egen TODO-punkt under
  "Routing & Vyer". Den nya koden här (MemberAdmin) använder genomgående vanliga HTML-formulär
  och den nya URL-formen.

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

- [x] Ta bort kvarvarande Umbraco-beroenden i två vyer  
  `Views/Partials/Settings/ChangePassword.cshtml` anropade `Html.BeginUmbracoForm<PasswordSurfaceController>(...)`
  — en riktig Umbraco-helper (kräver `Umbraco.Web`), och med en generisk type-constraint
  (`where TController : SurfaceController`) som `PasswordSurfaceController` inte längre uppfyller
  sedan SurfaceController-bytet tidigare i listan. Eftersom Razor-vyer bara kompileras vid första
  anropet (inget `MvcBuildViews` i csprojen) hade detta sannolikt gått obemärkt förbi som ett
  runtime-fel — sidan "Byt lösenord" i Inställningar var med stor sannolikhet trasig. Ersatt med
  vanligt `Html.BeginForm("ChangePassword", "PasswordSurface", FormMethod.Post, ...)`.
  `PasswordSurfaceController.ChangePassword` byggde dessutom sina redirects på
  `Request.Url.AbsolutePath` (ett Umbraco-trick för att posta till en "snygg" URL och landa
  tillbaka på samma sida) — pekade på `/bestaellningar/instaellningar`, en URL som vid det här
  laget inte matchade `RouteConfig` alls (redirects pekade tillfälligt uttryckligen på
  `/ChalmersILLSettingsPage` istället). **Uppdaterat igen senare:** `/bestaellningar` och
  `/bestaellningar/instaellningar` är återinförda som permanenta alias-routes i `RouteConfig.cs`
  (samma mönster som `LegacyUmbracoSurfaceAlias`), så att gamla bokmärken till dessa "fina" slugs
  fortsätter fungera. Samtliga hårdkodade `/ChalmersILL{OrderListPage,SettingsPage}`-länkar i
  vyer och denna controller pekar nu återigen på de gamla sluggarna; se `RoutingTest.cs`.
  `Views/Partials/Chalmers.ILL.ChangePassword.cshtml` (och dess modell `PasswordModel.cs`) var
  dödkod utan anropare — borttagna helt, liknande `GetMemberSurfaceController` tidigare.
  `Views/Partials/Settings/EditTemplates.cshtml`s inbäddade JS bytte från den gamla
  `/umbraco/surface/TemplatesSurface/...`-routen till `/TemplatesSurface/...`, som resten av
  Inställningar-sidan redan använder. De fyra `Umbraco.*`-namnrymderna i `Views/Web.config`s
  globala Razor-`<namespaces>` togs bort — inget `.cshtml`-fil använder dem längre.

  **OBS — mycket större kvarstående fynd, ej åtgärdat:** samma gamla `/umbraco/surface/...`-rutt
  används fortfarande av **12 andra vyer**, bland dem centrala orderhanteringsflöden
  (`Chalmers.ILL.Action.Return.cshtml`, `...PatronData.cshtml`, `...Delivery.cshtml`,
  `...Claim.cshtml`, `...Mail.cshtml`, `...Provider.cshtml`, `...ProviderReturnDate.cshtml`,
  `...PatronReturnDate.cshtml`, `DeliveryType/ArticleByEmail.cshtml`,
  `DeliveryType/ArticleByMailOrInternalMail.cshtml`, `Settings/ChillinText.cshtml`,
  `Settings/ModifyProviderData.cshtml`). Att dessa fortfarande fungerar (appen är i drift) tyder
  på att `UmbracoModule` i `Web.config` fortfarande aktivt routar `/umbraco/surface/*`-anrop till
  rätt controller/action, trots att `SurfaceController`-basklassen är borttagen — dvs. en dold,
  fungerande beroendekedja till Umbraco som blockerar att `UmbracoModule` tas bort. Det här är ett
  betydligt större och riskablare jobb än vyerna ovan (rör kärnfunktionalitet, inte bara
  Inställningar) och bör vara en egen TODO-punkt/avstämning innan `UmbracoCms`-paketen tas bort
  (se "NuGet-paket" nedan).

- [x] Migrera de återstående 12 vyerna bort från `/umbraco/surface/`-routen  
  Bytte `/umbraco/surface/{Controller}Surface/{Action}` till `/{Controller}Surface/{Action}` i
  samtliga 12 (14 anropsställen): `Chalmers.ILL.Action.Return/PatronData/Delivery/Claim/Mail/
  Provider/ProviderReturnDate/PatronReturnDate.cshtml`, `DeliveryType/ArticleByEmail.cshtml`,
  `DeliveryType/ArticleByMailOrInternalMail.cshtml`, `Settings/ChillinText.cshtml`,
  `Settings/ModifyProviderData.cshtml`. Byggt och testat grönt (dessa ändringar rör bara
  inbäddad JS, ingen `.cs`-kod, så testsviten är opåverkad) — men **ej verifierat i webbläsare**,
  se stora fyndet nedan som gör detta mer osäkert än väntat.

  **OBS — mycket större kvarstående fynd, ej åtgärdat, INTE samma sak som ovan:**
  `Scripts/chalmers.ill.js` — appens huvudsakliga JS-fil — har **~44 anrop** till samma gamla
  `/umbraco/surface/...`-rutt, och täcker i praktiken hela orderhanteringsgränssnittet: låsa/låsa
  upp order, importera dokument, sätta status/typ/leveransbibliotek, leverans, reklamation, mail,
  patrondata, provider, ta emot bok, loggposter m.m. Dessutom bakar
  `OrderItemDeliverySurfaceController.cs` (rad 117) in samma gamla URL i en **QR-kod som skrivs ut
  på en fysisk följesedel** (`OrderItemReceivedAtBranchSurface/RenderResponse`) — skannas senare
  när boken anländer till en filial. Till skillnad från `ChangePassword`-buggen och de 12 vyerna
  ovan är det här kärnfunktionalitet i daglig drift, så det är osannolikt att den är trasig —
  vilket gör det troligt att `UmbracoModule` (fortfarande registrerad i `Web.config`) aktivt
  routar `/umbraco/surface/*` oavsett `SurfaceController`-arv. Detta är en väsentligt större och
  känsligare ändring än vyerna ovan: 44 anropsställen i en fil som allt bygger på, plus redan
  utskrivna fysiska följesedlar vars QR-koder pekar på den gamla URL:en och som inte kan bytas ut
  i efterhand. Kräver en medveten avstämning (och sannolikt manuell verifiering i webbläsare,
  inte bara textersättning) innan den rörs — se separat punkt nedan.

- [x] Migrera `chalmers.ill.js` och QR-kodsgenereringen bort från `/umbraco/surface/`-routen  
  Bytte alla ~44 `/umbraco/surface/{Controller}Surface/{Action}`-anrop i `Scripts/chalmers.ill.js`
  till `/{Controller}Surface/{Action}`, samma form som resten av appen. QR-kodsgenereringen i
  `OrderItemDeliverySurfaceController.cs` (rad 117) genererar nu också den nya URL:en för
  **nya** följesedlar. För att redan utskrivna följesedlar (fysiska, kan inte bytas ut) ska
  fortsätta fungera lades en explicit alias-route till i `RouteConfig.cs`:
  `umbraco/surface/{controller}/{action}/{id}` → samma controller/action som den vanliga
  `{controller}/{action}/{id}`-routen. Eftersom kontroller- och actionnamnen inte ändrats av
  Umbraco-borttagningen (bara basklassen) räcker en generisk alias-route för alla ~44+14
  anropsställen som fanns, snarare än att särbehandla QR-koden. Alias-routen är avsedd att vara
  kvar permanent (eller tills man är säker på att inga fysiska följesedlar med den gamla URL:en
  längre är i cirkulation) — det är den, inte `UmbracoModule`, som nu håller gamla QR-koder vid
  liv. Karaktäriseringstest tillagt i `RoutingTest.cs`.

  **OBS — ej verifierat i webbläsare:** detta rör kärnfunktionalitet i appen (orderhantering)
  och kunde inte köras/klickas igenom i denna miljö (kräver IIS Express, LocalDB, Elasticsearch
  m.m.). Byggt och testat grönt, men bör stämmas av manuellt i en riktig miljö innan man litar på
  att `UmbracoModule` verkligen kan tas bort.

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

- [x] Ta bort `UmbracoCms`/`UmbracoCms.Core`-paketen  
  Punkterna under "Autentisering & Membership" var redan klara (den ursprungliga farhågan om att
  `umbraco.dll` fortfarande användes direkt av tre controllers var inaktuell) — en genomsökning hittade
  bara två kvarvarande C#-beröringspunkter: en oanvänd `using Umbraco.Web.Models;` i
  `ChalmersILLLogoutPageModel.cs`, och en död `ReportNewOrderItemUpdate(IContent d) {}`-overload
  (plus dess `using Umbraco.Core.Models;`) i testprojektets `StubNotifier`
  (`OrderItemSurfaceControllerTest.cs`) — noterad som kvarlämning i "Tester"-punkten ovan. Båda
  togs bort utan vidare ersättning (dödkod). Alla övriga träffar på "umbraco" i kodbasen visade sig
  vara namnrymden `Chalmers.ILL.UmbracoApi` (avsiktligt kvar, se arkitekturnoteringen), URL-strängen
  `umbraco/surface/...` i alias-routen/QR-koden (avsiktligt kvar), eller konstantnamn/kommentarer.

  Själva paketreferenserna togs bort ur `Chalmers.ILL.csproj`/`packages.config`: alla
  `UmbracoCms.Core.6.1.6\lib\*`-referenser (`umbraco`, `Umbraco.Core`, `Umbraco.Web.UI`,
  `businesslogic`, `cms`, `controls`, `interfaces`, `umbraco.DataLayer`, `umbraco.editorControls`,
  `umbraco.MacroEngines`, `umbraco.macroRenderings`, `umbraco.providers`, `umbraco.XmlSerializers`,
  `UrlRewritingNet.UrlRewriter`, `SQLCE4Umbraco`, `System.Data.SqlServerCe(.Entity)`, `TidyNet`,
  `Microsoft.ApplicationBlocks.Data`, `Microsoft.Web.Helpers`, `Our.Umbraco.uGoLive*`), `UmbracoCms`-
  och `UmbracoCms.Core`-posterna i `packages.config`, samt `UmbracoCms.props`/`.targets`-import och
  felkontrollerna i csprojens `EnsureNuGetPackageBuildImports`-target. `Umbraco.Core`-referensen i
  `Chalmers.ILL.Tests.csproj` togs bort i samma veva. **OBS:** `log4net`-referensen pekade på
  `UmbracoCms.Core.6.1.6\lib\log4net.dll` — den pekar nu istället på den redan deklarerade
  fristående `log4net`-paketreferensen (`packages.config` hade redan `log4net 2.0.12` men csprojen
  använde av misstag fortfarande Umbraco-kopian).

  `Web.config` städades: `FileSystemProviders`-, `BaseRestExtensions`- och `microsoft.scripting`-
  sektionerna (med tillhörande `configSource`), alla `umbraco*`-appSettings (två block, inklusive
  de "Umbraco connections"-nycklar som pekade på content-/data type-namn — verifierat att ingen av
  dem läses någonstans i koden), `UmbracoModule`-registreringen (både den klassiska och IIS7-
  varianten), Umbraco-handlers (channels, GoogleSpellChecker), `umbraco`-tagPrefixet, samt
  `RazorBuildProvider`-overriden för `.cshtml`/`.vbhtml`/`.razor` togs bort. Den redan inaktiverade,
  utkommenterade "Umbraco backoffice locked to Chalmers network"-rewrite-regeln togs också bort.
  `<system.web.webPages.razor>`s `host`/`pages`/`namespaces` bytte från
  `umbraco.MacroEngines.RazorUmbracoFactory`/`DynamicNodeContext`/`umbraco`-namnrymden till samma
  standard-MVC-inställning (`MvcWebRazorHostFactory`/`WebViewPage`/`System.Web.Mvc*`-namnrymder) som
  `Views/Web.config` redan använder sedan vymigreringen tidigare i listan. Motsvarande fyra
  `Umbraco.*`-namnrymder togs bort ur `Views/Web.config.transform` (publish-transformen, missad vid
  den tidigare städningen av `Views/Web.config`).

  Elva rena Umbraco-konfigfiler utan kodberoenden togs bort helt (`Config/404handlers.config`,
  `BaseRestExtensions.config`, `EmbeddedMedia.config`, `feedProxy.config`, `FileSystemProviders.config`,
  `metablogConfig.config`, `restExtensions.config`, `scripting.config`, `Skinning.config`,
  `tinyMceConfig.config`, `xsltExtensions.config`) tillsammans med `Config/Splashes/booting.aspx`
  och `noNodes.aspx` (den senare hade en `CodeBehind`-attribut som pekade på en `noNodes.aspx.cs`
  som inte ens fanns — redan dödkod) och deras `<Content Include>`-poster i csprojen.

  Lokala, gitignorade runtime-kvarlämningar i `App_Data` (`Umbraco.sdf`, `umbraco.config`,
  `Logs/UmbracoTraceLog.txt*`, `TEMP/PluginCache/umbraco-plugins.*`) rördes inte — ospårade i git
  och utanför scope. De fysiska paketmapparna `packages\UmbracoCms.6.1.6`/`UmbracoCms.Core.6.1.6`
  (också gitignorade) lämnades kvar på disk men refereras inte längre av något projekt.

  **OBS — ej verifierat i webbläsare:** borttagningen av `RazorBuildProvider`-overriden och bytet av
  `system.web.webPages.razor`-värdfabriken påverkar bara hur `.cshtml`-vyer kompileras vid körning,
  vilket varken bygget eller testsviten (som inte renderar vyer) täcker. Byggt och alla 128 tester
  gröna före och efter, men liksom de tidigare routing-punkterna bör detta stämmas av manuellt i en
  riktig miljö (IIS/IIS Express) innan man litar på att appen fungerar utan `UmbracoCms`-paketen i
  produktion.

## Tester

- [x] Rensa Umbraco/Examine-beroenden i `Chalmers.ILL.Tests`  
  Alla Examine/Umbraco-referenser i testerna var döda (kvarvarande `using`-rader och en oanvänd
  `GetFakeSearchCriteria()`-hjälpmetod) — inga testomskrivningar behövdes. Borttaget: `Examine`-
  referenserna och `Fakes\Examine.fakes`, döda `using Examine*` + hjälpmetoder i `StatisticsTest.cs`,
  `AutomaticMailSendingEngineTest.cs` och `BulkDataManagerTest.cs`, samt den oanvända `umbraco`-
  referensen (`umbraco.dll`). OBS: `Umbraco.Core`-referensen måste vara kvar tills vidare —
  `OrderItemSurfaceControllerTest.cs` använder `Umbraco.Core.Models.IContent` i en (för närvarande
  oanvänd) `StubNotifier`-metod.
