# armapp
Simple Azure Resource Manager c# app to make REST calls

This app can be used to interactively test the Azure Resource Manager REST API by crafting REST calls to GET to PUT (also DELETE).

In order to use it you need to create a Service Principal and provide a Tenant ID, Client ID (AKA Application ID) and client secret to the application. You can create these by following the instructions here: <a href="https://azure.microsoft.com/en-us/documentation/articles/resource-group-authenticate-service-principal">Authenticating a service principal with Azure Resource Manager</a>.

The full set of steps required to create an Azure app and set up Visual Studio to make Azure Resource Manager REST calls in C# is described here: <a href="https://msftstack.wordpress.com/2016/01/03/how-to-call-the-azure-resource-manager-rest-api-from-c/">How to call the Azure Resource Manager REST API from C#</a>.

