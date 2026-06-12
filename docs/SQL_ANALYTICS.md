# SQL Server Performance & Schema

# 1. Schema & Indexes

## Schools

```sql
CREATE TABLE Schools
(
    Id      INT IDENTITY PRIMARY KEY,
    Name    NVARCHAR(200) NOT NULL
);
```

---

## Products

```sql
CREATE TABLE Products
(
    Id          INT IDENTITY PRIMARY KEY,
    Sku         VARCHAR(50) NOT NULL UNIQUE,
    Category    VARCHAR(100) NOT NULL,
    BasePrice   DECIMAL(18,2) NOT NULL
);
```

### Indexes

```sql
CREATE INDEX IX_Products_Category
ON Products(Category);
```

---

## Orders

```sql
CREATE TABLE Orders
(
    Id              BIGINT IDENTITY PRIMARY KEY,
    SchoolId        INT NOT NULL,
    OrderDateUtc    DATETIME2 NOT NULL,

    CONSTRAINT FK_Orders_Schools
        FOREIGN KEY (SchoolId)
        REFERENCES Schools(Id)
);
```

### Indexes

```sql
CREATE INDEX IX_Orders_OrderDate_School
ON Orders(OrderDateUtc, SchoolId);

CREATE INDEX IX_Orders_School
ON Orders(SchoolId);
```

---

## OrderLines

```sql
CREATE TABLE OrderLines
(
    Id              BIGINT IDENTITY PRIMARY KEY,

    OrderId         BIGINT NOT NULL,
    ProductId       INT NOT NULL,

    Quantity        INT NOT NULL,
    UnitPrice       DECIMAL(18,2) NOT NULL,

    CONSTRAINT FK_OrderLines_Orders
        FOREIGN KEY (OrderId)
        REFERENCES Orders(Id),

    CONSTRAINT FK_OrderLines_Products
        FOREIGN KEY (ProductId)
        REFERENCES Products(Id)
);
```

### Indexes

```sql
CREATE INDEX IX_OrderLines_Order_Product
ON OrderLines(OrderId, ProductId)
INCLUDE(Quantity, UnitPrice);
```

### Notes

`UnitPrice` is stored on `OrderLines` intentionally.

Although the current product price exists in `Products.BasePrice`, analytics should use the price at the time of purchase. Storing a historical price snapshot prevents reporting inaccuracies when product prices change in the future.

---

# 2. Analytics Query

## Year-on-Year Revenue by School, Season, and Category

```sql
/*
Trade-off:

Season is calculated at query time rather than stored.

Pros:
- No duplicated data.
- No synchronization concerns.

Cons:
- Additional CPU during aggregation.

Given only four season buckets and indexed date access,
the simplicity outweighs the computation cost.
*/

WITH Sales AS
(
    SELECT
        YEAR(o.OrderDateUtc) AS SalesYear,

        CASE
            WHEN MONTH(o.OrderDateUtc) IN (12,1,2)
                THEN 'Winter'

            WHEN MONTH(o.OrderDateUtc) IN (3,4,5)
                THEN 'Spring'

            WHEN MONTH(o.OrderDateUtc) IN (6,7,8)
                THEN 'Summer'

            ELSE 'Autumn'
        END AS Season,

        s.Name AS SchoolName,

        p.Category,

        CAST(
            ol.Quantity * ol.UnitPrice
            AS DECIMAL(18,2)
        ) AS Revenue

    FROM Orders o

    INNER JOIN Schools s
        ON s.Id = o.SchoolId

    INNER JOIN OrderLines ol
        ON ol.OrderId = o.Id

    INNER JOIN Products p
        ON p.Id = ol.ProductId
)
SELECT
    SalesYear,
    SchoolName,
    Season,
    Category,
    SUM(Revenue) AS TotalRevenue
FROM Sales
GROUP BY
    SalesYear,
    SchoolName,
    Season,
    Category
ORDER BY
    SalesYear DESC,
    SchoolName,
    Season,
    Category;
```

---

## Potential Future Optimization

If analytics traffic becomes significant, I would consider adding persisted computed columns:

```sql
ALTER TABLE Orders
ADD OrderYear AS YEAR(OrderDateUtc) PERSISTED;
```

```sql
ALTER TABLE Orders
ADD Season AS
(
    CASE
        WHEN MONTH(OrderDateUtc) IN (12,1,2)
            THEN 'Winter'
        WHEN MONTH(OrderDateUtc) IN (3,4,5)
            THEN 'Spring'
        WHEN MONTH(OrderDateUtc) IN (6,7,8)
            THEN 'Summer'
        ELSE 'Autumn'
    END
) PERSISTED;
```

Supporting index:

```sql
CREATE INDEX IX_Orders_Year_Season_School
ON Orders(OrderYear, Season, SchoolId);
```

This reduces repeated date calculations during reporting and can improve performance on large datasets.

---

# 3. Performance Risk & Recommendation

## Dangerous Anti-Pattern

Applying functions to indexed date columns inside predicates:

```sql
WHERE YEAR(OrderDateUtc) = 2025
```

or

```sql
WHERE MONTH(OrderDateUtc) = 8
```

These predicates are non-SARGable and prevent SQL Server from performing efficient index seeks, often resulting in scans over large portions of the table.

---

## Message

Performance Concern: Non-SARGable Date Predicates

I noticed several analytics queries using expressions such as:

```sql
WHERE YEAR(OrderDateUtc) = 2025
```

or

```sql
WHERE MONTH(OrderDateUtc) = 8
```

This prevents SQL Server from using indexes efficiently and can force scans across large datasets. With approximately 8 million order line records, this pattern is likely contributing to slow analytics performance during peak traffic periods.

Instead, we should use range predicates:

```sql
WHERE OrderDateUtc >= '2025-01-01'
  AND OrderDateUtc <  '2026-01-01'
```

This allows SQL Server to perform index seeks and significantly reduces the amount of data scanned.

If year and season filtering remain common reporting requirements, I recommend introducing persisted computed columns with supporting indexes to further improve query performance while keeping reporting logic simple and maintainable.
