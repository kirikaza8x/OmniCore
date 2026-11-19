# Integration: owl_editor (Python service)

`owl_editor` (the Otherworld Legends save and hotfix toolkit, repo `kirikaza8x/owl_editor`) ships an optional HTTP
service, `owl_api/` (FastAPI, file-in / file-out asynchronous jobs: hotfix decrypt/encrypt, PlayerPrefs
decrypt/encode, save edits such as Battle Pass, skins, resources and character unlock; see that repo's `docs/api.md`).
It deliberately uses a **flat Python service layer** (routers -> `JobService` -> storage / toolkit adapter) instead of
this repo's five C# layers. What has to match is the **contract**:

- **OpenAPI** per service (FastAPI generates it), so the gateway can aggregate specs like it does for Auth.
- **JWT**: today one shared `X-API-Key`; target is OmniCore JWTs verified through a JWKS (roadmap phase 4).
- **Events**: `JobFinished` through an `on_finished` hook to RabbitMQ once the contracts exist (phase 5).
- **Container + gateway**: `owl_editor` now builds an image (`Dockerfile`, pushed to `ghcr.io/kirikaza8x/owl_editor`) and
  has a compose file; here it needs a compose entry under `infra/docker` and a `/api/v1/owl/` route in nginx (phase 3).
- **Storage**: results move from local disk to MinIO (the bucket already exists); jobs from memory to a database. The
  plan is in `owl_editor/docs/storage-plan.md`.

Caveats: the toolkit has never been run against the real game, and hosting other people's saves or APKs has privacy and
terms-of-service implications. Decide that before exposing it.
