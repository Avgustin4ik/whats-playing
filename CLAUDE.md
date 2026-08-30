# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**cinemarcer** is a .NET 10.0 console application that aggregates movie showtimes from local cinemas. It uses the SerpAPI Google Maps engine to discover nearby cinemas by geo-coordinates, then scrapes showtime data from each cinema chain's own API/site using chain-specific parsers, and renders the result as Markdown.

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

The flow is: **SerpAPI (Google Maps) → group by cinema website → ScrapperStrategy → IConvertor → Movie[] → MdPrinter**

- **`Program.cs`** — Entry point. Has hardcoded location coordinates and a cinema name filter (`"Galaxy"`) for testing. Queries Google Maps (`engine=google_maps`, `type=search`) for results of type `Movie theater` near the coordinates, filters to one cinema chain for now, then calls `ShowtimeAggregator.Aggregate()` and writes the result to Markdown via `MdPrinter.Print()`.

- **`parsers/ShowtimeAggregator.cs`** — Groups the raw SerpAPI `JToken` results by the cinema's `website` field (not by name), then calls `SeansesScrapper.Scrap()` for each group.

- **`parsers/SeansesScrapper.cs` + `ScrapperStrategy.cs`** — Strategy pattern. `ScrapperStrategy.Convertors` is a `Dictionary<string, IConvertor>` registry keyed by each convertor's `Key` (a website URL substring, e.g. `"https://www.galaxycine.vn/"`). `GetProvider(cinemaWebsite)` finds the convertor whose key is contained in the SerpAPI `website` value. Convertors must be registered at startup (see `ScrapperStrategy.Register(...)` in `Program.cs`).

- **`parsers/abstract/IConvertor.cs`** — Interface for cinema-specific parsers: `Movie[] Convert(IGrouping<string?, JToken> json)` plus a `Key` (website) property used for strategy lookup.

- **`parsers/abstract/Movie.cs`** — Common movie data model (`struct Movie`): `Title`, `PosterUrl`, `Description`, `StartDate`, `EndDate`, `CinemaName`, `City`, `Country`. Has a constructor that parses a chain-specific session `JToken` directly (`movie`/`cinema` sub-objects), which convertors can use as a shortcut instead of hand-mapping fields.

- **`parsers/countries/<country-code>/`** — Country/chain-specific convertors. `GalaxyCinemaConvertor` (Vietnam) is the reference implementation: it bootstraps a session cookie from the cinema's homepage, calls its mobile sessions API, caches the raw sessions in a static field, then filters sessions to the cinema names present in the SerpAPI group and maps them via the `Movie(JToken)` constructor.

- **`MdPrinter.cs`** — Intended to render `IEnumerable<Movie>` into a Markdown file using a template (referenced as "Claude Design" template in comments). Currently a stub with no implementation.

## Adding a New Cinema Chain

1. Create a new `IConvertor` implementation under `parsers/countries/<country-code>/`.
2. Set `Key` to a substring that matches the cinema's `website` field as returned by SerpAPI/Google Maps.
3. Register an instance via `ScrapperStrategy.Register(new YourConvertor())` (currently done inline in `Program.cs`).
