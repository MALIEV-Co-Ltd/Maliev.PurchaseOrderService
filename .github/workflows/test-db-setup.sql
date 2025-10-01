-- PostgreSQL performance optimization script for CI testing
-- This file contains the SQL commands used to optimize PostgreSQL for CI testing

-- Disable fsync for faster writes (safe for test environments)
ALTER SYSTEM SET fsync = off;

-- Disable full page writes to speed up checkpoints
ALTER SYSTEM SET full_page_writes = off;

-- Disable synchronous commits for faster transactions
ALTER SYSTEM SET synchronous_commit = off;

-- Increase checkpoint segments for better performance
ALTER SYSTEM SET checkpoint_segments = 32;

-- Optimize checkpoint completion target
ALTER SYSTEM SET checkpoint_completion_target = 0.9;

-- Increase WAL buffers for better write performance
ALTER SYSTEM SET wal_buffers = '16MB';

-- Increase shared buffers for better caching
ALTER SYSTEM SET shared_buffers = '256MB';

-- Apply the configuration changes
SELECT pg_reload_conf();