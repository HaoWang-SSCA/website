#!/bin/bash
# ============================================================
# Centralized Terraform State Storage Bootstrap
# ============================================================
# This creates a SINGLE storage account for ALL your Terraform projects.
# Each project uses a unique state file key (e.g., "ssca-website.tfstate").
# Run this ONCE, then use the same storage account for all projects.

set -e

# ============================================================
# Configuration - UPDATE THESE VALUES FOR YOUR ORGANIZATION
# ============================================================
RESOURCE_GROUP_NAME="terraform-state-rg"
STORAGE_ACCOUNT_NAME="hwtfstate"  # Must be globally unique, lowercase, 3-24 chars
CONTAINER_NAME="tfstate"
LOCATION="westus2"

echo "========================================================"
echo " Centralized Terraform State Storage Bootstrap"
echo "========================================================"
echo ""
echo "This will create:"
echo "  Resource Group:    $RESOURCE_GROUP_NAME"
echo "  Storage Account:   $STORAGE_ACCOUNT_NAME"
echo "  Container:         $CONTAINER_NAME"
echo "  Location:          $LOCATION"
echo ""

# Check if logged in
az account show > /dev/null 2>&1 || {
    echo "❌ Please login to Azure first: az login"
    exit 1
}

SUBSCRIPTION=$(az account show --query name -o tsv)
echo "Using subscription: $SUBSCRIPTION"
echo ""

# Create resource group
echo "📁 Creating resource group..."
az group create \
    --name "$RESOURCE_GROUP_NAME" \
    --location "$LOCATION" \
    --output none

# Create storage account with security features
echo "📦 Creating storage account with security features..."
az storage account create \
    --name "$STORAGE_ACCOUNT_NAME" \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --location "$LOCATION" \
    --sku Standard_LRS \
    --kind StorageV2 \
    --https-only true \
    --min-tls-version TLS1_2 \
    --allow-blob-public-access false \
    --output none

# Enable blob versioning (rollback protection)
echo "🔄 Enabling blob versioning..."
az storage account blob-service-properties update \
    --account-name "$STORAGE_ACCOUNT_NAME" \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --enable-versioning true \
    --output none

# Enable soft delete (accidental deletion protection)
echo "🗑️  Enabling soft delete (14 days retention)..."
az storage blob service-properties delete-policy update \
    --account-name "$STORAGE_ACCOUNT_NAME" \
    --days-retained 14 \
    --enable true \
    --output none

# Enable container soft delete
az storage account blob-service-properties update \
    --account-name "$STORAGE_ACCOUNT_NAME" \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --enable-container-delete-retention true \
    --container-delete-retention-days 14 \
    --output none

# Get storage account key
ACCOUNT_KEY=$(az storage account keys list \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --account-name "$STORAGE_ACCOUNT_NAME" \
    --query '[0].value' \
    --output tsv)

# Create blob container
echo "📂 Creating container..."
az storage container create \
    --name "$CONTAINER_NAME" \
    --account-name "$STORAGE_ACCOUNT_NAME" \
    --account-key "$ACCOUNT_KEY" \
    --output none

# Add management lock to prevent accidental deletion
echo "🔒 Adding CanNotDelete lock..."
az lock create \
    --name "terraform-state-lock" \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --lock-type CanNotDelete \
    --notes "Protects Terraform state storage from accidental deletion" \
    --output none

echo ""
echo "========================================================"
echo " ✅ Bootstrap Complete!"
echo "========================================================"
echo ""
echo "Storage Account: $STORAGE_ACCOUNT_NAME"
echo "Container:       $CONTAINER_NAME"
echo "Resource Group:  $RESOURCE_GROUP_NAME"
echo ""
echo "Safety features enabled:"
echo "  ✓ Blob versioning (rollback support)"
echo "  ✓ Soft delete (14-day recovery)"
echo "  ✓ Management lock (deletion protection)"
echo "  ✓ HTTPS only + TLS 1.2"
echo ""
echo "========================================================"
echo " Storage Account Key (for cross-tenant access)"
echo "========================================================"
echo ""
echo "ARM_ACCESS_KEY=$ACCOUNT_KEY"
echo ""
echo "To set as environment variable, run:"
echo "  export ARM_ACCESS_KEY=\"$ACCOUNT_KEY\""
echo ""
echo "========================================================"
echo " Backend Configuration for Your Projects"
echo "========================================================"
echo ""
echo "Add this to your Terraform main.tf files:"
echo ""
echo '  backend "azurerm" {'
echo "    storage_account_name = \"$STORAGE_ACCOUNT_NAME\""
echo "    container_name       = \"$CONTAINER_NAME\""
echo '    key                  = "YOUR_PROJECT_NAME.tfstate"  # Unique per project!'
echo '    # Uses ARM_ACCESS_KEY environment variable'
echo '  }'
echo ""
echo "Example keys for different projects:"
echo "  - ssca-website.tfstate"
echo "  - focus-logistics.tfstate"
echo "  - my-other-project.tfstate"
echo ""
