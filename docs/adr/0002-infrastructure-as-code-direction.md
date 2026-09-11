# ADR 0002: Use Terraform for portfolio infrastructure as code

**Status:** Proposed

## Context

[ADR 0001](0001-aws-target-deployment-direction.md) proposes ECS/Fargate, ALB ingress, private discovery, and managed data-service candidates while deferring IaC selection. The [AWS target proposal](../aws-target-architecture.md) remains unimplemented. This ADR proposes a portfolio-specific direction for that deferred choice; it does not change deployment scope or imply production readiness.

## Decision

Propose **Terraform** for portfolio infrastructure as code because its declarative configuration and explicit plans support reviewable changes, and its workflow is broadly transferable across teams and providers. Transferable workflow does not mean AWS resource definitions are portable unchanged.

Keep Aspire for local development. Follow the [staged readiness guide](../aws-deployment-readiness.md) before any cloud experiment. Terraform installation, AWS access, planning against AWS, and applying infrastructure require separately authorized work. This is not a tooling choice for a production employer.

## Alternatives considered

- **AWS CDK:** A credible choice for expressing infrastructure in familiar programming languages and reusable constructs. For this portfolio, prefer direct declarative configuration and an explicit plan-review workflow; CDK is not rejected on capability grounds.
- **AWS CloudFormation:** A credible AWS-native declarative choice that avoids operating a separate Terraform state backend. Prefer Terraform here for transferable workflow experience, while accepting its additional state-management responsibility.

These alternatives can also support review and controlled deployment. The decision reflects learning and delivery objectives rather than universal superiority.

## Consequences

- Review configuration and plans before apply; explicitly inspect environment selection, IAM permissions, resource replacement/deletion, and cost drivers. A plan is not authorization to apply.
- Pin tool/provider versions, review dependency changes, and keep initial scope small rather than introducing broad modules prematurely.
- Design state encryption, restricted access, locking, backup, recovery, and environment separation. State and saved plans may contain secrets; sensitive-output redaction is not a substitute for protecting artifacts. See [Terraform sensitive-data guidance](https://developer.hashicorp.com/terraform/language/manage-sensitive-data).
- Keep credentials, secret values, state, and saved plans outside source control. Prefer temporary role-based access when cloud execution is later authorized.
- Define ownership, budget alerts, retention, and teardown verification. An apply or destroy result alone does not establish application correctness or complete cost cleanup.

## Explicitly deferred decisions

Tool installation and versions; provider/module structure; state backend and bootstrap process; real AWS provisioning; pgvector validation; Redis OSS versus Valkey; identity strategy; HA, scaling, and cost sizing; CI/CD and approval automation.

No account configuration, resource creation, Free Tier eligibility, or AWS deployment success is asserted.
