# AWS connectivity test web app

This repository includes a small ASP.NET 10 Razor Pages app at `src/AwsTestWeb` for testing:

- **PostgreSQL IAM database authentication** against RDS
- **Secrets Manager access** from the app's ECS task role

The home page exposes two buttons:

1. **Test RDS access** — connects to PostgreSQL with an IAM auth token and runs:
   `select id from dev.dashboards order by id limit 1`
2. **Test Secrets Manager access** — reads the configured secret payload, parses it as JSON, and confirms the `iPhone passcode` key exists **without** showing the secret value

## Current deployment recommendation

AWS now recommends **Amazon ECS Express Mode** for simple public containerized web apps. It provisions a Fargate-based ECS service, HTTPS load balancing, scaling, and supporting networking for you, while keeping the resources in your own account.

This app is intended to listen on **container port `8080`** inside ECS. Keep the load balancer listener on `80` or `443`, but make sure the ECS service, target group, and health check all point at container port `8080`.

This repo now targets:

1. **CodeBuild** builds the image in AWS using `buildspec.ecs-express.yml`
2. The image is pushed to **ECR**
3. **ECS Express Mode** deploys the image from ECR

## Quick answer on Docker

You do **not** need Docker Desktop on your machine.

The intended path is to build the image in **AWS CodeBuild** and deploy it from **ECR** into ECS Express Mode.

## Required environment variables

Set these on the ECS Express Mode service:

| Variable | Required | Notes |
| --- | --- | --- |
| `AWS_REGION` | Yes | Region that contains RDS and Secrets Manager |
| `RDS_HOST` | Yes | Use the real RDS endpoint hostname |
| `RDS_PORT` | No | Defaults to `5432` |
| `RDS_DATABASE` | Yes | Database name |
| `RDS_USERNAME` | Yes | PostgreSQL user granted `rds_iam` |
| `RDS_QUERY` | No | Defaults to `select id from dev.dashboards order by id limit 1` |
| `RDS_SSL_MODE` | No | Defaults to `Require` |
| `RDS_ROOT_CERTIFICATE` | No | Path to an RDS CA bundle if you want explicit certificate validation |
| `SECRET_ID` | Yes | Secret name or ARN |
| `SECRET_JSON_KEY` | No | Defaults to `iPhone passcode` |

## IAM roles you need

ECS Express Mode starts from:

1. a container image
2. a **task execution role**
3. an **infrastructure role**

For this app you also need a **task role**, because the container itself calls Secrets Manager and generates RDS IAM auth tokens.

### Task role

This is the app runtime role. Attach:

- the trust policy in `src/AwsTestWeb/deployment/ecs-task-role-trust-policy.json`
- the permissions policy in `src/AwsTestWeb/deployment/ecs-task-runtime-policy.json`

Put these permissions on the **task role**:

- `rds-db:connect`
- `secretsmanager:GetSecretValue`
- `secretsmanager:PutSecretValue`
- `secretsmanager:DescribeSecret`
- optional `secretsmanager:UpdateSecret`
- optional `kms:Decrypt`, `kms:Encrypt`, `kms:GenerateDataKey`, `kms:DescribeKey`

### Task execution role

This is used by ECS/Fargate to pull the image and write logs. Use the AWS-managed policy:

- `arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy`

If you later reference Secrets Manager values directly in the task definition as injected secrets, add the extra permissions AWS documents for that use case.

### Infrastructure role

This lets ECS Express Mode manage the AWS resources it creates for the service. Use:

- the trust policy in `src/AwsTestWeb/deployment/ecs-infrastructure-role-trust-policy.json`
- the AWS-managed policy `AmazonECSInfrastructureRoleforExpressGatewayServices`

## Step-by-step AWS deployment

### 1. Enable PostgreSQL IAM auth

In the RDS console:

1. Open the target PostgreSQL instance or cluster.
2. Enable **IAM DB authentication** if it is not already enabled.
3. Apply the change and reboot if AWS requires it.
4. Record the **DB resource ID** or **cluster resource ID**; you need it for `rds-db:connect`.

In PostgreSQL, run something like:

```sql
CREATE USER ecs_express_test_user WITH LOGIN;
GRANT rds_iam TO ecs_express_test_user;
GRANT USAGE ON SCHEMA dev TO ecs_express_test_user;
GRANT SELECT ON TABLE dev.dashboards TO ecs_express_test_user;
```

### 2. Confirm the secret contents

In Secrets Manager:

1. Open the target secret.
2. Confirm the secret value is JSON.
3. Confirm the JSON contains the `iPhone passcode` key.
4. Do not copy that value into app logs or configuration.

### 3. Create an ECR repository

In ECR:

1. Create a private repository, for example `aws-rds-secrets-test-web`.
2. Keep the repository URI for later.

### 4. Create the ECS task role for the app

In IAM:

1. Create a new role using `src/AwsTestWeb/deployment/ecs-task-role-trust-policy.json`.
2. Create an inline policy from `src/AwsTestWeb/deployment/ecs-task-runtime-policy.json`.
3. Replace the placeholders:
   - `<region>`
   - `<account-id>`
   - `<db-or-cluster-resource-id>`
   - `<db-username>`
   - `<secret-name>`
   - `<kms-key-id>` if the secret uses a customer-managed key

Important:

- `rds-db:connect` belongs on the **task role**
- the Secrets Manager read/write permissions for the app also belong on the **task role**

### 5. Create the ECS task execution role

In IAM:

1. Create a role for **Elastic Container Service Task**
2. Attach `AmazonECSTaskExecutionRolePolicy`
3. Name it something like `ecsTaskExecutionRole`

### 6. Create the ECS infrastructure role

In IAM:

1. Create a role using `src/AwsTestWeb/deployment/ecs-infrastructure-role-trust-policy.json`
2. Attach the AWS-managed policy `AmazonECSInfrastructureRoleforExpressGatewayServices`
3. Name it something like `ecsInfrastructureRole`

If the person creating the ECS service is a human IAM user or a separate deployment role, they also need `iam:PassRole` permission for the task role, task execution role, and infrastructure role.

### 7. Prepare networking for private RDS access

In VPC:

1. Identify the VPC and subnets that can reach the PostgreSQL database
2. Identify or create a security group for the ECS service tasks
3. On the RDS security group, allow inbound PostgreSQL traffic from the ECS service security group

If the selected subnets do not have outbound internet through NAT and you need private AWS API access, add interface VPC endpoints for:

- `com.amazonaws.<region>.secretsmanager`
- `com.amazonaws.<region>.kms` when using a customer-managed key

### 8. Create the CodeBuild project

In CodeBuild:

1. Create a new build project
2. Point it at this repository
3. Enable **Privileged** mode so Docker builds work
4. Use `buildspec.ecs-express.yml`
5. Give the CodeBuild role permission to:
   - read the source repository
   - push images to the ECR repository
   - call `sts:GetCallerIdentity`

Run the build once. When it succeeds, confirm the image exists in ECR.

### 9. Create the ECS Express Mode service

In the ECS console:

1. Open **Express Mode**
2. Create a new service from a **private ECR image**
3. Choose the image built by CodeBuild
4. Set the container port to `8080`
5. Confirm the load balancer health check path is `/health` and the target port is `8080`
6. Select or create the **task execution role**
7. Select the **infrastructure role**
8. Select the VPC, subnets, and task security group that can reach RDS
9. Attach the **task role** so the app can call RDS IAM auth and Secrets Manager
10. Set the environment variables listed above
11. Create the service and wait for ECS Express Mode to finish provisioning

### 10. Test the app

After ECS Express Mode finishes deploying:

1. Open the service URL
2. Click **Run RDS test**
3. Confirm the page shows the first `id` from `dev.dashboards`
4. Click **Run Secrets test**
5. Confirm the page reports that the secret was read and that the `iPhone passcode` key exists
6. Confirm the page never displays the secret value

## Local development

If you want to run the app locally without AWS deployment:

```powershell
dotnet run --project .\src\AwsTestWeb\AwsTestWeb.csproj
```

You will still need valid AWS credentials and matching environment variables for the probes to succeed.

## Build and test

```powershell
dotnet build .\AwsTestWeb.slnx
dotnet test .\AwsTestWeb.slnx
```
