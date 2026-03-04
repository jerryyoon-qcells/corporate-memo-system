# Corporate Memo System — Azure Deployment Guide

## Architecture Overview

| Component | Azure Service | Notes |
|---|---|---|
| Web application | **Azure App Service** (Linux, container) | Blazor Server + SignalR |
| Database | **Azure SQL Database** | Serverless tier for cost savings |
| File attachments | **Azure Storage — File Share** | Mounted as a path inside the container |
| Container registry | **Azure Container Registry (ACR)** | Stores Docker images |
| Email | **SMTP relay** (SendGrid / Office 365) | Configured via App Settings |
| Secrets | **Azure Key Vault** *(optional)* | For production credential management |

---

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) installed and logged in (`az login`)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed (for local image builds)
- A GitHub repository containing the project (for CI/CD)
- An Azure subscription

---

## Step 1 — Create Azure Resources

Run the commands below in order. Replace the values in `< >` with your own.

```bash
# Variables — edit these
RESOURCE_GROUP="corporatememo-rg"
LOCATION="koreacentral"          # or eastus, westeurope, etc.
ACR_NAME="corporatememoregistry" # must be globally unique, lowercase letters/numbers only
APP_SERVICE_PLAN="corporatememo-plan"
WEBAPP_NAME="corporatememo-app"  # must be globally unique
SQL_SERVER_NAME="corporatememo-sql"
SQL_DB_NAME="CorporateMemo"
SQL_ADMIN_USER="sqladmin"
SQL_ADMIN_PASSWORD="<strong-password>"   # min 8 chars, uppercase, digit, special char
STORAGE_ACCOUNT="corporatememofiles"     # must be globally unique, 3-24 chars
FILE_SHARE_NAME="attachments"

# 1. Resource Group
az group create --name $RESOURCE_GROUP --location $LOCATION

# 2. Azure Container Registry
az acr create \
  --resource-group $RESOURCE_GROUP \
  --name $ACR_NAME \
  --sku Basic \
  --admin-enabled true

# 3. Azure SQL Server + Database
az sql server create \
  --resource-group $RESOURCE_GROUP \
  --name $SQL_SERVER_NAME \
  --location $LOCATION \
  --admin-user $SQL_ADMIN_USER \
  --admin-password "$SQL_ADMIN_PASSWORD"

az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

az sql db create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --name $SQL_DB_NAME \
  --edition GeneralPurpose \
  --family Gen5 \
  --capacity 2 \
  --compute-model Serverless \
  --auto-pause-delay 60

# 4. Azure Storage (for file attachments)
az storage account create \
  --resource-group $RESOURCE_GROUP \
  --name $STORAGE_ACCOUNT \
  --location $LOCATION \
  --sku Standard_LRS \
  --kind StorageV2

az storage share create \
  --account-name $STORAGE_ACCOUNT \
  --name $FILE_SHARE_NAME \
  --quota 100

# Save storage key for later
STORAGE_KEY=$(az storage account keys list \
  --resource-group $RESOURCE_GROUP \
  --account-name $STORAGE_ACCOUNT \
  --query "[0].value" --output tsv)

# 5. App Service Plan (Linux)
az appservice plan create \
  --resource-group $RESOURCE_GROUP \
  --name $APP_SERVICE_PLAN \
  --is-linux \
  --sku B2

# 6. Web App (container)
ACR_LOGIN_SERVER=$(az acr show --name $ACR_NAME --query loginServer --output tsv)

az webapp create \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --name $WEBAPP_NAME \
  --deployment-container-image-name "$ACR_LOGIN_SERVER/corporatememo:latest"
```

---

## Step 2 — Get Connection Strings

```bash
# Azure SQL connection string
SQL_CONN=$(az sql db show-connection-string \
  --server $SQL_SERVER_NAME \
  --name $SQL_DB_NAME \
  --client ado.net --output tsv | \
  sed "s/<username>/$SQL_ADMIN_USER/" | \
  sed "s/<password>/$SQL_ADMIN_PASSWORD/")

echo "SQL connection string:"
echo $SQL_CONN

# ACR credentials
ACR_USERNAME=$(az acr credential show --name $ACR_NAME --query username --output tsv)
ACR_PASSWORD=$(az acr credential show --name $ACR_NAME --query passwords[0].value --output tsv)

echo "ACR Login Server: $ACR_LOGIN_SERVER"
echo "ACR Username: $ACR_USERNAME"
echo "ACR Password: $ACR_PASSWORD"
```

---

## Step 3 — Configure the Web App

### 3a. Mount the Azure Files share (for attachments)

```bash
az webapp config storage-account add \
  --resource-group $RESOURCE_GROUP \
  --name $WEBAPP_NAME \
  --custom-id attachments \
  --storage-type AzureFiles \
  --account-name $STORAGE_ACCOUNT \
  --share-name $FILE_SHARE_NAME \
  --access-key "$STORAGE_KEY" \
  --mount-path /app/attachments
```

### 3b. Set Application Settings

```bash
az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $WEBAPP_NAME \
  --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    "ConnectionStrings__DefaultConnection=$SQL_CONN" \
    "AttachmentSettings__UploadPath=/app/attachments" \
    "AttachmentSettings__MaxFileSizeMb=100" \
    "SmtpSettings__Host=<your-smtp-host>" \
    "SmtpSettings__Port=587" \
    "SmtpSettings__Username=<your-smtp-username>" \
    "SmtpSettings__Password=<your-smtp-password>" \
    "SmtpSettings__FromAddress=noreply@yourdomain.com" \
    "SmtpSettings__FromName=Corporate Memo System" \
    "SmtpSettings__UseSsl=false"
```

> **SMTP options:**
> - **SendGrid**: Host=`smtp.sendgrid.net`, Port=`587`, Username=`apikey`, Password=your SendGrid API key
> - **Office 365**: Host=`smtp.office365.com`, Port=`587`, Username=your email, Password=your password, UseSsl=`false` (STARTTLS)
> - **Gmail**: Host=`smtp.gmail.com`, Port=`587`, Username=your email, Password=App Password

### 3c. Configure ACR access

```bash
ACR_CREDS=$(az acr credential show --name $ACR_NAME)

az webapp config container set \
  --resource-group $RESOURCE_GROUP \
  --name $WEBAPP_NAME \
  --docker-custom-image-name "$ACR_LOGIN_SERVER/corporatememo:latest" \
  --docker-registry-server-url "https://$ACR_LOGIN_SERVER" \
  --docker-registry-server-user "$ACR_USERNAME" \
  --docker-registry-server-password "$ACR_PASSWORD"
```

### 3d. Configure SignalR (Blazor Server requirement)

```bash
# Increase request timeout for long-running SignalR connections
az webapp config set \
  --resource-group $RESOURCE_GROUP \
  --name $WEBAPP_NAME \
  --web-sockets-enabled true

# Increase timeout for large file uploads (100 MB)
az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $WEBAPP_NAME \
  --settings \
    WEBSITE_LOAD_USER_PROFILE=1 \
    WEBSITES_CONTAINER_START_TIME_LIMIT=600
```

---

## Step 4 — Build and Push the Docker Image

### Option A — Manual (one-time or testing)

```bash
cd src/

# Log in to ACR
az acr login --name $ACR_NAME

# Build and push
docker build -t $ACR_LOGIN_SERVER/corporatememo:latest .
docker push $ACR_LOGIN_SERVER/corporatememo:latest

# Restart the web app to pull the new image
az webapp restart --resource-group $RESOURCE_GROUP --name $WEBAPP_NAME
```

### Option B — GitHub Actions CI/CD (recommended)

1. Push the project to a GitHub repository.

2. Add the following **GitHub Secrets** (Settings → Secrets and variables → Actions → New repository secret):

   | Secret name | Value |
   |---|---|
   | `AZURE_CREDENTIALS` | JSON from Step 4c below |
   | `REGISTRY_LOGIN_SERVER` | `<acr-name>.azurecr.io` |
   | `REGISTRY_USERNAME` | ACR admin username |
   | `REGISTRY_PASSWORD` | ACR admin password |
   | `AZURE_WEBAPP_NAME` | Your web app name |
   | `AZURE_RESOURCE_GROUP` | Your resource group name |

3. Create the Azure service principal for GitHub Actions:

   ```bash
   az ad sp create-for-rbac \
     --name "corporatememo-github-actions" \
     --role contributor \
     --scopes /subscriptions/<subscription-id>/resourceGroups/$RESOURCE_GROUP \
     --sdk-auth
   ```

   Copy the entire JSON output as the value for the `AZURE_CREDENTIALS` secret.

4. The workflow file is already at `.github/workflows/azure-deploy.yml`.
   Every push to `master` will automatically build, test, push the Docker image, and deploy.

---

## Step 5 — First-Time Database Initialization

The database schema is created automatically on first startup via `EnsureCreated()`. The application also seeds:

- Roles: **Admin**, **Collaborator**, **Viewer**
- Default admin account:
  - Email: `admin@corporatememo.local`
  - Password: `Admin1234`

**After first login, immediately:**
1. Go to **Administration → User Management**
2. Click **Reset Password** on the admin account and set a strong password
3. Create user accounts for your team

---

## Step 6 — Verify Deployment

```bash
# Get the app URL
az webapp show \
  --resource-group $RESOURCE_GROUP \
  --name $WEBAPP_NAME \
  --query defaultHostName --output tsv

# Check application logs
az webapp log tail \
  --resource-group $RESOURCE_GROUP \
  --name $WEBAPP_NAME
```

Open `https://<webapp-name>.azurewebsites.net` in your browser. You should see the login page.

---

## Step 7 — Custom Domain (Optional)

```bash
# Add custom domain
az webapp config hostname add \
  --resource-group $RESOURCE_GROUP \
  --webapp-name $WEBAPP_NAME \
  --hostname memo.yourdomain.com

# Enable managed SSL certificate (free)
az webapp config ssl create \
  --resource-group $RESOURCE_GROUP \
  --name $WEBAPP_NAME \
  --hostname memo.yourdomain.com

# Bind SSL
CERT_THUMBPRINT=$(az webapp config ssl show \
  --resource-group $RESOURCE_GROUP \
  --name $WEBAPP_NAME \
  --query thumbprint --output tsv)

az webapp config ssl bind \
  --resource-group $RESOURCE_GROUP \
  --name $WEBAPP_NAME \
  --certificate-thumbprint $CERT_THUMBPRINT \
  --ssl-type SNI
```

---

## Estimated Monthly Cost

| Resource | Tier | Est. Cost (USD/month) |
|---|---|---|
| App Service Plan | B2 (Linux) | ~$30 |
| Azure SQL Database | Serverless, 2 vCore | ~$10–$30 (auto-pause when idle) |
| Azure Container Registry | Basic | ~$5 |
| Azure Storage | Standard LRS, 100 GB | ~$2 |
| **Total** | | **~$47–$67** |

> Tip: Use the **F1 (Free)** App Service Plan for evaluation (1 GB RAM, shared, no custom domains).
> Switch to B2 for production (dedicated CPU, WebSockets required for Blazor Server).

---

## Troubleshooting

### App fails to start

```bash
# Stream live logs
az webapp log tail --resource-group $RESOURCE_GROUP --name $WEBAPP_NAME

# Download all logs
az webapp log download --resource-group $RESOURCE_GROUP --name $WEBAPP_NAME
```

**Common causes:**
- Wrong connection string format → check `ConnectionStrings__DefaultConnection` in App Settings
- SQL firewall blocking → ensure "Allow Azure services" is enabled on the SQL server
- Container failed to pull → verify ACR credentials in Web App container settings

### Database schema not created

The schema is created on first request. If the app starts but shows a database error:

1. Check the SQL connection string includes `MultipleActiveResultSets=True`
2. Verify the SQL firewall rule allows Azure services (IP 0.0.0.0 → 0.0.0.0)
3. Check the SQL user has `db_owner` on the `CorporateMemo` database

### File uploads fail (> 30 MB)

Azure App Service has a default request timeout of 230 seconds and a body limit. The app is configured for 128 MB at the SignalR level. If uploads fail:

1. Ensure the Azure Files share is mounted at `/app/attachments`
2. Verify `AttachmentSettings__UploadPath` is set to `/app/attachments`
3. Check the File Share quota in the Storage Account

### Blazor SignalR disconnects frequently

Blazor Server requires persistent WebSocket connections. On Azure App Service:

1. Confirm WebSockets is enabled (Step 3d above)
2. Use at least **B1** App Service Plan (Free/Shared tiers do not support WebSockets)
3. If using multiple instances, enable **ARR Affinity** (Azure portal → Web App → Configuration → General → ARR Affinity = On) to route a user's requests to the same instance

### Emails not sending

1. Check SMTP settings in App Settings (all `SmtpSettings__*` values)
2. For SendGrid, ensure the API key has **Mail Send** permission
3. For Office 365, ensure the account is not MFA-protected (use an App Password if MFA is enabled)
4. Test connectivity: `az webapp ssh` into the container and run `curl smtp.sendgrid.net:587`
