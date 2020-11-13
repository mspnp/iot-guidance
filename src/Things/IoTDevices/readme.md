# Deploy device simulation

## Open Azure Cloud Shell

[![](https://shell.azure.com/images/launchcloudshell.png "Launch Azure Cloud Shell")](https://shell.azure.com)

## Install prerequisites

```bash
# export environment variables
export LOCATION=<location>

export RESOURCE_GROUP=<resource-group>

export IOTHUB_NAME=<iothub-name>

export COSMOSDB_NAME=<storageadapter-cosmosdb-nane>

# create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# create Azure IoT Hub instance (if you have already created one, please skip this step)
az iot hub create -g $RESOURCE_GROUP -n $IOTHUB_NAME --sku S3 -l $LOCATION --unit 10 --partition-count 32

# create Azure Cosmos DB database account
az cosmosdb create --resource-group $RESOURCE_GROUP -n $COSMOSDB_NAME --enable-automatic-failover false --kind GlobalDocumentDB --default-consistency-level Strong

# create Azure Cosmos Database
az cosmosdb database create -g $RESOURCE_GROUP -n $COSMOSDB_NAME \
  --db-name pcs-storage
```

## Deploy

You can deploy to Kubernetes (for testing) or a Virtual Machine (for development)

### Kubernetes deployment option

Export additional environment variables

```bash
export DRONE_DEVICESIMULATION_IMAGE={MyDeviceSimulationDockerImage}

export IOTHUB_CONNSTR=$(az iot hub show-connection-string -g $RESOURCE_GROUP -n $IOTHUB_NAME --query connectionString | sed -e 's/^"//' -e 's/"$//') && \
export COSMOSDB_CONNSTR="AccountEndpoint=https://${COSMOSDB_NAME}.documents.azure.com:443/;AccountKey=$(az cosmosdb list-keys -g $RESOURCE_GROUP -n $COSMOSDB_NAME --query primaryMasterKey | sed -e 's/^"//' -e 's/"$//')"
```

Create ACS kubernetes cluster

> :warning: WARNING: The process that follows is out of date as it uses the legacy Azure Container Service (ACS) references and does not follow the [AKS Baseline Implementation](https://aka.ms/architecture/aks-baseline). You will likely need to adapt the following set of commands to work with the current AKS implementation.

```bash
# create Service Principal
export SP_CLIENT_SECRET=$(cat /proc/sys/kernel/random/uuid) && \
export SP_APP_ID=$(az ad sp create-for-rbac --role="Contributor" -p $SP_CLIENT_SECRET | grep -oP '(?<="appId": ")[^"]*')

# create cluster
az acs create \
   --resource-group $RESOURCE_GROUP \
   --name dronedelivery-cluster \
   --agent-count 3 \
   --agent-vm-size Standard_DS4_v2 \
   --orchestrator-type kubernetes \
   --generate-ssh-keys \
   --service-principal=$SP_APP_ID --client-secret=$SP_CLIENT_SECRET

# merge context
az acs kubernetes get-credentials \
   --resource-group $RESOURCE_GROUP \
   --name dronedelivery-cluster
```

Deploy storage adapter and device simulator

```bash
# clone the repo
git clone https://github.com/mspnp/iot-guidance.git iotguidance

# navigate the local repo folder
cd ./iotguidance/src/Things/IoTDevices/IoTDevicesDeployment/k8s

# update Kubernetes config values
sed -i "s#image:#image: ${DRONE_DEVICESIMULATION_IMAGE}#g" device-simulation.yaml

# create the Drone Management namespace
kubectl create ns dronemgmt

# create secrets
kubectl -n dronemgmt create secret generic devicesimulation \
  --save-config=true \
  --from-literal=IoTHub_ConnectionString=${IOTHUB_CONNSTR[@]//\"/} \
  --from-literal=CosmosDB_ConnectionString=${COSMOSDB_CONNSTR[@]//\"/}

# deploy storage adapter in Kubernetes
kubectl -n dronemgmt apply -f configmap.yaml && \
kubectl -n dronemgmt apply -f storage-adapter.yaml

# init storageadapter
kubectl -n dronemgmt exec $(kubectl -n dronemgmt get pods -l app=storageadapter -o jsonpath="{.items[0].metadata.name}") -- curl -H "Content-Type: application/json" -X POST -d '{}' http://localhost:9022/v1/collections/init/values

# create a new document in the pcs-storage collection of your new cosmosdb using the following as value
# {
#    "CollectionId": "simulations",
#    "Key": "1",
#    "Data": "{\"ETag\":\"\\\"0100f811-0000-0000-0000-5aa31b4e0000\\\"\",\"Id\":\"1\",\"Enabled\":true,\"DeviceModels\":[{\"Id\":\"drone-01\",\"Count\":10000}]}",
#    "id": "simulations.1",
#    "_rid": "U+cNAOqA7QABAAAAAAAAAA==",
#    "_self": "dbs/U+cNAA==/colls/U+cNAOqA7QA=/docs/U+cNAOqA7QABAAAAAAAAAA==/",
#    "_etag": "\"5000dcf2-0000-0000-0000-5af9e9090000\"",
#    "_attachments": "attachments/",
#    "_ts": 1526327561
#}

# deploy device simulation in k8s and add as many replicas as needed depending on the throughput required
kubectl -n dronemgmt apply -f device-simulation.yaml
```

Navigate to your Azure IoT Hub and you should start seeing some activity.

Add more Public IPs Addresses to your cluster

> WARNING: It's easy to run out of available ports when connecting thousands devices. In case this limitation is reached, please follow some of the problem solving approaches listed [here](https://docs.microsoft.com/azure/load-balancer/load-balancer-outbound-connections#problemsolving).

```bash
# Create a new Public IP address
az network public-ip create -g $RESOURCE_GROUP --sku basic -n <publicip-name> --allocation-method Dynamic

# Associate the recently created public VIP to one of your agent nodes. It is also possible to use a LB. For more information please visit https://docs.microsoft.com/en-us/azure/load-balancer/load-balancer-outbound-connections#problemsolving
az network nic ip-config update  -g $RESOURCE_GROUP --name ipconfig1 --public-ip-address <publicip-name> --nic-name <k8s-agentnode-nic-name>
```

### Virtual Machine deployment option

Create a new Azure Linux VM

```bash
az vm create -g $RESOURCE_GROUP -n <vm-name> --attach-os-disk <disk> --os-type linux
```

> Note: you could also create this Virtual Machine from [Azure Portal](https://ms.portal.azure.com)

Ensure SSH access is enabled

```bash
az network nsg rule create --name SSH --nsg-name <network-security-group> --priority 101 --destination-port-ranges 22 --access Allow --protocol TCP
```

Install the following components in the new VM:

  1. [NET core 2.0](https://www.microsoft.com/net/learn/get-started/linux/ubuntu16-04)
  1. [Docker](https://docs.docker.com/)
  1. [Docker Compose](https://docs.docker.com/compose/install/)

Clone or download the following repos within the new VM

```bash
git clone https://github.com/kirpasingh/device-simulation-dotnet.git && \
git clone https://github.com/Azure/pcs-storage-adapter-dotnet.git
```

The deployment steps shown here use Bash shell commands. On Windows, you can use the [Windows Subsystem for Linux](https://docs.microsoft.com/windows/wsl/about) to run Bash.

Run Storage and Simulation containers (commands need to be executed within the VM)

```bash
# build the PCS storage adapter docker image
cd pcs-storage-adapter-dotnet/scripts/docker
./build

# build the PCS device simulation docker image
cd ../../../device-simulation-dotnet/scripts/docker
./build

# edit the docker-compose.yml file with your text editor of preference and add the following information to the environment section under services->devicesimulation
- PCS_IOTHUB_CONNSTRING=<YOUR_PCS_IOT_HUB_CONNSTRING_HERE>
- PCS_TWIN_READ_WRITE_ENABLED=false
- PCS_LOG_LEVEL=Error
- PCS_IOTSDK_CONNECT_TIMEOUT=60000

# edit the docker-compose.yml file with your text editor of preference and add the following information to the environment section under services->storageadpater
- PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING=<PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING>

# Run containers
docker-compose up -d
```

> Note:
> Find your [Azure Cosmos DB Connection String](https://docs.microsoft.com/azure/cosmos-db/create-sql-api-dotnet#update-your-connection-string)
> Find your [Azure IoT Hub Connection String](https://blogs.msdn.microsoft.com/iotdev/2017/05/09/understand-different-connection-strings-in-azure-iot-hub/)
