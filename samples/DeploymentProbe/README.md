# DeploymentProbe: local container boundary

Standalone .NET 10 ASP.NET Core probe with no eShop service references, database, cache, broker, identity dependency, secrets, or AWS SDK. This validates only a local container boundary, not AWS or eShop behavior. No cloud deployment is included.

`GET /healthz` returns HTTP 200 with `{"status":"healthy"}`. It indicates that this process can respond, not downstream readiness. `GET /version` returns `service`, `environment`, and the assembly informational `version` (which may include a source revision). JSON console logs record startup and graceful shutdown with `ServiceName`, `Environment`, and `Version`. Forced termination cannot guarantee a shutdown log.

## Local commands

Run these PowerShell commands from the repository root. Tests require .NET 10; container commands require Docker Desktop in Linux-container mode. Use an available local port if 8080 is occupied.

```powershell
dotnet test --project tests/DeploymentProbe.Tests/DeploymentProbe.Tests.csproj
docker build -f samples/DeploymentProbe/Dockerfile -t eshop-deployment-probe:local .
docker run --detach --name eshop-deployment-probe --publish 127.0.0.1:8080:8080 --env ASPNETCORE_ENVIRONMENT=Production eshop-deployment-probe:local
# Wait for "Now listening" in the logs, or retry /healthz until it succeeds.
docker logs eshop-deployment-probe
curl.exe --fail http://localhost:8080/healthz
curl.exe --fail http://localhost:8080/version
docker logs eshop-deployment-probe
docker stop --time 10 eshop-deployment-probe
docker logs eshop-deployment-probe
docker rm eshop-deployment-probe
docker image rm eshop-deployment-probe:local
```

`docker run -d` (equivalently, `--detach`) confirms that the container process started; it does not guarantee the HTTP endpoint is ready. Wait for the "Now listening" log or retry `/healthz` until it succeeds before treating the container as ready and checking `/version`. In local validation, an immediate `/healthz` request closed unexpectedly while the app was still starting; after logs confirmed it was listening, retrying `/healthz` and `/version` succeeded. Inspect logs if failures persist rather than assuming every failure is a startup delay. This is local Docker guidance, not an ECS or ALB health-check configuration.

Stop the container promptly after validation. The loopback-only published port is for local inspection, not a public endpoint. The probe does not self-terminate; the operator bounds this local experiment using stop/remove. A future ECS experiment needs its own lifetime and teardown controls.

## Build and test boundaries

The multi-stage Dockerfile copies only the standalone project and source, publishes in a .NET SDK image, and runs under the ASP.NET image's non-root `APP_UID` on port 8080. It intentionally does not import eShop's build props or dependencies. Both base images are digest-pinned for reproducibility, retaining the `10.0` tags for readability. Security maintenance requires deliberate digest updates and renewed validation.

The Dockerfile-specific `samples/DeploymentProbe/Dockerfile.dockerignore` filters the repository-root build context for the documented command, excluding Git metadata, IDE state, and build outputs while retaining the required source files. Explicit `COPY` instructions also keep unrelated files out of image layers. Review the context before using a remote builder; no secrets are required. Root ignore files are not changed by this sample.

The .NET endpoint tests are automated through MSTest and use an in-memory ASP.NET Core TestServer with the actual application routes. Docker build/run/log/shutdown validation is intentionally performed manually against the real local container boundary. Passing the .NET tests does not validate image construction, non-root runtime behavior, port binding, or Docker shutdown. See the [Day 8 validation record](../../docs/deployment-probe-validation.md) for recorded local evidence; no AWS validation is claimed.
