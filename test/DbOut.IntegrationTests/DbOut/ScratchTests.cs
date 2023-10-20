using System.Data;
using MySqlConnector;

namespace DbOut;

public class ScratchTests
{
    [Fact]
    public async Task Scratch()
    {
        await using var connection = new MySqlConnection("Server=localhost;Database=db_export;UserId=root;Password=P@ssw0rd!");
        await connection.OpenAsync();
        var restrictions = new string[4];
        restrictions[2] = "users2";
        var schema = await connection.GetSchemaAsync("Columns", restrictions);
        var rows =   schema.Rows.Cast<DataRow>().Select(row => row.ItemArray).ToArray();
    }
}