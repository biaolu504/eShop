output "deployment_probe_image" {
  description = "Local portfolio image name; no registry publication is implied."
  value       = "eshop-deployment-probe:local"
}

output "deployment_scope" {
  description = "This configuration intentionally defines no infrastructure."
  value       = "local-only; no resources"
}
