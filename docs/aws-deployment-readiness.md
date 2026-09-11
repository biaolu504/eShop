# AWS deployment readiness

## Purpose and boundary

Define staged evidence before attempting the proposed [AWS target architecture](aws-target-architecture.md) and [ADR 0001](adr/0001-aws-target-deployment-direction.md). ECS/Fargate, ALB, private services, and managed data services remain proposed; Aspire remains local-development orchestration only.

**This document authorizes no provisioning or account access.** Every stage below is future work requiring its own agreed scope. Checklists are requirements, not claims about an existing account. The [proposed Terraform direction](adr/0002-infrastructure-as-code-direction.md) addresses the earlier IaC deferral without making the target deployable.

## Safe staged approach

1. **Account safety/readiness:** Before cloud work, verify the checklist below through a separately authorized review. Record completion without storing account identifiers or credentials in repository evidence.
2. **Reproducible local container build validation:** Later, add and validate the standalone DeploymentProbe described below. Record source revision, SDK/base-image versions, target architecture, build inputs, and image digest. Validate configuration, startup, intentional health behavior, logs, and shutdown locally. Exclude secrets from build context and layers. No image build or local container run is performed by this document.
3. **Later ECR image publication:** After local validation and account readiness, review the bounded registry scope, access permissions, image scanning, retention, and cleanup. Publish an identified image only under separate authorization; publication is not runtime validation.
4. **Later first ECS runtime experiment:** Run DeploymentProbe as a private, time-bounded task with a documented objective, duration, resource inventory, and teardown. Validate image pull, startup, health, permitted connectivity, and logs before increasing scope. Give it no public endpoint. A successful probe experiment would not validate eShop.
5. **Only later, an eShop vertical slice:** Select one bounded user journey and enumerate its real dependencies. Resolve relevant identity, discovery, database migration, and data compatibility decisions first. Record slice-specific acceptance criteria and cleanup; do not silently include the entire system.

Deploying all of eShop first would combine container packaging, HTTP/gRPC discovery, Identity, four logical databases, cache, RabbitMQ, processors, and callback egress before any cloud runtime evidence exists. Smaller stages isolate failures and limit the resources requiring cost review and teardown.

## Selected first runtime workload

The proposed first ECS runtime experiment is a future, standalone ASP.NET Core **DeploymentProbe**, to be added later in this repository. It will have no database, cache, broker, identity, secrets, or public endpoint. It will run as a private, time-bounded task and emit structured startup/shutdown evidence to logs.

Its purpose is to validate the container image, ECR publication path, ECS task lifecycle, IAM/network boundary, observability, and teardown—not eShop behavior or an application vertical slice. The probe is proposed, not implemented or deployed.

## Preconditions for any ECS runtime experiment

- Intentional production health/readiness semantics and access rules, verified for the selected workload before configuring ALB/ECS health behavior. eShop's Development-only `/health` and `/alive` endpoints are insufficient evidence.
- An image scan with findings reviewed and disposition recorded; scanning alone is not proof of safety.
- Externalized configuration and secrets, with ownership and least-privilege runtime access. No secrets in images, source, or published evidence.
- Reviewed VPC, security groups, private discovery, ingress/egress, TLS, IAM task roles, and separate task-execution permissions. Preserve private Identity access and address SSRF controls before enabling external webhook callbacks.
- A reviewed teardown plan, retention exceptions, responsible owner, and post-experiment cost check.

## Account safety checklist

- [ ] Require MFA, including protection of root access; do not use root for daily work.
- [ ] Prefer temporary, role-based access with least privilege. Follow [AWS IAM guidance](https://docs.aws.amazon.com/IAM/latest/UserGuide/best-practices.html).
- [ ] Establish a budget with actual and forecast alerts and confirmed recipients. Choose thresholds separately; alerts are not instant spending stops. See [AWS Budgets](https://docs.aws.amazon.com/cost-management/latest/userguide/budgets-managing-costs.html).
- [ ] Choose a region deliberately and review service availability and data-location requirements.
- [ ] Define project/environment tags, owner, and expiry; an expiry tag alone does not delete resources.
- [ ] Review expected cost drivers before each experiment and actual costs afterward.
- [ ] Confirm teardown against the inventory, including retained images, logs, snapshots, and network resources; revisit billing after reporting catches up.

## Proposed Terraform workflow

Use declarative, reviewable Terraform configuration with pinned tool/provider versions and reviewed dependency changes. Design encrypted, access-controlled state storage, locking, backup, and recovery before real infrastructure. State and saved plans can contain secrets; marking a value sensitive does not remove it from state. Keep these artifacts outside source control and restrict their distribution. See [Terraform sensitive-data guidance](https://developer.hashicorp.com/terraform/language/manage-sensitive-data).

Review configuration, then review a plan for the intended environment, replacements, deletions, permissions, and cost implications before separately approving apply. Planning may access AWS and is not authorized here. Re-plan after relevant changes; review destroy operations and retained resources explicitly. Avoid unattended approval as the initial workflow.

## Risks and not yet done

Risks include accidental exposure, leaked state, recurring charges, incomplete cleanup, and mistaking local success for cloud compatibility. pgvector, cache engine, identity, HA/sizing, and CI/CD remain unresolved. No software installation, credential use, account access, container build/push, resource provisioning, or deployment validation occurred. No Free Tier eligibility or deployment success is claimed.
