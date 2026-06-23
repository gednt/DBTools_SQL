-- Integration test schema for DBTools (PostgreSQL).

-- Create Users table
CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    email VARCHAR(100),
    age INT NOT NULL,
    status VARCHAR(50)
);

-- Create Orders table
CREATE TABLE IF NOT EXISTS orders (
    id SERIAL PRIMARY KEY,
    userid INT NOT NULL,
    product VARCHAR(100),
    amount DECIMAL(18, 2) NOT NULL
);

-- Insert test data
INSERT INTO users (id, name, email, age, status)
VALUES (1, 'Alice', 'alice@test.com', 25, 'active'), (2, 'Bob', 'bob@test.com', 30, 'active'), (3, 'Charlie', 'charlie@test.com', 16, 'inactive')
ON CONFLICT (id) DO NOTHING;

INSERT INTO orders (userid, product, amount)
VALUES (1, 'Widget', 10.00), (1, 'Gadget', 20.00), (2, 'Service', 15.50)
ON CONFLICT DO NOTHING;
