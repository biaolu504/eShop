# Proposed ECR Terraform bootstrap policy

`eshop-learning-ecr-terraform-policy.json` is a draft identity-based permissions policy for the repository and lifecycle configuration under `infra/terraform`. It is not a role trust policy or an ECR repository policy. No AWS access, IAM creation/attachment, or Terraform execution was performed to prepare it.

## Account and resource scope

Before use, an authorized reviewer must replace `REPLACE_WITH_ACCOUNT_ID` with the intended account's 12-digit ID in a private deployment copy. The checked-in placeholder is deliberately not a deployable account identifier. Do not replace it with an account wildcard.

IAM policy variables are supported only in the resource portion of an ARN, after the fifth colon, not in its account segment. Consequently, `${aws:PrincipalAccount}` is not used as an account-segment substitution. See [IAM policy variable placement](https://docs.aws.amazon.com/IAM/latest/UserGuide/reference_policies_variables.html#policy-vars-using).

The exact resource is `eshop-deployment-probe` in `us-east-1`; `aws:RequestedRegion` also restricts the endpoint region. The policy assumes the standard AWS partition. Changing Terraform's region variable does not expand these permissions.

## Included permissions

| Actions | Required purpose |
| --- | --- |
| `CreateRepository` | Create the named private repository with its configured settings. |
| `DescribeRepositories`, `ListTagsForResource` | Read the named repository's settings, identifiers, and tags during refresh. |
| `TagResource`, `UntagResource` | Reconcile Terraform-managed repository tags. |
| `PutImageTagMutability` | Update repository image-tag mutability. |
| `PutImageScanningConfiguration` | Update repository scan-on-push configuration. |
| `GetLifecyclePolicy`, `PutLifecyclePolicy`, `DeleteLifecyclePolicy` | Read, configure, and remove the repository's lifecycle policy. |
| `DeleteRepository` | Support reviewed teardown using the existing `force_delete=false` configuration. |

AWS supports repository-resource scoping for these actions, including creation. Terraform reads this repository by name, so no `Resource: "*"` discovery statement is added. Broad console listing is not a requirement. See the [ECR authorization reference](https://docs.aws.amazon.com/service-authorization/latest/reference/list_ecr.html). The operation mapping follows the provider's [repository implementation](https://github.com/hashicorp/terraform-provider-aws/blob/main/internal/service/ecr/repository.go) and [lifecycle implementation](https://github.com/hashicorp/terraform-provider-aws/blob/main/internal/service/ecr/lifecycle_policy.go); recheck against the resolved provider version before attachment.

## Deliberate exclusions and limitations

- No `ecr:GetAuthorizationToken`, Docker image push/pull actions, or direct image-deletion permissions. Image publication is later work.
- No repository-policy administration, registry-wide settings, enhanced-scanning administration, IAM, ECS, or state-backend permissions. AES256 configuration does not require customer-managed KMS permissions here.
- Empty-only deletion is enforced by the reviewed Terraform configuration's `force_delete=false`, not by a distinct IAM action. `DeleteRepository` itself can be invoked with a force option; this policy must not be represented as an IAM-enforced empty-only deletion boundary.
- Lifecycle-management permission permits policy changes that can expire images. It is not a guarantee that only the currently configured retention rules can be applied. Likewise, update permissions do not independently enforce immutability, scan-on-push, or particular tag values.
- This is a scoped Allow policy, not a permissions boundary. Other attached policies can grant additional access, and organizational controls can restrict it further. Review effective permissions and provider authentication behavior; do not broaden permissions automatically after a denied request.

## Manual bootstrap and authorization

Applying/attaching this policy requires root or another IAM administrator with the necessary permissions. Prefer an authorized IAM administrator using temporary access rather than root for daily work. After explicit review, that administrator would manually create and attach the policy to the approved Terraform execution identity; Terraform itself does not manage IAM in this learning step.

**No policy creation or attachment is authorized by this draft.** Before attachment, review the account replacement, intended role and trust configuration, effective permissions, region, cost authorization, and teardown responsibility. Before any apply, review the Terraform plan and preserve `force_delete=false`. Neither this document nor the policy claims deployed resources or validated AWS permissions.
