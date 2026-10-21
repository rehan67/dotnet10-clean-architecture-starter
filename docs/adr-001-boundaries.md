# ADR 001: Inward dependencies

Accepted. Keep domain policy independent of EF Core, transport and database adapters. Use a small application port rather than a generic persistence abstraction.

Course capacity is an invariant under concurrent requests, so PostgreSQL enforces a serialised critical section using a row lock. Database write failures roll back the transaction. SQLite tests cannot establish PostgreSQL locking behaviour; CI separately tests it.

Trade-off: the row lock limits hot-course write throughput. This reference optimises for understandable correctness, not maximum admission throughput.
