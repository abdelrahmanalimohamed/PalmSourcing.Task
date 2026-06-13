# OrderProcessor + Confirmation Workflow

I'd avoid jumping straight to "everything becomes a microservice". 
The existing system is a monolith and the business is a private-school software company, not Amazon.
 I'd propose **bounded services around clear business capabilities**, while keeping transactions manageable.

---

# 1. Proposed Service Boundaries

| Service              | Owns                                        | Database           | Future And Existing Public Contract   |
| -------------------- | ------------------------------------------- | ------------------ | ------------------------------------- |
| Order Service        | Order lifecycle, status, totals             | Orders, OrderLines | CreateOrder, ConfirmOrder, GetOrder   |
| Pricing Service      | School tiers, discounts, embroidery pricing | Pricing rules      | CalculatePrice                        |
| Inventory Service    | Stock availability and reservations         | Stock              | ReserveStock, ReleaseStock            |
| Payment Service      | Payment intents and payment status          | Payment records    | CreatePaymentIntent, PaymentSucceeded |
| Notification Service | Email/SMS delivery                          | Notification log   | SendOrderConfirmation                 |
| School Service       | School metadata and tier assignment         | Schools            | GetSchoolTier                         |

---

## Ownership Diagram

```text
Admin
  |
  v
Order Service
  |
  +--> School Service
  |
  +--> Pricing Service
  |
  +--> Inventory Service
  |
  +--> Payment Service
  |
  +--> Notification Service
```

---

## Transaction Boundaries

### Local Transaction

Inside Order Service:

```text
Create Order
Store Order Lines
Persist Pending Status
```

Single database transaction.

---

### Distributed Workflow

These should NOT participate in a distributed transaction:

```text
Inventory Reservation
Payment Processing
Email Sending
```

Instead:

```text
Order Created Event
        ↓
Inventory Reserved Event
        ↓
Payment Succeeded Event
        ↓
Order Confirmed Event
        ↓
Email Sent
```

using eventual consistency.

---

# 2. Confirmation Flow

When an admin clicks **Submit**, the Order Service creates a Pending order and publishes an `OrderSubmitted` event using the Outbox Pattern. 
I would use **RabbitMQ** because the workload is moderate, the system already appears transactional, and RabbitMQ is operationally simpler than Kafka for this scale.

The Inventory Service reserves stock and emits `InventoryReserved`. The Payment Service creates a payment intent and emits `PaymentSucceeded` or `PaymentFailed`.

The Order Service consumes these events and updates the order state.

Each message contains:

```text
OrderId
MessageId
IdempotencyKey
```

Consumers maintain a processed-message table to ensure idempotency.

Retry policy:

```text
Immediate retry x3
Exponential backoff
Dead-letter queue
```

If payment succeeds but email fails, 
the order remains **Confirmed**. 
Email delivery is not part of the transaction boundary. 
The Notification Service retries automatically and eventually moves the message to a DLQ if retries are exhausted. Operations can replay the notification later without impacting the order.

---

# 3. Scenario 

### School Admin Re-submits Identical Order After Apparent Hang

Incident: Duplicate Order Submission

Symptoms:

* Admin reports clicking Submit twice.
* Two orders may appear within a short time window.
* Payment provider may show duplicate payment attempts.

Investigation:

1. Search OrderId and ParentEmail for the previous 5 minutes.
2. Check idempotency key associated with the original submission.
3. Verify whether the second request reused the same idempotency key.
4. Review Payment Service logs for duplicate payment intent creation.

Expected Behaviour:

* Duplicate submissions using the same idempotency key should return the existing order result.
* No second payment intent should be created.

Remediation:

* If only one order exists, no action required.
* If duplicate orders exist but only one payment succeeded, cancel the duplicate order.
* If duplicate payments succeeded, initiate refund workflow for the duplicate transaction and notify the school administrator.

Prevention:

* Enforce client-generated idempotency keys on submit.
* Disable the submit button after first click.
* Surface order-processing status to the UI rather than relying solely on browser request completion.

---