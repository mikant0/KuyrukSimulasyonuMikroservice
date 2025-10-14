using KuyrukSimulasyonuMikroservice.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;



namespace KuyrukSimulasyonuMikroservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    
    public class KuyrukController : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping()
           => Ok(new { message = "pong", time = DateTime.UtcNow });
        [HttpGet("sample")]
        public IEnumerable<QueueRecord> Sample()
        {
            var baseDate = new DateTime(2024, 9, 1, 13, 10, 0);
            return new[]
            {
                new QueueRecord { PointId="BN01", Timestamp=baseDate,               DurationMin=335 },
                new QueueRecord { PointId="BN01", Timestamp=baseDate.AddMinutes(5), DurationMin=185 },
                new QueueRecord { PointId="BN02", Timestamp=baseDate,               DurationMin=120 },
                new QueueRecord { PointId="BN03", Timestamp=baseDate.AddMinutes(10),DurationMin= 90  },
            };
            }

        [HttpPost("query")]
        public async Task<IActionResult> QueryFromSql([FromBody] DbConnectRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Server) || string.IsNullOrWhiteSpace(req.Database))
                return BadRequest(new { error = "Server ve Database zorunludur." });

            var cs = string.IsNullOrWhiteSpace(req.UserId)
                ? $"Server={req.Server};Database={req.Database};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;"
                : $"Server={req.Server};Database={req.Database};User Id={req.UserId};Password={req.Password};TrustServerCertificate=True;Connect Timeout=5;";

            string ToIdent(string input)
            {
                if (string.IsNullOrWhiteSpace(input)) return "[dbo].[KuyrukKaydi]";
                var s = input.Trim().Trim('[', ']');
                if (s.Contains('.'))
                {
                    var p = s.Split('.', 2);
                    return $"[{p[0]}].[{p[1]}]";
                }
                return $"[dbo].[{s}]";
            }

            var table = ToIdent(req.Table);

            try
            {
                var result = new List<QueueRecord>();

                await using var con = new SqlConnection(cs);
                await con.OpenAsync();

                var cmd = new SqlCommand($"SELECT PointId, [Timestamp], DurationMin FROM {table} ORDER BY [Timestamp];", con);
                await using var rdr = await cmd.ExecuteReaderAsync();

                while (await rdr.ReadAsync())
                {
                    result.Add(new QueueRecord
                    {
                        PointId = rdr.GetString(0),
                        Timestamp = rdr.GetDateTime(1),
                        DurationMin = rdr.GetInt32(2)
                    });
                }

                // her zaman JSON döndür
                return new JsonResult(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { error = "SQL error", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "General error", message = ex.Message });
            }
        }

        private static string BuildTableIdentifier(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "[dbo].[KuyrukKaydi]";
        var s = input.Trim();
        s = s.Trim('[', ']');
        if (s.Contains('.'))
        {
            var parts = s.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
            var schema = parts[0].Trim('[', ']');
            var name = parts[1].Trim('[', ']');
            return $"[{schema}].[{name}]";
        }
        return $"[dbo].[{s}]";
    }
        // Bağlantıyı sadece dener (SELECT 1)
        [HttpPost("test-connection")]
        public async Task<IActionResult> TestConnection([FromBody] DbConnectRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Server) || string.IsNullOrWhiteSpace(req.Database))
                return BadRequest(new { error = "Server ve Database zorunludur." });

            var cs = string.IsNullOrWhiteSpace(req.UserId)
                ? $"Server={req.Server};Database={req.Database};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;"
                : $"Server={req.Server};Database={req.Database};User Id={req.UserId};Password={req.Password};TrustServerCertificate=True;Connect Timeout=5;";

            try
            {
                await using var con = new SqlConnection(cs);
                await con.OpenAsync();

                var cmd = new SqlCommand("SELECT @@VERSION", con);
                var ver = (string)await cmd.ExecuteScalarAsync();

                return Ok(new { ok = true, server = req.Server, database = req.Database, version = ver });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, error = ex.GetType().Name, message = ex.Message });
            }
        }

        // Tablo var mı? Kaç satır var?
        [HttpPost("inspect")]
        public async Task<IActionResult> Inspect([FromBody] DbConnectRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Server) || string.IsNullOrWhiteSpace(req.Database))
                return BadRequest(new { error = "Server ve Database zorunludur." });

            var cs = string.IsNullOrWhiteSpace(req.UserId)
                ? $"Server={req.Server};Database={req.Database};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;"
                : $"Server={req.Server};Database={req.Database};User Id={req.UserId};Password={req.Password};TrustServerCertificate=True;Connect Timeout=5;";

            // tablo adı -> [dbo].[KuyrukKaydi] formatına getirelim
            string ToIdent(string input)
            {
                if (string.IsNullOrWhiteSpace(input)) return "[dbo].[KuyrukKaydi]";
                var s = input.Trim().Trim('[', ']');
                if (s.Contains('.'))
                {
                    var p = s.Split('.', 2);
                    return $"[{p[0].Trim('[', ']')}].[{p[1].Trim('[', ']')}]";
                }
                return $"[dbo].[{s}]";
            }
            var tableIdent = ToIdent(req.Table);

            try
            {
                await using var con = new SqlConnection(cs);
                await con.OpenAsync();

                // tablo var mı?
                var existsCmd = new SqlCommand(@"
            SELECT 1 FROM sys.objects
            WHERE object_id = OBJECT_ID(@t) AND type IN ('U','V');", con);
                existsCmd.Parameters.AddWithValue("@t", tableIdent.Replace("[", "").Replace("]", ""));
                var exists = await existsCmd.ExecuteScalarAsync() != null;

                int rowCount = -1;
                if (exists)
                {
                    var cntCmd = new SqlCommand($"SELECT COUNT(*) FROM {tableIdent};", con);
                    rowCount = (int)await cntCmd.ExecuteScalarAsync();
                }

                return Ok(new { ok = true, table = tableIdent, exists, rowCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, error = ex.GetType().Name, message = ex.Message });
            }
        }



    }
}

