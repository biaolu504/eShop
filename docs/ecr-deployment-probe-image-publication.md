# Day 12: DeploymentProbe image publication

This record documents the verified Day 12 facts.

## Local build and validation

The locally rebuilt DeploymentProbe source revision was `fb22db6d7f66`. The local build passed, and all 3 DeploymentProbe endpoint tests passed.

Local container validation used the loopback-only port binding `127.0.0.1:8081`. `/healthz` returned healthy, and `/version` returned `DeploymentProbe`, `Production`, and version `1.0.0`. The temporary container was removed.

## Private ECR publication

One private ECR image was published under the immutable tag `sha-fb22db6d7f66`.

| Artifact | Digest |
| --- | --- |
| Tagged OCI index | `sha256:0db0595c7153954327fdb87dc27b0acecb9c475418860510301bb89055b3ede7` |
| Runnable image manifest | `sha256:d8eae6e5ac05fdd709910f69a7dedc71c32f60acd6b025a8e36513ff9bd94d95` |

## Scan results

ECR scanning completed on the runnable image manifest. It reported four Medium findings: `CVE-2026-15534` and `CVE-2026-19487` in `perl` and `perl-base`. ECR reported no fixed version for these findings.

The tagged OCI index itself had no scan record. The absence of an index scan record is not a clean scan; the completed scan and its findings apply to the runnable image manifest identified above.

## Publication limits

No ECS task, public endpoint, image promotion, deployment approval, or vulnerability-free claim exists for this Day 12 publication.
