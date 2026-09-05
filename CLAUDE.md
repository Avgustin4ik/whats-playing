# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**cinemarcer** is a .NET 10.0 console application that aggregates movie showtimes from local cinemas. It uses the SerpAPI Google Maps engine to discover nearby cinemas by geo-coordinates, then scrapes showtime data from each cinema chain's own API/site using chain-specific parsers, and renders the result as Markdown or a Telegram MarkdownV2 message.

## Commands

```bash
# Build
dotnet build

# Run
dotnet run --project cinemarcer-console/cinemarcer-console.csproj

# Set SerpAPI key for local development (user secrets)
dotnet user-secrets set "SerpApiKey" "<your-key>" --project cinemarcer-console/cinemarcer-console.csproj
```

The `SerpApiKey` must be provided via user secrets (dev) or `appsettings.json` (`SerpApiKey` key — the checked-in value is a placeholder). No tests exist yet.

## Architecture

The flow is: **SerpAPI (Google Maps) → group by cinema website → ScrapperStrategy → IConvertor → Movie[] → MdPrinter / TelegramMessagePrinter**

- **`Program.cs`** — Entry point. Has hardcoded location coordinates for testing. Queries Google Maps (`engine=google_maps`, `type=search`) for results of type `Movie theater` near the coordinates, then calls `ShowtimeAggregator.Aggregate()` and writes the result to Markdown via `MdPrinter.Print()`. Registers every `IConvertor` implementation with `ScrapperStrategy` before running.

- **`parsers/ShowtimeAggregator.cs`** — Groups the raw SerpAPI `JToken` results by the cinema's `website` field (not by name), then calls `SeansesScrapper.Scrap()` for each group.

- **`parsers/SeansesScrapper.cs` + `ScrapperStrategy.cs`** — Strategy pattern. `ScrapperStrategy.Convertors` is a `Dictionary<string, IConvertor>` registry keyed by each convertor's `Key` (a website URL substring, e.g. `"https://www.galaxycine.vn/"`). `GetProvider(cinemaWebsite)` finds the convertor whose key is contained in the SerpAPI `website` value. Convertors must be registered at startup (see `ScrapperStrategy.Register(...)` in `Program.cs`).

- **`parsers/abstract/IConvertor.cs`** — Interface for cinema-specific parsers: `Movie[] Convert(IGrouping<string?, JToken> json)` plus a `Key` (website) property used for strategy lookup.

- **`parsers/abstract/Movie.cs`** — Common movie data model (`struct Movie`): `Title`, `PosterUrl`, `Description`, `StartDate`, `EndDate`, `CinemaName`, `City`, `Country`, `ShowDateTime`, `Duration`, `Rate`. Has a constructor that parses a chain-specific session `JToken` directly (`movie`/`cinema` sub-objects), which convertors can use as a shortcut instead of hand-mapping fields.

- **`parsers/countries/<country-code>/`** — Country/chain-specific convertors, one HTTP client + one static cache per chain (fetched once, reused across `Convert` calls). All four current convertors are Vietnam (`vn`), each reverse-engineered from that chain's own site/app traffic since none publish official APIs:
  - `GalaxyCinemaConvertor` — the reference implementation. Bootstraps a session cookie from the homepage, calls its mobile sessions API for all cinemas/dates at once, caches the raw sessions, then filters to the cinema names present in the SerpAPI group and maps them via the `Movie(JToken)` constructor.
  - `CgvCinemaConvertor` — calls CGV's own mobile JSON API (`/en/api/cinema/list`, `/en/cinemas/catalog_mobile/siteschedules/id/{id}/date/{ddMMyyyy}`) with the CGV mobile app's `User-Agent`; date must be `ddMMyyyy` with no separators. Its edge only negotiates the legacy `TLS_RSA_WITH_AES_128_CBC_SHA` cipher suite, which .NET's default Linux TLS policy excludes, so its `HttpClient` sets an explicit `CipherSuitesPolicy` to opt back in.
  - `LotteCinemaConvertor` — calls Lotte's ASP.NET AJAX web service at `/LCWS/Ticketing/TicketingData.aspx` (POST, body `paramList=<json>`, method name in the JSON payload). `GetTicketingPage` returns the site-wide cinema and movie catalogs (cached); `GetPlaySequence` with an empty `representationMovieCode` returns every session for one cinema/date (passing multiple movie codes at once breaks the query — only "all" or one at a time work).
  - `BetaCinemaConvertor` — Beta has no JSON API; `Home/GetByCinemaID` on `lichchieu.betacorp.vn` returns a JSON-encoded HTML fragment for one cinema (today's showtimes only), parsed with regex since it's the only chain without structured data.
  
  Since each chain names/formats cinemas differently from SerpAPI's Google Maps result, every convertor other than Galaxy does its own fuzzy match between the SerpAPI group's titles and the chain's own cinema list (exact match, `Contains`, or a stripped-prefix heuristic depending on how far the naming diverges).

- **`MdPrinter.cs`** — Renders `IEnumerable<Movie>` into a Markdown file: groups by movie title, then lists per-cinema/city/format rows in a table under each title's section (with poster and date range when available).

- **`TelegramMessagePrinter.cs`** — Formats `IEnumerable<Movie>` into a single Telegram MarkdownV2 message, filtered to today's showtimes only, grouped by title then by cinema/format with times listed; only the first movie block is left expanded, the rest are rendered as Telegram's spoiler/blockquote (`**>`/`||`) collapsed blocks. Not yet wired into `Program.cs`.

## Adding a New Cinema Chain

1. Create a new `IConvertor` implementation under `parsers/countries/<country-code>/`.
2. Set `Key` to a substring that matches the cinema's `website` field as returned by SerpAPI/Google Maps.
3. Register an instance via `ScrapperStrategy.Register(new YourConvertor())` (currently done inline in `Program.cs`).
