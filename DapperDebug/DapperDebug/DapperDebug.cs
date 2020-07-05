using System;
using System.Data;
using System.Text;

namespace DapperDebug
{
    public static class DapperDebug
    {
        /// <summary>
        /// Creates table type object from DataTable
        /// </summary>
        /// <param name="table"></param>
        /// <param name="tblTypeName"></param>
        /// <returns></returns>
        public static string GetTableTypeSql(DataTable table, string tblTypeName)
        {
            StringBuilder sqlStr = new StringBuilder();
            sqlStr.AppendFormat($"INSERT INTO {tblTypeName} values ");

            for (int r = 0; r < table.Rows.Count; r++) {
                if (r >= 1) {
                    sqlStr.AppendFormat(", ");
                }

                for (int i = 0; i < table.Columns.Count; i++) {
                    if (i == 0) {
                        sqlStr.AppendFormat("(");
                    }

                    switch (table.Columns[i].DataType.ToString().ToUpper())
                    {
                        case "SYSTEM.INT16":
                        case "SYSTEM.INT32":
                        case "SYSTEM.INT64":
                        case "SYSTEM.DECIMAL":
                        case "SYSTEM.DOUBLE":
                        case "SYSTEM.SINGLE":
                            string num = table.Rows[r].ItemArray[i].ToString().Trim();

                            if (!String.IsNullOrWhiteSpace(num)) {
                                sqlStr.AppendFormat("{0}", num);
                            } else {
                                sqlStr.Append("''");
                            }

                            break;
                        case "SYSTEM.DATETIME":
                            sqlStr.AppendFormat("CAST('{0}' AS DATETIME)", table.Rows[r].ItemArray[i]);
                            break;
                        case "SYSTEM.STRING":
                            sqlStr.AppendFormat("N'{0}'", table.Rows[r].ItemArray[i]);
                            break;
                        case "SYSTEM.BOOLEAN":
                            bool val;
                            Boolean.TryParse(table.Rows[r].ItemArray[i].ToString(), out val);

                            if (val == true) {
                                sqlStr.AppendFormat("1");
                            } else {
                                sqlStr.AppendFormat("0");
                            }

                            break;
                        default:
                            sqlStr.AppendFormat("{0}", table.Rows[r].ItemArray[i]);
                            break;
                    }

                    if (i != (table.Columns.Count - 1)) {
                        sqlStr.Append(", ");
                    } else {
                        // end of row
                        sqlStr.AppendFormat(")\n");
                    }
                }
            }

            return sqlStr.ToString();
        }

        /// <summary>
        /// Used for debugging.
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static string GetCreateTableSql(DataTable table)
        {
            StringBuilder sqlStr = new StringBuilder();
            StringBuilder alterSql = new StringBuilder();

            sqlStr.AppendFormat("CREATE TABLE [{0}] (", table.TableName);

            for (int i = 0; i < table.Columns.Count; i++) {
                bool isNumeric = false;
                bool usesColumnDefault = true;

                sqlStr.AppendFormat("\n\t[{0}]", table.Columns[i].ColumnName);

                switch (table.Columns[i].DataType.ToString().ToUpper())
                {
                    case "SYSTEM.INT16":
                        sqlStr.Append(" smallint");
                        isNumeric = true;
                        break;
                    case "SYSTEM.INT32":
                        sqlStr.Append(" int");
                        isNumeric = true;
                        break;
                    case "SYSTEM.INT64":
                        sqlStr.Append(" bigint");
                        isNumeric = true;
                        break;
                    case "SYSTEM.DATETIME":
                        sqlStr.Append(" datetime");
                        usesColumnDefault = false;
                        break;
                    case "SYSTEM.STRING":
                        sqlStr.AppendFormat(" nvarchar({0})", table.Columns[i].MaxLength);
                        break;
                    case "SYSTEM.SINGLE":
                        sqlStr.Append(" single");
                        isNumeric = true;
                        break;
                    case "SYSTEM.DOUBLE":
                        sqlStr.Append(" double");
                        isNumeric = true;
                        break;
                    case "SYSTEM.DECIMAL":
                        sqlStr.AppendFormat(" decimal(18, 6)");
                        isNumeric = true;
                        break;
                    default:
                        sqlStr.AppendFormat(" nvarchar({0})", table.Columns[i].MaxLength);
                        break;
                }

                if (table.Columns[i].AutoIncrement) {
                    sqlStr.AppendFormat(" IDENTITY({0},{1})",
                        table.Columns[i].AutoIncrementSeed,
                        table.Columns[i].AutoIncrementStep);
                } else {
                    // DataColumns will add a blank DefaultValue for any AutoIncrement column. 
                    // We only want to create an ALTER statement for those columns that are not set to AutoIncrement. 
                    if (table.Columns[i].DefaultValue != null) {
                        if (usesColumnDefault) {
                            if (isNumeric) {
                                alterSql.AppendFormat(
                                    "\nALTER TABLE {0} ADD CONSTRAINT [DF_{0}_{1}]  DEFAULT ({2}) FOR [{1}];",
                                    table.TableName,
                                    table.Columns[i].ColumnName,
                                    table.Columns[i].DefaultValue);
                            } else {
                                alterSql.AppendFormat(
                                    "\nALTER TABLE {0} ADD CONSTRAINT [DF_{0}_{1}]  DEFAULT ('{2}') FOR [{1}];",
                                    table.TableName,
                                    table.Columns[i].ColumnName,
                                    table.Columns[i].DefaultValue);
                            }
                        } else  {
                            // Default values on Date columns, e.g., "DateTime.Now" will not translate to SQL.
                            // This inspects the caption for a simple XML string to see if there is a SQL compliant default value, e.g., "GETDATE()".
                            try
                            {
                                System.Xml.XmlDocument xml = new System.Xml.XmlDocument();

                                xml.LoadXml(table.Columns[i].Caption);

                                alterSql.AppendFormat(
                                    "\nALTER TABLE {0} ADD CONSTRAINT [DF_{0}_{1}]  DEFAULT ({2}) FOR [{1}];",
                                    table.TableName,
                                    table.Columns[i].ColumnName,
                                    xml.GetElementsByTagName("defaultValue")[0].InnerText);
                            }
                            catch
                            {
                                // Handle
                            }
                        }
                    }
                }

                if (!table.Columns[i].AllowDBNull) {
                    sqlStr.Append(" NOT NULL");
                }

                sqlStr.Append(",");
            }

            if (table.PrimaryKey.Length > 0) {
                StringBuilder primaryKeySql = new StringBuilder();

                primaryKeySql.AppendFormat("\n\tCONSTRAINT PK_{0} PRIMARY KEY (", table.TableName);

                for (int i = 0; i < table.PrimaryKey.Length; i++)
                {
                    primaryKeySql.AppendFormat("{0},", table.PrimaryKey[i].ColumnName);
                }

                primaryKeySql.Remove(primaryKeySql.Length - 1, 1);
                primaryKeySql.Append(")");

                sqlStr.Append(primaryKeySql);
            } else {
                sqlStr.Remove(sqlStr.Length - 1, 1);
            }

            sqlStr.AppendFormat("\n);\n{0}", alterSql.ToString());

            return sqlStr.ToString();
        }
    }
}
