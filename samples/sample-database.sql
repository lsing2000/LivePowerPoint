-- LivePowerPoint sample database (SQLite). The data service applies this on first start.
CREATE TABLE IF NOT EXISTS datasets (
  dataset_id TEXT PRIMARY KEY,
  version    INTEGER NOT NULL,
  updated_at TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS sales_summary (
  department TEXT PRIMARY KEY, sales INTEGER NOT NULL, status TEXT NOT NULL, sort_order INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS inventory (
  item TEXT PRIMARY KEY, stock INTEGER NOT NULL, health REAL NOT NULL, sort_order INTEGER NOT NULL
);
INSERT OR IGNORE INTO datasets VALUES ('sales-summary', 1, strftime('%Y-%m-%dT%H:%M:%SZ','now'));
INSERT OR IGNORE INTO datasets VALUES ('inventory',     1, strftime('%Y-%m-%dT%H:%M:%SZ','now'));
INSERT OR IGNORE INTO sales_summary VALUES ('North', 125000, 'On target', 1);
INSERT OR IGNORE INTO sales_summary VALUES ('South',  98500, 'Below target', 2);
INSERT OR IGNORE INTO sales_summary VALUES ('West',  143200, 'Above target', 3);
INSERT OR IGNORE INTO inventory VALUES ('Widgets', 420, 0.82, 1);
INSERT OR IGNORE INTO inventory VALUES ('Gadgets', 135, 0.41, 2);
INSERT OR IGNORE INTO inventory VALUES ('Gizmos',  980, 0.95, 3);
-- Manual edit example (then bump the version so clients see it):
-- UPDATE sales_summary SET sales = 150000 WHERE department = 'North';
-- UPDATE datasets SET version = version + 1, updated_at = strftime('%Y-%m-%dT%H:%M:%SZ','now') WHERE dataset_id = 'sales-summary';
