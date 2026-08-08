# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**cinemarcer** is a .NET 10.0 console application that aggregates movie showtimes from local cinemas. It uses the SerpAPI Google Maps engine to discover nearby cinemas by geo-coordinates, then scrapes showtime data using cinema-chain-specific parsers.

## Commands

```bash
# Build
dotnet build

# Run
dotnet run --project cinemarcer-console/cinemarcer-console.csproj

# Set SerpAPI key for local development (user secrets)
dotnet user-secrets set "SerpApiKey" "<your-key>" --project cinemarcer-console/cinemarcer-console.csproj
```

The `SerpApiKey` must be provided via user secrets (dev) or `appsettings.json`. No tests exist yet.

## Architecture

The flow is: **SerpAPI → ShowtimeAggregator → ScrapperStrategy → IConvertor → IMovie[]**

- **`Program.cs`** — Entry point. Hardcoded location coordinates and cinema name for testing. Queries Google Maps for cinemas of type `movie_theater`, then calls `ShowtimeAggregator.Aggregate()`.

- **`parsers/ShowtimeAggregator.cs`** — Groups raw SerpAPI JToken results by cinema name, then calls `SeansesScrapper.Scrap()` for each group.

- **`parsers/SeansesScrapper.cs` + `ScrapperStrategy.cs`** — Strategy pattern. `ScrapperStrategy` is a registry of `IConvertor` implementations keyed by cinema name. `GetProvider()` returns the correct convertor for a given cinema chain.

- **`parsers/abstract/IConvertor.cs`** — Interface for cinema-specific parsers. Receives a `IGrouping<string?, JToken>` and returns `IMovie[]`. Add new cinema chains by implementing this interface and registering in `ScrapperStrategy`.

- **`parsers/abstract/IMovie.cs`** — Common movie data model: `Title`, `PosterUrl`, `Description`, `StartDate`, `EndDate`, `CinemaName`, `City`, `Country`.

- **`parsers/countries/vn/`** — Vietnam-specific convertors (`GalaxyCinemaConvertor`, `GCVConvertor`). Currently stubs (`NotImplementedException`).

## Adding a New Cinema Chain

1. Create a new `IConvertor` implementation under `parsers/countries/<country-code>/`
2. Set `KeyName` to match the cinema's name as returned by Google Maps
3. Register it in `ScrapperStrategy.Convertors` (or update `GetProvider()`)
