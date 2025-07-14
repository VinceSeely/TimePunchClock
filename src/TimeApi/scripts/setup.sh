#!/bin/bash
set -e

# Wait for SQL Server to come up
echo "Waiting for SQL Server to be ready..."
for i in {1..60}; do
    if /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -Q "SELECT 1" &> /dev/null; then
        echo "SQL Server is ready"
        break
    fi
    echo "Waiting...$i"
    sleep 1
done

# Run the setup script to create the DB and schema
echo "Running initialization script..."
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -d master -i /scripts/init.sql

echo "SQL Server initialized successfully"
