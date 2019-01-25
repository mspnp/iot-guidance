# Deploy cold path

## Step 1:
```bash
cd ColdPathDeployment_EnhancedSecurity

az group create -n <resource-group> -l <location>

az group deployment create -n <deployment-name> \
  -g <resource-group> --template-file azuredeploy.json

# Allow ssh access (for security reasons, the recommendation is to enable ssh access only for dev purposes)

az network nsg rule create \
   -g <resource-group> \
   --nsg-name dronedelivery-coldpath-nsg \
   -n sshdevaccess \
   --protocol "*" \
   --source-port-range "*" \
   --destination-port-range "22" \
   --source-address-prefix "*" \
   --destination-address-prefix "VirtualNetwork" \
   --access "Allow" \
   --priority 304 \
   --direction "Inbound"

# Allow Ambari access 

az network nsg rule create \
   -g <resource-group> \
   --nsg-name dronedelivery-coldpath-nsg \
   -n ambariacess \
   --protocol "TCP" \
   --source-port-range "*" \
   --destination-port-range "443" \
   --source-address-prefix "*" \
   --destination-address-prefix "VirtualNetwork" \
   --access "Allow" \
   --priority 305 \
   --direction "Inbound"

# Change storage accounts to default action Deny

az storage account update --resource-group <resource-group> --name <telemetry-storageaccount> --default-action Deny

az storage account update --resource-group <resource-group> --name <hdimetadata-storageaccount> --default-action Deny
```

> Important: You can either use a region other than Brazil South, Canada East, Canada Central, West Central US, and West US 2, or modify the [Azure Data Center IP addresses defined in the Network Security Rule](https://docs.microsoft.com/en-us/azure/hdinsight/hdinsight-extend-hadoop-virtual-network?toc=%2fazure%2fvirtual-network%2ftoc.json#hdinsight-ip-1).

## Resume from readme.md
Secure setup ends just here. The rest of the setup can be achived by resuming from step #2 in [readme.md](readme.md) 
