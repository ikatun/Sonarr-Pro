# Development notes (ikatun-development-machine)

Updated 2026-10-20. Handoff so development can continue on this machine.
General agent guidance stays in [AGENTS.md](AGENTS.md), fork design in
[FORK.md](FORK.md), CI/deploy in [FORK-MAINTENANCE.md](FORK-MAINTENANCE.md).

## Where things are

- Checkout: `~/dev/sonarr-pro` on `ikatun-development-machine` (192.168.1.185),
  cloned 2026-10-20 from `git@github.com:ikatun/Sonarr-Pro.git`.
  Upstream: `https://github.com/KakarottoCake/Sonarr-Pro.git` (not added as a remote yet).
- Previous development happened in `homeserver-experiments` (192.168.1.184):
  checkout `~/sonarr-pro` (identical, clean, fully pushed) and the work dir
  `~/sonarr-pro-work` (build/test logs, per-show investigations, rollback
  copies). Both are kept there as history; nothing was copied over.
- Toolchain: `~/.local/sonarr-pro-tools` — .NET SDK 10.0.302 (matches
  `global.json`), Node 20.20.2, Yarn 1.22.22 via corepack. Put on PATH by
  `~/.bashrc` (`DOTNET_ROOT` + PATH). No nvm on this machine.
- Production: Dockge stack `/opt/stacks/sonarr-pro` on the main LXC `server`
  (192.168.1.111), image `ghcr.io/ikatun/sonarr-pro:edge`, currently running
  revision `d2e06dc05`. UI/API on `http://192.168.1.111:8990` (all interfaces).
  API key lives in the private ops repo `ikatun/homeserver-plex`
  (`service-tokens.md`); don't copy it here.

## Build and deploy

```bash
dotnet build src/Sonarr.sln -c Debug -p:NuGetAudit=false
yarn install --frozen-lockfile && yarn build --env production
```

- Verified 2026-10-20 on this machine: backend 0 errors (with the flag),
  frontend webpack build succeeds. Tests not run here yet.
- **Open issue:** without `-p:NuGetAudit=false` the build fails with NU1902/NU1903
  (warnings-as-errors) on SixLabors.ImageSharp 3.1.12 and
  System.Security.Cryptography.Pkcs 10.0.0. New advisories since the last
  green build; the GitHub Docker workflow will likely fail the same way on the
  next push. Fix by bumping those packages.
- Deploy: push to `main` → GitHub "Docker" workflow (runs tests in the Docker
  build, ~10 min) publishes `:edge` and `:sha-<commit>`. Then on the main LXC:
  `cd /opt/stacks/sonarr-pro && docker compose pull && docker compose up -d`.
  Back up the config/DB first.

## Recent changes (newest first)

- `d2e06dc05` 10-05 Leave specials unmonitored for series added without a monitor choice
- `501315f2f` 10-05 Prefer full-series packs and fresh episode state in bulk missing searches
- `0464b0b6c` 10-05 Honour UMASK in the Docker entrypoint, defaulting to 002
- `23efa34d0` 10-04 Run tests inside the Docker build; separate CI workflow dropped
- `751715c9d` 10-04 Targeted pack queries in complete-series searches
- `3dd798386`–`8fd91541e` 09-30 Fork CI/Docker setup, SDK pin to 10.0.302, docs
- 09-29…09-30: complete-series search (interactive + scoped automatic with
  dry-run), multi-season > season > episode pack priority, anime season-pack
  and multi-season coverage, complete-series grab tracking recovery,
  scene/library numbering verification, guard against automatic feed upgrades
  of complete seasons, batched queue lookups.

All of the above are pushed, built by CI and deployed (prod runs `d2e06dc05`).

## Known gap found 2026-10-20 (not fixed)

Ergo Proxy (anime, series 768) missed TorrentLeech's
`[Beta] Ergo Proxy (BD 1080p HEVC x265 10-bit FLAC)`:

1. Season/episode searches only send `Title S01`, `Title S01E01`, `Title 01`
   (anime absolute). TorrentLeech matches literally, so packs titled with just
   the series name return 0 results.
2. "Search complete" did query `Ergo Proxy` and got the release, but the parser
   returns nothing for titles without season/episode info (`/api/v3/parse`
   gives no `parsedEpisodeInfo`), so the release is dropped silently — not
   even a rejection log line. Same for `Ergo Proxy BD 1080p Hi10p FLAC5 1 x264x [Dual-Audio]`.
   The 1337x `Ergo Proxy (Season 1) (BD 1080p)… Judas` parsed (Season 1, full
   season) and won.

Possible fix: during a complete-series search only, treat a release whose
title matches the series (aliases included) and has no season/episode info
as a full-series pack — ideally only for single-season series or when the
size is plausible for the whole series.

## 2026-10-20: single-season title-only packs (deployed b3a1036fd)
CompleteSeriesReleaseParser accepts "<title> <quality tokens>" only for series with exactly one regular season.
Series and missing searches now also run the complete-series search for single-season series.
