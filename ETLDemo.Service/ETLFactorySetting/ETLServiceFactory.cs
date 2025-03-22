using Common.Config;
using ETL.Service.Model;
using ETL.Service.Repo.Interface;
using Microsoft.Extensions.Options;


namespace ETL.Service.ETLDemoFactorySetting
{
    public class ETLDemoServiceFactory
    {
        public static IETLService GetETLDemoService(DbConfig dbConfig, ETLAppSettings appsettings)
        {
            switch (dbConfig.DataProvider.ToLower().Trim())
            {
                case "sqlserver": return new ETL.Service.Repo.MSSQL.ETLService(dbConfig, appsettings);
                case "oracle": return new ETL.Service.Repo.Oracle.ETLService(dbConfig, appsettings);
                case "postgresql": return new ETL.Service.Repo.PostgreSql.ETLService(dbConfig, appsettings);
                default: return new ETL.Service.Repo.MSSQL.ETLService(dbConfig, appsettings);
            }
        }
    }
}
