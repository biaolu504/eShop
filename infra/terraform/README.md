# Local no-resource Terraform foundation

This folder does not define or create AWS infrastructure. It contains a Terraform version constraint and harmless static portfolio outputs only. There are no providers, resources, data sources, backend declarations, modules, or variable files. No AWS credentials are required.

`~> 1.16.0` permits Terraform 1.16 patch releases, starting at 1.16.0 and below 1.17.0. Terraform installation and execution were not performed to create this foundation.

## Commands and safety boundaries

Run the following from `infra/terraform` using an already installed compatible Terraform version. **Exactly these commands are safe to run today for the configuration as written:**

```powershell
terraform init
terraform fmt -check
terraform validate
terraform plan
```

| Command | Plain-language purpose | Safe to run today? |
| --- | --- | --- |
| `terraform init` | Prepares the working directory. This configuration has no providers or modules to download and no remote backend to initialize. | Yes. |
| `terraform fmt -check` | Checks formatting without rewriting the configuration; exits unsuccessfully if formatting differs. | Yes. |
| `terraform validate` | Checks configuration syntax and internal consistency after initialization. It does not prove deployment readiness. | Yes. |
| `terraform plan` | Previews proposed changes. Here it can show static output additions, but no infrastructure actions. | Yes, only for this no-resource configuration. |
| `terraform fmt` | Rewrites configuration into Terraform's standard format. | Not included in today's safe command list; review and authorize file edits first. |
| `terraform apply` | Executes an approved plan and records results in state. Even this configuration can write output values into local state. | No; never run without explicit review and authorization. |

These are instructions, not a record of commands already executed. If a command asks for AWS credentials or proposes infrastructure changes, stop: that does not match this foundation.

A future AWS provider or data source can contact AWS during `plan`, including when reading existing infrastructure. Planning is not universally offline or credential-free. Reassess access, configuration, state handling, and authorization before executing commands after any such additions. Review the intended environment and proposed changes before authorizing `apply`; do not use unattended approval as a substitute for review.

## Local artifacts and source control

The folder's `.gitignore` excludes `.terraform/`, Terraform state and backup files, crash logs, conventional plan files (`*.tfplan`, `*.plan`, and `tfplan`), and real `.tfvars`/`.tfvars.json` files, including automatically loaded variable files. State, plans, and variable files may contain sensitive information in future configurations. If later saving a plan under another filename, protect and exclude it explicitly before creating it.

`.terraform.lock.hcl` is deliberately not ignored. Commit provider lock files when providers are later introduced so dependency selections and checksums can be reviewed and reproduced. No provider lock file or AWS provider is introduced here.

This foundation neither publishes the DeploymentProbe image nor provisions ECR, ECS, networking, or any other cloud resource. Static output values are descriptive metadata, not deployment evidence.
