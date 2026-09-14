# DeploymentProbe validation: Day 8

This record captures the supplied local validation evidence. Docker and tests were not rerun to write this document or validate the accompanying base-image digest update.

| Check | Recorded outcome |
| --- | --- |
| `dotnet test --project tests/DeploymentProbe.Tests/DeploymentProbe.Tests.csproj` | 3 tests passed. |
| Local Docker build | Completed for `eshop-deployment-probe:local`. |
| Container health response | Returned `healthy` status. |
| Container version response | Returned service `DeploymentProbe`, environment `Production`, and version `1.0.0`. |
| JSON logs | Showed structured startup and graceful shutdown. |
| Cleanup | The container was removed; the local image remains. |

## Observed startup readiness

`docker run -d` confirms that the container process started; it does not guarantee HTTP endpoint readiness. During local validation, an immediate `/healthz` request closed unexpectedly while the app was still starting. After logs confirmed the app was listening, retrying `/healthz` and `/version` succeeded. Wait for the "Now listening" log or a successful `/healthz` retry before treating the local container as ready. This observation does not establish or configure ECS or ALB health checks.

No AWS account access, credentials, image push, ECR activity, or cloud deployment occurred.

This validates the local container boundary only, not AWS or eShop behavior. Local reproduction commands are in the [DeploymentProbe README](../samples/DeploymentProbe/README.md).
