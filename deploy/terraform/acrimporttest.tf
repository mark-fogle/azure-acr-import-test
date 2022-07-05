provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "rg" {
  name     = lower("${var.prefix}_rg")
  location = var.location
}

variable "prefix" {
  description = "Prefix set appropriately to ensure that resources are unique."
}

variable "location" {
  description = "The Azure Region in which all resources in this example should be created."
}

resource "azurerm_user_assigned_identity" "user-identity" {
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  name                = lower("${var.prefix}-user-identity")
}

resource "azurerm_storage_account" "storage" {
  name                      = lower("${var.prefix}storage")
  resource_group_name       = azurerm_resource_group.rg.name
  location                  = azurerm_resource_group.rg.location
  account_tier              = "Standard"
  account_kind              = "StorageV2"
  account_replication_type  = "LRS"
  enable_https_traffic_only = true
}

resource "azurerm_service_plan" "app-plan" {
  name                = lower("${var.prefix}-app-plan")
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  os_type             = "Windows"
  sku_name            = "Y1"
}

resource "azurerm_application_insights" "appinsights" {
  name                = lower("${var.prefix}-appinsights")
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  application_type    = "web"
  tags                = {}
}

data "azurerm_client_config" "current" {}

resource "azurerm_windows_function_app" "acr-import-demo" {
  name                        = lower("${var.prefix}-acr-import-demo")
  location                    = azurerm_resource_group.rg.location
  resource_group_name         = azurerm_resource_group.rg.name
  service_plan_id             = azurerm_service_plan.app-plan.id
  storage_account_name        = azurerm_storage_account.storage.name
  storage_account_access_key  = azurerm_storage_account.storage.primary_access_key
  tags                        = {}
  functions_extension_version = "~4"

  app_settings = {
    "ContainerRegistryManagementConfig:SubscriptionId"        = data.azurerm_client_config.current.subscription_id
    "ContainerRegistryManagementConfig:ResourceGroupName"     = azurerm_resource_group.rg.name
    "ContainerRegistryManagementConfig:ContainerRegistryName" = azurerm_container_registry.dest-acr.name
    "ContainerImageImportSource:RegistryUri"                  = azurerm_container_registry.source-acr.login_server
    "ContainerImageImportSource:Credentials:UserName"         = azurerm_container_registry.source-acr.admin_username
    "ContainerImageImportSource:Credentials:Password"         = azurerm_container_registry.source-acr.admin_password
    "WEBSITE_RUN_FROM_PACKAGE"                                = "1"
  }

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_insights_key               = azurerm_application_insights.appinsights.instrumentation_key
    application_insights_connection_string = azurerm_application_insights.appinsights.connection_string
    application_stack {
       dotnet_version              = "6"
       use_dotnet_isolated_runtime = false
    }

  }
}

resource "azurerm_container_registry" "source-acr" {
  name                = lower("${var.prefix}sourceacr")
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = "Basic"
  admin_enabled       = true
}

resource "azurerm_container_registry" "dest-acr" {
  name                = lower("${var.prefix}destacr")
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = "Basic"
  admin_enabled       = true
}

resource "azurerm_role_assignment" "function-manage-destination" {
  principal_id                     = azurerm_windows_function_app.acr-import-demo.identity[0].principal_id
  role_definition_name             = "Contributor"
  scope                            = azurerm_container_registry.dest-acr.id
  skip_service_principal_aad_check = true
}