# Perfume Store

A personal inventory and accounting application built with ASP.NET Core 10,
SQL Server, EF Core, MediatR, FluentValidation, and a plain JavaScript website.
The API, Application, Domain, and Infrastructure projects separate presentation,
use cases, business rules, and persistence. The deployment is intended for one
owner using localhost or a phone on the same trusted network.

## Prices and stock

- Product **cost price** and **sale price** are defaults for new invoice entries.
  Recording a purchase at a different cost does not overwrite those defaults.
- Each purchase invoice line saves its own **unit cost**. Its stock is represented
  by a batch with received and remaining quantities.
- Each sales invoice line saves its own **unit sale price**. Enter the price agreed
  with that customer, including a price below the default. Later catalog changes
  do not rewrite saved invoice prices.
- FIFO uses batches ordered by purchase receipt date, then batch ID. Recorded
  sales are allocated in invoice creation order, which stays stable when an
  invoice is edited. Changing an invoice's displayed date does not move that sale
  ahead of previously recorded sales in the stock ledger.
- Sales details show line revenue, FIFO cost, and **gross profit**. Gross profit
  excludes expenses. Dashboard **net profit** subtracts expenses from gross profit.
- Inventory details show remaining units and cost for each batch.
- Direct stock additions require an entered `AddedUnitCost` for the extra units.
  Reducing the count removes the oldest remaining units. Purchases should normally
  be recorded through purchase invoices so supplier and payment information stays linked.
- Purchase and sale invoices can be corrected after stock is sold. Costs are
  recalculated without changing other customers' agreed prices. Quantity changes
  must still leave enough stock to cover recorded sales; correct the related
  sales first when a purchase quantity was overstated.

For example, receive **4 Naxos at 750** and then **2 at 780**, keeping the default
sale price at **900**. Stock cost is **4,560**. Selling five at 900 uses four units
at 750 and one at 780: cost **3,780**, revenue **4,500**, gross profit **720**.
The remaining bottle costs **780**. Selling an item for 30 to one customer and 25
to another keeps both agreed prices on their respective invoices.

### Existing data

The old schema did not save purchase/sale prices on invoice lines. The new
migration snapshots current product prices onto old lines and marks them as
**estimated**. It creates opening batches from current inventory at the current
product cost. Old sold units are not deducted a second time from that opening
stock. Historical costs remain estimates; the missing original prices cannot
be recovered from this schema. New purchases and sales retain their own prices.

When an old purchase is corrected, that product's historical receipts and sales
are reconstructed using the estimated line prices. Opening stock is reconciled
so the current count is preserved. Unrecorded historical stock losses are taken
from the oldest estimated receipts. These reconstructed costs remain marked as
estimated; a correction that exceeds a reconciled historical receipt requires
correcting the inventory count or related invoices first.

## Run with Docker

Use Docker with its **Linux engine running**. SQL Server needs an x64 machine.

1. Copy `.env.example` to `.env`.
2. Set `SQL_PASSWORD` to your own SQL Server password (at least eight characters,
   including upper/lowercase letters, numbers, and punctuation). Avoid semicolons
   because it is inserted into a connection string.
3. Run:

   ```sh
   docker compose up --build -d
   ```

4. Open <http://localhost:8080>. The homepage redirects to the dashboard.

The website and API share one address, so the phone never tries to contact its
own `localhost`. SQL Server is private to the Docker network. The app waits for
SQL Server readiness and applies EF migrations on startup. The named `sql-data`
volume retains the database when containers stop or are rebuilt.

```sh
docker compose logs -f app
docker compose down
```

`docker compose down` preserves the data volume. **Do not add `-v` when you want
to keep your data.** This container database is separate from your existing
Windows SQL Server database; Docker does not automatically import it.

### Phone on your home network

Set `BIND_ADDRESS=0.0.0.0` in `.env` and recreate the app:

```sh
docker compose up -d
```

Connect the phone to the same network and open
`http://YOUR-COMPUTER-LAN-IP:8080`. Allow this port through the computer's firewall
on the private network if necessary. The computer and Docker must remain on.
The application currently has no sign-in; this setup is for your trusted local
network and should not be forwarded to the public internet. Existing Bootstrap
and chart assets load from CDNs, so those assets need internet access.

## Existing Windows SQL Server / development

Before upgrading a database with real data, make a SQL Server backup. Point
`ConnectionStrings:PerfumeStore` at the intended database using your untracked
`appsettings.Development.json` or an environment variable.

```sh
dotnet tool restore
dotnet ef database update --project PerfumeStore.App/PerfumeStore.Infrastructure --startup-project PerfumeStore.App/PerfumeStore.API
dotnet run --project PerfumeStore.App/PerfumeStore.API
```

Automatic migrations are opt-in through `Database:Initialize`; only Compose
enables them by default. A regular local API launch does not automatically alter
your database. The original development connection string remains unchanged.
To review the SQL before applying it:

```sh
dotnet ef migrations script ConvertDebtDirectionToEnum AddInvoicePricesAndFifoStock --project PerfumeStore.App/PerfumeStore.Infrastructure --startup-project PerfumeStore.App/PerfumeStore.API
```

For local API hosting of the website, copy `PerfumeStore.Web` contents into
`PerfumeStore.App/PerfumeStore.API/wwwroot`, or use the Docker publish package.
The original separate frontend workflow on port 5500 still targets the API at
`https://localhost:7209/api`. Other hosting addresses use their own `/api` path;
`window.__API_BASE_URL__` remains available as an explicit override.

## Verification

```sh
dotnet build PerfumeStore.App/PerfumeStore.slnx -c Release
dotnet test PerfumeStore.App/PerfumeStore.slnx -c Release --no-build
node --test PerfumeStore.Web/tests/*.test.cjs
docker compose config --quiet
```

Backend tests cover error contracts, existing validation, FIFO costs, price
snapshots, relational persistence, invoice editing, and rollback. Relational
tests use a temporary SQLite database with a test-only model syntax adapter;
they never connect to the owner's SQL Server. Production migrations still use
SQL Server. CI also builds the Docker image and starts a fresh SQL Server/app
stack to check migration startup and the inventory endpoint.
The container smoke script also exercises invoice pricing, FIFO, corrections,
and rollback through HTTP against SQL Server. It creates test records and must
only target a disposable database:

```sh
node scripts/docker-smoke.cjs http://127.0.0.1:18080 --disposable
```

## API compatibility and unchanged behavior

The existing `Products` dictionary (product ID to quantity) remains supported.
Purchase commands add optional `UnitCosts`; sale commands add optional
`UnitPrices`. Both map product IDs to decimal amounts. An omitted create price
uses the product default; an omitted edit price preserves the saved price.

```json
{
  "Products": { "7": 2 },
  "UnitPrices": { "7": 25 }
}
```

Continue supplying the existing date, customer/supplier, payment, account, debt,
and notes fields. Creation and editing require line totals to equal the original
payment plus original debt (or payment alone when Has Debt is unchecked).
Invoice edits use original debt; later voucher payments reduce the remaining
debt separately. Excess voucher payments after a correction are displayed as
credit/refund due, with a dismissible reminder on the invoice view. Dismissal
is remembered in this browser and does not remove the credit information.
Credits never automatically change a money account or apply to another invoice.
Sales-create still auto-fills payment from agreed prices. Sales-edit stock
validation includes units returned from
the invoice being corrected, so a fully sold invoice can still be edited.

Expected API errors return Problem Details with a trace ID. Missing resources
return 404, business-rule failures return 400, and unexpected errors return a
safe 500 message while the exception is logged.

## Skills demonstrated

- Clean Architecture and CQRS with MediatR.
- Domain modelling for batch inventory, FIFO allocations, and price snapshots.
- EF Core migrations, transactional persistence, and backwards-compatible APIs.
- xUnit integration/regression tests and JavaScript tests.
- Docker multi-stage publishing, SQL Server persistence, and GitHub Actions CI.

No caching, queues, load-testing targets, or distributed services are needed for
this personal-use workload. Resume descriptions should report measured tests
and completed functionality, rather than claim unmeasured performance.

See [the change walkthrough](docs/CHANGE_GUIDE.md) for an explanation of each
group of changes and examples to try when reviewing the project.

## Dashboard month selection and profit

Choose a month and year from January 2021 through the current year. The default
is the current month. Sales revenue uses saved customer-specific line prices;
cost of units sold uses the saved FIFO cost totals. Net profit equals sales
revenue minus cost of units sold minus expenses for the selected month. Stock
purchases remain a separate KPI and do not reduce profit until those units sell.
The daily chart shows individual days, whose values sum to monthly net profit.
Historical estimates are marked. Cash balances and stock/debt alerts stay live.
API: `GET /api/Dashboard?year=2026&month=10`; omitted fields use the current period.
