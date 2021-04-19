using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace AspNetCore.DynamicAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class DynamicController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        // ------------------------------------------------------------------------------------------------------------
        private static bool ValidateSecret(string value)
        {
            return value.Equals("12C1F7EF9AC8E288FBC2177B7F54D", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ValidateStoredProcedure(string name)
        {
            // Simple check sql injection
            if (name != null && (name.Contains(";") || name.Contains("SELECT") || name.Contains("DROP") || name.Contains("ALTER") || name.Contains("CREATE") || name.Contains("UPDATE") || name.Contains("DELETE") || name.Contains("INSERT") || name.Contains("EXEC")))
            {
                return false;
            }

            // You must check more security, example: check permission before return true;

            return true;
        }


        // ------------------------------------------------------------------------------------------------------------
        // CONSTRUCTOR
        // ------------------------------------------------------------------------------------------------------------

        public DynamicController(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            this._configuration = configuration;
            this._webHostEnvironment = webHostEnvironment;
            this._connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // ------------------------------------------------------------------------------------------------------------
        [HttpGet]
        [Produces("application/json")]
        [AllowAnonymous]
        [EnableCors("Allow-All")]
        public IActionResult Get(ApiVersion apiVersion)
        {
            return new OkObjectResult(new { ok = true, version = $"{apiVersion.MajorVersion}.{apiVersion.MinorVersion}" });
        }

        // ------------------------------------------------------------------------------------------------------------
        [HttpPost]
        [Produces("application/json")]
        [ProducesResponseType(200, Type = typeof(JsonResult))]
        [AllowAnonymous]
        [EnableCors("Allow-All")]
        public async Task<IActionResult> Post(ApiVersion apiVersion, [FromBody] dynamic body)
        {
            try
            {
                // VALIDATE TOKEN
                var authorizationHeader = Request.Headers["Authorization"].FirstOrDefault();

                if (authorizationHeader == null)
                {
                    return new BadRequestResult();
                }

                var key = authorizationHeader.Split(' ')[1];
                if (string.IsNullOrEmpty(key))
                {
                    return new BadRequestResult();
                }

                if (ValidateSecret(key) == false)
                {
                    return new BadRequestResult();
                }

                // DYNAMIC CONNECTION
                var connectionString = this._connectionString;
                var applicationName = Request.Headers["ApplicationName"].FirstOrDefault();
                if (applicationName != null)
                {
                    connectionString = this._configuration.GetConnectionString(applicationName + "Connection");
                }

                await using var db = new SqlConnection(connectionString);

                var sql = body.sqlCommand.Value as string;
              


                // VALIDATE SQL INJECTION
                if (ValidateStoredProcedure(sql) == false)
                {
                    return new BadRequestResult();
                }


                var jbody = new JObject(body);

                // READ PARAMETERS
                var parameters = new DynamicParameters();

                foreach (var p in jbody.GetValue("parameters").Children())
                {
                    var jparameter = (JProperty)p;
                    parameters.Add(jparameter.Name, jparameter.Value.ToString());
                }

                var items = await db.QueryAsync<dynamic>(sql: sql, param: parameters, commandType: CommandType.StoredProcedure);

                // FORMAT OUTPUT (APPLY FOR JSON AUTO - SQL SERVER 2016)
                var serializerSettings = new JsonSerializerSettings()
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };

                var finalResults = new List<ExpandoObject>();

                foreach (var result in items)
                {
                    dynamic finalResult = new ExpandoObject();
                    var finalProperties = (IDictionary<string, object>)finalResult;
                    var propertyValues = (IDictionary<string, object>)result;

                    foreach (var item in propertyValues)
                    {
                        finalProperties.Add(item.Key, item.Value);

                        if (item.Value == null) continue;

                        if (item.Value.ToString().StartsWith("{") || (item.Value.ToString().StartsWith("[{")))
                        {
                            var json = JsonConvert.DeserializeObject(item.Value.ToString(), serializerSettings);
                            finalProperties[item.Key] = json;
                        }
                    }

                    finalResults.Add((ExpandoObject)finalProperties);
                }

                return new OkObjectResult(new
                {
                    ok = true,
                    version = $"{apiVersion.MajorVersion}.{apiVersion.MinorVersion}",
                    results = finalResults
                });
            }
            catch (Exception e)
            {
                var response = new
                {
                    ok = false,
                    error = e.Message,
                    data = body
                };

                return new BadRequestObjectResult(response);
            }
        }
    }
}

