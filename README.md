## Webhook ingestion
A small practice project for learning proper ingestion in a backend API.

- Goal: Accept Stripe webhooks reliably, store them, and process them asynchronously.
- Provider: Stripe


## Prerequisites
- Stripe CLI (the `stripe` command has to work in cmd)
- .NET SDK
- Docker

## Stripe CLI
```bash
stripe login
stripe listen --forward-to http://localhost:5000/webhooks/stripe
```

### In a second terminal
```bash
stripe trigger payment_intent.succeeded
```


## Docker compose
In repo root:
```bash
docker compose up
```

This hosts Postgres on port `5433:5432` and pgAdmin on port `5050`.

```
http://localhost:5050/login?next=/
```
- User: `admin@example.com`
- Password: `admin_pw`

In pgAdmin, add a new server with the following connection properties:
- Host name: postgres
- Port: 5432
- Database: webhookingest
- Username: app
- Password: app_pw



## Webhook ingest API
In repo/solution root:
```bash
dotnet build
dotnet run --project .\Mayfair.WebhookIngest.Api
```

## Useful .env 
```
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS="http://localhost:5000"
Stripe__WebhookSecret=whsec_xxx
```

## EF Migrations
```
dotnet tool install --global dotnet-ef
```
Run these from repo/solution root.
### Initial
```bash
dotnet ef migrations add InitialCreate --project ./Mayfair.WebhookIngest.Infrastructure --startup-project ./Mayfair.WebhookIngest.Api --context AppDbContext
```
### Update
```bash
dotnet ef database update --project ./Mayfair.WebhookIngest.Infrastructure --startup-project ./Mayfair.WebhookIngest.Api --context AppDbContext
```