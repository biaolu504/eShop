# ECR IAM policy documentation

## Day 11: Terraform bootstrap policy

[eshop-learning-ecr-terraform-policy.json](eshop-learning-ecr-terraform-policy.json) is a documentation draft for Terraform's ECR repository and lifecycle management. It is an identity-based permissions policy, not a trust policy or an ECR repository policy.

The draft uses `REPLACE_WITH_ACCOUNT_ID`. Substitute the intended account ID privately before using the policy, and retain the placeholder in this documentation. An account wildcard would broaden the resource scope beyond the intended account. An IAM policy variable cannot substitute for the account segment of a resource ARN, so no IAM account-segment variable is used.

The exact resource scope is `arn:aws:ecr:us-east-1:REPLACE_WITH_ACCOUNT_ID:repository/eshop-deployment-probe`, with requests restricted to `us-east-1`. Changing Terraform variables does not expand this fixed repository or region restriction. A different repository or region requires a separately reviewed policy change.

| Terraform action | Purpose |
| --- | --- |
| `ecr:CreateRepository` | Create the scoped repository. |
| `ecr:DescribeRepositories`, `ecr:ListTagsForResource` | Read repository configuration and tags for Terraform refresh and planning. |
| `ecr:TagResource`, `ecr:UntagResource` | Manage repository tags. |
| `ecr:PutImageTagMutability` | Manage the repository's image-tag mutability setting. |
| `ecr:PutImageScanningConfiguration` | Manage the repository's basic scan-on-push configuration. |
| `ecr:GetLifecyclePolicy`, `ecr:PutLifecyclePolicy`, `ecr:DeleteLifecyclePolicy` | Read, configure, or remove the repository's lifecycle policy. |
| `ecr:DeleteRepository` | Delete the scoped repository when Terraform operations require it. |

The policy deliberately excludes ECR authorization tokens, image push/pull permissions, repository-policy administration, registry-wide settings, enhanced-scanning administration, IAM, ECS, and state-backend permissions.

Lifecycle management can cause image expiration under the configured rules. Removing a lifecycle policy stops that policy's future expiration rules; it does not restore expired images. `ecr:DeleteRepository` is destructive: deleting a nonempty repository requires a force deletion request, which also deletes its images. These permissions support Terraform management but do not constitute approval to expire images, destroy the repository, or force deletion. Review lifecycle changes and destructive Terraform operations before applying them.

This is a scoped Allow policy, not a permissions boundary. It does not cap permissions granted by other identity policies or independently enforce isolation against broader grants. Effective access also depends on applicable explicit denies and other AWS authorization controls.

Bootstrap is a manual IAM step: privately substitute the account placeholder, review the resulting policy, and have an authorized IAM administrator attach it to the intended Terraform execution identity. An authorized IAM administrator or root is required only for that bootstrap attachment step; use the Terraform identity for subsequent repository management. This policy does not grant permission to create or attach IAM policies, change its own permissions, or access a Terraform state backend. Any state-backend access must be provisioned separately with its own scope.

## DeploymentProbe ECR publisher policy

[eshop-learning-ecr-publisher-policy.json](eshop-learning-ecr-publisher-policy.json) is a documentation draft mirroring the deployed publisher policy, with `REPLACE_WITH_ACCOUNT_ID` substituted for the account ID.

The policy allows `ecr:GetAuthorizationToken` with `Resource: "*"`, subject to `aws:RequestedRegion` being `us-east-1`. Every other allowed action is scoped only to `arn:aws:ecr:us-east-1:REPLACE_WITH_ACCOUNT_ID:repository/eshop-deployment-probe` and has the same region restriction.

The repository permissions support image publication and verification:

- `ecr:BatchCheckLayerAvailability`, `ecr:InitiateLayerUpload`, `ecr:UploadLayerPart`, `ecr:CompleteLayerUpload`, and `ecr:PutImage` support publishing layers and manifests.
- `ecr:BatchGetImage` is required by Docker's push protocol to inspect an existing manifest. It is limited to this repository and does not grant broad image pull access.
- `ecr:DescribeImages` and `ecr:DescribeImageScanFindings` are read-only publication and scan verification permissions.

This policy does not grant ECS permissions, IAM administration, deletion, repository management, or broad image pull access. Authorization-token access does not expand the repository-scoped permissions.

See the [Day 12 image publication record](../ecr-deployment-probe-image-publication.md) for the verified publication and scan results and their limits.
