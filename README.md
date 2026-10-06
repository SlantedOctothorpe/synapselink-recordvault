# Synapse Link Record Vault

I was largely dissatisfied with the offerings from Microsoft for exporting data from Dynamics 365 F&O so I built this connector to efficiently move data exported by Synapse Link to any SQL Server database.

## Architecture

D365 F&O -> Synapse Link -> Blob Storage -> System Event Grid Topic -> Azure Service Bus Queue -> Azure Function

RecordVault uses the fact that Synapse Link creates Azure Blob Storage events when it exports data, these events are sent to an Azure Service Bus via the System Event Grid Topic for the storage account.
There are 2 main functions that are both triggered by Azure Service Bus messages:

1. The first takes the event messages that come from blob storage and forward them to a second queue but this time there is a sessionId attached as the entity/table name
2. The next function processes through the sessions as sub-queues to ensure the data for each table is processed in sequence but the data for separate tables can be processed in parallel.

This is built to be run on Azure Functions but could be trivially adapted to have a different entry point triggered by the same mechanism.

## Authentication

All aspects of this setup are design to use Managed Identities as the primary authentication method.
