# E-Verland v2 — Project Analysis

**Project:** E-Commerce Backend API  
**Stack:** ASP.NET Core .NET 10, PostgreSQL, Redis, AWS  
**Architecture:** Modular Monolith + CQRS

---

## 1. Architecture Overview

E-Verland v2 keeps the modular monolith direction of v1, but makes the boundaries clearer and prepares the system for future extraction.

- Each feature is isolated by module: Auth, User, Product, Cart, Order, Payment, Chat, Notification, Redis.
- Each module owns its database and persistence layer.
- CQRS with MediatR is used to separate reads and writes.
- Redis is used for caching hot data and temporary state.
- OpenSearch is used for search instead of relying only on PostgreSQL.

This design keeps deployment simple while still improving scalability and maintainability.

---

## 2. System Limitations & Evolution from v1 to v2

v1 worked well as a clean modular monolith, but analysis showed several limits.

### Tight synchronous flow

v1 handled many flows through direct service calls and MediatR chains, for example Order -> Product -> Payment.

- Impact: higher latency when one module depends on another.
- Impact: a slow module can block the whole request chain.
- v2 direction: move important cross-module events to SQS or EventBridge.

### Database-heavy querying

v1 used PostgreSQL JSONB and GIN indexes for flexible filtering.

- Impact: good for medium scale, but full-text search and ranking become limited.
- Trade-off: PostgreSQL is simple and consistent, but less powerful for search.
- v2 direction: keep PostgreSQL as source of truth and move search to OpenSearch.

### Single-binary deployment coupling

v1 is deployed as one host.

- Impact: no independent scaling per module.
- Impact: even small changes require redeploying the whole system.
- v2 direction: keep the monolith for now, but design module boundaries so extraction to microservices is possible later.

### Lack of async workflow management

Some flows such as Order -> Payment -> Fulfillment were still synchronous.

- Impact: partial failure handling is weak.
- Impact: retries and compensation are difficult.
- v2 direction: introduce async workflows and saga-style coordination where needed.

### Summary of trade-offs

- SQS/EventBridge: better decoupling, but adds eventual consistency and more operational complexity.
- OpenSearch: better search quality and speed, but indexing adds sync delay.
- Redis: very fast reads, but cache invalidation must be managed carefully.

---

## 3. Key Design Decisions

### Modular Monolith

Chosen because it gives a clean codebase, simpler deployment, and a lower operational burden than microservices.

### CQRS with MediatR

Used to separate commands and queries clearly.

- Write side stays focused on business rules and validation.
- Read side can be optimized independently with Redis or OpenSearch.
- Pipeline behaviors support logging, validation, and cross-cutting concerns.

### Redis Caching

Used for hot reads such as product catalog, token cache, and cart data.

- Benefit: lower latency and reduced database load.
- Trade-off: cache consistency must be handled explicitly.

### OpenSearch for Search

Used for product search and filtering at scale.

- Benefit: better ranking, faster search, and easier full-text queries.
- Trade-off: search results are eventually consistent with the database.

### Security and Access Control

Keep the core security layer simple and predictable.

- JWT for authentication.
- RBAC for Admin, User, Seller.
- Rate limiting per module to reduce abuse.

---

## 4. Selected Implementations

### Stock Reservation

One of the strongest v2 improvements is stock reservation during checkout.

- Prevents overselling under high concurrency.
- Holds inventory for a short time while payment is processed.
- Releases stock automatically if payment fails or times out.

### Receiver Snapshot in Orders

Order shipping data is stored as a snapshot when the order is created.

- Keeps the order history immutable.
- Prevents later profile edits from changing past orders.

### OTP Email Verification

Email-based OTP is used for registration and password reset.

- Lower cost than SMS.
- Short expiry and attempt limits improve security.

### Real-time Notifications

SSE is used for lightweight real-time updates.

- Simple to implement.
- Good enough for notification delivery.
- Avoids the overhead of a heavier websocket stack for this scope.

---

## 5. Summary

v2 keeps the strengths of v1, but shifts the system toward a more scalable and resilient direction.

- From direct synchronous coupling to more async communication.
- From database-only search to OpenSearch.
- From implementation detail to architecture-driven decisions.
- From a functional monolith to a monolith that is ready for future decomposition.

In short: v2 is not about adding more code detail, but about showing how the system evolves after recognizing the real limits of v1.

---

**Document Version:** 2.0  
**Last Updated:** April 2026