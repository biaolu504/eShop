# DeploymentProbe private ECR configuration

This configuration declares one private `eshop-deployment-probe` ECR repository and its lifecycle policy. It defines no ECS, IAM, VPC, CloudWatch, or application resources and does not build or push images. It is no longer a no-resource configuration. On Day 11, Terraform plan and the reviewed saved-plan apply were completed for the single repository and lifecycle policy. A post-apply plan showed no changes. See the [Day 11 evidence record](../../docs/ecr-deployment-probe-validation.md).

## Configuration

Terraform requires `~> 1.16.0`; the AWS provider constraint is `~> 6.0`. `terraform init -backend=false` resolved and locked `hashicorp/aws` v6.64.0 in `.terraform.lock.hcl`. `aws_region` defaults to `us-east-1`; confirm account and region before authenticated work.

Tags are `project=eshop`, `environment=learning`, and `component=deployment-probe`. `owner` and `expiry` are required inputs with no defaults. Supply an accountable owner containing non-whitespace text and a valid YYYY-MM-DD calendar date for the agreed review/teardown. The expiry tag does not automatically delete resources.

Image tags are immutable, scan on push is enabled, and repository encryption is explicitly configured as `AES256`. Review registry scanning settings, actual coverage, and findings before image use; no clean scan or enhanced scanning is asserted. `force_delete=false` prevents forced deletion of a nonempty repository, not lifecycle deletion of images or deletion of an empty repository.

The lifecycle policy expires untagged images older than seven days **since push**, not since losing a tag. A separate rule matches all tagged images and expires older images beyond the newest five. The limit counts images, not tags, and lifecycle enforcement is asynchronous rather than an instantaneous storage cap. Review effects on any digest needed for a future task or rollback. Immutable tags do not prevent lifecycle deletion. See [AWS lifecycle policy parameters](https://docs.aws.amazon.com/AmazonECR/latest/userguide/lifecycle_policy_parameters.html).

Outputs expose repository URL, ARN, and registry ID once available. The local image-name output remains descriptive metadata, not publication evidence. Keep account-specific outputs out of public documentation.

## Commands and authorization boundaries

Commands below run from `infra/terraform`. Initialization, planning, the reviewed saved-plan apply, and a post-apply plan were completed for Day 11. Future plans and applies remain separately authorized operations; this README does not authorize another execution.

```powershell
terraform fmt -check
terraform init -backend=false
terraform validate
```

`fmt -check` checks formatting offline without edits. `fmt` rewrites configuration and requires subsequent diff review. `init -backend=false` prepares dependencies and may download the provider from the Terraform registry: it is **not offline**, but does not provision AWS resources. Once dependencies are installed, `validate` checks configuration locally without AWS authentication; it does not prove deployability.

The following authenticated operations were used for Day 11; any future execution requires separate authorization and is not instructed here:

Before planning, supply both `owner` and `expiry` through approved input handling, such as `TF_VAR_owner` and `TF_VAR_expiry` environment variables or an ignored local variable file. No placeholder defaults are available. An apply without a saved plan also requires these inputs; the saved-plan apply below uses the values captured during planning, so review them in that plan rather than attempting to override them at apply time.

```powershell
terraform plan -out .\deployment-probe.tfplan
terraform apply .\deployment-probe.tfplan
```

`plan` can authenticate/contact AWS and refresh state. Before running it, confirm approved temporary role-based access, target account/region, real owner/expiry values, budget/cost authorization, and teardown responsibility. Review all proposed changes, replacements, deletions, and permissions. A first plan should contain only the repository and lifecycle policy as managed infrastructure. Investigate any unexpected scope.

No credentials belong in Terraform files, variable files, command examples, or source control. Use an approved external authentication flow; this provider contains no credential values.

`apply` executes the saved plan and is never to be run without explicit review and authorization. Re-plan after relevant changes; protect saved plans and do not bypass review with unattended approval.

## State, lock files, and cleanup

No remote backend is declared, so state is local. Local state is not a shared, centrally secured source of truth; it can be lost and may contain sensitive values. Protect state and backups, avoid concurrent operations, and decide encrypted remote storage, access controls, locking, and recovery before collaborative use. Protect saved plans as well.

The existing `.gitignore` excludes `.terraform/`, state/backups, crash logs, conventional plan filenames, and real `.tfvars` files. Keep saved plans within those filename conventions. `.terraform.lock.hcl` is deliberately not ignored and now records `hashicorp/aws` v6.64.0 and its checksums. Review the lock file for inclusion in source control to preserve that selection; future dependency changes require deliberate review.

Teardown requires separate review and authorization. Inspect retained images, approve their removal if needed, then remove the empty repository and confirm residual artifacts and costs. Budget alerts are notifications, not automatic spending stops. The Day 11 evidence establishes ECR repository and lifecycle-policy provisioning only; no Free Tier eligibility, image publication, or application deployment success is claimed.
