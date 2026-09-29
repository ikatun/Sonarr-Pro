# Complete-series search

The interactive series-toolbar button lists torrents advertising all known regular
seasons. It keeps normal release validation and manual selection.

For automatic selection, submit a one-off command to `POST /api/v3/command`:

```json
{"name":"CompleteSeriesSearch","seriesId":42,"dryRun":true}
```

Use the real library series ID. Inspect the command status and application log;
each candidate includes its approval or rejection reasons. Set `dryRun` to false
to run normal download ranking and submission. A dry run never submits downloads
or pending releases. The automatic command requires the series to be monitored,
uses automatic-search-enabled torrent indexers, and enforces episode monitoring.
It preserves profile, upgrade, size, identity, queue, delay and download-client
rules. It does not fall back to season/episode searches, change Search Monitored,
or schedule recurring searches. No show names or codec preferences are hardcoded.

Complete-series coverage is inferred from advertised release names, not verified
torrent file lists. Explicit incomplete season coverage is rejected. Specials are
optional. Ongoing shows use all regular seasons currently known to the library.

For a partially owned series, configure the pack-upgrade policy appropriately.
Codec preferences belong in custom formats and a quality profile; replacement of
existing files additionally needs upgrade permission and an unmet upgrade target.

Tests cover automatic versus interactive indexer selection, monitoring context,
manual versus scheduled triggers, dry runs, native decision processing, and no
season-search fallback. Existing parser and multi-season import tests remain relevant.
