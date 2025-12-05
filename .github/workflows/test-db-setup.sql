-- PostgreSQL performance optimization script for CI testing
-- This file contains the SQL commands used to optimize PostgreSQL for CI testing
--
-- IMPORTANT: These commands must be executed separately (not in a transaction block)
-- because ALTER SYSTEM cannot run inside a transaction.
--
-- Usage in CI workflows:
-- psql -h localhost -p 5432 -U postgres -d test_db -c "ALTER SYSTEM SET fsync = off"
-- psql -h localhost -p 5432 -U postgres -d test_db -c "ALTER SYSTEM SET full_page_writes = off"
-- ... (each command separately)

-- Disable fsync for faster writes (safe for test environments)
ALTER SYSTEM SET fsync = off;

-- Disable full page writes to speed up checkpoints
ALTER SYSTEM SET full_page_writes = off;

-- Disable synchronous commits for faster transactions
ALTER SYSTEM SET synchronous_commit = off;

-- Optimize checkpoint completion target
ALTER SYSTEM SET checkpoint_completion_target = 0.9;

-- Increase WAL buffers for better write performance
ALTER SYSTEM SET wal_buffers = '16MB';

-- Increase shared buffers for better caching
ALTER SYSTEM SET shared_buffers = '256MB';

-- Apply the configuration changes
SELECT pg_reload_conf();