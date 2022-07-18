## AzureCosmosDbRepositoryLib

This lib is a helper lib for Azure Cosmos DB. It has been
tested against Microsoft Azure Cosmos DB version 3.29.

The lib implements a repository pattern to work against a 'container'
inside a DB. You can pass in class instances and perform different CRUD
operations. 

Please note, for this repository to work, you must provide the connection string
and database name and container id. It is also suggested to provide the 
correct partition key. It will default to '/id'. Also note that operations against a 
container which adds, updates and deletes item(s) requires either a partition key or 
object identifier to be passed in to correctly identify the row in the Azure Cosmos DB. 

Last update : 
19.07.2022 


Tore Aurstad
