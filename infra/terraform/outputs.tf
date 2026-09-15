output "deployment_probe_image" {
  description = "Local portfolio image name; no registry publication is implied."
  value       = "eshop-deployment-probe:local"
}

output "deployment_scope" {
  description = "Declared scope, not evidence of a completed deployment."
  value       = "private ECR repository and image lifecycle policy only"
}

output "repository_url" {
  description = "Private ECR repository URL."
  value       = aws_ecr_repository.deployment_probe.repository_url
}

output "repository_arn" {
  description = "Private ECR repository ARN."
  value       = aws_ecr_repository.deployment_probe.arn
}

output "registry_id" {
  description = "ECR registry identifier; do not copy account details into public evidence."
  value       = aws_ecr_repository.deployment_probe.registry_id
}
