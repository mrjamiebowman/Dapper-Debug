# Dapper Debug
I created this to help debug and test stored procedures in Dapper. I used this with Dapper to convert DataTables to SQL Table Type string statements.

TODO: Docker Compose with MSSQL Server and sample database.
TODO: Setup sample project.
TODO: Add more debugging functionality.

## Convert Table Types to SQL
If you are using stored procedures with custom table types. This is extreamly useful for intercepting the data that is being sent to the stored procedure.

    /* for testing */
    if (debug == true) {
        string tableTypeSql = DapperDebug.GetTableTypeSql(dataTable, "@TblTypeObj");
        System.Diagnostics.Debug.WriteLine($"-- Stored Procedure: uspStoredProcedure");
        System.Diagnostics.Debug.WriteLine($"{tableTypeSql}");
    }
