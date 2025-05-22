using Microsoft.EntityFrameworkCore;

namespace ORM_testing
{
    public class WeatherForecastDbContex : DbContext
    {
        public WeatherForecastDbContex(DbContextOptions<WeatherForecastDbContex> options) : base(options) { }
        public DbSet<WeatherForecast> weatherForecasts { get; set; }
    }
}
