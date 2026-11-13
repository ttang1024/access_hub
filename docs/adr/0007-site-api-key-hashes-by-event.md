# ADR 0007: Site API key hashes distributed by integration event

- Status: Accepted
- Date: 2026-11-12

## Context

Site controllers authenticate to AccessControl (gRPC) and EventIngestion (REST). Cardholder owns sites and issues their keys. The gateway owns no data, and services may not call each other synchronously, so neither the gateway nor a service can ask Cardholder whether a key is valid.

## Decision

- A key is 32 random bytes, shown once at registration and on rotation as base64url, and stored only as a SHA-256 hash.
- `SiteRegistered` and `SiteApiKeyRotated` carry the hash. AccessControl and EventIngestion store it in their own databases, version-guarded, and authenticate `X-Site-Id` + `X-Api-Key` themselves, comparing hashes with `CryptographicOperations.FixedTimeEquals`.
- The site ID comes from the authenticated key, never from the request body. Ingestion rejects a route site ID that differs (403), and `AccessListSync` has no site parameter at all.

## Alternatives rejected

- **Validate at the gateway.** The gateway would need its own datastore, and a service reached by any other path would trust unauthenticated traffic.
- **Call Cardholder to validate.** A synchronous dependency: site traffic would stop whenever Cardholder is down.
- **Signed tokens issued to sites.** Removes the lookup but adds token refresh at the edge, which has to work through outages, and makes immediate revocation harder.

## Consequences

- Site traffic keeps working while Cardholder is down.
- A hash in a broker message reveals nothing usable. The key has 256 bits of entropy, so an unsalted SHA-256 hash cannot be brute-forced; a slow password hash would add cost to every request for no gain.
- Rotation is eventually consistent: the old key keeps working until each service consumes `SiteApiKeyRotated`, normally well under a second. This is tested in EventIngestion.
- Locking out a compromised key means rotating it. An immediate lock-out without a replacement would need a separate "revoke key" event.
