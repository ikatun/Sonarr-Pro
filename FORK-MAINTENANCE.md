# Maintaining our Sonarr Pro fork

Source: https://github.com/ikatun/Sonarr-Pro (main).
Upstream: https://github.com/KakarottoCake/Sonarr-Pro.

GitHub Actions uses hosted Ubuntu runners; no home-server runner or server
credentials are required. CI builds the solution, runs backend tests and frontend
queue regressions, and checks frontend lint and the production bundle. Pull
requests also build the Linux amd64 container without publishing.

Pushes to main build the container, start it with disposable configuration,
verify HTTP 200 from /ping, and publish ghcr.io/ikatun/sonarr-pro:edge plus an
immutable sha-<full-commit> tag. Version tags v* additionally publish release tags
and latest, with amd64 and arm64 images. Publication uses GITHUB_TOKEN.

Future deployments should pin an image digest or immutable commit tag after
successful CI and Docker runs. Deployment is manual; these workflows do not
connect to or modify the home server. Preserve runtime configuration and take a
consistent database backup before any future deployment.

The existing native installation is independent of these builds. Changing the
Git remote or pushing source does not update its running binaries.
