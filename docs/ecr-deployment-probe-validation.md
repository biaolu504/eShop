# DeploymentProbe ECR validation: Day 11

This record captures the supplied completed provisioning and live-validation evidence. No Terraform, AWS CLI, Docker, or Git commands were run to prepare this document.

## Provisioning outcome

Terraform applied one private ECR repository named `eshop-deployment-probe` and one lifecycle policy in `us-east-1`. The reviewed apply reported **2 added, 0 changed, 0 destroyed**.

## Verified live configuration

| Setting | Verified value |
| --- | --- |
| Image tags | Immutable |
| Encryption | AES256 |
| Scan on push | Enabled |
| Untagged-image lifecycle | Expire images seven days after push |
| Tagged-image lifecycle | Retain the five newest tagged images |

The lifecycle evidence confirms configured rules, not observed image expiration.

| Tag | Verified value |
| --- | --- |
| `project` | `eshop` |
| `environment` | `learning` |
| `component` | `deployment-probe` |
| `owner` | `bill-lu` |
| `expiry` | `2026-10-15` |

A post-apply Terraform plan reported **no changes**, confirming agreement between the configuration and managed infrastructure at that verification point.

## Not yet done and limitations

- No image has been pushed.
- No image scan findings are available; enabling scan on push is not evidence of a completed scan or a vulnerability-free image.
- No ECS task has been run.
- No public endpoint has been created.
- No claim of cost-free operation is made.

This evidence validates repository provisioning and configuration only. It does not validate image publication, container execution on AWS, or eShop behavior.
