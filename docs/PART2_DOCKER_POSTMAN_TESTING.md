\# CoffeeNChill Part 2 – Docker and Postman Testing



\## Contributor



Sena Ndlovu



\## Overview



This document explains how the CoffeeNChill Azure Functions application was containerised and tested for Part 2. Docker Compose runs the Azure Functions API together with Azurite, which locally emulates Azure Blob, Queue and Table Storage.



The Part 2 order-processing functionality allows a customer order to be added to an Azure Queue. A queue-triggered function processes the message and saves the order to Azure Table Storage. The order status can then be retrieved and updated through HTTP endpoints.



\## Technologies Used



\- .NET 10

\- C#

\- Azure Functions isolated worker

\- Azure Queue Storage

\- Azure Table Storage

\- Docker Desktop

\- Docker Compose

\- Azurite

\- Postman

\- GitHub

\- Docker Hub



\## Docker Hub Image



The CoffeeNChill Azure Functions image is published on Docker Hub.



Docker Hub repository:



https://hub.docker.com/r/senandlovu/coffeenchill-functions



Image name:



```text

senandlovu/coffeenchill-functions:part2

```



The image can be downloaded using:



```powershell

docker pull senandlovu/coffeenchill-functions:part2

```



\## Docker Components



The Docker Compose configuration contains two services:



\### Azurite



Azurite provides local emulation for:



\- Azure Blob Storage

\- Azure Queue Storage

\- Azure Table Storage



The Azurite container is named:



```text

coffeenchill-part2-azurite

```



\### Azure Functions



The Functions container runs the CoffeeNChill API and queue-triggered functions.



The Functions container is named:



```text

coffeenchill-part2-functions

```



\## Local Ports



The following local ports are used:



| Service | Host port | Container port |

|---|---:|---:|

| Azure Functions API | 18080 | 80 |

| Azurite Blob Storage | 11000 | 10000 |

| Azurite Queue Storage | 11001 | 10001 |

| Azurite Table Storage | 11002 | 10002 |



The local API base URL is:



```text

http://127.0.0.1:18080

```



\## Starting the Application



Open PowerShell in the repository root.



Run the following command to build and start the containers:



```powershell

docker compose up -d --build

```



Check the running containers:



```powershell

docker compose ps

```



Both of the following containers should show a status of `Up`:



```text

coffeenchill-part2-azurite

coffeenchill-part2-functions

```



\## Viewing Application Logs



Display the latest Functions logs with:



```powershell

docker compose logs functions --tail 100

```



Follow the Functions logs continuously with:



```powershell

docker compose logs -f functions

```



The logs should list the order-processing functions, including:



```text

QueueOrder

ProcessOrderQueue

GetOrderStatus

UpdateOrderStatus

```



\## Postman Environment



The Postman environment contains the following variable:



```text

baseUrl

```



Its Docker value is:



```text

http://127.0.0.1:18080

```



Postman requests can therefore use URLs such as:



```text

{{baseUrl}}/api/orders/queue

```



\## Test 1: Queue an Order



\### Method



```text

POST

```



\### URL



```text

{{baseUrl}}/api/orders/queue

```



Alternatively, the complete local URL is:



```text

http://127.0.0.1:18080/api/orders/queue

```



\### Request Body



Select `Body`, choose `raw`, select `JSON`, and use:



```json

{

&#x20; "orderId": "ORD-5001",

&#x20; "customerName": "Sena Ndlovu",

&#x20; "selectedItemSkus": \[

&#x20;   "COFFEE-001",

&#x20;   "CAKE-002"

&#x20; ],

&#x20; "totalPrice": 129.90,

&#x20; "orderTimestamp": "2026-10-11T13:15:00Z"

}

```



\### Expected Result



The API queues the order and returns a response similar to:



```json

{

&#x20; "orderId": "ORD-5001",

&#x20; "status": "Queued",

&#x20; "orderDate": "2026-10-11T13:15:00+00:00",

&#x20; "statusUrl": "/api/orders/ORD-5001"

}

```



The queue-triggered function then processes the order and stores it in the `Orders` Azure Table with an initial status of `Received`.



\## Test 2: Get Order Status



\### Method



```text

GET

```



\### URL



```text

{{baseUrl}}/api/orders/ORD-5001

```



Alternatively:



```text

http://127.0.0.1:18080/api/orders/ORD-5001

```



\### Expected Result



The API should return `200 OK` and order information similar to:



```json

{

&#x20; "orderId": "ORD-5001",

&#x20; "customerName": "Sena Ndlovu",

&#x20; "selectedItemSkus": \[

&#x20;   "COFFEE-001",

&#x20;   "CAKE-002"

&#x20; ],

&#x20; "totalPrice": 129.90,

&#x20; "status": "Received"

}

```



\### Postman Post-Response Tests



```javascript

pm.test("Status code is 200", function () {

&#x20;   pm.response.to.have.status(200);

});



pm.test("Response contains the correct order ID", function () {

&#x20;   const response = pm.response.json();

&#x20;   pm.expect(response.orderId).to.eql("ORD-5001");

});



pm.test("Response contains an order status", function () {

&#x20;   const response = pm.response.json();

&#x20;   pm.expect(response.status).to.be.a("string");

});

```



\## Test 3: Update Order Status



\### Method



```text

PUT

```



\### URL



```text

{{baseUrl}}/api/orders/ORD-5001/status

```



Alternatively:



```text

http://127.0.0.1:18080/api/orders/ORD-5001/status

```



\### Request Body



```json

{

&#x20; "status": "Preparing"

}

```



\### Expected Result



The API should return `200 OK` and a response similar to:



```json

{

&#x20; "orderId": "ORD-5001",

&#x20; "status": "Preparing",

&#x20; "message": "Order status updated successfully."

}

```



\### Postman Post-Response Tests



```javascript

pm.test("Status code is 200", function () {

&#x20;   pm.response.to.have.status(200);

});



pm.test("Order ID is correct", function () {

&#x20;   const response = pm.response.json();

&#x20;   pm.expect(response.orderId).to.eql("ORD-5001");

});



pm.test("Status changed to Preparing", function () {

&#x20;   const response = pm.response.json();

&#x20;   pm.expect(response.status).to.eql("Preparing");

});

```



\## Order Lifecycle



The supported order lifecycle is:



1\. `Received`

2\. `Preparing`

3\. `Ready`

4\. `Collected`



The status endpoint can be used to move an order through these processing stages.



\## Validation and Error Testing



The application validates order requests before adding them to the queue.



Examples of invalid requests include:



\- A missing order ID

\- A missing customer name

\- An empty list of item SKUs

\- A total price equal to or below zero

\- An invalid order timestamp

\- An unsupported order status



The API returns an appropriate `400 Bad Request` response when validation fails.



A request for an unknown order returns:



```text

404 Not Found

```



\## Stopping the Containers



Stop and remove the running containers with:



```powershell

docker compose down

```



To remove the containers and the Azurite storage volume, run:



```powershell

docker compose down -v

```



The `-v` option permanently removes the locally stored Azurite data and should only be used when a clean storage environment is required.



\## Docker Troubleshooting



\### Port Conflict



If port `18080` is already in use, stop the process or container using that port before restarting Docker Compose.



Check the containers with:



```powershell

docker ps

```



\### View Container Logs



```powershell

docker compose logs functions --tail 100

```



\### Rebuild Without Cache



If Docker uses an outdated application build, run:



```powershell

docker compose down

docker compose build --no-cache functions

docker compose up -d --force-recreate

```



\### Azurite API-Version Compatibility



The Compose configuration starts Azurite with:



```text

\--skipApiVersionCheck

```



This prevents newer Azure SDK request versions from being rejected by the local Azurite emulator.



\## Test Results



The following results were achieved:



\- The Docker image built successfully.

\- The Functions container started successfully.

\- The Azurite container started successfully.

\- The Queue Order endpoint returned a successful queued response.

\- The order message was added to Azure Queue Storage.

\- The queue-triggered function processed the order.

\- The processed order was saved in Azure Table Storage.

\- The Get Order Status endpoint returned the stored order.

\- The Update Order Status endpoint updated the order successfully.

\- The Postman response tests passed.

\- The Functions image was published to Docker Hub.



\## Conclusion



The CoffeeNChill Part 2 order-processing functionality was successfully containerised and tested. Docker Compose provides a repeatable local environment containing the Azure Functions application and Azurite. Postman verifies the queue, retrieval and status-update endpoints, while Docker Hub provides access to the published application image.

