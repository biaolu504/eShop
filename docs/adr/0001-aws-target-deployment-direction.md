# ADR 0001: Target AWS deployment direction

**Status:** Proposed

## Context

The [AWS target-architecture proposal](../aws-target-architecture.md) maps eShop's services, processors, and data dependencies to AWS candidates. Local development evidence does not establish AWS compatibility or deployment success.

## Decision

For the portfolio target, use containerized .NET services on **Amazon ECS/Fargate**, public ingress through an **Application Load Balancer**, and private service discovery. Managed candidates are **RDS PostgreSQL**, **ElastiCache**, and **Amazon MQ for RabbitMQ**, subject to compatibility validation.

Aspire remains local-development orchestration only, not the production runtime. AppHost configuration, discovery, and startup dependencies need deployment equivalents. Preserve HTTP/gRPC and processor boundaries; BFF packaging remains implementation work.

## Alternatives considered

- **ECS on EC2:** Host management is outside the initial delivery objective; reconsider for host control or measured economics.
- **Amazon EKS:** Kubernetes operation is not currently a portfolio objective; reconsider if that changes.
- **AWS App Runner:** Prefer one explicit model for public services, private services, and processors initially; reconsider for a separately scoped web service.

These are scope decisions, not claims that the alternatives are universally unsuitable.

## Consequences

- Package deployable components as container images and use ECR as the candidate registry.
- Design a two-AZ VPC baseline with ALB ingress, private application/data tiers, narrowly scoped security groups, controlled egress, and TLS.
- AppHost configures Identity endpoints for WebApp, Basket, Ordering, Webhooks, and WebhookClient. Require private Identity discovery and security-group access for these callers and the BFF; expose only required browser/OIDC routes through ALB.
- Distinguish internal demo WebhookClient callbacks from external subscribers. Production requires outbound-egress and SSRF controls, including approved destinations and blocking private/link-local external targets; these protections are not established today.
- Assign least-privilege IAM task roles and separate execution permissions; externalize secrets and configuration with defined ownership and rotation.
- Connect application OpenTelemetry and container logs to an operational observability design, including health, readiness, and failure diagnosis.
- Current Development-only `/health` and `/alive` endpoints are insufficient deployment evidence. Require intentional production health/readiness design before configuring ALB/ECS health behavior or declaring deployability.
- Translate migration/startup gates explicitly and address documented reliability and authentication risks before production use.
- Establish budgets, retention limits, resource ownership, and teardown procedures before provisioning. Managed services do not remove cost or recovery responsibilities.

## Explicitly deferred decisions

- Infrastructure-as-code tool selection and real AWS provisioning.
- pgvector compatibility, versions, privileges, and operating model.
- Redis OSS versus Valkey.
- Identity API hardening or replacement strategy.
- HA topology, scaling, resource sizing, and cost estimates.
- CI/CD design, including federation, promotion, migrations, and rollback.

This ADR records a proposed direction only. It does not claim Free Tier eligibility or AWS deployment success, and does not authorize provisioning or credential access.
