# Understanding the changes

The project remains a personal store application: one ASP.NET Core API, one
SQL Server database, and the existing JavaScript website. Its existing layered
architecture was extended rather than replaced.

## 1. Consistent API errors

Before, an expected error could become a generic server failure, and the browser
could hide its useful explanation. The API now returns a consistent error body:
404 for a missing record, 400 for a business-rule failure, and a safe 500 message
for an unexpected failure. A trace ID connects that response to server logs.
The browser displays the explanation and still prioritizes field validation.

This changes error handling rather than stock, debt, or payment calculations.
Tests run the API in memory and check its actual HTTP responses.

## 2. Purchase batches and saved invoice prices

A product's prices are defaults. Each purchase line now saves its actual unit
cost, and each sale line saves the price agreed with that customer. Changing the
catalog later does not rewrite those saved invoice prices.

For your example:

| Receipt | Quantity | Cost per bottle | Total cost |
| --- | ---: | ---: | ---: |
| Earlier stock | 4 | 750 | 3,000 |
| New purchase | 2 | 780 | 1,560 |

Selling five uses the four older bottles and one newer bottle. The sale costs
3,780 and leaves one bottle costing 780. Selling at 900 gives revenue 4,500 and
gross profit 720. The default selling price can stay 900 throughout.

Two customers can buy the same product at 30 and 25: those prices belong to
their invoice lines. Payment and debt remain separate fields; the application
does not introduce a rule forcing them to equal the line totals.

FIFO allocations record which batches supplied each sale. Correcting a purchase
cost replays those allocations and updates affected sale costs. Correcting a
sale returns its old quantity before applying the replacement quantity. Edits
that leave insufficient stock roll back; the related sale quantities must also
be corrected when necessary.

Direct inventory increases require a cost for the added units. Decreases remove
oldest remaining stock. These adjustments do not create supplier payments or
purchase invoices automatically.

Older records lack original invoice prices. Migration uses current product
prices as explicitly marked estimates and creates opening stock. Editing an old
purchase reconstructs its product's historical ledger without adding the opening
stock twice. Historical stock discrepancies remain estimates, not recovered facts.

Backend tests exercise the costing rules and real relational persistence,
including corrections and rollback, using disposable SQLite databases. SQL
Server container checks verify the production migration path separately.

Receipt dates are compared as calendar dates, then by batch ID. This keeps
same-day entries in their recorded order even when one entry has a time and
another invoice stores midnight.

## 3. Screens

Purchase forms contain an editable unit cost. Sale forms contain an editable
customer price. Edit forms reload saved prices. Invoice details show quantities,
prices, totals, and estimate labels. Sales details additionally show FIFO cost
and gross profit. Inventory details show the remaining batches and their costs.

The existing sales payment auto-fill remains, using the agreed line prices.
The dashboard now shows monthly net profit: saved sale revenue minus FIFO cost
of units sold minus expenses. A breakdown appears above the daily profit chart.
Month and year selectors cover January 2021 through the current year. Sales and
expense dates determine their reporting month; buying unsold stock no longer
reduces profit. Historical estimates are labeled, while cash and stock alerts
remain live. Invoice payment, debt, and stock allocation rules are unchanged.

## 4. Docker and phone access

The Docker image publishes the API and serves the website from the same address.
Compose starts SQL Server first and retains its files in a named volume. The
website calls that address's API, which lets a phone use the computer's LAN IP.

Localhost is the default. Phone access requires the documented LAN binding and
private-network firewall configuration. The computer must remain running.
Docker uses a separate database: it does not silently move or replace your
existing Windows SQL Server data.

## 5. Verification and documentation

GitHub Actions builds the solution, runs backend and browser-script tests, builds
the Docker image, and checks startup with a fresh SQL Server. Its cleanup removes
only the disposable CI database. README contains local startup, phone access,
migration, data persistence, and verification instructions.

Two additional maintenance fixes were saved separately: OpenAPI was pinned to
the patched 2.7.5 release, and an old migration now releases the purchase foreign
key before replacing the supplier primary key. The following migration restores
that relationship. These changes repair dependencies and database setup rather
than change payment, debt, or invoice rules.

## Review examples

1. Add four units at 750; purchase two at 780; sell five and inspect the costs.
2. Sell one product at 30 to one customer and 25 to another.
3. Change the product defaults and check that old invoice prices stay unchanged.
4. Correct a purchase cost after selling its stock and inspect the sale cost.
5. Correct a fully sold sale's price or quantity.
6. Increase inventory without a purchase and enter the added stock's cost.
7. Try a quantity correction that cannot cover existing sales; verify that the
   original invoice and stock remain intact.

## Resume wording

Implemented FIFO batch inventory and invoice-level price snapshots in an
ASP.NET Core store application using EF Core, MediatR, and FluentValidation;
added transactional invoice corrections, xUnit regression tests, Docker
deployment with persistent SQL Server storage, and GitHub Actions verification.

Use this wording after verifying the finished project. Avoid unmeasured claims
about request throughput or production scale: this application serves a personal
inventory workflow.

## Shared dropdowns and low-stock summary

All pages now use a dark native dropdown palette with readable option text.
The dashboard low-stock alert opens Inventory with `?lowStock=true`. This view
shows only items whose quantity is at or below a configured minimum, including
items exactly at the minimum. It shows quantity, minimum, stock status, and the
out-of-stock count. Search stays within that view; Show all inventory clears it.
No stock thresholds or inventory quantities are changed.

## Invoice totals, original debt and correction credits

Sales and purchase invoices reject a mismatch between saved item totals and payment plus original debt, before changing stock or balances. Edit forms show original debt; remaining debt subtracts applied vouchers. Example: total110, originally paid40, original debt70, vouchers30 -> remaining40. If original debt becomes20 with vouchers30, remaining debt is0 and credit/refund due is10. The invoice view offers a dismissible reminder; credit stays visible and no cash refund or automatic credit application occurs. Dismissal is specific to the invoice and current amounts, stored in this browser. Historical mismatches are not automatically repriced.
