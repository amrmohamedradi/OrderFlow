# OrderFlow 🚀

> **OrderFlow** is an event-driven, distributed order processing microservices platform built with **.NET 10**, **Clean Architecture**, **CQRS**, **YARP API Gateway**, **MassTransit**, **RabbitMQ**, **SQL Server**, and the **Transactional Outbox Pattern**.

---

## 📐 Architecture Overview

```
                                      +--------------------------+
                                      |    Client / HTTP Request |
                                      +------------+-------------+
                                                   |
                                           X-Correlation-ID
                                                   v
                                      +------------+-------------+
                                      |    YARP API Gateway      |
                                      |       (Port 5000)        |
                                      +------------+-------------+
                                                   |
                                                   v
                                      +------------+-------------+
                                      |      OrderService        |
                                      |       (Port 5001)        |
                                      +-----+--------------+-----+
                                            |              |
                    1. Atomic Transaction   |              |  2. Outbox Publisher
                                            v              v
                        +-------------------+--+        +--+-------------------+
                        |   SQL Server DB      |        |     RabbitMQ         |
                        | (Orders + Outbox)    |        | (OrderCreatedEvent)  |
                        +----------------------+        +----------+-----------+
                                                                   |
                                                            3. Consume Event
                                                                   v
                                                        +----------+-----------+
                                                        |  NotificationService |
                                                        |      (Port 5002)     |
                                                        +-----+----------+-----+
                                                              |          |
                                         4. Save Notification |          | 5. SMTP Email
                                                              v          v
                                                        +-----+----+   +-+-----+
                                                        | SQL DB   |   | Mail  |
                                                        | (Inbox)  |   | Trap  |
                                                        +----------+   +-------+
```

---

## ✨ Key Architectural & Resilience Patterns

- **Clean Architecture & CQRS**: Strict separation of concerns between `Domain`, `Application`, `Infrastructure`, and `API` layers.
- **YARP API Gateway**: Single, unified reverse-proxy entry point routing external requests to internal microservices.
- **Transactional Outbox Pattern**: Orders and outbox event messages are saved atomically within the same database transaction. Prevents data loss during message broker downtime.
- **Consumer Inbox & Deduplication**: MassTransit `InboxState` guarantees idempotent event consumption and guards against duplicate event processing.
- **Distributed Tracing & Correlation**: End-to-end `X-Correlation-ID` header propagation attached to Serilog `LogContext` across HTTP requests, MediatR commands, and RabbitMQ message headers.
- **Health Checks**: Standardized `/health/live` (process health) and `/health/ready` (SQL Server & RabbitMQ readiness) endpoints.
- **Exponential Retry Policy**: MassTransit consumer definitions implement exponential backoff retry algorithms for failure resilience.

---

## 🛠️ Technology Stack

| Component | Technology / Library |
| :--- | :--- |
| **Framework** | .NET 10 / ASP.NET Core |
| **API Gateway** | YARP (Yet Another Reverse Proxy) |
| **Mediation & CQRS** | MediatR |
| **Validation** | FluentValidation + Pipeline Behaviors |
| **Messaging & Bus** | MassTransit v8.1.3 + RabbitMQ 3 |
| **Reliability** | MassTransit EF Core Transactional Outbox / Inbox |
| **ORM & Database** | Entity Framework Core 10 + SQL Server 2022 |
| **Observability** | Serilog (Console & Rolling File Sinks) |
| **Testing** | xUnit, FluentAssertions, WebApplicationFactory, SQLite In-Memory |
| **Containerization** | Docker, Docker Compose |

---

## 🔌 Microservices & Endpoints

| Service | Internal Port | Docker Host Port | Public Routes |
| :--- | :---: | :---: | :--- |
| **ApiGateway** | `8080` | **`5000`** | `POST /api/orders`, `/docs` (Swagger UI proxied from OrderService) |
| **OrderService** | `8080` | **`5001`** | `POST /api/orders`, `/health/live`, `/health/ready`, `/docs` |
| **NotificationService** | `8080` | **`5002`** | `/health/live`, `/health/ready`, `/docs` |
| **RabbitMQ Management** | `15672` | **`15672`** | `http://localhost:15672` (guest/guest) |
| **SQL Server** | `1433` | **`1433`** | `localhost,1433` (sa/YourStrong@Pass123) |

---

## 🚀 Quick Start with Docker Compose

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Step 1: Clone & Navigate
```bash
git clone https://github.com/amrmohamedradi/OrderFlow.git
cd OrderFlow
```

### Step 2: Spin Up Infrastructure & Microservices
```bash
docker-compose up -d --build
```

### Step 3: Verify Container Health
```bash
docker-compose ps
```
All containers (`orderflow-sqlserver`, `orderflow-rabbitmq`, `orderflow-orderservice`, `orderflow-notificationservice`, `orderflow-apigateway-1`) should show `Up` and `healthy`.

> 💡 **Swagger UI** is served at `/docs` on every service (e.g. `http://localhost:5000/docs` through the gateway). Opening the site root (`/`) redirects to `/docs`, and the OpenAPI document is available at `/swagger/v1/swagger.json`.

---

## 📖 API Usage & Request Examples

### 1. Create a New Order (via Gateway)

```http
POST http://localhost:5000/api/orders
Content-Type: application/json
X-Correlation-ID: e2e-test-1001

{
  "customerName": "Alice Johnson",
  "email": "alice.johnson@example.com",
  "totalPrice": 149.99
}
```

#### Expected Success Response (`201 Created`):
```json
{
  "id": "5b93f126-b6d3-4469-b000-059a7f2095c1",
  "customerName": "Alice Johnson",
  "email": "alice.johnson@example.com",
  "totalPrice": 149.99,
  "status": "Pending",
  "createdAt": "2026-07-23T16:16:44.1730471Z"
}
```

---

### 2. Validation Constraints

- `customerName`: Required, non-whitespace, max 100 characters.
- `email`: Required, valid email format.
- `totalPrice`: Must be greater than `0`.

---

## ☁️ Deploying to MonsterASP.NET (Windows / IIS hosting)

MonsterASP.NET is shared IIS hosting: it runs ASP.NET Core apps and provides SQL Server, but it has **no Docker and no RabbitMQ**. Each service therefore needs its own site and database, and the message broker must be hosted externally (e.g. the free [CloudAMQP](https://www.cloudamqp.com/) plan).

### What you need
| Requirement | Details |
| :--- | :--- |
| **3 sites** | One each for `ApiGateway`, `OrderService`, `NotificationService` (each with a Web Deploy publish profile) |
| **2 SQL Server databases** | One for `OrderService`, one for `NotificationService`. They must be separate because both use the default `__EFMigrationsHistory` table |
| **External RabbitMQ** | Host, port (`5671` for TLS), virtual host, username, password |
| **SMTP account** | Optional: Mailtrap sandbox is used by default for notification emails |
| **.NET 10 hosting** | Confirm the hosting plan supports the .NET 10 runtime |

### Configuration (environment variables)
Never commit credentials. Set them as environment variables (in the `<environmentVariables>` section of the published `web.config`, or through the hosting panel):

| Variable | Service | Description |
| :--- | :--- | :--- |
| `ConnectionStrings__DefaultConnection` | Order, Notification | SQL Server connection string (one database per service) |
| `RabbitMQ__Host` | Order, Notification | Broker host name |
| `RabbitMQ__Port` | Order, Notification | `5672` (plain) or `5671` (TLS). Default `5672` |
| `RabbitMQ__UseSsl` | Order, Notification | `true` for TLS (required by CloudAMQP). Default `false` |
| `RabbitMQ__VirtualHost` | Order, Notification | Virtual host. Default `/` |
| `RabbitMQ__Username` / `RabbitMQ__Password` | Order, Notification | Broker credentials |
| `Email__Host`, `Email__Port`, `Email__Username`, `Email__Password`, `Email__From` | Notification | SMTP settings |
| `ReverseProxy__Clusters__orders-cluster__Destinations__destination1__Address` | Gateway | Public URL of the deployed OrderService, e.g. `http://<order-site>.runasp.net/` |

### Publish & deploy
```bash
# 1. Publish each project
dotnet publish src/Services/OrderService/OrderFlow.OrderService.API -c Release -o publish/order
dotnet publish src/Services/NotificationService/OrderFlow.NotificationService.API -c Release -o publish/notification
dotnet publish src/Gateway/OrderFlow.ApiGateway -c Release -o publish/gateway

# 2. Add the environment variables above to each publish/*/web.config
#    (inside <aspNetCore ...><environmentVariables>...</environmentVariables></aspNetCore>)

# 3. Deploy each folder with Web Deploy (or Visual Studio's "Import Profile")
msdeploy -verb:sync -source:contentPath=publish/order   -dest:contentPath=<siteId>,computerName=https://<siteId>.siteasp.net:8172/msdeploy.axd?site=<siteId>,userName=<siteId>,password=<password>,authType=Basic   -allowUntrusted -enableRule:AppOffline -enableRule:DoNotDeleteRule
```

> ℹ️ EF Core migrations are applied automatically when each service starts, so the databases only need to exist and be empty.
>
> ⚠️ If a deployment fails, MonsterASP may leave an `app_offline.htm` file on the site (it shows "Site Under Construction"). Remove it with `msdeploy -verb:delete -dest:contentPath=<siteId>/app_offline.htm,...`.
>
> 🔒 Use HTTPS once SSL is enabled for your domain, and rotate any credentials that were shared while setting things up.

### Verify the deployment
- `GET /health/live` and `GET /health/ready` on OrderService and NotificationService should return `Healthy` (the MassTransit bus can take a few seconds to report `Ready` after a cold start).
- Open `/docs` on the gateway and try `POST /api/orders`.

---

## 🧪 Automated Testing

The project includes unit and integration tests across all layers.

### Run All Tests via CLI
```bash
dotnet test OrderFlow.slnx
```

### Test Suite Structure
- `OrderFlow.OrderService.UnitTests`: Domain rules & Mediator handler unit tests.
- `OrderFlow.NotificationService.UnitTests`: Notification logic & Consumer unit tests.
- `OrderFlow.OrderService.IntegrationTests`: WebApplicationFactory end-to-end integration tests using SQLite in-memory DB and MassTransit TestHarness.
- `OrderFlow.NotificationService.IntegrationTests`: Consumer outbox & handler integration tests.

---

## 🛡️ Reliability Verification (Transactional Outbox Test)

You can verify the Outbox reliability pattern in action during broker outages:

1. **Stop RabbitMQ:**
   ```bash
   docker-compose stop rabbitmq
   ```
2. **Submit a Create Order request:**
   ```bash
   curl -X POST http://localhost:5000/api/orders -H "Content-Type: application/json" -d '{"customerName":"Outbox Test","email":"outbox@example.com","totalPrice":99.00}'
   ```
   *Result:* The API still returns `201 Created` because the order and outbox record are saved atomically into SQL Server.
3. **Restart RabbitMQ:**
   ```bash
   docker-compose start rabbitmq
   ```
   *Result:* MassTransit's outbox background worker automatically detects broker recovery and publishes all pending events to RabbitMQ without message loss.

---

## 📄 License

This project is licensed under the MIT License.
