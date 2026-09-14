# AWS account readiness: Day 10

## Verified account-safety record

The following facts were supplied as verified Day 10 evidence; this document does not represent a new account inspection.

- Root user MFA was verified as enabled with one assigned MFA device.
- A monthly AWS cost budget named `eShop-learning-zero-spend` was created.
- It is a **$1.00 monthly cost budget**, with an **actual-cost alert above $0.01**.
- At verification time, month-to-date cost was **$0.00** and budget status was **Healthy**.
- No cost monitor existed at the initial review. Its subsequent status is not established by this record.
- Budget alerts are notifications, not automatic spending stops.

The budget name does not imply a zero-spending enforcement mechanism. The recorded cost and status are a point-in-time observation, not a guarantee about later charges. A forecast alert, cost monitor, or automated spending control is not verified here.

## Proposed least-privilege role map

These are future permission boundaries, not existing roles or implemented IAM policies. Prefer temporary role-based access and explicit trust relationships. Do not use root for daily development. Final permissions require review against the approved operation and target resources.

| Proposed role | Allowed purpose | Must not be allowed |
| --- | --- | --- |
| Infrastructure/Terraform role | Plan and manage only the separately approved portfolio infrastructure; access its designated state storage. Scope service actions, resource access, and any role-passing permissions to that approved design. | General account administration, billing/security-control changes, unrelated resource access, unrestricted IAM creation or role passing, or privilege escalation. Infrastructure authorization must not imply permission to publish arbitrary images. |
| Image publisher role | Authenticate to the approved private registry and upload image layers/manifests to the designated DeploymentProbe repository; inspect only the information needed to verify publication. | Repository creation/deletion, repository-policy or scanning/retention changes, IAM administration, ECS task deployment, or writes to unrelated repositories. |
| ECS task execution role | Let the ECS agent pull the approved image and deliver logs to the designated log destination. Scope repository and log permissions where supported. | Image publication, infrastructure or IAM administration, unrelated repository/log access, or application-level data access. Do not treat execution permissions as application permissions. |
| DeploymentProbe application task role | No AWS API permissions are required by the standalone probe. Omit an application role unless required by the later design; if present, give it no AWS permissions without a new justified requirement. | ECR push/pull permissions merely to start the container, secrets access, data-service access, infrastructure changes, IAM administration, or assumed publisher/infrastructure privileges. |

Some authentication actions cannot be scoped to a repository; review these exceptions separately rather than broadening repository operations. Any future Terraform role's ability to create or pass IAM roles requires explicit safeguards against privilege escalation. This document grants no authorization to create these roles or execute a deployment.

## Decisions still open

- **Region:** Choose and record the permitted region and any required global-service exceptions before cloud work. No region selection is verified here.
- **Tagging:** Agree project/environment tags, responsible owner, and expiry. An expiry tag is descriptive unless a separately designed cleanup mechanism enforces it.
- **Teardown:** Define who removes which artifacts, when cleanup occurs, what may be retained, and how removal is confirmed. Include later images, logs, state storage, and runtime/network components in the approved inventory.
- **Cost review:** Agree review frequency, alert-response ownership, whether forecast alerts or a cost monitor are needed, and how costs are checked after teardown and billing updates. Do not treat the existing budget as an automatic cap.
- **Role implementation:** Decide trust policies, temporary access flow, exact actions/resources, state protection, and narrowly scoped role passing before IAM or Terraform work.

## Boundary

This record contains no account identifiers or credential material and makes no claim that application infrastructure or IAM roles were provisioned. No AWS access, credential use, IAM creation, or Terraform execution occurred to write it. It is not deployment evidence or a claim of Free Tier eligibility.
