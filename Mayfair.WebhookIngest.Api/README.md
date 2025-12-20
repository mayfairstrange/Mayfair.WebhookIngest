## Webhook ingestion
A small practice project for learning proper ingestion in a backend API.

- Goal: Accept Stripe webhooks reliably, store them, and process them asynchronously.
- Provider: Stripe


## Stripe CLI
```bash
stripe login
stripe listen --forward-to http://localhost:5000/webhooks/stripe
```

### In a second terminal
```bash
stripe trigger payment_intent.succeeded
```