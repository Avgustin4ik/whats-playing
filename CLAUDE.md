# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**cinemarcer** is a .NET 10.0 application that aggregates movie showtimes from local cinemas and delivers them through a Telegram bot. It uses the SerpAPI Google Maps engine to discover nearby cinemas by geo-coordinates, then scrapes showtime data from each cinema chain's own API/site using chain-specific parsers, and renders the result as Markdown or a Telegram MarkdownV2 message.

The project is mid-pivot: the SerpAPI → scrape → render pipeline is fully built and was previously driven directly from `Program.cs`; it is now being wired behind a Telegram bot instead. The bot host scaffolding is in place, but the handler is still the stock Telegram.Bot example and is **not yet connected** to the aggregation pipeline — see "Current state of the Telegram bot" below.

## Commands

```bash
# Build
dotnet build

# Run (starts a long-lived Telegram long-polling bot host)
dotnet run --project cinemarcer-console/cinemarcer-console.csproj

# Set SerpAPI key and bot token for local development (user secrets)
dotnet user-secrets set "SerpApiKey" "<your-key>" --project cinemarcer-console/cinemarcer-console.csproj
dotnet user-secrets set "BotToken" "<your-bot-token>" --project cinemarcer-console/cinemarcer-console.csproj
```

Both `SerpApiKey` and `BotToken` must be provided via user secrets (dev) or `appsettings.json` (the checked-in values are placeholders). `dotnet run` now starts a `Microsoft.Extensions.Hosting` host that polls Telegram for updates and blocks until stopped — it is no longer a one-shot script. No tests exist yet.

## Architecture

The showtime pipeline is: **SerpAPI (Google Maps) → group by cinema website → ScrapperStrategy → IConvertor → Movie[] → MdPrinter / TelegramMessagePrinter**

- **`Program.cs`** — Entry point. Builds a generic `Host`, registers `ITelegramBotClient` (via `AddHttpClient("telegram_bot_client")`, reading `BotToken` from configuration) plus `UpdateHandler`, `ReceiverService`, and the `PollingService` hosted service, registers every `IConvertor` implementation with `ScrapperStrategy`, then calls `host.RunAsync()`. The previous SerpAPI-driven flow (query Google Maps, `ShowtimeAggregator.Aggregate()`, `MdPrinter.Print()`) is commented out at the bottom of the file — it's kept as a reference for the pipeline the bot is meant to invoke once a user shares a location.

- **`Movies/ShowtimeAggregator.cs`** — Groups the raw SerpAPI `JToken` results by the cinema's `website` field (not by name), then calls `SeansesScrapper.Scrap()` for each group.

- **`Movies/SeansesScrapper.cs` + `Movies/ScrapperStrategy.cs`** — Strategy pattern. `ScrapperStrategy.Convertors` is a `Dictionary<string, IConvertor>` registry keyed by each convertor's `Key` (a website URL substring, e.g. `"https://www.galaxycine.vn/"`). `GetProvider(cinemaWebsite)` finds the convertor whose key is contained in the SerpAPI `website` value. Convertors must be registered at startup (see `ScrapperStrategy.Register(...)` in `Program.cs`).

- **`Movies/Abstract/IConvertor.cs`** — Interface for cinema-specific parsers: `Movie[] Convert(IGrouping<string?, JToken> json)` plus a `Key` (website) property used for strategy lookup.

- **`Movies/Abstract/Movie.cs`** — Common movie data model (`struct Movie`): `Title`, `PosterUrl`, `Description`, `StartDate`, `EndDate`, `CinemaName`, `City`, `Country`, `ShowDateTime`, `Duration`, `Rate`. Has a constructor that parses a chain-specific session `JToken` directly (`movie`/`cinema` sub-objects), which convertors can use as a shortcut instead of hand-mapping fields.

- **`Movies/Countries/<country-code>/`** — Country/chain-specific convertors, one HTTP client + one static cache per chain (fetched once, reused across `Convert` calls). All four current convertors are Vietnam (`vn`), each reverse-engineered from that chain's own site/app traffic since none publish official APIs:
  - `GalaxyCinemaConvertor` — the reference implementation. Bootstraps a session cookie from the homepage, calls its mobile sessions API for all cinemas/dates at once, caches the raw sessions, then filters to the cinema names present in the SerpAPI group and maps them via the `Movie(JToken)` constructor.
  - `CgvCinemaConvertor` — calls CGV's own mobile JSON API (`/en/api/cinema/list`, `/en/cinemas/catalog_mobile/siteschedules/id/{id}/date/{ddMMyyyy}`) with the CGV mobile app's `User-Agent`; date must be `ddMMyyyy` with no separators. Its edge only negotiates the legacy `TLS_RSA_WITH_AES_128_CBC_SHA` cipher suite, which .NET's default Linux TLS policy excludes, so its `HttpClient` sets an explicit `CipherSuitesPolicy` to opt back in.
  - `LotteCinemaConvertor` — calls Lotte's ASP.NET AJAX web service at `/LCWS/Ticketing/TicketingData.aspx` (POST, body `paramList=<json>`, method name in the JSON payload). `GetTicketingPage` returns the site-wide cinema and movie catalogs (cached); `GetPlaySequence` with an empty `representationMovieCode` returns every session for one cinema/date (passing multiple movie codes at once breaks the query — only "all" or one at a time work).
  - `BetaCinemaConvertor` — Beta has no JSON API; `Home/GetByCinemaID` on `lichchieu.betacorp.vn` returns a JSON-encoded HTML fragment for one cinema (today's showtimes only), parsed with regex since it's the only chain without structured data.

  Since each chain names/formats cinemas differently from SerpAPI's Google Maps result, every convertor other than Galaxy does its own fuzzy match between the SerpAPI group's titles and the chain's own cinema list (exact match, `Contains`, or a stripped-prefix heuristic depending on how far the naming diverges).

  `Movies/Abstract/CinemasGeo.cs` and `Movies/Abstract/MoviesDataByCinema.cs` are unused scaffold types (not referenced anywhere yet), presumably groundwork for grouping results by city/country in the bot flow.

- **`MdPrinter.cs`** — Renders `IEnumerable<Movie>` into a Markdown file: groups by movie title, then lists per-cinema/city/format rows in a table under each title's section (with poster and date range when available).

- **`TelegramMessagePrinter.cs`** — Formats `IEnumerable<Movie>` into a single Telegram MarkdownV2 message, filtered to today's showtimes only, grouped by title then by cinema/format with times listed; only the first movie block is left expanded, the rest are rendered as Telegram's spoiler/blockquote (`**>`/`||`) collapsed blocks. Not yet called from anywhere.

### Current state of the Telegram bot

The bot host follows the standard TelegramBots.NET long-polling hosted-service pattern:

- **`Abstract/IReceiverService.cs`, `Abstract/PollingServiceBase.cs`, `Abstract/ReceiverServiceBase.cs`** — generic base classes: `PollingServiceBase<TReceiverService>` is a `BackgroundService` that loops, creating a fresh DI scope each iteration and calling `TReceiverService.ReceiveAsync`; `ReceiverServiceBase<TUpdateHandler>` calls `botClient.ReceiveAsync(updateHandler, ...)`.
- **`Services/PollingService.cs`, `Services/ReceiverService.cs`** — concrete compositions of the above base classes over `ReceiverService`/`UpdateHandler`, registered in `Program.cs`.
- **`Services/UpdateHandler.cs`** — still the stock Telegram.Bot example handler (commands like `/photo`, `/poll`, `/inline_mode`, `/keyboard`, `/request`, etc.). It is **not yet wired to the cinema pipeline** — it doesn't call `ShowtimeAggregator`, `MdPrinter`, or `TelegramMessagePrinter`. The intended flow (per `//todo` comments left in `Program.cs`): prompt the user to share their Telegram location, feed the coordinates into the existing SerpAPI → `ShowtimeAggregator` pipeline, and reply with a `TelegramMessagePrinter`-formatted message. Building that handler is the next real piece of work on this project, not something already functioning.

## Adding a New Cinema Chain

1. Create a new `IConvertor` implementation under `Movies/Countries/<country-code>/`.
2. Set `Key` to a substring that matches the cinema's `website` field as returned by SerpAPI/Google Maps.
3. Register an instance via `ScrapperStrategy.Register(new YourConvertor())` (currently done inline in `Program.cs`).
