resource "aws_iam_role" "app_role" {
  name = "everland-app-service-role"
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole", Effect = "Allow",
      Principal = { Service = ["ec2.amazonaws.com", "ecs-tasks.amazonaws.com"] }
    }]
  })
}

resource "aws_iam_role_policy_attachment" "main_policies" {
  for_each = toset([
    "arn:aws:iam::aws:policy/AmazonS3FullAccess",
    "arn:aws:iam::aws:policy/AmazonSQSFullAccess",
    "arn:aws:iam::aws:policy/AmazonSNSFullAccess",
    "arn:aws:iam::aws:policy/AWSXRayDaemonWriteAccess",
    "arn:aws:iam::aws:policy/CloudWatchAgentServerPolicy",
    "arn:aws:iam::aws:policy/AmazonSSMReadOnlyAccess",
    "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
  ])
  role       = aws_iam_role.app_role.name
  policy_arn = each.value
}

resource "aws_iam_instance_profile" "app_profile" {
  name = "everland-instance-profile"
  role = aws_iam_role.app_role.name
}