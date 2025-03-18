using Common.Config;
using ETL.Service.Repo.Interface;


namespace ETL.Service.ETLDemoFactorySetting
{
    public class ETLDemoServiceFactory
    {
        public static IETLService GetETLDemoService(DbConfig dbConfig)
        {
            switch (dbConfig.DataProvider.ToLower().Trim())
            {
                case "sqlserver": return new ETL.Service.Repo.MSSQL.ETLService(dbConfig);
                case "oracle": return new ETL.Service.Repo.Oracle.ETLService(dbConfig);
                case "postgresql": return new ETL.Service.Repo.PostgreSql.ETLService(dbConfig);
                default: return new ETL.Service.Repo.MSSQL.ETLService(dbConfig);
            }
        }
    }
}
