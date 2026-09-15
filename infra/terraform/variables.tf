variable "aws_region" {
  description = "Region for the approved DeploymentProbe ECR repository."
  type        = string
  default     = "us-east-1"
}

variable "owner" {
  description = "Accountable owner required for planning and applying the repository configuration."
  type        = string
  nullable    = false

  validation {
    condition     = length(trimspace(var.owner)) > 0
    error_message = "owner must contain at least one non-whitespace character."
  }
}

variable "expiry" {
  description = "Required agreed review/teardown date (YYYY-MM-DD); this tag does not trigger deletion."
  type        = string
  nullable    = false

  validation {
    condition     = can(regex("^[0-9]{4}-[0-9]{2}-[0-9]{2}$", var.expiry)) && can(formatdate("YYYY-MM-DD", "${var.expiry}T00:00:00Z"))
    error_message = "expiry must be a valid calendar date in YYYY-MM-DD format."
  }
}
