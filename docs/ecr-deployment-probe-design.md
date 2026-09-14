# DeploymentProbe image publication to ECR

**Status: Proposed design, not an implementation.** This follows the [AWS readiness stages](aws-deployment-readiness.md), [Terraform direction](adr/0002-infrastructure-as-code-direction.md), and [local probe validation](deployment-probe-validation.md).

## Purpose and excluded scope

Propose a controlled path from the local `eshop-deployment-probe:local` image to a private Amazon ECR repository and, separately, a later ECS/Fargate task. ECR publication stores an image; it does not run the application or validate ECS readiness.

This document excludes provisioning, Terraform provider/resource code, credential use, Docker execution, image publication, and ECS execution. The future probe task remains private, time-bounded, and without a public endpoint, database, cache, broker, identity dependency, or application secrets. It is not an eShop vertical slice.

## Image identity and repository

Proposed private repository name: **`eshop-deployment-probe`**. Confirm naming and region before implementation. Retag the validated local image with a unique release/build identifier tied to its source revision; do not use a reusable `latest` deployment tag.

Recommend immutable tags without overwrite exceptions. After publication, record the ECR manifest digest and use a digest reference for the later ECS task. A local image ID is not necessarily the registry manifest digest. Immutability prevents tag replacement, not image deletion or vulnerability discovery. See [ECR tag immutability](https://docs.aws.amazon.com/AmazonECR/latest/userguide/image-tag-mutability.html).

## Push and pull boundaries

| Actor | Proposed access |
| --- | --- |
| Authorized infrastructure role | Create/configure the approved repository, retention, scanning, and permissions; cleanup only within reviewed scope. |
| Publisher role, assumed by an approved developer or future CI identity | Authenticate and upload layers/manifests to this repository; only required inspection permissions. No repository deletion, policy administration, or ECS deployment permission by default. |
| ECS task execution role | Authenticate and pull this repository's image; separately scoped log-delivery permissions. No push or repository administration. |
| Application task role | No AWS permissions required by the probe application itself; do not give it publisher credentials or ECR access merely to start the container. |

Use temporary role-based access and repository-scoped permissions where supported. ECR authorization-token access requires separate IAM treatment because it is not repository-scoped; restrict image operations to the intended repository. Do not introduce cross-account access by default. ECS image pull belongs to the [task execution role](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/task_execution_IAM_role.html), not application code.

## Scanning, retention, and cleanup

Choose basic versus enhanced scanning before provisioning, accounting for coverage, frequency, permissions, and cost authorization. Review completed findings and document remediation or explicitly approved exceptions before any runtime experiment. An image being pushed successfully or having no reported findings is not proof of safety. See [ECR scanning](https://docs.aws.amazon.com/AmazonECR/latest/userguide/image-scanning.html).

Define bounded retention for tagged builds and untagged images. Preview lifecycle rules before enabling deletion; preserve any digest required for an active experiment or agreed rollback. Image deletion can prevent future task starts even if an existing task is running. Immutable tags do not replace retention controls.

Assign cleanup ownership for images, repository, scan-related settings, and later runtime/log/network resources. Removing a local container does not remove its local image or an ECR copy. Confirm retained artifacts and follow up on costs after teardown.

## Authorization before cloud work

Require explicit AWS-account and cost authorization before repository creation or publication. Confirm the readiness checklist: MFA, non-root daily access, temporary roles, chosen region, actual/forecast budget alerts, owner/expiry tags, cost review, and teardown scope. Alerts are not instant spending stops. No account setup, thresholds, pricing, or Free Tier eligibility is assumed here.

## Future validation sequence

1. **Authenticate:** After authorization, obtain temporary access for the intended role and verify target environment/region. Keep registry authentication material out of logs, source, and evidence.
2. **Create repository:** Use the separately authorized infrastructure role and reviewed configuration for private visibility, immutable tags, encryption, scanning, and retention.
3. **Tag:** Identify the locally validated image, source revision, base-image digests, and CPU architecture; attach its unique repository-qualified tag.
4. **Push:** Publish through the restricted publisher role; retain sanitized completion evidence.
5. **Inspect digest:** Confirm the repository manifest digest, tag association, platform, and scan results. Record the approved digest for runtime use.
6. **Only later, ECS:** Separately authorize a private task experiment after health/readiness, IAM, networking, observability, cost, and teardown review. Verify startup, graceful shutdown, and cleanup; do not infer these from ECR success.

## Risks and open decisions

Open decisions include region, role trust policies, tag convention, encryption key ownership, scanning mode/acceptance policy, retention periods, CI federation, and Terraform implementation boundaries. Later ECS work must decide private image-pull connectivity, task architecture, health behavior, and time limits. Risks include publishing the wrong image, stale pinned bases, credential leakage, overbroad permissions, premature lifecycle deletion, and recurring charges. No AWS access, push, provisioning, or deployment result is claimed.
