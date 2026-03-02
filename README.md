# Instructions for candidates

This is the .NET version of the Payment Gateway challenge. If you haven't already read this [README.md](https://github.com/cko-recruitment/) on the details of this exercise, please do so now.

## Template structure

```
src/
    PaymentGateway.Api - a skeleton ASP.NET Core Web API
test/
    PaymentGateway.Api.Tests - an empty xUnit test project
imposters/ - contains the bank simulator configuration. Don't change this

.editorconfig - don't change this. It ensures a consistent set of rules for submissions when reformatting code
docker-compose.yml - configures the bank simulator
PaymentGateway.sln
```

Feel free to change the structure of the solution, use a different test library etc.

---

## Design and assumptions

- **Response shape:** The same DTO (`PostPaymentResponse`) is used for both POST (process payment) and GET (retrieve payment). The requirements list the same fields for both; reusing one type keeps the API simple.

- **Rejected vs Authorized/Declined:** When request validation fails, the API returns **400 Bad Request** with validation errors so the merchant can treat that as “Rejected” (invalid data, no call to the bank). The requirements say responses from the gateway should be Authorized, Declined, or Rejected; 400 + error details satisfies Rejected. Only **Authorized** and **Declined** are returned in the response body, as the spec states “Status must be one of … Authorized, Declined” for payments that reached the bank.

- **Bank 503:** When the bank simulator returns **503 Service Unavailable** (e.g. card ending in 0), the gateway returns **502 Bad Gateway** and does not store a payment. The request was valid but the bank was unavailable. The requirements do not say whether to persist Rejected payments. I assumed logging is sufficient for now and that we do **not** store or return a payment record for Rejected. Persisting Rejected for audit could be added later if needed.

- **Currency:** Validation is limited to **three** ISO codes as required: **GBP**, **USD**, **EUR**.

- **Storage:** An **in-memory** repository is used as allowed by the spec (“It is fine to use the test double repository provided in the sample code”).

- **OneOf for ProcessPayment:** The service uses **OneOf&lt;PostPaymentResponse, HttpStatusCode&gt;** so the controller can represent either a successful payment (with the response body) or a failure that should return a specific HTTP status (e.g. 502) without a payment body. This avoids throwing for expected bank failures and keeps the flow explicit.

- **Authorization code:** The bank returns an `authorization_code` which is **not** in the API response (the spec does not require it). I assume we need it for later processing (e.g. refunds, reconciliation), so it is stored on the domain **Payment** model and in the repository. The public response DTO omits it for clarity and PCI hygiene.

- **Expiry year** I assumed this be a 4 digit year i.e. 2026. It will fail validation if it is just a two digit number.

---

## Improvements for extending the app

- **Idempotency:** Make the POST payment endpoint idempotent by accepting an **idempotency key** in a request header (e.g. `Idempotency-Key`). Store the key with the payment or in a short-lived cache; on a duplicate request with the same key, return the existing payment response instead of calling the bank again. This avoids creating multiple payments when the client retries.

- **Database store:** Introduce an **IDatabase** or persist via the existing **IPaymentsRepository** with a real implementation (e.g. SQL Server, PostgreSQL). The in-memory repository can be swapped for a DB-backed one without changing the service contract.

- **Cancellation tokens:** Thread **CancellationToken** from the controller action through the service and into the bank **HttpClient** call (e.g. `ExecutePostAsync(..., cancellationToken)`). If using a real database, pass the same token into async DB operations so that client disconnects or timeouts cancel in-flight work and release resources.

- **CreatedAt and MerchantId:** Add a **CreatedAt** (e.g. `DateTimeOffset`) and a **MerchantId** (or similar) to the **Payment** model. CreatedAt supports reporting and reconciliation; MerchantId scopes payments to a merchant and allows multi-tenant storage and retrieval (e.g. GET only returns payments for the authenticated merchant).

---

## Starting the app

Start the bank simulator first:

```bash
docker-compose up -d
```

Then start the Api:

```bash
dotnet run --project src/PaymentGateway.Api
```

## Testing

```bash
dotnet test
```

The Integration tests require the bank simulator to be running (`docker-compose up -d`).
