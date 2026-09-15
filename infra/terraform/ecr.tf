resource "aws_ecr_repository" "deployment_probe" {
  name                 = "eshop-deployment-probe"
  image_tag_mutability = "IMMUTABLE"
  force_delete         = false

  encryption_configuration {
    encryption_type = "AES256"
  }

  image_scanning_configuration {
    scan_on_push = true
  }

  tags = {
    project     = "eshop"
    environment = "learning"
    component   = "deployment-probe"
    owner       = var.owner
    expiry      = var.expiry
  }
}

resource "aws_ecr_lifecycle_policy" "deployment_probe" {
  repository = aws_ecr_repository.deployment_probe.name

  policy = jsonencode({
    rules = [
      {
        rulePriority = 1
        description  = "Expire untagged images older than seven days since push"
        selection = {
          tagStatus   = "untagged"
          countType   = "sinceImagePushed"
          countUnit   = "days"
          countNumber = 7
        }
        action = { type = "expire" }
      },
      {
        rulePriority = 2
        description  = "Retain the five newest tagged images"
        selection = {
          tagStatus      = "tagged"
          tagPatternList = ["*"]
          countType      = "imageCountMoreThan"
          countNumber    = 5
        }
        action = { type = "expire" }
      }
    ]
  })
}
