-- Integration test schema for DBTools (SQLite).
-- This script is run manually to create the SQLite database file.

CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT,
    email TEXT,
    age INTEGER NOT NULL,
    status TEXT
);

CREATE TABLE IF NOT EXISTS orders (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    userid INTEGER NOT NULL,
    product TEXT,
    amount REAL NOT NULL
);

INSERT OR IGNORE INTO users (id, name, email, age, status) VALUES
    (1, 'Alice', 'alice@test.com', 25, 'active'),
    (2, 'Bob', 'bob@test.com', 30, 'active'),
    (3, 'Charlie', 'charlie@test.com', 16, 'inactive');

INSERT OR IGNORE INTO orders (userid, product, amount) VALUES
    (1, 'Widget', 10.00),
    (1, 'Gadget', 20.00),
    (2, 'Service', 15.50);
