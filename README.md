# Requirements:

- AspNet Core 5.0

#Nuget:

- Microsoft.AspNetCore.Mvc.Versioning
- Newtonsoft.Json
- Microsoft.AspNetCore.Mvc.NewtonsoftJson
- Swashbuckle.AspNetCore
- Microsoft.Data.SqlClient
- Dapper

# Startup:

```
public void ConfigureServices(IServiceCollection services)
{
    services.AddControllers();

    //CORS
    services.AddCors(o => o.AddPolicy("Allow-All", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    }));


    // JSON Camel Case Property Names
    // Nuget: Microsoft.AspNetCore.Mvc.NewtonsoftJson
    services.AddMvc().AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
    });


    // API VERSIONING
    // Nuget: Microsoft.AspNetCore.Mvc.Versioning
    services.AddApiVersioning(options =>
    {
        options.ReportApiVersions = true;
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.DefaultApiVersion = new ApiVersion(1, 0);
    });

    // SWAGGER
    // Register the Swagger generator, defining 1 or more Swagger documents
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Version = "v1",
            Title = "ASP.NET Core Web API",
            Description = "ASP.NET Core Web API",
            TermsOfService = new Uri("https://example.com/terms"),
            Contact = new OpenApiContact
            {
                Name = "Softech Corporation",
                Email = "tungnt@softech.vn",
                Url = new Uri("https://softech.vn")
            },
            License = new OpenApiLicense
            {
                Name = "Use under LICX",
                Url = new Uri("https://softech.vn")
            }
        });

        // Set the comments path for the Swagger JSON and UI.
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath);
    });

    services.AddRouting(options => options.LowercaseUrls = true);
}

// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    // Swagger
    // Enable middleware to serve generated Swagger as a JSON endpoint.
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ASP.NET Core Web API");
        //c.RoutePrefix = string.Empty;
    });


    app.UseHttpsRedirection();

    app.UseRouting();

    app.UseAuthorization();

    // CORS
    app.UseCors();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}

```

# Postman (Test API tool):

- Download: https://www.getpostman.com/

# Run project:

- At browser, type url: https://localhost:44338/swagger/index.html

# Prepare (Api will call a stored procedure):

- ConnectionString: look in appsettings.json
- Create a stored procedure:

```
CREATE OR ALTER PROCEDURE [p_API_GetProducts]
	@Color VARCHAR(50)
AS
BEGIN
	SELECT * FROM [SalesLT].[Product] WHERE Color = @Color
END
```

# Call api (testing):

- Endpoint url: https://localhost:44338/api/v1/dynamic
- Method: POST
- Content-Type: application/json
- Body:

```
{
    "sqlCommand": "[p_API_GetProducts]",
    "parameters": {
    	"color": "Black"
    }
}
```

# Swagger: https://localhost:44338/swagger/index.html

# Swagger on Azure: https://aspnetcoredynamicapi.azurewebsites.net/swagger/index.html

# Test on Azure (within Postman tool):

## GET: Only for testing (run correctly)

- Endpoint url: https://aspnetcoredynamicapi.azurewebsites.net/api/v1.0/dynamic

## POST (Primary API)

- Endpoint url: https://aspnetcoredynamicapi.azurewebsites.net/api/v1.0/dynamic
- Method: POST
- Header:
  - Content-Type: application/json
  - 'Authorization': 'Basic 12C1F7EF9AC8E288FBC2177B7F54D',
- Body:

```
{
    "sqlCommand": "[p_API_GetProducts]",
    "parameters": {
    	"color": "Black"
    }
}
```
