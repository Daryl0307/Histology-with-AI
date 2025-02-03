using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Reflection;
using System.Security.Cryptography;
namespace RP.SOI.DotNet.Utils;

public static class DBUtl
{
    /* DB_SQL made a property to return the final SQL string
       after substitutions to assist debugging 2022 PK */
    public static string DB_Message { get; set; } = "";
    public static string DB_SQL { get; set; } = "";

    static readonly string? env =
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    private static readonly IConfiguration config =
       new ConfigurationBuilder()
          .SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile("appsettings.json")
          .AddJsonFile($"appsettings.{env}.json", optional: true)
          .Build()
          .GetSection("ConnectionStrings");

    private static readonly string DB_CONNECTION = config.GetValue<String>("DefaultConnection") ?? "";

    public static List<dynamic> GetList(string sql, params object[] list)
    {
        return GetTable(sql, list).ToDynamic();
    }

    public static List<ModelClass> GetList<ModelClass>(string sql, params object[] list)
    {
        return GetTable(sql, list).ToStatic<ModelClass>();
    }

    private static List<DTO> ToStatic<DTO>(this DataTable dt)
    {
        var list = new List<DTO>();
        foreach (DataRow row in dt.Rows)
        {
            DTO obj = (DTO)Activator.CreateInstance(typeof(DTO))!;
            foreach (DataColumn column in dt.Columns)
            {
                PropertyInfo? Prop = obj.GetType().GetProperty(column.ColumnName, BindingFlags.Public | BindingFlags.Instance);
                //Debug.WriteLine(obj.GetType().GetProperty(column.ColumnName, BindingFlags.Public | BindingFlags.Instance));
                if (row[column] == DBNull.Value)
                {
                    Prop?.SetValue(obj, null);
                }
                else
                {
                    //Debug.WriteLine( row[column].GetType() + " " + Prop?.PropertyType);
                    if (row[column].GetType() == Prop?.PropertyType)
                    {
                        Prop?.SetValue(obj, row[column]);
                        //Debug.WriteLine(row[column]);
                    }
                }
            }
            list.Add(obj);
        }
        return list;
    }

    private static List<dynamic> ToDynamic(this DataTable dt)
    {
        var dynamicDt = new List<dynamic>();
        foreach (DataRow row in dt.Rows)
        {
            dynamic dyn = new ExpandoObject();
            foreach (DataColumn column in dt.Columns)
            {
                var dic = (IDictionary<string, object>)dyn;
                dic[column.ColumnName] = row[column];
            }
            dynamicDt.Add(dyn);
        }
        return dynamicDt;
    }

    public static DataTable GetTable(string sql, params object?[] list)
    {
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] is string parameter)
            {
                list[i] = parameter?.EscQuote(); // prevent SQL Injection
            }
        }

        DB_Message = "";
        DB_SQL = string.Format(sql, list);
        DataTable dt = new();
        using SqlConnection dbConn = new(DB_CONNECTION);
        using SqlDataAdapter dAdptr = new(DB_SQL, dbConn);

        try
        {
            dAdptr.Fill(dt);
            return dt;
        }
        catch (Exception ex)
        {
            DB_Message = ex.Message;
            return dt;
        }
    }

    public static int ExecSQL(string sql, params object?[] list)
    {
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] is string parameter)
            {
                list[i] = parameter?.EscQuote(); // prevent SQL Injection
            }
        }

        DB_Message = "";
        DB_SQL = string.Format(sql, list);


        int rowsAffected = 0;
        using (SqlConnection dbConn = new(DB_CONNECTION))
        using (SqlCommand dbCmd = dbConn.CreateCommand())
        {
            try
            {
                dbConn.Open();
                dbCmd.CommandText = DB_SQL;
                rowsAffected = dbCmd.ExecuteNonQuery();
            }

            catch (Exception ex)
            {
                DB_Message = ex.Message;
                rowsAffected = -1;
            }
        }
        return rowsAffected;
    }
    public static int ExecSQLReturnId(string sql, params object?[] list)
    {
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] is string parameter)
            {
                list[i] = parameter?.EscQuote(); // prevent SQL Injection
            }
        }

        DB_Message = "";
        DB_SQL = string.Format(sql, list);

        int insertedId = 0;
        using (SqlConnection dbConn = new(DB_CONNECTION))
        using (SqlCommand dbCmd = dbConn.CreateCommand())
        {
            try
            {
                dbConn.Open();
                dbCmd.CommandText = DB_SQL;
                insertedId = Convert.ToInt32(dbCmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                DB_Message = ex.Message;
                insertedId = -1;
            }
        }
        return insertedId;
    }
    public static object? GetValue(string sql, params object?[] list)
    {
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] is string parameter)
            {
                list[i] = parameter?.EscQuote(); // prevent SQL Injection
            }
        }

        DB_Message = "";
        DB_SQL = string.Format(sql, list);

        object? result = null;
        using (SqlConnection dbConn = new(DB_CONNECTION))
        using (SqlCommand dbCmd = dbConn.CreateCommand())
        {
            try
            {
                dbConn.Open();
                dbCmd.CommandText = DB_SQL;
                result = dbCmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                DB_Message = ex.Message;
            }
        }
        return result;
    }

    public static string EscQuote(this string line)
    {
        return line?.Replace("'", "''")!;
    }

    public static string HashPassword(string password)
    {
        using (var sha1 = SHA1.Create())
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(password);
            byte[] hash = sha1.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }
    }


}